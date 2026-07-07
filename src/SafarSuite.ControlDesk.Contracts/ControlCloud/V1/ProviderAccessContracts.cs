namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ProviderAccessOperatorsResponse(
    IReadOnlyCollection<ProviderAccessOperatorResponse> Operators);

public sealed record CreateProviderOperatorSessionRequest(
    string Email,
    string Password,
    string[]? Scopes = null,
    int? ExpiresInMinutes = null,
    string? RecoveryCode = null);

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
    int RecoveryCodeCount,
    DateTimeOffset? RecoveryCodesUpdatedAtUtc,
    string? RecoveryCodesUpdatedBy,
    DateTimeOffset? LastRecoveryCodeUsedAtUtc);

public sealed record ProviderOperatorRecoveryCodesResponse(
    ProviderAccessOperatorResponse Operator,
    IReadOnlyCollection<string> RecoveryCodes);

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

public sealed record UpdateProviderOperatorScopesRequest(
    string[] Scopes,
    string? UpdatedBy = null);

public sealed record UpdateProviderOperatorStatusRequest(
    string Status,
    string? UpdatedBy = null);
