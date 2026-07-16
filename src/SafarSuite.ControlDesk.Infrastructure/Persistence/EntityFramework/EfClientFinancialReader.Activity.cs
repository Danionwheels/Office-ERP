using System.Data;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfClientFinancialReader
{
    public Task<ClientFinancialActivityReadPage> ReadActivityPageAsync(
        ClientFinancialActivityReadRequest request,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                WITH invoice_amounts AS MATERIALIZED
                (
                    SELECT
                        i.invoice_id,
                        i.number,
                        i.issue_date,
                        i.status,
                        i.currency_code,
                        COALESCE(SUM(il.amount), 0)::numeric AS total_amount
                    FROM control.invoices AS i
                    LEFT JOIN control.invoice_lines AS il ON il.invoice_id = i.invoice_id
                    WHERE i.client_id = @client_id
                      AND (@from_date IS NULL OR i.issue_date >= @from_date)
                      AND (@to_date IS NULL OR i.issue_date <= @to_date)
                    GROUP BY
                        i.invoice_id,
                        i.number,
                        i.issue_date,
                        i.status,
                        i.currency_code
                ),
                raw_activity AS MATERIALIZED
                (
                    SELECT
                        COALESCE(j.entry_date, i.issue_date) AS entry_date,
                        1 AS sort_order,
                        'Invoice'::text AS document_type,
                        i.number::text AS reference,
                        i.invoice_id AS document_id,
                        i.invoice_id,
                        NULL::uuid AS payment_id,
                        NULL::uuid AS refund_id,
                        NULL::uuid AS credit_application_id,
                        ('Invoice ' || i.number)::text AS description,
                        i.total_amount::numeric AS debit,
                        0::numeric AS credit,
                        i.currency_code::text AS currency_code,
                        j.journal_entry_id
                    FROM invoice_amounts AS i
                    LEFT JOIN LATERAL
                    (
                        SELECT entry_date, journal_entry_id
                        FROM control.journal_entries
                        WHERE client_id = @client_id
                          AND source_type = 'BillingInvoice'
                          AND source_document_id = i.invoice_id
                        ORDER BY entry_date, created_at_utc, journal_entry_id
                        LIMIT 1
                    ) AS j ON TRUE
                    WHERE i.status IN ('Issued', 'PartiallyPaid', 'Paid', 'Void')

                    UNION ALL

                    SELECT
                        COALESCE(j.entry_date, i.issue_date),
                        2,
                        'Invoice void'::text,
                        i.number::text,
                        i.invoice_id,
                        i.invoice_id,
                        NULL::uuid,
                        NULL::uuid,
                        NULL::uuid,
                        ('Void invoice ' || i.number)::text,
                        0::numeric,
                        i.total_amount::numeric,
                        i.currency_code::text,
                        j.journal_entry_id
                    FROM invoice_amounts AS i
                    LEFT JOIN LATERAL
                    (
                        SELECT entry_date, journal_entry_id
                        FROM control.journal_entries
                        WHERE client_id = @client_id
                          AND source_type = 'BillingInvoiceVoid'
                          AND source_document_id = i.invoice_id
                        ORDER BY entry_date, created_at_utc, journal_entry_id
                        LIMIT 1
                    ) AS j ON TRUE
                    WHERE i.status = 'Void'

                    UNION ALL

                    SELECT
                        COALESCE(j.entry_date, p.received_on),
                        3,
                        'Payment'::text,
                        p.reference::text,
                        p.payment_id,
                        p.invoice_id,
                        p.payment_id,
                        NULL::uuid,
                        NULL::uuid,
                        ('Payment ' || p.reference || ' (' || p.method || ') - ' || p.status)::text,
                        0::numeric,
                        CASE WHEN p.status IN ('Approved', 'Reversed') THEN p.amount ELSE 0 END::numeric,
                        p.currency_code::text,
                        j.journal_entry_id
                    FROM control.payments AS p
                    LEFT JOIN LATERAL
                    (
                        SELECT entry_date, journal_entry_id
                        FROM control.journal_entries
                        WHERE client_id = @client_id
                          AND source_type = 'PaymentReceipt'
                          AND source_document_id = p.payment_id
                        ORDER BY entry_date, created_at_utc, journal_entry_id
                        LIMIT 1
                    ) AS j ON TRUE
                    WHERE p.client_id = @client_id
                      AND (@from_date IS NULL OR p.received_on >= @from_date)
                      AND (@to_date IS NULL OR p.received_on <= @to_date)

                    UNION ALL

                    SELECT
                        COALESCE(j.entry_date, p.received_on),
                        4,
                        'Payment reversal'::text,
                        p.reference::text,
                        p.payment_id,
                        p.invoice_id,
                        p.payment_id,
                        NULL::uuid,
                        NULL::uuid,
                        ('Reversal of payment ' || p.reference)::text,
                        p.amount::numeric,
                        0::numeric,
                        p.currency_code::text,
                        j.journal_entry_id
                    FROM control.payments AS p
                    LEFT JOIN LATERAL
                    (
                        SELECT entry_date, journal_entry_id
                        FROM control.journal_entries
                        WHERE client_id = @client_id
                          AND source_type = 'PaymentReversal'
                          AND source_document_id = p.payment_id
                        ORDER BY entry_date, created_at_utc, journal_entry_id
                        LIMIT 1
                    ) AS j ON TRUE
                    WHERE p.client_id = @client_id
                      AND p.status = 'Reversed'
                      AND (@from_date IS NULL OR p.received_on >= @from_date)
                      AND (@to_date IS NULL OR p.received_on <= @to_date)

                    UNION ALL

                    SELECT
                        COALESCE(j.entry_date, n.credit_date),
                        5,
                        'Credit note'::text,
                        n.number::text,
                        n.credit_note_id,
                        n.invoice_id,
                        NULL::uuid,
                        NULL::uuid,
                        NULL::uuid,
                        ('Credit note ' || n.number)::text,
                        0::numeric,
                        n.total_amount::numeric,
                        n.currency_code::text,
                        j.journal_entry_id
                    FROM control.credit_notes AS n
                    LEFT JOIN LATERAL
                    (
                        SELECT entry_date, journal_entry_id
                        FROM control.journal_entries
                        WHERE client_id = @client_id
                          AND source_type = 'BillingCreditNote'
                          AND source_document_id = n.credit_note_id
                        ORDER BY entry_date, created_at_utc, journal_entry_id
                        LIMIT 1
                    ) AS j ON TRUE
                    WHERE n.client_id = @client_id
                      AND (@from_date IS NULL OR n.credit_date >= @from_date)
                      AND (@to_date IS NULL OR n.credit_date <= @to_date)

                    UNION ALL

                    SELECT
                        COALESCE(j.entry_date, r.refunded_on),
                        6,
                        'Client refund'::text,
                        r.reference::text,
                        r.client_refund_id,
                        NULL::uuid,
                        NULL::uuid,
                        r.client_refund_id,
                        NULL::uuid,
                        ('Client refund ' || r.reference || ' (' || r.method || ')')::text,
                        r.amount::numeric,
                        0::numeric,
                        r.currency_code::text,
                        j.journal_entry_id
                    FROM control.client_refunds AS r
                    LEFT JOIN LATERAL
                    (
                        SELECT entry_date, journal_entry_id
                        FROM control.journal_entries
                        WHERE client_id = @client_id
                          AND source_type = 'ClientRefund'
                          AND source_document_id = r.client_refund_id
                        ORDER BY entry_date, created_at_utc, journal_entry_id
                        LIMIT 1
                    ) AS j ON TRUE
                    WHERE r.client_id = @client_id
                      AND (@from_date IS NULL OR r.refunded_on >= @from_date)
                      AND (@to_date IS NULL OR r.refunded_on <= @to_date)

                    UNION ALL

                    SELECT
                        a.applied_on,
                        7,
                        'Applied credit'::text,
                        a.reference::text,
                        a.client_credit_application_id,
                        a.invoice_id,
                        NULL::uuid,
                        NULL::uuid,
                        a.client_credit_application_id,
                        ('Applied credit ' || a.reference)::text,
                        a.amount::numeric,
                        a.amount::numeric,
                        a.currency_code::text,
                        NULL::uuid
                    FROM control.client_credit_applications AS a
                    WHERE a.client_id = @client_id
                      AND (@from_date IS NULL OR a.applied_on >= @from_date)
                      AND (@to_date IS NULL OR a.applied_on <= @to_date)
                ),
                balanced AS MATERIALIZED
                (
                    SELECT
                        activity.*,
                        SUM(activity.debit - activity.credit) OVER
                        (
                            PARTITION BY activity.currency_code
                            ORDER BY
                                activity.entry_date,
                                activity.sort_order,
                                activity.reference,
                                activity.document_id
                            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                        )::numeric AS running_balance
                    FROM raw_activity AS activity
                ),
                filtered AS MATERIALIZED
                (
                    SELECT *
                    FROM balanced
                    WHERE @search = ''
                       OR LOWER(reference) LIKE '%' || @search || '%'
                       OR LOWER(document_type) LIKE '%' || @search || '%'
                       OR LOWER(description) LIKE '%' || @search || '%'
                ),
                page AS
                (
                    SELECT *
                    FROM filtered
                    WHERE NOT @has_cursor
                       OR (entry_date, sort_order, reference, document_id)
                          < (@after_date, @after_sort_order, @after_reference, @after_id)
                    ORDER BY entry_date DESC, sort_order DESC, reference DESC, document_id DESC
                    LIMIT @take
                ),
                totals AS
                (
                    SELECT COUNT(*)::bigint AS filtered_count FROM filtered
                )
                SELECT
                    p.entry_date,
                    p.sort_order,
                    p.document_type,
                    p.reference,
                    p.document_id,
                    p.invoice_id,
                    p.payment_id,
                    p.refund_id,
                    p.credit_application_id,
                    p.description,
                    p.debit,
                    p.credit,
                    p.running_balance,
                    p.currency_code,
                    p.journal_entry_id,
                    totals.filtered_count
                FROM totals
                LEFT JOIN page AS p ON TRUE
                ORDER BY p.entry_date DESC, p.sort_order DESC, p.reference DESC, p.document_id DESC;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", request.ClientId.Value, DbType.Guid);
            AddParameter(command, "from_date", request.FromDate, DbType.Date);
            AddParameter(command, "to_date", request.ToDate, DbType.Date);
            AddParameter(command, "search", request.Search, DbType.String);
            AddParameter(command, "has_cursor", request.AfterDocumentId.HasValue, DbType.Boolean);
            AddParameter(command, "after_date", request.AfterEntryDate ?? DateOnly.MinValue, DbType.Date);
            AddParameter(command, "after_sort_order", request.AfterSortOrder ?? 0, DbType.Int32);
            AddParameter(command, "after_reference", request.AfterReference ?? string.Empty, DbType.String);
            AddParameter(command, "after_id", request.AfterDocumentId ?? Guid.Empty, DbType.Guid);
            AddParameter(command, "take", request.Take, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<ClientFinancialActivityReadItem>(request.Take);
            long filteredCount = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                filteredCount = reader.GetInt64(15);

                if (reader.IsDBNull(0))
                {
                    continue;
                }

                items.Add(new ClientFinancialActivityReadItem(
                    reader.GetFieldValue<DateOnly>(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetGuid(4),
                    reader.IsDBNull(5) ? null : reader.GetGuid(5),
                    reader.IsDBNull(6) ? null : reader.GetGuid(6),
                    reader.IsDBNull(7) ? null : reader.GetGuid(7),
                    reader.IsDBNull(8) ? null : reader.GetGuid(8),
                    reader.GetString(9),
                    reader.GetDecimal(10),
                    reader.GetDecimal(11),
                    reader.GetDecimal(12),
                    reader.GetString(13),
                    reader.IsDBNull(14) ? null : reader.GetGuid(14)));
            }

            return new ClientFinancialActivityReadPage(items, filteredCount);
        }, cancellationToken);
    }
}
