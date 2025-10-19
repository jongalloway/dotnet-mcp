# Check if a package exists on NuGet.org
param(
    [string]$PackageId = "Community.Mcp.DotNet",
    [string]$Version = "0.1.0-alpha.3"
)

$packageIdLower = $PackageId.ToLower()
$versionLower = $Version.ToLower()

Write-Host "Checking package: $PackageId version $Version" -ForegroundColor Cyan
Write-Host ""

# Check via API
$apiUrl = "https://api.nuget.org/v3-flatcontainer/$packageIdLower/$versionLower/$packageIdLower.$versionLower.nupkg"
Write-Host "API URL: $apiUrl"

try {
    $response = Invoke-WebRequest -Uri $apiUrl -Method Head -ErrorAction Stop
    Write-Host "? Package EXISTS on NuGet.org!" -ForegroundColor Green
    Write-Host ""
    Write-Host "View at: https://www.nuget.org/packages/$PackageId/$Version" -ForegroundColor Yellow
    Write-Host "Or (lowercase): https://www.nuget.org/packages/$packageIdLower/$Version" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "If you can't see it on the website, it might be:" -ForegroundColor Yellow
    Write-Host "  1. Unlisted - check your account: https://www.nuget.org/account/Packages"
    Write-Host "  2. Still being validated (can take 5-10 minutes)"
    Write-Host "  3. Requires you to be logged in to see"
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "? Package NOT FOUND on NuGet.org" -ForegroundColor Red
        Write-Host "You can publish version $Version"
    } else {
        Write-Host "??  Error checking package: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Checking package metadata..."
$metadataUrl = "https://api.nuget.org/v3/registration5-semver1/$packageIdLower/index.json"
try {
    $metadata = Invoke-RestMethod -Uri $metadataUrl -ErrorAction Stop
    Write-Host "? Package registered on NuGet.org" -ForegroundColor Green
    Write-Host "Available versions:"
    $metadata.items | ForEach-Object {
        $_.items | ForEach-Object {
            Write-Host "  - $($_.catalogEntry.version)" -ForegroundColor Cyan
        }
    }
} catch {
    Write-Host "? Package not found in registration" -ForegroundColor Red
}
