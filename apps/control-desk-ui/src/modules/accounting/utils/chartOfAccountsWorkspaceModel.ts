import type { ApiErrorItem } from "../../../shared/api/apiError";
import {
  accountingCurrencyCode,
  legacyAccountLevels,
  type LegacyAccountLevel,
  type LegacyAccountLevelCode
} from "../constants/accountingConstants";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  AccountCodeRangeValidation,
  AccountCodeRangeValidationIssue,
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  LedgerAccountCreateContext,
  LedgerAccountEditorInput,
  JournalEntrySummary,
  LedgerAccountSummary
} from "../types/accountingTypes";
import { getLegacyAccountLevel } from "./chartOfAccountsModel";

export type AccountTreeRow = {
  account: LedgerAccountSummary;
  childCount: number;
  depth: number;
  hasChildren: boolean;
  isMatched: boolean;
  level: LegacyAccountLevel;
  parentAccountId: string | null;
};

export type LedgerActivityStatusItem = {
  label: string;
  value: string;
  tone: "ready" | "warning" | "neutral";
};

export type CoaInlineCreateStatusItem = {
  label: string;
  value: string;
  tone: "ready" | "warning" | "neutral";
  title?: string;
};

export type CoaTreeRowContextItem = {
  label: string;
  tone: "ready" | "warning" | "neutral" | "debit" | "credit";
  title?: string;
};

export type CoaTreeRowContextOptions = {
  childCreateContext: LedgerAccountCreateContext | null;
  depth: number;
  parentAccount: LedgerAccountSummary | null;
};

export type CoaChildCodeBounds = {
  start: bigint;
  end: bigint;
};

export type CoaRangeFact = {
  label: string;
  value: string;
  tone: "ready" | "warning" | "danger" | "neutral";
  title?: string;
};

export type CoaRangeIssueGroup = {
  code: string;
  count: number;
  tone: "warning" | "danger" | "neutral";
  title: string;
};
export function getCreateRangeForAccount(
  account: LedgerAccountSummary,
  ranges: AccountCodeRange[]
): AccountCodeRange | null {
  const byRole = ranges.find((range) => range.role === account.rangeRole);

  if (byRole !== undefined) {
    return byRole;
  }

  return ranges.find((range) => rangeContainsAccount(range, account)) ?? null;
}

export function getChildCreateRangeForAccount(
  account: LedgerAccountSummary,
  level: LegacyAccountLevel,
  ranges: AccountCodeRange[],
  accounts: LedgerAccountSummary[]
): AccountCodeRange | null {
  if (account.status !== "Active") {
    return null;
  }

  return ranges
    .filter((range) =>
      isUsableChildRangeForAccount(range, account, level, accounts))
    .sort((left, right) => compareChildCreateRanges(left, right, account, level))[0] ?? null;
}

export function getDefaultChildLevelForRange(
  range: AccountCodeRange,
  parentLevel: LegacyAccountLevel
): string {
  return getLegacyLevelLabel(getDefaultChildLevelCodeForRange(range, parentLevel));
}

export function hasChildCodeCapacity(range: AccountCodeRange, account: LedgerAccountSummary): boolean {
  if (account.code.length < range.codeLength) {
    return true;
  }

  return range.rangeStart !== range.rangeEnd || range.rangeStart !== account.code;
}

export function isUsableChildRangeForAccount(
  range: AccountCodeRange,
  account: LedgerAccountSummary,
  parentLevel: LegacyAccountLevel,
  accounts: LedgerAccountSummary[]
): boolean {
  if (!range.isActive) {
    return false;
  }

  if (range.accountType !== account.type || range.normalBalance !== account.normalBalance) {
    return false;
  }

  const childLevelCode = getDefaultChildLevelCodeForRange(range, parentLevel);

  if (!getPreferredChildLevelOrder(parentLevel.code).includes(childLevelCode)) {
    return false;
  }

  if (!canParentOwnChildLevel(account, parentLevel, range, childLevelCode)) {
    return false;
  }

  return hasAvailableChildCode(range, account, childLevelCode, accounts);
}

export function compareChildCreateRanges(
  left: AccountCodeRange,
  right: AccountCodeRange,
  account: LedgerAccountSummary,
  parentLevel: LegacyAccountLevel
): number {
  const leftLevel = getDefaultChildLevelCodeForRange(left, parentLevel);
  const rightLevel = getDefaultChildLevelCodeForRange(right, parentLevel);
  const leftExplicit = (left.parentCode?.trim() ?? "") === account.code;
  const rightExplicit = (right.parentCode?.trim() ?? "") === account.code;

  if (leftExplicit !== rightExplicit) {
    return leftExplicit ? -1 : 1;
  }

  const levelOrder =
    getPreferredChildLevelRank(parentLevel.code, leftLevel)
    - getPreferredChildLevelRank(parentLevel.code, rightLevel);

  if (levelOrder !== 0) {
    return levelOrder;
  }

  const prefixOrder =
    getCommonPrefixLength(right.rangeStart, account.code)
    - getCommonPrefixLength(left.rangeStart, account.code);

  if (prefixOrder !== 0) {
    return prefixOrder;
  }

  return left.rangeStart.localeCompare(right.rangeStart);
}

export function getDefaultChildLevelCodeForRange(
  range: AccountCodeRange,
  parentLevel: LegacyAccountLevel
): LegacyAccountLevelCode {
  const rangeLevel = formatRangeLevelRule(range);

  if (rangeLevel === "Header") {
    return "H";
  }

  if (rangeLevel === "Total") {
    return "T";
  }

  if (rangeLevel === "Control") {
    return "C";
  }

  if (rangeLevel === "Master") {
    return "M";
  }

  if (rangeLevel === "Subsidiary") {
    return "S";
  }

  if (range.isPostingAccount) {
    return parentLevel.code === "M" ? "D" : "S";
  }

  return "M";
}

