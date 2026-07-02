namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed record ClientCreditBalance(
    string CurrencyCode,
    decimal InvoiceBalance,
    decimal CreditNoteAmount,
    decimal RefundAmount,
    decimal AppliedCreditAmount)
{
    public decimal AvailableCredit => Math.Max(CreditNoteAmount - RefundAmount - AppliedCreditAmount, 0m);

    public decimal StatementBalance => InvoiceBalance - CreditNoteAmount + RefundAmount + AppliedCreditAmount;

    public decimal RefundableCredit => StatementBalance < 0m
        ? Math.Min(Math.Abs(StatementBalance), AvailableCredit)
        : 0m;
}
