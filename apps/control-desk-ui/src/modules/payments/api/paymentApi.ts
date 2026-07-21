import { apiDownload, apiRequest } from "../../../shared/api/httpClient";
import type {
  ApplyClientCreditInput,
  AppliedClientCredit,
  ApproveInvoicePaymentInput,
  ClientRefundDocument,
  ImportPortalPaymentClaimsResult,
  InvoicePaymentDocument,
  IssueClientRefundInput,
  IssuedClientRefund,
  PortalPaymentClaim,
  PortalPaymentClaimList,
  ProviderBankDetails,
  RecordedInvoicePayment,
  RecordInvoicePaymentInput,
  RejectInvoicePaymentResult,
  ReversedInvoicePayment,
  ReverseInvoicePaymentInput,
  UpdateProviderBankDetailsInput,
  VerifyPortalPaymentClaimInput,
  VerifyPortalPaymentClaimResult
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

export async function listPortalPaymentClaims(clientId: string): Promise<PortalPaymentClaimList> {
  const query = new URLSearchParams({ clientId });
  return apiRequest<PortalPaymentClaimList>(`/api/v1/payments/portal-payment-claims?${query}`);
}

export async function importPortalPaymentClaims(
  clientId: string
): Promise<ImportPortalPaymentClaimsResult> {
  const query = new URLSearchParams({ clientId });
  return apiRequest<ImportPortalPaymentClaimsResult>(
    `/api/v1/payments/portal-payment-claims/import?${query}`,
    { method: "POST", body: JSON.stringify({}) }
  );
}

export async function verifyPortalPaymentClaim(
  claimId: string,
  input: VerifyPortalPaymentClaimInput
): Promise<VerifyPortalPaymentClaimResult> {
  return apiRequest<VerifyPortalPaymentClaimResult>(
    `/api/v1/payments/portal-payment-claims/${claimId}/verify`,
    { method: "POST", body: JSON.stringify(input) }
  );
}

export async function rejectPortalPaymentClaim(
  claimId: string,
  reason: string
): Promise<PortalPaymentClaim> {
  return apiRequest<PortalPaymentClaim>(
    `/api/v1/payments/portal-payment-claims/${claimId}/reject`,
    { method: "POST", body: JSON.stringify({ reason }) }
  );
}

export async function downloadPortalPaymentClaimProof(
  claimId: string,
  fallbackFileName: string
): Promise<void> {
  await apiDownload(
    `/api/v1/payments/portal-payment-claims/${claimId}/proof`,
    fallbackFileName
  );
}

export async function getProviderBankDetails(): Promise<ProviderBankDetails> {
  return apiRequest<ProviderBankDetails>("/api/v1/payments/provider-bank-details");
}

export async function updateProviderBankDetails(
  input: UpdateProviderBankDetailsInput
): Promise<ProviderBankDetails> {
  return apiRequest<ProviderBankDetails>("/api/v1/payments/provider-bank-details", {
    method: "PUT",
    body: JSON.stringify(input)
  });
}
