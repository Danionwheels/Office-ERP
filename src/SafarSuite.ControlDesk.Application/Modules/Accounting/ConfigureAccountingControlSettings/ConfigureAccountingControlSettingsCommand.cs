namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountingControlSettings;

public sealed record ConfigureAccountingControlSettingsCommand(
    string? CompanyCode,
    string BaseCurrencyCode,
    Guid? RetainedEarningsAccountId,
    Guid? IncomeSummaryAccountId,
    Guid? RoundingAccountId);
