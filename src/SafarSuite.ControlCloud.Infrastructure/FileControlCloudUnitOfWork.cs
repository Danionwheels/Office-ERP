using SafarSuite.ControlCloud.Application.Common;

namespace SafarSuite.ControlCloud.Infrastructure;

public sealed class FileControlCloudUnitOfWork : IControlCloudUnitOfWork
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
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        return await operation(cancellationToken);
    }
}
