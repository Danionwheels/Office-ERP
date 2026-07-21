using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public static class VoucherNumberingDefaults
{
    private static readonly IReadOnlyCollection<VoucherNumberingRuleDefault> Rules =
    [
        new(JournalSourceType.Manual, "MJ", 4),
        new(JournalSourceType.BillingInvoice, "INV", 4),
        new(JournalSourceType.PaymentReceipt, "RCPT", 4),
        new(JournalSourceType.OpeningBalance, "OB", 4),
        new(JournalSourceType.Adjustment, "ADJ", 4),
        new(JournalSourceType.PaymentReversal, "PR", 4),
        new(JournalSourceType.BillingInvoiceVoid, "VOID", 4),
        new(JournalSourceType.BillingCreditNote, "CN", 4),
        new(JournalSourceType.ClientRefund, "REF", 4),
        new(JournalSourceType.ManualReversal, "MR", 4),
        new(JournalSourceType.PeriodClose, "CL", 4),
        new(JournalSourceType.PeriodCloseReversal, "CR", 4)
    ];

    public static IReadOnlyCollection<VoucherNumberingRuleDefault> All => Rules;

    public static VoucherNumberingRuleDefault GetDefault(JournalSourceType sourceType)
    {
        return Rules.FirstOrDefault(rule => rule.SourceType == sourceType)
            ?? new VoucherNumberingRuleDefault(sourceType, sourceType.ToString().ToUpperInvariant(), 4);
    }
}

public sealed record VoucherNumberingRuleDefault(
    JournalSourceType SourceType,
    string Prefix,
    int NumberPaddingWidth);
