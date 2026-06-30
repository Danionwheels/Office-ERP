namespace SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;

public sealed record ConfigureClientAccountingProfileCommand(
    Guid ClientId,
    Guid AccountsReceivableAccountId,
    string DefaultCurrencyCode,
    string? CloudCustomerId);
