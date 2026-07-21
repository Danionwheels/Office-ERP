using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfRevenueSummaryReader : IRevenueSummaryReader
{
    private readonly ControlDeskDbContext _dbContext;

    public EfRevenueSummaryReader(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RevenueSummaryPeriodReadModel>> ReadAsync(
        RevenueSummaryReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            const string commandText = """
                SELECT
                    CASE
                        WHEN @period = 'Quarterly'
                            THEN date_trunc('quarter', j.entry_date::timestamp)::date
                        ELSE date_trunc('month', j.entry_date::timestamp)::date
                    END AS period_start,
                    COALESCE(SUM(l.debit_amount), 0)::numeric AS debit,
                    COALESCE(SUM(l.credit_amount), 0)::numeric AS credit,
                    COUNT(DISTINCT j.journal_entry_id)::bigint AS activity_count
                FROM control.journal_entries AS j
                INNER JOIN control.journal_lines AS l
                    ON l.journal_entry_id = j.journal_entry_id
                INNER JOIN control.ledger_accounts AS a
                    ON a.ledger_account_id = l.ledger_account_id
                WHERE j.entry_date >= @from_date
                  AND j.entry_date <= @to_date
                  AND j.currency_code = @currency_code
                  AND j.status <> 'Draft'
                  AND j.source_type NOT IN ('PeriodClose', 'PeriodCloseReversal')
                  AND a.type = 'Revenue'
                GROUP BY 1
                ORDER BY 1;
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "from_date", request.FromDate, DbType.Date);
            AddParameter(command, "to_date", request.ToDate, DbType.Date);
            AddParameter(command, "period", request.Period, DbType.String);
            AddParameter(command, "currency_code", request.CurrencyCode, DbType.String);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var periods = new List<RevenueSummaryPeriodReadModel>();

            while (await reader.ReadAsync(cancellationToken))
            {
                periods.Add(new RevenueSummaryPeriodReadModel(
                    reader.GetFieldValue<DateOnly>(0),
                    reader.GetDecimal(1),
                    reader.GetDecimal(2),
                    checked((int)reader.GetInt64(3))));
            }

            return periods;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value, DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
