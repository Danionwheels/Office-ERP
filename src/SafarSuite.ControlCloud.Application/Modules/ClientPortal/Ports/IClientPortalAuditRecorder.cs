namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalAuditRecorder
{
    Task RecordAsync(
        ClientPortalAuditRecord audit,
        CancellationToken cancellationToken = default);
}

public sealed record ClientPortalAuditRecord(
    Guid AuditEventId,
    Guid? ClientId,
    Guid? InvitationId,
    Guid? UserId,
    string SubjectEmail,
    string EventType,
    string Actor,
    string Detail,
    DateTimeOffset OccurredAtUtc);

public static class ClientPortalAuditEventTypes
{
    public const string InvitationCreated = "InvitationCreated";
    public const string InvitationDeliveryRecorded = "InvitationDeliveryRecorded";
    public const string InvitationResent = "InvitationResent";
    public const string InvitationRevoked = "InvitationRevoked";
    public const string InvitationAccepted = "InvitationAccepted";
    public const string SessionCreated = "SessionCreated";
    public const string SessionRejected = "SessionRejected";
    public const string SetupTokenCreated = "SetupTokenCreated";
    public const string BootstrapPackageGenerated = "BootstrapPackageGenerated";
    public const string LocalServerRegistrationAccepted = "LocalServerRegistrationAccepted";
    public const string LocalServerRegistrationRejected = "LocalServerRegistrationRejected";
    public const string LocalServerDiagnosticsUploaded = "LocalServerDiagnosticsUploaded";
    public const string OfflineRenewalFileGenerated = "OfflineRenewalFileGenerated";
}

public static class ClientPortalAuditActors
{
    public const string ClientPortal = "ClientPortal";
    public const string ControlCloud = "ControlCloud";
    public const string ControlDesk = "ControlDesk";
    public const string LocalServer = "LocalServer";
}
