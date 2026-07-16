using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Entitlements;

public sealed class ClientAccessRevision : Entity<ClientAccessRevisionId>
{
    private readonly List<ClientAccessRevisionModule> _modules = [];
    private readonly List<ModuleFeatureLimit> _featureLimits = [];

    private ClientAccessRevision()
    {
        SourceInvoiceNumber = null;
        ApprovedBy = null!;
        ApprovalReason = null!;
    }

    private ClientAccessRevision(
        ClientAccessRevisionId id,
        ClientId clientId,
        ContractId contractId,
        long contractRevisionNumber,
        ProductCatalogRevisionId productCatalogRevisionId,
        long productCatalogRevisionNumber,
        InvoiceId sourceInvoiceId,
        string sourceInvoiceNumber,
        long revisionNumber,
        ClientAccessRevisionId? supersedesRevisionId,
        DateOnly paidUntil,
        DateOnly graceUntil,
        DateOnly offlineValidUntil,
        int allowedDevices,
        int allowedBranches,
        int? allowedNamedUsers,
        int? allowedConcurrentUsers,
        DateTimeOffset effectiveFromUtc,
        string approvedBy,
        string approvalReason,
        DateTimeOffset approvedAtUtc)
        : base(id)
    {
        ClientId = clientId;
        ContractId = contractId;
        ContractRevisionNumber = contractRevisionNumber;
        ProductCatalogRevisionId = productCatalogRevisionId;
        ProductCatalogRevisionNumber = productCatalogRevisionNumber;
        SourceInvoiceId = sourceInvoiceId;
        SourceInvoiceNumber = sourceInvoiceNumber;
        EvidenceType = ClientAccessEvidenceType.PaidInvoice;
        RevisionNumber = revisionNumber;
        SupersedesRevisionId = supersedesRevisionId;
        PaidUntil = paidUntil;
        GraceUntil = graceUntil;
        OfflineValidUntil = offlineValidUntil;
        AllowedDevices = allowedDevices;
        AllowedBranches = allowedBranches;
        AllowedNamedUsers = allowedNamedUsers;
        AllowedConcurrentUsers = allowedConcurrentUsers;
        EffectiveFromUtc = effectiveFromUtc;
        ApprovedBy = approvedBy;
        ApprovalReason = approvalReason;
        ApprovedAtUtc = approvedAtUtc;
    }

    public ClientId ClientId { get; private set; }

    public ContractId ContractId { get; private set; }

    public long ContractRevisionNumber { get; private set; }

    public ProductCatalogRevisionId ProductCatalogRevisionId { get; private set; }

    public long ProductCatalogRevisionNumber { get; private set; }

    public InvoiceId? SourceInvoiceId { get; private set; }

    public string? SourceInvoiceNumber { get; private set; }

    public ClientAccessEvidenceType EvidenceType { get; private set; }

    public long RevisionNumber { get; private set; }

    public ClientAccessRevisionId? SupersedesRevisionId { get; private set; }

    public DateOnly PaidUntil { get; private set; }

    public DateOnly GraceUntil { get; private set; }

    public DateOnly OfflineValidUntil { get; private set; }

    public int AllowedDevices { get; private set; }

    public int AllowedBranches { get; private set; }

    public int? AllowedNamedUsers { get; private set; }

    public int? AllowedConcurrentUsers { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public string ApprovedBy { get; private set; }

    public string ApprovalReason { get; private set; }

    public DateTimeOffset ApprovedAtUtc { get; private set; }

    public IReadOnlyCollection<ClientAccessRevisionModule> Modules => _modules.AsReadOnly();

    public IReadOnlyCollection<ModuleFeatureLimit> FeatureLimits => _featureLimits.AsReadOnly();

    public static ClientAccessRevision ApproveFromPaidInvoice(
        ClientAccessRevisionId id,
        ClientId clientId,
        ContractId contractId,
        long contractRevisionNumber,
        ProductCatalogRevisionId productCatalogRevisionId,
        long productCatalogRevisionNumber,
        InvoiceId sourceInvoiceId,
        string sourceInvoiceNumber,
        long revisionNumber,
        ClientAccessRevisionId? supersedesRevisionId,
        DateOnly paidUntil,
        DateOnly graceUntil,
        DateOnly offlineValidUntil,
        int allowedDevices,
        int allowedBranches,
        IEnumerable<ClientAccessRevisionModule> modules,
        string approvedBy,
        string approvalReason,
        DateTimeOffset approvedAtUtc,
        int? allowedNamedUsers = null,
        int? allowedConcurrentUsers = null,
        IEnumerable<ModuleFeatureLimit>? featureLimits = null,
        DateTimeOffset? effectiveFromUtc = null)
    {
        if (revisionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revisionNumber),
                "Client access revision number must be greater than zero.");
        }

