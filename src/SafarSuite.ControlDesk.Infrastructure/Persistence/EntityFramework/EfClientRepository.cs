using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientRepository : IClientRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _dbContext.Clients.AddAsync(client, cancellationToken);
    }

    public async Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clients
            .Include(client => client.Contacts)
            .Include(client => client.SupportNotes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(client => client.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(ClientCode code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clients.AnyAsync(client => client.Code == code, cancellationToken);
    }
}
