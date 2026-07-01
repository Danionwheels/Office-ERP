namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ListClientContracts;

public sealed record ListClientContractsResult(
    Guid ClientId,
    IReadOnlyCollection<ClientContractResult> Contracts);
