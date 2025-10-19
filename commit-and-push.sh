#!/bin/bash
# Commit and push changes for MCP Server package rename

echo "=== Committing changes ==="

# Stage all changes
git add -A

# Commit with descriptive message
git commit -m "Rename package to Community.Mcp.DotNet and add MCP Server metadata

- Changed PackageId from dotnet.mcp to Community.Mcp.DotNet (avoids reserved prefix)
- Added PackageType=McpServer for MCP discoverability  
- Created .mcp/server.json with server metadata and tool descriptions
- Updated README with new package name in all examples
- Updated workflow to reference new package name
- Updated check-nuget-package.ps1 with new package name
- Added NuGet badge for Community.Mcp.DotNet"

echo ""
echo "=== Creating version tag ==="
git tag v0.1.0-alpha.4

echo ""
echo "=== Pushing to GitHub ==="
git push origin main
git push origin v0.1.0-alpha.4

echo ""
echo "? Done! GitHub Actions will now build and publish the package."
echo ""
echo "??  IMPORTANT: Make sure to update your NuGet Trusted Publishing policy"
echo "    to use the new package name: Community.Mcp.DotNet"
echo ""
echo "    Visit: https://www.nuget.org/account/apikeys"
