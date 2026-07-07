namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewChartOfAccountsImportText;

public sealed record PreviewChartOfAccountsImportTextCommand(
    string? CompanyCode,
    string ImportText,
    string? Delimiter);
