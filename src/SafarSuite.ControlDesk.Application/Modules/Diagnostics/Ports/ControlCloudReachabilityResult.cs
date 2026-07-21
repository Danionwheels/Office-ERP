namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

public sealed record ControlCloudReachabilityResult(
    ControlCloudReachabilityStatus Status,
    string Code,
    int? HttpStatusCode,
    long? LatencyMilliseconds)
{
    public bool IsReachable => Status == ControlCloudReachabilityStatus.Reachable;
}

public enum ControlCloudReachabilityStatus
{
    Reachable,
    Unreachable,
    NotConfigured,
    TimedOut
}

public static class ControlCloudReachabilityCodes
{
    public const string Reachable = "ControlCloudReachable";

    public const string Unreachable = "ControlCloudUnreachable";

    public const string NotConfigured = "ControlCloudNotConfigured";

    public const string TimedOut = "ControlCloudInspectionTimedOut";
}
