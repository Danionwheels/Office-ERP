using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed class PaymentPostingService
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public PaymentPostingService(
        ILedgerAccountRepository ledgerAccounts,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _ledgerAccounts = ledgerAccounts;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<LedgerAccount>> ValidateAssetPostingAccountAsync(
        LedgerAccountId accountId,
        string target,
        string label,
        CancellationToken cancellationToken)
    {
        var account = await _ledgerAccounts.GetByIdAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Result<LedgerAccount>.Failure(ApplicationError.NotFound(
                target,
                $"{label} was not found."));
        }

        if (!account.IsPostingAccount)
        {
            return Result<LedgerAccount>.Failure(ApplicationError.Validation(
                target,
                $"{label} must be a posting account."));
        }

        if (account.Type != LedgerAccountType.Asset)
        {
            return Result<LedgerAccount>.Failure(ApplicationError.Validation(
                target,
                $"{label} must be an asset account."));
        }

        if (account.Status != LedgerAccountStatus.Active)
        {
            return Result<LedgerAccount>.Failure(ApplicationError.Validation(
                target,
                $"{label} must be active."));
        }

        return Result<LedgerAccount>.Success(account);
    }

    public JournalEntry CreateReceiptJournalEntry(
        Invoice invoice,
        Payment payment,
        LedgerAccountId cashOrBankAccountId,
        LedgerAccountId accountsReceivableAccountId,
        DateOnly postingDate)
    {
        var journalEntry = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            postingDate,
            payment.Amount.CurrencyCode,
            JournalSourceType.PaymentReceipt,
            payment.Reference.Value,
            $"Payment {payment.Reference.Value} for invoice {invoice.Number.Value}",
            _clock.UtcNow,
            payment.ClientId,
            payment.Id.Value);

        journalEntry.AddLine(JournalLine.DebitLine(
            cashOrBankAccountId,
            payment.Amount,
            $"Receipt for invoice {invoice.Number.Value}"));

        journalEntry.AddLine(JournalLine.CreditLine(
            accountsReceivableAccountId,
            payment.Amount,
            $"Accounts receivable settlement for invoice {invoice.Number.Value}"));

        return journalEntry;
    }

    public JournalEntry CreateReversalJournalEntry(
        Invoice invoice,
        Payment payment,
        JournalEntry originalReceiptJournal,
        DateOnly reversalDate)
    {
        var journalEntry = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            reversalDate,
            originalReceiptJournal.CurrencyCode,
            JournalSourceType.PaymentReversal,
            payment.Reference.Value,
            $"Reversal of payment {payment.Reference.Value} for invoice {invoice.Number.Value}",
            _clock.UtcNow,
            payment.ClientId,
            payment.Id.Value);

        foreach (var line in originalReceiptJournal.Lines)
        {
            var description = $"Reversal: {line.Description ?? originalReceiptJournal.Memo ?? payment.Reference.Value}";

            if (line.IsDebit)
            {
                journalEntry.AddLine(JournalLine.CreditLine(line.LedgerAccountId, line.Debit, description));
            }
            else if (line.IsCredit)
            {
                journalEntry.AddLine(JournalLine.DebitLine(line.LedgerAccountId, line.Credit, description));
            }
        }

        return journalEntry;
    }

    public JournalEntry CreateClientRefundJournalEntry(
        ClientRefund refund,
        LedgerAccountId cashOrBankAccountId,
        LedgerAccountId accountsReceivableAccountId,
        DateOnly postingDate)
    {
        var journalEntry = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            postingDate,
            refund.Amount.CurrencyCode,
            JournalSourceType.ClientRefund,
            refund.Reference.Value,
            $"Client refund {refund.Reference.Value}",
            _clock.UtcNow,
            refund.ClientId,
            refund.Id.Value);

        journalEntry.AddLine(JournalLine.DebitLine(
            accountsReceivableAccountId,
            refund.Amount,
            $"Accounts receivable restored for refund {refund.Reference.Value}"));

        journalEntry.AddLine(JournalLine.CreditLine(
            cashOrBankAccountId,
            refund.Amount,
            $"Cash or bank refund {refund.Reference.Value}"));

        return journalEntry;
    }
}
