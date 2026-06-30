namespace SafarSuite.ControlDesk.Application.Modules.Clients;

public sealed record ClientAccountingProfileResult(
    Guid ClientId,
    Guid AccountsReceivableAccountId,
    string DefaultCurrencyCode,
    string? CloudCustomerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
