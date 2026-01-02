#!/bin/bash
cd /home/runner/work/dotnet-mcp/dotnet-mcp
dotnet run --project DotNetMcp/DotNetMcp.csproj &
sleep 2
# Test a simple validation error
echo "Testing DotnetToolInstall with empty packageName..."
curl -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","id":1,"method":"DotnetToolInstall","params":{"packageName":"","machineReadable":true}}' http://localhost:5000 2>/dev/null || echo "Cannot connect to server"
