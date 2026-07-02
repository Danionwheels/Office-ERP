using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.Ports;

public interface ILocalServerEntitlementImportAuditStore
{
    Task AppendAsync(
        LocalServerEntitlementImportAuditRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LocalServerEntitlementImportAuditRecord>> GetRecentAsync(
        string installationId,
        int take,
        CancellationToken cancellationToken = default);
}
