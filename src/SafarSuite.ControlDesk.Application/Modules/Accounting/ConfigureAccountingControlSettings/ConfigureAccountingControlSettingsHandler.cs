using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountingControlSettings;

public sealed class ConfigureAccountingControlSettingsHandler
{
    private readonly IAccountingControlSettingsRepository _settings;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly AccountingControlSettingsResultFactory _resultFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public ConfigureAccountingControlSettingsHandler(
        IAccountingControlSettingsRepository settings,
        ILedgerAccountRepository ledgerAccounts,
        AccountingControlSettingsResultFactory resultFactory,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _settings = settings;
        _ledgerAccounts = ledgerAccounts;
        _resultFactory = resultFactory;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<GetAccountingControlSettingsResult>> HandleAsync(
        ConfigureAccountingControlSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(companyError);
        }

        var validationError = ValidateBaseCurrency(command.BaseCurrencyCode);

        if (validationError is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(validationError);
        }

        var retainedEarningsAccount = await ResolveControlAccountAsync(
            command.RetainedEarningsAccountId,
            nameof(command.RetainedEarningsAccountId),
            "Retained earnings account",
            LedgerAccountType.Equity,
            cancellationToken);

        if (retainedEarningsAccount.Error is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(retainedEarningsAccount.Error);
        }

        var incomeSummaryAccount = await ResolveControlAccountAsync(
            command.IncomeSummaryAccountId,
            nameof(command.IncomeSummaryAccountId),
            "Income summary account",
            LedgerAccountType.Equity,
            cancellationToken);

        if (incomeSummaryAccount.Error is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(incomeSummaryAccount.Error);
        }

        var roundingAccount = await ResolveControlAccountAsync(
            command.RoundingAccountId,
            nameof(command.RoundingAccountId),
            "Rounding account",
            expectedType: null,
            cancellationToken);

        if (roundingAccount.Error is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(roundingAccount.Error);
        }

        try
        {
            var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);
            var existing = await _settings.GetByCompanyAsync(companyCode, cancellationToken);
            var now = _clock.UtcNow;

            if (existing is null)
            {
                existing = AccountingControlSettings.Create(
                    AccountingControlSettingsId.Create(_idGenerator.NewGuid()),
                    companyCode,
                    command.BaseCurrencyCode,
                    ToLedgerAccountId(command.RetainedEarningsAccountId),
                    ToLedgerAccountId(command.IncomeSummaryAccountId),
                    ToLedgerAccountId(command.RoundingAccountId),
                    now);

                await _settings.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.Configure(
                    command.BaseCurrencyCode,
                    ToLedgerAccountId(command.RetainedEarningsAccountId),
                    ToLedgerAccountId(command.IncomeSummaryAccountId),
                    ToLedgerAccountId(command.RoundingAccountId),
                    now);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<GetAccountingControlSettingsResult>.Success(
                await _resultFactory.CreateAsync(companyCode, existing, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private async Task<ControlAccountResolution> ResolveControlAccountAsync(
        Guid? accountId,
        string target,
        string label,
        LedgerAccountType? expectedType,
        CancellationToken cancellationToken)
    {
        if (!accountId.HasValue)
        {
            return new ControlAccountResolution(null, null);
        }

        if (accountId.Value == Guid.Empty)
        {
            return new ControlAccountResolution(null, ApplicationError.Validation(
                target,
                $"{label} id cannot be empty."));
        }

        var account = await _ledgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(accountId.Value),
            cancellationToken);

        if (account is null)
        {
            return new ControlAccountResolution(null, ApplicationError.NotFound(
                target,
                $"{label} was not found."));
        }

        if (!account.IsPostingAccount)
        {
            return new ControlAccountResolution(null, ApplicationError.Validation(
                target,
                $"{label} must be a posting account."));
        }

        if (account.Status != LedgerAccountStatus.Active)
        {
            return new ControlAccountResolution(null, ApplicationError.Validation(
                target,
                $"{label} must be active."));
        }

        if (expectedType.HasValue && account.Type != expectedType.Value)
        {
            return new ControlAccountResolution(null, ApplicationError.Validation(
                target,
                $"{label} must be a {expectedType.Value} account."));
        }

        return new ControlAccountResolution(account, null);
    }

    private static LedgerAccountId? ToLedgerAccountId(Guid? value)
    {
        return value.HasValue ? LedgerAccountId.Create(value.Value) : null;
    }

    private static ApplicationError? ValidateBaseCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ApplicationError.Validation(
                nameof(ConfigureAccountingControlSettingsCommand.BaseCurrencyCode),
                "Base currency code is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length == 3 && normalized.All(char.IsLetter)
            ? null
            : ApplicationError.Validation(
                nameof(ConfigureAccountingControlSettingsCommand.BaseCurrencyCode),
                "Base currency code must be a three-letter ISO code.");
    }

    private sealed record ControlAccountResolution(
        LedgerAccount? Account,
        ApplicationError? Error);
}
