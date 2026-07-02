using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudInstallationCommandRepository
{
    Task<ControlCloudInstallationCommand?> GetByCommandIdAsync(
        Guid commandId,
        CancellationToken cancellationToken = default);

    Task<ControlCloudInstallationCommand?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<long> GetLatestCommandVersionAsync(
        string installationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudInstallationCommand>> ListPendingAsync(
        string installationId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task<ControlCloudInstallationCommand?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlCloudInstallationCommand command,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudInstallationCommand command,
        CancellationToken cancellationToken = default);
}
