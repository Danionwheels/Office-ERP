using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileControlCloudClientCommercialProjectionRepository
    : IControlCloudClientCommercialProjectionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudClientCommercialProjectionRepository(ControlCloudReceiverOptions options)
    {
        _storePath = ResolveStorePath(options.ProjectionStorePath);
    }

    public async Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);

            return projections.TryGetValue(clientId, out var projection)
                ? projection
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ControlCloudClientCommercialProjection projection,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);
            projections[projection.ClientId] = projection;

            await WriteAllAsync(projections, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ApplyChangeAsync(
        ControlCloudCommercialProjectionChange change,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);
            var projection = projections.GetValueOrDefault(change.ClientId)
                ?? ControlCloudClientCommercialProjection.Create(change.ClientId, change.CurrencyCode);

            ApplyChange(projection, change);
            projections[projection.ClientId] = projection;

            await WriteAllAsync(projections, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControlCloudCommercialDocumentProjection>> ListDocumentsAsync(
        Guid clientId,
        string documentType,
        DateOnly? beforeDate,
        Guid? beforeDocumentId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);

            if (!projections.TryGetValue(clientId, out var projection))
            {
                return Array.Empty<ControlCloudCommercialDocumentProjection>();
            }

            var documents = ToDocuments(projection, documentType)
                .OrderByDescending(document => document.DocumentDate)
                .ThenByDescending(document => document.DocumentId);

            if (beforeDate is not null && beforeDocumentId is not null)
            {
                documents = documents
                    .Where(document =>
                        document.DocumentDate < beforeDate.Value
                        || (document.DocumentDate == beforeDate.Value
                            && document.DocumentId.CompareTo(beforeDocumentId.Value) < 0))
                    .OrderByDescending(document => document.DocumentDate)
                    .ThenByDescending(document => document.DocumentId);
            }

            return documents.Take(Math.Max(take, 0)).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControlCloudInvoiceProjection>> ListInvoicesAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);
            return projections.TryGetValue(clientId, out var projection)
                ? projection.Invoices.Values
                    .OrderByDescending(invoice => invoice.IssueDate)
                    .ThenByDescending(invoice => invoice.InvoiceId)
                    .ToArray()
                : Array.Empty<ControlCloudInvoiceProjection>();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudInvoiceProjection?> GetInvoiceAsync(
        Guid clientId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);
            return projections.TryGetValue(clientId, out var projection)
                && projection.Invoices.TryGetValue(invoiceId, out var invoice)
                    ? invoice
                    : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControlCloudPaymentProjection>> ListPaymentsAsync(
        Guid clientId,
        Guid? invoiceId = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);
            if (!projections.TryGetValue(clientId, out var projection))
            {
                return Array.Empty<ControlCloudPaymentProjection>();
            }

            return projection.Payments.Values
                .Where(payment => invoiceId is null || payment.InvoiceId == invoiceId.Value)
                .OrderByDescending(payment => payment.ReceivedOn)
                .ThenByDescending(payment => payment.PaymentId)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void ApplyChange(
        ControlCloudClientCommercialProjection projection,
        ControlCloudCommercialProjectionChange change)
    {
        switch (change)
        {
            case ControlCloudInvoiceIssuedChange invoiceIssued:
                projection.ApplyInvoiceIssued(invoiceIssued.Invoice, change.ProjectedAtUtc);
                break;
            case ControlCloudInvoiceStatusChangedChange invoiceStatus:
                projection.ApplyInvoiceStatus(
                    invoiceStatus.InvoiceId,
                    invoiceStatus.InvoiceStatus,
                    invoiceStatus.BalanceDue,
                    invoiceStatus.CurrencyCode,
                    change.ProjectedAtUtc);
                break;
            case ControlCloudInvoiceVoidedChange invoiceVoided:
                projection.ApplyInvoiceVoided(
                    invoiceVoided.InvoiceId,
                    invoiceVoided.InvoiceStatus,
                    invoiceVoided.BalanceDue,
                    invoiceVoided.CurrencyCode,
                    invoiceVoided.VoidedOn,
                    invoiceVoided.VoidReason,
                    change.ProjectedAtUtc);
                break;
            case ControlCloudPaymentRecordedChange paymentRecorded:
                projection.ApplyPaymentRecorded(paymentRecorded.Payment, change.ProjectedAtUtc);
                break;
            case ControlCloudPaymentReversedChange paymentReversed:
                projection.ApplyPaymentReversed(
                    paymentReversed.PaymentId,
                    paymentReversed.InvoiceId,
                    paymentReversed.PaymentStatus,
                    paymentReversed.Amount,
                    paymentReversed.InvoiceBalanceDue,
                    paymentReversed.CurrencyCode,
                    change.ProjectedAtUtc);
                break;
            case ControlCloudCreditNoteIssuedChange creditNoteIssued:
                projection.ApplyCreditNote(creditNoteIssued.CreditNote, change.ProjectedAtUtc);
                break;
            case ControlCloudRefundIssuedChange refundIssued:
                projection.ApplyRefund(refundIssued.Refund, change.ProjectedAtUtc);
                break;
            case ControlCloudCreditAppliedChange creditApplied:
                projection.ApplyCreditApplication(creditApplied.CreditApplication, change.ProjectedAtUtc);
                break;
            case ControlCloudEntitlementIssuedChange entitlementIssued:
                projection.ApplyEntitlement(entitlementIssued.Entitlement, change.ProjectedAtUtc);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported commercial projection change '{change.GetType().Name}'.");
        }
    }

    private static IEnumerable<ControlCloudCommercialDocumentProjection> ToDocuments(
        ControlCloudClientCommercialProjection projection,
        string documentType)
    {
        return documentType switch
        {
            ControlCloudCommercialDocumentTypes.Invoice => projection.Invoices.Values.Select(invoice =>
                ToDocument(
                    projection,
                    documentType,
                    invoice.InvoiceId,
                    null,
                    invoice.InvoiceNumber,
                    invoice.InvoiceStatus,
                    invoice.IssueDate,
                    invoice.TotalAmount,
                    invoice.BalanceDue,
                    invoice.CurrencyCode)),
            ControlCloudCommercialDocumentTypes.Payment => projection.Payments.Values.Select(payment =>
                ToDocument(
                    projection,
                    documentType,
                    payment.PaymentId,
                    payment.InvoiceId,
                    payment.PaymentReference,
                    payment.PaymentStatus,
                    payment.ReceivedOn,
                    payment.Amount,
                    payment.InvoiceBalanceDue,
                    payment.CurrencyCode)),
            ControlCloudCommercialDocumentTypes.CreditNote => projection.CreditNotes.Values.Select(creditNote =>
                ToDocument(
                    projection,
                    documentType,
                    creditNote.CreditNoteId,
                    creditNote.InvoiceId,
                    creditNote.CreditNoteNumber,
                    creditNote.CreditNoteStatus,
                    creditNote.CreditDate,
                    creditNote.Amount,
                    0,
                    creditNote.CurrencyCode)),
            ControlCloudCommercialDocumentTypes.Refund => projection.Refunds.Values.Select(refund =>
                ToDocument(
                    projection,
                    documentType,
                    refund.RefundId,
                    null,
                    refund.RefundReference,
                    refund.RefundStatus,
                    refund.RefundedOn,
                    refund.Amount,
                    refund.ClientBalanceAfter,
                    refund.CurrencyCode)),
            ControlCloudCommercialDocumentTypes.CreditApplication => projection.CreditApplications.Values.Select(application =>
                ToDocument(
                    projection,
                    documentType,
                    application.CreditApplicationId,
                    application.InvoiceId,
                    application.Reference,
                    application.CreditApplicationStatus,
                    application.AppliedOn,
                    application.Amount,
                    application.InvoiceBalanceAfter,
                    application.CurrencyCode)),
            _ => Array.Empty<ControlCloudCommercialDocumentProjection>()
        };
    }

    private static ControlCloudCommercialDocumentProjection ToDocument(
        ControlCloudClientCommercialProjection projection,
        string documentType,
        Guid documentId,
        Guid? relatedDocumentId,
        string reference,
        string status,
        DateOnly documentDate,
        decimal amount,
        decimal balanceAmount,
        string currencyCode)
    {
        return new ControlCloudCommercialDocumentProjection(
            projection.ClientId,
            documentType,
            documentId,
            relatedDocumentId,
            reference,
            status,
            documentDate,
            amount,
            balanceAmount,
            currencyCode,
            projection.LastUpdatedAtUtc,
            projection.LastUpdatedAtUtc);
    }

    private async Task<Dictionary<Guid, ControlCloudClientCommercialProjection>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new Dictionary<Guid, ControlCloudClientCommercialProjection>();
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var projections = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<ControlCloudClientCommercialProjection>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (projections ?? Array.Empty<ControlCloudClientCommercialProjection>())
            .GroupBy(projection => projection.ClientId)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    private async Task WriteAllAsync(
        IReadOnlyDictionary<Guid, ControlCloudClientCommercialProjection> projections,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        await JsonSerializer.SerializeAsync(
            stream,
            projections.Values.OrderBy(projection => projection.ClientId).ToArray(),
            JsonOptions,
            cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-client-projections.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
