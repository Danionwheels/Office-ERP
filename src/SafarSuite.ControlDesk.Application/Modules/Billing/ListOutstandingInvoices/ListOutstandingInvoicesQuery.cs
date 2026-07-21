namespace SafarSuite.ControlDesk.Application.Modules.Billing.ListOutstandingInvoices;

public sealed record ListOutstandingInvoicesQuery(
    Guid? ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal? MinAmount,
    decimal? MaxAmount,
    string? Status,
    string? CurrencyCode,
    int Take,
    string? Cursor);
