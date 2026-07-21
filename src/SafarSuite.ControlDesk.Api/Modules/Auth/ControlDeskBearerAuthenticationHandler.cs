using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public sealed class ControlDeskBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IControlDeskSessionTokenService tokens,
    ILocalOperatorRepository operators)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (!AuthenticationHeaderValue.TryParse(authorization, out var header)
            || !header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        var validation = tokens.Validate(header.Parameter.Trim());

        if (!validation.IsValid || validation.Snapshot is null)
        {
            return AuthenticateResult.Fail(validation.FailureCode ?? "SessionTokenInvalid");
        }

        LocalOperator? localOperator;

        try
        {
            localOperator = await operators.GetByIdAsync(
                LocalOperatorId.Create(validation.Snapshot.OperatorId),
                Context.RequestAborted);
        }
        catch (OperationCanceledException) when (Context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Logger.LogWarning(
                exception,
                "Control Desk session validation could not read the persisted operator store.");
            return AuthenticateResult.Fail("SessionOperatorStoreUnavailable");
        }

        if (localOperator is null || localOperator.Status != LocalOperatorStatus.Active)
        {
            return AuthenticateResult.Fail("SessionOperatorInactive");
        }

        if (!string.Equals(
                localOperator.Email,
                validation.Snapshot.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("SessionOperatorChanged");
        }

        if (localOperator.SecurityVersion != validation.Snapshot.SecurityVersion)
        {
            return AuthenticateResult.Fail("SessionSecurityVersionChanged");
        }

        var currentRoles = Normalize(localOperator.Roles);
        var currentScopes = Normalize(localOperator.Scopes);

        if (!currentRoles.SequenceEqual(
                Normalize(validation.Snapshot.Roles),
                StringComparer.Ordinal)
            || !currentScopes.SequenceEqual(
                Normalize(validation.Snapshot.Scopes),
                StringComparer.Ordinal))
        {
            return AuthenticateResult.Fail("SessionPermissionsChanged");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, localOperator.Id.Value.ToString()),
            new(ClaimTypes.Email, localOperator.Email),
            new(ClaimTypes.Name, localOperator.FullName)
        };

        claims.AddRange(currentRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(currentScopes.Select(scope =>
            new Claim(ControlDeskSessionTokenService.ScopeClaimType, scope)));

        var identity = new ClaimsIdentity(
            claims,
            Scheme.Name,
            ClaimTypes.Name,
            ClaimTypes.Role);

        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            Scheme.Name));
    }

    private static string[] Normalize(IEnumerable<string> values) =>
        values
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
}
