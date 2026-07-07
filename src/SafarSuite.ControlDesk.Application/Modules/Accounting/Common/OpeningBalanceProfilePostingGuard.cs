using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed class OpeningBalanceProfilePostingGuard
{
    private readonly ILedgerAccountRepository _ledgerAccounts;

    public OpeningBalanceProfilePostingGuard(ILedgerAccountRepository ledgerAccounts)
    {
        _ledgerAccounts = ledgerAccounts;
    }

    public async Task<IReadOnlyCollection<string>> ValidateAsync(
        DateOnly entryDate,
        DateOnly fiscalYearFrom,
        DateOnly fiscalYearTo,
        string status,
        bool transactionsAllowed,
        Guid? profitAndLossCarryForwardAccountId,
        CancellationToken cancellationToken = default)
    {
        var blockers = new List<string>();

        if (fiscalYearFrom == default)
        {
            blockers.Add("Opening balance fiscal year start date is required.");
        }

        if (fiscalYearTo == default)
        {
            blockers.Add("Opening balance fiscal year end date is required.");
        }

        if (fiscalYearFrom != default && fiscalYearTo != default && fiscalYearFrom > fiscalYearTo)
        {
            blockers.Add("Opening balance fiscal year start date cannot be after the end date.");
        }

        if (entryDate != default
            && fiscalYearFrom != default
            && fiscalYearTo != default
            && (entryDate < fiscalYearFrom || entryDate > fiscalYearTo))
        {
            blockers.Add("Opening balance date must stay inside the selected fiscal year.");
        }

        if (!Enum.TryParse<OpeningBalanceProfileStatus>(status, true, out var parsedStatus))
        {
            blockers.Add("Opening balance profile status is invalid.");
        }
        else if (parsedStatus == OpeningBalanceProfileStatus.Closed)
        {
            blockers.Add("Opening balance profile is closed.");
        }

        if (!transactionsAllowed)
        {
            blockers.Add("Opening balance profile does not allow transactions.");
        }

        var carryForwardAccountBlocker = await ValidateCarryForwardAccountAsync(
            profitAndLossCarryForwardAccountId,
            cancellationToken);

        if (carryForwardAccountBlocker is not null)
        {
            blockers.Add(carryForwardAccountBlocker);
        }

        return blockers;
    }

    private async Task<string?> ValidateCarryForwardAccountAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (!accountId.HasValue)
        {
            return "Profit and loss carry-forward account is required.";
        }

        if (accountId.Value == Guid.Empty)
        {
            return "Profit and loss carry-forward account id cannot be empty.";
        }

        var account = await _ledgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(accountId.Value),
            cancellationToken);

        if (account is null)
        {
            return "Profit and loss carry-forward account was not found.";
        }

        if (!account.IsPostingAccount)
        {
            return "Profit and loss carry-forward account must be a posting account.";
        }

        if (account.Status != LedgerAccountStatus.Active)
        {
            return "Profit and loss carry-forward account must be active.";
        }

        return account.Type == LedgerAccountType.Equity
            ? null
            : "Profit and loss carry-forward account must be an Equity account.";
    }
}
