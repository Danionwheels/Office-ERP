namespace SafarSuite.ControlDesk.Application.Modules.Auth.AuthenticateLocalOperator;

public sealed record LocalOperatorSessionPrincipal(
    Guid OperatorId,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes,
    long SecurityVersion);
