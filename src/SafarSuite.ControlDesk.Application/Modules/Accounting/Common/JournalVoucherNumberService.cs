using System.Globalization;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed class JournalVoucherNumberService
{
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IVoucherNumberingRuleRepository _voucherNumberingRules;

    public JournalVoucherNumberService(
        IJournalEntryRepository journalEntries,
        IVoucherNumberingRuleRepository voucherNumberingRules)
    {
        _journalEntries = journalEntries;
        _voucherNumberingRules = voucherNumberingRules;
    }

    public async Task<JournalVoucherNumberPreview> PreviewNextAsync(
        JournalSourceType sourceType,
        DateOnly entryDate,
        CancellationToken cancellationToken = default)
    {
        var rule = await ResolveRuleAsync(sourceType, cancellationToken);
        var prefix = rule.Prefix;
        var numberPaddingWidth = rule.NumberPaddingWidth;
        var sequenceYear = entryDate.Year;
        var maximumSequence = await _journalEntries.GetMaximumVoucherSequenceAsync(
            sourceType,
            prefix,
            sequenceYear,
            cancellationToken);
        var nextSequence = maximumSequence + 1;

        return new JournalVoucherNumberPreview(
            sourceType.ToString(),
            entryDate,
            prefix,
            sequenceYear,
            nextSequence,
            numberPaddingWidth,
            FormatReference(prefix, sequenceYear, nextSequence, numberPaddingWidth));
    }

    private async Task<VoucherNumberingResolvedRule> ResolveRuleAsync(
        JournalSourceType sourceType,
        CancellationToken cancellationToken)
    {
        var configuredRule = await _voucherNumberingRules.GetByCompanyAndSourceTypeAsync(
            AccountingSetupDefaults.DefaultCompanyCode,
            sourceType,
            cancellationToken);

        if (configuredRule is not null && configuredRule.IsActive)
        {
            return new VoucherNumberingResolvedRule(
                configuredRule.Prefix,
                configuredRule.NumberPaddingWidth);
        }

        var defaultRule = VoucherNumberingDefaults.GetDefault(sourceType);

        return new VoucherNumberingResolvedRule(
            defaultRule.Prefix,
            defaultRule.NumberPaddingWidth);
    }

    private static string FormatReference(
        string prefix,
        int sequenceYear,
        int sequence,
        int numberPaddingWidth)
    {
        var sequenceText = sequence.ToString(
            new string('0', numberPaddingWidth),
            CultureInfo.InvariantCulture);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix}-{sequenceYear:0000}-{sequenceText}");
    }

}

public sealed record JournalVoucherNumberPreview(
    string SourceType,
    DateOnly EntryDate,
    string Prefix,
    int SequenceYear,
    int NextSequence,
    int NumberPaddingWidth,
    string Reference);

internal sealed record VoucherNumberingResolvedRule(
    string Prefix,
    int NumberPaddingWidth);
