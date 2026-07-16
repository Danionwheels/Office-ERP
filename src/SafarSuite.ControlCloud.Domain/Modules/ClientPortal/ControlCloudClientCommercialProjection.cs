namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientCommercialProjection
{
    public Guid ClientId { get; set; }

    public string CurrencyCode { get; set; } = "PKR";

    public decimal TotalInvoiced { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalCredited { get; set; }

    public decimal TotalRefunded { get; set; }

    public decimal TotalCreditApplied { get; set; }

    public decimal BalanceDue { get; set; }

    public decimal AvailableCredit { get; set; }

    public bool IsPaid { get; set; }

    public DateTimeOffset LastUpdatedAtUtc { get; set; }

    public Dictionary<Guid, ControlCloudInvoiceProjection> Invoices { get; set; } = new();

    public Dictionary<Guid, ControlCloudPaymentProjection> Payments { get; set; } = new();

    public Dictionary<Guid, ControlCloudCreditNoteProjection> CreditNotes { get; set; } = new();

    public Dictionary<Guid, ControlCloudClientRefundProjection> Refunds { get; set; } = new();

    public Dictionary<Guid, ControlCloudCreditApplicationProjection> CreditApplications { get; set; } = new();

    public ControlCloudEntitlementProjection? LatestEntitlement { get; set; }

    public static ControlCloudClientCommercialProjection Create(Guid clientId, string currencyCode)
    {
        return new ControlCloudClientCommercialProjection
        {
            ClientId = clientId,
            CurrencyCode = NormalizeCurrency(currencyCode)
        };
    }

    public void ApplyInvoiceIssued(
        ControlCloudInvoiceProjection invoice,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(invoice.CurrencyCode);
        Invoices[invoice.InvoiceId] = MergeInvoiceIssued(invoice);
        LastUpdatedAtUtc = updatedAtUtc;
        RecalculateFinancialSummary();
    }

    public void ApplyInvoiceStatus(
        Guid invoiceId,
        string invoiceStatus,
        decimal balanceDue,
        string currencyCode,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(currencyCode);

        if (Invoices.TryGetValue(invoiceId, out var invoice))
        {
            Invoices[invoiceId] = invoice with
            {
                InvoiceStatus = invoiceStatus,
                BalanceDue = balanceDue
            };
        }
        else
        {
            BalanceDue = Math.Max(balanceDue, 0);
        }

        LastUpdatedAtUtc = updatedAtUtc;
        RecalculateFinancialSummary();
    }

    public void ApplyInvoiceVoided(
        Guid invoiceId,
        string invoiceStatus,
        decimal balanceDue,
        string currencyCode,
        DateOnly voidDate,
        string reason,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(currencyCode);

        if (Invoices.TryGetValue(invoiceId, out var invoice))
        {
            Invoices[invoiceId] = invoice with
            {
                InvoiceStatus = invoiceStatus,
                BalanceDue = balanceDue,
                VoidedOn = voidDate,
                VoidReason = reason
            };
        }

        LastUpdatedAtUtc = updatedAtUtc;
        RecalculateFinancialSummary();
    }

    public void ApplyPaymentRecorded(
        ControlCloudPaymentProjection payment,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(payment.CurrencyCode);
        Payments[payment.PaymentId] = payment;
        ApplyInvoiceStatus(
            payment.InvoiceId,
            payment.InvoiceBalanceDue <= 0 ? "Paid" : "PartiallyPaid",
            payment.InvoiceBalanceDue,
            payment.CurrencyCode,
            updatedAtUtc);
    }

    public void ApplyPaymentReversed(
        Guid paymentId,
        Guid invoiceId,
        string paymentStatus,
        decimal amount,
        decimal invoiceBalanceDue,
        string currencyCode,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(currencyCode);

        if (Payments.TryGetValue(paymentId, out var payment))
        {
            Payments[paymentId] = payment with
            {
                PaymentStatus = paymentStatus,
                InvoiceBalanceDue = invoiceBalanceDue
            };
        }

        ApplyInvoiceStatus(
            invoiceId,
            invoiceBalanceDue <= 0 ? "Paid" : "Issued",
            invoiceBalanceDue,
            currencyCode,
            updatedAtUtc);
    }

    public void ApplyCreditNote(
        ControlCloudCreditNoteProjection creditNote,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(creditNote.CurrencyCode);
        CreditNotes[creditNote.CreditNoteId] = creditNote;
        LastUpdatedAtUtc = updatedAtUtc;
        RecalculateFinancialSummary();
    }

    public void ApplyRefund(
        ControlCloudClientRefundProjection refund,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(refund.CurrencyCode);
        Refunds[refund.RefundId] = refund;
        LastUpdatedAtUtc = updatedAtUtc;
        RecalculateFinancialSummary();
    }

    public void ApplyCreditApplication(
        ControlCloudCreditApplicationProjection application,
        DateTimeOffset updatedAtUtc)
    {
        CurrencyCode = NormalizeCurrency(application.CurrencyCode);
        CreditApplications[application.CreditApplicationId] = application;

        if (Invoices.TryGetValue(application.InvoiceId, out var invoice))
        {
            Invoices[application.InvoiceId] = invoice with
            {
                InvoiceStatus = application.InvoiceStatus,
                BalanceDue = application.InvoiceBalanceAfter
            };
        }

        LastUpdatedAtUtc = updatedAtUtc;
        RecalculateFinancialSummary();
    }

    public void ApplyEntitlement(
        ControlCloudEntitlementProjection entitlement,
        DateTimeOffset updatedAtUtc)
    {
        if (entitlement.EntitlementVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entitlement),
                "Entitlement version must be greater than zero.");
        }

        if (LatestEntitlement is not null
            && entitlement.EntitlementVersion <= LatestEntitlement.EntitlementVersion)
        {
            return;
        }

        LatestEntitlement = entitlement;
        LastUpdatedAtUtc = updatedAtUtc;
    }

    private ControlCloudInvoiceProjection MergeInvoiceIssued(ControlCloudInvoiceProjection invoice)
    {
        var merged = invoice;

        if (Invoices.TryGetValue(invoice.InvoiceId, out var existing))
        {
            merged = invoice with
            {
                InvoiceStatus = existing.InvoiceStatus,
                BalanceDue = existing.BalanceDue,
                VoidedOn = existing.VoidedOn,
                VoidReason = existing.VoidReason
            };
        }

        var latestApplication = CreditApplications.Values
            .Where(application => application.InvoiceId == invoice.InvoiceId)
            .OrderBy(application => application.AppliedOn)
            .ThenBy(application => application.CreditApplicationId)
            .LastOrDefault();

        if (latestApplication is not null)
        {
            return merged with
            {
                InvoiceStatus = latestApplication.InvoiceStatus,
                BalanceDue = latestApplication.InvoiceBalanceAfter
            };
        }

        var latestPayment = Payments.Values
            .Where(payment => payment.InvoiceId == invoice.InvoiceId)
            .OrderBy(payment => payment.ReceivedOn)
            .ThenBy(payment => payment.PaymentId)
            .LastOrDefault();

        if (latestPayment is not null)
        {
            return merged with
            {
                InvoiceStatus = latestPayment.InvoiceBalanceDue <= 0 ? "Paid" : "PartiallyPaid",
                BalanceDue = latestPayment.InvoiceBalanceDue
            };
        }

        return merged;
    }

    private void RecalculateFinancialSummary()
    {
        TotalInvoiced = Invoices.Values
            .Where(invoice => !string.Equals(invoice.InvoiceStatus, "Void", StringComparison.OrdinalIgnoreCase))
            .Sum(invoice => invoice.TotalAmount);
        TotalPaid = Payments.Values
            .Where(payment => !string.Equals(payment.PaymentStatus, "Reversed", StringComparison.OrdinalIgnoreCase))
            .Sum(payment => payment.Amount);
        TotalCredited = CreditNotes.Values.Sum(creditNote => creditNote.Amount);
        TotalRefunded = Refunds.Values.Sum(refund => refund.Amount);
        TotalCreditApplied = CreditApplications.Values.Sum(application => application.Amount);
        BalanceDue = Invoices.Values
            .Where(invoice => !string.Equals(invoice.InvoiceStatus, "Void", StringComparison.OrdinalIgnoreCase))
            .Sum(invoice => invoice.BalanceDue);
        AvailableCredit = Math.Max(TotalCredited - TotalRefunded - TotalCreditApplied, 0);
        IsPaid = BalanceDue <= 0;
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();

        return normalized.Length == 0 ? "PKR" : normalized;
    }
}

