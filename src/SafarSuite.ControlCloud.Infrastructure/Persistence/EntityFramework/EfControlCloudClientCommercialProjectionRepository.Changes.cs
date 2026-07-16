using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfControlCloudClientCommercialProjectionRepository
{
    private async Task ApplyInvoiceIssuedAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudInvoiceIssuedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.Invoice,
            change.Invoice.InvoiceId,
            cancellationToken);
        var previous = Deserialize<ControlCloudInvoiceProjection>(entity);
        var invoice = change.Invoice;

        if (previous is not null)
        {
            invoice = invoice with
            {
                InvoiceStatus = previous.InvoiceStatus,
                BalanceDue = previous.BalanceDue,
                VoidedOn = previous.VoidedOn,
                VoidReason = previous.VoidReason
            };
        }

        var latestApplication = await GetLatestRelatedAsync<ControlCloudCreditApplicationProjection>(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.CreditApplication,
            invoice.InvoiceId,
            cancellationToken);

        if (latestApplication is not null)
        {
            invoice = invoice with
            {
                InvoiceStatus = latestApplication.InvoiceStatus,
                BalanceDue = latestApplication.InvoiceBalanceAfter
            };
        }
        else
        {
            var latestPayment = await GetLatestRelatedAsync<ControlCloudPaymentProjection>(
                change.ClientId,
                ControlCloudCommercialDocumentTypes.Payment,
                invoice.InvoiceId,
                cancellationToken);

            if (latestPayment is not null)
            {
                invoice = invoice with
                {
                    InvoiceStatus = latestPayment.InvoiceBalanceDue <= 0 ? "Paid" : "PartiallyPaid",
                    BalanceDue = latestPayment.InvoiceBalanceDue
                };
            }
        }

        ApplyInvoiceDelta(summary, previous, invoice);
        UpsertDocument(
            entity,
            change,
            ControlCloudCommercialDocumentTypes.Invoice,
            invoice.InvoiceId,
            null,
            invoice.InvoiceNumber,
            invoice.InvoiceStatus,
            invoice.IssueDate,
            invoice.TotalAmount,
            invoice.BalanceDue,
            invoice.CurrencyCode,
            invoice);
        TouchSummary(summary, change);
    }

    private async Task ApplyInvoiceStatusAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudInvoiceStatusChangedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.Invoice,
            change.InvoiceId,
            cancellationToken);
        var previous = Deserialize<ControlCloudInvoiceProjection>(entity);

        if (entity is not null && previous is not null)
        {
            var invoice = previous with
            {
                InvoiceStatus = change.InvoiceStatus,
                BalanceDue = change.BalanceDue
            };
            ApplyInvoiceDelta(summary, previous, invoice);
            UpsertDocument(
                entity,
                change,
                ControlCloudCommercialDocumentTypes.Invoice,
                invoice.InvoiceId,
                null,
                invoice.InvoiceNumber,
                invoice.InvoiceStatus,
                invoice.IssueDate,
                invoice.TotalAmount,
                invoice.BalanceDue,
                invoice.CurrencyCode,
                invoice);
        }

        TouchSummary(summary, change);
    }

    private async Task ApplyInvoiceVoidedAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudInvoiceVoidedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.Invoice,
            change.InvoiceId,
            cancellationToken);
        var previous = Deserialize<ControlCloudInvoiceProjection>(entity);

        if (entity is not null && previous is not null)
        {
            var invoice = previous with
            {
                InvoiceStatus = change.InvoiceStatus,
                BalanceDue = change.BalanceDue,
                VoidedOn = change.VoidedOn,
                VoidReason = change.VoidReason
            };
            ApplyInvoiceDelta(summary, previous, invoice);
            UpsertDocument(
                entity,
                change,
                ControlCloudCommercialDocumentTypes.Invoice,
                invoice.InvoiceId,
                null,
                invoice.InvoiceNumber,
                invoice.InvoiceStatus,
                invoice.IssueDate,
                invoice.TotalAmount,
                invoice.BalanceDue,
                invoice.CurrencyCode,
                invoice);
        }

        TouchSummary(summary, change);
    }

    private async Task ApplyPaymentRecordedAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudPaymentRecordedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.Payment,
            change.Payment.PaymentId,
            cancellationToken);
        var previous = Deserialize<ControlCloudPaymentProjection>(entity);

        summary.TotalPaid += PaymentContribution(change.Payment) - PaymentContribution(previous);
        UpsertDocument(
            entity,
            change,
            ControlCloudCommercialDocumentTypes.Payment,
            change.Payment.PaymentId,
            change.Payment.InvoiceId,
            change.Payment.PaymentReference,
            change.Payment.PaymentStatus,
            change.Payment.ReceivedOn,
            change.Payment.Amount,
            change.Payment.InvoiceBalanceDue,
            change.Payment.CurrencyCode,
            change.Payment);
        await UpdateInvoiceBalanceAsync(
            summary,
            change,
            change.Payment.InvoiceId,
            change.Payment.InvoiceBalanceDue <= 0 ? "Paid" : "PartiallyPaid",
            change.Payment.InvoiceBalanceDue,
            cancellationToken);
        TouchSummary(summary, change);
    }

    private async Task ApplyPaymentReversedAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudPaymentReversedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.Payment,
            change.PaymentId,
            cancellationToken);
        var previous = Deserialize<ControlCloudPaymentProjection>(entity);

        if (entity is not null && previous is not null)
        {
            var payment = previous with
            {
                PaymentStatus = change.PaymentStatus,
                InvoiceBalanceDue = change.InvoiceBalanceDue
            };
            summary.TotalPaid += PaymentContribution(payment) - PaymentContribution(previous);
            UpsertDocument(
                entity,
                change,
                ControlCloudCommercialDocumentTypes.Payment,
                payment.PaymentId,
                payment.InvoiceId,
                payment.PaymentReference,
                payment.PaymentStatus,
                payment.ReceivedOn,
                payment.Amount,
                payment.InvoiceBalanceDue,
                payment.CurrencyCode,
                payment);
        }

        await UpdateInvoiceBalanceAsync(
            summary,
            change,
            change.InvoiceId,
            change.InvoiceBalanceDue <= 0 ? "Paid" : "Issued",
            change.InvoiceBalanceDue,
            cancellationToken);
        TouchSummary(summary, change);
    }

    private async Task ApplyCreditNoteAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudCreditNoteIssuedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.CreditNote,
            change.CreditNote.CreditNoteId,
            cancellationToken);
        var previous = Deserialize<ControlCloudCreditNoteProjection>(entity);

        summary.TotalCredited += change.CreditNote.Amount - (previous?.Amount ?? 0);
        UpsertDocument(
            entity,
            change,
            ControlCloudCommercialDocumentTypes.CreditNote,
            change.CreditNote.CreditNoteId,
            change.CreditNote.InvoiceId,
            change.CreditNote.CreditNoteNumber,
            change.CreditNote.CreditNoteStatus,
            change.CreditNote.CreditDate,
            change.CreditNote.Amount,
            0,
            change.CreditNote.CurrencyCode,
            change.CreditNote);
        TouchSummary(summary, change);
    }

    private async Task ApplyRefundAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudRefundIssuedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.Refund,
            change.Refund.RefundId,
            cancellationToken);
        var previous = Deserialize<ControlCloudClientRefundProjection>(entity);

        summary.TotalRefunded += change.Refund.Amount - (previous?.Amount ?? 0);
        UpsertDocument(
            entity,
            change,
            ControlCloudCommercialDocumentTypes.Refund,
            change.Refund.RefundId,
            null,
            change.Refund.RefundReference,
            change.Refund.RefundStatus,
            change.Refund.RefundedOn,
            change.Refund.Amount,
            change.Refund.ClientBalanceAfter,
            change.Refund.CurrencyCode,
            change.Refund);
        TouchSummary(summary, change);
    }

    private async Task ApplyCreditApplicationAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudCreditAppliedChange change,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.CreditApplication,
            change.CreditApplication.CreditApplicationId,
            cancellationToken);
        var previous = Deserialize<ControlCloudCreditApplicationProjection>(entity);

        summary.TotalCreditApplied += change.CreditApplication.Amount - (previous?.Amount ?? 0);
        UpsertDocument(
            entity,
            change,
            ControlCloudCommercialDocumentTypes.CreditApplication,
            change.CreditApplication.CreditApplicationId,
            change.CreditApplication.InvoiceId,
            change.CreditApplication.Reference,
            change.CreditApplication.CreditApplicationStatus,
            change.CreditApplication.AppliedOn,
            change.CreditApplication.Amount,
            change.CreditApplication.InvoiceBalanceAfter,
            change.CreditApplication.CurrencyCode,
            change.CreditApplication);
        await UpdateInvoiceBalanceAsync(
            summary,
            change,
            change.CreditApplication.InvoiceId,
            change.CreditApplication.InvoiceStatus,
            change.CreditApplication.InvoiceBalanceAfter,
            cancellationToken);
        TouchSummary(summary, change);
    }

    private static void ApplyEntitlement(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudEntitlementIssuedChange change)
    {
        var current = string.IsNullOrWhiteSpace(summary.LatestEntitlementJson)
            ? null
            : JsonSerializer.Deserialize<ControlCloudEntitlementProjection>(
                summary.LatestEntitlementJson,
                JsonOptions);

        if (current is not null
            && change.Entitlement.EntitlementVersion <= current.EntitlementVersion)
        {
            return;
        }

        summary.LatestEntitlementJson = JsonSerializer.Serialize(change.Entitlement, JsonOptions);
        summary.LastUpdatedAtUtc = change.ProjectedAtUtc;
    }

    private async Task UpdateInvoiceBalanceAsync(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudCommercialProjectionChange change,
        Guid invoiceId,
        string status,
        decimal balanceDue,
        CancellationToken cancellationToken)
    {
        var entity = await GetDocumentAsync(
            change.ClientId,
            ControlCloudCommercialDocumentTypes.Invoice,
            invoiceId,
            cancellationToken);
        var previous = Deserialize<ControlCloudInvoiceProjection>(entity);

        if (entity is null || previous is null)
        {
            return;
        }

        var invoice = previous with
        {
            InvoiceStatus = status,
            BalanceDue = balanceDue
        };
        ApplyInvoiceDelta(summary, previous, invoice);
        UpsertDocument(
            entity,
            change,
            ControlCloudCommercialDocumentTypes.Invoice,
            invoice.InvoiceId,
            null,
            invoice.InvoiceNumber,
            invoice.InvoiceStatus,
            invoice.IssueDate,
            invoice.TotalAmount,
            invoice.BalanceDue,
            invoice.CurrencyCode,
            invoice);
    }

    private async Task<ControlCloudCommercialDocumentEntity?> GetDocumentAsync(
        Guid clientId,
        string documentType,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CommercialDocuments.SingleOrDefaultAsync(document =>
            document.ClientId == clientId
            && document.DocumentType == documentType
            && document.DocumentId == documentId,
            cancellationToken);
    }

    private async Task<T?> GetLatestRelatedAsync<T>(
        Guid clientId,
        string documentType,
        Guid relatedDocumentId,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.CommercialDocuments
            .Where(document =>
                document.ClientId == clientId
                && document.DocumentType == documentType
                && document.RelatedDocumentId == relatedDocumentId)
            .OrderByDescending(document => document.DocumentDate)
            .ThenByDescending(document => document.DocumentId)
            .FirstOrDefaultAsync(cancellationToken);

        return Deserialize<T>(entity);
    }

    private void UpsertDocument<T>(
        ControlCloudCommercialDocumentEntity? entity,
        ControlCloudCommercialProjectionChange change,
        string documentType,
        Guid documentId,
        Guid? relatedDocumentId,
        string reference,
        string status,
        DateOnly documentDate,
        decimal amount,
        decimal balanceAmount,
        string currencyCode,
        T detail)
    {
        if (entity is null)
        {
            entity = new ControlCloudCommercialDocumentEntity
            {
                ClientId = change.ClientId,
                DocumentType = documentType,
                DocumentId = documentId
            };
            _dbContext.CommercialDocuments.Add(entity);
        }

        entity.RelatedDocumentId = relatedDocumentId;
        entity.Reference = reference.Trim();
        entity.Status = status.Trim();
        entity.DocumentDate = documentDate;
        entity.Amount = amount;
        entity.BalanceAmount = balanceAmount;
        entity.CurrencyCode = NormalizeCurrency(currencyCode);
        entity.LastMessageId = change.MessageId;
        entity.OccurredAtUtc = change.OccurredAtUtc;
        entity.LastUpdatedAtUtc = change.ProjectedAtUtc;
        entity.DetailJson = JsonSerializer.Serialize(detail, JsonOptions);
    }

    private static T? Deserialize<T>(ControlCloudCommercialDocumentEntity? entity)
    {
        return entity is null
            ? default
            : JsonSerializer.Deserialize<T>(entity.DetailJson, JsonOptions);
    }

    private static void ApplyInvoiceDelta(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudInvoiceProjection? previous,
        ControlCloudInvoiceProjection? current)
    {
        summary.TotalInvoiced += InvoiceTotalContribution(current) - InvoiceTotalContribution(previous);
        summary.BalanceDue += InvoiceBalanceContribution(current) - InvoiceBalanceContribution(previous);
    }

    private static decimal InvoiceTotalContribution(ControlCloudInvoiceProjection? invoice)
    {
        return invoice is null || IsVoid(invoice.InvoiceStatus) ? 0 : invoice.TotalAmount;
    }

    private static decimal InvoiceBalanceContribution(ControlCloudInvoiceProjection? invoice)
    {
        return invoice is null || IsVoid(invoice.InvoiceStatus) ? 0 : invoice.BalanceDue;
    }

    private static decimal PaymentContribution(ControlCloudPaymentProjection? payment)
    {
        return payment is null
               || string.Equals(payment.PaymentStatus, "Reversed", StringComparison.OrdinalIgnoreCase)
            ? 0
            : payment.Amount;
    }

    private static bool IsVoid(string status)
    {
        return string.Equals(status, "Void", StringComparison.OrdinalIgnoreCase);
    }

    private static void TouchSummary(
        ControlCloudClientCommercialProjectionEntity summary,
        ControlCloudCommercialProjectionChange change)
    {
        summary.CurrencyCode = NormalizeCurrency(change.CurrencyCode);
        summary.AvailableCredit = Math.Max(
            summary.TotalCredited - summary.TotalRefunded - summary.TotalCreditApplied,
            0);
        summary.IsPaid = summary.BalanceDue <= 0;
        summary.LastUpdatedAtUtc = change.ProjectedAtUtc;
    }
}