export function getPreferredChildLevelOrder(parentLevelCode: LegacyAccountLevelCode): LegacyAccountLevelCode[] {
  switch (parentLevelCode) {
    case "H":
    case "T":
      return ["S", "D", "M", "C"];
    case "M":
      return ["D", "S", "C", "M"];
    case "C":
      return ["S", "C", "M"];
    case "D":
      return ["D", "S"];
    case "S":
      return ["S", "D"];
    default:
      return [];
  }
}

export function getPreferredChildLevelRank(
  parentLevelCode: LegacyAccountLevelCode,
  childLevelCode: LegacyAccountLevelCode
): number {
  const order = getPreferredChildLevelOrder(parentLevelCode);
  const index = order.indexOf(childLevelCode);

  return index === -1 ? 99 : index;
}

export function canParentOwnChildLevel(
  account: LedgerAccountSummary,
  parentLevel: LegacyAccountLevel,
  range: AccountCodeRange,
  childLevelCode: LegacyAccountLevelCode
): boolean {
  const configuredParentCode = range.parentCode?.trim() ?? "";

  if (configuredParentCode !== "" && !isParentInsideRangeFamily(range, account.code)) {
    return false;
  }

  if (childLevelCode === "S") {
    return range.isPostingAccount;
  }

  if (childLevelCode === "D") {
    return range.isPostingAccount;
  }

  return isStructuralLevelCode(childLevelCode)
    && isStructuralLevelCode(parentLevel.code)
    && !account.isPostingAccount;
}

export function hasAvailableChildCode(
  range: AccountCodeRange,
  account: LedgerAccountSummary,
  childLevelCode: LegacyAccountLevelCode,
  accounts: LedgerAccountSummary[]
): boolean {
  const bounds = getChildCodeBounds(range, account, childLevelCode);

  if (bounds === null || bounds.end < bounds.start) {
    return false;
  }

  const usedCount = accounts.reduce((count, existingAccount) => {
    if (!rangeContainsAccount(range, existingAccount)) {
      return count;
    }

    const code = parseCodeNumber(existingAccount.code);

    if (code === null || code < bounds.start || code > bounds.end) {
      return count;
    }

    return count + 1n;
  }, 0n);

  return usedCount < bounds.end - bounds.start + 1n;
}

export function getChildCodeBounds(
  range: AccountCodeRange,
  account: LedgerAccountSummary,
  childLevelCode: LegacyAccountLevelCode
): CoaChildCodeBounds | null {
  const rangeStart = parseCodeNumber(range.rangeStart);
  const rangeEnd = parseCodeNumber(range.rangeEnd);

  if (rangeStart === null || rangeEnd === null) {
    return null;
  }

  if (!range.isPostingAccount || (childLevelCode !== "D" && childLevelCode !== "S")) {
    return { start: rangeStart, end: rangeEnd };
  }

  const configuredParentCode = range.parentCode?.trim() ?? "";

  if (configuredParentCode !== "") {
    if (!isParentInsideRangeFamily(range, account.code)) {
      return null;
    }

    if (account.code === configuredParentCode) {
      return { start: rangeStart, end: rangeEnd };
    }
  }

  if (!isNumericCode(account.code)) {
    return null;
  }

  if (account.code.length < range.codeLength) {
    const scopedStart = parseCodeNumber(account.code.padEnd(range.codeLength, "0"));
    const scopedEnd = parseCodeNumber(account.code.padEnd(range.codeLength, "9"));

    if (scopedStart === null || scopedEnd === null) {
      return null;
    }

    return {
      start: maxBigInt(rangeStart, scopedStart + 1n),
      end: minBigInt(rangeEnd, scopedEnd)
    };
  }

  if (account.code.length === range.codeLength) {
    const parentCode = parseCodeNumber(account.code);

    if (parentCode === null || !account.code.startsWith(range.searchPrefix)) {
      return null;
    }

    return {
      start: maxBigInt(rangeStart, parentCode + 1n),
      end: rangeEnd
    };
  }

  return null;
}

export function parseCodeNumber(value: string): bigint | null {
  return isNumericCode(value) ? BigInt(value) : null;
}

export function isNumericCode(value: string): boolean {
  return /^\d+$/.test(value);
}

export function maxBigInt(left: bigint, right: bigint): bigint {
  return left > right ? left : right;
}

export function minBigInt(left: bigint, right: bigint): bigint {
  return left < right ? left : right;
}

export function isStructuralLevelCode(levelCode: LegacyAccountLevelCode): boolean {
  return levelCode === "H" || levelCode === "T" || levelCode === "M" || levelCode === "C";
}

export function getLegacyLevelLabel(levelCode: LegacyAccountLevelCode): string {
  return legacyAccountLevels.find((level) => level.code === levelCode)?.label ?? "Detail";
}

