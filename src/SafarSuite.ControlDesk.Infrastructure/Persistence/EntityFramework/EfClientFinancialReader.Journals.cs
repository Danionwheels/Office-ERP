using System.Data;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfClientFinancialReader
{
    public Task<ClientJournalPostingReadPage> ReadJournalPageAsync(
        ClientJournalPostingReadRequest request,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                WITH filtered AS MATERIALIZED
                (
                    SELECT
                        j.journal_entry_id,
                        j.entry_date,
                        j.source_type,
                        j.source_reference,
                        j.memo,
                        j.status,
                        j.currency_code,
                        j.created_at_utc
                    FROM control.journal_entries AS j
                    WHERE j.client_id = @client_id
                      AND (@from_date IS NULL OR j.entry_date >= @from_date)
                      AND (@to_date IS NULL OR j.entry_date <= @to_date)
                      AND (@source_type = '' OR j.source_type = @source_type)
                      AND
                      (
                          @search = ''
                          OR LOWER(COALESCE(j.source_reference, '')) LIKE '%' || @search || '%'
                          OR LOWER(COALESCE(j.memo, '')) LIKE '%' || @search || '%'
                          OR LOWER(j.source_type) LIKE '%' || @search || '%'
                      )
                ),
                page AS
                (
                    SELECT *
                    FROM filtered
                    WHERE NOT @has_cursor
                       OR (entry_date, created_at_utc, journal_entry_id)
                          < (@after_date, @after_created_at_utc, @after_id)
                    ORDER BY entry_date DESC, created_at_utc DESC, journal_entry_id DESC
                    LIMIT @take
                ),
                totals AS
                (
                    SELECT COUNT(*)::bigint AS filtered_count FROM filtered
                )
                SELECT
                    p.journal_entry_id,
                    p.entry_date,
                    p.source_type,
                    p.source_reference,
                    p.memo,
                    p.status,
                    lines.total_debit,
                    lines.total_credit,
                    p.currency_code,
                    lines.line_count,
                    p.created_at_utc,
                    totals.filtered_count
                FROM totals
                LEFT JOIN page AS p ON TRUE
                LEFT JOIN LATERAL
                (
                    SELECT
                        COALESCE(SUM(l.debit_amount), 0)::numeric AS total_debit,
                        COALESCE(SUM(l.credit_amount), 0)::numeric AS total_credit,
                        COUNT(l.journal_line_row_id)::integer AS line_count
                    FROM control.journal_lines AS l
                    WHERE l.journal_entry_id = p.journal_entry_id
                ) AS lines ON p.journal_entry_id IS NOT NULL
                ORDER BY p.entry_date DESC, p.created_at_utc DESC, p.journal_entry_id DESC;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", request.ClientId.Value, DbType.Guid);
            AddParameter(command, "from_date", request.FromDate, DbType.Date);
            AddParameter(command, "to_date", request.ToDate, DbType.Date);
            AddParameter(command, "search", request.Search, DbType.String);
            AddParameter(command, "source_type", request.SourceType ?? string.Empty, DbType.String);
            AddParameter(command, "has_cursor", request.AfterJournalEntryId.HasValue, DbType.Boolean);
            AddParameter(command, "after_date", request.AfterEntryDate ?? DateOnly.MinValue, DbType.Date);
            AddParameter(
                command,
                "after_created_at_utc",
                request.AfterCreatedAtUtc ?? DateTimeOffset.MinValue,
                DbType.DateTimeOffset);
            AddParameter(command, "after_id", request.AfterJournalEntryId ?? Guid.Empty, DbType.Guid);
            AddParameter(command, "take", request.Take, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<ClientJournalPostingReadItem>(request.Take);
            long filteredCount = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                filteredCount = reader.GetInt64(11);

                if (reader.IsDBNull(0))
                {
                    continue;
                }

                items.Add(new ClientJournalPostingReadItem(
                    reader.GetGuid(0),
                    reader.GetFieldValue<DateOnly>(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.GetString(5),
                    reader.GetDecimal(6),
                    reader.GetDecimal(7),
                    reader.GetString(8),
                    reader.GetInt32(9),
                    ReadDateTimeOffset(reader, 10)));
            }

            return new ClientJournalPostingReadPage(items, filteredCount);
        }, cancellationToken);
    }
}
