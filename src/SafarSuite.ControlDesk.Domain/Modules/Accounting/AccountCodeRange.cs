using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class AccountCodeRange : Entity<AccountCodeRangeId>
{
    private AccountCodeRange()
    {
        CompanyCode = string.Empty;
        Role = string.Empty;
        DisplayName = string.Empty;
        SearchPrefix = string.Empty;
        RangeStart = string.Empty;
        RangeEnd = string.Empty;
    }

    private AccountCodeRange(
        AccountCodeRangeId id,
        string companyCode,
        string role,
        string displayName,
        string searchPrefix,
        string rangeStart,
        string rangeEnd,
        int codeLength,
        LedgerAccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        string? parentCode,
        bool isActive,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        CompanyCode = CleanRequired(companyCode, nameof(companyCode), 32).ToUpperInvariant();
        Role = CleanRequired(role, nameof(role), 64);
        DisplayName = CleanRequired(displayName, nameof(displayName), 128);
        SearchPrefix = CleanDigits(searchPrefix, nameof(searchPrefix), 1, 32);
        RangeStart = CleanDigits(rangeStart, nameof(rangeStart), 1, 32);
        RangeEnd = CleanDigits(rangeEnd, nameof(rangeEnd), 1, 32);
        CodeLength = codeLength;
        AccountType = accountType;
        NormalBalance = normalBalance;
        IsPostingAccount = isPostingAccount;
        ParentCode = CleanOptionalDigits(parentCode, 32);
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;

        ValidateRange();
    }

    public string CompanyCode { get; private set; }

    public string Role { get; private set; }

    public string DisplayName { get; private set; }

    public string SearchPrefix { get; private set; }

    public string RangeStart { get; private set; }

    public string RangeEnd { get; private set; }

    public int CodeLength { get; private set; }

    public LedgerAccountType AccountType { get; private set; }

    public NormalBalance NormalBalance { get; private set; }

    public bool IsPostingAccount { get; private set; }

    public string? ParentCode { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static AccountCodeRange Create(
        AccountCodeRangeId id,
        string companyCode,
        string role,
        string displayName,
        string searchPrefix,
        string rangeStart,
        string rangeEnd,
        int codeLength,
        LedgerAccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        string? parentCode,
        bool isActive,
        DateTimeOffset createdAtUtc)
    {
        return new AccountCodeRange(
            id,
            companyCode,
            role,
            displayName,
            searchPrefix,
            rangeStart,
            rangeEnd,
            codeLength,
            accountType,
            normalBalance,
            isPostingAccount,
            parentCode,
            isActive,
            createdAtUtc);
    }

    public void Update(
        string displayName,
        string searchPrefix,
        string rangeStart,
        string rangeEnd,
        int codeLength,
        LedgerAccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        string? parentCode,
        bool isActive,
        DateTimeOffset updatedAtUtc)
    {
        DisplayName = CleanRequired(displayName, nameof(displayName), 128);
        SearchPrefix = CleanDigits(searchPrefix, nameof(searchPrefix), 1, 32);
        RangeStart = CleanDigits(rangeStart, nameof(rangeStart), 1, 32);
        RangeEnd = CleanDigits(rangeEnd, nameof(rangeEnd), 1, 32);
        CodeLength = codeLength;
        AccountType = accountType;
        NormalBalance = normalBalance;
        IsPostingAccount = isPostingAccount;
        ParentCode = CleanOptionalDigits(parentCode, 32);
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;

        ValidateRange();
    }

    public string FormatDisplayCode(string code)
    {
        var cleanCode = CleanDigits(code, nameof(code), CodeLength, CodeLength);

        return ParentCode is not null
            && cleanCode.Length > ParentCode.Length
            && cleanCode.StartsWith(ParentCode, StringComparison.Ordinal)
                ? $"{ParentCode}-{cleanCode[ParentCode.Length..]}"
                : cleanCode;
    }

    private void ValidateRange()
    {
        if (CodeLength is < 2 or > 32)
        {
            throw new ArgumentException("Code length must be between 2 and 32.", nameof(CodeLength));
        }

        if (RangeStart.Length != CodeLength || RangeEnd.Length != CodeLength)
        {
            throw new ArgumentException("Range start and end must match the configured code length.");
        }

        if (!RangeStart.StartsWith(SearchPrefix, StringComparison.Ordinal)
            || !RangeEnd.StartsWith(SearchPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Range start and end must be inside the configured search prefix.");
        }

        if (ParentCode is not null && ParentCode.Length > CodeLength)
        {
            throw new ArgumentException("Parent code cannot be longer than the configured code length.");
        }

        if (ParentCode is not null
            && ParentCode.Length < CodeLength
            && (!RangeStart.StartsWith(ParentCode, StringComparison.Ordinal)
                || !RangeEnd.StartsWith(ParentCode, StringComparison.Ordinal)))
        {
            throw new ArgumentException("Subsidiary range must stay inside the parent code prefix.");
        }

        if (StringComparer.Ordinal.Compare(RangeStart, RangeEnd) > 0)
        {
            throw new ArgumentException("Range start cannot be greater than range end.");
        }
    }

    private static string CleanRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var cleanValue = value.Trim();

        if (cleanValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return cleanValue;
    }

    private static string CleanDigits(
        string value,
        string parameterName,
        int minLength,
        int maxLength)
    {
        var cleanValue = CleanRequired(value, parameterName, maxLength);

        if (cleanValue.Length < minLength || cleanValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} must be between {minLength} and {maxLength} digits.",
                parameterName);
        }

        if (!cleanValue.All(char.IsDigit))
        {
            throw new ArgumentException($"{parameterName} must contain digits only.", parameterName);
        }

        return cleanValue;
    }

    private static string? CleanOptionalDigits(string? value, int maxLength)
    {
        var cleanValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(cleanValue))
        {
            return null;
        }

        if (cleanValue.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", nameof(value));
        }

        if (!cleanValue.All(char.IsDigit))
        {
            throw new ArgumentException("Value must contain digits only.", nameof(value));
        }

        return cleanValue;
    }
}
