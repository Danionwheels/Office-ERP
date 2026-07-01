export type LedgerAccount = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  isPostingAccount: boolean;
  status: string;
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
  unitPriceAmount: number;
  currencyCode: string;
  quantity: number;
  lineAmount: number;
  billingCycle: string;
  billingDayOfMonth: number;
  effectiveStartsOn: string;
  effectiveEndsOn: string;
  status: string;
};

export type ClientChargeRuleFormInput = {
  contractId: string;
  chargeCodeId: string;
  descriptionOverride: string;
  unitPriceAmount: string;
  currencyCode: string;
  quantity: string;
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

export type InvoiceDraftLine = {
  chargeCodeId?: string | null;
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
