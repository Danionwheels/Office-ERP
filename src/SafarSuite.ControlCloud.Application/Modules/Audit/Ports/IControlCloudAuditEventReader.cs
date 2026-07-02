using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.Audit.Ports;

public interface IControlCloudAuditEventReader
{
    Task<IReadOnlyList<ClientPortalAuditRecord>> ListAsync(
        Guid? clientId,
        string? eventType,
        int take,
        CancellationToken cancellationToken = default);
}
