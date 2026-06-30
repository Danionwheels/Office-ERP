using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

public interface IChargeCodeRepository
{
    Task AddAsync(ChargeCode chargeCode, CancellationToken cancellationToken = default);

    Task<ChargeCode?> GetByIdAsync(ChargeCodeId id, CancellationToken cancellationToken = default);

    Task<ChargeCode?> GetByCodeAsync(ChargeCodeKey code, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChargeCode>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(ChargeCodeKey code, CancellationToken cancellationToken = default);
}
