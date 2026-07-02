namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalInvitationDeliveryRecorder
{
    Task RecordAsync(
        ClientPortalInvitationDeliveryRecord delivery,
        CancellationToken cancellationToken = default);
}

public sealed record ClientPortalInvitationDeliveryRecord(
    Guid DeliveryId,
    Guid InvitationId,
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    string DeliveryReason,
    string InvitationUrl,
    string InvitationToken,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset ExpiresAtUtc);
