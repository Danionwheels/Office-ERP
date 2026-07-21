using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListVoucherNumberingRules;

public sealed class ListVoucherNumberingRulesHandler
{
    private readonly IVoucherNumberingRuleRepository _rules;

    public ListVoucherNumberingRulesHandler(IVoucherNumberingRuleRepository rules)
    {
        _rules = rules;
    }

    public async Task<Result<ListVoucherNumberingRulesResult>> HandleAsync(
        ListVoucherNumberingRulesQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<ListVoucherNumberingRulesResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        var configuredRules = (await _rules.ListByCompanyAsync(companyCode, cancellationToken))
            .ToDictionary(rule => rule.SourceType);
        var results = VoucherNumberingDefaults.All
            .Select(defaultRule => configuredRules.TryGetValue(defaultRule.SourceType, out var configuredRule)
                ? ToResult(configuredRule, isConfigured: true)
                : ToDefaultResult(companyCode, defaultRule))
            .OrderBy(rule => ParseSourceType(rule.SourceType))
            .ToArray();

        return Result<ListVoucherNumberingRulesResult>.Success(new ListVoucherNumberingRulesResult(
            companyCode,
            results));
    }

    public static VoucherNumberingRuleResult ToResult(
        VoucherNumberingRule rule,
        bool isConfigured = true)
    {
        return new VoucherNumberingRuleResult(
            rule.CompanyCode,
            rule.SourceType.ToString(),
            rule.Prefix,
            rule.NumberPaddingWidth,
            rule.IsActive,
            isConfigured,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc);
    }

    private static VoucherNumberingRuleResult ToDefaultResult(
        string companyCode,
        VoucherNumberingRuleDefault defaultRule)
    {
        return new VoucherNumberingRuleResult(
            companyCode,
            defaultRule.SourceType.ToString(),
            defaultRule.Prefix,
            defaultRule.NumberPaddingWidth,
            true,
            false,
            null,
            null);
    }

    private static JournalSourceType ParseSourceType(string sourceType)
    {
        return Enum.TryParse<JournalSourceType>(sourceType, out var parsed)
            ? parsed
            : JournalSourceType.Manual;
    }
}
