namespace SafarSuite.ControlDesk.Application.Modules.Auth.ChangeLocalOperatorAccess;

public sealed record ChangeLocalOperatorAccessResult(
    Guid OperatorId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes,
    long SecurityVersion,
    DateTimeOffset UpdatedAtUtc);
