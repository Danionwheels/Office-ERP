import { BookOpen } from "lucide-react";
import type {
  JournalEntrySummary,
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  LedgerAccountSummary
} from "../../types/accountingTypes";
import {
  formatActivityWindow,
  formatLedgerActivitySubtitle,
  formatRunningBalanceTitle,
  getActivityLinePrimaryNarration,
  getActivityLineSecondaryNarration,
  getAmountTone,
  getLedgerActivityStatusItems,
  hasJournalEntry
} from "../../utils/chartOfAccountsWorkspaceModel";
import { formatMoney } from "../../utils/journalModel";

type ChartOfAccountsActivityPanelProps = {
  account?: LedgerAccountSummary;
  activity: LedgerAccountActivity;
  journalEntries: JournalEntrySummary[];
  onViewJournalEntry: (line: LedgerAccountActivityLine) => Promise<void>;
};

export function ChartOfAccountsActivityPanel({
  account,
  activity,
  journalEntries,
  onViewJournalEntry
}: ChartOfAccountsActivityPanelProps) {
  const activityStatusItems = getLedgerActivityStatusItems(activity);

  return (
    <section className="entry-section coa-activity-panel">
      <div className="section-heading-row">
        <div>
          <h2>{account?.displayCode ?? activity.code} / {activity.name}</h2>
          <small>{formatLedgerActivitySubtitle(activity, account)}</small>
        </div>
        <span>{formatActivityWindow(activity)} / {activity.currencyCode ?? "No currency"}</span>
      </div>

      <div className="coa-activity-status-row" aria-label="Ledger activity status">
        {activityStatusItems.map((item) => (
          <span className={item.tone} key={item.label}>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
          </span>
        ))}
      </div>

      <div className="coa-activity-summary">
        <ActivityFact
          label="Opening"
          value={formatMoney(activity.openingBalance)}
          tone={getAmountTone(activity.openingBalance, activity.normalBalance)}
        />
        <ActivityFact label="Debit" value={formatMoney(activity.periodDebit)} tone="debit" />
        <ActivityFact label="Credit" value={formatMoney(activity.periodCredit)} tone="credit" />
        <ActivityFact
          label="Movement"
          value={formatMoney(activity.periodDebit - activity.periodCredit)}
          tone="neutral"
        />
        <ActivityFact
          label="Ending"
          value={formatMoney(activity.endingBalance)}
          tone={getAmountTone(activity.endingBalance, activity.normalBalance)}
        />
        <ActivityFact
          label="Lines"
          value={String(activity.lines.length)}
          tone={activity.lines.length === 0 ? "warning" : "neutral"}
        />
      </div>

      <table className="coa-activity-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Voucher</th>
            <th>Narration</th>
            <th>Status</th>
            <th>Debit</th>
            <th>Credit</th>
            <th>Balance</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {activity.lines.length === 0 ? (
            <tr>
              <td colSpan={8}>No account activity</td>
            </tr>
          ) : (
            activity.lines.map((line, index) => {
              const hasRegisterEntry = hasJournalEntry(line.journalEntryId, journalEntries);
              const primaryNarration = getActivityLinePrimaryNarration(line);
              const secondaryNarration = getActivityLineSecondaryNarration(line);

              return (
                <tr key={`${line.journalEntryId}-${line.entryDate}-${line.runningBalance}-${index}`}>
                  <td>{line.entryDate}</td>
                  <td>
                    <div className="coa-activity-source">
                      <strong>{line.sourceReference ?? line.journalEntryId}</strong>
                      <span>{line.sourceType}</span>
                      <small>{line.currencyCode}</small>
                    </div>
                  </td>
                  <td>
                    <div className="coa-activity-narration">
                      <strong>{primaryNarration}</strong>
                      {secondaryNarration !== "" ? <small>{secondaryNarration}</small> : null}
                    </div>
                  </td>
                  <td>
                    <span className={`status-pill ${line.status.toLowerCase()}`}>
                      {line.status}
                    </span>
                  </td>
                  <td className="numeric">{formatMoney(line.debit)}</td>
                  <td className="numeric">{formatMoney(line.credit)}</td>
                  <td className="numeric">
                    <span
                      className={`coa-activity-balance ${getAmountTone(line.runningBalance, activity.normalBalance)}`}
                      title={formatRunningBalanceTitle(line, activity.normalBalance)}
                    >
                      {formatMoney(line.runningBalance)}
                    </span>
                  </td>
                  <td>
                    <button
                      className={`table-icon-button${hasRegisterEntry ? "" : " muted"}`}
                      type="button"
                      onClick={() => void onViewJournalEntry(line)}
                      title={hasRegisterEntry ? "View journal lines" : "Load journal lines"}
                    >
                      <BookOpen size={14} />
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

function ActivityFact({
  label,
  value,
  tone = "neutral"
}: {
  label: string;
  value: string;
  tone?: "debit" | "credit" | "neutral" | "warning";
}) {
  return (
    <span className={tone}>
      <small>{label}</small>
      <strong>{value}</strong>
    </span>
  );
}
