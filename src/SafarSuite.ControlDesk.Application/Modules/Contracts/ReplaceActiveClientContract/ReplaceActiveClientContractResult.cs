namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;

public sealed record ReplaceActiveClientContractResult(
    ClientContractResult? SuspendedContract,
    ClientContractResult ActiveContract);