export function getChildCreateDisabledReason(
  account: LedgerAccountSummary,
  level: LegacyAccountLevel,
  ranges: AccountCodeRange[],
  accounts: LedgerAccountSummary[]
): string {
  const hasInactiveChildRange = ranges.some((range) =>
    !range.isActive
    && (range.parentCode?.trim() ?? "") === account.code);
  const accountRange = getCreateRangeForAccount(account, ranges);
  const matchingChildRanges = ranges.filter((range) =>
    range.isActive
    && canParentOwnChildLevel(account, level, range, getDefaultChildLevelCodeForRange(range, level))
    && getPreferredChildLevelOrder(level.code).includes(getDefaultChildLevelCodeForRange(range, level))
    && range.accountType === account.type
    && range.normalBalance === account.normalBalance);

  if (account.status !== "Active") {
    return "Reactivate this account before adding children";
  }

  if (hasInactiveChildRange) {
    return "Activate this account's child range before adding children";
  }

  if (accountRange !== null && !accountRange.isActive) {
    return "Activate this account's range before adding children";
  }

  if (accountRange !== null && !hasChildCodeCapacity(accountRange, account)) {
    return "This account range has no child code capacity";
  }

  if (matchingChildRanges.length > 0) {
    const allMatchingRangesAreFull = matchingChildRanges.every((range) =>
      !hasAvailableChildCode(range, account, getDefaultChildLevelCodeForRange(range, level), accounts));

    if (allMatchingRangesAreFull) {
      return "All valid child ranges for this account are full";
    }
  }

  if (level.code === "D" || level.code === "S" || account.isPostingAccount) {
    return "Configure an available child range for this posting account";
  }

  if (level.code === "C") {
    return "Configure an available Subsidiary, Control, or Master child range";
  }

  if (level.code === "M") {
    return "Configure an available Detail, Subsidiary, Control, or Master child range";
  }

  if (level.code === "H" || level.code === "T") {
    return "Configure an available posting or structural child range for this section";
  }

  return "This structural level is maintained through account ranges";
}

export function getInlineCreateParentDisplay(
  parentAccount: LedgerAccountSummary | null,
  parentAccountId: string,
  selectedRange: AccountCodeRange | null
): string {
  if (parentAccount !== null) {
    return `Parent ${parentAccount.displayCode} / ${parentAccount.name}`;
  }

  const normalizedParentAccountId = parentAccountId.trim();

  if (normalizedParentAccountId !== "") {
    return `Parent ${normalizedParentAccountId}`;
  }

  const parentCode = selectedRange?.parentCode?.trim() ?? "";

  if (parentCode !== "") {
    return `Parent code ${parentCode}`;
  }

  return "Top-level account";
}

export function getInlineCreateRuleDisplay(
  selectedRange: AccountCodeRange | null,
  accountValue: LedgerAccountEditorInput
): string {
  const level = accountValue.level.trim() === "" ? "Level" : accountValue.level;
  const type = accountValue.type.trim() === "" ? "Type" : accountValue.type;
  const balance = accountValue.normalBalance.trim() === "" ? "Balance" : accountValue.normalBalance;
  const posting = accountValue.isPostingAccount ? "Posting" : "Non-posting";

  if (selectedRange === null) {
    return `${level} / ${type} / ${balance} / ${posting}`;
  }

  return `${selectedRange.displayName} / ${formatRange(selectedRange)} / ${level} / ${type} / ${balance} / ${posting}`;
}

export function getInlineCreateStatusItems({
  accountSaveErrors,
  accountValue,
  canSaveAccount,
  depthLabel,
  parentAccount,
  selectedRange
}: {
  accountSaveErrors: ApiErrorItem[];
  accountValue: LedgerAccountEditorInput;
  canSaveAccount: boolean;
  depthLabel: string;
  parentAccount: LedgerAccountSummary | null;
  selectedRange: AccountCodeRange | null;
}): CoaInlineCreateStatusItem[] {
  const parentAccountId = accountValue.parentAccountId.trim();
  const parentCode = selectedRange?.parentCode?.trim() ?? "";
  const hasParent = parentAccount !== null || parentAccountId !== "" || parentCode !== "";
  const code = accountValue.code.trim();
  const name = accountValue.name.trim();
  const level = accountValue.level.trim();
  const type = accountValue.type.trim();
  const balance = accountValue.normalBalance.trim();
  const status = accountValue.status.trim();
  const posting = accountValue.isPostingAccount ? "Posting" : "Structural";
  const missingFields = [
    code === "" ? "code" : null,
    name === "" ? "name" : null,
    level === "" ? "level" : null,
    type === "" ? "type" : null,
    balance === "" ? "balance" : null,
    status === "" ? "status" : null
  ].filter((field): field is string => field !== null);

  return [
    {
      label: "Parent",
      value: parentAccount?.displayCode ?? (hasParent ? parentCode || "Selected" : "Top level"),
      tone: hasParent ? "ready" : "neutral",
      title: `${parentAccount?.name ?? (parentAccountId || parentCode || "Top-level account")} / ${depthLabel}`
    },
    {
      label: "Range",
      value: selectedRange?.role ?? "No range",
      tone: selectedRange === null ? "warning" : "ready",
      title: selectedRange === null ? "Select an account range" : formatRangeRule(selectedRange)
    },
    {
      label: "Code",
      value: code === "" ? "Suggest" : code,
      tone: code === "" ? "warning" : "ready",
      title: code === "" ? "Code is required" : "Ledger account code"
    },
    {
      label: "Class",
      value: `${level || "Level"} / ${type || "Type"}`,
      tone: level !== "" && type !== "" ? "ready" : "warning"
    },
    {
      label: "Balance",
      value: `${balance || "Balance"} / ${posting}`,
      tone: balance !== "" ? "ready" : "warning"
    },
    {
      label: "Save",
      value: accountSaveErrors.length > 0
        ? `${accountSaveErrors.length} error${accountSaveErrors.length === 1 ? "" : "s"}`
        : canSaveAccount
          ? "Ready"
          : missingFields.length > 0
            ? `Needs ${missingFields[0]}`
            : "Review",
      tone: accountSaveErrors.length > 0 || !canSaveAccount ? "warning" : "ready"
    }
  ];
}

export function getInlineAccountNamePlaceholder(
  level: string,
  parentAccount: LedgerAccountSummary | null,
  selectedRange: AccountCodeRange | null
): string {
  const normalizedLevel = level.trim().toLowerCase();

  if (normalizedLevel === "subsidiary" && parentAccount !== null) {
    return `${parentAccount.name} - client name`;
  }

  if (normalizedLevel === "detail") {
    return `${selectedRange?.displayName ?? "Detail"} account name`;
  }

  if (normalizedLevel === "control") {
    return "Control account name";
  }

  if (normalizedLevel === "master") {
    return "Master account name";
  }

  return "Ledger account name";
}

