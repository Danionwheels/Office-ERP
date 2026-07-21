import type {
  AccountingCloseJournalPreviewEntry,
  AccountingCloseJournalPreviewLine,
  AccountingPeriod,
  AccountingPeriodCloseCurrency,
  AccountingPeriodCloseJournalArtifact,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  AccountingPeriodCloseReadinessCheck,
  AccountingPeriodFormInput
} from "../types/accountingTypes";

export type AccountingPeriodCloseTone = "ready" | "warning" | "neutral";

export type AccountingPeriodCloseFact = {
  label: string;
  value: string;
  tone: AccountingPeriodCloseTone;
  title?: string;
};

export type CloseLineSide = {
  label: string;
  tone: "debit" | "credit" | "warning" | "neutral";
  title: string;
};

export type AccountingPeriodSummary = {
  currentPeriod: AccountingPeriod | null;
  openPeriods: number;
  closedPeriods: number;
};

export function getAccountingPeriodSummary(
  periods: AccountingPeriod[],
  value: AccountingPeriodFormInput
): AccountingPeriodSummary {
  return {
    currentPeriod: periods.find((period) => containsDate(period, value.startsOn)) ?? periods[0] ?? null,
    openPeriods: periods.filter((period) => period.status === "Open").length,
    closedPeriods: periods.filter((period) => period.status === "Closed").length
  };
}

export function getArtifactPeriod(
  periods: AccountingPeriod[],
  selectedArtifactPeriodId: string
): AccountingPeriod | null {
  return (
    periods.find((period) =>
      period.accountingPeriodId === selectedArtifactPeriodId && period.closeArtifact
    )
    ?? periods.find((period) => period.closeArtifact)
    ?? null
  );
}

export function getInspectedAccountingPeriodIds(
  readiness: AccountingPeriodCloseReadiness | null,
  closeJournalPreview: AccountingPeriodCloseJournalPreview | null,
  artifactPeriod: AccountingPeriod | null
): string[] {
  return [
    readiness?.period.accountingPeriodId ?? "",
    closeJournalPreview?.period.accountingPeriodId ?? "",
    artifactPeriod?.accountingPeriodId ?? ""
  ].filter((periodId) => periodId !== "");
}

export function canCreateAccountingPeriod(value: AccountingPeriodFormInput): boolean {
  return (
    value.startsOn.trim() !== ""
    && value.endsOn.trim() !== ""
    && value.endsOn >= value.startsOn
  );
}

export function isKnownBlocked(
  period: AccountingPeriod,
  readiness: AccountingPeriodCloseReadiness | null
): boolean {
  return readiness?.period.accountingPeriodId === period.accountingPeriodId && !readiness.canClose;
}

export function getAccountingPeriodReadinessFacts(
  readiness: AccountingPeriodCloseReadiness
): AccountingPeriodCloseFact[] {
  const blockedCheckCount = readiness.checks.filter(isBlockedReadinessCheck).length;
  const passedCheckCount = readiness.checks.filter(isPassedReadinessCheck).length;
  const draftJournalCount = readiness.currencies.reduce(
    (total, currency) => total + currency.draftJournalCount,
    0
  );
  const postedJournalCount = readiness.currencies.reduce(
    (total, currency) => total + currency.postedJournalCount,
    0
  );
  const outOfBalanceCurrencyCount = readiness.currencies.filter(
    (currency) => !isZeroMoney(currency.difference)
  ).length;

  return [
    {
      label: "Period window",
      value: formatPeriodWindow(readiness.period),
      tone: "neutral"
    },
    {
      label: "Close state",
      value: readiness.canClose ? "Ready to close" : "Blocked",
      tone: readiness.canClose ? "ready" : "warning"
    },
    {
      label: "Checks",
      value: `${passedCheckCount}/${readiness.checks.length} passed`,
      tone: blockedCheckCount === 0 ? "ready" : "warning",
      title: `${blockedCheckCount} blocked checks`
    },
    {
      label: "Currencies",
      value: formatCurrencyReadiness(readiness.currencies.length, outOfBalanceCurrencyCount),
      tone: outOfBalanceCurrencyCount === 0 ? "ready" : "warning"
    },
    {
      label: "Posted journals",
      value: String(postedJournalCount),
      tone: "neutral"
    },
    {
      label: "Draft journals",
      value: draftJournalCount === 0 ? "None" : String(draftJournalCount),
      tone: draftJournalCount === 0 ? "ready" : "warning"
    }
  ];
}

export function getCloseJournalPreviewFacts(
  preview: AccountingPeriodCloseJournalPreview
): AccountingPeriodCloseFact[] {
  const difference = preview.totalDebit - preview.totalCredit;
  const lineCount = preview.entries.reduce((total, entry) => total + entry.lines.length, 0);

  return [
    {
      label: "Base currency",
      value: preview.baseCurrencyCode || "-",
      tone: "neutral"
    },
    {
      label: "Net income",
      value: formatMoney(preview.netIncome),
      tone: "neutral"
    },
    {
      label: "Total debit",
      value: formatMoney(preview.totalDebit),
      tone: "neutral"
    },
    {
      label: "Total credit",
      value: formatMoney(preview.totalCredit),
      tone: "neutral"
    },
    {
      label: "Difference",
      value: formatMoney(Math.abs(difference)),
      tone: isZeroMoney(difference) ? "ready" : "warning"
    },
    {
      label: "Close lines",
      value: `${lineCount} in ${preview.entries.length} entries`,
      tone: preview.canGenerate ? "ready" : "warning"
    }
  ];
}

