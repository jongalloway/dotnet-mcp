#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that the codecov.yml exclusion patterns are working correctly.

.DESCRIPTION
    This script helps verify that the coverage exclusion patterns defined in codecov.yml
    match the files we intend to exclude from coverage reports. It parses the latest
    coverage report and checks which files would be excluded by our patterns.

.PARAMETER CoverageFile
    Path to a Cobertura coverage XML file. If not specified, searches for the latest
    coverage file in the test output directory.

.EXAMPLE
    pwsh -File scripts/validate-coverage-exclusions.ps1

.EXAMPLE
    pwsh -File scripts/validate-coverage-exclusions.ps1 -CoverageFile coverage.cobertura.xml
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$CoverageFile
)

$ErrorActionPreference = "Stop"

function ConvertGlobToRegex {
    param([string]$Pattern)
    
    # Normalize path separators
    $regex = $Pattern -replace '\\', '/'
    
    # Escape regex special chars except * and ?
    $regex = [Regex]::Escape($regex)
    
    # Convert glob patterns to regex
    $regex = $regex -replace '\\\*\\\*/', '.*?/'  # **/ matches any path segments
    $regex = $regex -replace '\\\*\\\*', '.*'     # ** matches everything
    $regex = $regex -replace '\\\*', '[^/]*'      # * matches within a segment
    $regex = $regex -replace '\\\?', '.'          # ? matches single char
    
    return "^$regex`$"
}

function Test-ExclusionMatch {
    param(
        [string]$FilePath,
        [string[]]$Patterns
    )
    
    # Normalize path
    $normalizedPath = $FilePath -replace '\\', '/'
    
    foreach ($pattern in $Patterns) {
        $regex = ConvertGlobToRegex $pattern
        if ($normalizedPath -match $regex) {
            return $pattern
        }
    }
    
    return $null
}

# Find codecov.yml
$repoRoot = $PSScriptRoot | Split-Path -Parent
$codecovPath = Join-Path $repoRoot "codecov.yml"

if (-not (Test-Path $codecovPath)) {
    Write-Error "codecov.yml not found at: $codecovPath"
    exit 1
}

Write-Host "Reading codecov.yml from: $codecovPath" -ForegroundColor Cyan

# Parse exclusion patterns from codecov.yml
$codecovContent = Get-Content $codecovPath -Raw
$ignoreSection = $codecovContent -match '(?ms)ignore:\s*(.*?)(?=\n\S|\z)'
if (-not $ignoreSection) {
    Write-Error "Could not find 'ignore:' section in codecov.yml"
    exit 1
}

# Extract patterns (lines starting with - and quoted strings)
$patterns = [System.Collections.ArrayList]::new()
$codecovContent -split "`n" | ForEach-Object {
    if ($_ -match '^\s*-\s*["''](.+?)["'']') {
        $null = $patterns.Add($Matches[1])
    }
}

Write-Host "Found $($patterns.Count) exclusion patterns:" -ForegroundColor Green
$patterns | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
Write-Host ""

# Find coverage file
if (-not $CoverageFile) {
    $testOutput = Join-Path $repoRoot "DotNetMcp.Tests/bin/Release"
    $coverageFiles = Get-ChildItem -Path $testOutput -Filter "*.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found. Run tests with coverage first:"
        Write-Host "  dotnet test --project DotNetMcp.Tests/DotNetMcp.Tests.csproj -c Release -- --coverage --coverage-output-format cobertura"
        exit 0
    }
    
    $CoverageFile = ($coverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}

if (-not (Test-Path $CoverageFile)) {
    Write-Error "Coverage file not found: $CoverageFile"
    exit 1
}

Write-Host "Analyzing coverage file: $CoverageFile" -ForegroundColor Cyan
Write-Host ""

# Parse coverage file
[xml]$coverage = Get-Content $CoverageFile

# Extract all file paths
$classNodes = $coverage.coverage.packages.package.classes.class
if ($null -eq $classNodes) {
    Write-Warning "No classes found in coverage file."
    exit 0
}

# Get unique filenames
$allFiles = @($classNodes | Select-Object -ExpandProperty filename -Unique | Sort-Object)
$fileCount = $allFiles.Count

if ($fileCount -eq 0) {
    Write-Warning "No files found in coverage report."
    exit 0
}

Write-Host "Total files in coverage report: $fileCount" -ForegroundColor Green
Write-Host ""

# Categorize files
$excludedFiles = [System.Collections.ArrayList]::new()
$includedFiles = [System.Collections.ArrayList]::new()

foreach ($file in $allFiles) {
    $matchedPattern = Test-ExclusionMatch -FilePath $file -Patterns $patterns
    if ($null -ne $matchedPattern) {
        $null = $excludedFiles.Add([PSCustomObject]@{
            Path = $file
            Pattern = $matchedPattern
        })
    } else {
        $null = $includedFiles.Add($file)
    }
}

Write-Host "Files that WILL BE EXCLUDED by Codecov ($($excludedFiles.Count)):" -ForegroundColor Yellow
if ($excludedFiles.Count -gt 0) {
    $excludedFiles | ForEach-Object {
        $relativePath = $_.Path -replace [Regex]::Escape($repoRoot), '.'
        Write-Host "  [$($_.Pattern)]" -ForegroundColor DarkGray -NoNewline
        Write-Host " $relativePath" -ForegroundColor Gray
    }
} else {
    Write-Host "  (none)" -ForegroundColor Gray
}
Write-Host ""

Write-Host "Files that WILL BE INCLUDED in coverage ($($includedFiles.Count)):" -ForegroundColor Green
if ($includedFiles.Count -gt 0) {
    $includedFiles | ForEach-Object {
        $relativePath = $_ -replace [Regex]::Escape($repoRoot), '.'
        Write-Host "  $relativePath" -ForegroundColor Gray
    }
} else {
    Write-Host "  (none)" -ForegroundColor Gray
}
Write-Host ""

# Summary
$excludedCount = $excludedFiles.Count
$includedCount = $includedFiles.Count

if ($fileCount -gt 0) {
    $excludedPercent = [math]::Round(($excludedCount / $fileCount) * 100, 1)
} else {
    $excludedPercent = 0
}

Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total files: $fileCount" -ForegroundColor White
Write-Host "  Excluded: $excludedCount ($excludedPercent%)" -ForegroundColor Yellow
Write-Host "  Included: $includedCount" -ForegroundColor Green
Write-Host ""

# Warnings
$objFileCount = @($allFiles | Where-Object { $_ -match '/obj/' }).Count
$generatedFileCount = @($allFiles | Where-Object { $_ -match '\.g\.cs$' }).Count
$excludedObjCount = @($excludedFiles | Where-Object { $_.Path -match '/obj/' }).Count
$excludedGenCount = @($excludedFiles | Where-Object { $_.Path -match '\.g\.cs$' }).Count

if ($objFileCount -gt 0 -and $excludedObjCount -eq 0) {
    Write-Warning "Found $objFileCount files in obj/ directories that are NOT excluded!"
}

if ($generatedFileCount -gt 0 -and $excludedGenCount -eq 0) {
    Write-Warning "Found $generatedFileCount .g.cs files that are NOT excluded!"
}

Write-Host "Validation complete." -ForegroundColor Cyan
