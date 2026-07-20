namespace SafarSuite.ControlDesk.Application.Modules.Auth.CreateLocalOperator;

public sealed record CreateLocalOperatorCommand(
    Guid ActingOperatorId,
    string Email,
    string FullName,
    string Password,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes);