public sealed record ControlCloudInvoiceProjection(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid ContractId,
    string InvoiceStatus,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal BalanceDue,
    string CurrencyCode,
    DateOnly? VoidedOn = null,
    string? VoidReason = null,
    ControlCloudClientBillingDetailProjection? Client = null,
    IReadOnlyCollection<ControlCloudInvoiceLineProjection>? Lines = null);

public sealed record ControlCloudClientBillingDetailProjection(
    string Name,
    string? ContactName,
    string? Email,
    string? Phone);

public sealed record ControlCloudInvoiceLineProjection(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string CurrencyCode);

public sealed record ControlCloudPaymentProjection(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string PaymentStatus,
    string PaymentMethod,
    string PaymentReference,
    decimal Amount,
    decimal InvoiceBalanceDue,
    string CurrencyCode,
    DateOnly ReceivedOn);

public sealed record ControlCloudCreditNoteProjection(
    Guid CreditNoteId,
    string CreditNoteNumber,
    Guid InvoiceId,
    string InvoiceNumber,
    string CreditNoteStatus,
    DateOnly CreditDate,
    decimal Amount,
    string CurrencyCode,
    string Reason);

public sealed record ControlCloudClientRefundProjection(
    Guid RefundId,
    string RefundStatus,
    string RefundMethod,
    string RefundReference,
    decimal Amount,
    decimal ClientBalanceBefore,
    decimal ClientBalanceAfter,
    string CurrencyCode,
    DateOnly RefundedOn);

public sealed record ControlCloudCreditApplicationProjection(
    Guid CreditApplicationId,
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

public sealed record ControlCloudEntitlementProjection(
    Guid EntitlementSnapshotId,
    Guid ClientAccessRevisionId,
    Guid ContractId,
    long ContractRevisionNumber,
    Guid ProductCatalogRevisionId,
    long ProductCatalogRevisionNumber,
    long EntitlementVersion,
    Guid SourceInvoiceId,
    string SourceInvoiceNumber,
    string Status,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyCollection<ControlCloudEntitlementModuleProjection> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ControlCloudEntitlementFeatureLimitProjection>? FeatureLimits = null,
    DateTimeOffset? EffectiveFromUtc = null);

public sealed record ControlCloudEntitlementModuleProjection(
    string ModuleCode,
    bool IsEnabled);

public sealed record ControlCloudEntitlementFeatureLimitProjection(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
