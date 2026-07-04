import { BookOpen, Landmark, RefreshCw } from "lucide-react";
import type {
  BalanceSheet,
  BalanceSheetFilters,
  BalanceSheetLine
} from "../types/accountingTypes";

type BalanceSheetPanelProps = {
  balanceSheet: BalanceSheet | null;
  filters: BalanceSheetFilters;
  isBusy: boolean;
  onFiltersChange: (value: BalanceSheetFilters) => void;
  onViewAccountActivity: (line: BalanceSheetLine) => Promise<void>;
  onRefresh: () => Promise<void>;
};

export function BalanceSheetPanel({
  balanceSheet,
  filters,
  isBusy,
  onFiltersChange,
  onViewAccountActivity,
  onRefresh
}: BalanceSheetPanelProps) {
  const difference = balanceSheet?.difference ?? 0;

  return (
    <section className="balance-sheet-workbench">
      <header className="client-panel balance-sheet-header">
        <div>
          <span>{filters.currencyCode.trim() === "" ? "PKR" : filters.currencyCode}</span>
          <h2>Balance sheet</h2>
        </div>
        <button
          className="icon-button"
          type="button"
          onClick={onRefresh}
          disabled={isBusy}
          title="Refresh balance sheet"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <div className="client-panel balance-sheet-filter-row">
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

      <div className="balance-sheet-summary-row">
        <article className="client-panel balance-sheet-summary-card">
          <span>Assets</span>
          <strong>{formatMoney(balanceSheet?.totalAssets ?? 0)}</strong>
        </article>
        <article className="client-panel balance-sheet-summary-card">
          <span>Liabilities</span>
          <strong>{formatMoney(balanceSheet?.totalLiabilities ?? 0)}</strong>
        </article>
        <article className="client-panel balance-sheet-summary-card">
          <span>Equity</span>
          <strong>{formatMoney(balanceSheet?.totalEquity ?? 0)}</strong>
        </article>
        <article className="client-panel balance-sheet-summary-card">
          <span>Liabilities + equity</span>
          <strong>{formatMoney(balanceSheet?.totalLiabilitiesAndEquity ?? 0)}</strong>
        </article>
        <article className={`client-panel balance-sheet-summary-card${difference === 0 ? " balanced" : " out"}`}>
          <span>Difference</span>
          <strong>{formatMoney(difference)}</strong>
        </article>
      </div>

      <section className="client-panel balance-sheet-table-panel">
        <div className="client-panel-heading">
          <div>
            <span>As of {balanceSheet?.asOfDate ?? filters.asOfDate}</span>
            <strong>Statement of financial position</strong>
          </div>
          <Landmark size={18} />
        </div>
        {balanceSheet === null || balanceSheet.sections.length === 0 ? (
          <div className="client-empty-state">No balance sheet lines</div>
        ) : (
          balanceSheet.sections.map((section) => (
            <section className="balance-sheet-section" key={section.type}>
              <header>
                <strong>{section.title}</strong>
                <span>{formatMoney(section.total)}</span>
              </header>
              <table className="balance-sheet-table">
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
                      <tr key={line.ledgerAccountId ?? line.code}>
                        <td>
                          <strong>{line.code}</strong>
                        </td>
                        <td>
                          {line.name}
                          {line.isSystemLine ? <span className="balance-sheet-system-line">System</span> : null}
                        </td>
                        <td>{formatMoney(line.debit)}</td>
                        <td>{formatMoney(line.credit)}</td>
                        <td>{formatMoney(line.amount)}</td>
                        <td>{line.activityCount}</td>
                        <td>
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => void onViewAccountActivity(line)}
                            disabled={
                              isBusy
                              || line.isSystemLine
                              || line.ledgerAccountId === null
                              || line.ledgerAccountId === undefined
                              || line.activityCount === 0
                            }
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
