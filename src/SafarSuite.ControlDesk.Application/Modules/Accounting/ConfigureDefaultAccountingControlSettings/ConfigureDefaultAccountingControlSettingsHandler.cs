using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureDefaultAccountingControlSettings;

public sealed class ConfigureDefaultAccountingControlSettingsHandler
{
    private const string DefaultBaseCurrencyCode = "PKR";
    private const string RetainedEarningsRole = "RetainedEarnings";
    private const string IncomeSummaryRole = "IncomeSummary";
    private const string RoundingAdjustmentRole = "RoundingAdjustment";

    private readonly IAccountingControlSettingsRepository _settings;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IAccountCodeRangeRepository _accountCodeRanges;
    private readonly AccountingSetupDefaults _defaults;
    private readonly SuggestLedgerAccountCodeHandler _suggestCodes;
    private readonly CreateLedgerAccountHandler _createLedgerAccount;
    private readonly ConfigureAccountingControlSettingsHandler _configureSettings;

    public ConfigureDefaultAccountingControlSettingsHandler(
        IAccountingControlSettingsRepository settings,
        ILedgerAccountRepository ledgerAccounts,
        IAccountCodeRangeRepository accountCodeRanges,
        AccountingSetupDefaults defaults,
        SuggestLedgerAccountCodeHandler suggestCodes,
        CreateLedgerAccountHandler createLedgerAccount,
        ConfigureAccountingControlSettingsHandler configureSettings)
    {
        _settings = settings;
        _ledgerAccounts = ledgerAccounts;
        _accountCodeRanges = accountCodeRanges;
        _defaults = defaults;
        _suggestCodes = suggestCodes;
        _createLedgerAccount = createLedgerAccount;
        _configureSettings = configureSettings;
    }

    public async Task<Result<GetAccountingControlSettingsResult>> HandleAsync(
        ConfigureDefaultAccountingControlSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var retainedEarningsAccount = await EnsureControlAccountAsync(
            companyCode,
            RetainedEarningsRole,
            "Retained earnings",
            cancellationToken);

        if (retainedEarningsAccount.Errors is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(retainedEarningsAccount.Errors);
        }

        var incomeSummaryAccount = await EnsureControlAccountAsync(
            companyCode,
            IncomeSummaryRole,
            "Income summary",
            cancellationToken);

        if (incomeSummaryAccount.Errors is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(incomeSummaryAccount.Errors);
        }

        var roundingAccount = await EnsureControlAccountAsync(
            companyCode,
            RoundingAdjustmentRole,
            "Rounding adjustment",
            cancellationToken);

        if (roundingAccount.Errors is not null)
        {
            return Result<GetAccountingControlSettingsResult>.Failure(roundingAccount.Errors);
        }

        var existing = await _settings.GetByCompanyAsync(companyCode, cancellationToken);
        var baseCurrencyCode = existing?.BaseCurrencyCode ?? DefaultBaseCurrencyCode;

        return await _configureSettings.HandleAsync(
            new ConfigureAccountingControlSettingsCommand(
                companyCode,
                baseCurrencyCode,
                retainedEarningsAccount.Account!.Id.Value,
                incomeSummaryAccount.Account!.Id.Value,
                roundingAccount.Account!.Id.Value),
            cancellationToken);
    }

    private async Task<ControlAccountResolution> EnsureControlAccountAsync(
        string companyCode,
        string role,
        string accountName,
        CancellationToken cancellationToken)
    {
        var existing = await FindReusableControlAccountAsync(
            companyCode,
            role,
            cancellationToken);

        if (existing is not null)
        {
            return new ControlAccountResolution(existing, null);
        }

        var suggestion = await _suggestCodes.HandleAsync(
            new SuggestLedgerAccountCodeQuery(role, companyCode),
            cancellationToken);

        if (suggestion.IsFailure)
        {
            return new ControlAccountResolution(null, suggestion.Errors);
        }

        var created = await _createLedgerAccount.HandleAsync(
            new CreateLedgerAccountCommand(
                suggestion.Value.SuggestedCode,
                accountName,
                suggestion.Value.Type,
                suggestion.Value.NormalBalance,
                null,
                suggestion.Value.IsPostingAccount),
            cancellationToken);

        if (created.IsFailure)
        {
            return new ControlAccountResolution(null, created.Errors);
        }

        var account = await _ledgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(created.Value.LedgerAccountId),
            cancellationToken);

