namespace SafarSuite.LocalServer.Application.Commands;

public static class LocalServerInstallationCommandTypes
{
    public const string RequestDiagnostics = "request_diagnostics";
    public const string RefreshEntitlement = "refresh_entitlement";
}

public static class LocalServerInstallationCommandAcknowledgementStatuses
{
    public const string Applied = "Applied";
    public const string Failed = "Failed";
    public const string Rejected = "Rejected";
}
