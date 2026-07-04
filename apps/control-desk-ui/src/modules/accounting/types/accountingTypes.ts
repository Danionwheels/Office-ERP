export type LedgerAccountSummary = {
  ledgerAccountId: string;
  code: string;
  displayCode: string;
  name: string;
  type: string;
  normalBalance: string;
  level?: string | null;
  parentAccountId?: string | null;
  isPostingAccount: boolean;
  status: string;
  createdAtUtc: string;
  rangeRole?: string | null;
  rangeDisplayName?: string | null;
};

export type LedgerAccountReconciliation = {
  companyCode: string;
  accountCount: number;
  issueCount: number;
  items: LedgerAccountReconciliationItem[];
};

export type LedgerAccountReconciliationItem = {
  ledgerAccountId: string;
  code: string;
  displayCode: string;
  name: string;
  type: string;
  normalBalance: string;
  level: string;
  parentAccountId?: string | null;
  isPostingAccount: boolean;
  status: string;
  rangeRole?: string | null;
  rangeDisplayName?: string | null;
  issues: LedgerAccountReconciliationIssue[];
};

export type LedgerAccountReconciliationIssue = {
  severity: string;
  code: string;
  message: string;
};

export type LedgerAccountRepairPlan = {
  companyCode: string;
  accountCount: number;
  issueCount: number;
  actionCount: number;
  items: LedgerAccountRepairPlanItem[];
};

export type LedgerAccountRepairPlanItem = {
  ledgerAccountId: string;
  code: string;
  displayCode: string;
  name: string;
  type: string;
  normalBalance: string;
  level: string;
  parentAccountId?: string | null;
  isPostingAccount: boolean;
  status: string;
  rangeRole?: string | null;
  rangeDisplayName?: string | null;
  actions: LedgerAccountRepairAction[];
};

export type LedgerAccountRepairAction = {
  issueCode: string;
  severity: string;
  actionCode: string;
  title: string;
  description: string;
  repairMode: string;
  isAutomatable: boolean;
  currentValue?: string | null;
  suggestedValue?: string | null;
  notes: string[];
};

export type LedgerAccountCodeSuggestion = {
  companyCode: string;
  role: string;
  suggestedCode: string;
  displayCode: string;
  type: string;
  normalBalance: string;
  isPostingAccount: boolean;
  rangeStart: string;
  rangeEnd: string;
  parentCode?: string | null;
};

export type LedgerAccountEditorInput = {
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  level: string;
  parentAccountId: string;
  isPostingAccount: boolean;
  status: string;
};

export type AccountCodeRange = {
  accountCodeRangeId: string;
  companyCode: string;
  role: string;
  displayName: string;
  searchPrefix: string;
  rangeStart: string;
  rangeEnd: string;
  codeLength: number;
  accountType: string;
  normalBalance: string;
  isPostingAccount: boolean;
  parentCode?: string | null;
  isActive: boolean;
};

export type AccountCodeRangeFormInput = {
  displayName: string;
  searchPrefix: string;
  rangeStart: string;
  rangeEnd: string;
  codeLength: string;
  accountType: string;
  normalBalance: string;
  isPostingAccount: boolean;
  parentCode: string;
  isActive: boolean;
};

export type AccountingControlSettings = {
  companyCode: string;
  baseCurrencyCode: string;
  retainedEarningsAccountId?: string | null;
  retainedEarningsAccount?: AccountingControlAccount | null;
  incomeSummaryAccountId?: string | null;
  incomeSummaryAccount?: AccountingControlAccount | null;
  roundingAccountId?: string | null;
  roundingAccount?: AccountingControlAccount | null;
  isConfigured: boolean;
  createdAtUtc?: string | null;
  updatedAtUtc?: string | null;
};

export type AccountingControlAccount = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  status: string;
};

export type AccountingControlSettingsInput = {
  companyCode: string;
  baseCurrencyCode: string;
  retainedEarningsAccountId: string;
  incomeSummaryAccountId: string;
  roundingAccountId: string;
};