export function getCommonPrefixLength(left: string, right: string): number {
  const limit = Math.min(left.length, right.length);
  let index = 0;

  while (index < limit && left[index] === right[index]) {
    index += 1;
  }

  return index;
}

export function rangeContainsAccount(range: AccountCodeRange, account: LedgerAccountSummary): boolean {
  return account.code.length === range.codeLength
    && account.code.startsWith(range.searchPrefix)
    && account.code >= range.rangeStart
    && account.code <= range.rangeEnd;
}

export function isParentInsideRangeFamily(range: AccountCodeRange, parentCode: string): boolean {
  const configuredParentCode = range.parentCode?.trim() ?? "";

  if (configuredParentCode === "") {
    return true;
  }

  if (parentCode === configuredParentCode) {
    return true;
  }

  return isNumericCode(parentCode)
    && parentCode.startsWith(configuredParentCode)
    && parentCode.length === range.codeLength
    && parentCode.startsWith(range.searchPrefix)
    && parentCode >= range.rangeStart
    && parentCode <= range.rangeEnd;
}

export function getAccountTreeLineageIds(
  accountId: string,
  accounts: LedgerAccountSummary[],
  ranges: AccountCodeRange[],
  isParentTreeView: boolean
): string[] {
  const sortedAccounts = [...accounts].sort(compareLedgerAccounts);
  const accountByCode = new Map(sortedAccounts.map((account) => [account.code, account]));
  const parentById = new Map<string, string | null>();

  sortedAccounts.forEach((account) => {
    parentById.set(
      account.ledgerAccountId,
      getParentAccountId(account, sortedAccounts, accountByCode, ranges, isParentTreeView)
    );
  });

  const lineageIds: string[] = [];
  let nextAccountId: string | null = accountId;
  const visitedIds = new Set<string>();
  let guard = 0;

  while (nextAccountId !== null && !visitedIds.has(nextAccountId) && guard < 64) {
    visitedIds.add(nextAccountId);
    lineageIds.push(nextAccountId);
    nextAccountId = parentById.get(nextAccountId) ?? null;
    guard += 1;
  }

  return lineageIds;
}

export function buildAccountTreeRows(
  accounts: LedgerAccountSummary[],
  matchedAccounts: LedgerAccountSummary[],
  ranges: AccountCodeRange[],
  collapsedAccountIds: Set<string>,
  includeMatchedDescendants = false
): AccountTreeRow[] {
  const sortedAccounts = [...accounts].sort(compareLedgerAccounts);
  const matchedIds = new Set(matchedAccounts.map((account) => account.ledgerAccountId));
  const displayIds = new Set(matchedIds);
  const emphasizedIds = new Set(matchedIds);
  const accountById = new Map(sortedAccounts.map((account) => [account.ledgerAccountId, account]));
  const accountByCode = new Map(sortedAccounts.map((account) => [account.code, account]));
  const parentById = new Map<string, string | null>();
  const childrenById = new Map<string, LedgerAccountSummary[]>();

  sortedAccounts.forEach((account) => {
    parentById.set(
      account.ledgerAccountId,
      getParentAccountId(account, sortedAccounts, accountByCode, ranges, includeMatchedDescendants)
    );
  });

  sortedAccounts.forEach((account) => {
    const parentId = parentById.get(account.ledgerAccountId) ?? null;

    if (parentId === null) {
      return;
    }

    const children = childrenById.get(parentId) ?? [];
    children.push(account);
    childrenById.set(parentId, children);
  });

  if (includeMatchedDescendants) {
    matchedAccounts.forEach((account) => {
      includeDescendantAccountIds(account.ledgerAccountId, childrenById, displayIds, emphasizedIds);
    });
  }

  matchedAccounts.forEach((account) => {
    let nextParentId = parentById.get(account.ledgerAccountId) ?? null;
    let guard = 0;

    while (nextParentId !== null && guard < 16) {
      displayIds.add(nextParentId);
      nextParentId = parentById.get(nextParentId) ?? null;
      guard += 1;
    }
  });

  const roots = sortedAccounts.filter((account) => {
    if (!displayIds.has(account.ledgerAccountId)) {
      return false;
    }

    const parentId = parentById.get(account.ledgerAccountId) ?? null;

    return parentId === null || !displayIds.has(parentId) || !accountById.has(parentId);
  });
  const rows: AccountTreeRow[] = [];

  roots.forEach((account) => {
    appendAccountTreeRows({
      account,
      childrenById,
      collapsedAccountIds,
      depth: 0,
      displayIds,
      matchedIds: emphasizedIds,
      parentById,
      ranges,
      rows
    });
  });

  return rows;
}

export function includeDescendantAccountIds(
  accountId: string,
  childrenById: Map<string, LedgerAccountSummary[]>,
  displayIds: Set<string>,
  emphasizedIds: Set<string>,
  visited = new Set<string>()
) {
  if (visited.has(accountId)) {
    return;
  }

  visited.add(accountId);

  (childrenById.get(accountId) ?? []).forEach((child) => {
    displayIds.add(child.ledgerAccountId);
    emphasizedIds.add(child.ledgerAccountId);
    includeDescendantAccountIds(child.ledgerAccountId, childrenById, displayIds, emphasizedIds, visited);
  });
}

