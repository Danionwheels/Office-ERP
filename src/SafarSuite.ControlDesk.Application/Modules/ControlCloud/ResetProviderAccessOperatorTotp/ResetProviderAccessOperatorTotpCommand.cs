namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorTotp;

public sealed record ResetProviderAccessOperatorTotpCommand(
    string UserId,
    string? UpdatedBy);
