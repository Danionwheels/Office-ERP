using Microsoft.Extensions.Options;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public sealed class ControlDeskOperatorAccessOptionsValidator(IHostEnvironment environment)
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

        if (secret.Length < 32)
        {
            failures.Add("ControlDesk:OperatorAccess:SessionSigningSecret must contain at least 32 characters.");
        }
        else if (!environment.IsDevelopment()
                 && PlaceholderMarkers.Any(marker => secret.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("ControlDesk:OperatorAccess:SessionSigningSecret must not use a development placeholder outside Development.");
        }

        var activeUsers = options.Users
            .Where(user => string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (activeUsers.Length == 0)
        {
            failures.Add("At least one active Control Desk operator must be configured.");
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
