using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryPortalPaymentClaimRepository : IPortalPaymentClaimRepository
{
    private readonly ConcurrentDictionary<Guid, PortalPaymentClaim> _claims = new();

    public Task AddAsync(PortalPaymentClaim claim, CancellationToken cancellationToken = default)
    {
        _claims.TryAdd(claim.Id.Value, claim);
        return Task.CompletedTask;
    }

    public Task<PortalPaymentClaim?> GetByIdAsync(
        PortalPaymentClaimId id,
        CancellationToken cancellationToken = default)
    {
        _claims.TryGetValue(id.Value, out var claim);
        return Task.FromResult(claim);
    }

    public Task<IReadOnlyCollection<PortalPaymentClaim>> ListAsync(
        ClientId? clientId = null,
        CancellationToken cancellationToken = default)
    {
        var claims = _claims.Values.AsEnumerable();

        if (clientId.HasValue)
        {
            claims = claims.Where(claim => claim.ClientId == clientId.Value);
        }

        return Task.FromResult<IReadOnlyCollection<PortalPaymentClaim>>(
            claims.OrderByDescending(claim => claim.SubmittedAtUtc)
                .ThenByDescending(claim => claim.Id.Value)
                .ToArray());
    }
}
