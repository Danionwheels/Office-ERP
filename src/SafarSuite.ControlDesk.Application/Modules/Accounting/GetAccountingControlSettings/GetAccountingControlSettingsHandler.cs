using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingControlSettings;

public sealed class GetAccountingControlSettingsHandler
{
    private readonly IAccountingControlSettingsRepository _settings;
    private readonly AccountingControlSettingsResultFactory _resultFactory;

    public GetAccountingControlSettingsHandler(
        IAccountingControlSettingsRepository settings,
        AccountingControlSettingsResultFactory resultFactory)
    {
        _settings = settings;
        _resultFactory = resultFactory;
    }

    public async Task<Result<GetAccountingControlSettingsResult>> HandleAsync(
        GetAccountingControlSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        var settings = await _settings.GetByCompanyAsync(companyCode, cancellationToken);

        return Result<GetAccountingControlSettingsResult>.Success(
            await _resultFactory.CreateAsync(companyCode, settings, cancellationToken));
    }
}
