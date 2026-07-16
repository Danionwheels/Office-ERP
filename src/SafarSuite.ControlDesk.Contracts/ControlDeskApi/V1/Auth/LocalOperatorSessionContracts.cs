namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Auth;

public sealed record CreateLocalOperatorSessionRequest(
    string Email,
    string Password,
    int? ExpiresInMinutes = null);

public sealed record LocalOperatorSessionResponse(
    string AccessToken,
    string TokenType,
    string Actor,
    string? Email,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset ExpiresAtUtc);
