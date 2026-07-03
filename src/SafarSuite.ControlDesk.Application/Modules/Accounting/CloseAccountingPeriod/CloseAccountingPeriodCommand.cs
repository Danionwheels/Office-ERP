namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CloseAccountingPeriod;

public sealed record CloseAccountingPeriodCommand(Guid AccountingPeriodId, string? ClosedBy = null);
