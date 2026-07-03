namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public enum JournalSourceType
{
    Manual = 1,
    BillingInvoice = 2,
    PaymentReceipt = 3,
    OpeningBalance = 4,
    Adjustment = 5,
    PaymentReversal = 6,
    BillingInvoiceVoid = 7,
    BillingCreditNote = 8,
    ClientRefund = 9,
    ManualReversal = 10
}
