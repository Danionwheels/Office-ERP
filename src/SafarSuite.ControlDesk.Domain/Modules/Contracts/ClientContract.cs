using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ClientContract : Entity<ContractId>
{
    private readonly List<ModuleAllowance> _moduleAllowances = [];

    private ClientContract()
    {
        Number = null!;
        Term = null!;
        Pricing = null!;
        DeviceAllowance = null!;
        BranchAllowance = null!;
    }

    private ClientContract(
        ContractId id,
        ClientId clientId,
        ContractNumber number,
        DateRange term,
        ContractPricing pricing,
        DeviceAllowance deviceAllowance,
        BranchAllowance branchAllowance,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        Number = number;
        Term = term;
        Pricing = pricing;
        DeviceAllowance = deviceAllowance;
        BranchAllowance = branchAllowance;
        CreatedAtUtc = createdAtUtc;
        Status = ContractStatus.Draft;
    }

    public ClientId ClientId { get; private set; }

    public ContractNumber Number { get; private set; }

    public DateRange Term { get; private set; }

    public ContractPricing Pricing { get; private set; }

    public DeviceAllowance DeviceAllowance { get; private set; }

    public BranchAllowance BranchAllowance { get; private set; }

    public ContractStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }

    public IReadOnlyCollection<ModuleAllowance> ModuleAllowances => _moduleAllowances.AsReadOnly();

    public static ClientContract Create(
        ContractId id,
        ClientId clientId,
        ContractNumber number,
        DateRange term,
        ContractPricing pricing,
        DeviceAllowance deviceAllowance,
        BranchAllowance branchAllowance,
        DateTimeOffset createdAtUtc)
    {
        return new ClientContract(
            id,
            clientId,
            number,
            term,
            pricing,
            deviceAllowance,
            branchAllowance,
            createdAtUtc);
    }

    public void Activate(DateTimeOffset activatedAtUtc)
    {
        if (_moduleAllowances.Count == 0)
        {
            throw new InvalidOperationException("At least one module allowance is required before activation.");
        }

        if (!_moduleAllowances.Any(module => module.IsEnabled))
        {
            throw new InvalidOperationException("At least one module must be enabled before activation.");
        }

        Status = ContractStatus.Active;
        ActivatedAtUtc = activatedAtUtc;
    }

    public void Suspend()
    {
        Status = ContractStatus.Suspended;
    }

    public void UpdateTerm(DateRange term)
    {
        Term = term;
    }

    public void UpdatePricing(ContractPricing pricing)
    {
        Pricing = pricing;
    }

    public void SetDeviceAllowance(DeviceAllowance allowance)
    {
        DeviceAllowance = allowance;
    }

    public void SetBranchAllowance(BranchAllowance allowance)
    {
        BranchAllowance = allowance;
    }

    public void SetModuleAllowance(ModuleAllowance allowance)
    {
        _moduleAllowances.RemoveAll(existing => existing.ModuleCode.Equals(allowance.ModuleCode));
        _moduleAllowances.Add(allowance);
    }
}
