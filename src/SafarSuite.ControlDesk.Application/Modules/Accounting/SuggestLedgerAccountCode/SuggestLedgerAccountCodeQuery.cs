namespace SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;

public sealed record SuggestLedgerAccountCodeQuery(string Role, string? CompanyCode = null);
