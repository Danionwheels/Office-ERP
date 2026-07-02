namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class LocalServerModuleGatewayFormat
{
    public const string Version = "safarsuite-local-module-gateway-v1";
}

public sealed record LocalServerModuleAccessRequest(
    string InstallationId,
    string ModuleCode,
    DateOnly? AsOfDate = null,
    string? RequestedBy = null);

public sealed record LocalServerModuleAccessResponse(
    string FormatVersion,
    string InstallationId,
    string ModuleCode,
    bool IsAllowed,
    string AccessState,
    string Reason,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly? GraceUntil,
    DateOnly? OfflineValidUntil,
    DateTimeOffset CheckedAtUtc);
