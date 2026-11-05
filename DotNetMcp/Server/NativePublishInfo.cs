namespace DotNetMcp;

internal sealed record NativePublishInfo(
    bool DynamicCodeSupported, string FrameworkDescription, string ProcessArchitecture);
