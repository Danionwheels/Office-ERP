using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientAccountingProfileRepository : IClientAccountingProfileRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientAccountingProfileRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClientAccountingProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientAccountingProfiles.AddAsync(profile, cancellationToken);
    }

    public async Task<ClientAccountingProfile?> GetByClientIdAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientAccountingProfiles
            .SingleOrDefaultAsync(profile => profile.ClientId == clientId, cancellationToken);
    }
}
