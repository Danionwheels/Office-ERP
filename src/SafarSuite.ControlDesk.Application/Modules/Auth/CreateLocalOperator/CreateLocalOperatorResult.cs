namespace SafarSuite.ControlDesk.Application.Modules.Auth.CreateLocalOperator;

public sealed record CreateLocalOperatorResult(
    Guid OperatorId,
    string Email,
    string FullName,
    string Status,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes,
    long SecurityVersion,
    DateTimeOffset CreatedAtUtc);
