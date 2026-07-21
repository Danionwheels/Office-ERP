using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfPaymentReportReader : IPaymentReportReader
{
    private readonly ControlDeskDbContext _dbContext;

    public EfPaymentReportReader(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PaymentReceiptReportReadPage> ReadReceiptsPageAsync(
        PaymentReceiptReportReadRequest request,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                WITH filtered AS MATERIALIZED
                (
                    SELECT
                        p.payment_id,
                        p.client_id,
                        c.code AS client_code,
                        c.display_name AS client_name,
                        p.invoice_id,
                        i.number AS invoice_number,
                        p.reference,
                        p.method,
                        p.status,
                        p.amount,
                        p.currency_code,
                        p.received_on,
                        p.recorded_at_utc
                    FROM control.payments AS p
                    INNER JOIN control.clients AS c ON c.client_id = p.client_id
                    INNER JOIN control.invoices AS i ON i.invoice_id = p.invoice_id
                    WHERE (@client_id IS NULL OR p.client_id = @client_id)
                      AND (@from_date IS NULL OR p.received_on >= @from_date)
                      AND (@to_date IS NULL OR p.received_on <= @to_date)
                      AND (@method = '' OR p.method = @method)
                      AND (@status = '' OR p.status = @status)
                      AND (@currency_code = '' OR p.currency_code = @currency_code)
                ),
                page AS
                (
                    SELECT *
                    FROM filtered
                    WHERE NOT @has_cursor
                       OR (received_on, recorded_at_utc, payment_id)
                          < (@after_received_on, @after_recorded_at_utc, @after_payment_id)
                    ORDER BY received_on DESC, recorded_at_utc DESC, payment_id DESC
                    LIMIT @take
                ),
                totals AS
                (
                    SELECT COUNT(*)::bigint AS filtered_count FROM filtered
                )
                SELECT
                    p.payment_id,
                    p.client_id,
                    p.client_code,
                    p.client_name,
                    p.invoice_id,
                    p.invoice_number,
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
                    WHERE j.client_id = p.client_id
                      AND j.source_document_id = p.payment_id
                      AND j.source_type IN ('PaymentReceipt', 'PaymentReversal')
                    ORDER BY j.entry_date DESC, j.created_at_utc DESC, j.journal_entry_id DESC
                    LIMIT 1
                ) AS journal ON p.payment_id IS NOT NULL
                ORDER BY p.received_on DESC, p.recorded_at_utc DESC, p.payment_id DESC;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", request.ClientId, DbType.Guid);
            AddParameter(command, "from_date", request.FromDate, DbType.Date);
            AddParameter(command, "to_date", request.ToDate, DbType.Date);
            AddParameter(command, "method", request.Method ?? string.Empty, DbType.String);
            AddParameter(command, "status", request.Status ?? string.Empty, DbType.String);
            AddParameter(command, "currency_code", request.CurrencyCode ?? string.Empty, DbType.String);
            AddParameter(command, "has_cursor", request.AfterPaymentId.HasValue, DbType.Boolean);
            AddParameter(
                command,
                "after_received_on",
                request.AfterReceivedOn ?? DateOnly.MinValue,
                DbType.Date);
            AddParameter(
                command,
                "after_recorded_at_utc",
                request.AfterRecordedAtUtc ?? DateTimeOffset.MinValue,
                DbType.DateTimeOffset);
            AddParameter(
                command,
                "after_payment_id",
                request.AfterPaymentId ?? Guid.Empty,
                DbType.Guid);
            AddParameter(command, "take", request.Take, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<PaymentReceiptReportReadItem>(request.Take);
            long filteredCount = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                filteredCount = reader.GetInt64(14);

                if (reader.IsDBNull(0))
                {
                    continue;
                }

                items.Add(new PaymentReceiptReportReadItem(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetGuid(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetString(7),
                    reader.GetString(8),
                    reader.GetDecimal(9),
                    reader.GetString(10),
                    reader.GetFieldValue<DateOnly>(11),
                    reader.IsDBNull(12) ? null : reader.GetGuid(12),
                    ReadDateTimeOffset(reader, 13)));
            }

            return new PaymentReceiptReportReadPage(items, filteredCount);
        }, cancellationToken);
    }

    private async Task<T> WithConnectionAsync<T>(
        Func<DbConnection, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            return await operation(connection);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object? value,
        DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static DateTimeOffset ReadDateTimeOffset(DbDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);

        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => reader.GetFieldValue<DateTimeOffset>(ordinal)
        };
    }
}
