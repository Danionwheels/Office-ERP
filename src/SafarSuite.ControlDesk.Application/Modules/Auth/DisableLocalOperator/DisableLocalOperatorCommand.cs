namespace SafarSuite.ControlDesk.Application.Modules.Auth.DisableLocalOperator;

public sealed record DisableLocalOperatorCommand(
    Guid ActingOperatorId,
    Guid TargetOperatorId);
