namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetProfitAndLossStatement;

public sealed record GetProfitAndLossStatementQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? CurrencyCode);
