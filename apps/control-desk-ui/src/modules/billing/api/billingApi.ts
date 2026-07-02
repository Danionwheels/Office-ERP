import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRule,
  ClientChargeRuleFormInput,
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueCreditNoteInput,
  IssuedCreditNote,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccount,
  LedgerAccountFormInput,
  VoidedInvoice,
  VoidInvoiceInput
} from "../types/billingTypes";

type ListChargeCodesResponse = {
  chargeCodes: ChargeCodeLookup[];
};

export async function createLedgerAccount(
  input: LedgerAccountFormInput
): Promise<LedgerAccount> {
  return apiRequest<LedgerAccount>("/api/v1/accounting/ledger-accounts", {
    method: "POST",
    body: JSON.stringify({
      code: input.code,
      name: input.name,
      type: input.type,
      normalBalance: input.normalBalance,
      parentAccountId: optionalText(input.parentAccountId),
      isPostingAccount: input.isPostingAccount
    })
  });
}

export async function listChargeCodes(): Promise<ChargeCodeLookup[]> {
  const response = await apiRequest<ListChargeCodesResponse>("/api/v1/billing/charge-codes");

  return response.chargeCodes;
}

export async function createChargeCode(
  input: ChargeCodeFormInput
): Promise<ChargeCodeLookup> {
  return apiRequest<ChargeCodeLookup>("/api/v1/billing/charge-codes", {
    method: "POST",
    body: JSON.stringify({
      code: input.code,
      name: input.name,
      description: optionalText(input.description),
      defaultUnitPriceAmount: Number(input.defaultUnitPriceAmount),
      currencyCode: input.currencyCode,
      revenueAccountId: input.revenueAccountId,
      taxAccountId: optionalText(input.taxAccountId)
    })
  });
}

export async function createClientChargeRule(
  clientId: string,
  input: ClientChargeRuleFormInput
): Promise<ClientChargeRule> {
  return apiRequest<ClientChargeRule>("/api/v1/billing/client-charge-rules", {
    method: "POST",
    body: JSON.stringify({
      clientId,
      contractId: optionalText(input.contractId),
      chargeCodeId: input.chargeCodeId,
      descriptionOverride: optionalText(input.descriptionOverride),
      unitPriceAmount: Number(input.unitPriceAmount),
      currencyCode: input.currencyCode,
      quantity: Number(input.quantity),
      taxPercent: Number(input.taxPercent),
      billingCycle: input.billingCycle,
      billingDayOfMonth: Number(input.billingDayOfMonth),
      effectiveStartsOn: input.effectiveStartsOn,
      effectiveEndsOn: input.effectiveEndsOn
    })
  });
}

export async function generateInvoiceDraft(
  clientId: string,
  input: InvoiceDraftFormInput
): Promise<InvoiceDraft> {
  return apiRequest<InvoiceDraft>("/api/v1/billing/invoice-drafts", {
    method: "POST",
    body: JSON.stringify({
      clientId,
      contractId: input.contractId,
      invoiceNumber: input.invoiceNumber,
      issueDate: input.issueDate,
      dueDate: input.dueDate,
      billingDate: input.billingDate,
      currencyCode: input.currencyCode
    })
  });
}

export async function issueInvoice(
  invoiceId: string,
  input: IssueInvoiceFormInput
): Promise<IssuedInvoice> {
  return apiRequest<IssuedInvoice>(`/api/v1/billing/invoices/${invoiceId}/issue`, {
    method: "POST",
    body: JSON.stringify({
      accountsReceivableAccountId: optionalText(input.accountsReceivableAccountId),
      postingDate: input.postingDate
    })
  });
}

export async function voidInvoice(
  invoiceId: string,
  input: VoidInvoiceInput
): Promise<VoidedInvoice> {
  return apiRequest<VoidedInvoice>(`/api/v1/billing/invoices/${invoiceId}/void`, {
    method: "POST",
    body: JSON.stringify({
      voidDate: input.voidDate,
      reason: input.reason
    })
  });
}

export async function issueCreditNote(
  invoiceId: string,
  input: IssueCreditNoteInput
): Promise<IssuedCreditNote> {
  return apiRequest<IssuedCreditNote>(`/api/v1/billing/invoices/${invoiceId}/credit-notes`, {
    method: "POST",
    body: JSON.stringify({
      creditNoteNumber: input.creditNoteNumber,
      creditDate: input.creditDate,
      reason: input.reason
    })
  });
}

function optionalText(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === "" ? undefined : trimmed;
}
