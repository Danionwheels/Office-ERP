export type ReportKey =
  | "aging"
  | "revenue"
  | "outstanding-invoices"
  | "payment-receipts"
  | "trial-balance";

export type ReportClientLookup = {
  clientId: string;
  code: string;
  displayName: string;
  legalName: string;
  status: string;
};

export type ReportClientDirectoryPage = {
  clients: ReportClientLookup[];
  hasMore: boolean;
  nextCursor?: string | null;
};

export type AccountsReceivableAgingFilters = {
  asOfDate: string;
  currencyCode: string;
};

export type AccountsReceivableAgingCurrency = {
  currencyCode: string;
  currentAmount: number;
  days1To30Amount: number;
  days31To60Amount: number;
  days61To90Amount: number;
  daysOver90Amount: number;
  totalOutstanding: number;
  invoiceCount: number;
  clientCount: number;
};

export type AccountsReceivableAgingClient = {
  clientId: string;
  clientCode: string;
  clientName: string;
  currencyCode: string;
  currentAmount: number;
  days1To30Amount: number;
  days31To60Amount: number;
  days61To90Amount: number;
  daysOver90Amount: number;
  totalOutstanding: number;
  invoiceCount: number;
};

export type AccountsReceivableAgingReport = {
  asOfDate: string;
  currencies: AccountsReceivableAgingCurrency[];
  clients: AccountsReceivableAgingClient[];
};

export type RevenuePeriod = "Monthly" | "Quarterly";

export type RevenueSummaryFilters = {
  fromDate: string;
  toDate: string;
  period: RevenuePeriod;
  currencyCode: string;
};

export type RevenueSummaryPeriod = {
  periodStart: string;
  periodEnd: string;
  label: string;
  debit: number;
  credit: number;
  revenue: number;
  activityCount: number;
};

export type RevenueSummaryReport = {
  fromDate: string;
  toDate: string;
  period: RevenuePeriod;
  currencyCode: string;
  totalRevenue: number;
  periods: RevenueSummaryPeriod[];
};

export type OutstandingInvoiceFilters = {
  clientId: string;
  fromDate: string;
  toDate: string;
  minAmount: string;
  maxAmount: string;
  status: string;
  currencyCode: string;
};

export type OutstandingInvoiceRow = {
  invoiceId: string;
  clientId: string;
  clientCode: string;
  clientName: string;
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  status: string;
  totalAmount: number;
  amountPaid: number;
  balanceDue: number;
  currencyCode: string;
  daysOverdue: number;
  agingBucket: string;
  journalEntryId: string | null;
};

export type OutstandingInvoicesReport = {
  invoices: OutstandingInvoiceRow[];
  pageSize: number;
  hasMore: boolean;
  nextCursor: string | null;
  filteredCount: number;
};

export type PaymentReceiptsFilters = {
  clientId: string;
  fromDate: string;
  toDate: string;
  method: string;
  status: string;
  currencyCode: string;
};

export type PaymentReceiptRow = {
  paymentId: string;
  clientId: string;
  clientCode: string;
  clientName: string;
  invoiceId: string;
  invoiceNumber: string;
  reference: string;
  method: string;
  status: string;
  amount: number;
  currencyCode: string;
  receivedOn: string;
  journalEntryId: string | null;
};

export type PaymentReceiptsReport = {
  payments: PaymentReceiptRow[];
  pageSize: number;
  hasMore: boolean;
  nextCursor: string | null;
  filteredCount: number;
};

export type TrialBalanceFilters = {
  fromDate: string;
  asOfDate: string;
  currencyCode: string;
};

export type TrialBalanceLine = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  openingBalance: number;
  periodDebit: number;
  periodCredit: number;
  debitBalance: number;
  creditBalance: number;
  netBalance: number;
  activityCount: number;
};

export type TrialBalanceReport = {
  fromDate: string | null;
  asOfDate: string;
  currencyCode: string;
  totalDebit: number;
  totalCredit: number;
  totalPeriodDebit: number;
  totalPeriodCredit: number;
  difference: number;
  lines: TrialBalanceLine[];
};

export type LedgerAccountActivity = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  fromDate?: string | null;
  toDate?: string | null;
  currencyCode?: string | null;
  openingBalance: number;
  periodDebit: number;
  periodCredit: number;
  endingBalance: number;
  lines: LedgerAccountActivityLine[];
};

export type LedgerAccountActivityLine = {
  journalEntryId: string;
  entryDate: string;
  sourceType: string;
  sourceReference?: string | null;
  memo?: string | null;
  status: string;
  debit: number;
  credit: number;
  runningBalance: number;
  currencyCode: string;
  description?: string | null;
};
