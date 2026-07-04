using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IVoucherNumberingRuleRepository
{
    Task AddAsync(VoucherNumberingRule rule, CancellationToken cancellationToken = default);

    Task<VoucherNumberingRule?> GetByCompanyAndSourceTypeAsync(
        string companyCode,
        JournalSourceType sourceType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VoucherNumberingRule>> ListByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default);
}
