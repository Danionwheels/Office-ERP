namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

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
    DateTimeOffset? LastLoginAtUtc);

public sealed record CreateProviderOperatorRequest(
    string Email,
    string FullName,
    string Password,
    string[] Scopes,
    string? CreatedBy = null);

public sealed record ResetProviderOperatorPasswordRequest(
    string Password,
    string? UpdatedBy = null);

public sealed record UpdateProviderOperatorScopesRequest(
    string[] Scopes,
    string? UpdatedBy = null);

public sealed record UpdateProviderOperatorStatusRequest(
    string Status,
    string? UpdatedBy = null);
