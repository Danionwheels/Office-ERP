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

    public async Task<ControlCloudClientCommercialProjection?> HandleAsync(
        GetClientPortalCommercialSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var projection = await _projections.GetByClientIdAsync(query.ClientId, cancellationToken);

        if (projection is null)
        {
            return null;
        }

        projection.Invoices.Clear();
        projection.Payments.Clear();
        projection.CreditNotes.Clear();
        projection.Refunds.Clear();
        projection.CreditApplications.Clear();

        return projection;
    }
}
