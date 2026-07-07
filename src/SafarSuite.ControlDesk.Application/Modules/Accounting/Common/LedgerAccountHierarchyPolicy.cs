using System.Numerics;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

internal static class LedgerAccountHierarchyPolicy
{
    public static LedgerAccountLevel DetermineExpectedLevel(
        AccountCodeRange? range,
        LedgerAccount? parentAccount,
        bool isPostingAccount)
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

        if (!isPostingAccount)
        {
            return LedgerAccountLevel.Master;
        }

        if (parentAccount is null && string.IsNullOrWhiteSpace(range?.ParentCode))
        {
            return LedgerAccountLevel.Detail;
        }

        return parentAccount?.Level == LedgerAccountLevel.Master
            ? LedgerAccountLevel.Detail
            : LedgerAccountLevel.Subsidiary;
    }

    public static bool RequiresNonPostingAccount(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Header
            or LedgerAccountLevel.Total
            or LedgerAccountLevel.Master
            or LedgerAccountLevel.Control;
    }

    public static bool RequiresPostingAccount(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Detail
            or LedgerAccountLevel.Subsidiary;
    }

    public static bool IsStructuralLevel(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Header
            or LedgerAccountLevel.Total
            or LedgerAccountLevel.Master
            or LedgerAccountLevel.Control;
    }

    public static bool IsParentInsideRangeFamily(AccountCodeRange range, string parentCode)
    {
        var configuredParentCode = range.ParentCode?.Trim() ?? string.Empty;

        if (configuredParentCode == string.Empty)
        {
            return true;
        }

        if (string.Equals(configuredParentCode, parentCode, StringComparison.Ordinal))
        {
            return true;
        }

        return IsNumeric(parentCode)
            && parentCode.StartsWith(configuredParentCode, StringComparison.Ordinal)
            && IsInsideRange(parentCode, range);
    }

    public static bool IsChildCodeInsideParentScope(
        string code,
        string parentCode,
        AccountCodeRange range)
    {
        if (!IsNumeric(code) || !IsNumeric(parentCode) || !IsInsideRange(code, range))
        {
            return false;
        }

        var configuredParentCode = range.ParentCode?.Trim() ?? string.Empty;

        if (configuredParentCode != string.Empty)
        {
            if (!IsParentInsideRangeFamily(range, parentCode))
            {
                return false;
            }

            return string.Equals(parentCode, configuredParentCode, StringComparison.Ordinal)
                || StringComparer.Ordinal.Compare(code, parentCode) > 0;
        }

        if (parentCode.Length < code.Length)
        {
            return code.StartsWith(parentCode, StringComparison.Ordinal);
        }

        if (parentCode.Length == code.Length)
        {
            return parentCode.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
                && StringComparer.Ordinal.Compare(code, parentCode) > 0;
        }

        return false;
    }

    public static LedgerAccountCodeBounds GetSuggestionBounds(
        AccountCodeRange range,
        LedgerAccount? parentAccount)
    {
        var rangeStart = BigInteger.Parse(range.RangeStart);
        var rangeEnd = BigInteger.Parse(range.RangeEnd);

        if (parentAccount is null || !range.IsPostingAccount)
        {
            return new LedgerAccountCodeBounds(rangeStart, rangeEnd);
        }

        var parentCode = parentAccount.Code.Value;
        var configuredParentCode = range.ParentCode?.Trim() ?? string.Empty;

        if (configuredParentCode != string.Empty
            && string.Equals(parentCode, configuredParentCode, StringComparison.Ordinal))
        {
            return new LedgerAccountCodeBounds(rangeStart, rangeEnd);
        }

        if (parentCode.Length < range.CodeLength)
        {
            var scopedStart = BigInteger.Parse(parentCode.PadRight(range.CodeLength, '0')) + BigInteger.One;
            var scopedEnd = BigInteger.Parse(parentCode.PadRight(range.CodeLength, '9'));

            return new LedgerAccountCodeBounds(
                BigInteger.Max(rangeStart, scopedStart),
                BigInteger.Min(rangeEnd, scopedEnd));
        }

        if (parentCode.Length == range.CodeLength)
        {
            return new LedgerAccountCodeBounds(
                BigInteger.Max(rangeStart, BigInteger.Parse(parentCode) + BigInteger.One),
                rangeEnd);
        }

        return new LedgerAccountCodeBounds(rangeStart, rangeEnd);
    }

    public static bool HasRangeIntent(AccountCodeRange range, string intent)
    {
        return range.Role.Contains(intent, StringComparison.OrdinalIgnoreCase)
            || range.DisplayName.Contains(intent, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInsideRange(string code, AccountCodeRange range)
    {
        return code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0;
    }

    private static bool IsNumeric(string value)
    {
        return value.All(char.IsDigit);
    }
}

internal readonly record struct LedgerAccountCodeBounds(
    BigInteger Start,
    BigInteger End);
