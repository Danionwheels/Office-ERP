import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRule,
  ClientChargeRuleFormInput,
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccount,
  LedgerAccountFormInput
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

function optionalText(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === "" ? undefined : trimmed;
}