        if (contractRevisionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contractRevisionNumber),
                "Contract revision number must be greater than zero.");
        }

        if (productCatalogRevisionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productCatalogRevisionNumber),
                "Product catalog revision number must be greater than zero.");
        }

        if (supersedesRevisionId == id)
        {
            throw new ArgumentException("A client access revision cannot supersede itself.", nameof(supersedesRevisionId));
        }

        if (string.IsNullOrWhiteSpace(sourceInvoiceNumber))
        {
            throw new ArgumentException("Source invoice number is required.", nameof(sourceInvoiceNumber));
        }

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

        var userAllowance = UserAllowance.Create(allowedNamedUsers, allowedConcurrentUsers);
        var normalizedApprovedAtUtc = approvedAtUtc.ToUniversalTime();
        var normalizedEffectiveFromUtc = (effectiveFromUtc ?? normalizedApprovedAtUtc).ToUniversalTime();

        if (normalizedEffectiveFromUtc < normalizedApprovedAtUtc)
        {
            throw new ArgumentException(
                "Effective-from time cannot be before approval time.",
                nameof(effectiveFromUtc));
        }

        if (DateOnly.FromDateTime(normalizedEffectiveFromUtc.UtcDateTime) > paidUntil)
        {
            throw new ArgumentException(
                "Effective-from date cannot be after the paid-until date.",
                nameof(effectiveFromUtc));
        }

        var normalizedApprovedBy = NormalizeRequired(approvedBy, nameof(approvedBy), "Approver is required.");
        var normalizedApprovalReason = NormalizeRequired(
            approvalReason,
            nameof(approvalReason),
            "Approval reason is required.");
        var revision = new ClientAccessRevision(
            id,
            clientId,
            contractId,
            contractRevisionNumber,
            productCatalogRevisionId,
            productCatalogRevisionNumber,
            sourceInvoiceId,
            sourceInvoiceNumber.Trim(),
            revisionNumber,
            supersedesRevisionId,
            paidUntil,
            graceUntil,
            offlineValidUntil,
            allowedDevices,
            allowedBranches,
            userAllowance.AllowedNamedUsers,
            userAllowance.AllowedConcurrentUsers,
            normalizedEffectiveFromUtc,
            normalizedApprovedBy,
            normalizedApprovalReason,
            normalizedApprovedAtUtc);

        foreach (var module in modules)
        {
            revision._modules.Add(module);
        }

        if (revision._modules.Count == 0)
        {
            throw new InvalidOperationException("Client access revision requires at least one module.");
        }

        if (!revision._modules.Any(module => module.IsEnabled))
        {
            throw new InvalidOperationException("Client access revision requires at least one enabled module.");
        }

        var duplicateModuleCode = revision._modules
            .GroupBy(module => module.ModuleCode.Value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateModuleCode is not null)
        {
            throw new InvalidOperationException($"Module code {duplicateModuleCode} is duplicated.");
        }

        var enabledModuleCodes = revision._modules
            .Where(module => module.IsEnabled)
            .Select(module => module.ModuleCode.Value)
            .ToHashSet(StringComparer.Ordinal);
        var featureLimitKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var featureLimit in featureLimits ?? [])
        {
            if (!enabledModuleCodes.Contains(featureLimit.ModuleCode.Value))
            {
                throw new InvalidOperationException(
                    $"Feature limit {featureLimit.ModuleCode.Value}.{featureLimit.FeatureCode.Value} requires an enabled module.");
            }

            var key = $"{featureLimit.ModuleCode.Value}:{featureLimit.FeatureCode.Value}";

            if (!featureLimitKeys.Add(key))
            {
                throw new InvalidOperationException(
                    $"Feature limit {featureLimit.ModuleCode.Value}.{featureLimit.FeatureCode.Value} is duplicated.");
            }

            revision._featureLimits.Add(ModuleFeatureLimit.Create(
                featureLimit.ModuleCode,
                featureLimit.FeatureCode,
                featureLimit.LimitValue,
                featureLimit.Unit));
        }

        return revision;
    }

    private static string NormalizeRequired(string value, string parameterName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }
}

public enum ClientAccessEvidenceType
{
    PaidInvoice = 1,
    LegacyEntitlementImport = 2
}
