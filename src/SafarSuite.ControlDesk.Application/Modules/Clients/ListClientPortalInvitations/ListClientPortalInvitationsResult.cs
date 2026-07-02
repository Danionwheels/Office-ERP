namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientPortalInvitations;

public sealed record ListClientPortalInvitationsResult(
    Guid ClientId,
    IReadOnlyCollection<ClientPortalInvitationResult> Invitations);
