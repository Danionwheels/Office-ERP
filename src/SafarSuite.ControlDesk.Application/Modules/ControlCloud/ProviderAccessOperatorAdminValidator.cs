using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud;

internal static class ProviderAccessOperatorAdminValidator
{
    private static readonly string[] SupportedScopes =
    [
        "*",
        "app-activation:read",
        "app-activation:write",
        "client-portal:manage",
        "deployment-packages:read",
        "deployment-packages:write",
        "provider-operators:manage"
    ];

    public static IReadOnlyCollection<ApplicationError> ValidateCreate(
        string email,
        string fullName,
        string password,
        IEnumerable<string>? scopes,
        string? actor)
    {
        var errors = new List<ApplicationError>();

        AddEmail(errors, nameof(email), email);
        AddRequiredText(errors, nameof(fullName), fullName, 180);
        AddPassword(errors, nameof(password), password);
        AddScopes(errors, nameof(scopes), scopes);
        AddOptionalText(errors, nameof(actor), actor, 120);

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> ValidateSession(
        string email,
        string password,
        IEnumerable<string>? scopes,
        int? expiresInMinutes)
    {
        var errors = new List<ApplicationError>();

        AddEmail(errors, nameof(email), email);
        AddRequiredText(errors, nameof(password), password, 200);
        AddOptionalScopes(errors, nameof(scopes), scopes);

        if (expiresInMinutes is < 5 or > 1440)
        {
            errors.Add(ApplicationError.Validation(
                nameof(expiresInMinutes),
                "Provider operator session length must be between 5 and 1440 minutes."));
        }

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> ValidatePasswordChange(
        string email,
        string currentPassword,
        string newPassword)
    {
        var errors = new List<ApplicationError>();

        AddEmail(errors, nameof(email), email);
        AddRequiredText(errors, nameof(currentPassword), currentPassword, 200);
        AddPassword(errors, nameof(newPassword), newPassword);

        if (!string.IsNullOrWhiteSpace(currentPassword)
            && !string.IsNullOrWhiteSpace(newPassword)
            && currentPassword == newPassword)
        {
            errors.Add(ApplicationError.Validation(
                nameof(newPassword),
                "New provider operator password must be different from the current password."));
        }

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> ValidatePasswordReset(
        string userId,
        string password,
        string? actor)
    {
        var errors = new List<ApplicationError>();

        AddRequiredText(errors, nameof(userId), userId, 120);
        AddPassword(errors, nameof(password), password);
        AddOptionalText(errors, nameof(actor), actor, 120);

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> ValidateRecoveryCodeReset(
        string userId,
        int? count,
        string? actor)
    {
        var errors = new List<ApplicationError>();

        AddRequiredText(errors, nameof(userId), userId, 120);
        AddOptionalText(errors, nameof(actor), actor, 120);

        if (count is < 1 or > 20)
        {
            errors.Add(ApplicationError.Validation(
                nameof(count),
                "Provider operator recovery code count must be between 1 and 20."));
        }

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> ValidateTotpReset(
        string userId,
        string? actor)
    {
        var errors = new List<ApplicationError>();

        AddRequiredText(errors, nameof(userId), userId, 120);
        AddOptionalText(errors, nameof(actor), actor, 120);

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> ValidateScopes(
        string userId,
        IEnumerable<string>? scopes,
        string? actor)
    {
        var errors = new List<ApplicationError>();

        AddRequiredText(errors, nameof(userId), userId, 120);
        AddScopes(errors, nameof(scopes), scopes);
        AddOptionalText(errors, nameof(actor), actor, 120);

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> ValidateStatus(
        string userId,
        string status,
        string? actor)
    {
        var errors = new List<ApplicationError>();

        AddRequiredText(errors, nameof(userId), userId, 120);
        AddRequiredText(errors, nameof(status), status, 32);
        AddOptionalText(errors, nameof(actor), actor, 120);

        if (!status.Equals("Active", StringComparison.Ordinal)
            && !status.Equals("Suspended", StringComparison.Ordinal))
        {
            errors.Add(ApplicationError.Validation(
                nameof(status),
                "Provider operator status must be Active or Suspended."));
        }

        return errors;
    }

    public static ApplicationError ToApplicationError(
        string? failureCode,
        string? detail,
        string credentialTarget = "password")
    {
        var message = string.IsNullOrWhiteSpace(detail)
            ? "Control Cloud provider access request failed."
            : detail;

        return failureCode switch
        {
            "ProviderOperatorAlreadyExists" => ApplicationError.Conflict("email", message),
            "ProviderOperatorNotFound" => ApplicationError.NotFound("userId", message),
            "ProviderOperatorEmailInvalid" => ApplicationError.Validation("email", message),
            "ProviderOperatorNameRequired" => ApplicationError.Validation("fullName", message),
            "ProviderOperatorPasswordInvalid" => ApplicationError.Validation("password", message),
            "ProviderOperatorPasswordUnchanged" => ApplicationError.Validation("newPassword", message),
            "ProviderOperatorRecoveryCodeCountInvalid" => ApplicationError.Validation("count", message),
            "ProviderOperatorScopesRequired" => ApplicationError.Validation("scopes", message),
            "ProviderOperatorScopesUnsupported" => ApplicationError.Validation("scopes", message),
            "ProviderOperatorStatusUnsupported" => ApplicationError.Validation("status", message),
            "ProviderCredentialsInvalid" => ApplicationError.Validation(credentialTarget, message),
            "ProviderLoginLocked" => ApplicationError.Validation(credentialTarget, message),
            "ProviderMfaRequired" => ApplicationError.Validation("totpCode", message),
            "ProviderMfaInvalid" => ApplicationError.Validation("totpCode", message),
            "ProviderMfaUnavailable" => ApplicationError.Validation("recoveryCode", message),
            "ProviderAccessDenied" => ApplicationError.ServiceUnavailable(message),
            "ProviderAccessNotConfigured" => ApplicationError.ServiceUnavailable(message),
            "ProviderAccessScopeDenied" => ApplicationError.ServiceUnavailable(message),
            "ProviderAccessScopeUnsupported" => ApplicationError.Validation("scopes", message),
            "ControlCloudProviderAccessDenied" => ApplicationError.ServiceUnavailable(message),
            "ControlCloudProviderAccessNotConfigured" => ApplicationError.ServiceUnavailable(message),
            _ => ApplicationError.Unexpected(message)
        };
    }

    public static string NormalizeActor(string? actor)
    {
        return string.IsNullOrWhiteSpace(actor)
            ? "SafarSuite Control Desk"
            : actor.Trim();
    }

    public static string[] NormalizeScopes(IEnumerable<string> scopes)
    {
        return scopes
            .Select(scope => scope.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string[]? NormalizeOptionalScopes(IEnumerable<string>? scopes)
    {
        var normalizedScopes = NormalizeScopes(scopes ?? []);

        return normalizedScopes.Length == 0
            ? null
            : normalizedScopes;
    }

    private static void AddEmail(
        ICollection<ApplicationError> errors,
        string target,
        string email)
    {
        AddRequiredText(errors, target, email, 320);

        if (!string.IsNullOrWhiteSpace(email)
            && !email.Contains('@', StringComparison.Ordinal))
        {
            errors.Add(ApplicationError.Validation(target, "Provider operator email is invalid."));
        }
    }

    private static void AddPassword(
        ICollection<ApplicationError> errors,
        string target,
        string password)
    {
        AddRequiredText(errors, target, password, 200);

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 12)
        {
            errors.Add(ApplicationError.Validation(
                target,
                "Provider operator password must be at least 12 characters."));
        }
    }

    private static void AddScopes(
        ICollection<ApplicationError> errors,
        string target,
        IEnumerable<string>? scopes)
    {
        var normalizedScopes = NormalizeScopes(scopes ?? []);

        if (normalizedScopes.Length == 0)
        {
            errors.Add(ApplicationError.Validation(
                target,
                "At least one provider operator scope is required."));

            return;
        }

        foreach (var scope in normalizedScopes)
        {
            if (!SupportedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(ApplicationError.Validation(
                    target,
                    $"Provider operator scope '{scope}' is not supported."));
            }
        }
    }

    private static void AddOptionalScopes(
        ICollection<ApplicationError> errors,
        string target,
        IEnumerable<string>? scopes)
    {
        var normalizedScopes = NormalizeScopes(scopes ?? []);

        foreach (var scope in normalizedScopes)
        {
            if (!SupportedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(ApplicationError.Validation(
                    target,
                    $"Provider operator scope '{scope}' is not supported."));
            }
        }
    }

    private static void AddRequiredText(
        ICollection<ApplicationError> errors,
        string target,
        string value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(ApplicationError.Validation(target, $"{target} is required."));

            return;
        }

        AddOptionalText(errors, target, value, maxLength);
    }

    private static void AddOptionalText(
        ICollection<ApplicationError> errors,
        string target,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors.Add(ApplicationError.Validation(
                target,
                $"{target} cannot exceed {maxLength} characters."));
        }
    }
}
