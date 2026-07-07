using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;

public sealed class CreateLedgerAccountHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IAccountCodeRangeRepository _accountCodeRanges;
    private readonly AccountingSetupDefaults _defaults;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateLedgerAccountValidator _validator;

    public CreateLedgerAccountHandler(
        ILedgerAccountRepository ledgerAccounts,
        IAccountCodeRangeRepository accountCodeRanges,
        AccountingSetupDefaults defaults,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateLedgerAccountValidator validator)
    {
        _ledgerAccounts = ledgerAccounts;
        _accountCodeRanges = accountCodeRanges;
        _defaults = defaults;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CreateLedgerAccountResult>> HandleAsync(
        CreateLedgerAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateLedgerAccountResult>.Failure(validationErrors);
        }

        try
        {
            if (!Enum.TryParse<LedgerAccountType>(command.Type, true, out var type))
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Validation(
                    nameof(command.Type),
                    "Ledger account type is invalid."));
            }

            if (!Enum.TryParse<NormalBalance>(command.NormalBalance, true, out var normalBalance))
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Validation(
                    nameof(command.NormalBalance),
                    "Normal balance is invalid."));
            }

            if (!TryParseLedgerAccountLevel(command.Level, out var requestedLevel))
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Validation(
                    nameof(command.Level),
                    "Ledger account level is invalid."));
            }

            var code = LedgerAccountCode.Create(command.Code);

            if (await _ledgerAccounts.ExistsByCodeAsync(code, cancellationToken))
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Conflict(
                    nameof(command.Code),
                    $"Ledger account {code.Value} already exists."));
            }

            var rangeMatch = await MatchAccountingSetupRangeAsync(
                code.Value,
                type,
                normalBalance,
                command.IsPostingAccount,
                cancellationToken);

            if (rangeMatch.Error is not null)
            {
                return Result<CreateLedgerAccountResult>.Failure(rangeMatch.Error);
            }

            var parentResolution = await ResolveParentAccountIdAsync(
                command.ParentAccountId,
                rangeMatch.Range,
                cancellationToken);

            if (parentResolution.Error is not null)
            {
                return Result<CreateLedgerAccountResult>.Failure(parentResolution.Error);
            }

            var levelResolution = ResolveLedgerAccountLevel(
                code.Value,
                rangeMatch.Range,
                parentResolution.ParentAccount,
                requestedLevel,
                command.IsPostingAccount);

            if (levelResolution.Error is not null)
            {
                return Result<CreateLedgerAccountResult>.Failure(levelResolution.Error);
            }

            var ledgerAccount = LedgerAccount.Create(
                LedgerAccountId.Create(_idGenerator.NewGuid()),
                code,
                command.Name,
                type,
                normalBalance,
                levelResolution.Level,
                parentResolution.ParentAccountId,
                command.IsPostingAccount,
                _clock.UtcNow);

            await _ledgerAccounts.AddAsync(ledgerAccount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CreateLedgerAccountResult>.Success(new CreateLedgerAccountResult(
                ledgerAccount.Id.Value,
                ledgerAccount.Code.Value,
                ledgerAccount.Name,
                ledgerAccount.Type.ToString(),
                ledgerAccount.NormalBalance.ToString(),
                ledgerAccount.Level.ToString(),
                ledgerAccount.ParentAccountId?.Value,
                ledgerAccount.IsPostingAccount,
                ledgerAccount.Status.ToString()));
        }
        catch (ArgumentException exception)
        {
            return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private async Task<AccountingSetupRangeMatch> MatchAccountingSetupRangeAsync(
        string code,
        LedgerAccountType type,
        NormalBalance normalBalance,
        bool isPostingAccount,
        CancellationToken cancellationToken)
    {
        if (!code.All(char.IsDigit))
        {
            return new AccountingSetupRangeMatch(null, ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.Code),
                "Ledger account code must be numeric and controlled by accounting setup ranges."));
        }

        var companyCode = AccountingSetupDefaults.DefaultCompanyCode;
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);
        var ranges = await _accountCodeRanges.ListByCompanyAsync(companyCode, cancellationToken);
        var matchingRange = ranges
            .Where(range => range.IsActive)
            .FirstOrDefault(range =>
                code.Length == range.CodeLength
                && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
                && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
                && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0);

        if (matchingRange is null)
        {
            return new AccountingSetupRangeMatch(null, ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.Code),
                "Ledger account code is outside the active accounting setup ranges."));
        }

        if (matchingRange.AccountType != type)
        {
            return new AccountingSetupRangeMatch(null, ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.Type),
                $"Ledger account type must be {matchingRange.AccountType} for range {matchingRange.DisplayName}."));
        }

        if (matchingRange.NormalBalance != normalBalance)
        {
            return new AccountingSetupRangeMatch(null, ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.NormalBalance),
                $"Normal balance must be {matchingRange.NormalBalance} for range {matchingRange.DisplayName}."));
        }

        if (matchingRange.IsPostingAccount != isPostingAccount)
        {
            return new AccountingSetupRangeMatch(null, ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.IsPostingAccount),
                $"Posting flag must be {matchingRange.IsPostingAccount} for range {matchingRange.DisplayName}."));
        }

        return new AccountingSetupRangeMatch(matchingRange, null);
    }

    private async Task<ParentAccountResolution> ResolveParentAccountIdAsync(
        Guid? requestedParentAccountId,
        AccountCodeRange? matchedRange,
        CancellationToken cancellationToken)
    {
        if (requestedParentAccountId.HasValue)
        {
            var parentAccountId = LedgerAccountId.Create(requestedParentAccountId.Value);
            var parentAccount = await _ledgerAccounts.GetByIdAsync(parentAccountId, cancellationToken);

            if (parentAccount is null)
            {
                return new ParentAccountResolution(null, null, ApplicationError.NotFound(
                    nameof(CreateLedgerAccountCommand.ParentAccountId),
                    "Parent ledger account was not found."));
            }

            var configuredParentCode = matchedRange?.ParentCode?.Trim() ?? "";

            if (configuredParentCode != ""
                && matchedRange is not null
                && !LedgerAccountHierarchyPolicy.IsParentInsideRangeFamily(
                    matchedRange,
                    parentAccount.Code.Value))
            {
                return new ParentAccountResolution(null, null, ApplicationError.Validation(
                    nameof(CreateLedgerAccountCommand.ParentAccountId),
                    $"Parent account must be {configuredParentCode} or one of its descendants for range {matchedRange.DisplayName}."));
            }

            return new ParentAccountResolution(parentAccountId, parentAccount, null);
        }

        if (string.IsNullOrWhiteSpace(matchedRange?.ParentCode))
        {
            return new ParentAccountResolution(null, null, null);
        }

        var parentCode = LedgerAccountCode.Create(matchedRange.ParentCode);
        var matchedParentAccount = await _ledgerAccounts.GetByCodeAsync(parentCode, cancellationToken);

        if (matchedParentAccount is null)
        {
            return new ParentAccountResolution(null, null, ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.ParentAccountId),
                $"Child range {matchedRange.DisplayName} requires parent account {matchedRange.ParentCode}."));
        }

        return new ParentAccountResolution(matchedParentAccount?.Id, matchedParentAccount, null);
    }

    private static LedgerAccountLevelResolution ResolveLedgerAccountLevel(
        string code,
        AccountCodeRange? matchedRange,
        LedgerAccount? parentAccount,
        LedgerAccountLevel? requestedLevel,
        bool isPostingAccount)
    {
        var level = requestedLevel
            ?? LedgerAccountHierarchyPolicy.DetermineExpectedLevel(
                matchedRange,
                parentAccount,
                isPostingAccount);
        var validationError = ValidateLedgerAccountLevel(
            code,
            level,
            matchedRange,
            parentAccount,
            isPostingAccount);

        return new LedgerAccountLevelResolution(level, validationError);
    }

    private static ApplicationError? ValidateLedgerAccountLevel(
        string code,
        LedgerAccountLevel level,
        AccountCodeRange? matchedRange,
        LedgerAccount? parentAccount,
        bool isPostingAccount)
    {
        if (LedgerAccountHierarchyPolicy.RequiresNonPostingAccount(level) && isPostingAccount)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.IsPostingAccount),
                $"{level} accounts cannot be posting accounts.");
        }

        if (LedgerAccountHierarchyPolicy.RequiresPostingAccount(level) && !isPostingAccount)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.IsPostingAccount),
                $"{level} accounts must be posting accounts.");
        }

        if (parentAccount is not null
            && matchedRange is not null
            && (parentAccount.Type != matchedRange.AccountType
                || parentAccount.NormalBalance != matchedRange.NormalBalance))
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.ParentAccountId),
                $"Parent account must use {matchedRange.AccountType} / {matchedRange.NormalBalance} for range {matchedRange.DisplayName}.");
        }

        if (parentAccount is not null && parentAccount.Status != LedgerAccountStatus.Active)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.ParentAccountId),
                "Parent account must be active before adding child accounts.");
        }

        var parentScopeError = ValidatePostingParentScope(
            code,
            level,
            matchedRange,
            parentAccount);

        if (parentScopeError is not null)
        {
            return parentScopeError;
        }

        if (matchedRange is not null
            && LedgerAccountHierarchyPolicy.HasRangeIntent(matchedRange, "Header")
            && level != LedgerAccountLevel.Header)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.Level),
                $"Ledger account level must be Header for range {matchedRange.DisplayName}.");
        }

        if (matchedRange is not null
            && LedgerAccountHierarchyPolicy.HasRangeIntent(matchedRange, "Total")
            && level != LedgerAccountLevel.Total)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.Level),
                $"Ledger account level must be Total for range {matchedRange.DisplayName}.");
        }

        if (matchedRange is not null
            && LedgerAccountHierarchyPolicy.HasRangeIntent(matchedRange, "Control")
            && level != LedgerAccountLevel.Control)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.Level),
                $"Ledger account level must be Control for range {matchedRange.DisplayName}.");
        }

        if (matchedRange is not null
            && !string.IsNullOrWhiteSpace(matchedRange.ParentCode)
            && level != LedgerAccountLevel.Subsidiary)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.Level),
                $"Ledger account level must be Subsidiary for child range {matchedRange.DisplayName}.");
        }

        if (LedgerAccountHierarchyPolicy.IsStructuralLevel(level))
        {
            return ValidateStructuralParent(level, parentAccount);
        }

        if (level == LedgerAccountLevel.Subsidiary && parentAccount is null)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.ParentAccountId),
                "Subsidiary accounts require an existing parent account.");
        }

        return null;
    }

    private static ApplicationError? ValidatePostingParentScope(
        string code,
        LedgerAccountLevel level,
        AccountCodeRange? matchedRange,
        LedgerAccount? parentAccount)
    {
        if (parentAccount is null
            || matchedRange is null
            || !LedgerAccountHierarchyPolicy.RequiresPostingAccount(level))
        {
            return null;
        }

        if (LedgerAccountHierarchyPolicy.IsChildCodeInsideParentScope(
            code,
            parentAccount.Code.Value,
            matchedRange))
        {
            return null;
        }

        return ApplicationError.Validation(
            nameof(CreateLedgerAccountCommand.ParentAccountId),
            $"Parent account {parentAccount.Code.Value} cannot own codes from range {matchedRange.DisplayName}.");
    }

    private static bool TryParseLedgerAccountLevel(
        string? value,
        out LedgerAccountLevel? level)
    {
        var normalizedValue = value?.Trim();
        level = null;

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return true;
        }

        level = normalizedValue.ToUpperInvariant() switch
        {
            "H" => LedgerAccountLevel.Header,
            "T" => LedgerAccountLevel.Total,
            "M" => LedgerAccountLevel.Master,
            "D" => LedgerAccountLevel.Detail,
            "C" => LedgerAccountLevel.Control,
            "S" => LedgerAccountLevel.Subsidiary,
            _ => null
        };

        if (level.HasValue)
        {
            return true;
        }

        if (!Enum.TryParse<LedgerAccountLevel>(normalizedValue, true, out var parsedLevel)
            || !Enum.IsDefined(parsedLevel))
        {
            return false;
        }

        level = parsedLevel;

        return true;
    }

    private static ApplicationError? ValidateStructuralParent(
        LedgerAccountLevel level,
        LedgerAccount? parentAccount)
    {
        if (parentAccount is null)
        {
            return null;
        }

        if (!LedgerAccountHierarchyPolicy.IsStructuralLevel(parentAccount.Level)
            || parentAccount.IsPostingAccount)
        {
            return ApplicationError.Validation(
                nameof(CreateLedgerAccountCommand.ParentAccountId),
                $"{level} account parent must be a non-posting structural account.");
        }

        return null;
    }

    private sealed record AccountingSetupRangeMatch(
        AccountCodeRange? Range,
        ApplicationError? Error);

    private sealed record LedgerAccountLevelResolution(
        LedgerAccountLevel Level,
        ApplicationError? Error);

    private sealed record ParentAccountResolution(
        LedgerAccountId? ParentAccountId,
        LedgerAccount? ParentAccount,
        ApplicationError? Error);
}
