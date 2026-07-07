import { BookOpen, Landmark, RefreshCw } from "lucide-react";
import type {
  BalanceSheet,
  BalanceSheetFilters,
  BalanceSheetLine,
  LedgerAccountSummary
} from "../types/accountingTypes";

type BalanceSheetStatusItem = {
  label: string;
  value: string;
  tone: "ready" | "warning";
};

type BalanceSheetPanelProps = {
  balanceSheet: BalanceSheet | null;
  accounts: LedgerAccountSummary[];
  filters: BalanceSheetFilters;
  isBusy: boolean;
  onFiltersChange: (value: BalanceSheetFilters) => void;
  onViewAccountActivity: (line: BalanceSheetLine) => Promise<void>;
  onRefresh: () => Promise<void>;
};

export function BalanceSheetPanel({
  balanceSheet,
  accounts,
  filters,
  isBusy,
  onFiltersChange,
  onViewAccountActivity,
  onRefresh
}: BalanceSheetPanelProps) {
  const difference = balanceSheet?.difference ?? 0;
  const isBalanced = isZeroAmount(difference);
  const accountsById = new Map(accounts.map((account) => [account.ledgerAccountId, account]));
  const statusItems = getBalanceSheetStatusItems(balanceSheet, filters);

  return (
    <section className="balance-sheet-workbench">
      <header className="client-panel balance-sheet-header">
        <div>
          <span>{filters.currencyCode.trim() === "" ? "PKR" : filters.currencyCode}</span>
          <h2>Balance Sheet</h2>
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
        <article className={`client-panel balance-sheet-summary-card${isBalanced ? " balanced" : " out"}`}>
          <span>Difference</span>
          <strong>{formatMoney(difference)}</strong>
        </article>
      </div>

      <div className="client-panel balance-sheet-readiness-row" aria-label="Balance sheet status">
        {statusItems.map((item) => (
          <span className={item.tone} key={item.label}>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
          </span>
        ))}
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
                      const account = line.ledgerAccountId === null || line.ledgerAccountId === undefined
                        ? undefined
                        : accountsById.get(line.ledgerAccountId);
                      const canViewActivity = (
                        !line.isSystemLine
                        && line.ledgerAccountId !== null
                        && line.ledgerAccountId !== undefined
                        && line.activityCount > 0
                      );

                      return (
                        <tr key={line.ledgerAccountId ?? line.code}>
                          <td>
                            <div className="financial-report-account-ref">
                              <strong>{account?.displayCode ?? line.code}</strong>
                              <span>{line.name}</span>
                              <div className="financial-report-account-tags">
                                {line.isSystemLine ? <small>System</small> : null}
                                {formatRangeLabel(account) !== "" ? <small>{formatRangeLabel(account)}</small> : null}
                              </div>
                            </div>
                          </td>
                          <td>
                            <div className="financial-report-account-class">
                              <strong>{line.type}</strong>
                              <span>{formatNormalBalance(line.normalBalance)}</span>
                              <small>{formatAccountRole(account, line)}</small>
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
                              disabled={isBusy || !canViewActivity}
                              title={canViewActivity ? `View ${line.code} activity` : "No drill-down activity"}
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

function getBalanceSheetStatusItems(
  balanceSheet: BalanceSheet | null,
  filters: BalanceSheetFilters
): BalanceSheetStatusItem[] {
  const lines = balanceSheet?.sections.flatMap((section) => section.lines) ?? [];
  const activeRows = lines.filter((line) => line.activityCount > 0 || !isZeroAmount(line.amount)).length;
  const difference = balanceSheet?.difference ?? 0;
  const currencyCode = balanceSheet?.currencyCode.trim() || filters.currencyCode.trim() || "PKR";

  return [
    {
      label: "As of",
      value: balanceSheet?.asOfDate ?? filters.asOfDate,
      tone: filters.asOfDate.trim() === "" ? "warning" : "ready"
    },
    {
      label: "Rows",
      value: lines.length === 0 ? "No lines" : `${activeRows}/${lines.length} active`,
      tone: lines.length === 0 ? "warning" : "ready"
    },
    {
      label: "Equation",
      value: isZeroAmount(difference) ? "Balanced" : `Out ${formatMoney(difference)}`,
      tone: isZeroAmount(difference) ? "ready" : "warning"
    },
    {
      label: "L + E",
      value: formatMoney(balanceSheet?.totalLiabilitiesAndEquity ?? 0),
      tone: "ready"
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

function formatAccountRole(account: LedgerAccountSummary | undefined, line: BalanceSheetLine): string {
  if (line.isSystemLine) {
    return "System line";
  }

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
