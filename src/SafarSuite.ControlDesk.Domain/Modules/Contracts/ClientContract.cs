using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ClientContract : Entity<ContractId>
{
    private readonly List<ModuleAllowance> _moduleAllowances = [];
    private readonly List<ModuleFeatureLimit> _featureLimits = [];

    private ClientContract()
    {
        Number = null!;
        Term = null!;
        Pricing = null!;
        DeviceAllowance = null!;
        BranchAllowance = null!;
        UserAllowance = null!;
        ApprovedBy = null!;
        ApprovalReason = null!;
    }

    private ClientContract(
        ContractId id,
        ClientId clientId,
        long revisionNumber,
        ContractId? supersedesContractId,
        ProductCatalogRevisionId productCatalogRevisionId,
        long productCatalogRevisionNumber,
        ContractNumber number,
        DateRange term,
        ContractPricing pricing,
        DeviceAllowance deviceAllowance,
        BranchAllowance branchAllowance,
        UserAllowance userAllowance,
        string approvedBy,
        string approvalReason,
        DateTimeOffset approvedAtUtc,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        RevisionNumber = revisionNumber;
        SupersedesContractId = supersedesContractId;
        ProductCatalogRevisionId = productCatalogRevisionId;
        ProductCatalogRevisionNumber = productCatalogRevisionNumber;
        Number = number;
        Term = term;
        Pricing = pricing;
        DeviceAllowance = deviceAllowance;
        BranchAllowance = branchAllowance;
        UserAllowance = userAllowance;
        ApprovedBy = approvedBy;
        ApprovalReason = approvalReason;
        ApprovedAtUtc = approvedAtUtc;
        CreatedAtUtc = createdAtUtc;
        Status = ContractStatus.Draft;
    }

    public ClientId ClientId { get; private set; }

    public long RevisionNumber { get; private set; }

    public ContractId? SupersedesContractId { get; private set; }

    public ProductCatalogRevisionId ProductCatalogRevisionId { get; private set; }

    public long ProductCatalogRevisionNumber { get; private set; }

    public ContractNumber Number { get; private set; }

    public DateRange Term { get; private set; }

    public ContractPricing Pricing { get; private set; }

    public DeviceAllowance DeviceAllowance { get; private set; }

    public BranchAllowance BranchAllowance { get; private set; }

    public UserAllowance UserAllowance { get; private set; }

    public ContractStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }

    public string ApprovedBy { get; private set; }

    public string ApprovalReason { get; private set; }

    public DateTimeOffset ApprovedAtUtc { get; private set; }

    public IReadOnlyCollection<ModuleAllowance> ModuleAllowances => _moduleAllowances.AsReadOnly();

    public IReadOnlyCollection<ModuleFeatureLimit> FeatureLimits => _featureLimits.AsReadOnly();

    public static ClientContract Create(
        ContractId id,
        ClientId clientId,
        long revisionNumber,
        ContractId? supersedesContractId,
        ProductCatalogRevisionId productCatalogRevisionId,
        long productCatalogRevisionNumber,
        ContractNumber number,
        DateRange term,
        ContractPricing pricing,
        DeviceAllowance deviceAllowance,
        BranchAllowance branchAllowance,
        string approvedBy,
        string approvalReason,
        DateTimeOffset approvedAtUtc,
        DateTimeOffset createdAtUtc,
        UserAllowance? userAllowance = null,
        IEnumerable<ModuleFeatureLimit>? featureLimits = null)
    {
        if (revisionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revisionNumber),
                "Contract revision number must be greater than zero.");
        }

        if (supersedesContractId == id)
        {
            throw new ArgumentException("A contract revision cannot supersede itself.", nameof(supersedesContractId));
        }

        if (revisionNumber == 1 && supersedesContractId is not null)
        {
            throw new ArgumentException("The first contract revision cannot supersede another revision.", nameof(supersedesContractId));
        }

        if (revisionNumber > 1 && supersedesContractId is null)
        {
            throw new ArgumentException("A later contract revision must supersede the previous revision.", nameof(supersedesContractId));
        }

        if (productCatalogRevisionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productCatalogRevisionNumber),
                "Product catalog revision number must be greater than zero.");
        }

        var contract = new ClientContract(
            id,
            clientId,
            revisionNumber,
            supersedesContractId,
            productCatalogRevisionId,
            productCatalogRevisionNumber,
            number,
            term,
            pricing,
            deviceAllowance,
            branchAllowance,
            userAllowance ?? UserAllowance.Unspecified,
            NormalizeRequired(approvedBy, nameof(approvedBy), "Contract approver is required."),
            NormalizeRequired(approvalReason, nameof(approvalReason), "Contract approval reason is required."),
            approvedAtUtc,
            createdAtUtc);

        foreach (var featureLimit in featureLimits ?? [])
        {
            contract.SetFeatureLimit(featureLimit);
        }

        return contract;
    }

    public void Activate(DateTimeOffset activatedAtUtc)
    {
        EnsureDraft();

        if (_moduleAllowances.Count == 0)
        {
            throw new InvalidOperationException("At least one module allowance is required before activation.");
        }

        if (!_moduleAllowances.Any(module => module.IsEnabled))
        {
            throw new InvalidOperationException("At least one module must be enabled before activation.");
        }

        var enabledModuleCodes = _moduleAllowances
            .Where(module => module.IsEnabled)
            .Select(module => module.ModuleCode.Value)
            .ToHashSet(StringComparer.Ordinal);
        var disabledFeatureLimit = _featureLimits.FirstOrDefault(
            limit => !enabledModuleCodes.Contains(limit.ModuleCode.Value));

        if (disabledFeatureLimit is not null)
        {
            throw new InvalidOperationException(
                $"Feature limit {disabledFeatureLimit.ModuleCode.Value}.{disabledFeatureLimit.FeatureCode.Value} requires an enabled module.");
        }

        Status = ContractStatus.Active;
        ActivatedAtUtc = activatedAtUtc;
    }

    public void Suspend()
    {
        if (Status != ContractStatus.Active)
        {
            throw new InvalidOperationException("Only an active contract revision can be suspended.");
        }

        Status = ContractStatus.Suspended;
    }

    public void UpdateTerm(DateRange term)
    {
        EnsureDraft();
        Term = term;
    }

    public void UpdatePricing(ContractPricing pricing)
    {
        EnsureDraft();
        Pricing = pricing;
    }

    public void SetDeviceAllowance(DeviceAllowance allowance)
    {
        EnsureDraft();
        DeviceAllowance = allowance;
    }

    public void SetBranchAllowance(BranchAllowance allowance)
    {
        EnsureDraft();
        BranchAllowance = allowance;
    }

    public void SetUserAllowance(UserAllowance allowance)
    {
        EnsureDraft();
        UserAllowance = allowance;
    }

    public void SetModuleAllowance(ModuleAllowance allowance)
    {
        EnsureDraft();
        _moduleAllowances.RemoveAll(existing => existing.ModuleCode.Equals(allowance.ModuleCode));
        _moduleAllowances.Add(allowance);
    }

    public void SetFeatureLimit(ModuleFeatureLimit featureLimit)
    {
        EnsureDraft();
        _featureLimits.RemoveAll(existing =>
            existing.ModuleCode.Equals(featureLimit.ModuleCode)
            && existing.FeatureCode.Equals(featureLimit.FeatureCode));
        _featureLimits.Add(ModuleFeatureLimit.Create(
            featureLimit.ModuleCode,
            featureLimit.FeatureCode,
            featureLimit.LimitValue,
            featureLimit.Unit));
    }

    private void EnsureDraft()
    {
        if (Status != ContractStatus.Draft)
        {
            throw new InvalidOperationException(
                "Approved contract terms are immutable. Create a replacement revision instead.");
        }
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
