import type {
  JournalEntryFilters,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  ManualJournalEntryLineInput,
  OpeningBalanceImportInput,
  OpeningBalanceImportLineInput,
  OpeningBalanceImportPreview,
  OpeningBalanceImportTemplateFormat
} from "../types/accountingTypes";
import {
  amount,
  formatMoney,
  getPostingPeriodState,
  roundMoney
} from "./journalModel";

export type VoucherInputMode = "single" | "multiple";

export type VoucherTypeOption = {
  code: string;
  label: string;
};

export type SingleVoucherValue = {
  cashOrBankAccountId: string;
  referenceNo: string;
  chequeNo: string;
  paidTo: string;
};

export type SingleVoucherSide = "debit" | "credit" | "manual";
export type SingleVoucherPostingSide = Exclude<SingleVoucherSide, "manual">;
export type VoucherStateTone = "ready" | "warning" | "neutral";

export type JournalRegisterStatusItem = {
  label: string;
  value: string;
  tone: "ready" | "warning" | "neutral";
};

export type JournalDetailStatusItem = {
  label: string;
  value: string;
  tone: "ready" | "warning" | "neutral";
};

export type OpeningBalanceScopeItem = {
  label: string;
  value: string;
  detail: string;
  tone: "ready" | "warning" | "neutral";
};

export type OpeningBalanceTemplateOption = {
  value: OpeningBalanceImportTemplateFormat;
  label: string;
  title: string;
};

export type OpeningBalanceProfilePostBlocker = {
  title: string;
};

export type JournalLineState = "ready" | "missing-account" | "missing-amount" | "double-sided";

export type VoucherPostingReadinessItem = {
  label: string;
  status: string;
  detail: string;
  tone: "ready" | "warning";
};

export type OpeningBalanceReadinessItem = VoucherPostingReadinessItem;

export type OpeningBalanceLineState =
  | "ready"
  | "missing-account"
  | "unmatched-account"
  | "missing-amount"
  | "double-sided";

export const voucherTypeOptions: VoucherTypeOption[] = [
  { code: "J", label: "Journal" },
  { code: "P", label: "Payment" },
  { code: "R", label: "Receipt" },
  { code: "A", label: "Purchase" },
  { code: "B", label: "Purchase Return" },
  { code: "C", label: "Sales" },
  { code: "D", label: "Sales Return" },
  { code: "E", label: "Transfer" },
  { code: "F", label: "Challan" },
  { code: "G", label: "Production" },
  { code: "H", label: "Order" },
  { code: "I", label: "Fuel" },
  { code: "K", label: "Purchase Order" },
  { code: "L", label: "Debit Note" },
  { code: "M", label: "Credit Note" }
];

export const openingBalanceAccountListId = "opening-balance-posting-account-list";

export const openingBalanceTemplateOptions: OpeningBalanceTemplateOption[] = [
  {
    value: "legacy-sql",
    label: "SQL year opening",
    title: "ACT_SD_COA_OPNBAL columns: DCO_COA3_CODE, DCO_DBT_AMT, DCO_CRD_AMT"
  },
  {
    value: "legacy-access",
    label: "Access OP_BAL",
    title: "OP_BAL columns: ACC_CODE, DAMT, CAMT"
  },
  {
    value: "standard",
    label: "Simple paste",
    title: "Standard columns: accountCode, debit, credit, description"
  }
];

export function getActivePostingAccounts(accounts: LedgerAccountSummary[]): LedgerAccountSummary[] {
  return accounts.filter((account) => account.status === "Active" && account.isPostingAccount);
}

export function createEmptyJournalLine(): ManualJournalEntryLineInput {
  return {
    ledgerAccountId: "",
    debit: "",
    credit: "",
    description: ""
  };
}

export function createEmptyOpeningBalanceLine(): OpeningBalanceImportLineInput {
  return {
    accountCode: "",
    debit: "",
    credit: "",
    description: ""
  };
}

export function normalizeSingleVoucherLines(
  lines: ManualJournalEntryLineInput[]
): ManualJournalEntryLineInput[] {
  const nextLines = lines.length === 0 ? [createEmptyJournalLine()] : [...lines];

  while (nextLines.length < 2) {
    nextLines.push(createEmptyJournalLine());
  }

  return nextLines;
}

export function getSingleVoucherCashSide(voucherTypeCode: string): SingleVoucherSide {
  if (voucherTypeCode === "R") {
    return "debit";
  }

  if (voucherTypeCode === "P" || voucherTypeCode === "E") {
    return "credit";
  }

  return "manual";
}

export function getOppositeSingleVoucherSide(side: SingleVoucherSide): SingleVoucherSide {
  if (side === "debit") {
    return "credit";
  }

  if (side === "credit") {
    return "debit";
  }

  return "manual";
}

