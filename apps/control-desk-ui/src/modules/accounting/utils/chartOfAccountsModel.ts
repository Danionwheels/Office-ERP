import {
  legacyAccountLevels,
  type LegacyAccountLevel,
  type LegacyAccountLevelCode
} from "../constants/accountingConstants";
import type {
  AccountCodeRange,
  LedgerAccountFilters,
  LedgerAccountSummary
} from "../types/accountingTypes";

export function getVisibleAccounts(
  accounts: LedgerAccountSummary[],
  ranges: AccountCodeRange[],
  filters: LedgerAccountFilters
): LedgerAccountSummary[] {
  const search = filters.search.trim().toLowerCase();

  return accounts.filter((account) => {
    if (
      search !== ""
      && !account.code.toLowerCase().includes(search)
      && !account.displayCode.toLowerCase().includes(search)
      && !account.name.toLowerCase().includes(search)
    ) {
      return false;
    }

    if (filters.type !== "" && account.type !== filters.type) {
      return false;
    }

    if (filters.status !== "" && account.status !== filters.status) {
      return false;
    }

    if (filters.posting === "posting" && !account.isPostingAccount) {
      return false;
    }

    if (filters.posting === "control" && account.isPostingAccount) {
      return false;
    }

    if (filters.role !== "") {
      const matchedRange = ranges.find((range) => range.role === filters.role);

      if (
        account.rangeRole !== filters.role
        && (matchedRange === undefined || !account.code.startsWith(matchedRange.searchPrefix))
      ) {
        return false;
      }
    }

    const level = getLegacyAccountLevel(account, ranges);

    if (filters.level !== "" && level.code !== filters.level) {
      return false;
    }

    if (filters.viewMode === "headerTotal") {
      return level.code === "H" || level.code === "T";
    }

    if (filters.viewMode === "default") {
      return level.code !== "H" && level.code !== "T";
    }

    return true;
  });
}

export function getLegacyAccountLevel(
  account: LedgerAccountSummary,
  ranges: AccountCodeRange[]
): LegacyAccountLevel {
  const persistedLevel = getPersistedLegacyAccountLevel(account.level);

  if (persistedLevel !== null) {
    return persistedLevel;
  }

  const range = getAccountRange(account, ranges);

  if (range !== null && isControlRange(range)) {
    return getLegacyAccountLevelDefinition("C");
  }

  if (!account.isPostingAccount) {
    return getLegacyAccountLevelDefinition("M");
  }

  if (account.parentAccountId !== null && account.parentAccountId !== undefined) {
    return getLegacyAccountLevelDefinition("S");
  }

  if ((range?.parentCode ?? "").trim() !== "") {
    return getLegacyAccountLevelDefinition("S");
  }

  return getLegacyAccountLevelDefinition("D");
}

export function getPersistedLegacyAccountLevel(
  value?: string | null
): LegacyAccountLevel | null {
  const normalized = value?.trim().toLowerCase() ?? "";

  if (normalized === "") {
    return null;
  }

  return legacyAccountLevels.find((level) =>
    level.label.toLowerCase() === normalized
    || level.code.toLowerCase() === normalized) ?? null;
}

export function getLedgerAccountLevelOptions(
  range: AccountCodeRange | null,
  accountMode: "create" | "edit",
  currentLevel: string
): LegacyAccountLevel[] {
  if (accountMode === "edit") {
    return ensureCurrentLevelOption(legacyAccountLevels, currentLevel);
  }

  if (range === null) {
    return legacyAccountLevels;
  }

  if (hasRangeIntent(range, "Header")) {
    return [getLegacyAccountLevelDefinition("H")];
  }

  if (hasRangeIntent(range, "Total")) {
    return [getLegacyAccountLevelDefinition("T")];
  }

  if (isControlRange(range)) {
    return [getLegacyAccountLevelDefinition("C")];
  }

  if (hasRangeIntent(range, "Master")) {
    return [getLegacyAccountLevelDefinition("M")];
  }

  if ((range.parentCode ?? "").trim() !== "") {
    return [getLegacyAccountLevelDefinition("S")];
  }

  if (range.isPostingAccount) {
    return [getLegacyAccountLevelDefinition("D")];
  }

  return [
    getLegacyAccountLevelDefinition("H"),
    getLegacyAccountLevelDefinition("T"),
    getLegacyAccountLevelDefinition("M"),
    getLegacyAccountLevelDefinition("C")
  ];
}

export function isPostingLedgerAccountLevel(level: string): boolean {
  const legacyLevel = getPersistedLegacyAccountLevel(level);

  return legacyLevel?.code === "D" || legacyLevel?.code === "S";
}

function ensureCurrentLevelOption(
  options: LegacyAccountLevel[],
  currentLevel: string
): LegacyAccountLevel[] {
  const current = getPersistedLegacyAccountLevel(currentLevel);

  if (current === null || options.some((option) => option.code === current.code)) {
    return options;
  }

  return [current, ...options];
}

function getAccountRange(
  account: LedgerAccountSummary,
  ranges: AccountCodeRange[]
): AccountCodeRange | null {
  const byRole = ranges.find((range) => range.role === account.rangeRole);

  if (byRole !== undefined) {
    return byRole;
  }

  return ranges.find((range) =>
    account.code.length === range.codeLength
    && account.code.startsWith(range.searchPrefix)
    && account.code >= range.rangeStart
    && account.code <= range.rangeEnd) ?? null;
}

function isControlRange(range: AccountCodeRange): boolean {
  return hasRangeIntent(range, "Control");
}

function hasRangeIntent(range: AccountCodeRange, intent: string): boolean {
  const normalizedIntent = intent.toLowerCase();

  return range.role.toLowerCase().includes(normalizedIntent)
    || range.displayName.toLowerCase().includes(normalizedIntent);
}

function getLegacyAccountLevelDefinition(
  code: LegacyAccountLevelCode
): LegacyAccountLevel {
  return legacyAccountLevels.find((level) => level.code === code) ?? legacyAccountLevels[0];
}
