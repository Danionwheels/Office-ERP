using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public sealed class ControlDeskOperatorAccessOptionsValidator(
    IHostEnvironment environment,
    IConfiguration configuration,
    IControlDeskSessionSigningKeyProvider? signingKeyProvider = null)
    : IValidateOptions<ControlDeskOperatorAccessOptions>
{
    private const string DevelopmentOperatorUserId = "local-control-desk-admin";

    private static readonly string[] PlaceholderMarkers =
    [
        "local-development",
        "change-before",
        "replace-with",
        "placeholder"
    ];

    public ValidateOptionsResult Validate(string? name, ControlDeskOperatorAccessOptions options)
    {
        var failures = new List<string>();

        if (options.SessionMinutes is < 5 or > 1_440)
        {
            failures.Add("ControlDesk:OperatorAccess:SessionMinutes must be between 5 and 1440.");
        }

        var secret = options.SessionSigningSecret?.Trim() ?? string.Empty;

        if (environment.IsProduction())
        {
            if (!string.IsNullOrWhiteSpace(secret)
                || !string.IsNullOrWhiteSpace(configuration[
                    $"{ControlDeskOperatorAccessOptions.SectionName}:SessionSigningSecret"]))
            {
                failures.Add(
                    "ControlDesk:OperatorAccess:SessionSigningSecret must not be supplied through Production configuration.");
            }

            if (signingKeyProvider is null)
            {
                failures.Add("The installed Control Desk machine-secret provider is unavailable.");
            }
            else
            {
                try
                {
                    var key = signingKeyProvider.CopySessionSigningKey();

                    try
                    {
                        if (key.Length < 32 || string.IsNullOrWhiteSpace(signingKeyProvider.SessionSigningKeyId))
                        {
                            failures.Add("The installed Control Desk machine-secret provider is unavailable.");
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(key);
                    }
                }
                catch (Exception exception) when (exception is not OutOfMemoryException
                                                   and not StackOverflowException)
                {
                    failures.Add("The installed Control Desk machine-secret provider is unavailable.");
                }
            }
        }
        else if (secret.Length < 32)
        {
            failures.Add("ControlDesk:OperatorAccess:SessionSigningSecret must contain at least 32 characters.");
        }
        else if (!environment.IsDevelopment()
                 && PlaceholderMarkers.Any(marker => secret.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("ControlDesk:OperatorAccess:SessionSigningSecret must not use a development placeholder outside Development.");
        }

        var configuredUsers = options.Users ?? [];
        var activeUsers = configuredUsers
            .Where(user => string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var persistenceProvider = configuration.GetValue<string>("Persistence:Provider") ?? "InMemory";
        var usesDevelopmentFixtures = persistenceProvider.Equals(
            "InMemory",
            StringComparison.OrdinalIgnoreCase);

        if (usesDevelopmentFixtures && activeUsers.Length == 0)
        {
            failures.Add("InMemory Control Desk authentication requires at least one active fixture operator.");
        }

        if (!environment.IsDevelopment()
            && !environment.IsEnvironment("Testing")
            && configuredUsers.Count > 0)
        {
            failures.Add(
                "Control Desk operator users must not be supplied through configuration outside Development or Testing.");
        }

        if (activeUsers.Any(user => string.IsNullOrWhiteSpace(user.UserId)
                                    || string.IsNullOrWhiteSpace(user.Email)
                                    || string.IsNullOrWhiteSpace(user.PasswordHash)))
        {
            failures.Add("Every active Control Desk operator must have a user id, email, and password hash.");
        }

        if (activeUsers.Any(user => (user.Roles?.Count ?? 0) == 0 && (user.Scopes?.Count ?? 0) == 0))
        {
            failures.Add("Every active Control Desk operator must have at least one role or scope.");
        }

        if (!environment.IsDevelopment()
            && activeUsers.Any(user => string.Equals(
                user.UserId?.Trim(),
                DevelopmentOperatorUserId,
                StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("The built-in development Control Desk operator must be replaced outside Development.");
        }

        if (activeUsers.Select(user => user.UserId?.Trim() ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != activeUsers.Length)
        {
            failures.Add("Active Control Desk operator user ids must be unique.");
        }

        if (activeUsers.Select(user => user.Email?.Trim() ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != activeUsers.Length)
        {
            failures.Add("Active Control Desk operator emails must be unique.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