export function toPostingSide(side: SingleVoucherSide): SingleVoucherPostingSide | null {
  return side === "manual" ? null : side;
}

export function formatSingleVoucherSide(side: SingleVoucherSide): string {
  if (side === "debit") {
    return "Debit";
  }

  if (side === "credit") {
    return "Credit";
  }

  return "Manual";
}

export function formatVoucherModeFact(
  mode: VoucherInputMode,
  cashSide: SingleVoucherSide,
  detailSide: SingleVoucherSide
): string {
  if (mode === "multiple" || cashSide === "manual" || detailSide === "manual") {
    return "Manual Dr / Cr";
  }

  return `Cash ${formatSingleVoucherSide(cashSide)} / Detail ${formatSingleVoucherSide(detailSide)}`;
}

export function formatSingleVoucherCashPosting(side: SingleVoucherSide, value: number): string {
  if (side === "manual") {
    return "Manual";
  }

  return `${formatSingleVoucherSide(side)} ${formatMoney(value)}`;
}

export function singleVoucherCashDescription(
  value: SingleVoucherValue,
  voucherType: VoucherTypeOption
): string {
  return [
    voucherType.label,
    value.referenceNo.trim() === "" ? "" : `Ref ${value.referenceNo.trim()}`,
    value.chequeNo.trim() === "" ? "" : `Chq ${value.chequeNo.trim()}`,
    value.paidTo.trim()
  ].filter((item) => item !== "").join(" / ");
}

export function amountToInput(value: number): string {
  return value <= 0 ? "" : roundMoney(value).toString();
}

export function getSingleVoucherGuideStatus({
  cashAmount,
  detailLineCount,
  detailTotal,
  hasCashAccount,
  incompleteDetailCount,
  isGuided
}: {
  cashAmount: number;
  detailLineCount: number;
  detailTotal: number;
  hasCashAccount: boolean;
  incompleteDetailCount: number;
  isGuided: boolean;
}): { label: string; tone: "ready" | "warning" | "neutral" } {
  if (!isGuided) {
    return { label: "Manual", tone: "neutral" };
  }

  if (!hasCashAccount) {
    return { label: "Cash A/C missing", tone: "warning" };
  }

  if (detailLineCount === 0 || incompleteDetailCount > 0) {
    return { label: "Detail missing", tone: "warning" };
  }

  if (detailTotal <= 0) {
    return { label: "Amount missing", tone: "warning" };
  }

  return roundMoney(cashAmount - detailTotal) === 0
    ? { label: "Ready", tone: "ready" }
    : { label: "Out of balance", tone: "warning" };
}

export function isCashOrBankAccount(account: LedgerAccountSummary): boolean {
  const accountText = [
    account.code,
    account.displayCode,
    account.name,
    account.rangeRole ?? "",
    account.rangeDisplayName ?? ""
  ].join(" ");

  return /\b(bank|cash|wallet|deposit|cheque|checking|savings|petty|mcb|hbl|ubl|meezan|alfalah|faysal)\b/i
    .test(accountText);
}

export function filterJournalAccounts(
  accounts: LedgerAccountSummary[],
  lookupText: string
): LedgerAccountSummary[] {
  const lookupTokens = lookupText.trim().toLowerCase().split(/\s+/).filter((token) => token !== "");

  if (lookupTokens.length === 0) {
    return accounts;
  }

  return accounts.filter((account) => {
    const searchText = journalAccountSearchText(account);

    return lookupTokens.every((token) => searchText.includes(token));
  });
}

export function withSelectedJournalAccount(
  accounts: LedgerAccountSummary[],
  selectedAccount: LedgerAccountSummary | null
): LedgerAccountSummary[] {
  if (
    selectedAccount === null
    || accounts.some((account) => account.ledgerAccountId === selectedAccount.ledgerAccountId)
  ) {
    return accounts;
  }

  return [selectedAccount, ...accounts];
}

export function journalAccountSearchText(account: LedgerAccountSummary): string {
  return [
    account.code,
    account.displayCode,
    account.name,
    account.type,
    account.normalBalance,
    formatJournalAccountLevel(account.level),
    account.rangeRole ?? "",
    account.rangeDisplayName ?? ""
  ].join(" ").toLowerCase();
}

export function formatJournalAccountOption(account: LedgerAccountSummary): string {
  return `${account.displayCode} - ${account.name}`;
}

export function formatJournalAccountContext(account: LedgerAccountSummary): string {
  return [
    formatJournalAccountLevel(account.level),
    account.type,
    `${account.normalBalance} normal`,
    account.rangeDisplayName ?? account.rangeRole ?? ""
  ].filter((item) => item !== "").join(" / ");
}

export function formatOpeningBalanceMatchedAccountLabel(account: LedgerAccountSummary): string {
  return `${account.displayCode} - ${account.name}`;
}

