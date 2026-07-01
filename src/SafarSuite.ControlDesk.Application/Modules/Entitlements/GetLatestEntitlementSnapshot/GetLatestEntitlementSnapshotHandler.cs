using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;

public sealed class GetLatestEntitlementSnapshotHandler
{
    private readonly IEntitlementSnapshotRepository _entitlementSnapshots;

    public GetLatestEntitlementSnapshotHandler(IEntitlementSnapshotRepository entitlementSnapshots)
    {
        _entitlementSnapshots = entitlementSnapshots;
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

            return Result<GetLatestEntitlementSnapshotResult>.Success(ToResult(snapshot));
        }
        catch (ArgumentException exception)
        {
            return Result<GetLatestEntitlementSnapshotResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }

    private static GetLatestEntitlementSnapshotResult ToResult(EntitlementSnapshot snapshot)
    {
        return new GetLatestEntitlementSnapshotResult(
            snapshot.Id.Value,
            snapshot.ClientId.Value,
            snapshot.ContractId.Value,
            snapshot.Status.ToString(),
            snapshot.PaidUntil,
            snapshot.GraceUntil,
            snapshot.OfflineValidUntil,
            snapshot.AllowedDevices,
            snapshot.AllowedBranches,
            snapshot.IssuedAtUtc,
            snapshot.Modules.Select(module => new GetLatestEntitlementSnapshotModuleResult(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray());
    }
}
