using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetOpeningBalanceProfile;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureOpeningBalanceProfile;

public sealed class ConfigureOpeningBalanceProfileHandler
{
    private readonly IOpeningBalanceProfileRepository _profiles;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly OpeningBalanceProfileResultFactory _resultFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public ConfigureOpeningBalanceProfileHandler(
        IOpeningBalanceProfileRepository profiles,
        ILedgerAccountRepository ledgerAccounts,
        OpeningBalanceProfileResultFactory resultFactory,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _profiles = profiles;
        _ledgerAccounts = ledgerAccounts;
        _resultFactory = resultFactory;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<GetOpeningBalanceProfileResult>> HandleAsync(
        ConfigureOpeningBalanceProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetOpeningBalanceProfileResult>.Failure(companyError);
        }

        if (!Enum.TryParse<OpeningBalanceProfileStatus>(command.Status, true, out var status))
        {
            return Result<GetOpeningBalanceProfileResult>.Failure(ApplicationError.Validation(
                nameof(command.Status),
                "Opening balance profile status is invalid."));
        }

        var carryForwardAccountError = await ValidateCarryForwardAccountAsync(
            command.ProfitAndLossCarryForwardAccountId,
            nameof(command.ProfitAndLossCarryForwardAccountId),
            cancellationToken);

        if (carryForwardAccountError is not null)
        {
            return Result<GetOpeningBalanceProfileResult>.Failure(carryForwardAccountError);
        }

        try
        {
            var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);
            var existing = await _profiles.GetByCompanyAsync(companyCode, cancellationToken);
            var now = _clock.UtcNow;
            var carryForwardAccountId = ToLedgerAccountId(command.ProfitAndLossCarryForwardAccountId);

            if (existing is null)
            {
                existing = OpeningBalanceProfile.Create(
                    OpeningBalanceProfileId.Create(_idGenerator.NewGuid()),
                    companyCode,
                    command.FiscalYearFrom,
                    command.FiscalYearTo,
                    status,
                    command.TransactionsAllowed,
                    carryForwardAccountId,
                    now);

                await _profiles.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.Configure(
                    command.FiscalYearFrom,
                    command.FiscalYearTo,
                    status,
                    command.TransactionsAllowed,
                    carryForwardAccountId,
                    now);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<GetOpeningBalanceProfileResult>.Success(
                await _resultFactory.CreateAsync(companyCode, existing, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Result<GetOpeningBalanceProfileResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private async Task<ApplicationError?> ValidateCarryForwardAccountAsync(
        Guid? accountId,
        string target,
        CancellationToken cancellationToken)
    {
        if (!accountId.HasValue)
        {
            return ApplicationError.Validation(
                target,
                "Profit and loss carry-forward account is required.");
        }

        if (accountId.Value == Guid.Empty)
        {
            return ApplicationError.Validation(
                target,
                "Profit and loss carry-forward account id cannot be empty.");
        }

        var account = await _ledgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(accountId.Value),
            cancellationToken);

        if (account is null)
        {
            return ApplicationError.NotFound(
                target,
                "Profit and loss carry-forward account was not found.");
        }

        if (!account.IsPostingAccount)
        {
            return ApplicationError.Validation(
                target,
                "Profit and loss carry-forward account must be a posting account.");
        }

        if (account.Status != LedgerAccountStatus.Active)
        {
            return ApplicationError.Validation(
                target,
                "Profit and loss carry-forward account must be active.");
        }

        return account.Type == LedgerAccountType.Equity
            ? null
            : ApplicationError.Validation(
                target,
                "Profit and loss carry-forward account must be an Equity account.");
    }

    private static LedgerAccountId? ToLedgerAccountId(Guid? value)
    {
        return value.HasValue ? LedgerAccountId.Create(value.Value) : null;
    }
}
