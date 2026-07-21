namespace SafarSuite.ControlDesk.Application.Modules.Auth.AuthenticateLocalOperator;

public sealed record AuthenticateLocalOperatorResult(
    bool IsAuthenticated,
    LocalOperatorSessionPrincipal? Principal)
{
    public static AuthenticateLocalOperatorResult Failed { get; } = new(false, null);

    public static AuthenticateLocalOperatorResult Success(LocalOperatorSessionPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return new AuthenticateLocalOperatorResult(true, principal);
    }
}
