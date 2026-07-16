namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;

public interface IEntitlementVersionAllocator
{
    Task<long> AllocateNextAsync(CancellationToken cancellationToken = default);
}
