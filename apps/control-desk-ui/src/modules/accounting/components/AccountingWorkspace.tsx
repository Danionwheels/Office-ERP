import {
  FileBarChart,
  RefreshCw
} from "lucide-react";
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
}> = [
  { key: "setup", label: "Chart of Accounts" },
  { key: "controls", label: "Controls" },
  { key: "periods", label: "Periods" },
  { key: "journal", label: "Journal" },
  { key: "reports", label: "Reports" },
  { key: "reconcile", label: "Reconcile" }
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
  const headerStatus = getAccountingHeaderStatus(workspace);

  return (
    <section className="accounting-workspace">
      <header className="accounting-workspace-header">
        <div>
          <span>{accountingCompanyCode}</span>
          <h2>General Ledger</h2>
          <small>{headerStatus}</small>
        </div>
        <label className="toolbar-field accounting-area-picker">
          <span>Section</span>
          <select
            value={workspace.activeArea}
            onChange={(event) =>
              workspace.setActiveArea(event.target.value as AccountingWorkspaceArea)
            }
            disabled={isBusy}
          >
            {workspaceAreas.map((area) => (
              <option key={area.key} value={area.key}>
                {area.label}
              </option>
            ))}
          </select>
        </label>
      </header>

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
            accountSaveErrors={workspace.ledgerAccountSaveErrors}
            reconciliation={workspace.ledgerAccountReconciliation}
            repairPlan={workspace.ledgerAccountRepairPlan}
            rangeValidation={workspace.accountCodeRangeValidation}
            importText={workspace.chartOfAccountsImportText}
            importDelimiter={workspace.chartOfAccountsImportDelimiter}
            importPreview={workspace.chartOfAccountsImportPreview}
            activity={workspace.ledgerAccountActivity}
            journalEntries={workspace.journalEntries}
            isBusy={isBusy}
            onFiltersChange={workspace.handleLedgerAccountFiltersChange}
            onRangeSelect={workspace.handleSelectAccountCodeRange}
            onRangeChange={workspace.setAccountCodeRangeForm}
            onSaveRange={workspace.handleSaveAccountCodeRange}
            onAccountChange={workspace.handleLedgerAccountEditorFormChange}
            onNewAccount={workspace.handleNewLedgerAccount}
            onBootstrapStandardChartOfAccounts={workspace.handleBootstrapStandardChartOfAccounts}
            onStartAccountCreate={workspace.handleStartLedgerAccountCreate}
            onEditAccount={workspace.handleEditLedgerAccount}
            onSaveAccount={workspace.handleSaveLedgerAccount}
            onToggleAccountStatus={workspace.handleToggleLedgerAccountStatus}
            onImportTextChange={workspace.setChartOfAccountsImportText}
            onImportDelimiterChange={workspace.setChartOfAccountsImportDelimiter}
            onPreviewImport={workspace.handlePreviewChartOfAccountsImport}
            onUseImportTemplate={workspace.handleUseChartOfAccountsImportTemplate}
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
            voucherRules={workspace.voucherNumberingRules}
            voucherRuleForms={workspace.voucherNumberingRuleForms}
            accounts={workspace.ledgerAccounts}
            isBusy={isBusy}
            onValueChange={workspace.setAccountingControlSettingsForm}
            onVoucherRuleChange={workspace.handleVoucherNumberingRuleFormChange}
            onSaveVoucherRule={workspace.handleSaveVoucherNumberingRule}
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
            manualVoucherPreview={workspace.manualJournalVoucherPreview}
            openingBalanceValue={workspace.openingBalanceImportForm}
            openingBalancePreview={workspace.openingBalanceImportPreview}
            openingBalanceImportText={workspace.openingBalanceImportText}
            openingBalanceImportDelimiter={workspace.openingBalanceImportDelimiter}
            openingBalanceImportTextPreview={workspace.openingBalanceImportTextPreview}
            focusedJournalEntryId={workspace.focusedJournalEntryId}
            focusedJournalEntry={workspace.focusedJournalEntry}
            sourceDocumentsByJournalEntryId={workspace.journalSourceDocumentsById}
            isBusy={isBusy}
            onFiltersChange={workspace.setJournalEntryFilters}
            onValueChange={workspace.setManualJournalEntryForm}
            onSuggestVoucherNumber={workspace.handleSuggestManualJournalVoucherNumber}
            onOpeningBalanceValueChange={workspace.setOpeningBalanceImportForm}
            onPreviewOpeningBalance={workspace.handlePreviewOpeningBalanceImport}
            onOpeningBalanceImportTextChange={workspace.setOpeningBalanceImportText}
            onOpeningBalanceImportDelimiterChange={workspace.setOpeningBalanceImportDelimiter}
            onPreviewOpeningBalanceText={workspace.handlePreviewOpeningBalanceImportText}
            onUseOpeningBalanceTemplate={workspace.handleUseOpeningBalanceImportTemplate}
            onSaveOpeningBalanceProfile={workspace.handleSaveOpeningBalanceProfile}
            onPostOpeningBalance={workspace.handlePostOpeningBalanceImport}
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
                accounts={workspace.ledgerAccounts}
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
                accounts={workspace.ledgerAccounts}
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
                accounts={workspace.ledgerAccounts}
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
            onApplyRepairAction={workspace.handleApplyLedgerAccountRepairAction}
            onRefresh={() => workspace.refreshLedgerAccountReconciliation()}
          />
        )}
      </div>
    </section>
  );
}

function getAccountingHeaderStatus(workspace: AccountingWorkspaceProps["workspace"]): string {
  if (workspace.ledgerAccountReconciliation !== null && workspace.ledgerAccountReconciliation.issueCount > 0) {
    return `${workspace.ledgerAccountReconciliation.issueCount} COA issues`;
  }

  if (workspace.accountingControlSettings?.isConfigured === true) {
    return "Controls configured";
  }

  if (workspace.ledgerAccounts.length > 0) {
    return `${workspace.ledgerAccounts.length} ledger accounts`;
  }

  return "Not loaded";
}
