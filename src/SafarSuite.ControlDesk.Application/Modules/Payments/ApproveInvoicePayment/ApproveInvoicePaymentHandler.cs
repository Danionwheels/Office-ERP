using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApproveInvoicePayment;

public sealed class ApproveInvoicePaymentHandler
{
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly PaymentPostingService _postingService;
    private readonly PaymentCloudOutboxMessageFactory _outboxMessageFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ApproveInvoicePaymentValidator _validator;

    public ApproveInvoicePaymentHandler(
        IPaymentRepository payments,
        IInvoiceRepository invoices,
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        AccountingPeriodPostingGuard periodGuard,
        PaymentPostingService postingService,
        PaymentCloudOutboxMessageFactory outboxMessageFactory,
        IUnitOfWork unitOfWork,
        IClock clock,
        ApproveInvoicePaymentValidator validator)
    {
        _payments = payments;
        _invoices = invoices;
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
        _cloudOutboxMessages = cloudOutboxMessages;
        _periodGuard = periodGuard;
        _postingService = postingService;
        _outboxMessageFactory = outboxMessageFactory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ApproveInvoicePaymentResult>> HandleAsync(
        ApproveInvoicePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ApproveInvoicePaymentResult>.Failure(validationErrors);
        }

        try
        {
            var payment = await _payments.GetByIdAsync(PaymentId.Create(command.PaymentId), cancellationToken);

            if (payment is null)
            {
                return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.NotFound(
                    nameof(command.PaymentId),
                    "Payment was not found."));
            }

            if (payment.Status != PaymentStatus.PendingReview)
            {
                return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(command.PaymentId),
                    "Only pending review payments can be approved."));
            }

            var invoice = await _invoices.GetByIdAsync(payment.InvoiceId, cancellationToken);

            if (invoice is null)
            {
                return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.NotFound(
                    nameof(payment.InvoiceId),
                    "Invoice for this payment was not found."));
            }

            if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid))
            {
                return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(payment.InvoiceId),
                    "Only issued or partially paid invoices can receive payment approvals."));
            }

            if (!string.Equals(payment.Amount.CurrencyCode, invoice.CurrencyCode, StringComparison.Ordinal))
            {
                return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(payment.Amount.CurrencyCode),
                    "Payment currency must match the invoice currency."));
            }

            if (payment.Amount.Amount > invoice.BalanceDue.Amount)
            {
                return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(payment.Amount),
                    "Payment amount cannot exceed the invoice balance due."));
            }

            var cashOrBankAccountId = LedgerAccountId.Create(command.CashOrBankAccountId);
            var accountsReceivableAccountId = LedgerAccountId.Create(command.AccountsReceivableAccountId);

            var cashOrBankAccountCheck = await _postingService.ValidateAssetPostingAccountAsync(
                cashOrBankAccountId,
                nameof(command.CashOrBankAccountId),
                "Cash or bank ledger account",
                cancellationToken);

            if (cashOrBankAccountCheck.IsFailure)
            {
                return Result<ApproveInvoicePaymentResult>.Failure(cashOrBankAccountCheck.Errors);
            }

            var receivableAccountCheck = await _postingService.ValidateAssetPostingAccountAsync(
                accountsReceivableAccountId,
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account",
                cancellationToken);

            if (receivableAccountCheck.IsFailure)
            {
                return Result<ApproveInvoicePaymentResult>.Failure(receivableAccountCheck.Errors);
            }

            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.PostingDate,
                nameof(command.PostingDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<ApproveInvoicePaymentResult>.Failure(periodError);
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var journalEntry = _postingService.CreateReceiptJournalEntry(
                        invoice,
                        payment,
                        cashOrBankAccountId,
                        accountsReceivableAccountId,
                        command.PostingDate);

                    payment.Approve(command.DecisionNote);
                    invoice.ApplyPayment(payment.Amount);
                    journalEntry.Post(_clock.UtcNow);

                    await _journalEntries.AddAsync(journalEntry, token);
                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreatePaymentRecorded(payment, invoice, journalEntry),
                        token);

                    if (invoice.Status == InvoiceStatus.Paid)
                    {
                        await _cloudOutboxMessages.AddAsync(
                            _outboxMessageFactory.CreateClientPaidStatusChanged(payment, invoice, journalEntry, isPaid: true),
                            token);
                    }

                    return await ToResultAsync(payment, invoice, journalEntry, token);
                },
                cancellationToken);

            return Result<ApproveInvoicePaymentResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ApproveInvoicePaymentResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<ApproveInvoicePaymentResult> ToResultAsync(
        Payment payment,
        Invoice invoice,
        JournalEntry journalEntry,
        CancellationToken cancellationToken)
    {
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return new ApproveInvoicePaymentResult(
            payment.Id.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            payment.Status.ToString(),
            payment.Amount.Amount,
            invoice.BalanceDue.Amount,
            payment.Amount.CurrencyCode,
            journalEntry.Id.Value,
            journalEntry.Status.ToString(),
            journalEntry.EntryDate,
            journalEntry.TotalDebit.Amount,
            journalEntry.TotalCredit.Amount,
            journalEntry.Lines
                .Select(line => PaymentDocumentResultFactory.ToApproveInvoicePaymentJournalLineResult(
                    line,
                    ledgerAccountsById))
                .ToArray());
    }
}
