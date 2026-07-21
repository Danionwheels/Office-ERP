using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryEntitlementVersionAllocator : IEntitlementVersionAllocator
{
    private long _latestVersion;

    public Task<long> AllocateNextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Interlocked.Increment(ref _latestVersion));
    }
}
