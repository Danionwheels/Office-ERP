using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;

public sealed class AccountingSetupDefaults
{
    public const string DefaultCompanyCode = "MAIN";

    private static readonly IReadOnlyCollection<DefaultAccountCodeRange> Ranges =
    [
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
            "Refund",
            "Client refund clearing",
            "142",
            "14200",
            "14299",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            true,
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

        if (await _ranges.AnyByCompanyAsync(normalizedCompanyCode, cancellationToken))
        {
            return;
        }

        foreach (var range in Ranges)
        {
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
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public static string NormalizeCompanyCode(string? companyCode)
    {
        return string.IsNullOrWhiteSpace(companyCode)
            ? DefaultCompanyCode
            : companyCode.Trim().ToUpperInvariant();
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