export function formatOpeningBalanceAccountOption(account: LedgerAccountSummary): string {
  return `${formatOpeningBalanceMatchedAccountLabel(account)} | ${formatOpeningBalanceAccountContext(account)}`;
}

export function formatOpeningBalanceAccountContext(account: LedgerAccountSummary): string {
  return [
    "Posting account",
    formatJournalAccountContext(account)
  ].filter((item) => item.trim() !== "").join(" / ");
}

export function formatJournalAccountLevel(level?: string | null): string {
  const normalizedLevel = level?.trim() ?? "";

  switch (normalizedLevel.toUpperCase()) {
    case "H":
      return "Header";
    case "T":
      return "Total";
    case "M":
      return "Master";
    case "D":
      return "Detail";
    case "C":
      return "Control";
    case "S":
      return "Subsidiary";
    default:
      return normalizedLevel;
  }
}

export function getVoucherPostingReadiness({
  currencyCode,
  difference,
  entryDate,
  hasAccounts,
  hasCredit,
  hasDebit,
  incompleteLineCount,
  lineCount,
  postingPeriodState
}: {
  currencyCode: string;
  difference: number;
  entryDate: string;
  hasAccounts: boolean;
  hasCredit: boolean;
  hasDebit: boolean;
  incompleteLineCount: number;
  lineCount: number;
  postingPeriodState: ReturnType<typeof getPostingPeriodState>;
}): VoucherPostingReadinessItem[] {
  const hasDate = entryDate.trim() !== "";
  const hasCurrency = currencyCode.trim() !== "";
  const voucherReady = hasDate && hasCurrency;
  const linesReady = lineCount >= 2 && hasAccounts && incompleteLineCount === 0;
  const sidesReady = hasDebit && hasCredit && difference === 0;

  return [
    {
      label: "Period",
      status: postingPeriodState.blocksPosting ? "Blocked" : postingPeriodState.status,
      detail: postingPeriodState.detail,
      tone: postingPeriodState.blocksPosting ? "warning" : "ready"
    },
    {
      label: "Voucher",
      status: voucherReady ? "Dated" : "Missing",
      detail: getVoucherMasterReadinessDetail(hasDate, hasCurrency),
      tone: voucherReady ? "ready" : "warning"
    },
    {
      label: "Lines",
      status: getVoucherLineReadinessStatus(lineCount, hasAccounts, incompleteLineCount),
      detail: getVoucherLineReadinessDetail(lineCount, hasAccounts, incompleteLineCount),
      tone: linesReady ? "ready" : "warning"
    },
    {
      label: "Dr/Cr",
      status: sidesReady ? "Balanced" : getVoucherSideReadinessStatus(hasDebit, hasCredit, difference),
      detail: getVoucherSideReadinessDetail(hasDebit, hasCredit, difference),
      tone: sidesReady ? "ready" : "warning"
    }
  ];
}

export function getVoucherMasterReadinessDetail(hasDate: boolean, hasCurrency: boolean): string {
  if (!hasDate && !hasCurrency) {
    return "Enter voucher date and currency.";
  }

  if (!hasDate) {
    return "Enter voucher date.";
  }

  if (!hasCurrency) {
    return "Enter currency.";
  }

  return "Voucher master is ready.";
}

export function getVoucherLineReadinessStatus(
  lineCount: number,
  hasAccounts: boolean,
  incompleteLineCount: number
): string {
  if (lineCount < 2) {
    return "Need 2";
  }

  if (!hasAccounts) {
    return "A/C missing";
  }

  if (incompleteLineCount > 0) {
    return `${incompleteLineCount} issue${incompleteLineCount === 1 ? "" : "s"}`;
  }

  return "Ready";
}

export function getVoucherLineReadinessDetail(
  lineCount: number,
  hasAccounts: boolean,
  incompleteLineCount: number
): string {
  if (lineCount < 2) {
    return "A voucher needs at least two lines.";
  }

  if (!hasAccounts) {
    return "Select an account on every voucher line.";
  }

  if (incompleteLineCount > 0) {
    return `${incompleteLineCount} voucher line${incompleteLineCount === 1 ? "" : "s"} need attention.`;
  }

  return "All voucher lines are ready.";
}

export function getVoucherSideReadinessStatus(
  hasDebit: boolean,
  hasCredit: boolean,
  difference: number
): string {
  if (!hasDebit || !hasCredit) {
    return "Need side";
  }

  return `Out ${formatMoney(Math.abs(difference))}`;
}

