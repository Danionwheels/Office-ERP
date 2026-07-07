import {
  accountingCompanyCode,
  accountingCurrencyCode
} from "../constants/accountingConstants";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  AccountingControlSettings,
  AccountingControlSettingsInput,
  AccountingPeriod,
  AccountingPeriodFormInput,
  BalanceSheetFilters,
  LedgerAccountActivityFilters,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  OpeningBalanceImportInput,
  ProfitAndLossStatementFilters,
  TrialBalanceFilters
} from "../types/accountingTypes";
import {
  addDays,
  parseDateInput,
  toDateInputValue
} from "./accountingDates";

export const defaultLedgerAccountFilters: LedgerAccountFilters = {
  companyCode: accountingCompanyCode,
  search: "",
  type: "",
  status: "",
  posting: "",
  role: "",
  viewMode: "default",
  level: ""
};

export const emptyAccountCodeRangeForm: AccountCodeRangeFormInput = {
  displayName: "",
  searchPrefix: "",
  rangeStart: "",
  rangeEnd: "",
  codeLength: "",
  accountType: "Asset",
  normalBalance: "Debit",
  isPostingAccount: true,
  parentCode: "",
  isActive: true
};

export const emptyLedgerAccountEditorForm: LedgerAccountEditorInput = {
  code: "",
  name: "",
  type: "Asset",
  normalBalance: "Debit",
  level: "Detail",
  parentAccountId: "",
  isPostingAccount: true,
  status: "Active"
};

export const defaultAccountingControlSettingsForm: AccountingControlSettingsInput = {
  companyCode: accountingCompanyCode,
  baseCurrencyCode: accountingCurrencyCode,
  retainedEarningsAccountId: "",
  incomeSummaryAccountId: "",
  roundingAccountId: ""
};

export const defaultJournalEntryFilters = {
  fromDate: "",
  toDate: "",
  sourceType: ""
};

export function createDefaultTrialBalanceFilters(): TrialBalanceFilters {
  const today = new Date();
  const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);

  return {
    fromDate: toDateInputValue(monthStart),
    asOfDate: toDateInputValue(today),
    currencyCode: accountingCurrencyCode
  };
}

export function createDefaultProfitAndLossFilters(): ProfitAndLossStatementFilters {
  const today = new Date();
  const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);

  return {
    fromDate: toDateInputValue(monthStart),
    toDate: toDateInputValue(today),
    currencyCode: accountingCurrencyCode
  };
}

export function createDefaultBalanceSheetFilters(): BalanceSheetFilters {
  return {
    asOfDate: toDateInputValue(new Date()),
    currencyCode: accountingCurrencyCode
  };
}

export function createDefaultAccountingPeriodForm(
  periods: AccountingPeriod[] = [],
  companyCode = accountingCompanyCode
): AccountingPeriodFormInput {
  const nextPeriod = getNextMonthlyPeriod(periods);

  return {
    companyCode: normalizeAccountingCompanyCode(companyCode),
    name: formatAccountingPeriodName(nextPeriod.startsOn),
    startsOn: nextPeriod.startsOn,
    endsOn: nextPeriod.endsOn
  };
}

export function createDefaultAccountingControlSettingsForm(
  companyCode = accountingCompanyCode
): AccountingControlSettingsInput {
  return {
    ...defaultAccountingControlSettingsForm,
    companyCode: normalizeAccountingCompanyCode(companyCode)
  };
}

export function toAccountingControlSettingsForm(
  settings: AccountingControlSettings
): AccountingControlSettingsInput {
  return {
    companyCode: normalizeAccountingCompanyCode(settings.companyCode),
    baseCurrencyCode: settings.baseCurrencyCode,
    retainedEarningsAccountId: settings.retainedEarningsAccountId ?? "",
    incomeSummaryAccountId: settings.incomeSummaryAccountId ?? "",
    roundingAccountId: settings.roundingAccountId ?? ""
  };
}

export function toAccountCodeRangeForm(range: AccountCodeRange): AccountCodeRangeFormInput {
  return {
    displayName: range.displayName,
    searchPrefix: range.searchPrefix,
    rangeStart: range.rangeStart,
    rangeEnd: range.rangeEnd,
    codeLength: range.codeLength.toString(),
    accountType: range.accountType,
    normalBalance: range.normalBalance,
    isPostingAccount: range.isPostingAccount,
    parentCode: range.parentCode ?? "",
    isActive: range.isActive
  };
}

export function createDefaultLedgerAccountEditorForm(
  range?: AccountCodeRange | null
): LedgerAccountEditorInput {
  return {
    code: "",
    name: "",
    type: range?.accountType ?? "Asset",
    normalBalance: range?.normalBalance ?? "Debit",
    level: getDefaultLedgerAccountLevel(range),
    parentAccountId: "",
    isPostingAccount: range?.isPostingAccount ?? true,
    status: "Active"
  };
}