export function getAccountingPeriodArtifactFacts(period: AccountingPeriod): AccountingPeriodCloseFact[] {
  if (!period.closeArtifact) {
    return [];
  }

  const artifact = period.closeArtifact;
  const passedCheckCount = Math.max(artifact.checkCount - artifact.blockedCheckCount, 0);
  const outOfBalanceCurrencyCount = artifact.currencies.filter(
    (currency) => !isZeroMoney(currency.difference)
  ).length;

  return [
    {
      label: "Closed window",
      value: formatPeriodWindow(period),
      tone: "neutral"
    },
    {
      label: "Generated by",
      value: artifact.generatedBy,
      tone: "neutral"
    },
    {
      label: "Checks",
      value: `${passedCheckCount}/${artifact.checkCount} passed`,
      tone: artifact.blockedCheckCount === 0 ? "ready" : "warning"
    },
    {
      label: "Currencies",
      value: formatCurrencyReadiness(artifact.currencyCount, outOfBalanceCurrencyCount),
      tone: outOfBalanceCurrencyCount === 0 ? "ready" : "warning"
    },
    {
      label: "Posted journals",
      value: String(artifact.postedJournalCount),
      tone: "neutral"
    },
    {
      label: "Draft journals",
      value: artifact.draftJournalCount === 0 ? "None" : String(artifact.draftJournalCount),
      tone: artifact.draftJournalCount === 0 ? "ready" : "warning"
    }
  ];
}

export function getCurrencyCloseState(currency: AccountingPeriodCloseCurrency): AccountingPeriodCloseFact {
  if (!isZeroMoney(currency.difference)) {
    return {
      label: "Out of balance",
      value: "Out of balance",
      tone: "warning",
      title: `Difference ${formatMoney(currency.difference)}`
    };
  }

  if (currency.draftJournalCount > 0) {
    return {
      label: "Drafts open",
      value: "Drafts open",
      tone: "warning",
      title: `${currency.draftJournalCount} draft journals remain in this period`
    };
  }

  if (currency.postedJournalCount === 0) {
    return {
      label: "No activity",
      value: "No activity",
      tone: "neutral",
      title: "No posted journals in this currency"
    };
  }

  return {
    label: "Balanced",
    value: "Balanced",
    tone: "ready",
    title: "Debits and credits match"
  };
}

export function getClosePreviewEntryState(
  entry: AccountingCloseJournalPreviewEntry
): AccountingPeriodCloseFact {
  const difference = entry.totalDebit - entry.totalCredit;

  return {
    label: isZeroMoney(difference) ? "Balanced" : `Out ${formatMoney(Math.abs(difference))}`,
    value: isZeroMoney(difference) ? "Balanced" : `Out ${formatMoney(Math.abs(difference))}`,
    tone: isZeroMoney(difference) ? "ready" : "warning",
    title: `Debit ${formatMoney(entry.totalDebit)} / Credit ${formatMoney(entry.totalCredit)}`
  };
}

export function getCloseJournalArtifactState(
  entry: AccountingPeriodCloseJournalArtifact
): AccountingPeriodCloseFact {
  const difference = entry.totalDebit - entry.totalCredit;

  return {
    label: isZeroMoney(difference) ? "Balanced" : `Out ${formatMoney(Math.abs(difference))}`,
    value: isZeroMoney(difference) ? "Balanced" : `Out ${formatMoney(Math.abs(difference))}`,
    tone: isZeroMoney(difference) ? "ready" : "warning",
    title: `Debit ${formatMoney(entry.totalDebit)} / Credit ${formatMoney(entry.totalCredit)}`
  };
}

export function getClosePreviewLineSide(line: AccountingCloseJournalPreviewLine): CloseLineSide {
  const hasDebit = !isZeroMoney(line.debit);
  const hasCredit = !isZeroMoney(line.credit);

  if (hasDebit && hasCredit) {
    return {
      label: "Both",
      tone: "warning",
      title: "Line carries both debit and credit"
    };
  }

  if (hasDebit) {
    return {
      label: "Debit",
      tone: "debit",
      title: "Debit close line"
    };
  }

  if (hasCredit) {
    return {
      label: "Credit",
      tone: "credit",
      title: "Credit close line"
    };
  }

  return {
    label: "Zero",
    tone: "neutral",
    title: "No debit or credit amount"
  };
}

export function formatReadinessCheckMessage(check: AccountingPeriodCloseReadinessCheck): string {
  const target = check.target?.trim();
  return target ? `${target}: ${check.message}` : check.message;
}

export function formatPeriodWindow(period: AccountingPeriod): string {
  return `${period.startsOn} to ${period.endsOn}`;
}

export function containsDate(period: AccountingPeriod, date: string): boolean {
  return date.trim() !== "" && date >= period.startsOn && date <= period.endsOn;
}

export function formatTimestamp(value: string): string {
  return value.slice(0, 10);
}

export function formatMoney(value: number): string {
  return value.toFixed(2);
}

export function isZeroMoney(value: number): boolean {
  return Math.abs(value) < 0.005;
}

function isPassedReadinessCheck(check: AccountingPeriodCloseReadinessCheck): boolean {
  return check.status.trim().toLowerCase() === "passed";
}

function isBlockedReadinessCheck(check: AccountingPeriodCloseReadinessCheck): boolean {
  return check.status.trim().toLowerCase() === "blocked";
}

function formatCurrencyReadiness(currencyCount: number, outOfBalanceCurrencyCount: number): string {
  if (currencyCount === 0) {
    return "No activity";
  }

  if (outOfBalanceCurrencyCount === 0) {
    return `${currencyCount} balanced`;
  }

  return `${outOfBalanceCurrencyCount} out`;
}
