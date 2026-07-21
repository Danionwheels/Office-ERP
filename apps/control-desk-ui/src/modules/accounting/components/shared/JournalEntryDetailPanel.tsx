import type {
  JournalEntrySourceDocument,
  JournalEntrySummary,
  LedgerAccountSummary
} from "../../types/accountingTypes";
import {
  formatJournalLineAccountCode,
  formatJournalLineAccountMeta,
  formatJournalLineAccountName,
  formatJournalLineClass,
  formatOpeningBalanceSourceCarryForwardAccount,
  formatOpeningBalanceSourceFiscalYear,
  findLedgerAccount,
  getJournalDetailStatusItems,
  getJournalEntryLineSide
} from "../../utils/journalWorkbenchModel";
import {
  formatMoney,
  formatSourceAmount,
  roundMoney
} from "../../utils/journalModel";

type JournalEntryDetailPanelProps = {
  entry: JournalEntrySummary | null;
  accounts: LedgerAccountSummary[];
  sourceDocument: JournalEntrySourceDocument | null;
  sourceDocumentClientLabel: string;
};

export function JournalEntryDetailPanel({
  entry,
  accounts,
  sourceDocument,
  sourceDocumentClientLabel
}: JournalEntryDetailPanelProps) {
  if (entry === null) {
    return (
      <section className="client-panel journal-detail-empty">
        <div className="client-panel-heading journal-window-heading">
          <div>
            <span>Journal Detail</span>
            <strong>No journal selected</strong>
          </div>
        </div>
      </section>
    );
  }

  const detailDifference = roundMoney(entry.totalDebit - entry.totalCredit);
  const detailTone = detailDifference === 0 ? "ready" : "warning";
  const statusItems = getJournalDetailStatusItems(entry, sourceDocument);

  return (
    <section className="client-panel journal-line-detail">
      <div className="journal-line-detail-heading">
        <div>
          <span>{entry.journalEntryId}</span>
          <strong>{entry.sourceReference ?? entry.sourceType}</strong>
          <small>{entry.entryDate} / {entry.sourceType} / {entry.currencyCode}</small>
        </div>
        <div className="journal-line-detail-facts">
          <span>
            <small>Lines</small>
            <strong>{entry.lines.length}</strong>
          </span>
          <span>
            <small>Debit</small>
            <strong>{formatMoney(entry.totalDebit)}</strong>
          </span>
          <span>
            <small>Credit</small>
            <strong>{formatMoney(entry.totalCredit)}</strong>
          </span>
          <span className={detailTone}>
            <small>Balance</small>
            <strong>{detailDifference === 0 ? "Balanced" : formatMoney(Math.abs(detailDifference))}</strong>
          </span>
        </div>
      </div>
      <div className="journal-line-detail-status-row" aria-label="Journal detail status">
        {statusItems.map((item) => (
          <span className={item.tone} key={item.label}>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
          </span>
        ))}
      </div>
      {sourceDocument !== null && (
        <SourceDocumentSummary
          sourceDocument={sourceDocument}
          clientLabel={sourceDocumentClientLabel}
        />
      )}
      <div className="journal-detail-table-frame">
        <table className="journal-line-detail-table">
          <thead>
            <tr>
              <th>No.</th>
              <th>Side</th>
              <th>Account</th>
              <th>Class</th>
              <th>Narration</th>
              <th>Debit</th>
              <th>Credit</th>
            </tr>
          </thead>
          <tbody>
            {entry.lines.map((line, index) => {
              const account = findLedgerAccount(line.ledgerAccountId, accounts);
              const lineSide = getJournalEntryLineSide(line);

              return (
                <tr
                  className={lineSide.toLowerCase()}
                  key={`${entry.journalEntryId}-${line.ledgerAccountId}-${index}`}
                >
                  <td className="numeric">{index + 1}</td>
                  <td>
                    <span className={`journal-line-side ${lineSide.toLowerCase()}`}>{lineSide}</span>
                  </td>
                  <td>
                    <span className="journal-line-account">
                      <strong>{formatJournalLineAccountCode(account, line.ledgerAccountId)}</strong>
                      <small>{formatJournalLineAccountName(account)}</small>
                    </span>
                  </td>
                  <td>
                    <span className="journal-line-class">
                      <strong>{formatJournalLineClass(account)}</strong>
                      <small>{formatJournalLineAccountMeta(account)}</small>
                    </span>
                  </td>
                  <td>
                    <span className="journal-line-narration">
                      {line.description?.trim() === "" || line.description === null || line.description === undefined
                        ? "-"
                        : line.description}
                    </span>
                  </td>
                  <td className="numeric">{formatMoney(line.debit)}</td>
                  <td className="numeric">{formatMoney(line.credit)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function SourceDocumentSummary({
  sourceDocument,
  clientLabel
}: {
  sourceDocument: JournalEntrySourceDocument;
  clientLabel: string;
}) {
  if (!sourceDocument.isResolved) {
    return (
      <div className="journal-source-summary unresolved">
        <span>
          <small>Source document</small>
          <strong>{sourceDocument.message ?? "Not resolved"}</strong>
        </span>
      </div>
    );
  }

  return (
    <div className="journal-source-summary">
      <span>
        <small>Source document</small>
        <strong>{sourceDocument.label ?? sourceDocument.reference ?? sourceDocument.documentKind ?? "Resolved"}</strong>
      </span>
      <span>
        <small>Status</small>
        <strong>{sourceDocument.status ?? "-"}</strong>
      </span>
      <span>
        <small>Date</small>
        <strong>{sourceDocument.documentDate ?? "-"}</strong>
      </span>
      <span>
        <small>Amount</small>
        <strong>{formatSourceAmount(sourceDocument)}</strong>
      </span>
      <span>
        <small>Client</small>
        <strong>{sourceDocument.documentKind === "OpeningBalance" ? "MAIN" : clientLabel}</strong>
      </span>
      {sourceDocument.documentKind === "OpeningBalance" && (
        <>
          <span>
            <small>FY Range</small>
            <strong>{formatOpeningBalanceSourceFiscalYear(sourceDocument)}</strong>
          </span>
          <span>
            <small>Transactions</small>
            <strong>{sourceDocument.transactionsAllowed === false ? "Locked" : "Allowed"}</strong>
          </span>
          <span>
            <small>PL Carry-forward</small>
            <strong>{formatOpeningBalanceSourceCarryForwardAccount(sourceDocument)}</strong>
          </span>
          {sourceDocument.message !== null && sourceDocument.message !== undefined && (
            <span className="warning">
              <small>Profile note</small>
              <strong>{sourceDocument.message}</strong>
            </span>
          )}
        </>
      )}
    </div>
  );
}
