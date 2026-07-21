import {
  BarChart3,
  BookOpenText,
  FileClock,
  FileText,
  ReceiptText,
  TrendingUp
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { AgingReportPanel } from "../components/AgingReportPanel";
import { OutstandingInvoicesReportPanel } from "../components/OutstandingInvoicesReportPanel";
import { PaymentReceiptsReportPanel } from "../components/PaymentReceiptsReportPanel";
import { RevenueReportPanel } from "../components/RevenueReportPanel";
import { TrialBalanceReportPanel } from "../components/TrialBalanceReportPanel";
import { useReportsWorkspace } from "../hooks/useReportsWorkspace";
import type { ReportKey } from "../types/reportTypes";

type ReportTab = {
  key: ReportKey;
  label: string;
  description: string;
  Icon: LucideIcon;
};

const reportTabs: ReportTab[] = [
  {
    key: "aging",
    label: "AR aging",
    description: "Current, 1–30, 31–60, 61–90, and 91+ receivable exposure by client.",
    Icon: FileClock
  },
  {
    key: "revenue",
    label: "Revenue",
    description: "Monthly or quarterly posted revenue trends.",
    Icon: TrendingUp
  },
  {
    key: "outstanding-invoices",
    label: "Outstanding invoices",
    description: "Open invoice balances across the client portfolio.",
    Icon: FileText
  },
  {
    key: "payment-receipts",
    label: "Payment receipts",
    description: "Receipt evidence by date, client, method, and reference.",
    Icon: ReceiptText
  },
  {
    key: "trial-balance",
    label: "Journal / ledger",
    description: "Trial balance with account-level journal activity.",
    Icon: BookOpenText
  }
];

export function ReportsAuditPage() {
  const workspace = useReportsWorkspace();
  const activeTab =
    reportTabs.find((tab) => tab.key === workspace.activeReport) ?? reportTabs[0];

  return (
    <main className="reports-workspace">
      <header className="client-panel reports-hero">
        <div>
          <span className="reports-eyebrow">Cross-client evidence</span>
          <h2>Reports &amp; Audit</h2>
          <p>
            Review receivables, revenue, receipts, and ledger evidence across the
            provider office. Every view can be exported to CSV or printed cleanly to PDF.
          </p>
        </div>
        <div className="reports-hero-badge">
          <BarChart3 size={18} />
          <span>
            <strong>{reportTabs.length} live reports</strong>
            <small>Current reporting window defaults applied</small>
          </span>
        </div>
      </header>

      <nav className="report-tabs report-no-print" aria-label="Cross-client reports">
        {reportTabs.map(({ key, label, description, Icon }) => (
          <button
            className={key === workspace.activeReport ? "active" : ""}
            type="button"
            key={key}
            onClick={() => workspace.selectReport(key)}
            aria-current={key === workspace.activeReport ? "page" : undefined}
            title={description}
          >
            <Icon size={16} />
            <span>{label}</span>
          </button>
        ))}
      </nav>

      <div className="report-tab-context report-no-print">
        <strong>{activeTab.label}</strong>
        <span>{activeTab.description}</span>
      </div>

      {workspace.errors.clients && (
        <div className="report-feedback warning report-no-print" role="status">
          Client filter options could not be loaded: {workspace.errors.clients}
        </div>
      )}

      {workspace.activeReport === "aging" && (
        <AgingReportPanel
          report={workspace.aging}
          filters={workspace.agingFilters}
          isBusy={Boolean(workspace.busy.aging)}
          error={workspace.errors.aging}
          onFiltersChange={workspace.setAgingFilters}
          onRefresh={workspace.refreshAging}
        />
      )}

      {workspace.activeReport === "revenue" && (
        <RevenueReportPanel
          report={workspace.revenue}
          filters={workspace.revenueFilters}
          isBusy={Boolean(workspace.busy.revenue)}
          error={workspace.errors.revenue}
          onFiltersChange={workspace.setRevenueFilters}
          onRefresh={workspace.refreshRevenue}
        />
      )}

      {workspace.activeReport === "outstanding-invoices" && (
        <OutstandingInvoicesReportPanel
          report={workspace.outstanding}
          clients={workspace.clients}
          filters={workspace.outstandingFilters}
          isBusy={Boolean(workspace.busy["outstanding-invoices"])}
          isClientSearchBusy={Boolean(workspace.busy.clients)}
          error={workspace.errors["outstanding-invoices"]}
          onFiltersChange={workspace.setOutstandingFilters}
          onRefresh={workspace.refreshOutstanding}
          onLoadMore={workspace.loadMoreOutstanding}
          onLoadAll={workspace.loadAllOutstanding}
          onSearchClients={workspace.searchClients}
        />
      )}

      {workspace.activeReport === "payment-receipts" && (
        <PaymentReceiptsReportPanel
          report={workspace.receipts}
          clients={workspace.clients}
          filters={workspace.receiptFilters}
          isBusy={Boolean(workspace.busy["payment-receipts"])}
          isClientSearchBusy={Boolean(workspace.busy.clients)}
          error={workspace.errors["payment-receipts"]}
          onFiltersChange={workspace.setReceiptFilters}
          onRefresh={workspace.refreshReceipts}
          onLoadMore={workspace.loadMoreReceipts}
          onLoadAll={workspace.loadAllReceipts}
          onSearchClients={workspace.searchClients}
        />
      )}

      {workspace.activeReport === "trial-balance" && (
        <TrialBalanceReportPanel
          report={workspace.trialBalance}
          ledgerActivity={workspace.ledgerActivity}
          selectedLedgerAccountId={workspace.selectedLedgerAccountId}
          filters={workspace.trialBalanceFilters}
          isBusy={Boolean(workspace.busy["trial-balance"])}
          isLedgerBusy={Boolean(workspace.busy.ledger)}
          error={workspace.errors["trial-balance"]}
          ledgerError={workspace.errors.ledger}
          onFiltersChange={workspace.setTrialBalanceFilters}
          onRefresh={workspace.refreshTrialBalance}
          onSelectLedgerAccount={workspace.selectLedgerAccount}
        />
      )}
    </main>
  );
}
