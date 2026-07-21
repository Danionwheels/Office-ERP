using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudFirstManagerSetupTokenIssueRepository
{
    Task AddAsync(
        ControlCloudFirstManagerSetupTokenIssue issue,
        CancellationToken cancellationToken = default);

    Task<ControlCloudFirstManagerSetupTokenIssue?> GetByTokenIdAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default);
}