export function appendAccountTreeRows({
  account,
  childrenById,
  collapsedAccountIds,
  depth,
  displayIds,
  matchedIds,
  parentById,
  ranges,
  rows
}: {
  account: LedgerAccountSummary;
  childrenById: Map<string, LedgerAccountSummary[]>;
  collapsedAccountIds: Set<string>;
  depth: number;
  displayIds: Set<string>;
  matchedIds: Set<string>;
  parentById: Map<string, string | null>;
  ranges: AccountCodeRange[];
  rows: AccountTreeRow[];
}) {
  const children = (childrenById.get(account.ledgerAccountId) ?? [])
    .filter((child) => displayIds.has(child.ledgerAccountId))
    .sort(compareLedgerAccounts);
  const level = getLegacyAccountLevel(account, ranges);

  rows.push({
    account,
    childCount: children.length,
    depth,
    hasChildren: children.length > 0,
    isMatched: matchedIds.has(account.ledgerAccountId),
    level,
    parentAccountId: parentById.get(account.ledgerAccountId) ?? null
  });

  if (collapsedAccountIds.has(account.ledgerAccountId)) {
    return;
  }

  children.forEach((child) => {
    appendAccountTreeRows({
      account: child,
      childrenById,
      collapsedAccountIds,
      depth: depth + 1,
      displayIds,
      matchedIds,
      parentById,
      ranges,
      rows
    });
  });
}

export function getParentAccountId(
  account: LedgerAccountSummary,
  accounts: LedgerAccountSummary[],
  accountByCode: Map<string, LedgerAccountSummary>,
  ranges: AccountCodeRange[],
  isParentTreeView: boolean
): string | null {
  const level = getLegacyAccountLevel(account, ranges);

  if (isParentTreeView && level.code === "T") {
    return null;
  }

  if (account.parentAccountId !== null && account.parentAccountId !== undefined) {
    return account.parentAccountId;
  }

  const range = ranges.find((candidate) => candidate.role === account.rangeRole)
    ?? ranges.find((candidate) =>
      account.code.length === candidate.codeLength
      && account.code.startsWith(candidate.searchPrefix)
      && account.code >= candidate.rangeStart
      && account.code <= candidate.rangeEnd);
  const parentCode = range?.parentCode?.trim() ?? "";

  if (parentCode !== "") {
    return accountByCode.get(parentCode)?.ledgerAccountId ?? null;
  }

  const parentByPrefix = accounts
    .filter((candidate) =>
      candidate.ledgerAccountId !== account.ledgerAccountId
      && account.code.startsWith(candidate.code)
      && candidate.code.length < account.code.length)
    .sort((left, right) => right.code.length - left.code.length)[0];

  if (parentByPrefix !== undefined) {
    return parentByPrefix.ledgerAccountId;
  }

  if (isParentTreeView && level.code !== "H") {
    const totalParent = accounts
      .filter((candidate) => {
        const candidateLevel = getLegacyAccountLevel(candidate, ranges);

        return candidateLevel.code === "T"
          && candidate.ledgerAccountId !== account.ledgerAccountId
          && candidate.type === account.type
          && candidate.normalBalance === account.normalBalance;
      })
      .sort(compareLedgerAccounts)[0];

    return totalParent?.ledgerAccountId ?? null;
  }

  return null;
}

export function compareLedgerAccounts(left: LedgerAccountSummary, right: LedgerAccountSummary): number {
  const codeOrder = left.code.localeCompare(right.code);

  return codeOrder === 0 ? left.name.localeCompare(right.name) : codeOrder;
}

export function getCoaTreeRowContextItems(
  account: LedgerAccountSummary,
  level: LegacyAccountLevel,
  options: CoaTreeRowContextOptions
): CoaTreeRowContextItem[] {
  const range = account.rangeDisplayName?.trim() || account.rangeRole?.trim() || "No range";
  const normalBalance = account.normalBalance.trim();
  const status = account.status.trim();
  const contextItems: CoaTreeRowContextItem[] = [
    {
      label: account.type,
      tone: "neutral",
      title: "Account class"
    },
    {
      label: formatNormalBalance(normalBalance),
      tone: getNormalBalanceTone(normalBalance),
      title: "Normal balance"
    },
    {
      label: account.isPostingAccount ? "Posting" : "Structural",
      tone: account.isPostingAccount ? "ready" : "neutral",
      title: `${level.label} account`
    }
  ];

  if (options.parentAccount !== null) {
    contextItems.push({
      label: `Parent ${options.parentAccount.displayCode}`,
      tone: "neutral",
      title: `Direct parent: ${options.parentAccount.displayCode} - ${options.parentAccount.name}`
    });
  } else if (options.depth > 0) {
    contextItems.push({
      label: `Depth ${options.depth + 1}`,
      tone: "warning",
      title: "Nested account parent is inferred by code"
    });
  }

  if (options.childCreateContext !== null) {
    contextItems.push({
      label: "Add child",
      tone: "ready",
      title: `Child creation available in ${options.childCreateContext.rangeRole}`
    });
  }

  contextItems.push(
    {
      label: range,
      tone: account.rangeRole === null || account.rangeRole === undefined ? "warning" : "neutral",
      title: "Account range"
    },
    {
      label: status === "" ? "Status n/a" : status,
      tone: status.toLowerCase() === "active" ? "ready" : "warning",
      title: "Account status"
    }
  );

  return contextItems;
}

export function getNormalBalanceTone(normalBalance: string): "debit" | "credit" | "neutral" {
  const normalized = normalBalance.trim().toLowerCase();

  if (normalized === "debit") {
    return "debit";
  }

  if (normalized === "credit") {
    return "credit";
  }

  return "neutral";
}

export function formatAccountTreeHeading(
  shownCount: number,
  seedCount: number,
  accountCount: number,
  postingCount: number,
  isParentTreeView: boolean
): string {
  if (isParentTreeView) {
    return `${shownCount} shown from ${seedCount} total rows / ${postingCount} posting / nested`;
  }

  return `${seedCount} of ${accountCount} accounts / ${postingCount} posting / nested`;
}

