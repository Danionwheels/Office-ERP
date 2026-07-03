using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IAccountCodeRangeRepository
{
    Task AddAsync(AccountCodeRange range, CancellationToken cancellationToken = default);

    Task<bool> AnyByCompanyAsync(string companyCode, CancellationToken cancellationToken = default);

    Task<AccountCodeRange?> GetByCompanyAndRoleAsync(
        string companyCode,
        string role,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountCodeRange>> ListByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default);
}
