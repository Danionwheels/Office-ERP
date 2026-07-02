using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientCreditApplicationRepository : IClientCreditApplicationRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientCreditApplicationRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ClientCreditApplication application,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientCreditApplications.AddAsync(application, cancellationToken);
    }

    public async Task<ClientCreditApplication?> GetByIdAsync(
        ClientCreditApplicationId id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientCreditApplications
            .SingleOrDefaultAsync(application => application.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientCreditApplication>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var applications = _dbContext.ClientCreditApplications
            .Where(application => application.ClientId == clientId);

        if (fromDate.HasValue)
        {
            applications = applications.Where(application => application.AppliedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            applications = applications.Where(application => application.AppliedOn <= toDate.Value);
        }

        return await applications
            .OrderBy(application => application.AppliedOn)
            .ThenBy(application => application.CreatedAtUtc)
            .ThenBy(application => application.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsByReferenceAsync(
        ClientCreditApplicationReference reference,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientCreditApplications
            .AnyAsync(application => application.Reference == reference, cancellationToken);
    }
}
