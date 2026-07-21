using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfEntitlementVersionAllocator : IEntitlementVersionAllocator
{
    private readonly ControlDeskDbContext _dbContext;

    public EfEntitlementVersionAllocator(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<long> AllocateNextAsync(CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;

        if (shouldCloseConnection)
        {
            await _dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT nextval('control.entitlement_version_sequence')";

            if (_dbContext.Database.CurrentTransaction is { } transaction)
            {
                command.Transaction = transaction.GetDbTransaction();
            }

            var value = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await _dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
