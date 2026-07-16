using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfControlCloudClientCommercialProjectionRepository
    : IControlCloudClientCommercialProjectionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudClientCommercialProjectionRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientCommercialProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(projection => projection.ClientId == clientId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(
        ControlCloudClientCommercialProjection projection,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientCommercialProjections
            .SingleOrDefaultAsync(stored => stored.ClientId == projection.ClientId, cancellationToken);

        if (entity is null)
        {
            entity = new ControlCloudClientCommercialProjectionEntity
            {
                ClientId = projection.ClientId
            };
            await _dbContext.ClientCommercialProjections.AddAsync(entity, cancellationToken);
        }

        CopySummary(projection, entity);
    }

    public async Task ApplyChangeAsync(
        ControlCloudCommercialProjectionChange change,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetLockedSummaryAsync(change, cancellationToken);

        switch (change)
        {
            case ControlCloudInvoiceIssuedChange invoiceIssued:
                await ApplyInvoiceIssuedAsync(summary, invoiceIssued, cancellationToken);
                break;
            case ControlCloudInvoiceStatusChangedChange invoiceStatus:
                await ApplyInvoiceStatusAsync(summary, invoiceStatus, cancellationToken);
                break;
            case ControlCloudInvoiceVoidedChange invoiceVoided:
                await ApplyInvoiceVoidedAsync(summary, invoiceVoided, cancellationToken);
                break;
            case ControlCloudPaymentRecordedChange paymentRecorded:
                await ApplyPaymentRecordedAsync(summary, paymentRecorded, cancellationToken);
                break;
            case ControlCloudPaymentReversedChange paymentReversed:
                await ApplyPaymentReversedAsync(summary, paymentReversed, cancellationToken);
                break;
            case ControlCloudCreditNoteIssuedChange creditNoteIssued:
                await ApplyCreditNoteAsync(summary, creditNoteIssued, cancellationToken);
                break;
            case ControlCloudRefundIssuedChange refundIssued:
                await ApplyRefundAsync(summary, refundIssued, cancellationToken);
                break;
            case ControlCloudCreditAppliedChange creditApplied:
                await ApplyCreditApplicationAsync(summary, creditApplied, cancellationToken);
                break;
            case ControlCloudEntitlementIssuedChange entitlementIssued:
                ApplyEntitlement(summary, entitlementIssued);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported commercial projection change '{change.GetType().Name}'.");
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
        var query = _dbContext.CommercialDocuments
            .AsNoTracking()
            .Where(document =>
                document.ClientId == clientId
                && document.DocumentType == documentType);

        if (beforeDate is not null && beforeDocumentId is not null)
        {
            var date = beforeDate.Value;
            var documentId = beforeDocumentId.Value;
            query = query.Where(document =>
                document.DocumentDate < date
                || (document.DocumentDate == date
                    && document.DocumentId.CompareTo(documentId) < 0));
        }

        var entities = await query
            .OrderByDescending(document => document.DocumentDate)
            .ThenByDescending(document => document.DocumentId)
            .Take(Math.Clamp(take, 0, 101))
            .ToArrayAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyCollection<ControlCloudInvoiceProjection>> ListInvoicesAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.CommercialDocuments
            .AsNoTracking()
            .Where(document =>
                document.ClientId == clientId
                && document.DocumentType == ControlCloudCommercialDocumentTypes.Invoice)
            .OrderByDescending(document => document.DocumentDate)
            .ThenByDescending(document => document.DocumentId)
            .ToArrayAsync(cancellationToken);

        return entities
            .Select(Deserialize<ControlCloudInvoiceProjection>)
            .OfType<ControlCloudInvoiceProjection>()
            .ToArray();
    }

    public async Task<ControlCloudInvoiceProjection?> GetInvoiceAsync(
        Guid clientId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CommercialDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(document =>
                document.ClientId == clientId
                && document.DocumentType == ControlCloudCommercialDocumentTypes.Invoice
                && document.DocumentId == invoiceId,
                cancellationToken);

        return Deserialize<ControlCloudInvoiceProjection>(entity);
    }

    public async Task<IReadOnlyCollection<ControlCloudPaymentProjection>> ListPaymentsAsync(
        Guid clientId,
        Guid? invoiceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CommercialDocuments
            .AsNoTracking()
            .Where(document =>
                document.ClientId == clientId
                && document.DocumentType == ControlCloudCommercialDocumentTypes.Payment);

        if (invoiceId is not null)
        {
            query = query.Where(document => document.RelatedDocumentId == invoiceId.Value);
        }

        var entities = await query
            .OrderByDescending(document => document.DocumentDate)
            .ThenByDescending(document => document.DocumentId)
            .ToArrayAsync(cancellationToken);

        return entities
            .Select(Deserialize<ControlCloudPaymentProjection>)
            .OfType<ControlCloudPaymentProjection>()
            .ToArray();
    }

    private async Task<ControlCloudClientCommercialProjectionEntity> GetLockedSummaryAsync(
        ControlCloudCommercialProjectionChange change,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsNpgsql())
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cloud.client_commercial_projections
                    (client_id, currency_code, total_invoiced, total_paid, total_credited,
                     total_refunded, total_credit_applied, balance_due, available_credit,
                     is_paid, last_updated_at_utc, latest_entitlement_json)
                VALUES
                    ({change.ClientId}, {NormalizeCurrency(change.CurrencyCode)}, 0, 0, 0,
                     0, 0, 0, 0, TRUE, {change.ProjectedAtUtc}, NULL)
                ON CONFLICT (client_id) DO NOTHING
                """,
                cancellationToken);

            return await _dbContext.ClientCommercialProjections
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM cloud.client_commercial_projections
                    WHERE client_id = {change.ClientId}
                    FOR UPDATE
                    """)
                .SingleAsync(cancellationToken);
        }

        var entity = await _dbContext.ClientCommercialProjections
            .SingleOrDefaultAsync(summary => summary.ClientId == change.ClientId, cancellationToken);

        if (entity is not null)
        {
            return entity;
        }

        entity = new ControlCloudClientCommercialProjectionEntity
        {
            ClientId = change.ClientId,
            CurrencyCode = NormalizeCurrency(change.CurrencyCode),
            IsPaid = true,
            LastUpdatedAtUtc = change.ProjectedAtUtc
        };
        await _dbContext.ClientCommercialProjections.AddAsync(entity, cancellationToken);

        return entity;
    }

    private static void CopySummary(
        ControlCloudClientCommercialProjection source,
        ControlCloudClientCommercialProjectionEntity target)
    {
        target.CurrencyCode = NormalizeCurrency(source.CurrencyCode);
        target.TotalInvoiced = source.TotalInvoiced;
        target.TotalPaid = source.TotalPaid;
        target.TotalCredited = source.TotalCredited;
        target.TotalRefunded = source.TotalRefunded;
        target.TotalCreditApplied = source.TotalCreditApplied;
        target.BalanceDue = source.BalanceDue;
        target.AvailableCredit = source.AvailableCredit;
        target.IsPaid = source.IsPaid;
        target.LastUpdatedAtUtc = source.LastUpdatedAtUtc;
        target.LatestEntitlementJson = source.LatestEntitlement is null
            ? null
            : JsonSerializer.Serialize(source.LatestEntitlement, JsonOptions);
    }

    private static ControlCloudClientCommercialProjection ToDomain(
        ControlCloudClientCommercialProjectionEntity entity)
    {
        return new ControlCloudClientCommercialProjection
        {
            ClientId = entity.ClientId,
            CurrencyCode = entity.CurrencyCode,
            TotalInvoiced = entity.TotalInvoiced,
            TotalPaid = entity.TotalPaid,
            TotalCredited = entity.TotalCredited,
            TotalRefunded = entity.TotalRefunded,
            TotalCreditApplied = entity.TotalCreditApplied,
            BalanceDue = entity.BalanceDue,
            AvailableCredit = entity.AvailableCredit,
            IsPaid = entity.IsPaid,
            LastUpdatedAtUtc = entity.LastUpdatedAtUtc,
            LatestEntitlement = string.IsNullOrWhiteSpace(entity.LatestEntitlementJson)
                ? null
                : JsonSerializer.Deserialize<ControlCloudEntitlementProjection>(
                    entity.LatestEntitlementJson,
                    JsonOptions)
        };
    }

    private static ControlCloudCommercialDocumentProjection ToDomain(
        ControlCloudCommercialDocumentEntity entity)
    {
        return new ControlCloudCommercialDocumentProjection(
            entity.ClientId,
            entity.DocumentType,
            entity.DocumentId,
            entity.RelatedDocumentId,
            entity.Reference,
            entity.Status,
            entity.DocumentDate,
            entity.Amount,
            entity.BalanceAmount,
            entity.CurrencyCode,
            entity.OccurredAtUtc,
            entity.LastUpdatedAtUtc);
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();

        return normalized.Length == 0 ? "PKR" : normalized;
    }
}
