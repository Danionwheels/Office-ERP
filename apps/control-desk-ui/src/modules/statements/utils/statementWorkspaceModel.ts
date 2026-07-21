import type { ClientStatement } from "../types/statementTypes";
import type {
  StatementControlRow,
  StatementLedgerBridgeItem,
  StatementTotals
} from "../types/statementWorkspaceTypes";

export function getStatementLedgerBridgeItems(statement: ClientStatement): StatementLedgerBridgeItem[] {
  const journalLineCount = getJournalLineCount(statement);
  const unlinkedStatementLineCount = statement.lines.filter((line) => !hasJournalEntry(line.journalEntryId)).length;
  const unlinkedDocumentCount = statement.invoices.filter((invoice) => !hasJournalEntry(invoice.journalEntryId)).length
    + statement.payments.filter((payment) => !hasJournalEntry(payment.journalEntryId)).length;
  const hasPostings = statement.journalPostings.length > 0;
  const postingsAreBalanced = hasPostings && statement.journalPostings.every(isBalancedPosting);

  return [
    {
      label: "Running balance",
      value: `${statement.registers.lines.filteredCount} lines`,
      detail: unlinkedStatementLineCount === 0
        ? `${statement.lines.length} recent lines loaded`
        : `${unlinkedStatementLineCount} loaded lines need journal review`,
      tone: statement.registers.lines.filteredCount === 0
        ? "neutral"
        : unlinkedStatementLineCount === 0
          ? "ready"
          : "warning"
    },
    {
      label: "Journal postings",
      value: `${statement.registers.journalPostings.filteredCount} entries`,
      detail: `${journalLineCount} debit/credit lines loaded`,
      tone: hasPostings ? "ready" : "neutral"
    },
    {
      label: "Posting balance",
      value: hasPostings ? (postingsAreBalanced ? "Balanced" : "Review") : "No postings",
      detail: formatPostingTotals(statement),
      tone: !hasPostings ? "neutral" : postingsAreBalanced ? "ready" : "warning"
    },
    {
      label: "Documents",
      value: `${statement.registers.invoices.filteredCount + statement.registers.payments.filteredCount} docs`,
      detail: unlinkedDocumentCount === 0 ? "Invoices and receipts are journal-linked" : `${unlinkedDocumentCount} documents need journal links`,
      tone: statement.registers.invoices.filteredCount + statement.registers.payments.filteredCount === 0
        ? "neutral"
        : unlinkedDocumentCount === 0
          ? "ready"
          : "warning"
    }
  ];
}

export function getStatementControlRows(statement: ClientStatement): StatementControlRow[] {
  const summaryTotals = getStatementTotals(statement);
  const journalLineCount = getJournalLineCount(statement);
  const period = statement.fromDate !== null
    && statement.fromDate !== undefined
    && statement.toDate !== null
    && statement.toDate !== undefined
    ? `${formatDate(statement.fromDate)} to ${formatDate(statement.toDate)}`
    : "Full available history";

  return [
    {
      key: "period",
      label: "Period",
      status: statement.fromDate === null || statement.fromDate === undefined ? "All dates" : "Filtered",
      detail: period,
      tone: "neutral"
    },
    {
      key: "balance",
      label: "Balance",
      status: summaryTotals.openInvoiceCount === 0 ? "Clear" : `${summaryTotals.openInvoiceCount} open`,
      detail: summaryTotals.balanceSummary,
      tone: summaryTotals.openInvoiceCount === 0 ? "ready" : "warning"
    },
    {
      key: "invoices",
      label: "Invoices",
      status: `${statement.registers.invoices.filteredCount} invoices`,
      detail: `${summaryTotals.invoiceCount} posted, ${summaryTotals.openInvoiceCount} open`,
      tone: summaryTotals.openInvoiceCount === 0 ? "ready" : "warning"
    },
    {
      key: "receipts",
      label: "Receipts",
      status: `${statement.registers.payments.filteredCount} payments`,
      detail: summaryTotals.paidSummary,
      tone: statement.registers.payments.filteredCount === 0 ? "neutral" : "ready"
    },
    {
      key: "lines",
      label: "Statement lines",
      status: `${statement.registers.lines.filteredCount} lines`,
      detail: statement.registers.lines.filteredCount === 0 ? "No posted invoice or payment lines" : "Running balance trail is available",
      tone: statement.registers.lines.filteredCount === 0 ? "neutral" : "ready"
    },
    {
      key: "postings",
      label: "GL postings",
      status: `${statement.registers.journalPostings.filteredCount} postings`,
      detail: statement.registers.journalPostings.filteredCount === 0 ? "No journal postings" : `${journalLineCount} debit and credit lines loaded`,
      tone: statement.registers.journalPostings.filteredCount === 0 ? "neutral" : "ready"
    }
  ];
}

export function getStatementTotals(statement: ClientStatement): StatementTotals {
  const balanceSummary = statement.currencySummaries.length === 0
    ? "No receivables posted"
    : statement.currencySummaries
        .map((summary) => formatMoney(summary.balanceDue, summary.currencyCode))
        .join(" / ");
  const paidSummary = statement.currencySummaries.length === 0
    ? "No receipts posted"
    : statement.currencySummaries
        .map((summary) => `${formatMoney(summary.totalPaid, summary.currencyCode)} collected`)
        .join(" / ");

  return {
    balanceSummary,
    invoiceCount: statement.currencySummaries.reduce(
      (total, summary) => total + summary.invoiceCount,
      0
    ),
    openInvoiceCount: statement.currencySummaries.reduce(
      (total, summary) => total + summary.openInvoiceCount,
      0
    ),
    paidSummary
  };
}

export function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

export function formatPostingTotals(statement: ClientStatement): string {
  if (statement.journalPostings.length === 0) {
    return "No debit/credit totals";
  }

  return `${formatCurrencyTotals(statement, (posting) => posting.totalDebit)} debit / ${formatCurrencyTotals(
    statement,
    (posting) => posting.totalCredit
  )} credit`;
}

export function getJournalLineCount(statement: ClientStatement): number {
  return statement.journalPostings.reduce((total, posting) => total + posting.lineCount, 0);
}

export function hasJournalEntry(journalEntryId?: string | null): boolean {
  return cleanOptional(journalEntryId) !== null;
}

export function isBalancedPosting(posting: ClientStatement["journalPostings"][number]): boolean {
  return Math.abs(posting.totalDebit - posting.totalCredit) < 0.005;
}

export function formatSourceType(value: string): string {
  return value.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
}

export function cleanOptional(value?: string | null): string | null {
  const trimmed = value?.trim() ?? "";

  return trimmed === "" ? null : trimmed;
}

export function shortId(value?: string | null): string {
  return value === undefined || value === null || value === "" ? "-" : value.slice(0, 8);
}

function formatCurrencyTotals(
  statement: ClientStatement,
  pickAmount: (posting: ClientStatement["journalPostings"][number]) => number
): string {
  const totalsByCurrency = new Map<string, number>();

  statement.journalPostings.forEach((posting) => {
    totalsByCurrency.set(
      posting.currencyCode,
      (totalsByCurrency.get(posting.currencyCode) ?? 0) + pickAmount(posting)
    );
  });

  return Array.from(totalsByCurrency.entries())
    .sort(([firstCurrency], [secondCurrency]) => firstCurrency.localeCompare(secondCurrency))
    .map(([currencyCode, amount]) => formatMoney(amount, currencyCode))
    .join(" / ");
}
