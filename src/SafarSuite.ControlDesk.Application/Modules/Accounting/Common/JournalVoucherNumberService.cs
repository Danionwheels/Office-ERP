using System.Globalization;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed class JournalVoucherNumberService
{
    private readonly IJournalEntryRepository _journalEntries;

    public JournalVoucherNumberService(IJournalEntryRepository journalEntries)
    {
        _journalEntries = journalEntries;
    }

    public async Task<JournalVoucherNumberPreview> PreviewNextAsync(
        JournalSourceType sourceType,
        DateOnly entryDate,
        CancellationToken cancellationToken = default)
    {
        var prefix = GetPrefix(sourceType);
        var sequenceYear = entryDate.Year;
        var yearStart = new DateOnly(sequenceYear, 1, 1);
        var yearEnd = new DateOnly(sequenceYear, 12, 31);
        var entries = await _journalEntries.ListAsync(
            yearStart,
            yearEnd,
            sourceType,
            cancellationToken);
        var nextSequence = entries
            .Select(entry => TryParseSequence(entry.SourceReference, prefix, sequenceYear))
            .Where(sequence => sequence.HasValue)
            .Select(sequence => sequence!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return new JournalVoucherNumberPreview(
            sourceType.ToString(),
            entryDate,
            prefix,
            sequenceYear,
            nextSequence,
            FormatReference(prefix, sequenceYear, nextSequence));
    }

    private static string FormatReference(string prefix, int sequenceYear, int sequence)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix}-{sequenceYear:0000}-{sequence:0000}");
    }

    private static string GetPrefix(JournalSourceType sourceType)
    {
        return sourceType switch
        {
            JournalSourceType.Manual => "MJ",
            JournalSourceType.OpeningBalance => "OB",
            JournalSourceType.Adjustment => "ADJ",
            JournalSourceType.ManualReversal => "MR",
            JournalSourceType.PeriodClose => "CL",
            JournalSourceType.PeriodCloseReversal => "CR",
            _ => sourceType.ToString().ToUpperInvariant()
        };
    }

    private static int? TryParseSequence(string? sourceReference, string prefix, int sequenceYear)
    {
        if (string.IsNullOrWhiteSpace(sourceReference))
        {
            return null;
        }

        var parts = sourceReference.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3
            || !string.Equals(parts[0], prefix, StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var referenceYear)
            || referenceYear != sequenceYear
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
        {
            return null;
        }

        return sequence;
    }
}

public sealed record JournalVoucherNumberPreview(
    string SourceType,
    DateOnly EntryDate,
    string Prefix,
    int SequenceYear,
    int NextSequence,
    string Reference);
