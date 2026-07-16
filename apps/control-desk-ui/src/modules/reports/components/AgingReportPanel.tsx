import type {
  AccountsReceivableAgingClient,
  AccountsReceivableAgingFilters,
  AccountsReceivableAgingReport
} from "../types/reportTypes";
import { CurrencyField, ReportFilterBar } from "./ReportFilterBar";
import {
  MoneyCell,
  PrintMetadata,
  ReportContentState,
  ReportHeading
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

export function AgingReportPanel({
  report,
  filters,
  isBusy,
  error,
  onFiltersChange,
  onRefresh
}: {
  report: AccountsReceivableAgingReport | null;
  filters: AccountsReceivableAgingFilters;
  isBusy: boolean;
  error?: string;
  onFiltersChange: (filters: AccountsReceivableAgingFilters) => void;
  onRefresh: () => Promise<void>;
}) {
  const asOfDate = report?.asOfDate ?? filters.asOfDate;
  const totals = report?.currencies[0];

  function exportCsv(): void {
    if (report === null) {
      return;
    }

    downloadReportCsv<AccountsReceivableAgingClient>(
      createReportFileName("accounts-receivable-aging", report.asOfDate),
      [
        { header: "Client code", value: (row) => row.clientCode },
        { header: "Client", value: (row) => row.clientName },
        { header: "Currency", value: (row) => row.currencyCode },
        { header: "Current", value: (row) => row.currentAmount },
        { header: "1-30 days", value: (row) => row.days1To30Amount },
        { header: "31-60 days", value: (row) => row.days31To60Amount },
        { header: "61-90 days", value: (row) => row.days61To90Amount },
        { header: "Over 90 days", value: (row) => row.daysOver90Amount },
        { header: "Total outstanding", value: (row) => row.totalOutstanding },
        { header: "Invoice count", value: (row) => row.invoiceCount }
      ],
      report.clients
    );
  }

  return (
    <section className="report-view">
      <ReportFilterBar isBusy={isBusy} onSubmit={onRefresh}>
        <label className="form-field report-readonly-field">
          <span>As of date</span>
          <input type="date" disabled value={asOfDate} />
          <small>Current operational balances as of today</small>
        </label>
        <CurrencyField
          value={filters.currencyCode}
          onChange={(currencyCode) => onFiltersChange({ ...filters, currencyCode })}
        />
      </ReportFilterBar>

      <article className="client-panel report-print-surface">
        <ReportHeading
          kicker={`As of ${formatReportDate(asOfDate)}`}
          title="Accounts receivable aging"
          rowLabel={report === null ? "No rows" : `${report.clients.length} clients`}
          onExport={exportCsv}
          onPrint={() => printReport("Accounts Receivable Aging")}
          disabled={report === null || report.clients.length === 0}
        />
        <PrintMetadata
          detail={`Current operational aging as of ${formatReportDate(asOfDate)} · Currency ${filters.currencyCode}`}
        />

        <ReportContentState
          isBusy={isBusy}
          error={error}
          hasData={report !== null}
          isEmpty={report !== null && report.clients.length === 0}
          emptyMessage="No outstanding receivables match this aging view."
        >
          {report !== null && (
            <>
              <div className="report-currency-summary">
                {report.currencies.map((currency) => (
                  <div key={currency.currencyCode}>
                    <span>{currency.currencyCode}</span>
                    <strong>
                      {formatReportMoney(currency.totalOutstanding, currency.currencyCode)}
                    </strong>
                    <small>
                      {formatReportInteger(currency.clientCount)} clients ·{" "}
                      {formatReportInteger(currency.invoiceCount)} invoices
                    </small>
                  </div>
                ))}
              </div>
              <div className="report-table-frame">
                <table className="report-table">
                  <thead>
                    <tr>
                      <th scope="col">Client</th>
                      <th scope="col" className="numeric">Current</th>
                      <th scope="col" className="numeric">1–30</th>
                      <th scope="col" className="numeric">31–60</th>
                      <th scope="col" className="numeric">61–90</th>
                      <th scope="col" className="numeric">91+</th>
                      <th scope="col" className="numeric">Total</th>
                      <th scope="col" className="numeric">Invoices</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.clients.map((row) => (
                      <tr key={`${row.clientId}-${row.currencyCode}`}>
                        <th scope="row">
                          <strong>{row.clientName}</strong>
                          <small>{row.clientCode} · {row.currencyCode}</small>
                        </th>
                        <MoneyCell value={row.currentAmount} currencyCode={row.currencyCode} />
                        <MoneyCell value={row.days1To30Amount} currencyCode={row.currencyCode} />
                        <MoneyCell value={row.days31To60Amount} currencyCode={row.currencyCode} />
                        <MoneyCell value={row.days61To90Amount} currencyCode={row.currencyCode} />
                        <MoneyCell value={row.daysOver90Amount} currencyCode={row.currencyCode} />
                        <MoneyCell value={row.totalOutstanding} currencyCode={row.currencyCode} strong />
                        <td className="numeric">{formatReportInteger(row.invoiceCount)}</td>
                      </tr>
                    ))}
                  </tbody>
                  {totals !== undefined && (
                    <tfoot>
                      <tr>
                        <th scope="row">
                          <strong>Totals</strong>
                          <small>{totals.currencyCode}</small>
                        </th>
                        <MoneyCell value={totals.currentAmount} currencyCode={totals.currencyCode} strong />
                        <MoneyCell value={totals.days1To30Amount} currencyCode={totals.currencyCode} strong />
                        <MoneyCell value={totals.days31To60Amount} currencyCode={totals.currencyCode} strong />
                        <MoneyCell value={totals.days61To90Amount} currencyCode={totals.currencyCode} strong />
                        <MoneyCell value={totals.daysOver90Amount} currencyCode={totals.currencyCode} strong />
                        <MoneyCell value={totals.totalOutstanding} currencyCode={totals.currencyCode} strong />
                        <td className="numeric"><strong>{formatReportInteger(totals.invoiceCount)}</strong></td>
                      </tr>
                    </tfoot>
                  )}
                </table>
              </div>
            </>
          )}
        </ReportContentState>
      </article>
    </section>
  );
}
