namespace SafarSuite.LocalServer.Application.ModuleGateway.EvaluateModuleAccess;

public sealed record EvaluateModuleAccessGatewayCommand(
    string InstallationId,
    string ModuleCode,
    DateOnly? AsOfDate,
    string? RequestedBy = null);
