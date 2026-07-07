using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IOpeningBalanceProfileRepository
{
    Task AddAsync(OpeningBalanceProfile profile, CancellationToken cancellationToken = default);

    Task<OpeningBalanceProfile?> GetByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default);
}
