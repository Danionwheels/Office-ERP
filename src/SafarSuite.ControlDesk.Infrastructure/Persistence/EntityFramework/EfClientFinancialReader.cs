using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfClientFinancialReader : IClientFinancialReader
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientFinancialReader(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ClientExistsAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Clients.AnyAsync(client => client.Id == clientId, cancellationToken);
    }

    public Task<ClientFinancialSummaryReadModel> ReadSummaryAsync(
        ClientId clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                WITH invoice_amounts AS MATERIALIZED
                (
                    SELECT
                        i.invoice_id,
                        i.currency_code,
                        i.status,
                        i.amount_paid_amount,
                        COALESCE(SUM(il.amount), 0)::numeric AS total_amount
                    FROM control.invoices AS i
                    LEFT JOIN control.invoice_lines AS il ON il.invoice_id = i.invoice_id
                    WHERE i.client_id = @client_id
                      AND (@from_date IS NULL OR i.issue_date >= @from_date)
                      AND (@to_date IS NULL OR i.issue_date <= @to_date)
                    GROUP BY
                        i.invoice_id,
                        i.currency_code,
                        i.status,
                        i.amount_paid_amount
                ),
                invoice_summary AS
                (
                    SELECT
                        currency_code,
                        COALESCE(SUM(total_amount) FILTER (
                            WHERE status IN ('Issued', 'PartiallyPaid', 'Paid')), 0)::numeric AS total_invoiced,
                        COALESCE(SUM(total_amount - amount_paid_amount) FILTER (
                            WHERE status IN ('Issued', 'PartiallyPaid', 'Paid')), 0)::numeric AS invoice_balance,
                        COUNT(*) FILTER (
                            WHERE status IN ('Issued', 'PartiallyPaid', 'Paid'))::bigint AS invoice_count,
                        COUNT(*) FILTER (
                            WHERE status IN ('Issued', 'PartiallyPaid', 'Paid')
                              AND total_amount - amount_paid_amount > 0)::bigint AS open_invoice_count
                    FROM invoice_amounts
                    GROUP BY currency_code
                ),
                payment_summary AS
                (
                    SELECT
                        p.currency_code,
                        COALESCE(SUM(p.amount) FILTER (WHERE p.status = 'Approved'), 0)::numeric AS total_paid
                    FROM control.payments AS p
                    WHERE p.client_id = @client_id
                      AND (@from_date IS NULL OR p.received_on >= @from_date)
                      AND (@to_date IS NULL OR p.received_on <= @to_date)
                    GROUP BY p.currency_code
                ),
                credit_summary AS
                (
                    SELECT
                        n.currency_code,
                        COALESCE(SUM(n.total_amount), 0)::numeric AS credit_amount
                    FROM control.credit_notes AS n
                    WHERE n.client_id = @client_id
                      AND (@from_date IS NULL OR n.credit_date >= @from_date)
                      AND (@to_date IS NULL OR n.credit_date <= @to_date)
                    GROUP BY n.currency_code
                ),
                refund_summary AS
                (
                    SELECT
                        r.currency_code,
                        COALESCE(SUM(r.amount) FILTER (WHERE r.status = 'Issued'), 0)::numeric AS refund_amount
                    FROM control.client_refunds AS r
                    WHERE r.client_id = @client_id
                      AND (@from_date IS NULL OR r.refunded_on >= @from_date)
                      AND (@to_date IS NULL OR r.refunded_on <= @to_date)
                    GROUP BY r.currency_code
                ),
                application_summary AS
                (
                    SELECT
                        a.currency_code,
                        COALESCE(SUM(a.amount) FILTER (WHERE a.status = 'Applied'), 0)::numeric AS application_amount
                    FROM control.client_credit_applications AS a
                    WHERE a.client_id = @client_id
                      AND (@from_date IS NULL OR a.applied_on >= @from_date)
                      AND (@to_date IS NULL OR a.applied_on <= @to_date)
                    GROUP BY a.currency_code
                ),
                currencies AS
                (
                    SELECT currency_code FROM invoice_amounts
                    WHERE status IN ('Issued', 'PartiallyPaid', 'Paid', 'Void')
                    UNION
                    SELECT p.currency_code FROM control.payments AS p
                    WHERE p.client_id = @client_id
                      AND (@from_date IS NULL OR p.received_on >= @from_date)
                      AND (@to_date IS NULL OR p.received_on <= @to_date)
                    UNION
                    SELECT n.currency_code FROM control.credit_notes AS n
                    WHERE n.client_id = @client_id
                      AND (@from_date IS NULL OR n.credit_date >= @from_date)
                      AND (@to_date IS NULL OR n.credit_date <= @to_date)
                    UNION
                    SELECT r.currency_code FROM control.client_refunds AS r
                    WHERE r.client_id = @client_id
                      AND (@from_date IS NULL OR r.refunded_on >= @from_date)
                      AND (@to_date IS NULL OR r.refunded_on <= @to_date)
                    UNION
                    SELECT a.currency_code FROM control.client_credit_applications AS a
                    WHERE a.client_id = @client_id
                      AND (@from_date IS NULL OR a.applied_on >= @from_date)
                      AND (@to_date IS NULL OR a.applied_on <= @to_date)
                )
                SELECT
                    c.currency_code,
                    COALESCE(i.total_invoiced, 0)::numeric AS total_invoiced,
                    COALESCE(p.total_paid, 0)::numeric AS total_paid,
                    GREATEST(
                        COALESCE(n.credit_amount, 0)
                            - COALESCE(r.refund_amount, 0)
                            - COALESCE(a.application_amount, 0),
                        0)::numeric AS available_credit,
                    (COALESCE(i.invoice_balance, 0)
                        - COALESCE(n.credit_amount, 0)
                        + COALESCE(a.application_amount, 0)
                        + COALESCE(r.refund_amount, 0))::numeric AS balance_due,
                    COALESCE(i.invoice_count, 0)::bigint AS invoice_count,
                    COALESCE(i.open_invoice_count, 0)::bigint AS open_invoice_count
                FROM currencies AS c
                LEFT JOIN invoice_summary AS i USING (currency_code)
                LEFT JOIN payment_summary AS p USING (currency_code)
                LEFT JOIN credit_summary AS n USING (currency_code)
                LEFT JOIN refund_summary AS r USING (currency_code)
                LEFT JOIN application_summary AS a USING (currency_code)
                ORDER BY c.currency_code;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", clientId.Value, DbType.Guid);
            AddParameter(command, "from_date", fromDate, DbType.Date);
            AddParameter(command, "to_date", toDate, DbType.Date);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var currencies = new List<ClientFinancialCurrencySummaryReadModel>();

            while (await reader.ReadAsync(cancellationToken))
            {
                currencies.Add(new ClientFinancialCurrencySummaryReadModel(
                    reader.GetString(0),
                    reader.GetDecimal(1),
                    reader.GetDecimal(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    reader.GetInt64(5),
                    reader.GetInt64(6)));
            }

            return new ClientFinancialSummaryReadModel(currencies);
        }, cancellationToken);
    }

    public Task<ClientCreditBalanceReadModel> ReadCreditBalanceAsync(
        ClientId clientId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(async connection =>
        {
            const string commandText = """
                SELECT
                    @currency_code::text,
                    COALESCE((
                        SELECT SUM(amounts.total_amount - amounts.amount_paid_amount)
                        FROM
                        (
                            SELECT
                                i.invoice_id,
                                i.amount_paid_amount,
                                COALESCE(SUM(il.amount), 0)::numeric AS total_amount
                            FROM control.invoices AS i
                            LEFT JOIN control.invoice_lines AS il ON il.invoice_id = i.invoice_id
                            WHERE i.client_id = @client_id
                              AND i.currency_code = @currency_code
                              AND i.status IN ('Issued', 'PartiallyPaid', 'Paid')
                            GROUP BY i.invoice_id, i.amount_paid_amount
                        ) AS amounts
                    ), 0)::numeric AS invoice_balance,
                    COALESCE((
                        SELECT SUM(n.total_amount)
                        FROM control.credit_notes AS n
                        WHERE n.client_id = @client_id
                          AND n.currency_code = @currency_code
                    ), 0)::numeric AS credit_note_amount,
                    COALESCE((
                        SELECT SUM(r.amount)
                        FROM control.client_refunds AS r
                        WHERE r.client_id = @client_id
                          AND r.currency_code = @currency_code
                          AND r.status = 'Issued'
                    ), 0)::numeric AS refund_amount,
                    COALESCE((
                        SELECT SUM(a.amount)
                        FROM control.client_credit_applications AS a
                        WHERE a.client_id = @client_id
                          AND a.currency_code = @currency_code
                          AND a.status = 'Applied'
                    ), 0)::numeric AS applied_credit_amount;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "client_id", clientId.Value, DbType.Guid);
            AddParameter(command, "currency_code", currencyCode.Trim().ToUpperInvariant(), DbType.String);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            return new ClientCreditBalanceReadModel(
                reader.GetString(0),
                reader.GetDecimal(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4));
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
            DateTime dateTime => new DateTimeOffset(
                DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => reader.GetFieldValue<DateTimeOffset>(ordinal)
        };
    }
}
