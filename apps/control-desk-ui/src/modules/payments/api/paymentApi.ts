import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ApplyClientCreditInput,
  AppliedClientCredit,
  ApproveInvoicePaymentInput,
  ClientRefundDocument,
  InvoicePaymentDocument,
  IssueClientRefundInput,
  IssuedClientRefund,
  RecordedInvoicePayment,
  RecordInvoicePaymentInput,
  RejectInvoicePaymentResult,
  ReversedInvoicePayment,
  ReverseInvoicePaymentInput
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

export async function getInvoicePaymentDocument(paymentId: string): Promise<InvoicePaymentDocument> {
  return apiRequest<InvoicePaymentDocument>(`/api/v1/payments/invoice-payments/${paymentId}`);
}

export async function approveInvoicePayment(
  paymentId: string,
  input: ApproveInvoicePaymentInput
): Promise<RecordedInvoicePayment> {
  return apiRequest<RecordedInvoicePayment>(`/api/v1/payments/invoice-payments/${paymentId}/approve`, {
    method: "POST",
    body: JSON.stringify({
      cashOrBankAccountId: input.cashOrBankAccountId,
      accountsReceivableAccountId: input.accountsReceivableAccountId,
      postingDate: input.postingDate,
      decisionNote: input.decisionNote ?? null
    })
  });
}

export async function rejectInvoicePayment(
  paymentId: string,
  decisionNote: string
): Promise<RejectInvoicePaymentResult> {
  return apiRequest<RejectInvoicePaymentResult>(`/api/v1/payments/invoice-payments/${paymentId}/reject`, {
    method: "POST",
    body: JSON.stringify({ decisionNote })
  });
}

export async function reverseInvoicePayment(
  paymentId: string,
  input: ReverseInvoicePaymentInput
): Promise<ReversedInvoicePayment> {
  return apiRequest<ReversedInvoicePayment>(`/api/v1/payments/invoice-payments/${paymentId}/reverse`, {
    method: "POST",
    body: JSON.stringify({
      reversalDate: input.reversalDate,
      decisionNote: input.decisionNote
    })
  });
}

export async function issueClientRefund(
  input: IssueClientRefundInput
): Promise<IssuedClientRefund> {
  return apiRequest<IssuedClientRefund>("/api/v1/payments/client-refunds", {
    method: "POST",
    body: JSON.stringify({
      clientId: input.clientId,
      method: input.method,
      reference: input.reference,
      amount: Number(input.amount),
      currencyCode: input.currencyCode,
      refundedOn: input.refundedOn,
      cashOrBankAccountId: input.cashOrBankAccountId,
      accountsReceivableAccountId: input.accountsReceivableAccountId,
      postingDate: input.postingDate,
      note: input.note.trim() === "" ? null : input.note
    })
  });
}

export async function getClientRefundDocument(refundId: string): Promise<ClientRefundDocument> {
  return apiRequest<ClientRefundDocument>(`/api/v1/payments/client-refunds/${refundId}`);
}

export async function applyClientCredit(
  input: ApplyClientCreditInput
): Promise<AppliedClientCredit> {
  return apiRequest<AppliedClientCredit>("/api/v1/payments/client-credit-applications", {
    method: "POST",
    body: JSON.stringify({
      clientId: input.clientId,
      invoiceId: input.invoiceId,
      reference: input.reference,
      amount: Number(input.amount),
      currencyCode: input.currencyCode,
      appliedOn: input.appliedOn,
      note: input.note.trim() === "" ? null : input.note
    })
  });
}
