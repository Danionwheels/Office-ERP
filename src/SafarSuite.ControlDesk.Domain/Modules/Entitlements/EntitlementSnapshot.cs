using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Entitlements;

public sealed class EntitlementSnapshot : Entity<EntitlementSnapshotId>
{
    private readonly List<EntitlementModule> _modules = [];
    private readonly List<ModuleFeatureLimit> _featureLimits = [];

    private EntitlementSnapshot()
    {
    }

    private EntitlementSnapshot(
        EntitlementSnapshotId id,
        ClientId clientId,
        ContractId contractId,
        ClientAccessRevisionId clientAccessRevisionId,
        long entitlementVersion,
        EntitlementStatus status,
        DateOnly paidUntil,
        DateOnly graceUntil,
        DateOnly offlineValidUntil,
        int allowedDevices,
        int allowedBranches,
        int? allowedNamedUsers,
        int? allowedConcurrentUsers,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset issuedAtUtc)
        : base(id)
    {
        ClientId = clientId;
        ContractId = contractId;
        ClientAccessRevisionId = clientAccessRevisionId;
        EntitlementVersion = entitlementVersion;
        Status = status;
        PaidUntil = paidUntil;
        GraceUntil = graceUntil;
        OfflineValidUntil = offlineValidUntil;
        AllowedDevices = allowedDevices;
        AllowedBranches = allowedBranches;
        AllowedNamedUsers = allowedNamedUsers;
        AllowedConcurrentUsers = allowedConcurrentUsers;
        EffectiveFromUtc = effectiveFromUtc;
        IssuedAtUtc = issuedAtUtc;
    }

    public ClientId ClientId { get; private set; }

    public ContractId ContractId { get; private set; }

    public ClientAccessRevisionId ClientAccessRevisionId { get; private set; }

    public long EntitlementVersion { get; private set; }

    public EntitlementStatus Status { get; private set; }

    public DateOnly PaidUntil { get; private set; }

    public DateOnly GraceUntil { get; private set; }

    public DateOnly OfflineValidUntil { get; private set; }

    public int AllowedDevices { get; private set; }

    public int AllowedBranches { get; private set; }

    public int? AllowedNamedUsers { get; private set; }

    public int? AllowedConcurrentUsers { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public DateTimeOffset IssuedAtUtc { get; private set; }

    public IReadOnlyCollection<EntitlementModule> Modules => _modules.AsReadOnly();

    public IReadOnlyCollection<ModuleFeatureLimit> FeatureLimits => _featureLimits.AsReadOnly();

    public static EntitlementSnapshot IssueFromApprovedRevision(
        EntitlementSnapshotId id,
        ClientAccessRevision revision,
        EntitlementStatus status,
        DateTimeOffset issuedAtUtc)
    {
        var snapshot = new EntitlementSnapshot(
            id,
            revision.ClientId,
            revision.ContractId,
            revision.Id,
            revision.RevisionNumber,
            status,
            revision.PaidUntil,
            revision.GraceUntil,
            revision.OfflineValidUntil,
            revision.AllowedDevices,
            revision.AllowedBranches,
            revision.AllowedNamedUsers,
            revision.AllowedConcurrentUsers,
            revision.EffectiveFromUtc,
            issuedAtUtc);

        foreach (var module in revision.Modules)
        {
            snapshot._modules.Add(EntitlementModule.Create(module.ModuleCode, module.IsEnabled));
        }

        foreach (var featureLimit in revision.FeatureLimits)
        {
            snapshot._featureLimits.Add(ModuleFeatureLimit.Create(
                featureLimit.ModuleCode,
                featureLimit.FeatureCode,
                featureLimit.LimitValue,
                featureLimit.Unit));
        }

        return snapshot;
    }

    public bool AllowsUseOn(DateOnly date)
    {
        if (date < DateOnly.FromDateTime(EffectiveFromUtc.UtcDateTime))
        {
            return false;
        }

        return Status switch
        {
            EntitlementStatus.Active => date <= PaidUntil,
            EntitlementStatus.Grace => date <= GraceUntil,
            _ => false
        };
    }
}
