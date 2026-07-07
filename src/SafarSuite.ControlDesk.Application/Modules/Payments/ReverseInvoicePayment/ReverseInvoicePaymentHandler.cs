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

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;

public sealed class ReverseInvoicePaymentHandler
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
    private readonly ReverseInvoicePaymentValidator _validator;

    public ReverseInvoicePaymentHandler(
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
        ReverseInvoicePaymentValidator validator)
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

    public async Task<Result<ReverseInvoicePaymentResult>> HandleAsync(
        ReverseInvoicePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ReverseInvoicePaymentResult>.Failure(validationErrors);
        }

        try
        {
            var payment = await _payments.GetByIdAsync(PaymentId.Create(command.PaymentId), cancellationToken);

            if (payment is null)
            {
                return Result<ReverseInvoicePaymentResult>.Failure(ApplicationError.NotFound(
                    nameof(command.PaymentId),
                    "Payment was not found."));
            }

            if (payment.Status != PaymentStatus.Approved)
            {
                return Result<ReverseInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(command.PaymentId),
                    "Only approved payments can be reversed."));
            }

            var invoice = await _invoices.GetByIdAsync(payment.InvoiceId, cancellationToken);

            if (invoice is null)
            {
                return Result<ReverseInvoicePaymentResult>.Failure(ApplicationError.NotFound(
                    nameof(payment.InvoiceId),
                    "Invoice for this payment was not found."));
            }

            var originalReceiptJournal = await FindOriginalReceiptJournalAsync(payment, cancellationToken);

            if (originalReceiptJournal is null)
            {
                return Result<ReverseInvoicePaymentResult>.Failure(ApplicationError.NotFound(
                    nameof(command.PaymentId),
                    "Original posted payment receipt journal was not found."));
            }

            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.ReversalDate,
                nameof(command.ReversalDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<ReverseInvoicePaymentResult>.Failure(periodError);
            }

            var wasPaid = invoice.Status == InvoiceStatus.Paid;

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var reversalJournalEntry = _postingService.CreateReversalJournalEntry(
                        invoice,
                        payment,
                        originalReceiptJournal,
                        command.ReversalDate);

                    payment.Reverse(command.DecisionNote);
                    invoice.RemovePayment(payment.Amount);
                    reversalJournalEntry.Post(_clock.UtcNow);

                    await _journalEntries.AddAsync(reversalJournalEntry, token);
                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreatePaymentReversed(payment, invoice, reversalJournalEntry, originalReceiptJournal),
                        token);

                    if (wasPaid && invoice.Status != InvoiceStatus.Paid)
                    {
                        await _cloudOutboxMessages.AddAsync(
                            _outboxMessageFactory.CreateClientPaidStatusChanged(payment, invoice, reversalJournalEntry, isPaid: false),
                            token);
                    }

                    return await ToResultAsync(payment, invoice, reversalJournalEntry, originalReceiptJournal, token);
                },
                cancellationToken);

            return Result<ReverseInvoicePaymentResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<ReverseInvoicePaymentResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ReverseInvoicePaymentResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<JournalEntry?> FindOriginalReceiptJournalAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        var receiptJournals = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.PaymentReceipt,
            cancellationToken: cancellationToken);

        return receiptJournals
            .Where(entry => entry.Status == JournalEntryStatus.Posted)
            .Where(entry => string.Equals(entry.SourceReference, payment.Reference.Value, StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.Equals(entry.CurrencyCode, payment.Amount.CurrencyCode, StringComparison.Ordinal))
            .Where(entry => entry.TotalDebit.Amount == payment.Amount.Amount)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .FirstOrDefault();
    }

    private async Task<ReverseInvoicePaymentResult> ToResultAsync(
        Payment payment,
        Invoice invoice,
        JournalEntry reversalJournalEntry,
        JournalEntry originalReceiptJournal,
        CancellationToken cancellationToken)
    {
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return PaymentDocumentResultFactory.ToReverseInvoicePaymentResult(
            payment,
            invoice,
            reversalJournalEntry,
            originalReceiptJournal,
            ledgerAccountsById);
    }
}
