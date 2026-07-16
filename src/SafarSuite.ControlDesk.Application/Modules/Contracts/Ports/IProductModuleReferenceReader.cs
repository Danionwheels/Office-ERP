namespace SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

public interface IProductModuleReferenceReader
{
    Task<IReadOnlyCollection<ProductModuleContractReference>> ListActiveAsync(
        CancellationToken cancellationToken = default);
}

public sealed record ProductModuleContractReference(
    string ModuleCode,
    Guid ContractId,
    string ContractNumber,
    long ContractRevisionNumber,
    Guid ClientId);
