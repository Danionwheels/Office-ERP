using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IAccountingControlSettingsRepository
{
    Task AddAsync(AccountingControlSettings settings, CancellationToken cancellationToken = default);

    Task<AccountingControlSettings?> GetByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default);
}
