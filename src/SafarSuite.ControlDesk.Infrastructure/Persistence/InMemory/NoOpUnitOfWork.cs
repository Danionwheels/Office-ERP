using SafarSuite.ControlDesk.Application.Common.Abstractions;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await operation(cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var result = await operation(cancellationToken);
        await SaveChangesAsync(cancellationToken);

        return result;
    }
}
