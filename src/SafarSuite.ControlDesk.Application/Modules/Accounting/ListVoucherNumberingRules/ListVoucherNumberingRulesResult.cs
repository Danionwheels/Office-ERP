namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListVoucherNumberingRules;

public sealed record ListVoucherNumberingRulesResult(
    string CompanyCode,
    IReadOnlyCollection<VoucherNumberingRuleResult> Rules);

public sealed record VoucherNumberingRuleResult(
    string CompanyCode,
    string SourceType,
    string Prefix,
    int NumberPaddingWidth,
    bool IsActive,
    bool IsConfigured,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
