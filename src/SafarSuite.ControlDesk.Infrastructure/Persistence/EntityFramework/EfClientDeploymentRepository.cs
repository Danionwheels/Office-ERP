using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientDeploymentRepository : IClientDeploymentRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientDeploymentRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClientDeployment deployment, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientDeployments.AddAsync(deployment, cancellationToken);
    }

    public async Task<ClientDeployment?> GetByClientAndInstallationIdAsync(
        ClientId clientId,
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedInstallationId = installationId.Trim();

        return await _dbContext.ClientDeployments
            .SingleOrDefaultAsync(
                deployment => deployment.ClientId == clientId
                    && deployment.InstallationId == normalizedInstallationId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientDeployment>> ListByClientIdAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientDeployments
            .Where(deployment => deployment.ClientId == clientId)
            .OrderByDescending(deployment => deployment.IsPrimary)
            .ThenBy(deployment => deployment.DisplayName)
            .ThenBy(deployment => deployment.InstallationId)
            .ToArrayAsync(cancellationToken);
    }
}
