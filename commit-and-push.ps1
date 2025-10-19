# Commit and push changes for MCP Server package rename

Write-Host "=== Committing changes ===" -ForegroundColor Cyan

# Stage all changes
git add -A

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

Write-Host ""
Write-Host "=== Creating version tag ===" -ForegroundColor Cyan
git tag v0.1.0-alpha.4

Write-Host ""
Write-Host "=== Pushing to GitHub ===" -ForegroundColor Cyan
git push origin main
git push origin v0.1.0-alpha.4

Write-Host ""
Write-Host "? Done! GitHub Actions will now build and publish the package." -ForegroundColor Green
Write-Host ""
Write-Host "??  IMPORTANT: Make sure to update your NuGet Trusted Publishing policy" -ForegroundColor Yellow
Write-Host "    to use the new package name: Community.Mcp.DotNet" -ForegroundColor Yellow
Write-Host ""
Write-Host "    Visit: https://www.nuget.org/account/apikeys" -ForegroundColor Gray
