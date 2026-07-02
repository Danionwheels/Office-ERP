namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;

public sealed record ApplyClientCreditCommand(
    Guid ClientId,
    Guid InvoiceId,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly AppliedOn,
    string? Note);
