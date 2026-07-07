using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class VoucherNumberingRule : Entity<VoucherNumberingRuleId>
{
    private VoucherNumberingRule()
    {
        CompanyCode = string.Empty;
        Prefix = string.Empty;
    }

    private VoucherNumberingRule(
        VoucherNumberingRuleId id,
        string companyCode,
        JournalSourceType sourceType,
        string prefix,
        int numberPaddingWidth,
        bool isActive,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        CompanyCode = CleanCompanyCode(companyCode);
        SourceType = sourceType;
        Prefix = string.Empty;
        CreatedAtUtc = createdAtUtc;
        Configure(prefix, numberPaddingWidth, isActive, createdAtUtc);
    }

    public string CompanyCode { get; private set; }

    public JournalSourceType SourceType { get; private set; }

    public string Prefix { get; private set; }

    public int NumberPaddingWidth { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static VoucherNumberingRule Create(
        VoucherNumberingRuleId id,
        string companyCode,
        JournalSourceType sourceType,
        string prefix,
        int numberPaddingWidth,
        bool isActive,
        DateTimeOffset createdAtUtc)
    {
        return new VoucherNumberingRule(
            id,
            companyCode,
            sourceType,
            prefix,
            numberPaddingWidth,
            isActive,
            createdAtUtc);
    }

    public void Configure(
        string prefix,
        int numberPaddingWidth,
        bool isActive,
        DateTimeOffset updatedAtUtc)
    {
        Prefix = CleanPrefix(prefix);
        NumberPaddingWidth = CleanNumberPaddingWidth(numberPaddingWidth);
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string CleanCompanyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Company code is required.", nameof(value));
        }

        return value.Trim().ToUpperInvariant();
    }

    private static string CleanPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Voucher prefix is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 16)
        {
            throw new ArgumentException("Voucher prefix cannot exceed 16 characters.", nameof(value));
        }

        if (!normalized.All(character => char.IsLetterOrDigit(character) || character == '_'))
        {
            throw new ArgumentException("Voucher prefix can contain only letters, numbers, or underscore.", nameof(value));
        }

        return normalized;
    }

    private static int CleanNumberPaddingWidth(int value)
    {
        if (value < 1 || value > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Voucher number padding width must be between 1 and 10.");
        }

        return value;
    }
}
