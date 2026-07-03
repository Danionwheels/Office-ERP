using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
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

            var ledgerAccount = LedgerAccount.Create(
                LedgerAccountId.Create(_idGenerator.NewGuid()),
                code,
                command.Name,
                type,
                normalBalance,
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

            if (await _ledgerAccounts.GetByIdAsync(parentAccountId, cancellationToken) is null)
            {
                return new ParentAccountResolution(null, ApplicationError.NotFound(
                    nameof(CreateLedgerAccountCommand.ParentAccountId),
                    "Parent ledger account was not found."));
            }

            return new ParentAccountResolution(parentAccountId, null);
        }

        if (string.IsNullOrWhiteSpace(matchedRange?.ParentCode))
        {
            return new ParentAccountResolution(null, null);
        }

        var parentCode = LedgerAccountCode.Create(matchedRange.ParentCode);
        var parentAccount = await _ledgerAccounts.GetByCodeAsync(parentCode, cancellationToken);

        return new ParentAccountResolution(parentAccount?.Id, null);
    }

    private sealed record AccountingSetupRangeMatch(
        AccountCodeRange? Range,
        ApplicationError? Error);

    private sealed record ParentAccountResolution(
        LedgerAccountId? ParentAccountId,
        ApplicationError? Error);
}
