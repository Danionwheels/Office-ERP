using System.Globalization;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;

public sealed class ControlDeskEnvelopeProjectionService
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;

    public ControlDeskEnvelopeProjectionService(
        IControlCloudClientCommercialProjectionRepository projections)
    {
        _projections = projections;
    }

    public async Task<bool> ProjectAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        return envelope.MessageType switch
        {
            "InvoiceIssued" => await ProjectInvoiceIssuedAsync(envelope, projectedAtUtc, cancellationToken),
            "InvoiceVoided" => await ProjectInvoiceVoidedAsync(envelope, projectedAtUtc, cancellationToken),
            "PaymentRecorded" => await ProjectPaymentRecordedAsync(envelope, projectedAtUtc, cancellationToken),
            "PaymentReversed" => await ProjectPaymentReversedAsync(envelope, projectedAtUtc, cancellationToken),
            "ClientPaidStatusChanged" => await ProjectClientPaidStatusChangedAsync(envelope, projectedAtUtc, cancellationToken),
            "CreditNoteIssued" => await ProjectCreditNoteIssuedAsync(envelope, projectedAtUtc, cancellationToken),
            "ClientRefundIssued" => await ProjectClientRefundIssuedAsync(envelope, projectedAtUtc, cancellationToken),
            "ClientCreditApplied" => await ProjectClientCreditAppliedAsync(envelope, projectedAtUtc, cancellationToken),
            "EntitlementSnapshotIssued" => await ProjectEntitlementSnapshotIssuedAsync(envelope, projectedAtUtc, cancellationToken),
            _ => false
        };
    }

    private async Task<bool> ProjectInvoiceIssuedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);
        var invoice = new ControlCloudInvoiceProjection(
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceNumber"),
            GetGuid(payload, "contractId"),
            GetString(payload, "invoiceStatus"),
            GetDateOnly(payload, "issueDate"),
            GetDateOnly(payload, "dueDate"),
            GetDecimal(payload, "totalAmount"),
            GetDecimal(payload, "balanceDue"),
            GetString(payload, "currencyCode"));

        projection.ApplyInvoiceIssued(invoice, projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectInvoiceVoidedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);

        projection.ApplyInvoiceVoided(
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceStatus"),
            GetDecimal(payload, "balanceDue"),
            GetString(payload, "currencyCode"),
            GetDateOnly(payload, "voidDate"),
            GetString(payload, "reason"),
            projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectPaymentRecordedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);
        var payment = new ControlCloudPaymentProjection(
            GetGuid(payload, "paymentId"),
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceNumber"),
            GetString(payload, "paymentStatus"),
            GetString(payload, "paymentMethod"),
            GetString(payload, "paymentReference"),
            GetDecimal(payload, "amount"),
            GetDecimal(payload, "invoiceBalanceDue"),
            GetString(payload, "currencyCode"),
            GetDateOnly(payload, "receivedOn"));

        projection.ApplyPaymentRecorded(payment, projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectPaymentReversedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);

        projection.ApplyPaymentReversed(
            GetGuid(payload, "paymentId"),
            GetGuid(payload, "invoiceId"),
            GetString(payload, "paymentStatus"),
            GetDecimal(payload, "amount"),
            GetDecimal(payload, "invoiceBalanceDue"),
            GetString(payload, "currencyCode"),
            projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectClientPaidStatusChangedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);

        projection.ApplyInvoiceStatus(
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceStatus"),
            GetDecimal(payload, "balanceDue"),
            GetString(payload, "currencyCode"),
            projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectCreditNoteIssuedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);
        var creditNote = new ControlCloudCreditNoteProjection(
            GetGuid(payload, "creditNoteId"),
            GetString(payload, "creditNoteNumber"),
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceNumber"),
            GetString(payload, "creditNoteStatus"),
            GetDateOnly(payload, "creditDate"),
            GetDecimal(payload, "amount"),
            GetString(payload, "currencyCode"),
            GetString(payload, "reason"));

        projection.ApplyCreditNote(creditNote, projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectClientRefundIssuedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);
        var refund = new ControlCloudClientRefundProjection(
            GetGuid(payload, "refundId"),
            GetString(payload, "refundStatus"),
            GetString(payload, "refundMethod"),
            GetString(payload, "refundReference"),
            GetDecimal(payload, "amount"),
            GetDecimal(payload, "clientBalanceBefore"),
            GetDecimal(payload, "clientBalanceAfter"),
            GetString(payload, "currencyCode"),
            GetDateOnly(payload, "refundedOn"));

        projection.ApplyRefund(refund, projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectClientCreditAppliedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, GetString(payload, "currencyCode"), cancellationToken);
        var application = new ControlCloudCreditApplicationProjection(
            GetGuid(payload, "creditApplicationId"),
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceNumber"),
            GetString(payload, "invoiceStatus"),
            GetString(payload, "creditApplicationStatus"),
            GetString(payload, "reference"),
            GetDecimal(payload, "amount"),
            GetDecimal(payload, "invoiceBalanceBefore"),
            GetDecimal(payload, "invoiceBalanceAfter"),
            GetDecimal(payload, "availableCreditBefore"),
            GetDecimal(payload, "availableCreditAfter"),
            GetDecimal(payload, "clientBalanceBefore"),
            GetDecimal(payload, "clientBalanceAfter"),
            GetString(payload, "currencyCode"),
            GetDateOnly(payload, "appliedOn"));

        projection.ApplyCreditApplication(application, projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<bool> ProjectEntitlementSnapshotIssuedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var projection = await LoadProjectionAsync(clientId, "PKR", cancellationToken);
        var modules = payload.GetProperty("modules")
            .EnumerateArray()
            .Select(module => new ControlCloudEntitlementModuleProjection(
                GetString(module, "moduleCode"),
                GetBoolean(module, "isEnabled")))
            .ToArray();
        var entitlement = new ControlCloudEntitlementProjection(
            GetGuid(payload, "entitlementSnapshotId"),
            GetGuid(payload, "contractId"),
            GetGuid(payload, "sourceInvoiceId"),
            GetString(payload, "sourceInvoiceNumber"),
            GetString(payload, "status"),
            GetDateOnly(payload, "paidUntil"),
            GetDateOnly(payload, "graceUntil"),
            GetDateOnly(payload, "offlineValidUntil"),
            GetInt32(payload, "allowedDevices"),
            GetInt32(payload, "allowedBranches"),
            GetDateTimeOffset(payload, "issuedAtUtc"),
            modules);

        projection.ApplyEntitlement(entitlement, projectedAtUtc);

        await _projections.SaveAsync(projection, cancellationToken);

        return true;
    }

    private async Task<ControlCloudClientCommercialProjection> LoadProjectionAsync(
        Guid clientId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        return await _projections.GetByClientIdAsync(clientId, cancellationToken)
            ?? ControlCloudClientCommercialProjection.Create(clientId, currencyCode);
    }

    private static Guid GetGuid(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetGuid();
    }

    private static string GetString(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetString()?.Trim() ?? "";
    }

    private static decimal GetDecimal(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetDecimal();
    }

    private static int GetInt32(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetInt32();
    }

    private static bool GetBoolean(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetBoolean();
    }

    private static DateOnly GetDateOnly(JsonElement payload, string propertyName)
    {
        return DateOnly.Parse(GetString(payload, propertyName), CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset GetDateTimeOffset(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetDateTimeOffset();
    }
}
