namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxPublicationLeaseProvider
{
    Task<ICloudOutboxPublicationLease?> TryAcquireAsync(
        CancellationToken cancellationToken = default);
}

public interface ICloudOutboxPublicationLease : IAsyncDisposable;
