#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates the server.json file against the MCP schema.
.DESCRIPTION
    This script validates that the .mcp/server.json file is valid JSON and conforms
    to the Model Context Protocol server schema. Can also validate published NuGet packages.
.EXAMPLE
    .\validate-server-json.ps1
.EXAMPLE
    .\validate-server-json.ps1 -ValidatePublished -PackageVersion "0.1.0-rc1"
.NOTES
    Exit codes:
    0 - Validation successful
    1 - JSON parsing error
    2 - Schema validation error
    3 - File not found
    4 - Download error
#>

[CmdletBinding()]
param(
    [string]$ServerJsonPath = "DotNetMcp\.mcp\server.json",
    [string]$SchemaUrl = "https://static.modelcontextprotocol.io/schemas/2025-12-11/server.schema.json",
    [switch]$ValidatePublished,
    [string]$PackageId = "Community.Mcp.DotNet",
    [string]$PackageVersion
)

$ErrorActionPreference = "Stop"

function Test-JsonAgainstSchema {
    param(
        [string]$JsonPath,
        [string]$SchemaUrl
    )

    Write-Host "🔍 Validating server.json..." -ForegroundColor Cyan
    Write-Host "   File: $JsonPath" -ForegroundColor Gray
    Write-Host "   Schema: $SchemaUrl" -ForegroundColor Gray
    Write-Host ""

    # Check if file exists
    if (-not (Test-Path $JsonPath)) {
        Write-Host "❌ ERROR: File not found: $JsonPath" -ForegroundColor Red
        exit 3
    }

    # Validate JSON can be parsed
    Write-Host "📄 Step 1: Validating JSON syntax..." -ForegroundColor Yellow
    try {
        $json = Get-Content -Path $JsonPath -Raw | ConvertFrom-Json
        Write-Host "✅ JSON is valid and parseable" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ ERROR: Invalid JSON syntax" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }

    # Validate required properties exist
    Write-Host ""
    Write-Host "📋 Step 2: Validating required properties..." -ForegroundColor Yellow
    
    $requiredProperties = @("name", "description", "version")
    $missingProperties = @()
    
    foreach ($prop in $requiredProperties) {
        if (-not $json.PSObject.Properties.Name.Contains($prop)) {
            $missingProperties += $prop
        }
        else {
            Write-Host "   ✓ $prop : $($json.$prop)" -ForegroundColor Gray
        }
    }

    if ($missingProperties.Count -gt 0) {
        Write-Host "❌ ERROR: Missing required properties: $($missingProperties -join ', ')" -ForegroundColor Red
        exit 2
    }

    Write-Host "✅ All required properties present" -ForegroundColor Green

    # Validate schema reference
    Write-Host ""
    Write-Host "📚 Step 3: Validating schema reference..." -ForegroundColor Yellow
    
    if ($json.'$schema' -eq $SchemaUrl) {
        Write-Host "✅ Schema reference is correct" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  WARNING: Schema reference mismatch" -ForegroundColor Yellow
        Write-Host "   Expected: $SchemaUrl" -ForegroundColor Gray
        Write-Host "   Actual: $($json.'$schema')" -ForegroundColor Gray
    }

    # Validate name format
    Write-Host ""
    Write-Host "🏷️  Step 4: Validating name format..." -ForegroundColor Yellow
    
    if ($json.name -match '^[a-zA-Z0-9.-]+/[a-zA-Z0-9._-]+$') {
        Write-Host "✅ Name format is valid (reverse-DNS format)" -ForegroundColor Green
    }
    else {
        Write-Host "❌ ERROR: Name must be in reverse-DNS format (e.g., 'io.github.user/server')" -ForegroundColor Red
        exit 2
    }

    # Validate version format
    Write-Host ""
    Write-Host "🔢 Step 5: Validating version..." -ForegroundColor Yellow
    
    if ($json.version -match '^\d+\.\d+\.\d+') {
        Write-Host "✅ Version format is valid" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  WARNING: Version should follow semantic versioning (e.g., '1.0.2')" -ForegroundColor Yellow
    }

    # Validate packages
    Write-Host ""
    Write-Host "📦 Step 6: Validating packages..." -ForegroundColor Yellow
    
    if ($json.packages -and $json.packages.Count -gt 0) {
        foreach ($package in $json.packages) {
            if (-not $package.registryType) {
                Write-Host "❌ ERROR: Package missing 'registryType'" -ForegroundColor Red
                exit 2
            }
            if (-not $package.identifier) {
                Write-Host "❌ ERROR: Package missing 'identifier'" -ForegroundColor Red
                exit 2
            }
            if (-not $package.transport) {
                Write-Host "❌ ERROR: Package missing 'transport'" -ForegroundColor Red
                exit 2
            }
            Write-Host "   ✓ Package: $($package.identifier) ($($package.registryType))" -ForegroundColor Gray
        }
        Write-Host "✅ All packages valid" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  WARNING: No packages defined" -ForegroundColor Yellow
    }

    # Check for invalid properties
    Write-Host ""
    Write-Host "🔍 Step 7: Checking for invalid properties..." -ForegroundColor Yellow
    
    $validProperties = @(
        '$schema', '_meta', 'description', 'icons', 'name', 
        'packages', 'remotes', 'repository', 'title', 'version', 'websiteUrl'
    )
    
    $invalidProperties = @()
    foreach ($prop in $json.PSObject.Properties.Name) {
        if ($prop -notin $validProperties) {
            $invalidProperties += $prop
        }
    }

    if ($invalidProperties.Count -gt 0) {
        Write-Host "❌ ERROR: Invalid properties found: $($invalidProperties -join ', ')" -ForegroundColor Red
        Write-Host "   Note: 'tools' and 'resources' are dynamically exposed by the server, not declared in server.json" -ForegroundColor Yellow
        exit 2
    }

    Write-Host "✅ No invalid properties found" -ForegroundColor Green

    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "✅ Validation successful!" -ForegroundColor Green
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
}

# Download and validate published package if requested
if ($ValidatePublished) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "📦 VALIDATING PUBLISHED PACKAGE" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""

    try {
        $tempDir = New-Item -ItemType Directory -Path "$env:TEMP\dotnet-mcp-validation-$(Get-Date -Format 'yyyyMMddHHmmss')" -Force
        Push-Location $tempDir

        # Get version if not specified
        if (-not $PackageVersion) {
            Write-Host "🔍 Finding latest published version..." -ForegroundColor Yellow
            $indexUrl = "https://api.nuget.org/v3/index.json"
            $index = Invoke-RestMethod -Uri $indexUrl
            $searchQueryService = ($index.resources | Where-Object { $_.'@type' -eq 'SearchQueryService' })[0].'@id'
            $searchResult = Invoke-RestMethod -Uri "$searchQueryService`?q=packageid:$PackageId&prerelease=true"
            $PackageVersion = $searchResult.data[0].version
            Write-Host "   Found version: $PackageVersion" -ForegroundColor Gray
        }

        # Download package
        Write-Host ""
        Write-Host "📥 Downloading package..." -ForegroundColor Yellow
        $downloadUrl = "https://www.nuget.org/api/v2/package/$PackageId/$PackageVersion"
        Invoke-WebRequest -Uri $downloadUrl -OutFile "package.nupkg" -ErrorAction Stop
        Write-Host "   ✓ Downloaded $PackageId v$PackageVersion" -ForegroundColor Gray

        # Extract package
        Write-Host ""
        Write-Host "📂 Extracting package..." -ForegroundColor Yellow
        Expand-Archive -Path "package.nupkg" -DestinationPath "extracted" -Force
        $extractedServerJson = "extracted\.mcp\server.json"
        
        if (-not (Test-Path $extractedServerJson)) {
            Write-Host "❌ ERROR: server.json not found in package" -ForegroundColor Red
            Pop-Location
            exit 4
        }
        Write-Host "   ✓ Found server.json" -ForegroundColor Gray

        Write-Host ""
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

        # Validate the extracted server.json
        Test-JsonAgainstSchema -JsonPath $extractedServerJson -SchemaUrl $SchemaUrl

        Pop-Location
    }
    catch {
        Write-Host ""
        Write-Host "❌ ERROR downloading or validating published package" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        if (Test-Path variable:tempDir) {
            Pop-Location
        }
        exit 4
    }
}
else {
    # Run validation on local file
    Test-JsonAgainstSchema -JsonPath $ServerJsonPath -SchemaUrl $SchemaUrl
}
