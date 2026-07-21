import {
  BookOpenCheck,
  CalendarDays,
  FileText,
  Link2,
  ReceiptText,
  ScrollText,
  WalletCards,
  type LucideIcon
} from "lucide-react";
import type { ClientStatement } from "../../types/statementTypes";
import type { StatementControlKey } from "../../types/statementWorkspaceTypes";
import {
  cleanOptional,
  formatDate,
  formatMoney,
  formatSourceType,
  getStatementControlRows,
  getStatementLedgerBridgeItems,
  isBalancedPosting,
  shortId
} from "../../utils/statementWorkspaceModel";

const controlIcons: Record<StatementControlKey, LucideIcon> = {
  period: CalendarDays,
  balance: WalletCards,
  invoices: FileText,
  receipts: ReceiptText,
  lines: ScrollText,
  postings: BookOpenCheck
};

export function StatementStatePanel({
  eyebrow,
  title,
  detail,
  icon = "statement",
  compact = false,
  panel = false
}: {
  eyebrow: string;
  title: string;
  detail: string;
  icon?: "statement" | "ledger";
  compact?: boolean;
  panel?: boolean;
}) {
  const Icon = icon === "ledger" ? BookOpenCheck : ScrollText;
  const className = [
    "statement-state-panel",
    compact ? "compact" : null,
    panel ? "client-panel" : null
  ].filter((item): item is string => item !== null).join(" ");

  return (
    <section className={className} aria-label={eyebrow}>
      <span className="statement-state-icon" aria-hidden="true">
        <Icon size={18} />
      </span>
      <div>
        <span>{eyebrow}</span>
        <strong>{title}</strong>
        <small>{detail}</small>
      </div>
    </section>
  );
}

export function StatementControlBoard({ statement }: { statement: ClientStatement }) {
  const rows = getStatementControlRows(statement);

  return (
    <div className="statement-control-board">
      {rows.map((row) => {
        const Icon = controlIcons[row.key];

        return (
          <article className={`statement-control-card ${row.tone}`} key={row.key}>
            <Icon size={17} />
            <span>
              <strong>{row.label}</strong>
              <em>{row.status}</em>
              <small>{row.detail}</small>
            </span>
          </article>
        );
      })}
    </div>
  );
}

export function StatementCurrencyBoard({ statement }: { statement: ClientStatement }) {
  if (statement.currencySummaries.length === 0) {
    return (
      <StatementStatePanel
        eyebrow="Currency summary"
        title="No posted receivables"
        detail="No invoice, receipt, or credit balance is available for this statement."
        compact
      />
    );
  }

  return (
    <div className="statement-currency-board">
      {statement.currencySummaries.map((summary) => (
        <article
          className={`statement-currency-card ${summary.balanceDue > 0 ? "warning" : "ready"}`}
          key={summary.currencyCode}
        >
          <header>
            <span>Currency</span>
            <strong>{summary.currencyCode}</strong>
          </header>
          <dl>
            <div>
              <dt>Balance</dt>
              <dd>{formatMoney(summary.balanceDue, summary.currencyCode)}</dd>
            </div>
            <div>
              <dt>Invoiced</dt>
              <dd>{formatMoney(summary.totalInvoiced, summary.currencyCode)}</dd>
            </div>
            <div>
              <dt>Paid</dt>
              <dd>{formatMoney(summary.totalPaid, summary.currencyCode)}</dd>
            </div>
            <div>
              <dt>Credit</dt>
              <dd>{formatMoney(summary.availableCredit, summary.currencyCode)}</dd>
            </div>
            <div>
              <dt>Open invoices</dt>
              <dd>{summary.openInvoiceCount} / {summary.invoiceCount}</dd>
            </div>
          </dl>
        </article>
      ))}
    </div>
  );
}

export function StatementLedgerBridge({ statement }: { statement: ClientStatement }) {
  const items = getStatementLedgerBridgeItems(statement);

  return (
    <section className="client-panel statement-ledger-bridge">
      <div className="client-panel-heading">
        <div>
          <span>GL trail</span>
          <strong>Posting bridge</strong>
        </div>
        <BookOpenCheck size={18} />
      </div>
      <div className="statement-ledger-bridge-grid">
        {items.map((item) => (
          <span className={`statement-ledger-bridge-item ${item.tone}`} key={item.label}>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
            <em>{item.detail}</em>
          </span>
        ))}
      </div>
    </section>
  );
}

