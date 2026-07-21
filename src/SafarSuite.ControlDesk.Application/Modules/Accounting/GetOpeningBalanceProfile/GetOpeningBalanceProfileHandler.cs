using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetOpeningBalanceProfile;

public sealed class GetOpeningBalanceProfileHandler
{
    private readonly IOpeningBalanceProfileRepository _profiles;
    private readonly OpeningBalanceProfileResultFactory _resultFactory;

    public GetOpeningBalanceProfileHandler(
        IOpeningBalanceProfileRepository profiles,
        OpeningBalanceProfileResultFactory resultFactory)
    {
        _profiles = profiles;
        _resultFactory = resultFactory;
    }

    public async Task<Result<GetOpeningBalanceProfileResult>> HandleAsync(
        GetOpeningBalanceProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetOpeningBalanceProfileResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        var profile = await _profiles.GetByCompanyAsync(companyCode, cancellationToken);

        return Result<GetOpeningBalanceProfileResult>.Success(
            await _resultFactory.CreateAsync(companyCode, profile, cancellationToken));
    }
}
