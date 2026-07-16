using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfPortalPaymentClaimRepository : IPortalPaymentClaimRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfPortalPaymentClaimRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PortalPaymentClaim claim, CancellationToken cancellationToken = default)
    {
        await _dbContext.PortalPaymentClaims.AddAsync(claim, cancellationToken);
    }

    public Task<PortalPaymentClaim?> GetByIdAsync(
        PortalPaymentClaimId id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PortalPaymentClaims
            .SingleOrDefaultAsync(claim => claim.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PortalPaymentClaim>> ListAsync(
        ClientId? clientId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PortalPaymentClaims.AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(claim => claim.ClientId == clientId.Value);
        }

        return await query
            .OrderByDescending(claim => claim.SubmittedAtUtc)
            .ThenByDescending(claim => claim.Id)
            .ToArrayAsync(cancellationToken);
    }
}
