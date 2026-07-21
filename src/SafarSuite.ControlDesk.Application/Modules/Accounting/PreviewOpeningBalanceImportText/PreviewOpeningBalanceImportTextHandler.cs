using System.Globalization;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;

public sealed class PreviewOpeningBalanceImportTextHandler
{
    private readonly PreviewOpeningBalanceImportHandler _previewOpeningBalanceImport;

    public PreviewOpeningBalanceImportTextHandler(
        PreviewOpeningBalanceImportHandler previewOpeningBalanceImport)
    {
        _previewOpeningBalanceImport = previewOpeningBalanceImport;
    }

    public async Task<Result<PreviewOpeningBalanceImportTextResult>> HandleAsync(
        PreviewOpeningBalanceImportTextCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImportText))
        {
            return Result<PreviewOpeningBalanceImportTextResult>.Failure(ApplicationError.Validation(
                nameof(command.ImportText),
                "Opening balance import text is required."));
        }

        var parseResult = ParseImportText(command.ImportText, command.Delimiter);
        var previewResult = await _previewOpeningBalanceImport.HandleAsync(
            new PreviewOpeningBalanceImportCommand(
                command.EntryDate,
                command.CurrencyCode,
                command.SourceReference,
                command.Memo,
                command.ProfileFromDate,
                command.ProfileToDate,
                command.ProfileStatus,
                command.TransactionsAllowed,
                command.ProfitAndLossCarryForwardAccountId,
                parseResult.Lines),
            cancellationToken);

        if (previewResult.IsFailure)
        {
            return Result<PreviewOpeningBalanceImportTextResult>.Failure(previewResult.Errors);
        }

        var preview = previewResult.Value;

        if (parseResult.ParseIssues.Count > 0)
        {
            var blockers = preview.Blockers
                .Concat([$"{parseResult.ParseIssues.Count} opening balance import text issue(s) must be fixed."])
                .ToArray();

            preview = preview with
            {
                CanPost = false,
                Blockers = blockers
            };
        }

        return Result<PreviewOpeningBalanceImportTextResult>.Success(new PreviewOpeningBalanceImportTextResult(
            parseResult.Format,
            parseResult.Lines.Count,
            parseResult.IgnoredLineCount,
            parseResult.ParseIssues,
            preview));
    }

    private static OpeningBalanceImportTextParseResult ParseImportText(
        string importText,
        string? delimiter)
    {
        var rows = importText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var firstDataLine = rows.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row)) ?? string.Empty;
        var delimiterChar = ResolveDelimiter(delimiter, firstDataLine);
        var delimiterName = delimiterChar switch
        {
            '\t' => "Tab",
            '|' => "Pipe",
            ',' => "Comma",
            _ => delimiterChar.ToString()
        };
        var lines = new List<PreviewOpeningBalanceImportLineCommand>();
        var parseIssues = new List<OpeningBalanceImportTextParseIssueResult>();
        var ignoredLineCount = 0;
        ColumnMap? headerMap = null;
        var hasSeenContent = false;

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var lineNumber = rowIndex + 1;
            var row = rows[rowIndex];

            if (string.IsNullOrWhiteSpace(row))
            {
                ignoredLineCount++;
                continue;
            }

            var cells = SplitDelimitedRow(row, delimiterChar);

            if (!hasSeenContent && TryCreateHeaderMap(cells, out var mappedHeader))
            {
                headerMap = mappedHeader;
                hasSeenContent = true;
                ignoredLineCount++;
                continue;
            }

            hasSeenContent = true;
            var map = headerMap ?? ColumnMap.Default;
            var accountCode = GetCell(cells, map.AccountCodeIndex);
            var description = GetCell(cells, map.DescriptionIndex);
            var debit = ParseAmount(
                GetCell(cells, map.DebitIndex),
                lineNumber,
                "Debit",
                parseIssues);
            var credit = ParseAmount(
                GetCell(cells, map.CreditIndex),
                lineNumber,
                "Credit",
                parseIssues);

            lines.Add(new PreviewOpeningBalanceImportLineCommand(
                accountCode,
                debit,
                credit,
                description));
        }

        var format = headerMap is null
            ? delimiterName
            : $"{delimiterName} / {headerMap.FormatName}";

        return new OpeningBalanceImportTextParseResult(
            format,
            ignoredLineCount,
            lines,
            parseIssues);
    }

    private static char ResolveDelimiter(string? delimiter, string sample)
    {
        if (!string.IsNullOrWhiteSpace(delimiter))
        {
            var normalized = delimiter.Trim().ToLowerInvariant();

            return normalized switch
            {
                "tab" or "\\t" => '\t',
                "pipe" => '|',
                "comma" or "csv" => ',',
                _ when normalized.Length == 1 => normalized[0],
                _ => ','
            };
        }

        var candidates = new[]
            {
                (Value: '\t', Count: sample.Count(character => character == '\t')),
                (Value: '|', Count: sample.Count(character => character == '|')),
                (Value: ',', Count: sample.Count(character => character == ','))
            }
            .OrderByDescending(candidate => candidate.Count)
            .ToArray();
        var best = candidates.First();

        return best.Count == 0 ? ',' : best.Value;
    }

    private static IReadOnlyCollection<string> SplitDelimitedRow(string row, char delimiter)
    {
        var cells = new List<string>();
        var current = new List<char>();
        var isQuoted = false;

        for (var index = 0; index < row.Length; index++)
        {
            var character = row[index];

            if (character == '"')
            {
                if (isQuoted && index + 1 < row.Length && row[index + 1] == '"')
                {
                    current.Add('"');
                    index++;
                    continue;
                }

                isQuoted = !isQuoted;
                continue;
            }

            if (character == delimiter && !isQuoted)
            {
                cells.Add(new string(current.ToArray()).Trim());
                current.Clear();
                continue;
            }

            current.Add(character);
        }

        cells.Add(new string(current.ToArray()).Trim());

        return cells;
    }

    private static bool TryCreateHeaderMap(
        IReadOnlyCollection<string> cells,
        out ColumnMap columnMap)
    {
        var normalizedCells = cells
            .Select((cell, index) => (Name: NormalizeHeader(cell), Index: index))
            .ToArray();
        var accountIndex = FindHeaderIndex(
            normalizedCells,
            "accountcode",
            "account",
            "code",
            "glcode",
            "acccode",
            "coa3code",
            "dcocoa3code",
            "dvhcoa3code",
            "rptcoa3code");
        var debitIndex = FindHeaderIndex(
            normalizedCells,
            "debit",
            "dr",
            "damt",
            "adamt",
            "dbtamt",
            "dcodbtamt",
            "baldbtamt",
            "opndbtamt");
        var creditIndex = FindHeaderIndex(
            normalizedCells,
            "credit",
            "cr",
            "camt",
            "acamt",
            "crdamt",
            "dcocrdamt",
            "balcrdamt",
            "opncrdamt");

        if (accountIndex < 0 || debitIndex < 0 || creditIndex < 0)
        {
            columnMap = ColumnMap.Default;
            return false;
        }

        columnMap = new ColumnMap(
            accountIndex,
            debitIndex,
            creditIndex,
            FindHeaderIndex(normalizedCells, "description", "memo", "narration", "remarks", "remark", "drem", "mcoremark"),
            ResolveFormatName(normalizedCells));
        return true;
    }

    private static string ResolveFormatName(IReadOnlyCollection<(string Name, int Index)> cells)
    {
        var names = cells.Select(cell => cell.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (names.Contains("dcocoa3code") || names.Contains("dcodbtamt") || names.Contains("dcocrdamt"))
        {
            return "ACT_SD_COA_OPNBAL";
        }

        if (names.Contains("acccode") || names.Contains("damt") || names.Contains("camt"))
        {
            return "OP_BAL";
        }

        return "Standard";
    }

    private static int FindHeaderIndex(
        IEnumerable<(string Name, int Index)> cells,
        params string[] names)
    {
        return cells
            .Where(cell => names.Contains(cell.Name, StringComparer.OrdinalIgnoreCase))
            .Select(cell => cell.Index)
            .DefaultIfEmpty(-1)
            .First();
    }

    private static string NormalizeHeader(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string GetCell(IReadOnlyCollection<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count)
        {
            return string.Empty;
        }

        return cells.ElementAt(index).Trim();
    }

    private static decimal ParseAmount(
        string value,
        int lineNumber,
        string column,
        List<OpeningBalanceImportTextParseIssueResult> parseIssues)
    {
        var normalized = value.Trim();

        if (normalized.Length == 0)
        {
            return 0m;
        }

        normalized = normalized.Replace(",", string.Empty, StringComparison.Ordinal);

        if (decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var amount))
        {
            return decimal.Round(amount, 2);
        }

        parseIssues.Add(new OpeningBalanceImportTextParseIssueResult(
            lineNumber,
            column,
            $"{column} amount is not a valid decimal value.",
            value));

        return 0m;
    }

    private sealed record ColumnMap(
        int AccountCodeIndex,
        int DebitIndex,
        int CreditIndex,
        int DescriptionIndex,
        string FormatName)
    {
        public static ColumnMap Default { get; } = new(0, 1, 2, 3, "Standard");
    }

    private sealed record OpeningBalanceImportTextParseResult(
        string Format,
        int IgnoredLineCount,
        IReadOnlyCollection<PreviewOpeningBalanceImportLineCommand> Lines,
        IReadOnlyCollection<OpeningBalanceImportTextParseIssueResult> ParseIssues);
}
