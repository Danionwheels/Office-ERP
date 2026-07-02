using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class GetClientPortalCommercialSummaryHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;

    public GetClientPortalCommercialSummaryHandler(
        IControlCloudClientCommercialProjectionRepository projections)
    {
        _projections = projections;
    }

    public Task<ControlCloudClientCommercialProjection?> HandleAsync(
        GetClientPortalCommercialSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        return _projections.GetByClientIdAsync(query.ClientId, cancellationToken);
    }
}
