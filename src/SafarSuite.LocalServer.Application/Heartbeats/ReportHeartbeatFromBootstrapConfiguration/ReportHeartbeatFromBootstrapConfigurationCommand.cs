namespace SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;

public sealed record ReportHeartbeatFromBootstrapConfigurationCommand(
    DateOnly? AsOfDate = null,
    string? Detail = null);