export function getRangeSetupFacts(
  ranges: AccountCodeRange[],
  rangeValidation: AccountCodeRangeValidation | null,
  selectedRange: AccountCodeRange | null,
  selectedRangeIssues: AccountCodeRangeValidationIssue[],
  selectedRangeUsage: number
): CoaRangeFact[] {
  const activeRangeCount = ranges.filter((range) => range.isActive).length;
  const validationTone = getRangeValidationTone(rangeValidation);

  return [
    {
      label: "Setup state",
      value: rangeValidation === null
        ? "Not checked"
        : rangeValidation.issueCount === 0
          ? "Clean"
          : "Needs review",
      tone: validationTone
    },
    {
      label: "Active ranges",
      value: `${activeRangeCount}/${ranges.length}`,
      tone: activeRangeCount === ranges.length && ranges.length > 0 ? "ready" : "warning"
    },
    {
      label: "Errors",
      value: String(rangeValidation?.errorCount ?? 0),
      tone: (rangeValidation?.errorCount ?? 0) === 0 ? "ready" : "danger"
    },
    {
      label: "Warnings",
      value: String(rangeValidation?.warningCount ?? 0),
      tone: (rangeValidation?.warningCount ?? 0) === 0 ? "ready" : "warning"
    },
    {
      label: "Selected range",
      value: selectedRange?.role ?? "None",
      tone: selectedRange === null ? "warning" : selectedRange.isActive ? "ready" : "warning"
    },
    {
      label: "Selected usage",
      value: selectedRange === null ? "-" : formatRangeUsage(selectedRange, selectedRangeUsage),
      tone: selectedRange === null
        ? "neutral"
        : getRangeUsageTone(selectedRange, selectedRangeUsage, selectedRangeIssues)
    }
  ];
}

export function getSelectedRangeFacts(
  range: AccountCodeRange,
  rangeIssues: AccountCodeRangeValidationIssue[],
  usageCount: number
): CoaRangeFact[] {
  return [
    {
      label: "Role",
      value: range.role,
      tone: range.isActive ? "ready" : "warning"
    },
    {
      label: "Code rule",
      value: formatRange(range),
      tone: getRangeUsageTone(range, usageCount, rangeIssues),
      title: `${range.searchPrefix} prefix / ${range.codeLength} digits`
    },
    {
      label: "Capacity",
      value: formatRangeCapacity(range),
      tone: "neutral"
    },
    {
      label: "Used",
      value: formatRangeUsage(range, usageCount),
      tone: getRangeUsageTone(range, usageCount, rangeIssues)
    },
    {
      label: "Rule",
      value: `${formatRangeLevelRule(range)} / ${range.accountType} / ${range.normalBalance}`,
      tone: rangeIssues.length === 0 ? "ready" : getRangeIssueTone(rangeIssues)
    },
    {
      label: "Parent",
      value: (range.parentCode ?? "").trim() === "" ? "Root" : range.parentCode ?? "",
      tone: "neutral"
    }
  ];
}

export function getRangeEditorFacts(
  value: AccountCodeRangeFormInput,
  selectedRangeIssues: AccountCodeRangeValidationIssue[]
): CoaRangeFact[] {
  const rangeOrderOk = compareRangeCodes(value.rangeStart, value.rangeEnd) <= 0;
  const prefixOk =
    value.searchPrefix.trim() !== ""
    && value.rangeStart.startsWith(value.searchPrefix)
    && value.rangeEnd.startsWith(value.searchPrefix);
  const length = Number(value.codeLength);
  const lengthOk =
    Number.isInteger(length)
    && length > 0
    && value.rangeStart.length === length
    && value.rangeEnd.length === length;

  return [
    {
      label: "Code order",
      value: rangeOrderOk ? "Start before end" : "Check start/end",
      tone: rangeOrderOk ? "ready" : "danger"
    },
    {
      label: "Prefix fit",
      value: prefixOk ? value.searchPrefix : "Mismatch",
      tone: prefixOk ? "ready" : "warning"
    },
    {
      label: "Code length",
      value: lengthOk ? `${length} digits` : "Mismatch",
      tone: lengthOk ? "ready" : "warning"
    },
    {
      label: "Posting rule",
      value: value.isPostingAccount ? "Posting" : "Structural",
      tone: value.isPostingAccount ? "ready" : "neutral"
    },
    {
      label: "Parent rule",
      value: value.parentCode.trim() === "" ? "Root" : value.parentCode,
      tone: "neutral"
    },
    {
      label: "Selected issues",
      value: selectedRangeIssues.length === 0 ? "None" : String(selectedRangeIssues.length),
      tone: selectedRangeIssues.length === 0 ? "ready" : getRangeIssueTone(selectedRangeIssues)
    }
  ];
}

export function buildRangeUsageByRole(
  accounts: LedgerAccountSummary[],
  ranges: AccountCodeRange[]
): Map<string, number> {
  const usageByRole = new Map<string, number>();

  accounts.forEach((account) => {
    const range =
      ranges.find((candidate) => candidate.role === account.rangeRole)
      ?? ranges.find((candidate) => rangeContainsAccount(candidate, account));

    if (range === undefined) {
      return;
    }

    usageByRole.set(range.role, (usageByRole.get(range.role) ?? 0) + 1);
  });

  return usageByRole;
}

export function getRangeValidationIssueGroups(
  issues: AccountCodeRangeValidationIssue[]
): CoaRangeIssueGroup[] {
  const groups = new Map<string, CoaRangeIssueGroup>();

  issues.forEach((issue) => {
    const tone = getRangeIssueTone([issue]);
    const key = `${tone}:${issue.code}`;
    const current = groups.get(key);

    if (current) {
      current.count += 1;
      return;
    }

    groups.set(key, {
      code: issue.code,
      count: 1,
      tone: tone === "danger" ? "danger" : tone === "warning" ? "warning" : "neutral",
      title: formatRangeValidationIssueMeta(issue)
    });
  });

  return Array.from(groups.values())
    .sort((left, right) =>
      getRangeToneRank(right.tone) - getRangeToneRank(left.tone)
      || right.count - left.count
      || left.code.localeCompare(right.code)
    )
    .slice(0, 5);
}

