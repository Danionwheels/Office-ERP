import {
  Banknote,
  CircleDollarSign,
  FileCheck2,
  FilePlus2
} from "lucide-react";
import type { ClientContract, ProductModule } from "../../contracts/types/contractTypes";
import type { ClientAccountingProfile } from "../../clients/types/clientTypes";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRule,
  ClientChargeRuleFormInput,
  InvoiceDraft,
  IssueInvoiceFormInput,
  IssuedInvoice
} from "../types/billingTypes";
import type {
  BillingPostingBridgeItem,
  BillingStep,
  BillingStepItem,
  ModuleBillingSuggestion
} from "../types/billingWorkflowTypes";

type BillingStepInput = {
  accountingProfile: ClientAccountingProfile | null;
  accountingProfileMissing: boolean;
  chargeCodes: ChargeCodeLookup[];
  latestChargeRule: ClientChargeRule | null;
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
};

type BillingPostingBridgeInput = {
  accountingProfile: ClientAccountingProfile | null;
  chargeCodeValue: ChargeCodeFormInput;
  chargeCodes: ChargeCodeLookup[];
  chargeRuleValue: ClientChargeRuleFormInput;
  invoiceDraft: InvoiceDraft;
  issueInvoiceValue: IssueInvoiceFormInput;
  issuedInvoice: IssuedInvoice | null;
};

export function getBillingStepItems({
  accountingProfile,
  accountingProfileMissing,
  chargeCodes,
  latestChargeRule,
  invoiceDraft,
  issuedInvoice
}: BillingStepInput): BillingStepItem[] {
  return [
    {
      step: "accounting",
      label: "Accounting profile",
      summary: accountingProfileMissing || accountingProfile === null
        ? "Missing"
        : `${accountingProfile.defaultCurrencyCode} linked`,
      tone: accountingProfileMissing || accountingProfile === null ? "warning" : "ready",
      Icon: Banknote
    },
    {
      step: "rules",
      label: "Charge rules",
      summary: latestChargeRule === null
        ? `${chargeCodes.length} charge codes`
        : `${latestChargeRule.status} ${formatMoney(
            latestChargeRule.totalLineAmount,
            latestChargeRule.currencyCode
          )}`,
      tone: latestChargeRule === null ? "neutral" : "ready",
      Icon: CircleDollarSign
    },
    {
      step: "draft",
      label: "Invoice draft",
      summary: invoiceDraft === null
        ? "No draft"
        : `${invoiceDraft.status} ${formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}`,
      tone: invoiceDraft === null ? "warning" : "ready",
      Icon: FilePlus2
    },
    {
      step: "issue",
      label: "Invoice issue",
      summary: issuedInvoice === null ? "Not issued" : issuedInvoice.invoiceStatus,
      tone: issuedInvoice === null ? "neutral" : "ready",
      Icon: FileCheck2
    }
  ];
}

export function getBillingStepCue(step: BillingStep): string {
  switch (step) {
    case "accounting":
      return "Confirm AR account and customer profile";
    case "rules":
      return "Set revenue account, charge code, and rule";
    case "draft":
      return "Generate draft with billing and due dates";
    case "issue":
      return "Post invoice, void, or issue credit note";
  }
}

