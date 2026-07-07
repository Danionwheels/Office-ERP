import type { LucideIcon } from "lucide-react";
import type { ClientContract, ProductModule } from "../../contracts/types/contractTypes";
import type {
  ClientAccountingProfile,
  ClientDetails,
  ConfigureClientAccountingProfileInput
} from "../../clients/types/clientTypes";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRule,
  ClientChargeRuleFormInput,
  IssueCreditNoteInput,
  IssuedCreditNote,
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccountFormInput,
  VoidedInvoice,
  VoidInvoiceInput
} from "./billingTypes";

export type BillingStep = "accounting" | "rules" | "draft" | "issue";

export type ClientBillingSetupPanelProps = {
  client: ClientDetails | null;
  contracts: ClientContract[];
  productModules: ProductModule[];
  initialStep?: BillingStep;
  accountingProfile: ClientAccountingProfile | null;
  accountingProfileMissing: boolean;
  chargeCodes: ChargeCodeLookup[];
  receivableAccountValue: LedgerAccountFormInput;
  revenueAccountValue: LedgerAccountFormInput;
  accountingProfileValue: ConfigureClientAccountingProfileInput;
  chargeCodeValue: ChargeCodeFormInput;
  chargeRuleValue: ClientChargeRuleFormInput;
  invoiceDraftValue: InvoiceDraftFormInput;
  issueInvoiceValue: IssueInvoiceFormInput;
  latestChargeRule: ClientChargeRule | null;
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
  voidedInvoice: VoidedInvoice | null;
  issuedCreditNote: IssuedCreditNote | null;
  isBusy: boolean;
  onReceivableAccountChange: (value: LedgerAccountFormInput) => void;
  onRevenueAccountChange: (value: LedgerAccountFormInput) => void;
  onAccountingProfileChange: (value: ConfigureClientAccountingProfileInput) => void;
  onChargeCodeChange: (value: ChargeCodeFormInput) => void;
  onChargeRuleChange: (value: ClientChargeRuleFormInput) => void;
  onInvoiceDraftChange: (value: InvoiceDraftFormInput) => void;
  onIssueInvoiceChange: (value: IssueInvoiceFormInput) => void;
  onCreateReceivableAccount: () => Promise<void>;
  onCreateRevenueAccount: () => Promise<void>;
  onSaveAccountingProfile: () => Promise<void>;
  onCreateChargeCode: () => Promise<void>;
  onRefreshChargeCodes: () => Promise<void>;
  onCreateChargeRule: () => Promise<void>;
  onGenerateInvoiceDraft: () => Promise<void>;
  onIssueInvoice: () => Promise<void>;
  onVoidInvoice: (input: VoidInvoiceInput) => Promise<void>;
  onIssueCreditNote: (input: IssueCreditNoteInput) => Promise<void>;
  onViewJournalEntry: (journalEntryId: string) => Promise<void>;
};

export type ModuleBillingSuggestion = {
  module: ProductModule;
  contract: ClientContract;
  existingChargeCode: ChargeCodeLookup | null;
};

export type BillingStepItem = {
  step: BillingStep;
  label: string;
  summary: string;
  tone: "neutral" | "ready" | "warning";
  Icon: LucideIcon;
};

export type BillingPostingBridgeItem = {
  label: string;
  value: string;
  detail: string;
  tone: "neutral" | "ready" | "warning";
};

export type BillingJournalLine = {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  description?: string | null;
  ledgerAccountCode?: string | null;
  ledgerAccountName?: string | null;
  ledgerAccountType?: string | null;
  ledgerAccountNormalBalance?: string | null;
  ledgerAccountLevel?: string | null;
  isPostingAccount?: boolean | null;
  ledgerAccountStatus?: string | null;
};
