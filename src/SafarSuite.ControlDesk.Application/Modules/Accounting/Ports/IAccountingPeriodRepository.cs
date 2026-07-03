using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IAccountingPeriodRepository
{
    Task AddAsync(AccountingPeriod period, CancellationToken cancellationToken = default);

    Task<AccountingPeriod?> GetByIdAsync(AccountingPeriodId id, CancellationToken cancellationToken = default);

    Task<AccountingPeriod?> GetByCompanyAndStartDateAsync(
        string companyCode,
        DateOnly startsOn,
        CancellationToken cancellationToken = default);

    Task<AccountingPeriod?> GetContainingDateAsync(
        string companyCode,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountingPeriod>> ListByCompanyAsync(
        string companyCode,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}
