# Advanced Topics

This document covers advanced configuration, optimization, and security topics for the .NET MCP Server.

## Performance Optimization

The server implements several performance optimizations to ensure responsive operation:

### Template Caching

- Template Engine data is cached for 5 minutes
- Reduces Template Engine initialization overhead from ~1500ms to ~510ms (67% improvement)
- Cache is automatically invalidated after the TTL expires
- Thread-safe implementation using `SemaphoreSlim` for async locking

### Output Limiting

- Command output is limited to 1 million characters to prevent memory issues
- Prevents out-of-memory errors with extremely verbose commands
- Truncated output includes a clear message indicating truncation occurred
- Both stdout and stderr are independently limited

### Async Operations

- All I/O operations use async/await patterns
- Non-blocking command execution
- Efficient resource utilization
- Proper cancellation token support

## Logging

The server implements MCP-compliant logging that integrates with the MCP protocol:

### Log Destinations

- Logs are sent to the MCP client via the MCP logging protocol
- Logs appear in the client's logging interface (e.g., VS Code Output panel, Claude Desktop logs)
- stderr is reserved for MCP protocol messages, not application logs

### Log Levels

The server supports standard .NET logging levels:

- **Debug** - Detailed diagnostic information (cache hits/misses, command execution details)
- **Information** - General informational messages (cache operations, successful command execution)
- **Warning** - Warning messages (output truncation, deprecated features)
- **Error** - Error messages (command failures, exceptions)

### Configuration

- Log level is configurable by the MCP client
- Default log level is Information
- Debug logging can be enabled for troubleshooting

### What Gets Logged

- **Command Execution**: All `dotnet` commands executed with their arguments
- **Cache Operations**: Cache hits, misses, and clearing operations
- **Output Truncation**: Warnings when command output is truncated
- **Errors**: Command failures with exit codes and error messages

## Security

The MCP server follows security best practices to ensure safe operation:

### Local Execution Only

- All operations run on the user's local machine
- No network communication except for NuGet package searches (via official NuGet.org API)
- No telemetry or data collection

### No External Communication

- Server does not send data to external servers (except NuGet.org for package search)
- All .NET SDK operations are local
- Template Engine and MSBuild APIs access local data only

### Standard Permissions

- Server runs with the user's permissions
- Uses standard .NET SDK security model
- No elevation or special privileges required
- Access to files and directories is limited by user's OS permissions

### Input Validation

- All parameters are validated before execution
- Type checking ensures correct parameter types
- Required parameters are enforced
- Invalid values are rejected with clear error messages

### Path Sanitization

- File paths are validated to prevent directory traversal attacks
- Relative paths are resolved safely
- Malicious path patterns (e.g., `../../../etc/passwd`) are rejected
- All file operations use safe path manipulation APIs

### Command Execution Safety

- Commands are constructed safely using parameterized execution
- No shell injection vulnerabilities
- Arguments are properly escaped
- Process execution is isolated

### Output Sanitization

- Command output is limited to prevent memory exhaustion
- Binary data is not returned (only text output)
- Large outputs are truncated with clear indication

## Best Practices

### For Users

1. **Keep .NET SDK Updated** - Ensure you have the latest .NET SDK for best compatibility
2. **Review Commands** - Understand what commands the AI assistant is executing
3. **Use Version Control** - Always work in a version-controlled directory
4. **Test in Isolated Environments** - Test new commands in development environments first

### For Contributors

1. **Follow Async Patterns** - Always use async/await for I/O operations
2. **Add Logging** - Log command execution and important operations
3. **Validate Input** - Always validate parameters before execution
4. **Handle Errors** - Provide clear, structured error messages
5. **Document Tools** - Add clear descriptions to all MCP tools
6. **Test Thoroughly** - Test with various .NET SDK versions and configurations

## Troubleshooting Performance Issues

### Template Engine Slow

If template operations are slow:

- Check if caching is working (enable Debug logging)
- Ensure .NET SDK is properly installed
- Verify template package integrity: `dotnet new --debug:reinit`

### High Memory Usage

If the server uses excessive memory:

- Check for extremely verbose command output (triggers truncation)
- Review log level (Debug logging increases memory usage)
- Restart the MCP client to reset server state

### Command Execution Slow

If commands execute slowly:

- Verify .NET SDK performance: `dotnet --info`
- Check system resources (CPU, disk I/O)
- Review command complexity (large solutions, many projects)

## Architecture Details

For detailed information about the server's architecture and SDK integration, see:

- [SDK Integration Documentation](sdk-integration.md)
- [GitHub Copilot Instructions](.github/copilot-instructions.md) (for contributors)
