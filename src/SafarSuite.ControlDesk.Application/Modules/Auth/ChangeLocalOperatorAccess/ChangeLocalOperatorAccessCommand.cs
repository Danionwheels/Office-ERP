namespace SafarSuite.ControlDesk.Application.Modules.Auth.ChangeLocalOperatorAccess;

public sealed record ChangeLocalOperatorAccessCommand(
    Guid ActingOperatorId,
    Guid TargetOperatorId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes);
