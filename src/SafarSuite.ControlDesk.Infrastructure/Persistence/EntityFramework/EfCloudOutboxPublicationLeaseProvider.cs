using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfCloudOutboxPublicationLeaseProvider(ControlDeskDbContext dbContext)
    : ICloudOutboxPublicationLeaseProvider
{
    private const long PublicationLockKey = 0x53414641524F5554L;

    public async Task<ICloudOutboxPublicationLease?> TryAcquireAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            var acquired = await ExecuteLockCommandAsync(
                connection,
                "SELECT pg_try_advisory_lock(@lock_key);",
                cancellationToken);

            if (!acquired)
            {
                await dbContext.Database.CloseConnectionAsync();
                return null;
            }

            return new Lease(dbContext, connection);
        }
        catch
        {
            await dbContext.Database.CloseConnectionAsync();
            throw;
        }
    }

    private static async Task<bool> ExecuteLockCommandAsync(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.DbType = DbType.Int64;
        parameter.Value = PublicationLockKey;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private sealed class Lease(
        ControlDeskDbContext dbContext,
        DbConnection connection) : ICloudOutboxPublicationLease
    {
        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return;
            }

            try
            {
                if (connection.State == ConnectionState.Open)
                {
                    await ExecuteLockCommandAsync(
                        connection,
                        "SELECT pg_advisory_unlock(@lock_key);",
                        CancellationToken.None);
                }
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
