using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;

public sealed class GetLatestEntitlementSnapshotHandler
{
    private readonly IEntitlementSnapshotRepository _entitlementSnapshots;
    private readonly IClientAccessRevisionRepository _clientAccessRevisions;

    public GetLatestEntitlementSnapshotHandler(
        IEntitlementSnapshotRepository entitlementSnapshots,
        IClientAccessRevisionRepository clientAccessRevisions)
    {
        _entitlementSnapshots = entitlementSnapshots;
        _clientAccessRevisions = clientAccessRevisions;
    }

    public async Task<Result<GetLatestEntitlementSnapshotResult>> HandleAsync(
        GetLatestEntitlementSnapshotQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);
            var snapshot = await _entitlementSnapshots.GetLatestForClientAsync(clientId, cancellationToken);

            if (snapshot is null)
            {
                return Result<GetLatestEntitlementSnapshotResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Entitlement snapshot was not found for this client."));
            }

            var revision = await _clientAccessRevisions.GetByIdAsync(
                snapshot.ClientAccessRevisionId,
                cancellationToken);

            if (revision is null)
            {
                return Result<GetLatestEntitlementSnapshotResult>.Failure(ApplicationError.NotFound(
                    nameof(snapshot.ClientAccessRevisionId),
                    "The approved client access revision for this entitlement snapshot was not found."));
            }

            return Result<GetLatestEntitlementSnapshotResult>.Success(ToResult(snapshot, revision));
        }
        catch (ArgumentException exception)
        {
            return Result<GetLatestEntitlementSnapshotResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }

    private static GetLatestEntitlementSnapshotResult ToResult(
        EntitlementSnapshot snapshot,
        ClientAccessRevision revision)
    {
        return new GetLatestEntitlementSnapshotResult(
            snapshot.Id.Value,
            snapshot.ClientId.Value,
            snapshot.ContractId.Value,
            revision.ContractRevisionNumber,
            revision.ProductCatalogRevisionId.Value,
            revision.ProductCatalogRevisionNumber,
            revision.Id.Value,
            snapshot.EntitlementVersion,
            snapshot.Status.ToString(),
            snapshot.PaidUntil,
            snapshot.GraceUntil,
            snapshot.OfflineValidUntil,
            snapshot.AllowedDevices,
            snapshot.AllowedBranches,
            snapshot.IssuedAtUtc,
            snapshot.EffectiveFromUtc,
            revision.SupersedesRevisionId?.Value,
            revision.ApprovedBy,
            revision.ApprovalReason,
            revision.ApprovedAtUtc,
            snapshot.Modules.Select(module => new GetLatestEntitlementSnapshotModuleResult(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray(),
            snapshot.AllowedNamedUsers,
            snapshot.AllowedConcurrentUsers,
            snapshot.FeatureLimits.Select(limit => new GetLatestEntitlementSnapshotFeatureLimitResult(
                limit.ModuleCode.Value,
                limit.FeatureCode.Value,
                limit.LimitValue,
                limit.Unit)).ToArray());
    }
}
