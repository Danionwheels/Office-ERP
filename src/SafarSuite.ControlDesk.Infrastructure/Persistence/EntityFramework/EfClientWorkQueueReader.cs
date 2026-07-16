using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.CommandCenter.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientWorkQueueReader : IClientWorkQueueReader
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientWorkQueueReader(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClientWorkQueueReadPage> ReadPageAsync(
        ClientWorkQueueReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var (sortValueExpression, cursorPredicate, orderBy) = request.Sort switch
        {
            ClientWorkQueueSort.Client => (
                "queue.client_sort",
                "(queue.client_sort, queue.priority, queue.code, queue.client_id) "
                    + "> (@after_sort_value, @after_priority, @after_code, @after_client_id)",
                "queue.client_sort, queue.priority, queue.code, queue.client_id"),
            ClientWorkQueueSort.Action => (
                "queue.action_sort",
                "(queue.action_sort, queue.priority, queue.code, queue.client_id) "
                    + "> (@after_sort_value, @after_priority, @after_code, @after_client_id)",
                "queue.action_sort, queue.priority, queue.code, queue.client_id"),
            _ => (
                "lpad(queue.priority::text, 2, '0')",
                "(queue.priority, queue.code, queue.client_id) "
                    + "> (@after_priority, @after_code, @after_client_id)",
                "queue.priority, queue.code, queue.client_id")
        };
        var commandText = $$"""
            SELECT
                queue.client_id,
                queue.code,
                queue.name,
                queue.status,
                queue.action_label,
                queue.detail,
                queue.tab,
                queue.tone,
                queue.priority,
                {{sortValueExpression}} AS sort_value
            FROM control.client_work_queue_items AS queue
            WHERE (@search = '' OR queue.search_text LIKE '%' || @search || '%')
              AND (@lane = 'all' OR queue.tab = @lane)
              AND (NOT @has_cursor OR {{cursorPredicate}})
            ORDER BY {{orderBy}}
            LIMIT @take;

            SELECT
                COUNT(*) AS total_count,
                COUNT(*) FILTER (WHERE tab = 'setup') AS setup_count,
                COUNT(*) FILTER (WHERE tab = 'billing') AS billing_count,
                COUNT(*) FILTER (WHERE tab = 'payments') AS payments_count,
                COUNT(*) FILTER (WHERE tab = 'access') AS access_count,
                COUNT(*) FILTER (WHERE tab = 'cloud') AS cloud_count,
                COUNT(*) FILTER (WHERE tab = 'overview') AS overview_count
            FROM control.client_work_queue_items
            WHERE @search = '' OR search_text LIKE '%' || @search || '%';
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameter(command, "search", request.Search.ToLowerInvariant());
            AddParameter(command, "lane", request.Lane.ToString().ToLowerInvariant());
            AddParameter(command, "has_cursor", request.AfterClientId.HasValue);
            AddParameter(command, "after_sort_value", request.AfterSortValue ?? string.Empty);
            AddParameter(command, "after_priority", request.AfterPriority ?? 0);
            AddParameter(command, "after_code", request.AfterCode ?? string.Empty);
            AddParameter(command, "after_client_id", request.AfterClientId ?? Guid.Empty);
            AddParameter(command, "take", request.Take);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<ClientWorkQueueReadItem>(request.Take);

            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new ClientWorkQueueReadItem(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetString(7),
                    reader.GetInt32(8),
                    reader.GetString(9)));
            }

            if (!await reader.NextResultAsync(cancellationToken)
                || !await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Client work queue summary was not returned.");
            }

            var summary = new ClientWorkQueueReadSummary(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetInt64(3),
                reader.GetInt64(4),
                reader.GetInt64(5),
                reader.GetInt64(6));
            var filteredCount = request.Lane switch
            {
                ClientWorkQueueLane.Setup => summary.SetupCount,
                ClientWorkQueueLane.Billing => summary.BillingCount,
                ClientWorkQueueLane.Payments => summary.PaymentsCount,
                ClientWorkQueueLane.Access => summary.AccessCount,
                ClientWorkQueueLane.Cloud => summary.CloudCount,
                ClientWorkQueueLane.Overview => summary.OverviewCount,
                _ => summary.TotalCount
            };

            return new ClientWorkQueueReadPage(items, filteredCount, summary);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
