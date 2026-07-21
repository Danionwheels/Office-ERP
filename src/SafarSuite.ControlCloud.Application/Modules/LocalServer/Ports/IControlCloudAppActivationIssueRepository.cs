using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudAppActivationIssueRepository
{
    Task AddAsync(
        ControlCloudAppActivationIssue issue,
        CancellationToken cancellationToken = default);

    Task<ControlCloudAppActivationIssue?> GetByIdAsync(
        Guid activationIssueId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ControlCloudAppActivationIssue>> ListAsync(
        Guid clientId,
        string? installationId,
        Guid? appServerInstallationId,
        string? query,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudAppActivationIssue issue,
        CancellationToken cancellationToken = default);
}
