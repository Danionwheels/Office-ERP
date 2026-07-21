import { BookOpenText } from "lucide-react";
import type {
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  TrialBalanceFilters,
  TrialBalanceLine,
  TrialBalanceReport
} from "../types/reportTypes";
import { CurrencyField, ReportFilterBar } from "./ReportFilterBar";
import {
  humanizeToken,
  MoneyCell,
  PrintMetadata,
  ReportActions,
  ReportContentState,
  ReportHeading,
  ReportSummary,
  StatusPill
} from "./ReportPanelChrome";
import {
  formatReportDate,
  formatReportInteger,
  formatReportMoney
} from "../utils/reportFormatting";
import {
  createReportFileName,
  downloadReportCsv,
  printReport
} from "../utils/reportExports";

export function TrialBalanceReportPanel({
  report,
  ledgerActivity,
  selectedLedgerAccountId,
  filters,
  isBusy,
  isLedgerBusy,
  error,
  ledgerError,
  onFiltersChange,
  onRefresh,
  onSelectLedgerAccount
}: {
  report: TrialBalanceReport | null;
  ledgerActivity: LedgerAccountActivity | null;
  selectedLedgerAccountId: string;
  filters: TrialBalanceFilters;
  isBusy: boolean;
  isLedgerBusy: boolean;
  error?: string;
  ledgerError?: string;
  onFiltersChange: (filters: TrialBalanceFilters) => void;
  onRefresh: () => Promise<void>;
  onSelectLedgerAccount: (ledgerAccountId: string) => Promise<void>;
}) {
  function exportTrialBalanceCsv(): void {
    if (report === null) {
      return;
    }

    downloadReportCsv<TrialBalanceLine>(
      createReportFileName("trial-balance", report.asOfDate),
      trialBalanceCsvColumns,
      report.lines
    );
  }

  function exportLedgerCsv(): void {
    if (ledgerActivity === null) {
      return;
    }

    downloadReportCsv<LedgerAccountActivityLine>(
      createReportFileName(`ledger-${ledgerActivity.code}`, filters.asOfDate),
      ledgerActivityCsvColumns,
      ledgerActivity.lines
    );
  }

  return (
    <section className="report-view">
      <ReportFilterBar isBusy={isBusy} onSubmit={onRefresh}>
        <label className="form-field">
          <span>From</span>
          <input
            type="date"
            required
            max={filters.asOfDate}
            value={filters.fromDate}
            onChange={(event) =>
              onFiltersChange({ ...filters, fromDate: event.target.value })
            }
          />
        </label>
        <label className="form-field">
          <span>As of</span>
          <input
            type="date"
            required
            min={filters.fromDate}
            value={filters.asOfDate}
            onChange={(event) =>
              onFiltersChange({ ...filters, asOfDate: event.target.value })
            }
          />
        </label>
        <CurrencyField
          value={filters.currencyCode}
          onChange={(currencyCode) => onFiltersChange({ ...filters, currencyCode })}
        />
      </ReportFilterBar>

      <article className="client-panel report-print-surface">
        <ReportHeading
          kicker="General ledger control"
          title="Trial balance & ledger activity"
          rowLabel={report === null ? "No accounts" : `${report.lines.length} accounts`}
          onExport={exportTrialBalanceCsv}
          onPrint={() => printReport("Trial Balance and Ledger Activity")}
          disabled={report === null || report.lines.length === 0}
        />
        <PrintMetadata
          detail={`${formatReportDate(filters.fromDate)} to ${formatReportDate(filters.asOfDate)} · ${filters.currencyCode}`}
        />

        <ReportContentState
          isBusy={isBusy}
          error={error}
          hasData={report !== null}
          isEmpty={report !== null && report.lines.length === 0}
          emptyMessage="No ledger balances match the selected reporting window."
        >
          {report !== null && (
            <>
              <ReportSummary
                items={[
                  {
                    label: "Debit balance",
                    value: formatReportMoney(report.totalDebit, report.currencyCode)
                  },
                  {
                    label: "Credit balance",
                    value: formatReportMoney(report.totalCredit, report.currencyCode)
                  },
                  {
                    label: "Period debit",
                    value: formatReportMoney(report.totalPeriodDebit, report.currencyCode)
                  },
                  {
                    label: "Difference",
                    value: formatReportMoney(report.difference, report.currencyCode),
                    tone: Math.abs(report.difference) < 0.005 ? "ready" : "warning"
                  }
                ]}
              />
              <div className="report-table-frame">
                <table className="report-table report-wide-table">
                  <thead>
                    <tr>
                      <th scope="col">Account</th>
                      <th scope="col">Type</th>
                      <th scope="col" className="numeric">Opening</th>
                      <th scope="col" className="numeric">Period Dr</th>
                      <th scope="col" className="numeric">Period Cr</th>
                      <th scope="col" className="numeric">Debit</th>
                      <th scope="col" className="numeric">Credit</th>
                      <th scope="col" className="numeric">Net</th>
                      <th scope="col" className="report-no-print">Activity</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.lines.map((line) => (
                      <tr key={line.ledgerAccountId}>
                        <th scope="row">
                          <strong>{line.code}</strong>
                          <small>{line.name}</small>
                        </th>
                        <td>{line.type} · {line.normalBalance}</td>
                        <MoneyCell value={line.openingBalance} currencyCode={report.currencyCode} />
                        <MoneyCell value={line.periodDebit} currencyCode={report.currencyCode} />
                        <MoneyCell value={line.periodCredit} currencyCode={report.currencyCode} />
                        <MoneyCell value={line.debitBalance} currencyCode={report.currencyCode} />
                        <MoneyCell value={line.creditBalance} currencyCode={report.currencyCode} />
                        <MoneyCell value={line.netBalance} currencyCode={report.currencyCode} strong />
                        <td className="report-no-print">
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => void onSelectLedgerAccount(line.ledgerAccountId)}
                            title={`View ${line.code} ledger activity`}
                            aria-label={`View ${line.code} ${line.name} ledger activity`}
                          >
                            <BookOpenText size={14} />
                            <span>{formatReportInteger(line.activityCount)}</span>
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <section className="ledger-report-section">
                <div className="ledger-report-selector report-no-print">
                  <label className="form-field">
                    <span>Account activity</span>
                    <select
                      value={selectedLedgerAccountId}
                      onChange={(event) => void onSelectLedgerAccount(event.target.value)}
                    >
                      <option value="">Select an account</option>
                      {report.lines.map((line) => (
                        <option key={line.ledgerAccountId} value={line.ledgerAccountId}>
                          {line.code} — {line.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <span>
                    Select an account to inspect journal debits, credits, and its
                    running balance.
                  </span>
                </div>

                {isLedgerBusy && ledgerActivity === null && (
                  <div className="report-feedback">Loading ledger activity…</div>
                )}
                {ledgerError && (
                  <div className="report-feedback warning" role="alert">{ledgerError}</div>
                )}
                {ledgerActivity !== null && (
                  <>
                    <div className="ledger-report-heading">
                      <div>
                        <span>Account ledger</span>
                        <strong>{ledgerActivity.code} · {ledgerActivity.name}</strong>
                      </div>
                      <ReportActions
                        onExport={exportLedgerCsv}
                        onPrint={() => printReport("Trial Balance and Ledger Activity")}
                        disabled={ledgerActivity.lines.length === 0}
                      />
                    </div>
                    <ReportSummary
                      items={[
                        {
                          label: "Opening",
                          value: formatReportMoney(
                            ledgerActivity.openingBalance,
                            ledgerActivity.currencyCode ?? filters.currencyCode
                          )
                        },
                        {
                          label: "Debit",
                          value: formatReportMoney(
                            ledgerActivity.periodDebit,
                            ledgerActivity.currencyCode ?? filters.currencyCode
                          )
                        },
                        {
                          label: "Credit",
                          value: formatReportMoney(
                            ledgerActivity.periodCredit,
                            ledgerActivity.currencyCode ?? filters.currencyCode
                          )
                        },
                        {
                          label: "Ending",
                          value: formatReportMoney(
                            ledgerActivity.endingBalance,
                            ledgerActivity.currencyCode ?? filters.currencyCode
                          )
                        }
                      ]}
                    />
                    <div className="report-table-frame">
                      <table className="report-table report-wide-table">
                        <thead>
                          <tr>
                            <th scope="col">Date</th>
                            <th scope="col">Source</th>
                            <th scope="col">Reference</th>
                            <th scope="col">Description</th>
                            <th scope="col">Status</th>
                            <th scope="col" className="numeric">Debit</th>
                            <th scope="col" className="numeric">Credit</th>
                            <th scope="col" className="numeric">Running balance</th>
                          </tr>
                        </thead>
                        <tbody>
                          {ledgerActivity.lines.map((line, index) => (
                            <tr key={`${line.journalEntryId}-${index}`}>
                              <td>{formatReportDate(line.entryDate)}</td>
                              <td>{humanizeToken(line.sourceType)}</td>
                              <th scope="row">
                                {line.sourceReference || shortId(line.journalEntryId)}
                              </th>
                              <td>{line.description || line.memo || "—"}</td>
                              <td><StatusPill value={line.status} /></td>
                              <MoneyCell value={line.debit} currencyCode={line.currencyCode} />
                              <MoneyCell value={line.credit} currencyCode={line.currencyCode} />
                              <MoneyCell value={line.runningBalance} currencyCode={line.currencyCode} strong />
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    {ledgerActivity.lines.length === 0 && (
                      <div className="report-empty-state">
                        This account has no journal activity in the selected window.
                      </div>
                    )}
                  </>
                )}
              </section>
            </>
          )}
        </ReportContentState>
      </article>
    </section>
  );
}

const trialBalanceCsvColumns = [
  { header: "Account code", value: (row: TrialBalanceLine) => row.code },
  { header: "Account name", value: (row: TrialBalanceLine) => row.name },
  { header: "Type", value: (row: TrialBalanceLine) => row.type },
  { header: "Normal balance", value: (row: TrialBalanceLine) => row.normalBalance },
  { header: "Opening balance", value: (row: TrialBalanceLine) => row.openingBalance },
  { header: "Period debit", value: (row: TrialBalanceLine) => row.periodDebit },
  { header: "Period credit", value: (row: TrialBalanceLine) => row.periodCredit },
  { header: "Debit balance", value: (row: TrialBalanceLine) => row.debitBalance },
  { header: "Credit balance", value: (row: TrialBalanceLine) => row.creditBalance },
  { header: "Net balance", value: (row: TrialBalanceLine) => row.netBalance },
  { header: "Activity count", value: (row: TrialBalanceLine) => row.activityCount }
];

const ledgerActivityCsvColumns = [
  { header: "Entry date", value: (row: LedgerAccountActivityLine) => row.entryDate },
  { header: "Source", value: (row: LedgerAccountActivityLine) => row.sourceType },
  { header: "Reference", value: (row: LedgerAccountActivityLine) => row.sourceReference },
  {
    header: "Description",
    value: (row: LedgerAccountActivityLine) => row.description ?? row.memo
  },
  { header: "Status", value: (row: LedgerAccountActivityLine) => row.status },
  { header: "Debit", value: (row: LedgerAccountActivityLine) => row.debit },
  { header: "Credit", value: (row: LedgerAccountActivityLine) => row.credit },
  {
    header: "Running balance",
    value: (row: LedgerAccountActivityLine) => row.runningBalance
  },
  { header: "Currency", value: (row: LedgerAccountActivityLine) => row.currencyCode }
];

function shortId(value: string): string {
  return value.length <= 8 ? value : value.slice(0, 8);
}
