using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalSessionRepository
{
    Task<ControlCloudClientPortalSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<ControlCloudClientPortalSession?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudClientPortalSession>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlCloudClientPortalSession session,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudClientPortalSession session,
        CancellationToken cancellationToken = default);
}