export function getVoucherSideReadinessDetail(
  hasDebit: boolean,
  hasCredit: boolean,
  difference: number
): string {
  if (!hasDebit && !hasCredit) {
    return "Enter debit and credit amounts.";
  }

  if (!hasDebit) {
    return "Enter at least one debit amount.";
  }

  if (!hasCredit) {
    return "Enter at least one credit amount.";
  }

  if (difference !== 0) {
    return `Debit and credit are out by ${formatMoney(Math.abs(difference))}.`;
  }

  return "Debit and credit are balanced.";
}

export function getVoucherPostButtonTitle(
  canPost: boolean,
  postingReadinessItems: VoucherPostingReadinessItem[]
): string {
  if (canPost) {
    return "Post balanced manual voucher";
  }

  const firstBlocker = postingReadinessItems.find((item) => item.tone === "warning");

  return firstBlocker === undefined
    ? "Complete voucher before posting"
    : `${firstBlocker.label}: ${firstBlocker.detail}`;
}

export function getOpeningBalanceReadiness({
  accounts,
  currencyCode,
  difference,
  entryDate,
  lines,
  preview,
  totalCredit,
  totalDebit
}: {
  accounts: LedgerAccountSummary[];
  currencyCode: string;
  difference: number;
  entryDate: string;
  lines: OpeningBalanceImportLineInput[];
  preview: OpeningBalanceImportPreview | null;
  totalCredit: number;
  totalDebit: number;
}): OpeningBalanceReadinessItem[] {
  const hasDate = entryDate.trim() !== "";
  const hasCurrency = currencyCode.trim() !== "";
  const enteredLines = lines.filter(hasOpeningBalanceLineValue);
  const accountCodes = enteredLines.filter((line) => line.accountCode.trim() !== "");
  const matchedAccountCount = accountCodes.filter((line) =>
    findOpeningBalanceAccount(line.accountCode, accounts) !== null).length;
  const amountIssueCount = enteredLines.filter(hasOpeningBalanceLineAmountIssue).length;
  const accountsReady = accountCodes.length > 0 && matchedAccountCount === accountCodes.length;
  const amountReady = totalDebit > 0 && totalCredit > 0 && difference === 0 && amountIssueCount === 0;
  const previewReady = preview?.canPost === true;

  return [
    {
      label: "Master",
      status: hasDate && hasCurrency ? "Ready" : "Missing",
      detail: getVoucherMasterReadinessDetail(hasDate, hasCurrency),
      tone: hasDate && hasCurrency ? "ready" : "warning"
    },
    {
      label: "Accounts",
      status: getOpeningBalanceAccountStatus(accountCodes.length, matchedAccountCount),
      detail: getOpeningBalanceAccountDetail(accountCodes.length, matchedAccountCount),
      tone: accountsReady ? "ready" : "warning"
    },
    {
      label: "Dr/Cr",
      status: amountReady ? "Balanced" : getOpeningBalanceAmountStatus(totalDebit, totalCredit, difference, amountIssueCount),
      detail: getOpeningBalanceAmountDetail(totalDebit, totalCredit, difference, amountIssueCount),
      tone: amountReady ? "ready" : "warning"
    },
    {
      label: "Preview",
      status: preview === null ? "Not run" : previewReady ? "Ready" : "Blocked",
      detail: getOpeningBalancePreviewDetail(preview),
      tone: previewReady ? "ready" : "warning"
    }
  ];
}

export function hasOpeningBalanceLineValue(line: OpeningBalanceImportLineInput): boolean {
  return line.accountCode.trim() !== ""
    || line.debit.trim() !== ""
    || line.credit.trim() !== ""
    || line.description.trim() !== "";
}

export function hasOpeningBalanceLineAmountIssue(line: OpeningBalanceImportLineInput): boolean {
  const debit = amount(line.debit);
  const credit = amount(line.credit);

  return (debit <= 0 && credit <= 0) || (debit > 0 && credit > 0);
}

export function getOpeningBalanceLineState(
  line: OpeningBalanceImportLineInput,
  account: LedgerAccountSummary | null
): OpeningBalanceLineState {
  const debit = amount(line.debit);
  const credit = amount(line.credit);

  if (line.accountCode.trim() === "") {
    return "missing-account";
  }

  if (account === null) {
    return "unmatched-account";
  }

  if (debit <= 0 && credit <= 0) {
    return "missing-amount";
  }

  if (debit > 0 && credit > 0) {
    return "double-sided";
  }

  return "ready";
}

export function openingBalanceLineStateLabel(state: OpeningBalanceLineState): string {
  switch (state) {
    case "ready":
      return "Ready";
    case "missing-account":
      return "No A/C";
    case "unmatched-account":
      return "No Match";
    case "missing-amount":
      return "No Amt";
    case "double-sided":
      return "Both";
    default:
      return "Check";
  }
}

