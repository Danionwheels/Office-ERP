using System.Security.Cryptography;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public static class ProviderAccessEndpoints
{
    public static IEndpointRouteBuilder MapProviderAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/provider-access")
            .WithTags("Provider Access");

        group.MapPost("/sessions", CreateSessionAsync)
            .WithName("CreateProviderAccessSession");
        group.MapPost("/operator-sessions", CreateOperatorSessionAsync)
            .WithName("CreateProviderOperatorSession");
        group.MapPost("/operator-password", ChangeOperatorPasswordAsync)
            .WithName("ChangeProviderOperatorPassword");

        group.MapGet("/operators", ListOperatorsAsync)
            .WithName("ListProviderAccessOperators");
        group.MapPost("/operators", CreateOperatorAsync)
            .WithName("CreateProviderAccessOperator");
        group.MapPost("/operators/{userId}/password", ResetOperatorPasswordAsync)
            .WithName("ResetProviderAccessOperatorPassword");
        group.MapPost("/operators/{userId}/recovery-codes", ResetOperatorRecoveryCodesAsync)
            .WithName("ResetProviderAccessOperatorRecoveryCodes");
        group.MapPost("/operators/{userId}/totp", ResetOperatorTotpAsync)
            .WithName("ResetProviderAccessOperatorTotp");
        group.MapPost("/operators/{userId}/scopes", UpdateOperatorScopesAsync)
            .WithName("UpdateProviderAccessOperatorScopes");
        group.MapPost("/operators/{userId}/status", UpdateOperatorStatusAsync)
            .WithName("UpdateProviderAccessOperatorStatus");

        return endpoints;
    }

    private static Task<IResult> CreateSessionAsync(
        CreateProviderAccessSessionRequest request,
        ProviderAccessSessionService sessions)
    {
        var result = sessions.CreateSession(
            request.SharedSecret,
            request.Actor,
            request.Scopes,
            request.ExpiresInMinutes);

        if (!result.IsSuccess)
        {
            return Task.FromResult<IResult>(Results.Json(
                new { code = result.FailureCode, detail = result.Detail },
                statusCode: result.StatusCode));
        }

        return Task.FromResult<IResult>(Results.Ok(ToSessionResponse(result)));
    }

    private static async Task<IResult> CreateOperatorSessionAsync(
        CreateProviderOperatorSessionRequest request,
        ProviderAccessSessionService sessions,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        var result = await sessions.CreateSessionFromCredentialsAsync(
            request.Email,
            request.Password,
            request.Scopes,
            request.ExpiresInMinutes,
            request.RecoveryCode,
            request.TotpCode,
            cancellationToken);

        await RecordAuditAsync(
            audit,
            result.IsSuccess
                ? ClientPortalAuditEventTypes.ProviderOperatorSessionIssued
                : ClientPortalAuditEventTypes.ProviderOperatorSessionRejected,
            NormalizeEmail(request.Email),
            result.IsSuccess ? result.Actor! : ClientPortalAuditActors.ControlCloud,
            result.IsSuccess
                ? "Provider operator session was issued."
                : result.Detail ?? "Provider operator session was rejected.",
            clock,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Json(
                new { code = result.FailureCode, detail = result.Detail },
                statusCode: result.StatusCode);
        }

        return Results.Ok(ToSessionResponse(result));
    }

    private static async Task<IResult> ChangeOperatorPasswordAsync(
        ChangeProviderOperatorPasswordRequest request,
        IProviderAccessOperatorStore operators,
        IClientPortalCredentialService credentials,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Results.Json(new
            {
                code = "ProviderCredentialsInvalid",
                detail = "Provider operator credentials are invalid."
            }, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorPasswordInvalid",
                detail = "Provider operator password must be at least 12 characters."
            });
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorPasswordUnchanged",
                detail = "New provider operator password must be different from the current password."
            });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var providerOperator = await operators.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (providerOperator is null
            || !providerOperator.Status.Equals(ProviderAccessOperatorStatuses.Active, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(providerOperator.PasswordHash)
            || !credentials.VerifyPassword(request.CurrentPassword, providerOperator.PasswordHash))
        {
            return Results.Json(new
            {
                code = "ProviderCredentialsInvalid",
                detail = "Provider operator credentials are invalid."
            }, statusCode: StatusCodes.Status401Unauthorized);
        }

        var actor = string.IsNullOrWhiteSpace(providerOperator.FullName)
            ? providerOperator.Email
            : providerOperator.FullName.Trim();
        providerOperator.PasswordHash = credentials.HashPassword(request.NewPassword);
        providerOperator.UpdatedAtUtc = clock.UtcNow;
        providerOperator.UpdatedBy = actor;

        await operators.SaveAsync(providerOperator, cancellationToken);
        await RecordAuditAsync(
            audit,
            ClientPortalAuditEventTypes.ProviderOperatorPasswordChanged,
            providerOperator.Email,
            actor,
            "Provider operator changed their password.",
            clock,
            cancellationToken);

        return Results.Ok(ToResponse(providerOperator));
    }

    private static async Task<IResult> ListOperatorsAsync(
        HttpRequest request,
        ProviderAccessSessionService sessions,
        IProviderAccessOperatorStore operators,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(
            request,
            ProviderAccessScopes.ProviderOperatorsManage);

        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var providerOperators = await operators.ListAsync(cancellationToken);

        return Results.Ok(new ProviderAccessOperatorsResponse(
            providerOperators.Select(ToResponse).ToArray()));
    }

    private static async Task<IResult> CreateOperatorAsync(
        CreateProviderOperatorRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService sessions,
        IProviderAccessOperatorStore operators,
        IClientPortalCredentialService credentials,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(
            httpRequest,
            ProviderAccessScopes.ProviderOperatorsManage);

        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var validationFailure = ValidateCreateOperatorRequest(request);

        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var existingOperator = await operators.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (existingOperator is not null)
        {
            return Results.Conflict(new
            {
                code = "ProviderOperatorAlreadyExists",
                detail = "A provider operator already exists for this email address."
            });
        }

        var now = clock.UtcNow;
        var actor = NormalizeActor(request.CreatedBy, authorization.Principal!.Actor);
        var providerOperator = new ProviderAccessOperator
        {
            UserId = Guid.NewGuid().ToString("N"),
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordHash = credentials.HashPassword(request.Password),
            Status = ProviderAccessOperatorStatuses.Active,
            Scopes = NormalizeScopes(request.Scopes).ToArray(),
            CreatedAtUtc = now,
            CreatedBy = actor
        };

        await operators.SaveAsync(providerOperator, cancellationToken);
        await RecordAuditAsync(
            audit,
            ClientPortalAuditEventTypes.ProviderOperatorCreated,
            providerOperator.Email,
            actor,
            "Provider operator was created.",
            clock,
            cancellationToken);

        return Results.Created(
            $"/api/v1/provider-access/operators/{Uri.EscapeDataString(providerOperator.UserId)}",
            ToResponse(providerOperator));
    }

    private static async Task<IResult> ResetOperatorPasswordAsync(
        string userId,
        ResetProviderOperatorPasswordRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService sessions,
        IProviderAccessOperatorStore operators,
        IClientPortalCredentialService credentials,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(
            httpRequest,
            ProviderAccessScopes.ProviderOperatorsManage);

        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorPasswordInvalid",
                detail = "Provider operator password must be at least 12 characters."
            });
        }

        var providerOperator = await operators.GetByUserIdAsync(userId, cancellationToken);

        if (providerOperator is null)
        {
            return OperatorNotFound();
        }

        var actor = NormalizeActor(request.UpdatedBy, authorization.Principal!.Actor);
        providerOperator.PasswordHash = credentials.HashPassword(request.Password);
        providerOperator.UpdatedAtUtc = clock.UtcNow;
        providerOperator.UpdatedBy = actor;

        await operators.SaveAsync(providerOperator, cancellationToken);
        await RecordAuditAsync(
            audit,
            ClientPortalAuditEventTypes.ProviderOperatorPasswordReset,
            providerOperator.Email,
            actor,
            "Provider operator password was reset.",
            clock,
            cancellationToken);

        return Results.Ok(ToResponse(providerOperator));
    }

    private static async Task<IResult> UpdateOperatorScopesAsync(
        string userId,
        UpdateProviderOperatorScopesRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService sessions,
        IProviderAccessOperatorStore operators,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(
            httpRequest,
            ProviderAccessScopes.ProviderOperatorsManage);

        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var scopes = NormalizeScopes(request.Scopes).ToArray();

        if (scopes.Length == 0)
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorScopesRequired",
                detail = "At least one provider operator scope is required."
            });
        }

        var unsupportedScopes = ProviderAccessScopes.FindUnsupported(scopes);

        if (unsupportedScopes.Count > 0)
        {
            return UnsupportedScopes(unsupportedScopes);
        }

        var providerOperator = await operators.GetByUserIdAsync(userId, cancellationToken);

        if (providerOperator is null)
        {
            return OperatorNotFound();
        }

        var actor = NormalizeActor(request.UpdatedBy, authorization.Principal!.Actor);
        providerOperator.Scopes = scopes;
        providerOperator.UpdatedAtUtc = clock.UtcNow;
        providerOperator.UpdatedBy = actor;

        await operators.SaveAsync(providerOperator, cancellationToken);
        await RecordAuditAsync(
            audit,
            ClientPortalAuditEventTypes.ProviderOperatorScopesUpdated,
            providerOperator.Email,
            actor,
            "Provider operator scopes were updated.",
            clock,
            cancellationToken);

        return Results.Ok(ToResponse(providerOperator));
    }

    private static async Task<IResult> UpdateOperatorStatusAsync(
        string userId,
        UpdateProviderOperatorStatusRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService sessions,
        IProviderAccessOperatorStore operators,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(
            httpRequest,
            ProviderAccessScopes.ProviderOperatorsManage);

        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        if (!ProviderAccessOperatorStatuses.IsSupported(request.Status))
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorStatusUnsupported",
                detail = "Provider operator status must be Active or Suspended."
            });
        }

        var providerOperator = await operators.GetByUserIdAsync(userId, cancellationToken);

        if (providerOperator is null)
        {
            return OperatorNotFound();
        }

        var actor = NormalizeActor(request.UpdatedBy, authorization.Principal!.Actor);
        providerOperator.Status = request.Status.Trim();
        providerOperator.UpdatedAtUtc = clock.UtcNow;
        providerOperator.UpdatedBy = actor;

        await operators.SaveAsync(providerOperator, cancellationToken);
        await RecordAuditAsync(
            audit,
            ClientPortalAuditEventTypes.ProviderOperatorStatusUpdated,
            providerOperator.Email,
            actor,
            "Provider operator status was updated.",
            clock,
            cancellationToken);

        return Results.Ok(ToResponse(providerOperator));
    }

    private static IResult? ValidateCreateOperatorRequest(CreateProviderOperatorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorEmailInvalid",
                detail = "Provider operator email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorNameRequired",
                detail = "Provider operator full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorPasswordInvalid",
                detail = "Provider operator password must be at least 12 characters."
            });
        }

        if (NormalizeScopes(request.Scopes).Count == 0)
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorScopesRequired",
                detail = "At least one provider operator scope is required."
            });
        }

        var unsupportedScopes = ProviderAccessScopes.FindUnsupported(request.Scopes);

        if (unsupportedScopes.Count > 0)
        {
            return UnsupportedScopes(unsupportedScopes);
        }

        return null;
    }

    private static async Task<IResult> ResetOperatorRecoveryCodesAsync(
        string userId,
        ResetProviderOperatorRecoveryCodesRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService sessions,
        IProviderAccessOperatorStore operators,
        IClientPortalCredentialService credentials,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(
            httpRequest,
            ProviderAccessScopes.ProviderOperatorsManage);

        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var count = request.Count ?? 10;

        if (count is < 1 or > 20)
        {
            return Results.BadRequest(new
            {
                code = "ProviderOperatorRecoveryCodeCountInvalid",
                detail = "Provider operator recovery code count must be between 1 and 20."
            });
        }

        var providerOperator = await operators.GetByUserIdAsync(userId, cancellationToken);

        if (providerOperator is null)
        {
            return OperatorNotFound();
        }

        var actor = NormalizeActor(request.UpdatedBy, authorization.Principal!.Actor);
        var recoveryCodes = Enumerable.Range(0, count)
            .Select(_ => CreateRecoveryCode())
            .ToArray();
        providerOperator.RecoveryCodeHashes = recoveryCodes
            .Select(code => credentials.HashSecret($"provider-access-recovery-code:{NormalizeRecoveryCode(code)}"))
            .ToArray();
        providerOperator.RecoveryCodesUpdatedAtUtc = clock.UtcNow;
        providerOperator.RecoveryCodesUpdatedBy = actor;
        providerOperator.LastRecoveryCodeUsedAtUtc = null;
        providerOperator.UpdatedAtUtc = clock.UtcNow;
        providerOperator.UpdatedBy = actor;

        await operators.SaveAsync(providerOperator, cancellationToken);
        await RecordAuditAsync(
            audit,
            ClientPortalAuditEventTypes.ProviderOperatorRecoveryCodesReset,
            providerOperator.Email,
            actor,
            "Provider operator recovery codes were reset.",
            clock,
            cancellationToken);

        return Results.Ok(new ProviderOperatorRecoveryCodesResponse(
            ToResponse(providerOperator),
            recoveryCodes));
    }

    private static async Task<IResult> ResetOperatorTotpAsync(
        string userId,
        ResetProviderOperatorTotpRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService sessions,
        IProviderAccessOperatorStore operators,
        IProviderAccessTotpSecretProtector totpSecrets,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(
            httpRequest,
            ProviderAccessScopes.ProviderOperatorsManage);

        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var providerOperator = await operators.GetByUserIdAsync(userId, cancellationToken);

        if (providerOperator is null)
        {
            return OperatorNotFound();
        }

        var actor = NormalizeActor(request.UpdatedBy, authorization.Principal!.Actor);
        var secret = ProviderAccessTotp.CreateSecret();

        providerOperator.TotpSecret = totpSecrets.Protect(secret);
        providerOperator.TotpEnabledAtUtc ??= clock.UtcNow;
        providerOperator.TotpUpdatedAtUtc = clock.UtcNow;
        providerOperator.TotpUpdatedBy = actor;
        providerOperator.LastTotpUsedAtUtc = null;
        providerOperator.LastTotpStep = null;
        providerOperator.UpdatedAtUtc = clock.UtcNow;
        providerOperator.UpdatedBy = actor;

        await operators.SaveAsync(providerOperator, cancellationToken);
        await RecordAuditAsync(
            audit,
            ClientPortalAuditEventTypes.ProviderOperatorTotpReset,
            providerOperator.Email,
            actor,
            "Provider operator TOTP enrollment was reset.",
            clock,
            cancellationToken);

        return Results.Ok(new ProviderOperatorTotpEnrollmentResponse(
            ToResponse(providerOperator),
            secret,
            ProviderAccessTotp.CreateOtpAuthUri(
                "SafarSuite Provider",
                providerOperator.Email,
                secret)));
    }

    private static IResult ToAuthorizationFailure(ProviderAccessAuthorizationResult authorization)
    {
        return Results.Json(
            new { code = authorization.FailureCode, detail = authorization.Detail },
            statusCode: authorization.StatusCode);
    }

    private static IResult OperatorNotFound()
    {
        return Results.NotFound(new
        {
            code = "ProviderOperatorNotFound",
            detail = "Provider operator was not found."
        });
    }

    private static IResult UnsupportedScopes(IReadOnlyCollection<string> scopes)
    {
        return Results.BadRequest(new
        {
            code = "ProviderOperatorScopesUnsupported",
            detail = FormatUnsupportedScopes(scopes)
        });
    }

    private static ProviderAccessSessionResponse ToSessionResponse(
        ProviderAccessSessionResult result)
    {
        return new ProviderAccessSessionResponse(
            result.AccessToken!,
            "Bearer",
            result.Actor!,
            result.Scopes!,
            result.ExpiresAtUtc!.Value);
    }

    private static ProviderAccessOperatorResponse ToResponse(
        ProviderAccessOperator providerOperator)
    {
        return new ProviderAccessOperatorResponse(
            providerOperator.UserId,
            providerOperator.Email,
            providerOperator.FullName,
            providerOperator.Status,
            providerOperator.Scopes,
            providerOperator.CreatedAtUtc,
            providerOperator.CreatedBy,
            providerOperator.UpdatedAtUtc,
            providerOperator.UpdatedBy,
            providerOperator.LastLoginAtUtc,
            providerOperator.RecoveryCodesUpdatedAtUtc is not null
                || !string.IsNullOrWhiteSpace(providerOperator.TotpSecret),
            !string.IsNullOrWhiteSpace(providerOperator.TotpSecret),
            providerOperator.TotpEnabledAtUtc,
            providerOperator.TotpUpdatedAtUtc,
            providerOperator.TotpUpdatedBy,
            providerOperator.LastTotpUsedAtUtc,
            providerOperator.RecoveryCodeHashes.Length,
            providerOperator.RecoveryCodesUpdatedAtUtc,
            providerOperator.RecoveryCodesUpdatedBy,
            providerOperator.LastRecoveryCodeUsedAtUtc,
            providerOperator.FailedLoginAttemptCount,
            providerOperator.LastFailedLoginAtUtc,
            providerOperator.LockoutEndsAtUtc);
    }

    private static async Task RecordAuditAsync(
        IClientPortalAuditRecorder audit,
        string eventType,
        string subjectEmail,
        string actor,
        string detail,
        IControlCloudClock clock,
        CancellationToken cancellationToken)
    {
        try
        {
            await audit.RecordAsync(
                new ClientPortalAuditRecord(
                    Guid.NewGuid(),
                    ClientId: null,
                    InvitationId: null,
                    UserId: null,
                    subjectEmail,
                    eventType,
                    actor,
                    detail,
                    clock.UtcNow),
                cancellationToken);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static IReadOnlyCollection<string> NormalizeScopes(IEnumerable<string>? scopes)
    {
        return (scopes ?? [])
            .Select(scope => scope.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string FormatUnsupportedScopes(IReadOnlyCollection<string> scopes)
    {
        return $"Unsupported provider operator scope(s): {string.Join(", ", scopes)}.";
    }

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? ""
            : email.Trim().ToLowerInvariant();
    }

    private static string NormalizeActor(string? requestedActor, string sessionActor)
    {
        var normalized = requestedActor?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? sessionActor
            : normalized;
    }

    private static string CreateRecoveryCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var characters = Enumerable.Range(0, 12)
            .Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)])
            .ToArray();

        return string.Join(
            "-",
            new string(characters, 0, 4),
            new string(characters, 4, 4),
            new string(characters, 8, 4));
    }

    private static string NormalizeRecoveryCode(string? recoveryCode)
    {
        if (string.IsNullOrWhiteSpace(recoveryCode))
        {
            return "";
        }

        return new string(recoveryCode
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(character => char.ToUpperInvariant(character))
            .ToArray());
    }

    public sealed record CreateProviderAccessSessionRequest(
        string SharedSecret,
        string? Actor,
        string[]? Scopes = null,
        int? ExpiresInMinutes = null);

    public sealed record CreateProviderOperatorSessionRequest(
        string Email,
        string Password,
        string[]? Scopes = null,
        int? ExpiresInMinutes = null,
        string? RecoveryCode = null,
        string? TotpCode = null);

    public sealed record ChangeProviderOperatorPasswordRequest(
        string Email,
        string CurrentPassword,
        string NewPassword);

    public sealed record ProviderAccessSessionResponse(
        string AccessToken,
        string TokenType,
        string Actor,
        IReadOnlyCollection<string> Scopes,
        DateTimeOffset ExpiresAtUtc);

    public sealed record ProviderAccessOperatorsResponse(
        IReadOnlyCollection<ProviderAccessOperatorResponse> Operators);

    public sealed record ProviderAccessOperatorResponse(
        string UserId,
        string Email,
        string FullName,
        string Status,
        IReadOnlyCollection<string> Scopes,
        DateTimeOffset CreatedAtUtc,
        string CreatedBy,
        DateTimeOffset? UpdatedAtUtc,
        string? UpdatedBy,
        DateTimeOffset? LastLoginAtUtc,
        bool MfaEnabled,
        bool TotpEnabled,
        DateTimeOffset? TotpEnabledAtUtc,
        DateTimeOffset? TotpUpdatedAtUtc,
        string? TotpUpdatedBy,
        DateTimeOffset? LastTotpUsedAtUtc,
        int RecoveryCodeCount,
        DateTimeOffset? RecoveryCodesUpdatedAtUtc,
        string? RecoveryCodesUpdatedBy,
        DateTimeOffset? LastRecoveryCodeUsedAtUtc,
        int FailedLoginAttemptCount,
        DateTimeOffset? LastFailedLoginAtUtc,
        DateTimeOffset? LockoutEndsAtUtc);

    public sealed record ProviderOperatorRecoveryCodesResponse(
        ProviderAccessOperatorResponse Operator,
        IReadOnlyCollection<string> RecoveryCodes);

    public sealed record ProviderOperatorTotpEnrollmentResponse(
        ProviderAccessOperatorResponse Operator,
        string Secret,
        string OtpAuthUri);

    public sealed record CreateProviderOperatorRequest(
        string Email,
        string FullName,
        string Password,
        string[] Scopes,
        string? CreatedBy = null);

    public sealed record ResetProviderOperatorPasswordRequest(
        string Password,
        string? UpdatedBy = null);

    public sealed record ResetProviderOperatorRecoveryCodesRequest(
        int? Count = null,
        string? UpdatedBy = null);

    public sealed record ResetProviderOperatorTotpRequest(
        string? UpdatedBy = null);

    public sealed record UpdateProviderOperatorScopesRequest(
        string[] Scopes,
        string? UpdatedBy = null);

    public sealed record UpdateProviderOperatorStatusRequest(
        string Status,
        string? UpdatedBy = null);
}
