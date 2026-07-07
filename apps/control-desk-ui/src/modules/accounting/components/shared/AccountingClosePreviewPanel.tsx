import type { AccountingPeriodCloseJournalPreview } from "../../types/accountingTypes";
import {
  formatMoney,
  getCloseJournalPreviewFacts,
  getClosePreviewEntryState,
  getClosePreviewLineSide
} from "../../utils/accountingPeriodsWorkspaceModel";

type AccountingClosePreviewPanelProps = {
  closeJournalPreview: AccountingPeriodCloseJournalPreview;
};

export function AccountingClosePreviewPanel({
  closeJournalPreview
}: AccountingClosePreviewPanelProps) {
  return (
    <section className={`client-panel accounting-close-preview-panel${
      closeJournalPreview.canGenerate ? " ready" : " blocked"
    }`}>
      <div className="client-panel-heading">
        <div>
          <span>{closeJournalPreview.period.name}</span>
          <strong>Close journal preview</strong>
        </div>
        <span className={`status-pill ${closeJournalPreview.canGenerate ? "open" : "voided"}`}>
          {closeJournalPreview.canGenerate ? "Ready" : "Blocked"}
        </span>
      </div>
      <div className="accounting-close-preview-summary">
        {getCloseJournalPreviewFacts(closeJournalPreview).map((fact) => (
          <span className={fact.tone} key={fact.label} title={fact.title}>
            <small>{fact.label}</small>
            <strong>{fact.value}</strong>
          </span>
        ))}
      </div>
      {closeJournalPreview.blockers.length > 0 && (
        <div className="accounting-close-preview-blockers">
          {closeJournalPreview.blockers.map((blocker) => (
            <span key={blocker}>{blocker}</span>
          ))}
        </div>
      )}
      <div className="accounting-close-preview-entry-list">
        {closeJournalPreview.entries.length === 0 ? (
          <p className="accounting-close-preview-empty">No close journal lines</p>
        ) : (
          closeJournalPreview.entries.map((entry) => {
            const entryState = getClosePreviewEntryState(entry);

            return (
              <article className="accounting-close-preview-entry" key={entry.sourceReference}>
                <header className="accounting-close-preview-entry-header">
                  <div>
                    <span>{entry.sourceReference}</span>
                    <strong>{entry.memo}</strong>
                  </div>
                  <div className="accounting-close-preview-entry-meta">
                    <span>{entry.entryDate}</span>
                    <strong>{entry.currencyCode}</strong>
                    <em className={entryState.tone} title={entryState.title}>
                      {entryState.label}
                    </em>
                  </div>
                </header>
                <table className="accounting-close-preview-table">
                  <thead>
                    <tr>
                      <th>Side</th>
                      <th>Account</th>
                      <th>Type</th>
                      <th>Description</th>
                      <th>Debit</th>
                      <th>Credit</th>
                    </tr>
                  </thead>
                  <tbody>
                    {entry.lines.map((line) => {
                      const lineSide = getClosePreviewLineSide(line);

                      return (
                        <tr key={`${entry.sourceReference}-${line.ledgerAccountId}-${line.description}`}>
                          <td>
                            <span
                              className={`accounting-close-preview-side ${lineSide.tone}`}
                              title={lineSide.title}
                            >
                              {lineSide.label}
                            </span>
                          </td>
                          <td>
                            <strong>{line.code}</strong>
                            <small>{line.name}</small>
                          </td>
                          <td>{line.type}</td>
                          <td>{line.description}</td>
                          <td className="numeric">{formatMoney(line.debit)}</td>
                          <td className="numeric">{formatMoney(line.credit)}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                  <tfoot>
                    <tr>
                      <td colSpan={4}>Entry total</td>
                      <td className="numeric">{formatMoney(entry.totalDebit)}</td>
                      <td className="numeric">{formatMoney(entry.totalCredit)}</td>
                    </tr>
                  </tfoot>
                </table>
              </article>
            );
          })
        )}
      </div>
    </section>
  );
}
