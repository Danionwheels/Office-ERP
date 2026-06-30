namespace SafarSuite.ControlDesk.Application.Modules.Clients;

public sealed record ClientContactResult(
    Guid ClientContactId,
    string Role,
    string FullName,
    string? JobTitle,
    string? Email,
    string? Phone,
    bool IsPrimary,
    DateTimeOffset CreatedAtUtc);