export type AccountingPeriod = {
  accountingPeriodId: string;
  companyCode: string;
  name: string;
  startsOn: string;
  endsOn: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  closedAtUtc?: string | null;
  reopenedAtUtc?: string | null;
  closeArtifact?: AccountingPeriodCloseArtifact | null;
};

export type AccountingPeriodFormInput = {
  companyCode: string;
  name: string;
  startsOn: string;
  endsOn: string;
};

export type AccountingPeriodCloseReadiness = {
  period: AccountingPeriod;
  canClose: boolean;
  checks: AccountingPeriodCloseReadinessCheck[];
  currencies: AccountingPeriodCloseCurrency[];
};

export type AccountingPeriodCloseJournalPreview = {
  period: AccountingPeriod;
  baseCurrencyCode: string;
  canGenerate: boolean;
  netIncome: number;
  totalDebit: number;
  totalCredit: number;
  blockers: string[];
  entries: AccountingCloseJournalPreviewEntry[];
};

export type AccountingCloseJournalPreviewEntry = {
  sourceReference: string;
  memo: string;
  entryDate: string;
  currencyCode: string;
  totalDebit: number;
  totalCredit: number;
  lines: AccountingCloseJournalPreviewLine[];
};

export type AccountingCloseJournalPreviewLine = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  debit: number;
  credit: number;
  description: string;
};

export type AccountingPeriodCloseReadinessCheck = {
  code: string;
  status: string;
  message: string;
  target?: string | null;
};

export type AccountingPeriodCloseCurrency = {
  currencyCode: string;
  totalDebit: number;
  totalCredit: number;
  difference: number;
  postedJournalCount: number;
  draftJournalCount: number;
};

export type AccountingPeriodCloseArtifact = {
  generatedAtUtc: string;
  generatedBy: string;
  checkCount: number;
  blockedCheckCount: number;
  currencyCount: number;
  postedJournalCount: number;
  draftJournalCount: number;
  checks: AccountingPeriodCloseReadinessCheck[];
  currencies: AccountingPeriodCloseCurrency[];
  closeJournalEntries: AccountingPeriodCloseJournalArtifact[];
};

export type AccountingPeriodCloseJournalArtifact = {
  journalEntryId: string;
  sourceReference: string;
  memo: string;
  entryDate: string;
  currencyCode: string;
  totalDebit: number;
  totalCredit: number;
};

export type LedgerAccountFilters = {
  companyCode: string;
  search: string;
  type: string;
  status: string;
  posting: string;
  role: string;
  viewMode: string;
  level: string;
};

export type JournalEntryLine = {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  description?: string | null;
};

export type JournalEntrySummary = {
  journalEntryId: string;
  entryDate: string;
  currencyCode: string;
  sourceType: string;
  sourceReference?: string | null;
  memo?: string | null;
  status: string;
  totalDebit: number;
  totalCredit: number;
  lines: JournalEntryLine[];
};

export type JournalEntrySourceDocument = {
  journalEntryId: string;
  sourceType: string;
  sourceReference?: string | null;
  isResolved: boolean;
  documentKind?: string | null;
  documentId?: string | null;
  clientId?: string | null;
  relatedInvoiceId?: string | null;
  reference?: string | null;
  status?: string | null;
  documentDate?: string | null;
  currencyCode?: string | null;
  amount?: number | null;
  label?: string | null;
  dashboardModule?: string | null;
  dashboardStep?: string | null;
  message?: string | null;
};

export type VoidManualJournalEntryInput = {
  voidDate: string;
  reason: string;
};

export type VoidManualJournalEntryResult = {
  originalJournalEntryId: string;
  reversalJournalEntryId: string;
  originalJournalEntryStatus: string;
  reversalJournalEntryStatus: string;
  voidDate: string;
  totalDebit: number;
  totalCredit: number;
  currencyCode: string;
  lines: JournalEntryLine[];
};

