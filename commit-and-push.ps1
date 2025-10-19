# Commit and push changes for MCP Server package rename

Write-Host "=== Committing changes ===" -ForegroundColor Cyan

# Stage all changes
git add -A

# Check if there are changes to commit
$status = git status --porcelain
if ([string]::IsNullOrEmpty($status)) {
    Write-Host "No changes to commit" -ForegroundColor Yellow
} else {
    # Commit with descriptive message
    git commit -m @"
Rename package to Community.Mcp.DotNet and add MCP Server metadata

- Changed PackageId from dotnet.mcp to Community.Mcp.DotNet (avoids reserved prefix)
- Added PackageType=McpServer for MCP discoverability  
- Created .mcp/server.json with server metadata and tool descriptions
- Updated README with new package name in all examples
- Updated workflow to reference new package name
- Updated check-nuget-package.ps1 with new package name
- Added NuGet badge for Community.Mcp.DotNet
"@
}

Write-Host ""
Write-Host "=== Creating version tag ===" -ForegroundColor Cyan

# Create new tag
$tagName = "v0.1.0-alpha.5"
git tag $tagName
Write-Host "Created new tag: $tagName" -ForegroundColor Green

Write-Host ""
Write-Host "=== Pushing to GitHub ===" -ForegroundColor Cyan
git push origin main
git push origin $tagName

Write-Host ""
Write-Host "? Done! GitHub Actions will now build and publish the package." -ForegroundColor Green
Write-Host ""
Write-Host "??  IMPORTANT: Make sure to update your NuGet Trusted Publishing policy" -ForegroundColor Yellow
Write-Host "    to use the new package name: Community.Mcp.DotNet" -ForegroundColor Yellow
Write-Host ""
Write-Host "    Visit: https://www.nuget.org/account/apikeys" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Monitor the build at: https://github.com/jongalloway/dotnet-mcp/actions" -ForegroundColor Cyan
