using Microsoft.EntityFrameworkCore.Storage;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ControlDeskDbContext _dbContext;
    private readonly EfClientWorkQueueProjector _clientWorkQueue;

    public EfUnitOfWork(
        ControlDeskDbContext dbContext,
        EfClientWorkQueueProjector clientWorkQueue)
    {
        _dbContext = dbContext;
        _clientWorkQueue = clientWorkQueue;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var affectedClientIds = CaptureAffectedClientIds();

        if (affectedClientIds.Length == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            await SaveAndRefreshClientWorkAsync(affectedClientIds, cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await SaveAndRefreshClientWorkAsync(affectedClientIds, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync<object?>(
            async token =>
            {
                await operation(token);
                return null;
            },
            cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            var nestedResult = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);

            return nestedResult;
        }

        await using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task SaveAndRefreshClientWorkAsync(
        IReadOnlyCollection<Guid> affectedClientIds,
        CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _clientWorkQueue.RefreshAsync(affectedClientIds, cancellationToken);
    }

    private Guid[] CaptureAffectedClientIds()
    {
        return _dbContext.ChangeTracker
            .Entries()
            .Select(entry => entry.Entity switch
            {
                Client client => (Guid?)client.Id.Value,
                ClientDeployment deployment => deployment.ClientId.Value,
                Invoice invoice => invoice.ClientId.Value,
                EntitlementSnapshot entitlement => entitlement.ClientId.Value,
                CloudOutboxMessage message => message.ClientId?.Value,
                _ => null
            })
            .Where(clientId => clientId.HasValue && clientId.Value != Guid.Empty)
            .Select(clientId => clientId!.Value)
            .Distinct()
            .ToArray();
    }
}
