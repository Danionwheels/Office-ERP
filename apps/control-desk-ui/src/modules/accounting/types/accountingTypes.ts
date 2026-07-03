export type LedgerAccountSummary = {
  ledgerAccountId: string;
  code: string;
  displayCode: string;
  name: string;
  type: string;
  normalBalance: string;
  parentAccountId?: string | null;
  isPostingAccount: boolean;
  status: string;
  createdAtUtc: string;
  rangeRole?: string | null;
  rangeDisplayName?: string | null;
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

export type LedgerAccountActivity = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  fromDate?: string | null;
  toDate?: string | null;
  currencyCode?: string | null;
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