export function findOpeningBalanceAccount(
  accountCode: string,
  accounts: LedgerAccountSummary[]
): LedgerAccountSummary | null {
  const normalizedAccountCode = normalizeAccountCode(accountCode);

  if (normalizedAccountCode === "") {
    return null;
  }

  return accounts.find((account) =>
    normalizeAccountCode(account.code) === normalizedAccountCode
    || normalizeAccountCode(account.displayCode) === normalizedAccountCode) ?? null;
}

export function normalizeAccountCode(accountCode: string): string {
  return accountCode.trim().toUpperCase();
}

export function getOpeningBalanceAccountStatus(accountCodeCount: number, matchedAccountCount: number): string {
  if (accountCodeCount === 0) {
    return "No A/C";
  }

  if (accountCodeCount === matchedAccountCount) {
    return `${matchedAccountCount} matched`;
  }

  return `${matchedAccountCount}/${accountCodeCount}`;
}

export function getOpeningBalanceAccountDetail(accountCodeCount: number, matchedAccountCount: number): string {
  if (accountCodeCount === 0) {
    return "Enter account codes for opening balance lines.";
  }

  if (accountCodeCount === matchedAccountCount) {
    return "Entered account codes match active posting accounts.";
  }

  return `${accountCodeCount - matchedAccountCount} account code${accountCodeCount - matchedAccountCount === 1 ? "" : "s"} did not match locally. Preview will confirm.`;
}

export function getOpeningBalanceAmountStatus(
  totalDebit: number,
  totalCredit: number,
  difference: number,
  amountIssueCount: number
): string {
  if (amountIssueCount > 0) {
    return `${amountIssueCount} issue${amountIssueCount === 1 ? "" : "s"}`;
  }

  if (totalDebit <= 0 || totalCredit <= 0) {
    return "Need Dr/Cr";
  }

  return `Out ${formatMoney(Math.abs(difference))}`;
}

export function getOpeningBalanceAmountDetail(
  totalDebit: number,
  totalCredit: number,
  difference: number,
  amountIssueCount: number
): string {
  if (amountIssueCount > 0) {
    return `${amountIssueCount} opening balance line${amountIssueCount === 1 ? "" : "s"} need either debit or credit, not both.`;
  }

  if (totalDebit <= 0 && totalCredit <= 0) {
    return "Enter debit and credit opening balances.";
  }

  if (totalDebit <= 0) {
    return "Enter at least one debit opening balance.";
  }

  if (totalCredit <= 0) {
    return "Enter at least one credit opening balance.";
  }

  if (difference !== 0) {
    return `Opening balance is out by ${formatMoney(Math.abs(difference))}.`;
  }

  return "Opening balance debit and credit totals are balanced.";
}

export function getOpeningBalancePreviewDetail(preview: OpeningBalanceImportPreview | null): string {
  if (preview === null) {
    return "Run preview before posting opening balances.";
  }

  if (preview.canPost) {
    return "Preview passed and can be posted.";
  }

  if (preview.blockers.length > 0) {
    return preview.blockers[0];
  }

  return `${preview.invalidLineCount} invalid line${preview.invalidLineCount === 1 ? "" : "s"} in preview.`;
}

export function getOpeningBalancePreviewLineSide(
  line: OpeningBalanceImportPreview["lines"][number]
): "Debit" | "Credit" | "Both" | "Zero" {
  if (line.debit > 0 && line.credit > 0) {
    return "Both";
  }

  if (line.debit > 0) {
    return "Debit";
  }

  if (line.credit > 0) {
    return "Credit";
  }

  return "Zero";
}

export function formatOpeningBalancePreviewAccountMeta(
  line: OpeningBalanceImportPreview["lines"][number]
): string {
  return [
    line.ledgerAccountName ?? "",
    line.ledgerAccountId === null || line.ledgerAccountId === undefined
      ? ""
      : line.isValid ? "Posting account" : "Not posting-ready",
    line.accountType ?? "",
    line.normalBalance === null || line.normalBalance === undefined
      ? ""
      : formatNormalBalanceLabel(line.normalBalance)
  ].filter((item) => item.trim() !== "").join(" / ") || "Account not matched";
}

export function getOpeningBalanceTemplateTitle(format: OpeningBalanceImportTemplateFormat): string {
  return openingBalanceTemplateOptions.find((option) => option.value === format)?.title
    ?? "Load opening balance import template";
}

export function findLedgerAccount(
  ledgerAccountId: string,
  accounts: LedgerAccountSummary[]
): LedgerAccountSummary | null {
  return accounts.find((account) => account.ledgerAccountId === ledgerAccountId) ?? null;
}

