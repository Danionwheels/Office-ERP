using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Entitlements;

public sealed class EntitlementSnapshot : Entity<EntitlementSnapshotId>
{
    private readonly List<EntitlementModule> _modules = [];

    private EntitlementSnapshot(
        EntitlementSnapshotId id,
        ClientId clientId,
        ContractId contractId,
        EntitlementStatus status,
        DateOnly paidUntil,
        DateOnly graceUntil,
        DateOnly offlineValidUntil,
        int allowedDevices,
        int allowedBranches,
        DateTimeOffset issuedAtUtc)
        : base(id)
    {
        ClientId = clientId;
        ContractId = contractId;
        Status = status;
        PaidUntil = paidUntil;
        GraceUntil = graceUntil;
        OfflineValidUntil = offlineValidUntil;
        AllowedDevices = allowedDevices;
        AllowedBranches = allowedBranches;
        IssuedAtUtc = issuedAtUtc;
    }

    public ClientId ClientId { get; }

    public ContractId ContractId { get; }

    public EntitlementStatus Status { get; }

    public DateOnly PaidUntil { get; }

    public DateOnly GraceUntil { get; }

    public DateOnly OfflineValidUntil { get; }

    public int AllowedDevices { get; }

    public int AllowedBranches { get; }

    public DateTimeOffset IssuedAtUtc { get; }

    public IReadOnlyCollection<EntitlementModule> Modules => _modules.AsReadOnly();

    public static EntitlementSnapshot Issue(
        EntitlementSnapshotId id,
        ClientId clientId,
        ContractId contractId,
        EntitlementStatus status,
        DateOnly paidUntil,
        DateOnly graceUntil,
        DateOnly offlineValidUntil,
        int allowedDevices,
        int allowedBranches,
        IEnumerable<EntitlementModule> modules,
        DateTimeOffset issuedAtUtc)
    {
        if (graceUntil < paidUntil)
        {
            throw new ArgumentException("Grace date cannot be before paid-until date.", nameof(graceUntil));
        }

        if (offlineValidUntil < paidUntil)
        {
            throw new ArgumentException("Offline validity cannot be before paid-until date.", nameof(offlineValidUntil));
        }

        if (allowedDevices < 0)
        {
            throw new ArgumentException("Allowed device count cannot be negative.", nameof(allowedDevices));
        }

        if (allowedBranches < 0)
        {
            throw new ArgumentException("Allowed branch count cannot be negative.", nameof(allowedBranches));
        }

        var snapshot = new EntitlementSnapshot(
            id,
            clientId,
            contractId,
            status,
            paidUntil,
            graceUntil,
            offlineValidUntil,
            allowedDevices,
            allowedBranches,
            issuedAtUtc);

        foreach (var module in modules)
        {
            snapshot._modules.Add(module);
        }

        if (snapshot._modules.Count == 0)
        {
            throw new InvalidOperationException("Entitlement snapshot requires at least one module.");
        }

        return snapshot;
    }

    public bool AllowsUseOn(DateOnly date)
    {
        return Status switch
        {
            EntitlementStatus.Active => date <= PaidUntil,
            EntitlementStatus.Grace => date <= GraceUntil,
            _ => false
        };
    }
}
