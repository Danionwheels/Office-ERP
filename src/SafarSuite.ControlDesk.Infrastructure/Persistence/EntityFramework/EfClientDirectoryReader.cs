using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientDirectoryReader : IClientDirectoryReader
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientDirectoryReader(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClientDirectoryReadPage> ReadPageAsync(
        ClientDirectoryReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var sortExpression = request.Sort switch
        {
            ClientDirectorySort.DisplayName => "c.display_name",
            ClientDirectorySort.LegalName => "c.legal_name",
            ClientDirectorySort.Status => "c.status",
            _ => "c.code"
        };
        var direction = request.Direction == ClientDirectorySortDirection.Descending
            ? "DESC"
            : "ASC";
        var cursorOperator = request.Direction == ClientDirectorySortDirection.Descending
            ? "<"
            : ">";
        var commandText = $$"""
            WITH filtered AS MATERIALIZED
            (
                SELECT
                    c.client_id,
                    c.code,
                    c.legal_name,
                    c.display_name,
                    c.status,
                    {{sortExpression}} AS sort_value,
                    c.code AS code_sort
                FROM control.clients AS c
                WHERE (@search = '' OR c.search_text LIKE '%' || @search || '%')
                  AND (@status = '' OR c.status = @status)
            ),
            page AS
            (
                SELECT *
                FROM filtered
                WHERE NOT @has_cursor
                   OR (sort_value, code_sort, client_id)
                      {{cursorOperator}} (@after_sort_value, @after_code, @after_client_id)
                ORDER BY sort_value {{direction}}, code_sort {{direction}}, client_id {{direction}}
                LIMIT @take
            ),
            register_summary AS
            (
                SELECT
                    COUNT(*) AS total_count,
                    COUNT(*) FILTER (WHERE status = 'Draft') AS draft_count,
                    COUNT(*) FILTER (WHERE status = 'Active') AS active_count,
                    COUNT(*) FILTER (WHERE status = 'Suspended') AS suspended_count,
                    COUNT(*) FILTER (WHERE status = 'Archived') AS archived_count
                FROM control.clients
            )
            SELECT
                p.client_id,
                p.code,
                p.legal_name,
                p.display_name,
                p.status,
                p.sort_value,
                (SELECT COUNT(*) FROM filtered) AS filtered_count,
                s.total_count,
                s.draft_count,
                s.active_count,
                s.suspended_count,
                s.archived_count
            FROM register_summary AS s
            LEFT JOIN page AS p ON TRUE
            ORDER BY p.sort_value {{direction}}, p.code_sort {{direction}}, p.client_id {{direction}};
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
            AddParameter(command, "status", request.Status ?? string.Empty);
            AddParameter(command, "has_cursor", request.AfterClientId.HasValue);
            AddParameter(command, "after_sort_value", request.AfterSortValue ?? string.Empty);
            AddParameter(command, "after_code", request.AfterCode ?? string.Empty);
            AddParameter(command, "after_client_id", request.AfterClientId ?? Guid.Empty);
            AddParameter(command, "take", request.Take);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<ClientDirectoryReadItem>(request.Take);
            long filteredCount = 0;
            var summary = new ClientDirectoryReadSummary(0, 0, 0, 0, 0);

            while (await reader.ReadAsync(cancellationToken))
            {
                filteredCount = reader.GetInt64(6);
                summary = new ClientDirectoryReadSummary(
                    reader.GetInt64(7),
                    reader.GetInt64(8),
                    reader.GetInt64(9),
                    reader.GetInt64(10),
                    reader.GetInt64(11));

                if (reader.IsDBNull(0))
                {
                    continue;
                }

                items.Add(new ClientDirectoryReadItem(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)));
            }

            return new ClientDirectoryReadPage(items, filteredCount, summary);
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
