using System.Data;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfClientFinancialReader
{
    public Task<ClientInvoiceReadPage> ReadInvoicePageAsync(
        ClientInvoiceReadRequest request,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                WITH invoice_amounts AS MATERIALIZED
                (
                    SELECT
                        i.invoice_id,
                        i.contract_id,
                        i.number,
                        i.issue_date,
                        i.due_date,
                        i.status,
                        COALESCE(SUM(il.amount), 0)::numeric AS total_amount,
                        i.amount_paid_amount,
                        i.currency_code,
                        i.created_at_utc
                    FROM control.invoices AS i
                    LEFT JOIN control.invoice_lines AS il ON il.invoice_id = i.invoice_id
                    WHERE i.client_id = @client_id
                      AND (@from_date IS NULL OR i.issue_date >= @from_date)
                      AND (@to_date IS NULL OR i.issue_date <= @to_date)
                    GROUP BY
                        i.invoice_id,
                        i.contract_id,
                        i.number,
                        i.issue_date,
                        i.due_date,
                        i.status,
                        i.amount_paid_amount,
                        i.currency_code,
                        i.created_at_utc
                ),
                filtered AS MATERIALIZED
                (
                    SELECT
                        i.*,
                        (i.total_amount - i.amount_paid_amount)::numeric AS balance_due
                    FROM invoice_amounts AS i
                    WHERE (@search = '' OR LOWER(i.number) LIKE '%' || @search || '%')
                      AND
                      (
                          @state = 'All'
                          OR (@state = 'Open'
                              AND i.status IN ('Issued', 'PartiallyPaid')
                              AND i.total_amount - i.amount_paid_amount > 0)
                          OR (@state = 'Paid' AND i.status = 'Paid')
                          OR (@state = 'Draft' AND i.status = 'Draft')
                          OR (@state = 'Void' AND i.status = 'Void')
                      )
                ),
                page AS
                (
                    SELECT *
                    FROM filtered
                    WHERE NOT @has_cursor
                       OR (issue_date, created_at_utc, invoice_id)
                          < (@after_date, @after_created_at_utc, @after_id)
                    ORDER BY issue_date DESC, created_at_utc DESC, invoice_id DESC
                    LIMIT @take
                ),
                totals AS
                (
                    SELECT COUNT(*)::bigint AS filtered_count FROM filtered
                )
                SELECT
                    p.invoice_id,
                    p.contract_id,
                    p.number,
                    p.issue_date,
                    p.due_date,
                    p.status,
                    p.total_amount,
                    p.amount_paid_amount,
                    p.balance_due,
                    p.currency_code,
                    journal.journal_entry_id,
                    p.created_at_utc,
                    totals.filtered_count
                FROM totals
                LEFT JOIN page AS p ON TRUE
                LEFT JOIN LATERAL
                (
                    SELECT j.journal_entry_id
                    FROM control.journal_entries AS j
                    WHERE j.client_id = @client_id
                      AND j.source_type = 'BillingInvoice'
                      AND j.source_document_id = p.invoice_id
                    ORDER BY j.entry_date, j.created_at_utc, j.journal_entry_id
                    LIMIT 1
                ) AS journal ON p.invoice_id IS NOT NULL
                ORDER BY p.issue_date DESC, p.created_at_utc DESC, p.invoice_id DESC;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", request.ClientId.Value, DbType.Guid);
            AddParameter(command, "from_date", request.FromDate, DbType.Date);
            AddParameter(command, "to_date", request.ToDate, DbType.Date);
            AddParameter(command, "search", request.Search, DbType.String);
            AddParameter(command, "state", request.State.ToString(), DbType.String);
            AddParameter(command, "has_cursor", request.AfterInvoiceId.HasValue, DbType.Boolean);
            AddParameter(command, "after_date", request.AfterIssueDate ?? DateOnly.MinValue, DbType.Date);
            AddParameter(
                command,
                "after_created_at_utc",
                request.AfterCreatedAtUtc ?? DateTimeOffset.MinValue,
                DbType.DateTimeOffset);
            AddParameter(command, "after_id", request.AfterInvoiceId ?? Guid.Empty, DbType.Guid);
            AddParameter(command, "take", request.Take, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<ClientInvoiceReadItem>(request.Take);
            long filteredCount = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                filteredCount = reader.GetInt64(12);

                if (reader.IsDBNull(0))
                {
                    continue;
                }

                items.Add(new ClientInvoiceReadItem(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetFieldValue<DateOnly>(3),
                    reader.GetFieldValue<DateOnly>(4),
                    reader.GetString(5),
                    reader.GetDecimal(6),
                    reader.GetDecimal(7),
                    reader.GetDecimal(8),
                    reader.GetString(9),
                    reader.IsDBNull(10) ? null : reader.GetGuid(10),
                    ReadDateTimeOffset(reader, 11)));
            }

            return new ClientInvoiceReadPage(items, filteredCount);
        }, cancellationToken);
    }

    public Task<ClientPaymentReadPage> ReadPaymentPageAsync(
        ClientPaymentReadRequest request,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                WITH filtered AS MATERIALIZED
                (
                    SELECT
                        p.payment_id,
                        p.invoice_id,
                        p.reference,
                        p.method,
                        p.status,
                        p.amount,
                        p.currency_code,
                        p.received_on,
                        p.recorded_at_utc
                    FROM control.payments AS p
                    WHERE p.client_id = @client_id
                      AND (@from_date IS NULL OR p.received_on >= @from_date)
                      AND (@to_date IS NULL OR p.received_on <= @to_date)
                      AND (@status = '' OR p.status = @status)
                      AND
                      (
                          @search = ''
                          OR LOWER(p.reference) LIKE '%' || @search || '%'
                          OR LOWER(p.method) LIKE '%' || @search || '%'
                      )
                ),
                page AS
                (
                    SELECT *
                    FROM filtered
                    WHERE NOT @has_cursor
                       OR (received_on, recorded_at_utc, payment_id)
                          < (@after_date, @after_recorded_at_utc, @after_id)
                    ORDER BY received_on DESC, recorded_at_utc DESC, payment_id DESC
                    LIMIT @take
                ),
                totals AS
                (
                    SELECT COUNT(*)::bigint AS filtered_count FROM filtered
                )
                SELECT
                    p.payment_id,
                    p.invoice_id,
                    p.reference,
                    p.method,
                    p.status,
                    p.amount,
                    p.currency_code,
                    p.received_on,
                    journal.journal_entry_id,
                    p.recorded_at_utc,
                    totals.filtered_count
                FROM totals
                LEFT JOIN page AS p ON TRUE
                LEFT JOIN LATERAL
                (
                    SELECT j.journal_entry_id
                    FROM control.journal_entries AS j
                    WHERE j.client_id = @client_id
                      AND j.source_document_id = p.payment_id
                      AND j.source_type IN ('PaymentReceipt', 'PaymentReversal')
                    ORDER BY j.entry_date DESC, j.created_at_utc DESC, j.journal_entry_id DESC
                    LIMIT 1
                ) AS journal ON p.payment_id IS NOT NULL
                ORDER BY p.received_on DESC, p.recorded_at_utc DESC, p.payment_id DESC;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", request.ClientId.Value, DbType.Guid);
            AddParameter(command, "from_date", request.FromDate, DbType.Date);
            AddParameter(command, "to_date", request.ToDate, DbType.Date);
            AddParameter(command, "search", request.Search, DbType.String);
            AddParameter(command, "status", request.Status ?? string.Empty, DbType.String);
            AddParameter(command, "has_cursor", request.AfterPaymentId.HasValue, DbType.Boolean);
            AddParameter(command, "after_date", request.AfterReceivedOn ?? DateOnly.MinValue, DbType.Date);
            AddParameter(
                command,
                "after_recorded_at_utc",
                request.AfterRecordedAtUtc ?? DateTimeOffset.MinValue,
                DbType.DateTimeOffset);
            AddParameter(command, "after_id", request.AfterPaymentId ?? Guid.Empty, DbType.Guid);
            AddParameter(command, "take", request.Take, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<ClientPaymentReadItem>(request.Take);
            long filteredCount = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                filteredCount = reader.GetInt64(10);

                if (reader.IsDBNull(0))
                {
                    continue;
                }

                items.Add(new ClientPaymentReadItem(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetDecimal(5),
                    reader.GetString(6),
                    reader.GetFieldValue<DateOnly>(7),
                    reader.IsDBNull(8) ? null : reader.GetGuid(8),
                    ReadDateTimeOffset(reader, 9)));
            }

            return new ClientPaymentReadPage(items, filteredCount);
        }, cancellationToken);
    }
}
