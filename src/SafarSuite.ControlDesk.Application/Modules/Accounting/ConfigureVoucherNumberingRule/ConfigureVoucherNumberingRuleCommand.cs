namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureVoucherNumberingRule;

public sealed record ConfigureVoucherNumberingRuleCommand(
    string? CompanyCode,
    string SourceType,
    string Prefix,
    int NumberPaddingWidth,
    bool IsActive);
