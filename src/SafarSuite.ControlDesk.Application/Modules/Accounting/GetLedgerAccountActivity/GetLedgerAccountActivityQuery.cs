namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;

public sealed record GetLedgerAccountActivityQuery(
    Guid LedgerAccountId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);
