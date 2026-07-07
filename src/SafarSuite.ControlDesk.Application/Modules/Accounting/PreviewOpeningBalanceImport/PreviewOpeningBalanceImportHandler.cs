using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;

public sealed class PreviewOpeningBalanceImportHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly OpeningBalanceProfilePostingGuard _profileGuard;
    private readonly JournalVoucherNumberService _voucherNumbers;

    public PreviewOpeningBalanceImportHandler(
        ILedgerAccountRepository ledgerAccounts,
        AccountingPeriodPostingGuard periodGuard,
        OpeningBalanceProfilePostingGuard profileGuard,
        JournalVoucherNumberService voucherNumbers)
    {
        _ledgerAccounts = ledgerAccounts;
        _periodGuard = periodGuard;
        _profileGuard = profileGuard;
        _voucherNumbers = voucherNumbers;
    }

    public async Task<Result<PreviewOpeningBalanceImportResult>> HandleAsync(
        PreviewOpeningBalanceImportCommand command,
        CancellationToken cancellationToken = default)
    {
        var blockers = new List<string>();

        if (command.EntryDate == default)
        {
            blockers.Add("Opening balance date is required.");
        }

        var currencyCode = string.IsNullOrWhiteSpace(command.CurrencyCode)
            ? string.Empty
            : command.CurrencyCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            blockers.Add("Opening balance currency code is required.");
        }

        if (command.EntryDate != default)
        {
            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.EntryDate,
                nameof(command.EntryDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                blockers.Add(periodError.Message);
            }
        }

        blockers.AddRange(await _profileGuard.ValidateAsync(
            command.EntryDate,
            command.ProfileFromDate,
            command.ProfileToDate,
            command.ProfileStatus,
            command.TransactionsAllowed,
            command.ProfitAndLossCarryForwardAccountId,
            cancellationToken));

        var sourceReference = await ResolveSourceReferenceAsync(command, cancellationToken);
        var lineResults = await ValidateLinesAsync(command.Lines ?? [], cancellationToken);
        var totalDebit = decimal.Round(lineResults.Sum(line => line.Debit), 2);
        var totalCredit = decimal.Round(lineResults.Sum(line => line.Credit), 2);
        var difference = decimal.Round(totalDebit - totalCredit, 2);
        var invalidLineCount = lineResults.Count(line => !line.IsValid);

        if (lineResults.Count == 0)
        {
            blockers.Add("Opening balance import lines are required.");
        }

        if (lineResults.Count(line => line.IsValid) < 2)
        {
            blockers.Add("Opening balance import must include at least two valid posting lines.");
        }

        if (totalDebit <= 0 || totalCredit <= 0)
        {
            blockers.Add("Opening balance import must include debit and credit amounts.");
        }

        if (difference != 0)
        {
            blockers.Add("Opening balance import debits and credits must balance.");
        }

        if (invalidLineCount > 0)
        {
            blockers.Add($"{invalidLineCount} opening balance line(s) have validation issues.");
        }

        return Result<PreviewOpeningBalanceImportResult>.Success(new PreviewOpeningBalanceImportResult(
            command.EntryDate,
            currencyCode,
            sourceReference,
            string.IsNullOrWhiteSpace(command.Memo) ? null : command.Memo.Trim(),
            blockers.Count == 0,
            totalDebit,
            totalCredit,
            difference,
            lineResults.Count,
            lineResults.Count(line => line.IsValid),
            invalidLineCount,
            blockers,
            lineResults));
    }

    private async Task<string> ResolveSourceReferenceAsync(
        PreviewOpeningBalanceImportCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.SourceReference))
        {
            return command.SourceReference.Trim();
        }

        if (command.EntryDate == default)
        {
            return string.Empty;
        }

        var preview = await _voucherNumbers.PreviewNextAsync(
            JournalSourceType.OpeningBalance,
            command.EntryDate,
            cancellationToken);

        return preview.Reference;
    }

    private async Task<IReadOnlyCollection<PreviewOpeningBalanceImportLineResult>> ValidateLinesAsync(
        IReadOnlyCollection<PreviewOpeningBalanceImportLineCommand> lines,
        CancellationToken cancellationToken)
    {
        var results = new List<PreviewOpeningBalanceImportLineResult>();
        var lineNumber = 0;

        foreach (var line in lines)
        {
            lineNumber++;
            var issues = new List<string>();
            LedgerAccount? ledgerAccount = null;
            var accountCode = string.IsNullOrWhiteSpace(line.AccountCode)
                ? string.Empty
                : line.AccountCode.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                issues.Add("Account code is required.");
            }
            else
            {
                try
                {
                    ledgerAccount = await _ledgerAccounts.GetByCodeAsync(
                        LedgerAccountCode.Create(accountCode),
                        cancellationToken);

                    if (ledgerAccount is null)
                    {
                        issues.Add($"Ledger account {accountCode} was not found.");
                    }
                    else
                    {
                        if (!ledgerAccount.IsPostingAccount)
                        {
                            issues.Add($"Ledger account {accountCode} must be a posting account.");
                        }

                        if (ledgerAccount.Status != LedgerAccountStatus.Active)
                        {
                            issues.Add($"Ledger account {accountCode} must be active.");
                        }
                    }
                }
                catch (ArgumentException exception)
                {
                    issues.Add(exception.Message);
                }
            }

            if (line.Debit < 0 || line.Credit < 0)
            {
                issues.Add("Debit and credit amounts cannot be negative.");
            }

            if (line.Debit > 0 && line.Credit > 0)
            {
                issues.Add("Line cannot include both debit and credit amounts.");
            }

            if (line.Debit == 0 && line.Credit == 0)
            {
                issues.Add("Line must include either a debit or credit amount.");
            }

            results.Add(new PreviewOpeningBalanceImportLineResult(
                lineNumber,
                accountCode,
                ledgerAccount?.Id.Value,
                ledgerAccount?.Name,
                ledgerAccount?.Type.ToString(),
                ledgerAccount?.NormalBalance.ToString(),
                decimal.Round(line.Debit, 2),
                decimal.Round(line.Credit, 2),
                string.IsNullOrWhiteSpace(line.Description) ? null : line.Description.Trim(),
                issues.Count == 0,
                issues));
        }

        return results;
    }
}