export function getBillingPostingBridgeItems({
  accountingProfile,
  chargeCodeValue,
  chargeCodes,
  chargeRuleValue,
  invoiceDraft,
  issueInvoiceValue,
  issuedInvoice
}: BillingPostingBridgeInput): BillingPostingBridgeItem[] {
  const selectedChargeCode = chargeCodes.find(
    (chargeCode) => chargeCode.chargeCodeId === chargeRuleValue.chargeCodeId
  );
  const arAccountId = cleanOptional(issueInvoiceValue.accountsReceivableAccountId)
    ?? cleanOptional(accountingProfile?.accountsReceivableAccountId);
  const revenueAccountId = cleanOptional(selectedChargeCode?.revenueAccountId)
    ?? cleanOptional(chargeCodeValue.revenueAccountId);
  const taxAccountId = cleanOptional(selectedChargeCode?.taxAccountId)
    ?? cleanOptional(chargeCodeValue.taxAccountId);
  const isBalanced = issuedInvoice !== null
    && Math.abs(issuedInvoice.totalDebit - issuedInvoice.totalCredit) < 0.005;

  return [
    {
      label: "Document",
      value: invoiceDraft.invoiceNumber,
      detail: `${invoiceDraft.status} / ${formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}`,
      tone: "ready"
    },
    {
      label: "AR control",
      value: arAccountId === null ? "Missing" : shortAccountId(arAccountId),
      detail: arAccountId === null ? "Receivable account required" : "Debits customer receivable",
      tone: arAccountId === null ? "warning" : "ready"
    },
    {
      label: "Revenue",
      value: revenueAccountId === null ? "Missing" : shortAccountId(revenueAccountId),
      detail: revenueAccountId === null ? "Charge code revenue account required" : "Credits earned revenue",
      tone: revenueAccountId === null ? "warning" : "ready"
    },
    {
      label: "Tax",
      value: taxAccountId === null ? "No tax account" : shortAccountId(taxAccountId),
      detail: taxAccountId === null ? "Only used when tax is posted" : "Credits tax payable",
      tone: taxAccountId === null ? "neutral" : "ready"
    },
    {
      label: "Journal",
      value: issuedInvoice === null ? "Not posted" : issuedInvoice.journalEntryStatus,
      detail: issuedInvoice === null
        ? "Issue posts the debit/credit entry"
        : `${issuedInvoice.journalLines.length} lines / ${isBalanced ? "balanced" : "review"}`,
      tone: issuedInvoice === null ? "neutral" : isBalanced ? "ready" : "warning"
    }
  ];
}

export function chargeRulePatchForChargeCode(
  chargeCodeId: string,
  chargeCodes: ChargeCodeLookup[]
): Partial<ClientChargeRuleFormInput> {
  const chargeCode = chargeCodes.find((item) => item.chargeCodeId === chargeCodeId);

  if (chargeCode === undefined) {
    return {};
  }

  return {
    unitPriceAmount: chargeCode.defaultUnitPriceAmount.toFixed(2),
    currencyCode: chargeCode.currencyCode,
    descriptionOverride: chargeCode.name
  };
}

export function getModuleBillingSuggestions(
  contracts: ClientContract[],
  productModules: ProductModule[],
  chargeCodes: ChargeCodeLookup[]
): ModuleBillingSuggestion[] {
  const activeContract = getActiveContract(contracts);

  if (activeContract === null) {
    return [];
  }

  const enabledModuleCodes = new Set(
    activeContract.modules
      .filter((module) => module.isEnabled)
      .map((module) => module.moduleCode)
  );

  return productModules
    .filter((module) =>
      module.isActive
      && module.commercialMode === "PaidAddOn"
      && module.billingDefaults !== null
      && module.billingDefaults !== undefined
      && enabledModuleCodes.has(module.moduleCode))
    .map((module) => ({
      module,
      contract: activeContract,
      existingChargeCode: chargeCodes.find(
        (chargeCode) => chargeCode.code === module.billingDefaults!.chargeCode
      ) ?? null
    }));
}

export function getActiveContract(contracts: ClientContract[]): ClientContract | null {
  return contracts.find((contract) => contract.status.toLowerCase() === "active")
    ?? contracts[0]
    ?? null;
}

export function formatLedgerAccountCode(code: string): string {
  return /^\d{9}$/.test(code)
    ? `${code.slice(0, 5)}-${code.slice(5)}`
    : code;
}

export function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

export function toDateInputValue(value: Date): string {
  return value.toISOString().slice(0, 10);
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

export function cleanOptional(value?: string | null): string | null {
  const trimmed = value?.trim() ?? "";

  return trimmed === "" ? null : trimmed;
}

export function shortAccountId(value?: string | null): string {
  const normalizedValue = cleanOptional(value);

  return normalizedValue === null ? "-" : normalizedValue.slice(0, 8);
}
