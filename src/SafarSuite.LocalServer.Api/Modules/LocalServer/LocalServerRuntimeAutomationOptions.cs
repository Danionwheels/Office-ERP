namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerRuntimeAutomationOptions
{
    public const string SectionName = "LocalServer:Runtime";

    public bool EnableBackgroundWorker { get; set; }

    public bool EnableEntitlementPull { get; set; } = true;

    public bool EnableHeartbeat { get; set; } = true;

    public bool EnableCommandPolling { get; set; } = true;

    public int EntitlementPullIntervalSeconds { get; set; } = 900;

    public int HeartbeatIntervalSeconds { get; set; } = 300;

    public int CommandPollIntervalSeconds { get; set; } = 120;

    public TimeSpan EntitlementPullInterval =>
        TimeSpan.FromSeconds(Math.Max(60, EntitlementPullIntervalSeconds));

    public TimeSpan HeartbeatInterval =>
        TimeSpan.FromSeconds(Math.Max(60, HeartbeatIntervalSeconds));

    public TimeSpan CommandPollInterval =>
        TimeSpan.FromSeconds(Math.Max(30, CommandPollIntervalSeconds));
}