export function getRangeState(
  range: AccountCodeRange,
  rangeIssues: AccountCodeRangeValidationIssue[]
): { label: string; tone: "ready" | "warning" | "danger" | "neutral" } {
  const issueTone = getRangeIssueTone(rangeIssues);

  if (issueTone === "danger") {
    return {
      label: "Error",
      tone: "danger"
    };
  }

  if (issueTone === "warning") {
    return {
      label: "Warning",
      tone: "warning"
    };
  }

  if (!range.isActive) {
    return {
      label: "Inactive",
      tone: "warning"
    };
  }

  return {
    label: range.isPostingAccount ? "Posting" : "Structural",
    tone: range.isPostingAccount ? "ready" : "neutral"
  };
}

export function getRangeValidationTone(
  rangeValidation: AccountCodeRangeValidation | null
): "ready" | "warning" | "danger" | "neutral" {
  if (rangeValidation === null) {
    return "neutral";
  }

  if (rangeValidation.errorCount > 0) {
    return "danger";
  }

  if (rangeValidation.warningCount > 0) {
    return "warning";
  }

  return "ready";
}

export function getRangeIssueTone(
  issues: AccountCodeRangeValidationIssue[]
): "ready" | "warning" | "danger" | "neutral" {
  const severity = getHighestIssueSeverity(issues);

  if (severity === "error") {
    return "danger";
  }

  if (severity === "warning") {
    return "warning";
  }

  return issues.length === 0 ? "ready" : "neutral";
}

export function getRangeUsageTone(
  range: AccountCodeRange,
  usageCount: number,
  rangeIssues: AccountCodeRangeValidationIssue[]
): "ready" | "warning" | "danger" | "neutral" {
  const issueTone = getRangeIssueTone(rangeIssues);

  if (issueTone === "danger" || issueTone === "warning") {
    return issueTone;
  }

  const capacity = getRangeCapacity(range);

  if (capacity === null) {
    return range.isActive ? "ready" : "warning";
  }

  if (usageCount >= capacity) {
    return "danger";
  }

  if (usageCount / capacity >= 0.9) {
    return "warning";
  }

  return range.isActive ? "ready" : "warning";
}

export function getRangeToneRank(tone: string): number {
  switch (tone) {
    case "danger":
      return 3;
    case "warning":
      return 2;
    case "ready":
      return 1;
    default:
      return 0;
  }
}

export function formatRangeRule(range: AccountCodeRange): string {
  return `${range.displayName} / ${formatRange(range)} / ${formatRangeLevelRule(range)}`;
}

export function formatRangeLevelRule(range: AccountCodeRange): string {
  const intent = `${range.role} ${range.displayName}`.toLowerCase();

  if (intent.includes("header")) {
    return "Header";
  }

  if (intent.includes("total")) {
    return "Total";
  }

  if (intent.includes("control")) {
    return "Control";
  }

  if (intent.includes("master")) {
    return "Master";
  }

  if ((range.parentCode ?? "").trim() !== "") {
    return "Subsidiary";
  }

  return range.isPostingAccount ? "Detail posting" : "Non-posting";
}

export function formatRange(range: AccountCodeRange): string {
  return `${range.rangeStart}-${range.rangeEnd}`;
}

export function formatRangeCapacity(range: AccountCodeRange): string {
  const capacity = getRangeCapacity(range);

  return capacity === null ? "Variable" : formatCount(capacity);
}

export function formatRangeUsage(range: AccountCodeRange, usageCount: number): string {
  const capacity = getRangeCapacity(range);

  return capacity === null ? `${formatCount(usageCount)} used` : `${formatCount(usageCount)}/${formatCount(capacity)}`;
}

export function getRangeCapacity(range: AccountCodeRange): number | null {
  return getRangeCapacityFromCodes(range.rangeStart, range.rangeEnd);
}

export function getRangeCapacityFromCodes(rangeStart: string, rangeEnd: string): number | null {
  const start = rangeStart.trim();
  const end = rangeEnd.trim();

  if (!/^\d+$/.test(start) || !/^\d+$/.test(end)) {
    return null;
  }

  const startValue = Number(start);
  const endValue = Number(end);

  if (!Number.isSafeInteger(startValue) || !Number.isSafeInteger(endValue) || endValue < startValue) {
    return null;
  }

  return endValue - startValue + 1;
}

export function compareRangeCodes(left: string, right: string): number {
  if (/^\d+$/.test(left) && /^\d+$/.test(right)) {
    const leftValue = Number(left);
    const rightValue = Number(right);

    if (Number.isSafeInteger(leftValue) && Number.isSafeInteger(rightValue)) {
      return leftValue - rightValue;
    }
  }

  return left.localeCompare(right);
}

export function formatCount(value: number): string {
  return value.toLocaleString("en-US");
}

export function formatAccountSaveError(error: ApiErrorItem): string {
  const target = formatAccountSaveErrorTarget(error.target);

  return target === "" ? error.message : `${target}: ${error.message}`;
}

export function formatAccountSaveErrorTarget(target?: string | null): string {
  const normalized = target?.trim().toLowerCase() ?? "";

  switch (normalized) {
    case "code":
      return "Code";
    case "name":
      return "Name";
    case "type":
      return "Type";
    case "normalbalance":
      return "Balance";
    case "level":
      return "Level";
    case "parentaccountid":
      return "Parent";
    case "ispostingaccount":
      return "Posting";
    case "status":
      return "Status";
    default:
      return "";
  }
}