export function getDefaultLedgerAccountLevel(
  range?: AccountCodeRange | null,
  isPostingAccount = range?.isPostingAccount ?? true
): string {
  if (range !== null && range !== undefined) {
    if (hasLedgerRangeIntent(range, "Header")) {
      return "Header";
    }

    if (hasLedgerRangeIntent(range, "Total")) {
      return "Total";
    }

    if (hasLedgerRangeIntent(range, "Control")) {
      return "Control";
    }

    if (hasLedgerRangeIntent(range, "Master")) {
      return "Master";
    }

    if ((range.parentCode ?? "").trim() !== "") {
      return "Subsidiary";
    }
  }

  return isPostingAccount ? "Detail" : "Master";
}

export function toLedgerAccountEditorForm(
  account: LedgerAccountSummary
): LedgerAccountEditorInput {
  return {
    code: account.code,
    name: account.name,
    type: account.type,
    normalBalance: account.normalBalance,
    level: account.level ?? getDefaultLedgerAccountLevel(null, account.isPostingAccount),
    parentAccountId: account.parentAccountId ?? "",
    isPostingAccount: account.isPostingAccount,
    status: account.status
  };
}

export function createDefaultManualJournalEntryForm(
  value = new Date()
): ManualJournalEntryInput {
  return {
    entryDate: toDateInputValue(value),
    currencyCode: accountingCurrencyCode,
    sourceReference: "",
    memo: "",
    lines: [
      {
        ledgerAccountId: "",
        debit: "",
        credit: "",
        description: ""
      },
      {
        ledgerAccountId: "",
        debit: "",
        credit: "",
        description: ""
      }
    ]
  };
}

export function createDefaultOpeningBalanceImportForm(
  value = new Date()
): OpeningBalanceImportInput {
  const profileFromDate = new Date(value.getFullYear(), 0, 1);
  const profileToDate = new Date(value.getFullYear(), 11, 31);

  return {
    entryDate: toDateInputValue(value),
    currencyCode: accountingCurrencyCode,
    sourceReference: "",
    memo: "Opening balance import",
    profileFromDate: toDateInputValue(profileFromDate),
    profileToDate: toDateInputValue(profileToDate),
    profileStatus: "open",
    transactionsAllowed: true,
    profitAndLossCarryForwardAccountId: "",
    lines: [
      {
        accountCode: "",
        debit: "",
        credit: "",
        description: ""
      },
      {
        accountCode: "",
        debit: "",
        credit: "",
        description: ""
      }
    ]
  };
}

export function toLedgerAccountActivityFilters(
  filters: TrialBalanceFilters
): LedgerAccountActivityFilters {
  return {
    fromDate: filters.fromDate,
    toDate: filters.asOfDate,
    currencyCode: filters.currencyCode
  };
}

export function toProfitAndLossActivityFilters(
  filters: ProfitAndLossStatementFilters
): LedgerAccountActivityFilters {
  return {
    fromDate: filters.fromDate,
    toDate: filters.toDate,
    currencyCode: filters.currencyCode
  };
}

export function toBalanceSheetActivityFilters(
  filters: BalanceSheetFilters
): LedgerAccountActivityFilters {
  return {
    fromDate: "",
    toDate: filters.asOfDate,
    currencyCode: filters.currencyCode
  };
}

export function sortAccountCodeRanges(ranges: AccountCodeRange[]): AccountCodeRange[] {
  return [...ranges].sort((left, right) => {
    const rangeOrder = left.rangeStart.localeCompare(right.rangeStart);

    return rangeOrder !== 0 ? rangeOrder : left.role.localeCompare(right.role);
  });
}

export function withAccountingCompanyCode(filters: LedgerAccountFilters): LedgerAccountFilters {
  return {
    ...filters,
    companyCode: accountingCompanyCode
  };
}

export function normalizeAccountingCompanyCode(companyCode?: string): string {
  if (companyCode?.trim().toUpperCase() === accountingCompanyCode) {
    return accountingCompanyCode;
  }

  return accountingCompanyCode;
}

function getNextMonthlyPeriod(periods: AccountingPeriod[]): { startsOn: string; endsOn: string } {
  if (periods.length === 0) {
    const today = new Date();
    const startsOn = new Date(today.getFullYear(), today.getMonth(), 1);
    const endsOn = new Date(today.getFullYear(), today.getMonth() + 1, 0);

    return {
      startsOn: toDateInputValue(startsOn),
      endsOn: toDateInputValue(endsOn)
    };
  }

  const latestPeriod = [...periods].sort((left, right) =>
    right.endsOn.localeCompare(left.endsOn)
  )[0];
  const startsOn = addDays(parseDateInput(latestPeriod.endsOn), 1);
  const endsOn = new Date(startsOn.getFullYear(), startsOn.getMonth() + 1, 0);

  return {
    startsOn: toDateInputValue(startsOn),
    endsOn: toDateInputValue(endsOn)
  };
}

function formatAccountingPeriodName(startsOn: string): string {
  return parseDateInput(startsOn).toLocaleString("en-US", {
    month: "short",
    year: "numeric"
  });
}

function hasLedgerRangeIntent(range: AccountCodeRange, intent: string): boolean {
  const normalizedIntent = intent.toLowerCase();

  return range.role.toLowerCase().includes(normalizedIntent)
    || range.displayName.toLowerCase().includes(normalizedIntent);
}