        return account is null
            ? new ControlAccountResolution(
                null,
                [ApplicationError.NotFound(nameof(role), $"Default ledger account for {role} was not found after creation.")])
            : new ControlAccountResolution(account, null);
    }

    private async Task<LedgerAccount?> FindReusableControlAccountAsync(
        string companyCode,
        string role,
        CancellationToken cancellationToken)
    {
        var range = await _accountCodeRanges.GetByCompanyAndRoleAsync(
            companyCode,
            role,
            cancellationToken);

        if (range is null || !range.IsActive)
        {
            return null;
        }

        var accounts = (await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken))
            .ToArray();
        var accountsById = accounts.ToDictionary(account => account.Id.Value);

        return accounts
            .Where(account => IsInsideRange(account.Code.Value, range)
                && IsCompatibleWithRange(account, range, accountsById))
            .OrderBy(account => account.Code.Value, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool IsInsideRange(string code, AccountCodeRange range)
    {
        return code.All(char.IsDigit)
            && code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0;
    }

    private static bool IsCompatibleWithRange(
        LedgerAccount account,
        AccountCodeRange range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        if (account.Status != LedgerAccountStatus.Active
            || account.Type != range.AccountType
            || account.NormalBalance != range.NormalBalance
            || account.IsPostingAccount != range.IsPostingAccount)
        {
            return false;
        }

        var expectedLevel = DetermineExpectedLevel(range, account);

        if (account.Level != expectedLevel)
        {
            return false;
        }

        if (expectedLevel is LedgerAccountLevel.Header
            or LedgerAccountLevel.Total
            or LedgerAccountLevel.Master
            or LedgerAccountLevel.Control)
        {
            return !account.ParentAccountId.HasValue;
        }

        if (expectedLevel != LedgerAccountLevel.Subsidiary)
        {
            return true;
        }

        if (!account.ParentAccountId.HasValue
            || !accountsById.TryGetValue(account.ParentAccountId.Value.Value, out var parentAccount))
        {
            return false;
        }

        return parentAccount.Level == LedgerAccountLevel.Control
            && (string.IsNullOrWhiteSpace(range.ParentCode)
                || string.Equals(parentAccount.Code.Value, range.ParentCode, StringComparison.Ordinal));
    }

    private static LedgerAccountLevel DetermineExpectedLevel(
        AccountCodeRange range,
        LedgerAccount account)
    {
        if (HasRangeIntent(range, "Header"))
        {
            return LedgerAccountLevel.Header;
        }

        if (HasRangeIntent(range, "Total"))
        {
            return LedgerAccountLevel.Total;
        }

        if (HasRangeIntent(range, "Control"))
        {
            return LedgerAccountLevel.Control;
        }

        if (HasRangeIntent(range, "Master"))
        {
            return LedgerAccountLevel.Master;
        }

        if (!string.IsNullOrWhiteSpace(range.ParentCode))
        {
            return LedgerAccountLevel.Subsidiary;
        }

        return account.IsPostingAccount
            ? LedgerAccountLevel.Detail
            : LedgerAccountLevel.Master;
    }

    private static bool HasRangeIntent(AccountCodeRange range, string intent)
    {
        return range.Role.Contains(intent, StringComparison.OrdinalIgnoreCase)
            || range.DisplayName.Contains(intent, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ControlAccountResolution(
        LedgerAccount? Account,
        IReadOnlyCollection<ApplicationError>? Errors);
}