export function getJournalRegisterStatusItems(
  entries: JournalEntrySummary[],
  filters: JournalEntryFilters,
  getSourceDocumentLabel: (entry: JournalEntrySummary) => string | null
): JournalRegisterStatusItem[] {
  const postedCount = entries.filter((entry) => entry.status.trim().toLowerCase() === "posted").length;
  const outOfBalanceCount = entries.filter((entry) => getJournalEntryBalanceDifference(entry) !== 0).length;
  const sourceDocumentCount = entries.filter((entry) => getSourceDocumentLabel(entry) !== null).length;
  const totalDebit = roundMoney(entries.reduce((total, entry) => total + entry.totalDebit, 0));

  return [
    {
      label: "Window",
      value: formatJournalRegisterWindow(filters),
      tone: "neutral"
    },
    {
      label: "Rows",
      value: entries.length === 0 ? "No entries" : `${entries.length} entries`,
      tone: entries.length === 0 ? "warning" : "ready"
    },
    {
      label: "Posted",
      value: `${postedCount}/${entries.length}`,
      tone: entries.length === 0 ? "warning" : postedCount === entries.length ? "ready" : "warning"
    },
    {
      label: "Dr/Cr",
      value: outOfBalanceCount === 0 ? "Balanced" : `${outOfBalanceCount} out`,
      tone: outOfBalanceCount === 0 ? "ready" : "warning"
    },
    {
      label: "Source",
      value: `${sourceDocumentCount}/${entries.length}`,
      tone: "neutral"
    },
    {
      label: "Debit",
      value: formatMoney(totalDebit),
      tone: "neutral"
    }
  ];
}

export function formatJournalRegisterWindow(filters: JournalEntryFilters): string {
  if (filters.fromDate.trim() === "" && filters.toDate.trim() === "") {
    return "All dates";
  }

  if (filters.fromDate.trim() === "") {
    return `Through ${filters.toDate}`;
  }

  if (filters.toDate.trim() === "") {
    return `From ${filters.fromDate}`;
  }

  return `${filters.fromDate} to ${filters.toDate}`;
}

export function getJournalEntryBalanceDifference(entry: JournalEntrySummary): number {
  return roundMoney(entry.totalDebit - entry.totalCredit);
}

export function formatJournalLineAccountCode(
  account: LedgerAccountSummary | null,
  ledgerAccountId: string
): string {
  return account === null ? ledgerAccountId : account.displayCode;
}

export function formatJournalLineAccountName(account: LedgerAccountSummary | null): string {
  return account === null ? "Account metadata not loaded" : account.name;
}

export function formatJournalLineClass(account: LedgerAccountSummary | null): string {
  return account === null
    ? "Unmatched"
    : `${account.type} / ${formatNormalBalanceLabel(account.normalBalance)}`;
}

export function formatJournalLineAccountMeta(account: LedgerAccountSummary | null): string {
  return account === null ? "Account metadata not loaded" : formatJournalAccountContext(account);
}

export function getJournalDetailStatusItems(
  entry: JournalEntrySummary,
  sourceDocument: JournalEntrySourceDocument | null
): JournalDetailStatusItem[] {
  const difference = roundMoney(entry.totalDebit - entry.totalCredit);

  return [
    {
      label: "Voucher",
      value: entry.sourceReference ?? entry.journalEntryId,
      tone: "neutral"
    },
    {
      label: "Status",
      value: entry.status,
      tone: getJournalEntryStatusTone(entry.status)
    },
    {
      label: "Dr/Cr",
      value: difference === 0 ? "Balanced" : `Out ${formatMoney(Math.abs(difference))}`,
      tone: difference === 0 ? "ready" : "warning"
    },
    {
      label: "Source",
      value: formatJournalDetailSourceState(sourceDocument),
      tone: sourceDocument === null || sourceDocument.isResolved ? "ready" : "warning"
    },
    {
      label: "Currency",
      value: entry.currencyCode,
      tone: "neutral"
    }
  ];
}

export function getJournalEntryStatusTone(status: string): "ready" | "warning" | "neutral" {
  const normalized = status.trim().toLowerCase();

  if (normalized === "posted" || normalized === "open") {
    return "ready";
  }

  if (normalized.includes("void") || normalized.includes("revers") || normalized.includes("cancel")) {
    return "warning";
  }

  return "neutral";
}

export function formatJournalDetailSourceState(sourceDocument: JournalEntrySourceDocument | null): string {
  if (sourceDocument === null) {
    return "Journal only";
  }

  if (!sourceDocument.isResolved) {
    return "Unresolved";
  }

  return sourceDocument.documentKind ?? sourceDocument.label ?? "Resolved";
}

export function formatNormalBalanceLabel(normalBalance: string): string {
  const normalized = normalBalance.trim().toLowerCase();

  if (normalized.includes("debit")) {
    return "Dr normal";
  }

  if (normalized.includes("credit")) {
    return "Cr normal";
  }

  return normalBalance.trim() === "" ? "Normal n/a" : `${normalBalance} normal`;
}

