import { BookOpen, RefreshCw, TrendingUp } from "lucide-react";
import type {
  ProfitAndLossStatement,
  ProfitAndLossStatementFilters,
  ProfitAndLossStatementLine
} from "../types/accountingTypes";

type ProfitAndLossPanelProps = {
  statement: ProfitAndLossStatement | null;
  filters: ProfitAndLossStatementFilters;
  isBusy: boolean;
  onFiltersChange: (value: ProfitAndLossStatementFilters) => void;
  onViewAccountActivity: (line: ProfitAndLossStatementLine) => Promise<void>;
  onRefresh: () => Promise<void>;
};

export function ProfitAndLossPanel({
  statement,
  filters,
  isBusy,
  onFiltersChange,
  onViewAccountActivity,
  onRefresh
}: ProfitAndLossPanelProps) {
  const netIncome = statement?.netIncome ?? 0;

  return (
    <section className="profit-loss-workbench">
      <header className="client-panel profit-loss-header">
        <div>
          <span>{filters.currencyCode.trim() === "" ? "PKR" : filters.currencyCode}</span>
          <h2>Profit and loss</h2>
        </div>
        <button
          className="icon-button"
          type="button"
          onClick={onRefresh}
          disabled={isBusy}
          title="Refresh profit and loss"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <div className="client-panel profit-loss-filter-row">
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
          <span>To</span>
          <input
            type="date"
            value={filters.toDate}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                toDate: event.target.value
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

      <div className="profit-loss-summary-row">
        <article className="client-panel profit-loss-summary-card">
          <span>Revenue</span>
          <strong>{formatMoney(statement?.totalRevenue ?? 0)}</strong>
        </article>
        <article className="client-panel profit-loss-summary-card">
          <span>Expense</span>
          <strong>{formatMoney(statement?.totalExpense ?? 0)}</strong>
        </article>
        <article className={`client-panel profit-loss-summary-card${netIncome >= 0 ? " income" : " loss"}`}>
          <span>Net income</span>
          <strong>{formatMoney(netIncome)}</strong>
        </article>
      </div>

      <section className="client-panel profit-loss-table-panel">
        <div className="client-panel-heading">
          <div>
            <span>{formatWindow(statement?.fromDate ?? filters.fromDate, statement?.toDate ?? filters.toDate)}</span>
            <strong>Statement lines</strong>
          </div>
          <TrendingUp size={18} />
        </div>
        {statement === null || statement.sections.length === 0 ? (
          <div className="client-empty-state">No profit and loss lines</div>
        ) : (
          statement.sections.map((section) => (
            <section className="profit-loss-section" key={section.type}>
              <header>
                <strong>{section.title}</strong>
                <span>{formatMoney(section.total)}</span>
              </header>
              <table className="profit-loss-table">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Name</th>
                    <th>Debit</th>
                    <th>Credit</th>
                    <th>Amount</th>
                    <th>Activity</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {section.lines.length === 0 ? (
                    <tr>
                      <td colSpan={7}>No lines</td>
                    </tr>
                  ) : (
                    section.lines.map((line) => (
                      <tr key={line.ledgerAccountId}>
                        <td>
                          <strong>{line.code}</strong>
                        </td>
                        <td>{line.name}</td>
                        <td>{formatMoney(line.debit)}</td>
                        <td>{formatMoney(line.credit)}</td>
                        <td>{formatMoney(line.amount)}</td>
                        <td>{line.activityCount}</td>
                        <td>
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => void onViewAccountActivity(line)}
                            disabled={isBusy || line.activityCount === 0}
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
          ))
        )}
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
