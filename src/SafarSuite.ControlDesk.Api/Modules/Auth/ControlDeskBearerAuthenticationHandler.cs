using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public sealed class ControlDeskBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IControlDeskSessionTokenService tokens)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (!AuthenticationHeaderValue.TryParse(authorization, out var header)
            || !header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var validation = tokens.Validate(header.Parameter.Trim());

        if (!validation.IsValid || validation.Principal is null)
        {
            return Task.FromResult(AuthenticateResult.Fail(validation.FailureCode ?? "SessionTokenInvalid"));
        }

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(validation.Principal, Scheme.Name)));
    }
}
