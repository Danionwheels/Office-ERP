using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;

public sealed class AccountingSetupDefaults
{
    public const string DefaultCompanyCode = "MAIN";

    private static readonly IReadOnlyCollection<DefaultAccountCodeRange> Ranges =
    [
        new(
            "AssetHeader",
            "Asset header",
            "10",
            "10000",
            "10999",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            false,
            null),
        new(
            "ReceivableControl",
            "Accounts receivable control",
            "15100",
            "15100",
            "15100",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            false,
            null),
        new(
            "ClientReceivable",
            "Client receivable subsidiary",
            "15100",
            "151000001",
            "151009999",
            9,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            true,
            "15100"),
        new(
            "AssetTotal",
            "Asset total",
            "19",
            "19000",
            "19999",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            false,
            null),
        new(
            "EquityHeader",
            "Equity header",
            "20",
            "20000",
            "20999",
            5,
            LedgerAccountType.Equity,
            NormalBalance.Credit,
            false,
            null),
        new(
            "RetainedEarnings",
            "Retained earnings",
            "21",
            "21000",
            "21999",
            5,
            LedgerAccountType.Equity,
            NormalBalance.Credit,
            true,
            null),
        new(
            "IncomeSummary",
            "Income summary",
            "23",
            "23000",
            "23999",
            5,
            LedgerAccountType.Equity,
            NormalBalance.Credit,
            true,
            null),
        new(
            "EquityTotal",
            "Equity total",
            "29",
            "29000",
            "29999",
            5,
            LedgerAccountType.Equity,
            NormalBalance.Credit,
            false,
            null),
        new(
            "LiabilityHeader",
            "Liability header",
            "30",
            "30000",
            "30999",
            5,
            LedgerAccountType.Liability,
            NormalBalance.Credit,
            false,
            null),
        new(
            "CashBankControl",
            "Cash and bank control",
            "14100",
            "14100",
            "14100",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            false,
            null),
        new(
            "SubscriptionRevenue",
            "Subscription revenue",
            "41",
            "41000",
            "41999",
            5,
            LedgerAccountType.Revenue,
            NormalBalance.Credit,
            true,
            null),
        new(
            "LiabilityTotal",
            "Liability total",
            "39",
            "39000",
            "39999",
            5,
            LedgerAccountType.Liability,
            NormalBalance.Credit,
            false,
            null),
        new(
            "RevenueHeader",
            "Revenue header",
            "40",
            "40000",
            "40999",
            5,
            LedgerAccountType.Revenue,
            NormalBalance.Credit,
            false,
            null),
        new(
            "CashBank",
            "Cash and bank",
            "141",
            "14110",
            "14199",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            true,
            null),
        new(
            "TaxPayable",
            "Tax payable",
            "32",
            "32100",
            "32999",
            5,
            LedgerAccountType.Liability,
            NormalBalance.Credit,
            true,
            null),
        new(
            "Discount",
            "Client discount",
            "52",
            "52000",
            "52999",
            5,
            LedgerAccountType.Revenue,
            NormalBalance.Debit,
            true,
            null),
        new(
            "RevenueTotal",
            "Revenue total",
            "59",
            "59000",
            "59999",
            5,
            LedgerAccountType.Revenue,
            NormalBalance.Credit,
            false,
            null),
        new(
            "ExpenseHeader",
            "Expense header",
            "60",
            "60000",
            "60999",
            5,
            LedgerAccountType.Expense,
            NormalBalance.Debit,
            false,
            null),
        new(
            "RoundingAdjustment",
            "Rounding adjustment",
            "61",
            "61000",
            "61999",
            5,
            LedgerAccountType.Expense,
            NormalBalance.Debit,
            true,
            null),
        new(
            "Refund",
            "Client refund clearing",
            "142",
            "14200",
            "14299",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            true,
            null),
        new(
            "ExpenseTotal",
            "Expense total",
            "99",
            "99000",
            "99999",
            5,
            LedgerAccountType.Expense,
            NormalBalance.Debit,
            false,
            null)
    ];

    private readonly IAccountCodeRangeRepository _ranges;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public AccountingSetupDefaults(
        IAccountCodeRangeRepository ranges,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _ranges = ranges;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task EnsureSeededAsync(
        string? companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var existingRoles = (await _ranges.ListByCompanyAsync(normalizedCompanyCode, cancellationToken))
            .Select(range => range.Role)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addedRange = false;

        foreach (var range in Ranges)
        {
            if (existingRoles.Contains(range.Role))
            {
                continue;
            }

            await _ranges.AddAsync(
                AccountCodeRange.Create(
                    AccountCodeRangeId.Create(_idGenerator.NewGuid()),
                    normalizedCompanyCode,
                    range.Role,
                    range.DisplayName,
                    range.SearchPrefix,
                    range.RangeStart,
                    range.RangeEnd,
                    range.CodeLength,
                    range.AccountType,
                    range.NormalBalance,
                    range.IsPostingAccount,
                    range.ParentCode,
                    isActive: true,
                    _clock.UtcNow),
                cancellationToken);
            addedRange = true;
        }

        if (addedRange)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public static string NormalizeCompanyCode(string? companyCode)
    {
        return string.IsNullOrWhiteSpace(companyCode)
            ? DefaultCompanyCode
            : companyCode.Trim().ToUpperInvariant();
    }

    public static ApplicationError? ValidateSingleCompanyCode(string? companyCode, string target)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            return null;
        }

        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);

        return string.Equals(normalizedCompanyCode, DefaultCompanyCode, StringComparison.Ordinal)
            ? null
            : ApplicationError.Validation(
                target,
                $"Accounting currently supports a single company ({DefaultCompanyCode}). Remove companyCode or use {DefaultCompanyCode}.");
    }

    private sealed record DefaultAccountCodeRange(
        string Role,
        string DisplayName,
        string SearchPrefix,
        string RangeStart,
        string RangeEnd,
        int CodeLength,
        LedgerAccountType AccountType,
        NormalBalance NormalBalance,
        bool IsPostingAccount,
        string? ParentCode);
}