export function StatementLineRegister({ statement }: { statement: ClientStatement }) {
  return (
    <div className="client-panel statement-lines-panel">
      <div className="client-panel-heading">
        <div>
          <span>Statement</span>
          <strong>{statement.lines.length} lines</strong>
        </div>
        <ScrollText size={18} />
      </div>
      <table className="statement-table statement-lines-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Type</th>
            <th>Reference</th>
            <th>Journal</th>
            <th>Description</th>
            <th className="numeric">Debit</th>
            <th className="numeric">Credit</th>
            <th className="numeric">Balance</th>
          </tr>
        </thead>
        <tbody>
          {statement.lines.length === 0 ? (
            <StatementEmptyRow colSpan={8} message="No posted invoice or payment lines" />
          ) : (
            statement.lines.map((line, index) => (
              <tr key={`${line.documentType}-${line.reference}-${line.entryDate}-${index}`}>
                <td>{formatDate(line.entryDate)}</td>
                <td>{line.documentType}</td>
                <td>{line.reference}</td>
                <td>
                  <JournalReference journalEntryId={line.journalEntryId} />
                </td>
                <td>{line.description}</td>
                <td className="numeric">{line.debit === 0 ? "-" : formatMoney(line.debit, line.currencyCode)}</td>
                <td className="numeric">{line.credit === 0 ? "-" : formatMoney(line.credit, line.currencyCode)}</td>
                <td className="numeric">{formatMoney(line.runningBalance, line.currencyCode)}</td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

export function StatementInvoiceRegister({ statement }: { statement: ClientStatement }) {
  return (
    <div className="statement-register-frame statement-document-register">
      <table className="statement-register-table statement-invoice-table" aria-label="Statement invoices">
        <thead>
          <tr>
            <th scope="col">Invoice</th>
            <th scope="col">Status</th>
            <th scope="col">Issue</th>
            <th scope="col">Due</th>
            <th className="numeric" scope="col">Total</th>
            <th className="numeric" scope="col">Paid</th>
            <th className="numeric" scope="col">Balance</th>
            <th scope="col">Journal</th>
          </tr>
        </thead>
        <tbody>
          {statement.invoices.length === 0 ? (
            <StatementEmptyRow colSpan={8} message="No invoices" />
          ) : (
            statement.invoices.map((invoice) => (
              <tr className={invoice.balanceDue > 0 ? "warning" : "ready"} key={invoice.invoiceId}>
                <td>
                  <strong>{invoice.invoiceNumber}</strong>
                </td>
                <td>
                  <span className={`statement-register-status ${invoice.balanceDue > 0 ? "warning" : "ready"}`}>
                    {invoice.status}
                  </span>
                </td>
                <td>{formatDate(invoice.issueDate)}</td>
                <td>{formatDate(invoice.dueDate)}</td>
                <td className="numeric">{formatMoney(invoice.totalAmount, invoice.currencyCode)}</td>
                <td className="numeric">{formatMoney(invoice.amountPaid, invoice.currencyCode)}</td>
                <td className="numeric">{formatMoney(invoice.balanceDue, invoice.currencyCode)}</td>
                <td>
                  <JournalReference journalEntryId={invoice.journalEntryId} />
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

export function StatementPaymentRegister({ statement }: { statement: ClientStatement }) {
  return (
    <div className="statement-register-frame statement-document-register">
      <table className="statement-register-table statement-payment-table" aria-label="Statement payments">
        <thead>
          <tr>
            <th scope="col">Reference</th>
            <th scope="col">Status</th>
            <th scope="col">Method</th>
            <th scope="col">Received</th>
            <th className="numeric" scope="col">Amount</th>
            <th scope="col">Journal</th>
          </tr>
        </thead>
        <tbody>
          {statement.payments.length === 0 ? (
            <StatementEmptyRow colSpan={6} message="No payments" />
          ) : (
            statement.payments.map((payment) => (
              <tr className="ready" key={payment.paymentId}>
                <td>
                  <strong>{payment.reference}</strong>
                </td>
                <td>
                  <span className="statement-register-status ready">{payment.status}</span>
                </td>
                <td>{payment.method}</td>
                <td>{formatDate(payment.receivedOn)}</td>
                <td className="numeric">{formatMoney(payment.amount, payment.currencyCode)}</td>
                <td>
                  <JournalReference journalEntryId={payment.journalEntryId} />
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

export function StatementJournalPostingRegister({ statement }: { statement: ClientStatement }) {
  return (
    <table className="statement-table statement-journal-table" aria-label="Statement journal postings">
      <thead>
        <tr>
          <th>Date</th>
          <th>Source</th>
          <th>Reference</th>
          <th>Status</th>
          <th className="numeric">Debit</th>
          <th className="numeric">Credit</th>
          <th>Lines</th>
        </tr>
      </thead>
      <tbody>
        {statement.journalPostings.length === 0 ? (
          <StatementEmptyRow colSpan={7} message="No journal postings" />
        ) : (
          statement.journalPostings.map((posting) => (
              <tr className={isBalancedPosting(posting) ? "ready" : "warning"} key={posting.journalEntryId}>
                <td>{formatDate(posting.entryDate)}</td>
                <td>
                  <span className="statement-source-cell">
                    <strong>{formatSourceType(posting.sourceType)}</strong>
                    <small>{posting.memo?.trim() === "" || posting.memo === null || posting.memo === undefined ? "No memo" : posting.memo}</small>
                  </span>
                </td>
                <td>
                  <span className="statement-source-cell">
                    <strong>{posting.sourceReference ?? shortId(posting.journalEntryId)}</strong>
                    <JournalReference journalEntryId={posting.journalEntryId} />
                  </span>
                </td>
                <td>
                  <span className={`statement-register-status ${isBalancedPosting(posting) ? "ready" : "warning"}`}>
                    {posting.status}
                  </span>
                </td>
                <td className="numeric">{formatMoney(posting.totalDebit, posting.currencyCode)}</td>
                <td className="numeric">{formatMoney(posting.totalCredit, posting.currencyCode)}</td>
                <td>{posting.lineCount} lines</td>
              </tr>
          ))
        )}
      </tbody>
    </table>
  );
}

function StatementEmptyRow({
  colSpan,
  message
}: {
  colSpan: number;
  message: string;
}) {
  return (
    <tr className="statement-empty-row">
      <td colSpan={colSpan}>
        <span>{message}</span>
      </td>
    </tr>
  );
}

function JournalReference({
  journalEntryId
}: {
  journalEntryId?: string | null;
}) {
  const normalizedJournalEntryId = cleanOptional(journalEntryId);
  const hasJournal = normalizedJournalEntryId !== null;

  return (
    <span
      className={`statement-journal-ref ${hasJournal ? "posted" : "missing"}`}
      title={hasJournal ? normalizedJournalEntryId : "No journal entry linked"}
    >
      <Link2 size={12} />
      {hasJournal ? shortId(normalizedJournalEntryId) : "No journal"}
    </span>
  );
}
