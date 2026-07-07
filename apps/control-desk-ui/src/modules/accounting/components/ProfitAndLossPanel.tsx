import { BookOpen, RefreshCw, TrendingUp } from "lucide-react";
import type {
  LedgerAccountSummary,
  ProfitAndLossStatement,
  ProfitAndLossStatementFilters,
  ProfitAndLossStatementLine
} from "../types/accountingTypes";

type ProfitAndLossStatusItem = {
  label: string;
  value: string;
  tone: "ready" | "warning";
};

type ProfitAndLossPanelProps = {
  statement: ProfitAndLossStatement | null;
  accounts: LedgerAccountSummary[];
  filters: ProfitAndLossStatementFilters;
  isBusy: boolean;
  onFiltersChange: (value: ProfitAndLossStatementFilters) => void;
  onViewAccountActivity: (line: ProfitAndLossStatementLine) => Promise<void>;
  onRefresh: () => Promise<void>;
};

export function ProfitAndLossPanel({
  statement,
  accounts,
  filters,
  isBusy,
  onFiltersChange,
  onViewAccountActivity,
  onRefresh
}: ProfitAndLossPanelProps) {
  const netIncome = statement?.netIncome ?? 0;
  const accountsById = new Map(accounts.map((account) => [account.ledgerAccountId, account]));
  const statusItems = getProfitAndLossStatusItems(statement, filters);

  return (
    <section className="profit-loss-workbench">
      <header className="client-panel profit-loss-header">
        <div>
          <span>{filters.currencyCode.trim() === "" ? "PKR" : filters.currencyCode}</span>
          <h2>Profit and Loss</h2>
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

      <div className="client-panel profit-loss-readiness-row" aria-label="Profit and loss status">
        {statusItems.map((item) => (
          <span className={item.tone} key={item.label}>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
          </span>
        ))}
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
                    <th>Account</th>
                    <th>Class</th>
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
                    section.lines.map((line) => {
                      const account = accountsById.get(line.ledgerAccountId);

                      return (
                        <tr key={line.ledgerAccountId}>
                          <td>
                            <div className="financial-report-account-ref">
                              <strong>{account?.displayCode ?? line.code}</strong>
                              <span>{line.name}</span>
                              {formatRangeLabel(account) !== "" ? <small>{formatRangeLabel(account)}</small> : null}
                            </div>
                          </td>
                          <td>
                            <div className="financial-report-account-class">
                              <strong>{line.type}</strong>
                              <span>{formatNormalBalance(line.normalBalance)}</span>
                              <small>{formatAccountRole(account)}</small>
                            </div>
                          </td>
                          <td className="numeric">{formatMoney(line.debit)}</td>
                          <td className="numeric">{formatMoney(line.credit)}</td>
                          <td className="numeric">{formatMoney(line.amount)}</td>
                          <td className="numeric">{line.activityCount}</td>
                          <td>
                            <button
                              className="table-icon-button"
                              type="button"
                              onClick={() => void onViewAccountActivity(line)}
                              disabled={isBusy || line.activityCount === 0}
                              title={line.activityCount === 0 ? "No posted activity" : `View ${line.code} activity`}
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
          ))
        )}
      </section>
    </section>
  );
}

function formatMoney(value: number): string {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

function formatWindow(fromDate: string | null | undefined, toDate: string): string {
  return fromDate === null || fromDate === undefined || fromDate.trim() === ""
    ? `Through ${toDate}`
    : `${fromDate} to ${toDate}`;
}

function getProfitAndLossStatusItems(
  statement: ProfitAndLossStatement | null,
  filters: ProfitAndLossStatementFilters
): ProfitAndLossStatusItem[] {
  const lines = statement?.sections.flatMap((section) => section.lines) ?? [];
  const activeRows = lines.filter((line) => line.activityCount > 0 || !isZeroAmount(line.amount)).length;
  const currencyCode = statement?.currencyCode.trim() || filters.currencyCode.trim() || "PKR";
  const netIncome = statement?.netIncome ?? 0;

  return [
    {
      label: "Period",
      value: formatWindow(statement?.fromDate ?? filters.fromDate, statement?.toDate ?? filters.toDate),
      tone: filters.toDate.trim() === "" ? "warning" : "ready"
    },
    {
      label: "Rows",
      value: lines.length === 0 ? "No lines" : `${activeRows}/${lines.length} active`,
      tone: lines.length === 0 ? "warning" : "ready"
    },
    {
      label: "Revenue",
      value: formatMoney(statement?.totalRevenue ?? 0),
      tone: "ready"
    },
    {
      label: "Expense",
      value: formatMoney(statement?.totalExpense ?? 0),
      tone: "ready"
    },
    {
      label: "Result",
      value: netIncome >= 0 ? "Income" : "Loss",
      tone: netIncome >= 0 ? "ready" : "warning"
    },
    {
      label: "Currency",
      value: currencyCode,
      tone: "ready"
    }
  ];
}

function formatNormalBalance(normalBalance: string): string {
  const normalized = normalBalance.trim().toLowerCase();

  if (normalized.includes("debit")) {
    return "Dr normal";
  }

  if (normalized.includes("credit")) {
    return "Cr normal";
  }

  return normalBalance.trim() === "" ? "Normal n/a" : `${normalBalance} normal`;
}

function formatAccountRole(account: LedgerAccountSummary | undefined): string {
  if (account === undefined) {
    return "Ledger account";
  }

  const level = account.level?.trim();
  const role = account.isPostingAccount ? "Posting" : "Control";

  return level === undefined || level === "" ? `${role} account` : `${level} / ${role}`;
}

function formatRangeLabel(account: LedgerAccountSummary | undefined): string {
  if (account === undefined) {
    return "";
  }

  return account.rangeDisplayName?.trim() || account.rangeRole?.trim() || "";
}

function isZeroAmount(value: number): boolean {
  return Math.abs(value) < 0.005;
}
