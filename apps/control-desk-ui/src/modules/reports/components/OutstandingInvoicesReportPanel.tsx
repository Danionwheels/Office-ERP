import { RefreshCw } from "lucide-react";
import type {
  OutstandingInvoiceFilters,
  OutstandingInvoiceRow,
  OutstandingInvoicesReport,
  ReportClientLookup
} from "../types/reportTypes";
import {
  ClientField,
  CurrencyField,
  DateRangeFields,
  ReportFilterBar
} from "./ReportFilterBar";
import {
  MoneyCell,
  PrintMetadata,
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
import { reportCountLabel, selectedClientLabel } from "../utils/reportLabels";
import {
  createReportFileName,
  downloadReportCsv,
  printReport
} from "../utils/reportExports";

export function OutstandingInvoicesReportPanel({
  report,
  clients,
  filters,
  isBusy,
  error,
  isClientSearchBusy,
  onFiltersChange,
  onRefresh,
  onLoadMore,
  onLoadAll,
  onSearchClients
}: {
  report: OutstandingInvoicesReport | null;
  clients: ReportClientLookup[];
  filters: OutstandingInvoiceFilters;
  isBusy: boolean;
  isClientSearchBusy: boolean;
  error?: string;
  onFiltersChange: (filters: OutstandingInvoiceFilters) => void;
  onRefresh: () => Promise<void>;
  onLoadMore: () => Promise<void>;
  onLoadAll: () => Promise<OutstandingInvoicesReport | null>;
  onSearchClients: (searchText: string) => Promise<void>;
}) {
  async function exportAllCsv(): Promise<void> {
    const fullReport = await onLoadAll();

    if (fullReport === null) {
      return;
    }

    downloadReportCsv<OutstandingInvoiceRow>(
      createReportFileName("outstanding-invoices", `${filters.fromDate}-to-${filters.toDate}`),
      outstandingInvoiceCsvColumns,
      fullReport.invoices
    );
  }

  async function printAll(): Promise<void> {
    const fullReport = await onLoadAll();

    if (fullReport !== null) {
      window.setTimeout(() => printReport("Outstanding Invoices"), 0);
    }
  }

  const rows = report?.invoices ?? [];
  const totalBalance = rows.reduce((sum, invoice) => sum + invoice.balanceDue, 0);
  const totalAmount = rows.reduce((sum, invoice) => sum + invoice.totalAmount, 0);
  const overdueCount = rows.filter((invoice) => invoice.daysOverdue > 0).length;

  return (
    <section className="report-view">
      <ReportFilterBar isBusy={isBusy} onSubmit={onRefresh}>
        <ClientField
          clients={clients}
          value={filters.clientId}
          isSearching={isClientSearchBusy}
          onChange={(clientId) => onFiltersChange({ ...filters, clientId })}
          onSearch={onSearchClients}
        />
        <DateRangeFields
          fromDate={filters.fromDate}
          toDate={filters.toDate}
          onChange={(range) => onFiltersChange({ ...filters, ...range })}
        />
        <label className="form-field">
          <span>Minimum balance</span>
          <input
            type="number"
            min="0"
            step="0.01"
            max={filters.maxAmount || undefined}
            value={filters.minAmount}
            onChange={(event) =>
              onFiltersChange({ ...filters, minAmount: event.target.value })
            }
            placeholder="Any"
          />
        </label>
        <label className="form-field">
          <span>Maximum balance</span>
          <input
            type="number"
            min={filters.minAmount || "0"}
            step="0.01"
            value={filters.maxAmount}
            onChange={(event) =>
              onFiltersChange({ ...filters, maxAmount: event.target.value })
            }
            placeholder="Any"
          />
        </label>
        <label className="form-field">
          <span>Status</span>
          <select
            value={filters.status}
            onChange={(event) =>
              onFiltersChange({ ...filters, status: event.target.value })
            }
          >
            <option value="">All open statuses</option>
            <option value="Issued">Issued</option>
            <option value="PartiallyPaid">Partially paid</option>
            <option value="Overdue">Overdue</option>
          </select>
        </label>
        <CurrencyField
          value={filters.currencyCode}
          onChange={(currencyCode) => onFiltersChange({ ...filters, currencyCode })}
        />
      </ReportFilterBar>
      <p className="report-filter-note report-no-print">
        The date range filters invoices issued in the selected range; balances remain
        current operational amounts.
      </p>

      <article className="client-panel report-print-surface">
        <ReportHeading
          kicker="Invoices issued in selected range"
          title="Outstanding invoices"
          rowLabel={reportCountLabel(rows.length, report?.filteredCount)}
          onExport={exportAllCsv}
          onPrint={printAll}
          disabled={report === null || rows.length === 0}
          isBusy={isBusy}
        />
        <PrintMetadata
          detail={`Invoices issued ${formatReportDate(filters.fromDate)} to ${formatReportDate(filters.toDate)} · Current balances · ${selectedClientLabel(filters.clientId, clients)} · ${filters.currencyCode}`}
        />

        <ReportContentState
          isBusy={isBusy}
          error={error}
          hasData={report !== null}
          isEmpty={report !== null && rows.length === 0}
          emptyMessage="No outstanding invoices match the selected filters."
        >
          {report !== null && (
            <>
              <ReportSummary
                items={[
                  {
                    label: report.hasMore
                      ? "Loaded current balance · issued range"
                      : "Current balance · issued range",
                    value: formatReportMoney(totalBalance, filters.currencyCode)
                  },
                  {
                    label: report.hasMore
                      ? "Loaded value · issued range"
                      : "Invoice value · issued range",
                    value: formatReportMoney(totalAmount, filters.currencyCode)
                  },
                  {
                    label: report.hasMore ? "Loaded overdue" : "Overdue invoices",
                    value: formatReportInteger(overdueCount)
                  },
                  {
                    label: "Loaded rows",
                    value: reportCountLabel(rows.length, report.filteredCount)
                  }
                ]}
              />
              <div className="report-table-frame">
                <table className="report-table report-wide-table">
                  <thead>
                    <tr>
                      <th scope="col">Invoice</th>
                      <th scope="col">Client</th>
                      <th scope="col">Issued</th>
                      <th scope="col">Due</th>
                      <th scope="col">Status</th>
                      <th scope="col">Aging</th>
                      <th scope="col" className="numeric">Total</th>
                      <th scope="col" className="numeric">Paid</th>
                      <th scope="col" className="numeric">Balance</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((invoice) => (
                      <tr key={invoice.invoiceId}>
                        <th scope="row">{invoice.invoiceNumber}</th>
                        <td>
                          <strong>{invoice.clientName}</strong>
                          <small>{invoice.clientCode}</small>
                        </td>
                        <td>{formatReportDate(invoice.issueDate)}</td>
                        <td>{formatReportDate(invoice.dueDate)}</td>
                        <td><StatusPill value={invoice.status} /></td>
                        <td>
                          <strong>{invoice.agingBucket}</strong>
                          <small>
                            {invoice.daysOverdue > 0
                              ? `${formatReportInteger(invoice.daysOverdue)} days overdue`
                              : "Current"}
                          </small>
                        </td>
                        <MoneyCell value={invoice.totalAmount} currencyCode={invoice.currencyCode} />
                        <MoneyCell value={invoice.amountPaid} currencyCode={invoice.currencyCode} />
                        <MoneyCell value={invoice.balanceDue} currencyCode={invoice.currencyCode} strong />
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {report.hasMore && (
                <div className="report-continuation report-no-print">
                  <span>{reportCountLabel(rows.length, report.filteredCount)} loaded</span>
                  <button
                    className="icon-button"
                    type="button"
                    disabled={isBusy}
                    onClick={() => void onLoadMore()}
                  >
                    <RefreshCw size={14} />
                    Load more
                  </button>
                </div>
              )}
            </>
          )}
        </ReportContentState>
      </article>
    </section>
  );
}

const outstandingInvoiceCsvColumns = [
  { header: "Invoice number", value: (row: OutstandingInvoiceRow) => row.invoiceNumber },
  { header: "Client code", value: (row: OutstandingInvoiceRow) => row.clientCode },
  { header: "Client", value: (row: OutstandingInvoiceRow) => row.clientName },
  { header: "Issue date", value: (row: OutstandingInvoiceRow) => row.issueDate },
  { header: "Due date", value: (row: OutstandingInvoiceRow) => row.dueDate },
  { header: "Status", value: (row: OutstandingInvoiceRow) => row.status },
  { header: "Aging bucket", value: (row: OutstandingInvoiceRow) => row.agingBucket },
  { header: "Days overdue", value: (row: OutstandingInvoiceRow) => row.daysOverdue },
  { header: "Total amount", value: (row: OutstandingInvoiceRow) => row.totalAmount },
  { header: "Amount paid", value: (row: OutstandingInvoiceRow) => row.amountPaid },
  { header: "Balance due", value: (row: OutstandingInvoiceRow) => row.balanceDue },
  { header: "Currency", value: (row: OutstandingInvoiceRow) => row.currencyCode }
];
