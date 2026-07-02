using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.Modules.Payments;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;

public sealed class RecordInvoicePaymentHandler
{
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentRepository _payments;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly PaymentPostingService _postingService;
    private readonly PaymentCloudOutboxMessageFactory _outboxMessageFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly RecordInvoicePaymentValidator _validator;

    public RecordInvoicePaymentHandler(
        IInvoiceRepository invoices,
        IPaymentRepository payments,
        IJournalEntryRepository journalEntries,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        PaymentPostingService postingService,
        PaymentCloudOutboxMessageFactory outboxMessageFactory,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        RecordInvoicePaymentValidator validator)
    {
        _invoices = invoices;
        _payments = payments;
        _journalEntries = journalEntries;
        _cloudOutboxMessages = cloudOutboxMessages;
        _postingService = postingService;
        _outboxMessageFactory = outboxMessageFactory;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<RecordInvoicePaymentResult>> HandleAsync(
        RecordInvoicePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<RecordInvoicePaymentResult>.Failure(validationErrors);
        }

        if (!Enum.TryParse<PaymentMethod>(command.Method, ignoreCase: true, out var method)
            || !Enum.IsDefined(method))
        {
            return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.Validation(
                nameof(command.Method),
                "Payment method is not valid."));
        }

        try
        {
            var invoiceId = InvoiceId.Create(command.InvoiceId);
            var invoice = await _invoices.GetByIdAsync(invoiceId, cancellationToken);

            if (invoice is null)
            {
                return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Invoice was not found."));
            }

            if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid))
            {
                return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(command.InvoiceId),
                    "Only issued or partially paid invoices can receive payments."));
            }

            var amount = Money.Of(command.Amount, command.CurrencyCode);

            if (!string.Equals(amount.CurrencyCode, invoice.CurrencyCode, StringComparison.Ordinal))
            {
                return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(command.CurrencyCode),
                    "Payment currency must match the invoice currency."));
            }

            if (amount.Amount > invoice.BalanceDue.Amount)
            {
                return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(command.Amount),
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
                return Result<RecordInvoicePaymentResult>.Failure(cashOrBankAccountCheck.Errors);
            }

            var receivableAccountCheck = await _postingService.ValidateAssetPostingAccountAsync(
                accountsReceivableAccountId,
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account",
                cancellationToken);

            if (receivableAccountCheck.IsFailure)
            {
                return Result<RecordInvoicePaymentResult>.Failure(receivableAccountCheck.Errors);
            }

            var payment = Payment.Record(
                PaymentId.Create(_idGenerator.NewGuid()),
                invoice.ClientId,
                invoice.Id,
                method,
                PaymentReference.Create(command.Reference),
                amount,
                command.ReceivedOn,
                _clock.UtcNow);

            if (payment.Status == PaymentStatus.PendingReview)
            {
                await _payments.AddAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<RecordInvoicePaymentResult>.Success(ToPendingReviewResult(payment, invoice));
            }

            if (payment.Status != PaymentStatus.Approved)
            {
                return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.Validation(
                    nameof(command.Method),
                    "Only approved or reviewable payments can be recorded."));
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

                    invoice.ApplyPayment(payment.Amount);
                    journalEntry.Post(_clock.UtcNow);

                    await _payments.AddAsync(payment, token);
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

                    return ToResult(payment, invoice, journalEntry);
                },
                cancellationToken);

            return Result<RecordInvoicePaymentResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<RecordInvoicePaymentResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private static RecordInvoicePaymentResult ToResult(
        Payment payment,
        Invoice invoice,
        JournalEntry journalEntry)
    {
        return new RecordInvoicePaymentResult(
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
            journalEntry.Lines.Select(line => new RecordInvoicePaymentJournalLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }

    private static RecordInvoicePaymentResult ToPendingReviewResult(Payment payment, Invoice invoice)
    {
        return new RecordInvoicePaymentResult(
            payment.Id.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            payment.Status.ToString(),
            payment.Amount.Amount,
            invoice.BalanceDue.Amount,
            payment.Amount.CurrencyCode,
            null,
            null,
            null,
            0m,
            0m,
            Array.Empty<RecordInvoicePaymentJournalLineResult>());
    }
}
