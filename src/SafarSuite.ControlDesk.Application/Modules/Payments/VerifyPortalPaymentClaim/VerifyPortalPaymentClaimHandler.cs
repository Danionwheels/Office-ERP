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
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.VerifyPortalPaymentClaim;

public sealed class VerifyPortalPaymentClaimHandler
{
    private readonly IPortalPaymentClaimRepository _claims;
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
    private readonly IIdGenerator _idGenerator;

    public VerifyPortalPaymentClaimHandler(
        IPortalPaymentClaimRepository claims,
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
        IIdGenerator idGenerator)
    {
        _claims = claims;
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
        _idGenerator = idGenerator;
    }

    public async Task<Result<VerifyPortalPaymentClaimResult>> HandleAsync(
        VerifyPortalPaymentClaimCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(command);

        if (validationError is not null)
        {
            return Result<VerifyPortalPaymentClaimResult>.Failure(validationError);
        }

        try
        {
            var claimId = PortalPaymentClaimId.Create(command.ClaimId);
            var claim = await _claims.GetByIdAsync(claimId, cancellationToken);

            if (claim is null)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClaimId),
                    "Portal payment claim was not found."));
            }

            if (claim.Status != PortalPaymentClaimStatus.PendingVerification)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Conflict(
                    nameof(command.ClaimId),
                    "Only pending portal payment claims can be verified."));
            }

            var correlatedPayment = await _payments.GetByPortalClaimIdAsync(claimId, cancellationToken);

            if (correlatedPayment is not null)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Conflict(
                    nameof(command.ClaimId),
                    "This portal payment claim already has a recorded payment."));
            }

            var invoice = await _invoices.GetByIdAsync(claim.InvoiceId, cancellationToken);

            if (invoice is null)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.NotFound(
                    nameof(claim.InvoiceId),
                    "Invoice for this portal payment claim was not found."));
            }

            if (invoice.ClientId != claim.ClientId)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Conflict(
                    nameof(command.ClaimId),
                    "Portal payment claim client does not match the invoice client."));
            }

            if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid))
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Validation(
                    nameof(claim.InvoiceId),
                    "Only issued or partially paid invoices can receive a portal payment."));
            }

            if (!string.Equals(claim.Amount.CurrencyCode, invoice.CurrencyCode, StringComparison.Ordinal))
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Validation(
                    nameof(claim.Amount.CurrencyCode),
                    "Portal payment claim currency must match the invoice currency."));
            }

            if (claim.Amount.Amount > invoice.BalanceDue.Amount)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Validation(
                    nameof(claim.Amount),
                    "Portal payment claim amount cannot exceed the invoice balance due."));
            }

            var cashOrBankAccountId = LedgerAccountId.Create(command.CashOrBankAccountId);
            var accountsReceivableAccountId = LedgerAccountId.Create(command.AccountsReceivableAccountId);
            var cashAccountCheck = await _postingService.ValidateAssetPostingAccountAsync(
                cashOrBankAccountId,
                nameof(command.CashOrBankAccountId),
                "Cash or bank ledger account",
                cancellationToken);

            if (cashAccountCheck.IsFailure)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(cashAccountCheck.Errors);
            }

            var receivableAccountCheck = await _postingService.ValidateAssetPostingAccountAsync(
                accountsReceivableAccountId,
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account",
                cancellationToken);

            if (receivableAccountCheck.IsFailure)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(receivableAccountCheck.Errors);
            }

            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.PostingDate,
                nameof(command.PostingDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<VerifyPortalPaymentClaimResult>.Failure(periodError);
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var reviewedAtUtc = _clock.UtcNow;
                    var payment = Payment.Record(
                        PaymentId.Create(_idGenerator.NewGuid()),
                        claim.ClientId,
                        invoice.Id,
                        PaymentMethod.BankTransfer,
                        PaymentReference.Create(claim.TransferReferenceNumber),
                        Money.Of(claim.Amount.Amount, claim.Amount.CurrencyCode),
                        DateOnly.FromDateTime(claim.SubmittedAtUtc.UtcDateTime),
                        reviewedAtUtc,
                        claim.Id);
                    var journalEntry = _postingService.CreateReceiptJournalEntry(
                        invoice,
                        payment,
                        cashOrBankAccountId,
                        accountsReceivableAccountId,
                        command.PostingDate);

                    payment.Approve(command.DecisionNote);
                    invoice.ApplyPayment(payment.Amount);
                    journalEntry.Post(reviewedAtUtc);
                    claim.MarkVerified(payment.Id, reviewedAtUtc);

                    await _payments.AddAsync(payment, token);
                    await _journalEntries.AddAsync(journalEntry, token);
                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreatePaymentRecorded(payment, invoice, journalEntry),
                        token);

                    if (invoice.Status == InvoiceStatus.Paid)
                    {
                        await _cloudOutboxMessages.AddAsync(
                            _outboxMessageFactory.CreateClientPaidStatusChanged(
                                payment,
                                invoice,
                                journalEntry,
                                isPaid: true),
                            token);
                    }

                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreatePortalPaymentClaimDecided(claim, payment.Id, reason: null),
                        token);

                    var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
                        await _ledgerAccounts.ListAsync(cancellationToken: token));

                    return new VerifyPortalPaymentClaimResult(
                        PortalPaymentClaimResultFactory.From(claim),
                        PaymentDocumentResultFactory.ToRecordInvoicePaymentResult(
                            payment,
                            invoice,
                            journalEntry,
                            ledgerAccountsById));
                },
                cancellationToken);

            return Result<VerifyPortalPaymentClaimResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<VerifyPortalPaymentClaimResult>.Failure(ApplicationError.Conflict(
                nameof(command.ClaimId),
                exception.Message));
        }
    }

    private static ApplicationError? Validate(VerifyPortalPaymentClaimCommand command)
    {
        if (command.ClaimId == Guid.Empty)
        {
            return ApplicationError.Validation(nameof(command.ClaimId), "Claim id cannot be empty.");
        }

        if (command.CashOrBankAccountId == Guid.Empty)
        {
            return ApplicationError.Validation(
                nameof(command.CashOrBankAccountId),
                "Cash or bank account id cannot be empty.");
        }

        if (command.AccountsReceivableAccountId == Guid.Empty)
        {
            return ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable account id cannot be empty.");
        }

        return command.PostingDate == default
            ? ApplicationError.Validation(nameof(command.PostingDate), "Posting date is required.")
            : null;
    }
}
