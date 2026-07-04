import type { JournalEntrySummary } from "../types/accountingTypes";

export function getJournalSourceDocumentFallbackLabel(
  entry: JournalEntrySummary
): string | null {
  const reference = entry.sourceReference?.trim();

  if (reference === undefined || reference === "") {
    return null;
  }

  switch (entry.sourceType) {
    case "BillingInvoice":
      return `invoice ${reference}`;
    case "BillingInvoiceVoid":
      return `voided invoice ${reference}`;
    case "BillingCreditNote":
      return `credit note ${reference}`;
    case "PaymentReceipt":
      return `payment ${reference}`;
    case "PaymentReversal":
      return `payment reversal ${reference}`;
    case "ClientRefund":
      return `refund ${reference}`;
    default:
      return null;
  }
}
