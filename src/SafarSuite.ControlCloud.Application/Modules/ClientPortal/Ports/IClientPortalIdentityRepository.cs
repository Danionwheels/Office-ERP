using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalIdentityRepository
{
    Task<ControlCloudClientPortalInvitation?> GetInvitationByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);

    Task<ControlCloudClientPortalInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudClientPortalInvitation>> ListInvitationsByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task AddInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default);

    Task SaveInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default);

    Task<ControlCloudClientPortalUser?> GetUserByClientAndEmailAsync(
        Guid clientId,
        string email,
        CancellationToken cancellationToken = default);

    Task AddUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default);

    Task SaveUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default);
}
