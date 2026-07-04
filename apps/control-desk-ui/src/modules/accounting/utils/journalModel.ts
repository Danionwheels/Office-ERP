import type {
  AccountingPeriod,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  LedgerAccountSummary
} from "../types/accountingTypes";
import { toDateInputValue } from "./accountingDates";

export type PostingPeriodState = {
  tone: "open" | "closed" | "draft" | "voided";
  label: string;
  status: string;
  detail: string;
  blocksPosting: boolean;
};

export function amount(value: string): number {
  const parsed = Number(value);

  return Number.isFinite(parsed) ? parsed : 0;
}

export function roundMoney(value: number): number {
  return Math.round(value * 100) / 100;
}

export function formatMoney(value: number): string {
  return roundMoney(value).toFixed(2);
}

export function sourceDocumentTitle(label: string | null): string {
  return label === null ? "Source document is not loaded" : `Open ${label}`;
}

export function voidManualJournalTitle(
  entry: JournalEntrySummary,
  reversalPeriodState: PostingPeriodState
): string {
  if (entry.sourceType !== "Manual") {
    return "Only manual journals can be voided here.";
  }

  if (entry.status !== "Posted") {
    return "Only posted manual journals can be voided.";
  }

  return reversalPeriodState.blocksPosting
    ? `Cannot void today: ${reversalPeriodState.detail}`
    : "Void manual journal";
}

export function getPostingPeriodState(
  entryDate: string,
  periods: AccountingPeriod[]
): PostingPeriodState {
  const normalizedDate = entryDate.trim();

  if (normalizedDate === "") {
    return {
      tone: "draft",
      label: "MAIN posting period",
      status: "Date needed",
      detail: "Enter a posting date.",
      blocksPosting: true
    };
  }

  if (periods.length === 0) {
    return {
      tone: "draft",
      label: "MAIN posting period",
      status: "Unconfigured",
      detail: "Posting is available until MAIN periods are configured.",
      blocksPosting: false
    };
  }

  const period = periods.find((candidate) =>
    normalizedDate >= candidate.startsOn && normalizedDate <= candidate.endsOn);

  if (period === undefined) {
    return {
      tone: "voided",
      label: "MAIN posting period",
      status: "No period",
      detail: "Create a MAIN period for this date or choose an open date.",
      blocksPosting: true
    };
  }

  if (period.status.toLowerCase() === "open") {
    return {
      tone: "open",
      label: "MAIN posting period",
      status: "Open",
      detail: `${period.name} ${period.startsOn} to ${period.endsOn}`,
      blocksPosting: false
    };
  }

  return {
    tone: "closed",
    label: "MAIN posting period",
    status: period.status,
    detail: `${period.name} is closed. Reopen it or choose an open date.`,
    blocksPosting: true
  };
}

export function getTodayPostingPeriodState(
  periods: AccountingPeriod[]
): PostingPeriodState {
  return getPostingPeriodState(toDateInputValue(new Date()), periods);
}

export function formatSourceAmount(sourceDocument: JournalEntrySourceDocument): string {
  return sourceDocument.amount === null || sourceDocument.amount === undefined
    ? "-"
    : `${formatMoney(sourceDocument.amount)} ${sourceDocument.currencyCode ?? ""}`.trim();
}

export function formatAccount(
  ledgerAccountId: string,
  accounts: LedgerAccountSummary[]
): string {
  const account = accounts.find((candidate) => candidate.ledgerAccountId === ledgerAccountId);

  return account === undefined
    ? ledgerAccountId
    : `${account.displayCode} ${account.name}`;
}
