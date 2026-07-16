using Microsoft.EntityFrameworkCore;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientWorkQueueProjector
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientWorkQueueProjector(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RefreshAsync(
        IEnumerable<Guid> clientIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var clientId in clientIds.Distinct())
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT control.refresh_client_work_queue({clientId})",
                cancellationToken);
        }
    }
}
