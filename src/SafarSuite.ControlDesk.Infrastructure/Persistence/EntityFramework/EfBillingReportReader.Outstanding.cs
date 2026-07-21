using System.Data;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfBillingReportReader
{
    public Task<OutstandingInvoiceReadPage> ReadOutstandingInvoicePageAsync(
        OutstandingInvoiceReadRequest request,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                WITH invoice_amounts AS MATERIALIZED
                (
                    SELECT
                        i.invoice_id,
                        i.client_id,
                        c.code AS client_code,
                        c.display_name AS client_name,
                        i.number,
                        i.issue_date,
                        i.due_date,
                        i.status,
                        COALESCE(SUM(il.amount), 0)::numeric AS total_amount,
                        i.amount_paid_amount,
                        i.currency_code,
                        i.created_at_utc
                    FROM control.invoices AS i
                    INNER JOIN control.clients AS c ON c.client_id = i.client_id
                    LEFT JOIN control.invoice_lines AS il ON il.invoice_id = i.invoice_id
                    WHERE i.status IN ('Issued', 'PartiallyPaid')
                      AND (@client_id IS NULL OR i.client_id = @client_id)
                      AND (@from_date IS NULL OR i.issue_date >= @from_date)
                      AND (@to_date IS NULL OR i.issue_date <= @to_date)
                      AND (@currency_code IS NULL OR i.currency_code = @currency_code)
                    GROUP BY
                        i.invoice_id,
                        i.client_id,
                        c.code,
                        c.display_name,
                        i.number,
                        i.issue_date,
                        i.due_date,
                        i.status,
                        i.amount_paid_amount,
                        i.currency_code,
                        i.created_at_utc
                ),
                residuals AS MATERIALIZED
                (
                    SELECT
                        i.*,
                        (i.total_amount - i.amount_paid_amount)::numeric AS balance_due,
                        GREATEST(@today - i.due_date, 0)::integer AS days_overdue,
                        CASE
                            WHEN i.due_date >= @today THEN 'Current'
                            WHEN @today - i.due_date <= 30 THEN '1-30'
                            WHEN @today - i.due_date <= 60 THEN '31-60'
                            WHEN @today - i.due_date <= 90 THEN '61-90'
                            ELSE '91+'
                        END AS aging_bucket
                    FROM invoice_amounts AS i
                ),
                filtered AS MATERIALIZED
                (
                    SELECT *
                    FROM residuals
                    WHERE balance_due > 0
                      AND (@min_amount IS NULL OR balance_due >= @min_amount)
                      AND (@max_amount IS NULL OR balance_due <= @max_amount)
                      AND
                      (
                          @status = 'All'
                          OR (@status = 'Issued' AND status = 'Issued')
                          OR (@status = 'PartiallyPaid' AND status = 'PartiallyPaid')
                          OR (@status = 'Overdue' AND due_date < @today)
                      )
                ),
                page AS
                (
                    SELECT *
                    FROM filtered
                    WHERE NOT @has_cursor
                       OR (issue_date, created_at_utc, invoice_id)
                          < (@after_issue_date, @after_created_at_utc, @after_invoice_id)
                    ORDER BY issue_date DESC, created_at_utc DESC, invoice_id DESC
                    LIMIT @take
                ),
                totals AS
                (
                    SELECT COUNT(*)::bigint AS filtered_count FROM filtered
                )
                SELECT
                    p.invoice_id,
                    p.client_id,
                    p.client_code,
                    p.client_name,
                    p.number,
                    p.issue_date,
                    p.due_date,
                    p.status,
                    p.total_amount,
                    p.amount_paid_amount,
                    p.balance_due,
                    p.currency_code,
                    p.days_overdue,
                    p.aging_bucket,
                    journal.journal_entry_id,
                    p.created_at_utc,
                    totals.filtered_count
                FROM totals
                LEFT JOIN page AS p ON TRUE
                LEFT JOIN LATERAL
                (
                    SELECT j.journal_entry_id
                    FROM control.journal_entries AS j
                    WHERE j.source_type = 'BillingInvoice'
                      AND j.source_document_id = p.invoice_id
                    ORDER BY j.entry_date, j.created_at_utc, j.journal_entry_id
                    LIMIT 1
                ) AS journal ON p.invoice_id IS NOT NULL
                ORDER BY p.issue_date DESC, p.created_at_utc DESC, p.invoice_id DESC;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", request.ClientId, DbType.Guid);
            AddParameter(command, "from_date", request.FromDate, DbType.Date);
            AddParameter(command, "to_date", request.ToDate, DbType.Date);
            AddParameter(command, "min_amount", request.MinAmount, DbType.Decimal);
            AddParameter(command, "max_amount", request.MaxAmount, DbType.Decimal);
            AddParameter(command, "status", request.Status, DbType.String);
            AddParameter(command, "currency_code", request.CurrencyCode, DbType.String);
            AddParameter(command, "today", request.Today, DbType.Date);
            AddParameter(command, "has_cursor", request.AfterInvoiceId.HasValue, DbType.Boolean);
            AddParameter(command, "after_issue_date", request.AfterIssueDate ?? DateOnly.MinValue, DbType.Date);
            AddParameter(
                command,
                "after_created_at_utc",
                request.AfterCreatedAtUtc ?? DateTimeOffset.MinValue,
                DbType.DateTimeOffset);
            AddParameter(command, "after_invoice_id", request.AfterInvoiceId ?? Guid.Empty, DbType.Guid);
            AddParameter(command, "take", request.Take, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<OutstandingInvoiceReadItem>(request.Take);
            long filteredCount = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                filteredCount = reader.GetInt64(16);
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                items.Add(new OutstandingInvoiceReadItem(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetFieldValue<DateOnly>(5),
                    reader.GetFieldValue<DateOnly>(6),
                    reader.GetString(7),
                    reader.GetDecimal(8),
                    reader.GetDecimal(9),
                    reader.GetDecimal(10),
                    reader.GetString(11),
                    reader.GetInt32(12),
                    reader.GetString(13),
                    reader.IsDBNull(14) ? null : reader.GetGuid(14),
                    ReadDateTimeOffset(reader, 15)));
            }

            return new OutstandingInvoiceReadPage(items, filteredCount);
        }, cancellationToken);
    }
}
