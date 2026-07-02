using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed class PaymentCloudOutboxMessageFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public PaymentCloudOutboxMessageFactory(IIdGenerator idGenerator, IClock clock)
    {
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public CloudOutboxMessage CreatePaymentRecorded(
        Payment payment,
        Invoice invoice,
        JournalEntry journalEntry)
    {
        var payload = new PaymentRecordedCloudPayload(
            "1",
            payment.Id.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.ClientId.Value,
            payment.Status.ToString(),
            payment.Method.ToString(),
            payment.Reference.Value,
            payment.Amount.Amount,
            invoice.BalanceDue.Amount,
            payment.Amount.CurrencyCode,
            payment.ReceivedOn,
            journalEntry.Id.Value,
            journalEntry.EntryDate,
            journalEntry.Status.ToString());

        return CreateMessage(
            "PaymentRecorded",
            "Payment",
            payment.Id.Value.ToString(),
            payload);
    }

    public CloudOutboxMessage CreateClientPaidStatusChanged(
        Payment payment,
        Invoice invoice,
        JournalEntry journalEntry,
        bool isPaid)
    {
        var payload = new ClientPaidStatusChangedCloudPayload(
            "1",
            invoice.ClientId.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            payment.Id.Value,
            invoice.Status.ToString(),
            isPaid,
            invoice.BalanceDue.Amount,
            invoice.CurrencyCode,
            journalEntry.Id.Value,
            journalEntry.EntryDate);

        return CreateMessage(
            "ClientPaidStatusChanged",
            "Client",
            invoice.ClientId.Value.ToString(),
            payload);
    }

    public CloudOutboxMessage CreatePaymentReversed(
        Payment payment,
        Invoice invoice,
        JournalEntry reversalJournalEntry,
        JournalEntry originalJournalEntry)
    {
        var payload = new PaymentReversedCloudPayload(
            "1",
            payment.Id.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.ClientId.Value,
            payment.Status.ToString(),
            payment.Method.ToString(),
            payment.Reference.Value,
            payment.Amount.Amount,
            invoice.BalanceDue.Amount,
            payment.Amount.CurrencyCode,
            reversalJournalEntry.Id.Value,
            reversalJournalEntry.EntryDate,
            reversalJournalEntry.Status.ToString(),
            originalJournalEntry.Id.Value);

        return CreateMessage(
            "PaymentReversed",
            "Payment",
            payment.Id.Value.ToString(),
            payload);
    }

    public CloudOutboxMessage CreateClientRefundIssued(
        ClientRefund refund,
        JournalEntry journalEntry,
        decimal clientBalanceBefore,
        decimal clientBalanceAfter)
    {
        var payload = new ClientRefundIssuedCloudPayload(
            "1",
            refund.Id.Value,
            refund.ClientId.Value,
            refund.Status.ToString(),
            refund.Method.ToString(),
            refund.Reference.Value,
            refund.Amount.Amount,
            clientBalanceBefore,
            clientBalanceAfter,
            refund.Amount.CurrencyCode,
            refund.RefundedOn,
            journalEntry.Id.Value,
            journalEntry.EntryDate,
            journalEntry.Status.ToString());

        return CreateMessage(
            "ClientRefundIssued",
            "ClientRefund",
            refund.Id.Value.ToString(),
            payload);
    }

    public CloudOutboxMessage CreateClientCreditApplied(
        ClientCreditApplication application,
        Invoice invoice,
        decimal invoiceBalanceBefore,
        decimal invoiceBalanceAfter,
        decimal availableCreditBefore,
        decimal availableCreditAfter,
        decimal clientBalanceBefore,
        decimal clientBalanceAfter)
    {
        var payload = new ClientCreditAppliedCloudPayload(
            "1",
            application.Id.Value,
            application.ClientId.Value,
            application.InvoiceId.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            application.Status.ToString(),
            application.Reference.Value,
            application.Amount.Amount,
            invoiceBalanceBefore,
            invoiceBalanceAfter,
            availableCreditBefore,
            availableCreditAfter,
            clientBalanceBefore,
            clientBalanceAfter,
            application.CurrencyCode,
            application.AppliedOn);

        return CreateMessage(
            "ClientCreditApplied",
            "ClientCreditApplication",
            application.Id.Value.ToString(),
            payload);
    }

    private CloudOutboxMessage CreateMessage<TPayload>(
        string messageType,
        string subjectType,
        string subjectId,
        TPayload payload)
    {
        return CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            messageType,
            subjectType,
            subjectId,
            JsonSerializer.Serialize(payload, JsonOptions),
            _clock.UtcNow);
    }

    private sealed record PaymentRecordedCloudPayload(
        string EventVersion,
        Guid PaymentId,
        Guid InvoiceId,
        string InvoiceNumber,
        Guid ClientId,
        string PaymentStatus,
        string PaymentMethod,
        string PaymentReference,
        decimal Amount,
        decimal InvoiceBalanceDue,
        string CurrencyCode,
        DateOnly ReceivedOn,
        Guid JournalEntryId,
        DateOnly PostingDate,
        string JournalEntryStatus);

    private sealed record ClientPaidStatusChangedCloudPayload(
        string EventVersion,
        Guid ClientId,
        Guid InvoiceId,
        string InvoiceNumber,
        Guid PaymentId,
        string InvoiceStatus,
        bool IsPaid,
        decimal BalanceDue,
        string CurrencyCode,
        Guid JournalEntryId,
        DateOnly PostingDate);

    private sealed record PaymentReversedCloudPayload(
        string EventVersion,
        Guid PaymentId,
        Guid InvoiceId,
        string InvoiceNumber,
        Guid ClientId,
        string PaymentStatus,
        string PaymentMethod,
        string PaymentReference,
        decimal Amount,
        decimal InvoiceBalanceDue,
        string CurrencyCode,
        Guid ReversalJournalEntryId,
        DateOnly ReversalDate,
        string ReversalJournalEntryStatus,
        Guid OriginalJournalEntryId);

    private sealed record ClientRefundIssuedCloudPayload(
        string EventVersion,
        Guid RefundId,
        Guid ClientId,
        string RefundStatus,
        string RefundMethod,
        string RefundReference,
        decimal Amount,
        decimal ClientBalanceBefore,
        decimal ClientBalanceAfter,
        string CurrencyCode,
        DateOnly RefundedOn,
        Guid JournalEntryId,
        DateOnly PostingDate,
        string JournalEntryStatus);

    private sealed record ClientCreditAppliedCloudPayload(
        string EventVersion,
        Guid CreditApplicationId,
        Guid ClientId,
        Guid InvoiceId,
        string InvoiceNumber,
        string InvoiceStatus,
        string CreditApplicationStatus,
        string Reference,
        decimal Amount,
        decimal InvoiceBalanceBefore,
        decimal InvoiceBalanceAfter,
        decimal AvailableCreditBefore,
        decimal AvailableCreditAfter,
        decimal ClientBalanceBefore,
        decimal ClientBalanceAfter,
        string CurrencyCode,
        DateOnly AppliedOn);
}
