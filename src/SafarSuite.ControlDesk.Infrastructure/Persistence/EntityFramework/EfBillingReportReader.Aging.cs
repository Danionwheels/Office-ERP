using System.Data;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed partial class EfBillingReportReader
{
    public Task<IReadOnlyCollection<AccountsReceivableAgingClientReadModel>> ReadAccountsReceivableAgingAsync(
        AccountsReceivableAgingReadRequest request,
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
                        i.due_date,
                        i.currency_code,
                        i.amount_paid_amount,
                        COALESCE(SUM(il.amount), 0)::numeric AS total_amount
                    FROM control.invoices AS i
                    LEFT JOIN control.invoice_lines AS il ON il.invoice_id = i.invoice_id
                    WHERE i.status IN ('Issued', 'PartiallyPaid')
                      AND i.currency_code = @currency_code
                      AND i.issue_date <= @as_of_date
                    GROUP BY
                        i.invoice_id,
                        i.client_id,
                        i.due_date,
                        i.currency_code,
                        i.amount_paid_amount
                ),
                open_invoices AS MATERIALIZED
                (
                    SELECT
                        invoice_id,
                        client_id,
                        due_date,
                        currency_code,
                        (total_amount - amount_paid_amount)::numeric AS balance_due
                    FROM invoice_amounts
                    WHERE total_amount - amount_paid_amount > 0
                )
                SELECT
                    c.client_id,
                    c.code,
                    c.display_name,
                    o.currency_code,
                    COALESCE(SUM(o.balance_due) FILTER (WHERE o.due_date >= @as_of_date), 0)::numeric AS current_amount,
                    COALESCE(SUM(o.balance_due) FILTER (
                        WHERE @as_of_date - o.due_date BETWEEN 1 AND 30), 0)::numeric AS days_1_to_30_amount,
                    COALESCE(SUM(o.balance_due) FILTER (
                        WHERE @as_of_date - o.due_date BETWEEN 31 AND 60), 0)::numeric AS days_31_to_60_amount,
                    COALESCE(SUM(o.balance_due) FILTER (
                        WHERE @as_of_date - o.due_date BETWEEN 61 AND 90), 0)::numeric AS days_61_to_90_amount,
                    COALESCE(SUM(o.balance_due) FILTER (
                        WHERE @as_of_date - o.due_date > 90), 0)::numeric AS days_over_90_amount,
                    COALESCE(SUM(o.balance_due), 0)::numeric AS total_outstanding,
                    COUNT(*)::bigint AS invoice_count
                FROM open_invoices AS o
                INNER JOIN control.clients AS c ON c.client_id = o.client_id
                GROUP BY c.client_id, c.code, c.display_name, o.currency_code
                ORDER BY c.display_name, c.code, c.client_id;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "currency_code", request.CurrencyCode, DbType.String);
            AddParameter(command, "as_of_date", request.AsOfDate, DbType.Date);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var clients = new List<AccountsReceivableAgingClientReadModel>();

            while (await reader.ReadAsync(cancellationToken))
            {
                clients.Add(new AccountsReceivableAgingClientReadModel(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDecimal(4),
                    reader.GetDecimal(5),
                    reader.GetDecimal(6),
                    reader.GetDecimal(7),
                    reader.GetDecimal(8),
                    reader.GetDecimal(9),
                    reader.GetInt64(10)));
            }

            return (IReadOnlyCollection<AccountsReceivableAgingClientReadModel>)clients;
        }, cancellationToken);
    }
}
