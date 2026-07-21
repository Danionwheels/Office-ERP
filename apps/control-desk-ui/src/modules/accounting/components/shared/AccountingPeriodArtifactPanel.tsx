import { ListTree } from "lucide-react";
import type { AccountingPeriod } from "../../types/accountingTypes";
import {
  formatMoney,
  formatReadinessCheckMessage,
  formatTimestamp,
  getAccountingPeriodArtifactFacts,
  getCloseJournalArtifactState,
  getCurrencyCloseState
} from "../../utils/accountingPeriodsWorkspaceModel";

type AccountingPeriodArtifactPanelProps = {
  period: AccountingPeriod;
  isBusy: boolean;
  onViewCloseJournalEntry: (journalEntryId: string) => Promise<void>;
};

export function AccountingPeriodArtifactPanel({
  period,
  isBusy,
  onViewCloseJournalEntry
}: AccountingPeriodArtifactPanelProps) {
  if (!period.closeArtifact) {
    return null;
  }

  return (
    <section className="client-panel accounting-period-close-artifact-panel">
      <div className="client-panel-heading">
        <div>
          <span>{period.name}</span>
          <strong>Close artifact</strong>
        </div>
        <span className="billing-small-fact">
          {formatTimestamp(period.closeArtifact.generatedAtUtc)}
        </span>
      </div>
      <div className="accounting-period-artifact-summary">
        {getAccountingPeriodArtifactFacts(period).map((fact) => (
          <span className={fact.tone} key={fact.label} title={fact.title}>
            <small>{fact.label}</small>
            <strong>{fact.value}</strong>
          </span>
        ))}
      </div>
      <div className="accounting-period-readiness-grid">
        {period.closeArtifact.checks.map((check) => (
          <article
            className={`accounting-period-readiness-check ${check.status.toLowerCase()}`}
            key={`${period.accountingPeriodId}-${check.code}`}
          >
            <span>{check.status}</span>
            <strong>{check.code}</strong>
            <small>{formatReadinessCheckMessage(check)}</small>
          </article>
        ))}
      </div>
      <table className="accounting-period-currency-table">
        <thead>
          <tr>
            <th>Currency</th>
            <th>Close state</th>
            <th>Debit</th>
            <th>Credit</th>
            <th>Difference</th>
            <th>Posted</th>
            <th>Draft</th>
          </tr>
        </thead>
        <tbody>
          {period.closeArtifact.currencies.length === 0 ? (
            <tr>
              <td colSpan={7}>No journal activity</td>
            </tr>
          ) : (
            period.closeArtifact.currencies.map((currency) => {
              const closeState = getCurrencyCloseState(currency);

              return (
                <tr key={`${period.accountingPeriodId}-${currency.currencyCode}`}>
                  <td>
                    <strong>{currency.currencyCode}</strong>
                  </td>
                  <td>
                    <span
                      className={`accounting-period-currency-state ${closeState.tone}`}
                      title={closeState.title}
                    >
                      {closeState.label}
                    </span>
                  </td>
                  <td className="numeric">{formatMoney(currency.totalDebit)}</td>
                  <td className="numeric">{formatMoney(currency.totalCredit)}</td>
                  <td className="numeric">{formatMoney(currency.difference)}</td>
                  <td className="numeric">{currency.postedJournalCount}</td>
                  <td className="numeric">{currency.draftJournalCount}</td>
                </tr>
              );
            })
          )}
        </tbody>
      </table>
      <table className="accounting-period-currency-table">
        <thead>
          <tr>
            <th>Close journal</th>
            <th>Date</th>
            <th>Currency</th>
            <th>Debit</th>
            <th>Credit</th>
            <th>State</th>
            <th>Memo</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {period.closeArtifact.closeJournalEntries.length === 0 ? (
            <tr>
              <td colSpan={8}>No generated close journals</td>
            </tr>
          ) : (
            period.closeArtifact.closeJournalEntries.map((entry) => {
              const journalState = getCloseJournalArtifactState(entry);

              return (
                <tr key={entry.journalEntryId}>
                  <td>
                    <strong>{entry.sourceReference}</strong>
                  </td>
                  <td>{entry.entryDate}</td>
                  <td>{entry.currencyCode}</td>
                  <td className="numeric">{formatMoney(entry.totalDebit)}</td>
                  <td className="numeric">{formatMoney(entry.totalCredit)}</td>
                  <td>
                    <span
                      className={`accounting-period-journal-state ${journalState.tone}`}
                      title={journalState.title}
                    >
                      {journalState.label}
                    </span>
                  </td>
                  <td>{entry.memo}</td>
                  <td>
                    <button
                      className="table-icon-button"
                      type="button"
                      onClick={() => void onViewCloseJournalEntry(entry.journalEntryId)}
                      disabled={isBusy}
                      title={`Open close journal ${entry.sourceReference}`}
                    >
                      <ListTree size={14} />
                    </button>
                  </td>
                </tr>
              );
            })
          )}
        </tbody>
      </table>
    </section>
  );
}
