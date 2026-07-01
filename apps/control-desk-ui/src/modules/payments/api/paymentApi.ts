import { apiRequest } from "../../../shared/api/httpClient";
import type {
  RecordedInvoicePayment,
  RecordInvoicePaymentInput
} from "../types/paymentTypes";

export async function recordInvoicePayment(
  input: RecordInvoicePaymentInput
): Promise<RecordedInvoicePayment> {
  return apiRequest<RecordedInvoicePayment>("/api/v1/payments/invoice-payments", {
    method: "POST",
    body: JSON.stringify({
      invoiceId: input.invoiceId,
      method: input.method,
      reference: input.reference,
      amount: Number(input.amount),
      currencyCode: input.currencyCode,
      receivedOn: input.receivedOn,
      cashOrBankAccountId: input.cashOrBankAccountId,
      accountsReceivableAccountId: input.accountsReceivableAccountId,
      postingDate: input.postingDate
    })
  });
}
