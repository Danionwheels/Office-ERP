using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth.AuthenticateLocalOperator;

public sealed class AuthenticateLocalOperatorHandler(
    ILocalOperatorRepository operators,
    ILocalOperatorPasswordCodec passwords)
{
    public async Task<AuthenticateLocalOperatorResult> HandleAsync(
        AuthenticateLocalOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrEmpty(command.Password))
        {
            return AuthenticateLocalOperatorResult.Failed;
        }

        LocalOperatorEmail email;

        try
        {
            email = LocalOperatorEmail.Create(command.Email);
        }
        catch (ArgumentException)
        {
            return AuthenticateLocalOperatorResult.Failed;
        }

        var localOperator = await operators.GetByNormalizedEmailAsync(
            email.NormalizedValue,
            cancellationToken);

        if (localOperator is null
            || localOperator.Status != LocalOperatorStatus.Active
            || !passwords.Verify(command.Password, localOperator.PasswordHash))
        {
            return AuthenticateLocalOperatorResult.Failed;
        }

        return AuthenticateLocalOperatorResult.Success(new LocalOperatorSessionPrincipal(
            localOperator.Id.Value,
            localOperator.Email,
            localOperator.FullName,
            localOperator.Roles.ToArray(),
            localOperator.Scopes.ToArray(),
            localOperator.SecurityVersion));
    }
}
