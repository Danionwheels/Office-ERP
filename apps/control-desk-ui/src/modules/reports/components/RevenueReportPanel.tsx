import { useState } from "react";
import type {
  RevenueSummaryFilters,
  RevenueSummaryPeriod,
  RevenueSummaryReport
} from "../types/reportTypes";
import {
  CurrencyField,
  DateRangeFields,
  ReportFilterBar
} from "./ReportFilterBar";
import {
  MoneyCell,
  PrintMetadata,
  ReportContentState,
  ReportHeading,
  ReportSummary
} from "./ReportPanelChrome";
import { RevenueChart } from "./RevenueChart";
import {
  formatReportDate,
  formatReportInteger,
  formatReportMoney,
  formatReportPeriod
} from "../utils/reportFormatting";
import {
  createReportFileName,
  downloadReportCsv,
  printReport
} from "../utils/reportExports";

export function RevenueReportPanel({
  report,
  filters,
  isBusy,
  error,
  onFiltersChange,
  onRefresh
}: {
  report: RevenueSummaryReport | null;
  filters: RevenueSummaryFilters;
  isBusy: boolean;
  error?: string;
  onFiltersChange: (filters: RevenueSummaryFilters) => void;
  onRefresh: () => Promise<void>;
}) {
  const [chartType, setChartType] = useState<"bar" | "line">("bar");

  function exportCsv(): void {
    if (report === null) {
      return;
    }

    downloadReportCsv<RevenueSummaryPeriod>(
      createReportFileName("revenue-summary", `${report.fromDate}-to-${report.toDate}`),
      [
        { header: "Period", value: (row) => row.label },
        { header: "Period start", value: (row) => row.periodStart },
        { header: "Period end", value: (row) => row.periodEnd },
        { header: "Debit", value: (row) => row.debit },
        { header: "Credit", value: (row) => row.credit },
        { header: "Revenue", value: (row) => row.revenue },
        { header: "Activity count", value: (row) => row.activityCount },
        { header: "Currency", value: () => report.currencyCode }
      ],
      report.periods
    );
  }

  const totalDebit = report?.periods.reduce((sum, period) => sum + period.debit, 0) ?? 0;
  const totalCredit = report?.periods.reduce((sum, period) => sum + period.credit, 0) ?? 0;

  return (
    <section className="report-view">
      <ReportFilterBar isBusy={isBusy} onSubmit={onRefresh}>
        <DateRangeFields
          fromDate={filters.fromDate}
          toDate={filters.toDate}
          onChange={(range) => onFiltersChange({ ...filters, ...range })}
        />
        <label className="form-field">
          <span>Period</span>
          <select
            value={filters.period}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                period: event.target.value as RevenueSummaryFilters["period"]
              })
            }
          >
            <option value="Monthly">Monthly</option>
            <option value="Quarterly">Quarterly</option>
          </select>
        </label>
        <CurrencyField
          value={filters.currencyCode}
          onChange={(currencyCode) => onFiltersChange({ ...filters, currencyCode })}
        />
      </ReportFilterBar>

      <article className="client-panel report-print-surface">
        <ReportHeading
          kicker={report?.period ?? filters.period}
          title="Revenue summary"
          rowLabel={report === null ? "No periods" : `${report.periods.length} periods`}
          onExport={exportCsv}
          onPrint={() => printReport("Revenue Summary")}
          disabled={report === null || report.periods.length === 0}
        />
        <PrintMetadata
          detail={`${formatReportDate(report?.fromDate ?? filters.fromDate)} to ${formatReportDate(report?.toDate ?? filters.toDate)} · ${filters.period} · ${filters.currencyCode}`}
        />

        <ReportContentState
          isBusy={isBusy}
          error={error}
          hasData={report !== null}
          isEmpty={report !== null && report.periods.length === 0}
          emptyMessage="No posted revenue activity matches this reporting window."
        >
          {report !== null && (
            <>
              <ReportSummary
                items={[
                  {
                    label: "Revenue",
                    value: formatReportMoney(report.totalRevenue, report.currencyCode)
                  },
                  {
                    label: "Debit movement",
                    value: formatReportMoney(totalDebit, report.currencyCode)
                  },
                  {
                    label: "Credit movement",
                    value: formatReportMoney(totalCredit, report.currencyCode)
                  },
                  {
                    label: "Periods",
                    value: formatReportInteger(report.periods.length)
                  }
                ]}
              />
              <div className="revenue-chart-toolbar report-no-print">
                <span>Chart view</span>
                <div role="group" aria-label="Revenue chart type">
                  <button
                    type="button"
                    className={chartType === "bar" ? "active" : ""}
                    aria-pressed={chartType === "bar"}
                    onClick={() => setChartType("bar")}
                  >
                    Bars
                  </button>
                  <button
                    type="button"
                    className={chartType === "line" ? "active" : ""}
                    aria-pressed={chartType === "line"}
                    onClick={() => setChartType("line")}
                  >
                    Line
                  </button>
                </div>
              </div>
              <RevenueChart
                periods={report.periods}
                currencyCode={report.currencyCode}
                chartType={chartType}
              />
              <div className="report-table-frame">
                <table className="report-table">
                  <thead>
                    <tr>
                      <th scope="col">Period</th>
                      <th scope="col">Date range</th>
                      <th scope="col" className="numeric">Debit</th>
                      <th scope="col" className="numeric">Credit</th>
                      <th scope="col" className="numeric">Revenue</th>
                      <th scope="col" className="numeric">Entries</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.periods.map((period) => (
                      <tr key={`${period.periodStart}-${period.periodEnd}`}>
                        <th scope="row">{period.label}</th>
                        <td>{formatReportPeriod(period.periodStart, period.periodEnd)}</td>
                        <MoneyCell value={period.debit} currencyCode={report.currencyCode} />
                        <MoneyCell value={period.credit} currencyCode={report.currencyCode} />
                        <MoneyCell value={period.revenue} currencyCode={report.currencyCode} strong />
                        <td className="numeric">{formatReportInteger(period.activityCount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </ReportContentState>
      </article>
    </section>
  );
}
