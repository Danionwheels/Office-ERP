import { RefreshCw, ScrollText } from "lucide-react";
import type { ClientDetails } from "../../clients/types/clientTypes";
import type { ClientStatement } from "../types/statementTypes";

type ClientStatementPanelProps = {
  client: ClientDetails | null;
  statement: ClientStatement | null;
  isBusy: boolean;
  onRefresh: () => Promise<void>;
};

export function ClientStatementPanel({
  client,
  statement,
  isBusy,
  onRefresh
}: ClientStatementPanelProps) {
  if (client === null) {
    return (
      <section className="client-panel statement-empty-panel">
        <div className="client-empty-detail">Select a client</div>
      </section>
    );
  }

  return (
    <section className="statement-workspace">
      <header className="statement-header client-panel">
        <div>
          <span>Receivables</span>
          <h2>Client statement</h2>
        </div>
        <button className="icon-button" type="button" onClick={onRefresh} disabled={isBusy} title="Refresh statement">
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      {statement === null ? (
        <div className="client-empty-state">No statement loaded</div>
      ) : (
        <>
          <div className="statement-summary-grid">
            {statement.currencySummaries.length === 0 ? (
              <div className="client-empty-state">No posted receivables yet</div>
            ) : (
              statement.currencySummaries.map((summary) => (
                <article className="client-panel statement-summary-card" key={summary.currencyCode}>
                  <span>{summary.currencyCode}</span>
                  <strong>{formatMoney(summary.balanceDue, summary.currencyCode)}</strong>
                  <dl>
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
                      <dt>Open</dt>
                      <dd>{summary.openInvoiceCount} / {summary.invoiceCount}</dd>
                    </div>
                  </dl>
                </article>
              ))
            )}
          </div>

          <div className="client-panel statement-lines-panel">
            <div className="client-panel-heading">
              <div>
                <span>Statement</span>
                <strong>{statement.lines.length} lines</strong>
              </div>
              <ScrollText size={18} />
            </div>
            <table className="statement-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Type</th>
                  <th>Reference</th>
                  <th>Description</th>
                  <th className="numeric">Debit</th>
                  <th className="numeric">Credit</th>
                  <th className="numeric">Balance</th>
                </tr>
              </thead>
              <tbody>
                {statement.lines.length === 0 ? (
                  <tr>
                    <td colSpan={7}>No posted invoice or payment lines</td>
                  </tr>
                ) : (
                  statement.lines.map((line) => (
                    <tr key={`${line.documentType}-${line.reference}-${line.entryDate}`}>
                      <td>{formatDate(line.entryDate)}</td>
                      <td>{line.documentType}</td>
                      <td>{line.reference}</td>
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

          <div className="statement-detail-grid">
            <div className="client-panel statement-list-panel">
              <div className="client-panel-heading">
                <div>
                  <span>Documents</span>
                  <strong>Invoices</strong>
                </div>
                <span className="billing-small-fact">{statement.invoices.length}</span>
              </div>
              <div className="statement-card-list">
                {statement.invoices.length === 0 && <div className="client-empty-state">No invoices</div>}
                {statement.invoices.map((invoice) => (
                  <article className="statement-card" key={invoice.invoiceId}>
                    <header>
                      <strong>{invoice.invoiceNumber}</strong>
                      <span className={`status-pill ${invoice.status.toLowerCase()}`}>{invoice.status}</span>
                    </header>
                    <dl>
                      <div>
                        <dt>Total</dt>
                        <dd>{formatMoney(invoice.totalAmount, invoice.currencyCode)}</dd>
                      </div>
                      <div>
                        <dt>Paid</dt>
                        <dd>{formatMoney(invoice.amountPaid, invoice.currencyCode)}</dd>
                      </div>
                      <div>
                        <dt>Balance</dt>
                        <dd>{formatMoney(invoice.balanceDue, invoice.currencyCode)}</dd>
                      </div>
                      <div>
                        <dt>Journal</dt>
                        <dd>{shortId(invoice.journalEntryId)}</dd>
                      </div>
                    </dl>
                  </article>
                ))}
              </div>
            </div>

            <div className="client-panel statement-list-panel">
              <div className="client-panel-heading">
                <div>
                  <span>Receipts</span>
                  <strong>Payments</strong>
                </div>
                <span className="billing-small-fact">{statement.payments.length}</span>
              </div>
              <div className="statement-card-list">
                {statement.payments.length === 0 && <div className="client-empty-state">No payments</div>}
                {statement.payments.map((payment) => (
                  <article className="statement-card" key={payment.paymentId}>
                    <header>
                      <strong>{payment.reference}</strong>
                      <span className={`status-pill ${payment.status.toLowerCase()}`}>{payment.status}</span>
                    </header>
                    <dl>
                      <div>
                        <dt>Amount</dt>
                        <dd>{formatMoney(payment.amount, payment.currencyCode)}</dd>
                      </div>
                      <div>
                        <dt>Method</dt>
                        <dd>{payment.method}</dd>
                      </div>
                      <div>
                        <dt>Received</dt>
                        <dd>{formatDate(payment.receivedOn)}</dd>
                      </div>
                      <div>
                        <dt>Journal</dt>
                        <dd>{shortId(payment.journalEntryId)}</dd>
                      </div>
                    </dl>
                  </article>
                ))}
              </div>
            </div>
          </div>

          <div className="client-panel statement-journal-panel">
            <div className="client-panel-heading">
              <div>
                <span>Accounting</span>
                <strong>Journal postings</strong>
              </div>
              <span className="billing-small-fact">{statement.journalPostings.length}</span>
            </div>
            <table className="statement-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Source</th>
                  <th>Reference</th>
                  <th>Status</th>
                  <th className="numeric">Debit</th>
                  <th className="numeric">Credit</th>
                </tr>
              </thead>
              <tbody>
                {statement.journalPostings.length === 0 ? (
                  <tr>
                    <td colSpan={6}>No journal postings</td>
                  </tr>
                ) : (
                  statement.journalPostings.map((posting) => (
                    <tr key={posting.journalEntryId}>
                      <td>{formatDate(posting.entryDate)}</td>
                      <td>{posting.sourceType}</td>
                      <td>{posting.sourceReference ?? shortId(posting.journalEntryId)}</td>
                      <td>{posting.status}</td>
                      <td className="numeric">{formatMoney(posting.totalDebit, posting.currencyCode)}</td>
                      <td className="numeric">{formatMoney(posting.totalCredit, posting.currencyCode)}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  );
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

function shortId(value?: string | null): string {
  return value === undefined || value === null || value === "" ? "-" : value.slice(0, 8);
}