export function getJournalEntryLineSide(
  line: JournalEntrySummary["lines"][number]
): "Debit" | "Credit" | "Both" | "Zero" {
  if (line.debit > 0 && line.credit > 0) {
    return "Both";
  }

  if (line.debit > 0) {
    return "Debit";
  }

  if (line.credit > 0) {
    return "Credit";
  }

  return "Zero";
}

export function getJournalLineState(line: ManualJournalEntryLineInput): JournalLineState {
  const debit = amount(line.debit);
  const credit = amount(line.credit);

  if (line.ledgerAccountId.trim() === "") {
    return "missing-account";
  }

  if (debit <= 0 && credit <= 0) {
    return "missing-amount";
  }

  if (debit > 0 && credit > 0) {
    return "double-sided";
  }

  return "ready";
}

export function journalLineTitle(state: JournalLineState): string {
  if (state === "missing-account") {
    return "Select an account for this voucher line";
  }

  if (state === "missing-amount") {
    return "Enter either debit or credit";
  }

  if (state === "double-sided") {
    return "Use either debit or credit on one line";
  }

  return "Voucher line ready";
}

export function journalLineStatusLabel(state: JournalLineState): string {
  if (state === "missing-account") {
    return "No A/C";
  }

  if (state === "missing-amount") {
    return "No Amt";
  }

  if (state === "double-sided") {
    return "Both";
  }

  return "Ready";
}

export function getVoucherState({
  difference,
  hasAccounts,
  hasCredit,
  hasDebit,
  incompleteLineCount,
  postingBlocked
}: {
  difference: number;
  hasAccounts: boolean;
  hasCredit: boolean;
  hasDebit: boolean;
  incompleteLineCount: number;
  postingBlocked: boolean;
}): string {
  if (postingBlocked) {
    return "Period blocked";
  }

  if (!hasAccounts) {
    return "Account missing";
  }

  if (incompleteLineCount > 0) {
    return `${incompleteLineCount} line issue${incompleteLineCount === 1 ? "" : "s"}`;
  }

  if (!hasDebit || !hasCredit) {
    return "Needs Dr/Cr";
  }

  return difference === 0 ? "Balanced" : "Out of balance";
}

export function getVoucherStateTone({
  difference,
  hasAccounts,
  hasCredit,
  hasDebit,
  incompleteLineCount,
  postingBlocked
}: {
  difference: number;
  hasAccounts: boolean;
  hasCredit: boolean;
  hasDebit: boolean;
  incompleteLineCount: number;
  postingBlocked: boolean;
}): VoucherStateTone {
  if (
    !postingBlocked
    && hasAccounts
    && incompleteLineCount === 0
    && hasDebit
    && hasCredit
    && difference === 0
  ) {
    return "ready";
  }

  return "warning";
}

export function getOpeningBalanceProfileReadiness(
  value: OpeningBalanceImportInput,
  carryForwardAccount: LedgerAccountSummary | null
): OpeningBalanceReadinessItem {
  const fromDate = value.profileFromDate.trim();
  const toDate = value.profileToDate.trim();

  if (fromDate === "" || toDate === "") {
    return {
      label: "Profile",
      status: "Missing",
      detail: "Set the opening balance financial year from/to dates.",
      tone: "warning"
    };
  }

  if (fromDate > toDate) {
    return {
      label: "Profile",
      status: "Date issue",
      detail: "Financial year from date must be on or before the to date.",
      tone: "warning"
    };
  }

  if (value.profileStatus === "closed") {
    return {
      label: "Profile",
      status: "Closed",
      detail: "This opening profile is closed; reopen it before posting new opening balances.",
      tone: "warning"
    };
  }

  if (!value.transactionsAllowed) {
    return {
      label: "Profile",
      status: "Locked",
      detail: "Transactions are not allowed for this opening profile.",
      tone: "warning"
    };
  }

  if (carryForwardAccount === null) {
    return {
      label: "Profile",
      status: "Needs PL",
      detail: "Select the profit/loss carry-forward account before finalizing the opening profile.",
      tone: "warning"
    };
  }

  return {
    label: "Profile",
    status: "Open",
    detail: `Financial year ${fromDate} to ${toDate}; carry-forward ${carryForwardAccount.displayCode}.`,
    tone: "ready"
  };
}

export function getOpeningBalanceProfilePostBlocker(
  value: OpeningBalanceImportInput
): OpeningBalanceProfilePostBlocker | null {
  const fromDate = value.profileFromDate.trim();
  const toDate = value.profileToDate.trim();

  if (fromDate === "" || toDate === "") {
    return { title: "Set opening balance financial year from/to dates before posting." };
  }

  if (fromDate > toDate) {
    return { title: "Financial year from date must be on or before the to date." };
  }

  if (value.profileStatus === "closed") {
    return { title: "Opening profile is closed. Reopen it before posting." };
  }

  if (!value.transactionsAllowed) {
    return { title: "Transactions are not allowed for this opening profile." };
  }

  return null;
}

