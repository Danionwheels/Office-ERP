using System.Globalization;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;

public sealed class ControlDeskEnvelopeProjectionService
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IClientPortalPaymentClaimRepository _paymentClaims;
    private readonly IControlCloudProviderBankDetailsRepository _bankDetails;

    public ControlDeskEnvelopeProjectionService(
        IControlCloudClientCommercialProjectionRepository projections,
        IClientPortalPaymentClaimRepository paymentClaims,
        IControlCloudProviderBankDetailsRepository bankDetails)
    {
        _projections = projections;
        _paymentClaims = paymentClaims;
        _bankDetails = bankDetails;
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
            "ProviderBankDetailsUpdated" => await ProjectProviderBankDetailsUpdatedAsync(envelope, cancellationToken),
            "PortalPaymentClaimDecided" => await ProjectPortalPaymentClaimDecidedAsync(envelope, cancellationToken),
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
        var currencyCode = GetString(payload, "currencyCode");
        ControlCloudClientBillingDetailProjection? client = null;
        if (payload.TryGetProperty("client", out var clientProperty)
            && clientProperty.ValueKind == JsonValueKind.Object)
        {
            client = new ControlCloudClientBillingDetailProjection(
                GetString(clientProperty, "name"),
                GetOptionalString(clientProperty, "contactName"),
                GetOptionalString(clientProperty, "email"),
                GetOptionalString(clientProperty, "phone"));
        }

        var lines = payload.TryGetProperty("lines", out var linesProperty)
                    && linesProperty.ValueKind == JsonValueKind.Array
            ? linesProperty.EnumerateArray()
                .Select(line =>
                {
                    var lineTotal = GetOptionalDecimal(line, "lineTotal")
                        ?? GetOptionalDecimal(line, "amount")
                        ?? 0;
                    var quantity = GetOptionalDecimal(line, "quantity") ?? 1;
                    return new ControlCloudInvoiceLineProjection(
                        GetString(line, "description"),
                        quantity,
                        GetOptionalDecimal(line, "unitPrice") ?? lineTotal,
                        lineTotal,
                        GetOptionalString(line, "currencyCode") ?? currencyCode);
                })
                .ToArray()
            : [];
        var invoice = new ControlCloudInvoiceProjection(
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceNumber"),
            GetGuid(payload, "contractId"),
            GetString(payload, "invoiceStatus"),
            GetDateOnly(payload, "issueDate"),
            GetDateOnly(payload, "dueDate"),
            GetDecimal(payload, "totalAmount"),
            GetDecimal(payload, "balanceDue"),
            currencyCode,
            Client: client,
            Lines: lines);

        await _projections.ApplyChangeAsync(
            new ControlCloudInvoiceIssuedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                invoice),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectProviderBankDetailsUpdatedAsync(
        ControlCloudEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var updatedAtUtc = GetOptionalDateTimeOffset(payload, "updatedAtUtc") ?? envelope.OccurredAtUtc;
        var existing = await _bankDetails.GetAsync(cancellationToken);
        if (existing is not null && existing.UpdatedAtUtc > updatedAtUtc)
        {
            return true;
        }

        await _bankDetails.SaveAsync(
            new ControlCloudProviderBankDetails(
                GetOptionalString(payload, "bankName") ?? "",
                GetOptionalString(payload, "accountTitle") ?? "",
                GetOptionalString(payload, "accountNumber") ?? "",
                GetOptionalString(payload, "iban") ?? "",
                GetOptionalString(payload, "branchOrRoutingInfo") ?? "",
                updatedAtUtc),
            cancellationToken);
        return true;
    }

    private async Task<bool> ProjectPortalPaymentClaimDecidedAsync(
        ControlCloudEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var claimId = GetGuid(payload, "claimId");
        var clientId = GetGuid(payload, "clientId");
        var claim = await _paymentClaims.GetByIdAsync(claimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Portal payment claim '{claimId:D}' was not found.");
        if (claim.ClientId != clientId)
        {
            throw new InvalidOperationException("Portal payment claim decision client does not match the claim.");
        }

        var status = GetString(payload, "status");
        var reviewedAtUtc = GetOptionalDateTimeOffset(payload, "reviewedAtUtc") ?? envelope.OccurredAtUtc;
        if (status.Equals("verified", StringComparison.OrdinalIgnoreCase))
        {
            claim.MarkVerified(
                GetOptionalGuid(payload, "paymentId")
                    ?? throw new InvalidOperationException("Verified payment claim decision requires a payment id."),
                reviewedAtUtc);
        }
        else if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
        {
            var reason = GetOptionalString(payload, "rejectionReason")
                ?? GetOptionalString(payload, "reason")
                ?? "Rejected by provider.";
            if (claim.Status != ControlCloudClientPortalPaymentClaimStatus.Rejected)
            {
                claim.Reject(reason, reviewedAtUtc);
            }
        }
        else
        {
            throw new InvalidOperationException($"Unsupported portal payment claim decision status '{status}'.");
        }

        await _paymentClaims.SaveAsync(claim, cancellationToken);
        return true;
    }

    private async Task<bool> ProjectInvoiceVoidedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var currencyCode = GetString(payload, "currencyCode");

        await _projections.ApplyChangeAsync(
            new ControlCloudInvoiceVoidedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                GetGuid(payload, "invoiceId"),
                GetString(payload, "invoiceStatus"),
                GetDecimal(payload, "balanceDue"),
                GetDateOnly(payload, "voidDate"),
                GetString(payload, "reason")),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectPaymentRecordedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var currencyCode = GetString(payload, "currencyCode");
        var payment = new ControlCloudPaymentProjection(
            GetGuid(payload, "paymentId"),
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceNumber"),
            GetString(payload, "paymentStatus"),
            GetString(payload, "paymentMethod"),
            GetString(payload, "paymentReference"),
            GetDecimal(payload, "amount"),
            GetDecimal(payload, "invoiceBalanceDue"),
            currencyCode,
            GetDateOnly(payload, "receivedOn"));

        await _projections.ApplyChangeAsync(
            new ControlCloudPaymentRecordedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                payment),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectPaymentReversedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var currencyCode = GetString(payload, "currencyCode");

        await _projections.ApplyChangeAsync(
            new ControlCloudPaymentReversedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                GetGuid(payload, "paymentId"),
                GetGuid(payload, "invoiceId"),
                GetString(payload, "paymentStatus"),
                GetDecimal(payload, "amount"),
                GetDecimal(payload, "invoiceBalanceDue")),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectClientPaidStatusChangedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var currencyCode = GetString(payload, "currencyCode");

        await _projections.ApplyChangeAsync(
            new ControlCloudInvoiceStatusChangedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                GetGuid(payload, "invoiceId"),
                GetString(payload, "invoiceStatus"),
                GetDecimal(payload, "balanceDue")),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectCreditNoteIssuedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var currencyCode = GetString(payload, "currencyCode");
        var creditNote = new ControlCloudCreditNoteProjection(
            GetGuid(payload, "creditNoteId"),
            GetString(payload, "creditNoteNumber"),
            GetGuid(payload, "invoiceId"),
            GetString(payload, "invoiceNumber"),
            GetString(payload, "creditNoteStatus"),
            GetDateOnly(payload, "creditDate"),
            GetDecimal(payload, "amount"),
            currencyCode,
            GetString(payload, "reason"));

        await _projections.ApplyChangeAsync(
            new ControlCloudCreditNoteIssuedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                creditNote),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectClientRefundIssuedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var currencyCode = GetString(payload, "currencyCode");
        var refund = new ControlCloudClientRefundProjection(
            GetGuid(payload, "refundId"),
            GetString(payload, "refundStatus"),
            GetString(payload, "refundMethod"),
            GetString(payload, "refundReference"),
            GetDecimal(payload, "amount"),
            GetDecimal(payload, "clientBalanceBefore"),
            GetDecimal(payload, "clientBalanceAfter"),
            currencyCode,
            GetDateOnly(payload, "refundedOn"));

        await _projections.ApplyChangeAsync(
            new ControlCloudRefundIssuedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                refund),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectClientCreditAppliedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var currencyCode = GetString(payload, "currencyCode");
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
            currencyCode,
            GetDateOnly(payload, "appliedOn"));

        await _projections.ApplyChangeAsync(
            new ControlCloudCreditAppliedChange(
                clientId,
                currencyCode,
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                application),
            cancellationToken);

        return true;
    }

    private async Task<bool> ProjectEntitlementSnapshotIssuedAsync(
        ControlCloudEnvelope envelope,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload;
        var clientId = GetGuid(payload, "clientId");
        var modules = payload.GetProperty("modules")
            .EnumerateArray()
            .Select(module => new ControlCloudEntitlementModuleProjection(
                GetString(module, "moduleCode"),
                GetBoolean(module, "isEnabled")))
            .ToArray();
        var featureLimits = payload.TryGetProperty("featureLimits", out var featureLimitsProperty)
                            && featureLimitsProperty.ValueKind == JsonValueKind.Array
            ? featureLimitsProperty
                .EnumerateArray()
                .Select(limit => new ControlCloudEntitlementFeatureLimitProjection(
                    GetString(limit, "moduleCode"),
                    GetString(limit, "featureCode"),
                    GetInt64(limit, "limitValue"),
                    GetString(limit, "unit")))
                .ToArray()
            : [];
        var entitlementSnapshotId = GetGuid(payload, "entitlementSnapshotId");
        var issuedAtUtc = GetDateTimeOffset(payload, "issuedAtUtc");
        var entitlement = new ControlCloudEntitlementProjection(
            entitlementSnapshotId,
            GetOptionalGuid(payload, "clientAccessRevisionId") ?? entitlementSnapshotId,
            GetGuid(payload, "contractId"),
            GetOptionalInt64(payload, "contractRevisionNumber") ?? 0,
            GetOptionalGuid(payload, "productCatalogRevisionId") ?? Guid.Empty,
            GetOptionalInt64(payload, "productCatalogRevisionNumber") ?? 0,
            GetInt64(payload, "entitlementVersion"),
            GetGuid(payload, "sourceInvoiceId"),
            GetString(payload, "sourceInvoiceNumber"),
            GetString(payload, "status"),
            GetDateOnly(payload, "paidUntil"),
            GetDateOnly(payload, "graceUntil"),
            GetDateOnly(payload, "offlineValidUntil"),
            GetInt32(payload, "allowedDevices"),
            GetInt32(payload, "allowedBranches"),
            issuedAtUtc,
            modules,
            GetOptionalInt32(payload, "allowedNamedUsers"),
            GetOptionalInt32(payload, "allowedConcurrentUsers"),
            featureLimits,
            GetOptionalDateTimeOffset(payload, "effectiveFromUtc") ?? issuedAtUtc);

        await _projections.ApplyChangeAsync(
            new ControlCloudEntitlementIssuedChange(
                clientId,
                "PKR",
                envelope.MessageId,
                envelope.OccurredAtUtc,
                projectedAtUtc,
                entitlement),
            cancellationToken);

        return true;
    }

    private static Guid GetGuid(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetGuid();
    }

    private static Guid? GetOptionalGuid(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && property.TryGetGuid(out var value)
                ? value
                : null;
    }

    private static string GetString(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetString()?.Trim() ?? "";
    }

    private static string? GetOptionalString(JsonElement payload, string propertyName)
    {
        if (!payload.TryGetProperty(propertyName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var value = property.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal GetDecimal(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetDecimal();
    }

    private static decimal? GetOptionalDecimal(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var property)
               && property.ValueKind != JsonValueKind.Null
               && property.TryGetDecimal(out var value)
            ? value
            : null;
    }

    private static int GetInt32(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetInt32();
    }

    private static int? GetOptionalInt32(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var property)
            && property.ValueKind != JsonValueKind.Null
            && property.TryGetInt32(out var value)
                ? value
                : null;
    }

    private static long GetInt64(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetInt64();
    }

    private static long? GetOptionalInt64(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var property)
            && property.TryGetInt64(out var value)
                ? value
                : null;
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

    private static DateTimeOffset? GetOptionalDateTimeOffset(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var property)
               && property.ValueKind == JsonValueKind.String
               && property.TryGetDateTimeOffset(out var value)
            ? value
            : null;
    }
}
