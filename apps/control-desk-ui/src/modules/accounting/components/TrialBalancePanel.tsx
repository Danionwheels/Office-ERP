import { BookOpen, RefreshCw, Scale } from "lucide-react";
import type {
  TrialBalance,
  TrialBalanceFilters,
  TrialBalanceLine
} from "../types/accountingTypes";

type TrialBalancePanelProps = {
  balance: TrialBalance | null;
  filters: TrialBalanceFilters;
  isBusy: boolean;
  onFiltersChange: (value: TrialBalanceFilters) => void;
  onViewAccountActivity: (line: TrialBalanceLine) => Promise<void>;
  onRefresh: () => Promise<void>;
};

export function TrialBalancePanel({
  balance,
  filters,
  isBusy,
  onFiltersChange,
  onViewAccountActivity,
  onRefresh
}: TrialBalancePanelProps) {
  const difference = balance?.difference ?? 0;

  return (
    <section className="trial-balance-workbench">
      <header className="client-panel trial-balance-header">
        <div>
          <span>{filters.currencyCode.trim() === "" ? "PKR" : filters.currencyCode}</span>
          <h2>Trial balance</h2>
        </div>
        <button
          className="icon-button"
          type="button"
          onClick={onRefresh}
          disabled={isBusy}
          title="Refresh trial balance"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <div className="client-panel trial-balance-filter-row">
        <label className="form-field">
          <span>From</span>
          <input
            type="date"
            value={filters.fromDate}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                fromDate: event.target.value
              })
            }
            disabled={isBusy}
          />
        </label>
        <label className="form-field">
          <span>As of</span>
          <input
            type="date"
            value={filters.asOfDate}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                asOfDate: event.target.value
              })
            }
            disabled={isBusy}
          />
        </label>
        <label className="form-field">
          <span>Currency</span>
          <input
            value={filters.currencyCode}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                currencyCode: event.target.value.toUpperCase()
              })
            }
            disabled={isBusy}
          />
        </label>
      </div>

      <div className="trial-balance-summary-row">
        <article className="client-panel trial-balance-summary-card">
          <span>Closing debit</span>
          <strong>{formatMoney(balance?.totalDebit ?? 0)}</strong>
        </article>
        <article className="client-panel trial-balance-summary-card">
          <span>Closing credit</span>
          <strong>{formatMoney(balance?.totalCredit ?? 0)}</strong>
        </article>
        <article className="client-panel trial-balance-summary-card">
          <span>Period debit</span>
          <strong>{formatMoney(balance?.totalPeriodDebit ?? 0)}</strong>
        </article>
        <article className="client-panel trial-balance-summary-card">
          <span>Period credit</span>
          <strong>{formatMoney(balance?.totalPeriodCredit ?? 0)}</strong>
        </article>
        <article className={`client-panel trial-balance-summary-card${difference === 0 ? " balanced" : " out"}`}>
          <span>Difference</span>
          <strong>{formatMoney(difference)}</strong>
        </article>
        <article className="client-panel trial-balance-summary-card">
          <span>Accounts</span>
          <strong>{balance?.lines.length ?? 0}</strong>
        </article>
      </div>

      <section className="client-panel trial-balance-table-panel">
        <div className="client-panel-heading">
          <div>
            <span>{formatWindow(balance?.fromDate ?? filters.fromDate, balance?.asOfDate ?? filters.asOfDate)}</span>
            <strong>Account balances</strong>
          </div>
          <Scale size={18} />
        </div>
        <table className="trial-balance-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Type</th>
              <th>Opening</th>
              <th>Debit</th>
              <th>Credit</th>
              <th>Closing Dr</th>
              <th>Closing Cr</th>
              <th>Net</th>
              <th>Activity</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {balance === null || balance.lines.length === 0 ? (
              <tr>
                <td colSpan={11}>No balances</td>
              </tr>
            ) : (
              balance.lines.map((line) => (
                <tr key={line.ledgerAccountId}>
                  <td>
                    <strong>{line.code}</strong>
                  </td>
                  <td>{line.name}</td>
                  <td>{line.type}</td>
                  <td>{formatMoney(line.openingBalance)}</td>
                  <td>{formatMoney(line.periodDebit)}</td>
                  <td>{formatMoney(line.periodCredit)}</td>
                  <td>{formatMoney(line.debitBalance)}</td>
                  <td>{formatMoney(line.creditBalance)}</td>
                  <td>{formatMoney(line.netBalance)}</td>
                  <td>{line.activityCount}</td>
                  <td>
                    <button
                      className="table-icon-button"
                      type="button"
                      onClick={() => void onViewAccountActivity(line)}
                      disabled={isBusy || (line.activityCount === 0 && line.openingBalance === 0)}
                      title="View account activity"
                    >
                      <BookOpen size={14} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </section>
    </section>
  );
}

function formatMoney(value: number): string {
  return value.toFixed(2);
}

function formatWindow(fromDate: string | null | undefined, toDate: string): string {
  return fromDate === null || fromDate === undefined || fromDate.trim() === ""
    ? `Through ${toDate}`
    : `${fromDate} to ${toDate}`;
}