export function getOpeningBalanceProfitLossCarryForwardAccounts(
  accounts: LedgerAccountSummary[]
): LedgerAccountSummary[] {
  const preferredAccounts = accounts.filter(isOpeningBalanceCarryForwardAccount);

  return preferredAccounts.length > 0 ? preferredAccounts : accounts;
}

export function isOpeningBalanceCarryForwardAccount(account: LedgerAccountSummary): boolean {
  const accountText = [
    account.code,
    account.displayCode,
    account.name,
    account.type,
    account.rangeRole ?? "",
    account.rangeDisplayName ?? ""
  ].join(" ");

  return account.type.trim().toLowerCase() === "equity"
    || /\b(retained|earning|profit|loss|income summary|capital|equity|owner)\b/i.test(accountText);
}

export function getOpeningBalanceAccountScope(accounts: LedgerAccountSummary[]): OpeningBalanceScopeItem[] {
  const rangeLabels = getOpeningBalanceRangeLabels(accounts);

  return [
    {
      label: "Posting A/C",
      value: accounts.length === 0 ? "None" : `${accounts.length} active`,
      detail: "Opening balances can be posted only to active posting accounts.",
      tone: accounts.length === 0 ? "warning" : "ready"
    },
    {
      label: "Allowed Levels",
      value: formatOpeningBalanceScopeLevels(accounts),
      detail: "Header, Total, Master, and Control accounts organize the COA; opening amounts belong on posting children.",
      tone: accounts.length === 0 ? "warning" : "ready"
    },
    {
      label: "COA Ranges",
      value: rangeLabels.length === 0 ? "Unassigned" : `${rangeLabels.length} range${rangeLabels.length === 1 ? "" : "s"}`,
      detail: rangeLabels.length === 0
        ? "Posting accounts are loaded, but no range labels are assigned yet."
        : rangeLabels.join(", "),
      tone: rangeLabels.length === 0 ? "neutral" : "ready"
    }
  ];
}

export function formatOpeningBalanceScopeLevels(accounts: LedgerAccountSummary[]): string {
  const levelCounts = new Map<string, number>();

  for (const account of accounts) {
    const levelLabel = formatJournalAccountLevel(account.level) || "Posting";

    levelCounts.set(levelLabel, (levelCounts.get(levelLabel) ?? 0) + 1);
  }

  if (levelCounts.size === 0) {
    return "No posting level";
  }

  return Array.from(levelCounts.entries())
    .sort(([left], [right]) => getOpeningBalanceLevelSort(left) - getOpeningBalanceLevelSort(right))
    .map(([level, count]) => `${level} ${count}`)
    .join(" / ");
}

export function getOpeningBalanceLevelSort(level: string): number {
  switch (level) {
    case "Detail":
      return 1;
    case "Subsidiary":
      return 2;
    case "Control":
      return 3;
    case "Master":
      return 4;
    case "Total":
      return 5;
    case "Header":
      return 6;
    default:
      return 9;
  }
}

export function getOpeningBalanceRangeLabels(accounts: LedgerAccountSummary[]): string[] {
  return Array.from(new Set(accounts.map((account) =>
    account.rangeDisplayName?.trim()
    || account.rangeRole?.trim()
    || ""
  ).filter((label) => label !== ""))).sort((left, right) => left.localeCompare(right));
}

export function formatOpeningBalanceSourceFiscalYear(
  sourceDocument: JournalEntrySourceDocument
): string {
  const fromDate = sourceDocument.fiscalYearFrom?.trim() ?? "";
  const toDate = sourceDocument.fiscalYearTo?.trim() ?? "";

  if (fromDate === "" && toDate === "") {
    return "-";
  }

  if (fromDate === "" || toDate === "") {
    return `${fromDate || "?"} to ${toDate || "?"}`;
  }

  return `${fromDate} to ${toDate}`;
}

export function formatOpeningBalanceSourceCarryForwardAccount(
  sourceDocument: JournalEntrySourceDocument
): string {
  const accountCode = sourceDocument.profitAndLossCarryForwardAccountCode?.trim() ?? "";
  const accountName = sourceDocument.profitAndLossCarryForwardAccountName?.trim() ?? "";

  if (accountCode === "" && accountName === "") {
    return "-";
  }

  return accountName === "" ? accountCode : `${accountCode} ${accountName}`;
}

export function sourceDocumentClientLabel(
  sourceDocument: JournalEntrySourceDocument | undefined,
  getSourceDocumentClientLabel: (sourceDocument: JournalEntrySourceDocument) => string
): string {
  return sourceDocument === undefined ? "-" : getSourceDocumentClientLabel(sourceDocument);
}