export function buildRangeValidationIssueMap(
  issues: AccountCodeRangeValidationIssue[]
): Map<string, AccountCodeRangeValidationIssue[]> {
  const issuesByRole = new Map<string, AccountCodeRangeValidationIssue[]>();

  issues.forEach((issue) => {
    [issue.rangeRole, issue.relatedRangeRole]
      .filter((role): role is string => role !== null && role !== undefined && role.trim() !== "")
      .forEach((role) => {
        const current = issuesByRole.get(role) ?? [];
        current.push(issue);
        issuesByRole.set(role, current);
      });
  });

  return issuesByRole;
}

export function formatRangeValidationIssueMeta(issue: AccountCodeRangeValidationIssue): string {
  const rangeStart = issue.rangeStart?.trim() ?? "";
  const rangeEnd = issue.rangeEnd?.trim() ?? "";
  const range = rangeStart !== "" && rangeEnd !== "" ? `${rangeStart}-${rangeEnd}` : "";

  return [
    issue.severity,
    issue.rangeRole ?? "Setup",
    issue.relatedRangeRole === null || issue.relatedRangeRole === undefined
      ? ""
      : `Related ${issue.relatedRangeRole}`,
    range
  ]
    .filter((value) => value.trim() !== "")
    .join(" / ");
}

export function getHighestIssueSeverity(issues: Array<{ severity: string }>): string {
  const normalized = issues.map((issue) => issue.severity.toLowerCase());

  if (normalized.some((severity) => severity === "critical" || severity === "error")) {
    return "error";
  }

  if (normalized.some((severity) => severity === "warning")) {
    return "warning";
  }

  return "info";
}

export function formatImportAction(action: string): string {
  return action === "NoChange" ? "No change" : action;
}

export function getLedgerActivityStatusItems(activity: LedgerAccountActivity): LedgerActivityStatusItem[] {
  const movement = activity.periodDebit - activity.periodCredit;

  return [
    {
      label: "Window",
      value: formatActivityWindow(activity),
      tone: "neutral"
    },
    {
      label: "Normal",
      value: formatNormalBalance(activity.normalBalance),
      tone: "ready"
    },
    {
      label: "Movement",
      value: isZeroAmount(movement) ? "No movement" : movement > 0 ? "Debit" : "Credit",
      tone: isZeroAmount(movement) ? "neutral" : "ready"
    },
    {
      label: "Lines",
      value: activity.lines.length === 0 ? "No entries" : `${activity.lines.length} entries`,
      tone: activity.lines.length === 0 ? "warning" : "ready"
    },
    {
      label: "Currency",
      value: activity.currencyCode?.trim() || accountingCurrencyCode,
      tone: "neutral"
    }
  ];
}

export function formatLedgerActivitySubtitle(
  activity: LedgerAccountActivity,
  account: LedgerAccountSummary | undefined
): string {
  const range = account?.rangeDisplayName?.trim() || account?.rangeRole?.trim() || "No range";
  const level = account?.level?.trim() || "Ledger";
  const role = account === undefined
    ? "Account"
    : account.isPostingAccount ? "Posting" : "Control";

  return `${activity.type} / ${formatNormalBalance(activity.normalBalance)} / ${level} / ${role} / ${range}`;
}

export function formatNormalBalance(normalBalance: string): string {
  const normalized = normalBalance.trim().toLowerCase();

  if (normalized.includes("debit")) {
    return "Dr normal";
  }

  if (normalized.includes("credit")) {
    return "Cr normal";
  }

  return normalBalance.trim() === "" ? "Normal n/a" : `${normalBalance} normal`;
}

export function getAmountTone(
  amount: number,
  normalBalance: string
): "debit" | "credit" | "neutral" | "warning" {
  if (isZeroAmount(amount)) {
    return "neutral";
  }

  const normalized = normalBalance.trim().toLowerCase();

  if (amount > 0 && normalized.includes("debit")) {
    return "debit";
  }

  if (amount < 0 && normalized.includes("credit")) {
    return "credit";
  }

  if (amount > 0) {
    return normalized.includes("credit") ? "warning" : "debit";
  }

  return normalized.includes("debit") ? "warning" : "credit";
}

export function formatRunningBalanceTitle(
  line: LedgerAccountActivityLine,
  normalBalance: string
): string {
  const side = getAmountTone(line.runningBalance, normalBalance);

  if (side === "warning") {
    return `Running balance is opposite of ${formatNormalBalance(normalBalance)}.`;
  }

  if (side === "neutral") {
    return "Running balance is zero.";
  }

  return `Running balance follows ${formatNormalBalance(normalBalance)}.`;
}

export function getActivityLinePrimaryNarration(line: LedgerAccountActivityLine): string {
  return line.description?.trim() || line.memo?.trim() || "No narration";
}

export function getActivityLineSecondaryNarration(line: LedgerAccountActivityLine): string {
  const description = line.description?.trim() ?? "";
  const memo = line.memo?.trim() ?? "";

  return description !== "" && memo !== "" && description !== memo ? memo : "";
}

export function formatActivityWindow(activity: LedgerAccountActivity): string {
  if ((activity.fromDate ?? "").trim() === "" && (activity.toDate ?? "").trim() === "") {
    return "All dates";
  }

  if ((activity.fromDate ?? "").trim() === "") {
    return `Through ${activity.toDate}`;
  }

  if ((activity.toDate ?? "").trim() === "") {
    return `From ${activity.fromDate}`;
  }

  return `${activity.fromDate} to ${activity.toDate}`;
}

export function hasJournalEntry(
  journalEntryId: string,
  journalEntries: JournalEntrySummary[]
): boolean {
  return journalEntries.some((entry) => entry.journalEntryId === journalEntryId);
}

export function isZeroAmount(value: number): boolean {
  return Math.abs(value) < 0.005;
}
