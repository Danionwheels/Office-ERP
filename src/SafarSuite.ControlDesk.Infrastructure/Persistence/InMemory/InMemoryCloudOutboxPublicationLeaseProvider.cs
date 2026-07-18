using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryCloudOutboxPublicationLeaseProvider
    : ICloudOutboxPublicationLeaseProvider, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<ICloudOutboxPublicationLease?> TryAcquireAsync(
        CancellationToken cancellationToken = default)
    {
        return await _gate.WaitAsync(0, cancellationToken)
            ? new Lease(_gate)
            : null;
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private sealed class Lease(SemaphoreSlim gate) : ICloudOutboxPublicationLease
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
