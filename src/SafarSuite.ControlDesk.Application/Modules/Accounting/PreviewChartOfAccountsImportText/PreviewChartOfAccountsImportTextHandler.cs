using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewChartOfAccountsImportText;

public sealed class PreviewChartOfAccountsImportTextHandler
{
    private const string ErrorSeverity = "Error";
    private const string WarningSeverity = "Warning";

    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IAccountCodeRangeRepository _accountCodeRanges;
    private readonly AccountingSetupDefaults _defaults;

    public PreviewChartOfAccountsImportTextHandler(
        ILedgerAccountRepository ledgerAccounts,
        IAccountCodeRangeRepository accountCodeRanges,
        AccountingSetupDefaults defaults)
    {
        _ledgerAccounts = ledgerAccounts;
        _accountCodeRanges = accountCodeRanges;
        _defaults = defaults;
    }

    public async Task<Result<PreviewChartOfAccountsImportTextResult>> HandleAsync(
        PreviewChartOfAccountsImportTextCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImportText))
        {
            return Result<PreviewChartOfAccountsImportTextResult>.Failure(ApplicationError.Validation(
                nameof(command.ImportText),
                "Chart of accounts import text is required."));
        }

        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<PreviewChartOfAccountsImportTextResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var parseResult = ParseImportText(command.ImportText, command.Delimiter);
        var activeRanges = (await _accountCodeRanges.ListByCompanyAsync(companyCode, cancellationToken))
            .Where(range => range.IsActive)
            .ToArray();
        var existingAccounts = (await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken))
            .ToArray();
        var existingByCode = existingAccounts.ToDictionary(
            account => account.Code.Value,
            StringComparer.OrdinalIgnoreCase);
        var importedByCode = parseResult.Rows
            .Where(row => !string.IsNullOrWhiteSpace(row.Code))
            .GroupBy(row => row.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var duplicateCodes = parseResult.Rows
            .Where(row => !string.IsNullOrWhiteSpace(row.Code))
            .GroupBy(row => row.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = parseResult.Rows
            .Select(row => PreviewRow(row, activeRanges, existingByCode, importedByCode, duplicateCodes))
            .ToArray();
        var warningCount = rows.Sum(row =>
            row.Issues.Count(issue => issue.Severity.Equals(WarningSeverity, StringComparison.OrdinalIgnoreCase)));
        var issueCount = rows.Sum(row => row.Issues.Count) + parseResult.ParseIssues.Count;
        var rejectCount = rows.Count(row => row.Action == "Reject");

        return Result<PreviewChartOfAccountsImportTextResult>.Success(
            new PreviewChartOfAccountsImportTextResult(
                companyCode,
                parseResult.Format,
                rows.Length,
                parseResult.IgnoredLineCount,
                rows.Length > 0 && rejectCount == 0 && parseResult.ParseIssues.Count == 0,
                rows.Count(row => row.Action == "Insert"),
                rows.Count(row => row.Action == "Update"),
                rows.Count(row => row.Action == "NoChange"),
                rejectCount,
                warningCount,
                issueCount,
                parseResult.ParseIssues,
                rows));
    }

    private static ChartOfAccountsImportRowResult PreviewRow(
        ParsedChartOfAccountsImportRow row,
        IReadOnlyCollection<AccountCodeRange> ranges,
        IReadOnlyDictionary<string, LedgerAccount> existingByCode,
        IReadOnlyDictionary<string, ParsedChartOfAccountsImportRow> importedByCode,
        IReadOnlySet<string> duplicateCodes)
    {
        var issues = new List<ChartOfAccountsImportIssueResult>();
        AccountCodeRange? matchedRange = null;
        LedgerAccount? existingAccount = null;
        LedgerAccountType? explicitAccountType = null;
        NormalBalance? explicitNormalBalance = null;
        LedgerAccountLevel? explicitLevel = null;
        LedgerAccountLevel? resolvedLevel = null;
        var resolvedType = LedgerAccountType.Asset;
        var resolvedNormalBalance = NormalBalance.Debit;
        var resolvedPosting = false;
        var parentCode = row.ParentCode;
        Guid? parentAccountId = null;
        string? parentSource = null;

        if (string.IsNullOrWhiteSpace(row.Code))
        {
            AddIssue(issues, ErrorSeverity, "MissingCode", "Account code is required.");
        }
        else if (!row.Code.All(char.IsDigit))
        {
            AddIssue(issues, ErrorSeverity, "InvalidCode", "Account code must contain digits only.");
        }
        else
        {
            try
            {
                _ = LedgerAccountCode.Create(row.Code);
            }
            catch (ArgumentException exception)
            {
                AddIssue(issues, ErrorSeverity, "InvalidCode", exception.Message);
            }

            if (duplicateCodes.Contains(row.Code))
            {
                AddIssue(issues, ErrorSeverity, "DuplicateImportCode", "Account code appears more than once in the import.");
            }

            matchedRange = FindRange(row.Code, ranges);

            if (matchedRange is null)
            {
                AddIssue(issues, ErrorSeverity, "OutsideSetupRange", "Account code is outside the active accounting setup ranges.");
            }
            else
            {
                resolvedType = matchedRange.AccountType;
                resolvedNormalBalance = matchedRange.NormalBalance;
            }

            existingByCode.TryGetValue(row.Code, out existingAccount);
        }

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            AddIssue(issues, ErrorSeverity, "MissingName", "Account name is required.");
        }

        if (!string.IsNullOrWhiteSpace(row.AccountType))
        {
            if (TryParseAccountType(row.AccountType, out var parsedAccountType))
            {
                explicitAccountType = parsedAccountType;
            }
            else
            {
                AddIssue(issues, ErrorSeverity, "InvalidAccountType", $"Account type '{row.AccountType}' is not supported.");
            }
        }

        if (explicitAccountType.HasValue)
        {
            if (matchedRange is not null && explicitAccountType.Value != matchedRange.AccountType)
            {
                AddIssue(
                    issues,
                    ErrorSeverity,
                    "AccountTypeConflict",
                    $"Account type must be {matchedRange.AccountType} for range {matchedRange.DisplayName}.");
            }

            resolvedType = explicitAccountType.Value;
        }

        if (!string.IsNullOrWhiteSpace(row.NormalBalance))
        {
            if (TryParseNormalBalance(row.NormalBalance, out var parsedNormalBalance))
            {
                explicitNormalBalance = parsedNormalBalance;
            }
            else
            {
                AddIssue(issues, ErrorSeverity, "InvalidNormalBalance", $"Normal balance '{row.NormalBalance}' is not supported.");
            }
        }

        if (explicitNormalBalance.HasValue)
        {
            if (matchedRange is not null && explicitNormalBalance.Value != matchedRange.NormalBalance)
            {
                AddIssue(
                    issues,
                    ErrorSeverity,
                    "NormalBalanceConflict",
                    $"Normal balance must be {matchedRange.NormalBalance} for range {matchedRange.DisplayName}.");
            }

            resolvedNormalBalance = explicitNormalBalance.Value;
        }

        if (!string.IsNullOrWhiteSpace(row.ImportedLevel))
        {
            if (TryParseLevel(row.ImportedLevel, out var parsedLevel))
            {
                explicitLevel = parsedLevel;
            }
            else
            {
                AddIssue(issues, ErrorSeverity, "InvalidLevel", $"Account level '{row.ImportedLevel}' is not supported.");
            }
        }

        resolvedLevel = explicitLevel ?? DetermineDefaultLevel(matchedRange);
        resolvedPosting = IsPostingLevel(resolvedLevel.Value);

        if (matchedRange is not null && matchedRange.IsPostingAccount != resolvedPosting)
        {
            AddIssue(
                issues,
                ErrorSeverity,
                "PostingRangeConflict",
                $"Posting behavior must be {matchedRange.IsPostingAccount} for range {matchedRange.DisplayName}.");
        }

        if (!string.IsNullOrWhiteSpace(row.PostingFlag))
        {
            if (!TryParseBoolean(row.PostingFlag, out var importedPosting))
            {
                AddIssue(issues, ErrorSeverity, "InvalidPostingFlag", $"Posting flag '{row.PostingFlag}' is not supported.");
            }
            else if (importedPosting != resolvedPosting)
            {
                AddIssue(
                    issues,
                    ErrorSeverity,
                    "PostingLevelConflict",
                    $"{resolvedLevel} accounts must be {(resolvedPosting ? "posting" : "non-posting")} accounts.");
            }
        }

        parentCode ??= matchedRange?.ParentCode;

        if (parentCode is null
            && !string.IsNullOrWhiteSpace(row.Code)
            && row.Code.Length > 5
            && row.Code.All(char.IsDigit))
        {
            parentCode = row.Code[..5];
        }

        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            if (string.Equals(parentCode, row.Code, StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(issues, ErrorSeverity, "SelfParent", "Parent account code cannot be the same as the account code.");
            }
            else if (existingByCode.TryGetValue(parentCode, out var parentAccount))
            {
                parentAccountId = parentAccount.Id.Value;
                parentSource = "Existing";
                AddParentReviewIssues(issues, resolvedLevel.Value, parentAccount.Level);
            }
            else if (importedByCode.ContainsKey(parentCode))
            {
                parentSource = "Import";
                AddIssue(
                    issues,
                    WarningSeverity,
                    "ParentImportPending",
                    $"Parent account {parentCode} is present in the same import preview.");
            }
            else
            {
                AddIssue(issues, ErrorSeverity, "MissingParent", $"Parent account {parentCode} was not found.");
            }

            if (!IsPostingLevel(resolvedLevel.Value))
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "NestedNonPostingReview",
                    $"{resolvedLevel} is non-posting and nested; rollup behavior should be reviewed before posting an import.");
            }
        }
        else if (resolvedLevel == LedgerAccountLevel.Subsidiary)
        {
            AddIssue(issues, ErrorSeverity, "MissingParent", "Subsidiary accounts require a parent account.");
        }

        if (!string.IsNullOrWhiteSpace(row.CurrencyCode))
        {
            if (!IsCurrencyCode(row.CurrencyCode))
            {
                AddIssue(issues, WarningSeverity, "CurrencyReview", $"Currency '{row.CurrencyCode}' should be a 3-letter code.");
            }
            else
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "CurrencyPreviewOnly",
                    $"Currency {row.CurrencyCode} is captured in preview; account currency persistence is not modeled yet.");
            }
        }

        var hasErrors = issues.Any(issue =>
            issue.Severity.Equals(ErrorSeverity, StringComparison.OrdinalIgnoreCase));

        if (existingAccount is not null && !hasErrors)
        {
            if (existingAccount.Type != resolvedType
                || existingAccount.NormalBalance != resolvedNormalBalance
                || existingAccount.Level != resolvedLevel
                || existingAccount.IsPostingAccount != resolvedPosting
                || ParentAccountChanged(existingAccount, parentAccountId, parentSource))
            {
                AddIssue(
                    issues,
                    ErrorSeverity,
                    "ExistingShapeConflict",
                    "Existing account has a different type, balance, level, posting flag, or parent. Review before changing immutable account structure.");
                hasErrors = true;
            }
        }

        var action = ResolveAction(row, existingAccount, hasErrors);
        var displayCode = matchedRange?.FormatDisplayCode(row.Code) ?? row.Code;

        return new ChartOfAccountsImportRowResult(
            row.LineNumber,
            action,
            row.Code,
            displayCode,
            row.Name,
            row.ImportedLevel,
            resolvedLevel.Value.ToString(),
            resolvedType.ToString(),
            resolvedNormalBalance.ToString(),
            resolvedPosting,
            parentCode,
            parentAccountId,
            parentSource,
            row.CurrencyCode,
            existingAccount?.Id.Value,
            existingAccount?.Status.ToString(),
            matchedRange?.Role,
            matchedRange?.DisplayName,
            issues);
    }

    private static string ResolveAction(
        ParsedChartOfAccountsImportRow row,
        LedgerAccount? existingAccount,
        bool hasErrors)
    {
        if (hasErrors)
        {
            return "Reject";
        }

        if (existingAccount is null)
        {
            return "Insert";
        }

        if (!string.Equals(existingAccount.Name, row.Name, StringComparison.Ordinal)
            || (row.Status is not null
                && !string.Equals(existingAccount.Status.ToString(), row.Status, StringComparison.OrdinalIgnoreCase)))
        {
            return "Update";
        }

        return "NoChange";
    }

    private static bool ParentAccountChanged(
        LedgerAccount existingAccount,
        Guid? parentAccountId,
        string? parentSource)
    {
        if (parentSource == "Import")
        {
            return existingAccount.ParentAccountId.HasValue;
        }

        return existingAccount.ParentAccountId?.Value != parentAccountId;
    }

    private static void AddParentReviewIssues(
        List<ChartOfAccountsImportIssueResult> issues,
        LedgerAccountLevel level,
        LedgerAccountLevel parentLevel)
    {
        if (level == LedgerAccountLevel.Subsidiary && parentLevel != LedgerAccountLevel.Control)
        {
            AddIssue(
                issues,
                WarningSeverity,
                "ParentLevelReview",
                "Subsidiary accounts can nest below any compatible parent in the same account family. Confirm this import preserves the intended rollup.");
        }

        if (level == LedgerAccountLevel.Detail && parentLevel != LedgerAccountLevel.Master)
        {
            AddIssue(
                issues,
                WarningSeverity,
                "ParentLevelReview",
                "Detail accounts can nest below any compatible parent in the same account family. Confirm this import preserves the intended rollup.");
        }
    }

    private static ChartOfAccountsImportTextParseResult ParseImportText(
        string importText,
        string? delimiter)
    {
        var rows = importText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var firstDataLine = rows.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row)) ?? string.Empty;
        var delimiterChar = ResolveDelimiter(delimiter, firstDataLine);
        var format = delimiterChar switch
        {
            '\t' => "Tab",
            '|' => "Pipe",
            ',' => "Comma",
            _ => delimiterChar.ToString()
        };
        var parsedRows = new List<ParsedChartOfAccountsImportRow>();
        var parseIssues = new List<ChartOfAccountsImportParseIssueResult>();
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
            parsedRows.Add(ParseDataCells(lineNumber, cells, headerMap));
        }

        return new ChartOfAccountsImportTextParseResult(
            format,
            ignoredLineCount,
            parsedRows,
            parseIssues);
    }

    private static ParsedChartOfAccountsImportRow ParseDataCells(
        int lineNumber,
        IReadOnlyCollection<string> cells,
        ColumnMap? headerMap)
    {
        if (headerMap is not null)
        {
            return new ParsedChartOfAccountsImportRow(
                lineNumber,
                NormalizeCode(GetCell(cells, headerMap.CodeIndex)),
                CleanText(GetCell(cells, headerMap.NameIndex)),
                CleanOptionalText(GetCell(cells, headerMap.LevelIndex)),
                NormalizeOptionalCode(GetCell(cells, headerMap.ParentCodeIndex)),
                NormalizeCurrencyCode(GetCell(cells, headerMap.CurrencyCodeIndex)),
                CleanOptionalText(GetCell(cells, headerMap.AccountTypeIndex)),
                CleanOptionalText(GetCell(cells, headerMap.NormalBalanceIndex)),
                CleanOptionalText(GetCell(cells, headerMap.PostingFlagIndex)),
                NormalizeStatus(GetCell(cells, headerMap.StatusIndex)));
        }

        var firstCell = GetCell(cells, 0);
        var secondCell = GetCell(cells, 1);

        if (TryParseLevel(firstCell, out _)
            && IsLikelyAccountCode(secondCell))
        {
            var thirdCell = GetCell(cells, 2);
            var detectedParentCode = IsLikelyAccountCode(thirdCell) && cells.Count >= 4
                ? NormalizeOptionalCode(thirdCell)
                : null;
            var nameIndex = detectedParentCode is null ? 2 : 3;
            var currencyIndex = detectedParentCode is null ? 3 : 4;

            return new ParsedChartOfAccountsImportRow(
                lineNumber,
                NormalizeCode(secondCell),
                CleanText(GetCell(cells, nameIndex)),
                CleanOptionalText(firstCell),
                detectedParentCode,
                NormalizeCurrencyCode(GetCell(cells, currencyIndex)),
                null,
                null,
                null,
                null);
        }

        var candidateLevel = GetCell(cells, 2);
        var importedLevel = TryParseLevel(candidateLevel, out _)
            ? CleanOptionalText(candidateLevel)
            : null;
        var fallbackParentCode = importedLevel is null && IsLikelyAccountCode(candidateLevel)
            ? NormalizeOptionalCode(candidateLevel)
            : NormalizeOptionalCode(GetCell(cells, 3));
        var currencyCode = importedLevel is null && IsLikelyCurrencyCode(candidateLevel)
            ? NormalizeCurrencyCode(candidateLevel)
            : fallbackParentCode == NormalizeOptionalCode(candidateLevel)
                ? NormalizeCurrencyCode(GetCell(cells, 3))
                : NormalizeCurrencyCode(GetCell(cells, 4));

        return new ParsedChartOfAccountsImportRow(
            lineNumber,
            NormalizeCode(firstCell),
            CleanText(secondCell),
            importedLevel,
            fallbackParentCode,
            currencyCode,
            null,
            null,
            null,
            null);
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
        var codeIndex = FindHeaderIndex(
            normalizedCells,
            "accountcode",
            "acccode",
            "code",
            "glcode",
            "coacode",
            "coa3code");
        var nameIndex = FindHeaderIndex(
            normalizedCells,
            "accountname",
            "accname",
            "name",
            "description",
            "desc",
            "coa3desc");

        if (codeIndex < 0 || nameIndex < 0)
        {
            columnMap = ColumnMap.Default;
            return false;
        }

        columnMap = new ColumnMap(
            codeIndex,
            nameIndex,
            FindHeaderIndex(normalizedCells, "acctype", "accountlevel", "level", "coa3type"),
            FindHeaderIndex(normalizedCells, "parentcode", "summarycode", "sumcode", "coasumcode", "coa3sumcode", "acclink", "link"),
            FindHeaderIndex(normalizedCells, "cur", "currency", "currencycode"),
            FindHeaderIndex(normalizedCells, "ledgeraccounttype", "statementtype", "accountclass", "accountnature"),
            FindHeaderIndex(normalizedCells, "normalbalance", "balance", "drcr"),
            FindHeaderIndex(normalizedCells, "posting", "postingflag", "isposting", "postable"),
            FindHeaderIndex(normalizedCells, "status", "active", "flag"));
        return true;
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

    private static string NormalizeCode(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }

    private static string? NormalizeOptionalCode(string? value)
    {
        var normalized = NormalizeCode(value);

        return normalized.Length == 0 ? null : normalized;
    }

    private static string CleanText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string? CleanOptionalText(string? value)
    {
        var cleaned = CleanText(value);

        return cleaned.Length == 0 ? null : cleaned;
    }

    private static string? NormalizeCurrencyCode(string? value)
    {
        var cleaned = CleanText(value);

        return cleaned.Length == 0 ? null : cleaned.ToUpperInvariant();
    }

    private static string? NormalizeStatus(string? value)
    {
        var cleaned = CleanText(value);

        if (cleaned.Length == 0)
        {
            return null;
        }

        return cleaned.Equals("Y", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("True", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("1", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("Active", StringComparison.OrdinalIgnoreCase)
                ? LedgerAccountStatus.Active.ToString()
                : cleaned.Equals("N", StringComparison.OrdinalIgnoreCase)
                    || cleaned.Equals("No", StringComparison.OrdinalIgnoreCase)
                    || cleaned.Equals("False", StringComparison.OrdinalIgnoreCase)
                    || cleaned.Equals("0", StringComparison.OrdinalIgnoreCase)
                    || cleaned.Equals("Inactive", StringComparison.OrdinalIgnoreCase)
                        ? LedgerAccountStatus.Inactive.ToString()
                        : cleaned;
    }

    private static bool IsLikelyAccountCode(string? value)
    {
        var normalized = NormalizeCode(value);

        return normalized.Length >= 2 && normalized.All(char.IsDigit);
    }

    private static bool IsCurrencyCode(string value)
    {
        return value.Length == 3 && value.All(char.IsLetter);
    }

    private static bool IsLikelyCurrencyCode(string? value)
    {
        var cleaned = CleanText(value);

        return cleaned.Length == 3 && cleaned.All(char.IsLetter);
    }

    private static AccountCodeRange? FindRange(
        string code,
        IReadOnlyCollection<AccountCodeRange> ranges)
    {
        return ranges.FirstOrDefault(range =>
            code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0);
    }

    private static LedgerAccountLevel DetermineDefaultLevel(AccountCodeRange? range)
    {
        if (range is not null && HasRangeIntent(range, "Header"))
        {
            return LedgerAccountLevel.Header;
        }

        if (range is not null && HasRangeIntent(range, "Total"))
        {
            return LedgerAccountLevel.Total;
        }

        if (range is not null && HasRangeIntent(range, "Control"))
        {
            return LedgerAccountLevel.Control;
        }

        if (range is not null && HasRangeIntent(range, "Master"))
        {
            return LedgerAccountLevel.Master;
        }

        if (!string.IsNullOrWhiteSpace(range?.ParentCode))
        {
            return LedgerAccountLevel.Subsidiary;
        }

        return range?.IsPostingAccount == true
            ? LedgerAccountLevel.Detail
            : LedgerAccountLevel.Master;
    }

    private static bool TryParseLevel(string? value, out LedgerAccountLevel level)
    {
        var normalized = value?.Trim();
        level = LedgerAccountLevel.Detail;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var parsed = normalized.ToUpperInvariant() switch
        {
            "H" => LedgerAccountLevel.Header,
            "HEADER" => LedgerAccountLevel.Header,
            "T" => LedgerAccountLevel.Total,
            "TOTAL" => LedgerAccountLevel.Total,
            "M" => LedgerAccountLevel.Master,
            "MASTER" => LedgerAccountLevel.Master,
            "D" => LedgerAccountLevel.Detail,
            "DETAIL" => LedgerAccountLevel.Detail,
            "C" => LedgerAccountLevel.Control,
            "CONTROL" => LedgerAccountLevel.Control,
            "S" => LedgerAccountLevel.Subsidiary,
            "SUBSIDIARY" => LedgerAccountLevel.Subsidiary,
            _ => (LedgerAccountLevel?)null
        };

        if (parsed.HasValue)
        {
            level = parsed.Value;
            return true;
        }

        if (!Enum.TryParse<LedgerAccountLevel>(normalized, true, out var enumLevel)
            || !Enum.IsDefined(enumLevel))
        {
            return false;
        }

        level = enumLevel;
        return true;
    }

    private static bool TryParseAccountType(string value, out LedgerAccountType type)
    {
        var normalized = value.Trim();

        if (normalized.Equals("Income", StringComparison.OrdinalIgnoreCase))
        {
            type = LedgerAccountType.Revenue;
            return true;
        }

        return Enum.TryParse(normalized, true, out type)
            && Enum.IsDefined(type);
    }

    private static bool TryParseNormalBalance(string value, out NormalBalance normalBalance)
    {
        var normalized = value.Trim();

        if (normalized.Equals("Dr", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Debit", StringComparison.OrdinalIgnoreCase))
        {
            normalBalance = NormalBalance.Debit;
            return true;
        }

        if (normalized.Equals("Cr", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Credit", StringComparison.OrdinalIgnoreCase))
        {
            normalBalance = NormalBalance.Credit;
            return true;
        }

        return Enum.TryParse(normalized, true, out normalBalance)
            && Enum.IsDefined(normalBalance);
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        var normalized = value.Trim();

        if (normalized.Equals("Y", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("True", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("1", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Posting", StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (normalized.Equals("N", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("No", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("False", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("0", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Control", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("NonPosting", StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        return bool.TryParse(normalized, out result);
    }

    private static bool IsPostingLevel(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Detail or LedgerAccountLevel.Subsidiary;
    }

    private static bool HasRangeIntent(AccountCodeRange range, string intent)
    {
        return range.Role.Contains(intent, StringComparison.OrdinalIgnoreCase)
            || range.DisplayName.Contains(intent, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddIssue(
        List<ChartOfAccountsImportIssueResult> issues,
        string severity,
        string code,
        string message)
    {
        issues.Add(new ChartOfAccountsImportIssueResult(severity, code, message));
    }

    private sealed record ColumnMap(
        int CodeIndex,
        int NameIndex,
        int LevelIndex,
        int ParentCodeIndex,
        int CurrencyCodeIndex,
        int AccountTypeIndex,
        int NormalBalanceIndex,
        int PostingFlagIndex,
        int StatusIndex)
    {
        public static ColumnMap Default { get; } = new(0, 1, 2, 3, 4, -1, -1, -1, -1);
    }

    private sealed record ParsedChartOfAccountsImportRow(
        int LineNumber,
        string Code,
        string Name,
        string? ImportedLevel,
        string? ParentCode,
        string? CurrencyCode,
        string? AccountType,
        string? NormalBalance,
        string? PostingFlag,
        string? Status);

    private sealed record ChartOfAccountsImportTextParseResult(
        string Format,
        int IgnoredLineCount,
        IReadOnlyCollection<ParsedChartOfAccountsImportRow> Rows,
        IReadOnlyCollection<ChartOfAccountsImportParseIssueResult> ParseIssues);
}