export type JournalEntryFilters = {
  fromDate: string;
  toDate: string;
  sourceType: string;
};

export type JournalVoucherNumberPreview = {
  sourceType: string;
  entryDate: string;
  prefix: string;
  sequenceYear: number;
  nextSequence: number;
  reference: string;
};

export type ManualJournalEntryInput = {
  entryDate: string;
  currencyCode: string;
  sourceReference: string;
  memo: string;
  lines: ManualJournalEntryLineInput[];
};

export type ManualJournalEntryLineInput = {
  ledgerAccountId: string;
  debit: string;
  credit: string;
  description: string;
};

export type OpeningBalanceImportInput = {
  entryDate: string;
  currencyCode: string;
  sourceReference: string;
  memo: string;
  lines: OpeningBalanceImportLineInput[];
};

export type OpeningBalanceImportLineInput = {
  accountCode: string;
  debit: string;
  credit: string;
  description: string;
};

export type OpeningBalanceImportPreview = {
  entryDate: string;
  currencyCode: string;
  sourceReference: string;
  memo?: string | null;
  canPost: boolean;
  totalDebit: number;
  totalCredit: number;
  difference: number;
  importedLineCount: number;
  validLineCount: number;
  invalidLineCount: number;
  blockers: string[];
  lines: OpeningBalanceImportPreviewLine[];
};

export type OpeningBalanceImportTextPreview = {
  format: string;
  parsedLineCount: number;
  ignoredLineCount: number;
  parseIssues: OpeningBalanceImportTextParseIssue[];
  preview: OpeningBalanceImportPreview;
};

export type OpeningBalanceImportTextParseIssue = {
  lineNumber: number;
  column: string;
  message: string;
  rawValue?: string | null;
};

export type OpeningBalanceImportPreviewLine = {
  lineNumber: number;
  accountCode: string;
  ledgerAccountId?: string | null;
  ledgerAccountName?: string | null;
  accountType?: string | null;
  normalBalance?: string | null;
  debit: number;
  credit: number;
  description?: string | null;
  isValid: boolean;
  issues: string[];
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

export type TrialBalance = {
  fromDate?: string | null;
  asOfDate: string;
  currencyCode: string;
  totalDebit: number;
  totalCredit: number;
  totalPeriodDebit: number;
  totalPeriodCredit: number;
  difference: number;
  lines: TrialBalanceLine[];
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

export type TrialBalanceFilters = {
  fromDate: string;
  asOfDate: string;
  currencyCode: string;
};

export type ProfitAndLossStatement = {
  fromDate?: string | null;
  toDate: string;
  currencyCode: string;
  totalRevenue: number;
  totalExpense: number;
  netIncome: number;
  sections: ProfitAndLossStatementSection[];
};

export type ProfitAndLossStatementSection = {
  type: string;
  title: string;
  total: number;
  lines: ProfitAndLossStatementLine[];
};

export type ProfitAndLossStatementLine = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  debit: number;
  credit: number;
  amount: number;
  activityCount: number;
};

export type ProfitAndLossStatementFilters = {
  fromDate: string;
  toDate: string;
  currencyCode: string;
};

export type BalanceSheet = {
  asOfDate: string;
  currencyCode: string;
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  totalLiabilitiesAndEquity: number;
  difference: number;
  sections: BalanceSheetSection[];
};

export type BalanceSheetSection = {
  type: string;
  title: string;
  total: number;
  lines: BalanceSheetLine[];
};

export type BalanceSheetLine = {
  ledgerAccountId?: string | null;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  debit: number;
  credit: number;
  amount: number;
  activityCount: number;
  isSystemLine: boolean;
};

export type BalanceSheetFilters = {
  asOfDate: string;
  currencyCode: string;
};

export type LedgerAccountActivityFilters = {
  fromDate: string;
  toDate: string;
  currencyCode: string;
};
