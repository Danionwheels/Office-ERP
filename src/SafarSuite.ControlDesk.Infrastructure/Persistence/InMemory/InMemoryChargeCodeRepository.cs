using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryChargeCodeRepository : IChargeCodeRepository
{
    private readonly ConcurrentDictionary<Guid, ChargeCode> _chargeCodesById = new();

    public Task AddAsync(ChargeCode chargeCode, CancellationToken cancellationToken = default)
    {
        _chargeCodesById.TryAdd(chargeCode.Id.Value, chargeCode);

        return Task.CompletedTask;
    }

    public Task<ChargeCode?> GetByIdAsync(ChargeCodeId id, CancellationToken cancellationToken = default)
    {
        _chargeCodesById.TryGetValue(id.Value, out var chargeCode);

        return Task.FromResult(chargeCode);
    }

    public Task<ChargeCode?> GetByCodeAsync(ChargeCodeKey code, CancellationToken cancellationToken = default)
    {
        var chargeCode = _chargeCodesById.Values.FirstOrDefault(chargeCode => chargeCode.Code.Equals(code));

        return Task.FromResult(chargeCode);
    }

    public Task<IReadOnlyCollection<ChargeCode>> ListAsync(CancellationToken cancellationToken = default)
    {
        var chargeCodes = _chargeCodesById.Values
            .OrderBy(chargeCode => chargeCode.Code.Value)
            .ThenBy(chargeCode => chargeCode.Name)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ChargeCode>>(chargeCodes);
    }

    public Task<bool> ExistsByCodeAsync(ChargeCodeKey code, CancellationToken cancellationToken = default)
    {
        var exists = _chargeCodesById.Values.Any(chargeCode => chargeCode.Code.Equals(code));

        return Task.FromResult(exists);
    }
}
