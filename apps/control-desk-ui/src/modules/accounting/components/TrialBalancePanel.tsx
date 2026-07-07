import { BookOpen, RefreshCw, Scale } from "lucide-react";
import type {
  LedgerAccountSummary,
  TrialBalance,
  TrialBalanceFilters,
  TrialBalanceLine
} from "../types/accountingTypes";

type TrialBalanceStatusItem = {
  label: string;
  value: string;
  tone: "ready" | "warning";
  title?: string;
};

type TrialBalancePanelProps = {
  balance: TrialBalance | null;
  accounts: LedgerAccountSummary[];
  filters: TrialBalanceFilters;
  isBusy: boolean;
  onFiltersChange: (value: TrialBalanceFilters) => void;
  onViewAccountActivity: (line: TrialBalanceLine) => Promise<void>;
  onRefresh: () => Promise<void>;
};

export function TrialBalancePanel({
  balance,
  accounts,
  filters,
  isBusy,
  onFiltersChange,
  onViewAccountActivity,
  onRefresh
}: TrialBalancePanelProps) {
  const difference = balance?.difference ?? 0;
  const isBalanced = isZeroAmount(difference);
  const accountsById = new Map(accounts.map((account) => [account.ledgerAccountId, account]));
  const statusItems = getTrialBalanceStatusItems(balance, filters);

  return (
    <section className="trial-balance-workbench">
      <header className="client-panel trial-balance-header">
        <div>
          <span>{filters.currencyCode.trim() === "" ? "PKR" : filters.currencyCode}</span>
          <h2>Trial Balance</h2>
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
        <article className={`client-panel trial-balance-summary-card${isBalanced ? " balanced" : " out"}`}>
          <span>Difference</span>
          <strong>{formatMoney(difference)}</strong>
        </article>
        <article className="client-panel trial-balance-summary-card">
          <span>Accounts</span>
          <strong>{balance?.lines.length ?? 0}</strong>
        </article>
      </div>

      <div className="client-panel trial-balance-readiness-row" aria-label="Trial balance status">
        {statusItems.map((item) => (
          <span className={item.tone} key={item.label} title={item.title}>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
          </span>
        ))}
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
              <th>Account</th>
              <th>Class</th>
              <th>Opening</th>
              <th>Debit</th>
              <th>Credit</th>
              <th>Closing Dr</th>
              <th>Closing Cr</th>
              <th>Net</th>
              <th>State</th>
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
              balance.lines.map((line) => {
                const account = accountsById.get(line.ledgerAccountId);
                const side = getTrialBalanceSide(line);
                const sideTone = getTrialBalanceSideTone(line);
                const isAbnormal = isTrialBalanceAbnormal(line);
                const canViewActivity = line.activityCount > 0 || !isZeroAmount(line.openingBalance);

                return (
                  <tr className={isAbnormal ? "trial-balance-row abnormal" : "trial-balance-row"} key={line.ledgerAccountId}>
                    <td>
                      <div className="trial-balance-account-ref">
                        <strong>{account?.displayCode ?? line.code}</strong>
                        <span>{line.name}</span>
                        {formatRangeLabel(account) !== "" ? <small>{formatRangeLabel(account)}</small> : null}
                      </div>
                    </td>
                    <td>
                      <div className="trial-balance-account-class">
                        <strong>{line.type}</strong>
                        <span>{formatNormalBalance(line.normalBalance)}</span>
                        <small>{formatAccountRole(account)}</small>
                      </div>
                    </td>
                    <td className="numeric">{formatMoney(line.openingBalance)}</td>
                    <td className="numeric">{formatMoney(line.periodDebit)}</td>
                    <td className="numeric">{formatMoney(line.periodCredit)}</td>
                    <td className="numeric">{formatMoney(line.debitBalance)}</td>
                    <td className="numeric">{formatMoney(line.creditBalance)}</td>
                    <td className="numeric">{formatMoney(line.netBalance)}</td>
                    <td>
                      <span
                        className={`trial-balance-side ${sideTone}${isAbnormal ? " abnormal" : ""}`}
                        title={getTrialBalanceSideTitle(line, isAbnormal)}
                      >
                        {isAbnormal ? `${side} *` : side}
                      </span>
                    </td>
                    <td className="numeric">{line.activityCount}</td>
                    <td>
                      <button
                        className="table-icon-button"
                        type="button"
                        onClick={() => void onViewAccountActivity(line)}
                        disabled={isBusy || !canViewActivity}
                        title={canViewActivity ? `View ${line.code} activity` : "No opening balance or posted activity"}
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

function getTrialBalanceStatusItems(
  balance: TrialBalance | null,
  filters: TrialBalanceFilters
): TrialBalanceStatusItem[] {
  const lines = balance?.lines ?? [];
  const activeRows = lines.filter(hasTrialBalanceActivity).length;
  const abnormalRows = lines.filter(isTrialBalanceAbnormal).length;
  const difference = balance?.difference ?? 0;
  const currencyCode = balance?.currencyCode.trim() || filters.currencyCode.trim() || "PKR";

  return [
    {
      label: "Period",
      value: formatWindow(balance?.fromDate ?? filters.fromDate, balance?.asOfDate ?? filters.asOfDate),
      tone: filters.asOfDate.trim() === "" ? "warning" : "ready"
    },
    {
      label: "Rows",
      value: lines.length === 0 ? "No balances" : `${activeRows}/${lines.length} active`,
      tone: lines.length === 0 ? "warning" : "ready"
    },
    {
      label: "Dr/Cr",
      value: isZeroAmount(difference) ? "Balanced" : `Out ${formatMoney(difference)}`,
      tone: isZeroAmount(difference) ? "ready" : "warning"
    },
    {
      label: "Review",
      value: abnormalRows === 0 ? "Normal" : `${abnormalRows} unusual`,
      tone: abnormalRows === 0 ? "ready" : "warning",
      title: "Rows marked unusual are closing on the opposite side of their normal balance."
    },
    {
      label: "Currency",
      value: currencyCode,
      tone: "ready"
    }
  ];
}

function hasTrialBalanceActivity(line: TrialBalanceLine): boolean {
  return (
    line.activityCount > 0
    || !isZeroAmount(line.openingBalance)
    || !isZeroAmount(line.periodDebit)
    || !isZeroAmount(line.periodCredit)
    || !isZeroAmount(line.debitBalance)
    || !isZeroAmount(line.creditBalance)
    || !isZeroAmount(line.netBalance)
  );
}

function getTrialBalanceSide(line: TrialBalanceLine): string {
  const hasDebit = !isZeroAmount(line.debitBalance);
  const hasCredit = !isZeroAmount(line.creditBalance);

  if (hasDebit && hasCredit) {
    return "Split";
  }

  if (hasDebit) {
    return "Debit";
  }

  if (hasCredit) {
    return "Credit";
  }

  return "Zero";
}

function getTrialBalanceSideTone(line: TrialBalanceLine): string {
  return getTrialBalanceSide(line).toLowerCase();
}

function getTrialBalanceSideTitle(line: TrialBalanceLine, isAbnormal: boolean): string {
  const side = getTrialBalanceSide(line);
  const normalBalance = formatNormalBalance(line.normalBalance);

  return isAbnormal
    ? `${side} closing balance; expected ${normalBalance}.`
    : `${side} closing balance; ${normalBalance}.`;
}

function isTrialBalanceAbnormal(line: TrialBalanceLine): boolean {
  const normalBalance = line.normalBalance.trim().toLowerCase();

  if (normalBalance.includes("debit")) {
    return !isZeroAmount(line.creditBalance);
  }

  if (normalBalance.includes("credit")) {
    return !isZeroAmount(line.debitBalance);
  }

  return false;
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
    return "Posting account";
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
