namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;

public sealed record ListAccountingPeriodsQuery(
    string? CompanyCode = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);
