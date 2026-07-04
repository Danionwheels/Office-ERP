import {
  BookOpenCheck,
  Calculator,
  CalendarDays,
  ClipboardList,
  FileBarChart,
  FileSpreadsheet,
  ListChecks,
  RefreshCw,
  Settings2
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { accountingCompanyCode } from "../constants/accountingConstants";
import {
  useAccountingWorkspace,
  type AccountingReportArea,
  type AccountingWorkspaceArea
} from "../hooks/useAccountingWorkspace";
import type {
  JournalEntrySourceDocument,
  JournalEntrySummary
} from "../types/accountingTypes";
import { AccountingControlsPanel } from "./AccountingControlsPanel";
import { AccountingPeriodsPanel } from "./AccountingPeriodsPanel";
import { BalanceSheetPanel } from "./BalanceSheetPanel";
import { ChartOfAccountsPanel } from "./ChartOfAccountsPanel";
import { JournalWorkbenchPanel } from "./JournalWorkbenchPanel";
import { LedgerAccountReconciliationPanel } from "./LedgerAccountReconciliationPanel";
import { ProfitAndLossPanel } from "./ProfitAndLossPanel";
import { AccountingKpiCard } from "./shared/AccountingKpiCard";
import { TrialBalancePanel } from "./TrialBalancePanel";

type AccountingWorkspaceProps = {
  workspace: ReturnType<typeof useAccountingWorkspace>;
  isBusy: boolean;
  onOpenSourceDocument: (entry: JournalEntrySummary) => Promise<void>;
  getSourceDocumentLabel: (entry: JournalEntrySummary) => string | null;
  getSourceDocumentClientLabel: (sourceDocument: JournalEntrySourceDocument) => string;
};

const workspaceAreas: Array<{
  key: AccountingWorkspaceArea;
  label: string;
  icon: LucideIcon;
}> = [
  { key: "setup", label: "Chart", icon: Calculator },
  { key: "controls", label: "Controls", icon: Settings2 },
  { key: "periods", label: "Periods", icon: CalendarDays },
  { key: "journal", label: "Journal", icon: BookOpenCheck },
  { key: "reports", label: "Reports", icon: FileSpreadsheet },
  { key: "reconcile", label: "Reconcile", icon: ListChecks }
];

const reportAreas: Array<{
  key: AccountingReportArea;
  label: string;
}> = [
  { key: "trialBalance", label: "Trial Balance" },
  { key: "profitAndLoss", label: "Profit & Loss" },
  { key: "balanceSheet", label: "Balance Sheet" }
];

export function AccountingWorkspace({
  workspace,
  isBusy,
  onOpenSourceDocument,
  getSourceDocumentLabel,
  getSourceDocumentClientLabel
}: AccountingWorkspaceProps) {
  const trialBalanceTone = workspace.trialBalance === null || workspace.trialBalance.difference === 0
    ? "good"
    : "attention";

  return (
    <section className="accounting-workspace">
      <header className="accounting-workspace-header">
        <div>
          <span>{accountingCompanyCode}</span>
          <h2>General ledger control</h2>
        </div>
        <div className="accounting-workspace-status">
          <ClipboardList size={18} />
          <span>{workspace.accountCodeRanges.length} ranges</span>
        </div>
      </header>

      <div className="accounting-kpi-grid">
        <AccountingKpiCard
          icon={Calculator}
          label="Ledger accounts"
          value={workspace.ledgerAccounts.length}
          detail={`${workspace.postingAccountCount} active posting`}
        />
        <AccountingKpiCard
          icon={CalendarDays}
          label="Open periods"
          value={workspace.openPeriodCount}
          detail={`${workspace.accountingPeriods.length} total periods`}
          tone={workspace.openPeriodCount > 0 ? "good" : "attention"}
        />
        <AccountingKpiCard
          icon={BookOpenCheck}
          label="Journal entries"
          value={workspace.journalEntries.length}
          detail={workspace.focusedJournalEntry === null ? "No focused journal" : "Journal focused"}
        />
        <AccountingKpiCard
          icon={FileSpreadsheet}
          label="Trial balance"
          value={workspace.trialBalance === null ? "-" : workspace.trialBalance.difference.toFixed(2)}
          detail={workspace.trialBalance === null ? "Awaiting balance" : workspace.trialBalance.currencyCode}
          tone={trialBalanceTone}
        />
      </div>

      <nav className="accounting-workspace-nav" aria-label="Accounting workspace">
        {workspaceAreas.map((area) => {
          const Icon = area.icon;

          return (
            <button
              aria-pressed={workspace.activeArea === area.key}
              className={workspace.activeArea === area.key ? "active" : ""}
              disabled={isBusy}
              key={area.key}
              onClick={() => workspace.setActiveArea(area.key)}
              type="button"
            >
              <Icon size={16} />
              <span>{area.label}</span>
            </button>
          );
        })}
      </nav>

      <div className="accounting-workspace-body">
        {workspace.activeArea === "setup" && (
          <ChartOfAccountsPanel
            accounts={workspace.ledgerAccounts}
            ranges={workspace.accountCodeRanges}
            filters={workspace.ledgerAccountFilters}
            selectedRangeRole={workspace.selectedAccountCodeRangeRole}
            rangeValue={workspace.accountCodeRangeForm}
            accountMode={workspace.selectedLedgerAccountId === "" ? "create" : "edit"}
            accountValue={workspace.ledgerAccountEditorForm}
            activity={workspace.ledgerAccountActivity}
            journalEntries={workspace.journalEntries}
            isBusy={isBusy}
            onFiltersChange={workspace.handleLedgerAccountFiltersChange}
            onRangeSelect={workspace.handleSelectAccountCodeRange}
            onRangeChange={workspace.setAccountCodeRangeForm}
            onSaveRange={workspace.handleSaveAccountCodeRange}
            onAccountChange={workspace.setLedgerAccountEditorForm}
            onNewAccount={workspace.handleNewLedgerAccount}
            onEditAccount={workspace.handleEditLedgerAccount}
            onSaveAccount={workspace.handleSaveLedgerAccount}
            onToggleAccountStatus={workspace.handleToggleLedgerAccountStatus}
            onViewAccountActivity={workspace.handleViewLedgerAccountActivity}
            onViewJournalEntry={workspace.handleViewJournalEntryFromActivity}
            onSuggestAccountCode={workspace.handleSuggestAccountingLedgerAccountCode}
            onRefresh={() => workspace.refreshAccountingSetup()}
          />
        )}

        {workspace.activeArea === "controls" && (
          <AccountingControlsPanel
            settings={workspace.accountingControlSettings}
            value={workspace.accountingControlSettingsForm}
            accounts={workspace.ledgerAccounts}
            isBusy={isBusy}
            onValueChange={workspace.setAccountingControlSettingsForm}
            onSave={workspace.handleSaveAccountingControls}
            onUseDefaults={workspace.handleUseDefaultAccountingControls}
            onRefresh={() => workspace.refreshAccountingControls()}
          />
        )}

        {workspace.activeArea === "periods" && (
          <AccountingPeriodsPanel
            periods={workspace.accountingPeriods}
            readiness={workspace.accountingPeriodReadiness}
            closeJournalPreview={workspace.accountingPeriodCloseJournalPreview}
            value={workspace.accountingPeriodForm}
            isBusy={isBusy}
            onValueChange={workspace.setAccountingPeriodForm}
            onPrepareNext={workspace.handlePrepareNextAccountingPeriod}
            onCreate={workspace.handleCreateAccountingPeriod}
            onCheckReadiness={workspace.handleCheckAccountingPeriodReadiness}
            onPreviewCloseJournal={workspace.handlePreviewAccountingCloseJournal}
            onClose={workspace.handleCloseAccountingPeriod}
            onReopen={workspace.handleReopenAccountingPeriod}
            onViewCloseJournalEntry={workspace.viewJournalEntryById}
            onRefresh={() => workspace.refreshAccountingPeriods()}
          />
        )}

        {workspace.activeArea === "journal" && (
          <JournalWorkbenchPanel
            accounts={workspace.ledgerAccounts}
            periods={workspace.accountingPeriods}
            entries={workspace.journalEntries}
            filters={workspace.journalEntryFilters}
            value={workspace.manualJournalEntryForm}
            focusedJournalEntryId={workspace.focusedJournalEntryId}
            focusedJournalEntry={workspace.focusedJournalEntry}
            sourceDocumentsByJournalEntryId={workspace.journalSourceDocumentsById}
            isBusy={isBusy}
            onFiltersChange={workspace.setJournalEntryFilters}
            onValueChange={workspace.setManualJournalEntryForm}
            onFocusJournalEntry={workspace.handleFocusJournalEntry}
            onPost={workspace.handlePostManualJournalEntry}
            onVoidEntry={workspace.handleVoidManualJournalEntry}
            onOpenSourceDocument={onOpenSourceDocument}
            getSourceDocumentLabel={getSourceDocumentLabel}
            getSourceDocumentClientLabel={getSourceDocumentClientLabel}
            onRefresh={() => workspace.refreshJournalEntries()}
          />
        )}

        {workspace.activeArea === "reports" && (
          <section className="accounting-report-workspace">
            <nav className="accounting-report-nav" aria-label="Accounting reports">
              {reportAreas.map((report) => (
                <button
                  aria-pressed={workspace.activeReportArea === report.key}
                  className={workspace.activeReportArea === report.key ? "active" : ""}
                  disabled={isBusy}
                  key={report.key}
                  onClick={() => workspace.setActiveReportArea(report.key)}
                  type="button"
                >
                  <FileBarChart size={15} />
                  <span>{report.label}</span>
                </button>
              ))}
              <button
                className="accounting-report-refresh"
                disabled={isBusy}
                onClick={() => workspace.refreshAccountingReports()}
                type="button"
                title="Refresh all accounting reports"
              >
                <RefreshCw size={15} />
                <span>Refresh all</span>
              </button>
            </nav>

            {workspace.activeReportArea === "trialBalance" && (
              <TrialBalancePanel
                balance={workspace.trialBalance}
                filters={workspace.trialBalanceFilters}
                isBusy={isBusy}
                onFiltersChange={workspace.setTrialBalanceFilters}
                onViewAccountActivity={workspace.handleViewTrialBalanceAccountActivity}
                onRefresh={() => workspace.refreshTrialBalance()}
              />
            )}

            {workspace.activeReportArea === "profitAndLoss" && (
              <ProfitAndLossPanel
                statement={workspace.profitAndLossStatement}
                filters={workspace.profitAndLossFilters}
                isBusy={isBusy}
                onFiltersChange={workspace.setProfitAndLossFilters}
                onViewAccountActivity={workspace.handleViewProfitAndLossAccountActivity}
                onRefresh={() => workspace.refreshProfitAndLossStatement()}
              />
            )}

            {workspace.activeReportArea === "balanceSheet" && (
              <BalanceSheetPanel
                balanceSheet={workspace.balanceSheet}
                filters={workspace.balanceSheetFilters}
                isBusy={isBusy}
                onFiltersChange={workspace.setBalanceSheetFilters}
                onViewAccountActivity={workspace.handleViewBalanceSheetAccountActivity}
                onRefresh={() => workspace.refreshBalanceSheet()}
              />
            )}
          </section>
        )}

        {workspace.activeArea === "reconcile" && (
          <LedgerAccountReconciliationPanel
            reconciliation={workspace.ledgerAccountReconciliation}
            repairPlan={workspace.ledgerAccountRepairPlan}
            isBusy={isBusy}
            onRefresh={() => workspace.refreshLedgerAccountReconciliation()}
          />
        )}
      </div>
    </section>
  );
}
