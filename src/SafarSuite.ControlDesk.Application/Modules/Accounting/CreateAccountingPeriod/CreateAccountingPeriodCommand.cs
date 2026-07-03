namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateAccountingPeriod;

public sealed record CreateAccountingPeriodCommand(
    string? CompanyCode,
    string? Name,
    DateOnly StartsOn,
    DateOnly EndsOn);
