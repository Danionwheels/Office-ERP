import { RefreshCw } from "lucide-react";
import type {
  PaymentReceiptRow,
  PaymentReceiptsFilters,
  PaymentReceiptsReport,
  ReportClientLookup
} from "../types/reportTypes";
import {
  ClientField,
  CurrencyField,
  DateRangeFields,
  ReportFilterBar
} from "./ReportFilterBar";
import {
  humanizeToken,
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

export function PaymentReceiptsReportPanel({
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
  report: PaymentReceiptsReport | null;
  clients: ReportClientLookup[];
  filters: PaymentReceiptsFilters;
  isBusy: boolean;
  isClientSearchBusy: boolean;
  error?: string;
  onFiltersChange: (filters: PaymentReceiptsFilters) => void;
  onRefresh: () => Promise<void>;
  onLoadMore: () => Promise<void>;
  onLoadAll: () => Promise<PaymentReceiptsReport | null>;
  onSearchClients: (searchText: string) => Promise<void>;
}) {
  async function exportAllCsv(): Promise<void> {
    const fullReport = await onLoadAll();

    if (fullReport === null) {
      return;
    }

    downloadReportCsv<PaymentReceiptRow>(
      createReportFileName("payment-receipts", `${filters.fromDate}-to-${filters.toDate}`),
      paymentReceiptCsvColumns,
      fullReport.payments
    );
  }

  async function printAll(): Promise<void> {
    const fullReport = await onLoadAll();

    if (fullReport !== null) {
      window.setTimeout(() => printReport("Payment Receipts"), 0);
    }
  }

  const rows = report?.payments ?? [];
  const approvedReceived = rows
    .filter((payment) => payment.status === "Approved")
    .reduce((sum, payment) => sum + payment.amount, 0);
  const clientCount = new Set(rows.map((payment) => payment.clientId)).size;

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
          <span>Method</span>
          <select
            value={filters.method}
            onChange={(event) =>
              onFiltersChange({ ...filters, method: event.target.value })
            }
          >
            <option value="">All methods</option>
            <option value="Card">Card</option>
            <option value="BankTransfer">Bank transfer</option>
            <option value="ManualCash">Manual cash</option>
            <option value="ManualAdjustment">Manual adjustment</option>
          </select>
        </label>
        <label className="form-field">
          <span>Status</span>
          <select
            value={filters.status}
            onChange={(event) =>
              onFiltersChange({ ...filters, status: event.target.value })
            }
          >
            <option value="">All statuses</option>
            <option value="PendingReview">Pending review</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
            <option value="Reversed">Reversed</option>
          </select>
        </label>
        <CurrencyField
          value={filters.currencyCode}
          onChange={(currencyCode) => onFiltersChange({ ...filters, currencyCode })}
        />
      </ReportFilterBar>
      <p className="report-filter-note report-no-print">
        This is a current-status receipt register. The date range filters the original
        received date; approval and reversal states reflect their current status.
      </p>

      <article className="client-panel report-print-surface">
        <ReportHeading
          kicker="Current-status receipt register"
          title="Payment receipts"
          rowLabel={reportCountLabel(rows.length, report?.filteredCount)}
          onExport={exportAllCsv}
          onPrint={printAll}
          disabled={report === null || rows.length === 0}
          isBusy={isBusy}
        />
        <PrintMetadata
          detail={`Original received date ${formatReportDate(filters.fromDate)} to ${formatReportDate(filters.toDate)} · Current statuses · ${selectedClientLabel(filters.clientId, clients)} · ${filters.currencyCode}`}
        />

        <ReportContentState
          isBusy={isBusy}
          error={error}
          hasData={report !== null}
          isEmpty={report !== null && rows.length === 0}
          emptyMessage="No payment receipts match the selected filters."
        >
          {report !== null && (
            <>
              <ReportSummary
                items={[
                  {
                    label: report.hasMore
                      ? "Loaded currently approved received"
                      : "Currently approved received",
                    value: formatReportMoney(approvedReceived, filters.currencyCode)
                  },
                  {
                    label: report.hasMore ? "Loaded rows" : "Receipts",
                    value: formatReportInteger(rows.length)
                  },
                  {
                    label: report.hasMore ? "Loaded clients" : "Clients",
                    value: formatReportInteger(clientCount)
                  },
                  {
                    label: "Result rows",
                    value: reportCountLabel(rows.length, report.filteredCount)
                  }
                ]}
              />
              <div className="report-table-frame">
                <table className="report-table report-wide-table">
                  <thead>
                    <tr>
                      <th scope="col">Date</th>
                      <th scope="col">Client</th>
                      <th scope="col">Reference</th>
                      <th scope="col">Invoice</th>
                      <th scope="col">Method</th>
                      <th scope="col">Status</th>
                      <th scope="col" className="numeric">Amount</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((payment) => (
                      <tr key={payment.paymentId}>
                        <td>{formatReportDate(payment.receivedOn)}</td>
                        <td>
                          <strong>{payment.clientName}</strong>
                          <small>{payment.clientCode}</small>
                        </td>
                        <th scope="row">{payment.reference}</th>
                        <td>{payment.invoiceNumber || "Unallocated"}</td>
                        <td>{humanizeToken(payment.method)}</td>
                        <td><StatusPill value={payment.status} /></td>
                        <MoneyCell value={payment.amount} currencyCode={payment.currencyCode} strong />
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

const paymentReceiptCsvColumns = [
  { header: "Received on", value: (row: PaymentReceiptRow) => row.receivedOn },
  { header: "Client code", value: (row: PaymentReceiptRow) => row.clientCode },
  { header: "Client", value: (row: PaymentReceiptRow) => row.clientName },
  { header: "Reference", value: (row: PaymentReceiptRow) => row.reference },
  { header: "Invoice number", value: (row: PaymentReceiptRow) => row.invoiceNumber },
  { header: "Method", value: (row: PaymentReceiptRow) => row.method },
  { header: "Status", value: (row: PaymentReceiptRow) => row.status },
  { header: "Amount", value: (row: PaymentReceiptRow) => row.amount },
  { header: "Currency", value: (row: PaymentReceiptRow) => row.currencyCode }
];
