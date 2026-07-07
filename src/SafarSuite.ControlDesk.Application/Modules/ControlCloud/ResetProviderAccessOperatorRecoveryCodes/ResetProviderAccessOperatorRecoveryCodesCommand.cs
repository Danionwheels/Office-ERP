namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorRecoveryCodes;

public sealed record ResetProviderAccessOperatorRecoveryCodesCommand(
    string UserId,
    int? Count,
    string? UpdatedBy);
