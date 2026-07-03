export type LedgerAccount = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  isPostingAccount: boolean;
  status: string;
};

export type LedgerAccountCodeRole =
  | "ReceivableControl"
  | "ClientReceivable"
  | "CashBankControl"
  | "SubscriptionRevenue"
  | "CashBank"
  | "TaxPayable"
  | "Discount"
  | "Refund";

export type LedgerAccountCodeSuggestion = {
  companyCode: string;
  role: LedgerAccountCodeRole;
  suggestedCode: string;
  displayCode: string;
  type: string;
  normalBalance: string;
  isPostingAccount: boolean;
  rangeStart: string;
  rangeEnd: string;
  parentCode?: string | null;
};

export type LedgerAccountFormInput = {
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  parentAccountId: string;
  isPostingAccount: boolean;
};

export type ChargeCodeLookup = {
  chargeCodeId: string;
  code: string;
  name: string;
  defaultUnitPriceAmount: number;
  currencyCode: string;
  revenueAccountId: string;
  taxAccountId?: string | null;
  status: string;
};

export type ChargeCodeFormInput = {
  code: string;
  name: string;
  description: string;
  defaultUnitPriceAmount: string;
  currencyCode: string;
  revenueAccountId: string;
  taxAccountId: string;
};

export type ClientChargeRule = {
  clientChargeRuleId: string;
  clientId: string;
  contractId?: string | null;
  chargeCodeId: string;
  productModuleCode?: string | null;
  unitPriceAmount: number;
  currencyCode: string;
  quantity: number;
  taxPercent: number;
  taxAmount: number;
  lineAmount: number;
  totalLineAmount: number;
  billingCycle: string;
  billingDayOfMonth: number;
  effectiveStartsOn: string;
  effectiveEndsOn: string;
  status: string;
};

export type ClientChargeRuleFormInput = {
  contractId: string;
  chargeCodeId: string;
  productModuleCode: string;
  descriptionOverride: string;
  unitPriceAmount: string;
  currencyCode: string;
  quantity: string;
  taxPercent: string;
  billingCycle: string;
  billingDayOfMonth: string;
  effectiveStartsOn: string;
  effectiveEndsOn: string;
};

export type InvoiceDraft = {
  invoiceId: string;
  clientId: string;
  contractId: string;
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  billingDate: string;
  totalAmount: number;
  balanceDue: number;
  currencyCode: string;
  status: string;
  lines: InvoiceDraftLine[];
};

export type InvoiceDocument = {
  invoice: InvoiceDraft;
  issuedInvoice?: IssuedInvoice | null;
  voidedInvoice?: VoidedInvoice | null;
  creditNote?: IssuedCreditNote | null;
};

export type CreditNoteDocument = {
  invoice: InvoiceDraft;
  creditNote: IssuedCreditNote;
};

export type InvoiceDraftLine = {
  chargeCodeId?: string | null;
  productModuleCode?: string | null;
  lineType: string;
  description: string;
  amount: number;
  currencyCode: string;
};

export type InvoiceDraftFormInput = {
  contractId: string;
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  billingDate: string;
  currencyCode: string;
};

export type IssueInvoiceFormInput = {
  postingDate: string;
  accountsReceivableAccountId: string;
};

export type IssuedInvoice = {
  invoiceId: string;
  invoiceNumber: string;
  invoiceStatus: string;
  journalEntryId: string;
  journalEntryStatus: string;
  postingDate: string;
  totalDebit: number;
  totalCredit: number;
  currencyCode: string;
  journalLines: IssuedInvoiceJournalLine[];
};

export type IssuedInvoiceJournalLine = {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  description?: string | null;
};

export type VoidInvoiceInput = {
  voidDate: string;
  reason: string;
};

export type VoidedInvoice = {
  invoiceId: string;
  invoiceNumber: string;
  invoiceStatus: string;
  originalJournalEntryId: string;
  reversalJournalEntryId: string;
  reversalJournalEntryStatus: string;
  voidDate: string;
  totalDebit: number;
  totalCredit: number;
  currencyCode: string;
  journalLines: VoidedInvoiceJournalLine[];
};

export type VoidedInvoiceJournalLine = {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  description?: string | null;
};

export type IssueCreditNoteInput = {
  creditNoteNumber: string;
  creditDate: string;
  reason: string;
};

export type IssuedCreditNote = {
  creditNoteId: string;
  invoiceId: string;
  creditNoteNumber: string;
  invoiceNumber: string;
  creditNoteStatus: string;
  creditDate: string;
  amount: number;
  currencyCode: string;
  journalEntryId: string;
  journalEntryStatus: string;
  totalDebit: number;
  totalCredit: number;
  journalLines: IssuedCreditNoteJournalLine[];
};

export type IssuedCreditNoteJournalLine = {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  description?: string | null;
};
