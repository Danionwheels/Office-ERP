import {
  FileInput,
  ListTree,
  PanelRightOpen,
  Plus,
  RefreshCw,
  Save,
  Search,
  Wand2
} from "lucide-react";
import { type FormEvent, useEffect, useState } from "react";
import type { ApiErrorItem } from "../../../shared/api/apiError";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  AccountCodeRangeValidation,
  ChartOfAccountsImportTextPreview,
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  LedgerAccountCreateContext,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  LedgerAccountReconciliation,
  LedgerAccountRepairPlan,
  JournalEntrySummary,
  LedgerAccountSummary
} from "../types/accountingTypes";
import {
  getPersistedLegacyAccountLevel,
  getLedgerAccountLevelOptions,
  getVisibleAccounts
} from "../utils/chartOfAccountsModel";
import {
  buildAccountTreeRows,
  buildRangeUsageByRole,
  buildRangeValidationIssueMap,
  formatRangeRule,
  getAccountTreeLineageIds,
  getInlineAccountNamePlaceholder,
  getInlineCreateParentDisplay,
  getInlineCreateRuleDisplay,
  getInlineCreateStatusItems,
  getRangeSetupFacts,
  getRangeValidationIssueGroups,
  getSelectedRangeFacts
} from "../utils/chartOfAccountsWorkspaceModel";
import { ChartOfAccountsActivityPanel } from "./shared/ChartOfAccountsActivityPanel";
import { ChartOfAccountsDetailsPanel } from "./shared/ChartOfAccountsDetailsPanel";
import { ChartOfAccountsImportPanel } from "./shared/ChartOfAccountsImportPanel";
import { ChartOfAccountsListPanel } from "./shared/ChartOfAccountsListPanel";
import { ChartOfAccountsRangePanel } from "./shared/ChartOfAccountsRangePanel";
import {
  ChartOfAccountsWorkWindow,
  type ChartOfAccountsWorkWindowView
} from "./shared/ChartOfAccountsWorkWindow";

type ChartOfAccountsPanelProps = {
  accounts: LedgerAccountSummary[];
  ranges: AccountCodeRange[];
  filters: LedgerAccountFilters;
  selectedRangeRole: string;
  rangeValue: AccountCodeRangeFormInput;
  accountMode: "create" | "edit";
  accountValue: LedgerAccountEditorInput;
  accountSaveErrors: ApiErrorItem[];
  reconciliation: LedgerAccountReconciliation | null;
  repairPlan: LedgerAccountRepairPlan | null;
  rangeValidation: AccountCodeRangeValidation | null;
  importText: string;
  importDelimiter: string;
  importPreview: ChartOfAccountsImportTextPreview | null;
  activity: LedgerAccountActivity | null;
  journalEntries: JournalEntrySummary[];
  isBusy: boolean;
  onFiltersChange: (value: LedgerAccountFilters) => void;
  onRangeSelect: (range: AccountCodeRange) => void;
  onRangeChange: (value: AccountCodeRangeFormInput) => void;
  onSaveRange: () => Promise<void>;
  onAccountChange: (value: LedgerAccountEditorInput) => void;
  onNewAccount: () => Promise<void>;
  onBootstrapStandardChartOfAccounts: () => Promise<void>;
  onStartAccountCreate: (context: LedgerAccountCreateContext) => Promise<void>;
  onEditAccount: (account: LedgerAccountSummary) => void;
  onSaveAccount: () => Promise<void>;
  onToggleAccountStatus: (account: LedgerAccountSummary) => Promise<void>;
  onImportTextChange: (value: string) => void;
  onImportDelimiterChange: (value: string) => void;
  onPreviewImport: () => Promise<void>;
  onUseImportTemplate: () => void;
  onViewAccountActivity: (account: LedgerAccountSummary) => Promise<void>;
  onViewJournalEntry: (line: LedgerAccountActivityLine) => Promise<void>;
  onSuggestAccountCode: () => Promise<void>;
  onRefresh: () => Promise<void>;
};

const selectedRangeInlineAnchorId = "__selected-range-create__";

export function ChartOfAccountsPanel({
  accounts,
  ranges,
  filters,
  selectedRangeRole,
  rangeValue,
  accountMode,
  accountValue,
  accountSaveErrors,
  reconciliation,
  repairPlan,
  rangeValidation,
  importText,
  importDelimiter,
  importPreview,
  activity,
  journalEntries,
  isBusy,
  onFiltersChange,
  onRangeSelect,
  onRangeChange,
  onSaveRange,
  onAccountChange,
  onNewAccount,
  onBootstrapStandardChartOfAccounts,
  onStartAccountCreate,
  onEditAccount,
  onSaveAccount,
  onToggleAccountStatus,
  onImportTextChange,
  onImportDelimiterChange,
  onPreviewImport,
  onUseImportTemplate,
  onViewAccountActivity,
  onViewJournalEntry,
  onSuggestAccountCode,
  onRefresh
}: ChartOfAccountsPanelProps) {
  const [collapsedAccountIds, setCollapsedAccountIds] = useState<Set<string>>(() => new Set());
  const [inlineCreateAnchorId, setInlineCreateAnchorId] = useState<string | null>(null);
  const [inlineCreateLabel, setInlineCreateLabel] = useState("New ledger account");
  const [isWorkbenchOpen, setIsWorkbenchOpen] = useState(false);
  const [activeWorkView, setActiveWorkView] =
    useState<ChartOfAccountsWorkWindowView>("account");
  const activeRanges = ranges.filter((range) => range.isActive);
  const selectedRange = ranges.find((range) => range.role === selectedRangeRole) ?? null;
  const accountLevelOptions = getLedgerAccountLevelOptions(
    selectedRange,
    accountMode,
    accountValue.level
  );
  const isParentTreeView = filters.viewMode === "headerTotal";
  const visibleAccounts = getVisibleAccounts(
    accounts,
    ranges,
    isParentTreeView ? { ...filters, posting: "" } : filters
  );
  const accountTreeRows = buildAccountTreeRows(
    accounts,
    visibleAccounts,
    ranges,
    collapsedAccountIds,
    isParentTreeView
  );
  const expandedAccountTreeRows = buildAccountTreeRows(
    accounts,
    visibleAccounts,
    ranges,
    new Set(),
    isParentTreeView
  );
  const collapsibleAccountIds = expandedAccountTreeRows
    .filter((row) => row.hasChildren)
    .map((row) => row.account.ledgerAccountId);
  const collapsedVisibleParentCount = collapsibleAccountIds.filter((accountId) =>
    collapsedAccountIds.has(accountId)
  ).length;
  const openVisibleParentCount = Math.max(collapsibleAccountIds.length - collapsedVisibleParentCount, 0);
  const rangeValidationIssues = rangeValidation?.issues ?? [];
  const rangeValidationIssuesByRole = buildRangeValidationIssueMap(rangeValidationIssues);
  const selectedRangeIssues = selectedRange === null
    ? []
    : rangeValidationIssuesByRole.get(selectedRange.role) ?? [];
  const displayedRangeValidationIssues =
    selectedRangeIssues.length > 0 ? selectedRangeIssues : rangeValidationIssues;
  const rangeUsageByRole = buildRangeUsageByRole(accounts, ranges);
  const selectedRangeUsage = selectedRange === null
    ? 0
    : rangeUsageByRole.get(selectedRange.role) ?? 0;
  const rangeSetupFacts = getRangeSetupFacts(
    ranges,
    rangeValidation,
    selectedRange,
    selectedRangeIssues,
    selectedRangeUsage
  );
  const selectedRangeFacts = selectedRange === null
    ? []
    : getSelectedRangeFacts(selectedRange, selectedRangeIssues, selectedRangeUsage);
  const displayedRangeIssueGroups = getRangeValidationIssueGroups(displayedRangeValidationIssues);
  const postingCount = visibleAccounts.filter((account) => account.isPostingAccount).length;
  const coaIssueCount = reconciliation?.issueCount ?? 0;
  const coaActionCount = repairPlan?.actionCount ?? 0;
  const affectedAccountCount = reconciliation?.items.length ?? 0;
  const importPreviewStatus = importPreview === null
    ? "No preview"
    : importPreview.rejectCount === 0
      ? "Ready"
      : `${importPreview.rejectCount} reject${importPreview.rejectCount === 1 ? "" : "s"}`;
  const coaHealthText =
    reconciliation === null
      ? "Not checked"
      : coaIssueCount === 0
        ? "Clean"
        : `${coaIssueCount} issue${coaIssueCount === 1 ? "" : "s"}`;
  const rangeValidationText =
    rangeValidation === null
      ? "Not checked"
      : rangeValidation.errorCount > 0
        ? `${rangeValidation.errorCount} error${rangeValidation.errorCount === 1 ? "" : "s"}`
        : rangeValidation.warningCount > 0
          ? `${rangeValidation.warningCount} warning${rangeValidation.warningCount === 1 ? "" : "s"}`
          : "Clean";
  const canSaveRange =
    selectedRange !== null
    && rangeValue.displayName.trim() !== ""
    && rangeValue.searchPrefix.trim() !== ""
    && rangeValue.rangeStart.trim() !== ""
    && rangeValue.rangeEnd.trim() !== ""
    && Number(rangeValue.codeLength) > 0
    && rangeValue.accountType.trim() !== ""
    && rangeValue.normalBalance.trim() !== "";
  const canSuggestAccountCode = accountMode === "create" && selectedRangeRole !== "";
  const createLevel = getPersistedLegacyAccountLevel(accountValue.level);
  const canUseCreateLevel =
    accountMode === "edit"
    || (createLevel !== null && createLevel.code !== "D");
  const canSaveAccount =
    accountValue.name.trim() !== ""
    && accountValue.status.trim() !== ""
    && (accountMode === "edit"
      || (accountValue.code.trim() !== ""
        && accountValue.type.trim() !== ""
        && accountValue.normalBalance.trim() !== ""
        && accountValue.level.trim() !== ""
        && canUseCreateLevel));
  const accountFormId = "coa-account-maintenance-form";
  const activityAccount = activity === null
    ? undefined
    : accounts.find((account) => account.ledgerAccountId === activity.ledgerAccountId);
  const isInlineCreating = accountMode === "create" && inlineCreateAnchorId !== null;
  const canSubmitAccountForm =
    accountMode === "edit" || isInlineCreating;
  const companyCode = filters.companyCode.trim() === "" ? "MAIN" : filters.companyCode;
  const inlineParentAccountId = accountValue.parentAccountId.trim();
  const parentAccountForEditor =
    inlineParentAccountId === ""
      ? null
      : accounts.find((account) => account.ledgerAccountId === inlineParentAccountId) ?? null;
  const inlineParentRow =
    inlineParentAccountId === ""
      ? null
      : expandedAccountTreeRows.find((row) => row.account.ledgerAccountId === inlineParentAccountId)
        ?? accountTreeRows.find((row) => row.account.ledgerAccountId === inlineParentAccountId)
        ?? null;
  const inlineCreateDepth = Math.max(Math.min((inlineParentRow?.depth ?? -1) + 1, 5), 0);
  const inlineCreateDepthLabel = inlineCreateDepth === 0 ? "Top level" : `Depth ${inlineCreateDepth}`;
  const inlineCreateReady = canSaveAccount && accountValue.name.trim() !== "";
  const parentAccountDisplay =
    parentAccountForEditor === null
      ? accountMode === "edit"
        ? inlineParentAccountId
        : selectedRange?.parentCode ?? ""
      : `${parentAccountForEditor.displayCode} / ${parentAccountForEditor.name}`;
  const inlineCreateParentDisplay = getInlineCreateParentDisplay(
    parentAccountForEditor,
    inlineParentAccountId,
    selectedRange
  );
  const inlineCreateRuleDisplay = getInlineCreateRuleDisplay(selectedRange, accountValue);
  const inlineNamePlaceholder = getInlineAccountNamePlaceholder(
    accountValue.level,
    parentAccountForEditor,
    selectedRange
  );
  const inlineCodePlaceholder = selectedRange === null
    ? "Account code"
    : `${selectedRange.rangeStart}-${selectedRange.rangeEnd}`;
  const inlineCreateStatusItems = getInlineCreateStatusItems({
    accountSaveErrors,
    accountValue,
    canSaveAccount,
    depthLabel: inlineCreateDepthLabel,
    parentAccount: parentAccountForEditor,
    selectedRange
  });

  useEffect(() => {
    if (accountMode !== "edit") {
      return;
    }

    setInlineCreateAnchorId(null);
    setInlineCreateLabel("New ledger account");

    const parentAccountId = accountValue.parentAccountId.trim();

    if (parentAccountId === "") {
      return;
    }

    openAccountLineage(parentAccountId);
  }, [accountMode, accountValue.code, accountValue.parentAccountId, accounts, ranges, isParentTreeView]);

  function handleSelectRange(range: AccountCodeRange) {
    onRangeSelect(range);
    onFiltersChange({
      ...filters,
      role: filters.role === range.role ? "" : range.role
    });
  }

  function handleSelectRangeRole(role: string) {
    const nextRange = activeRanges.find((range) => range.role === role) ?? null;

    if (nextRange !== null) {
      onRangeSelect(nextRange);
    }

    onFiltersChange({
      ...filters,
      role
    });
  }

  async function handleStartRangeAccountCreate() {
    setInlineCreateAnchorId(selectedRangeInlineAnchorId);
    setInlineCreateLabel(
      selectedRange === null
        ? "New ledger account"
        : `New account / ${selectedRange.displayName}`
    );
    setActiveWorkView("account");
    setIsWorkbenchOpen(true);

    await onNewAccount();
  }

  async function handleStartInlineCreate(
    account: LedgerAccountSummary,
    context: LedgerAccountCreateContext,
    label: string
  ) {
    setInlineCreateAnchorId(account.ledgerAccountId);
    setInlineCreateLabel(label);

    if (context.parentAccountId === account.ledgerAccountId) {
      openAccountLineage(account.ledgerAccountId);
    }

    await onStartAccountCreate(context);
  }

  async function handleInlineAccountFormSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!canSaveAccount || isBusy || !isInlineCreating) {
      return;
    }

    await onSaveAccount();
  }

  function openAccountLineage(accountId: string) {
    const lineageIds = getAccountTreeLineageIds(accountId, accounts, ranges, isParentTreeView);

    if (lineageIds.length === 0) {
      return;
    }

    setCollapsedAccountIds((current) => {
      const next = new Set(current);
      let changed = false;

      lineageIds.forEach((lineageId) => {
        if (next.delete(lineageId)) {
          changed = true;
        }
      });

      return changed ? next : current;
    });
  }

  function toggleAccountCollapse(accountId: string) {
    setCollapsedAccountIds((current) => {
      const next = new Set(current);

      if (next.has(accountId)) {
        next.delete(accountId);
      } else {
        next.add(accountId);
      }

      return next;
    });
  }

  function handleExpandAllTree() {
    setCollapsedAccountIds(new Set());
  }

  function handleCollapseAllTree() {
    setCollapsedAccountIds(new Set(collapsibleAccountIds));
  }

  function handleToggleRangeSetup() {
    if (isWorkbenchOpen && activeWorkView === "ranges") {
      setIsWorkbenchOpen(false);
      return;
    }

    setActiveWorkView("ranges");
    setIsWorkbenchOpen(true);
  }

  function handleOpenImport() {
    setActiveWorkView("import");
    setIsWorkbenchOpen(true);
  }

  function handleEditAccount(account: LedgerAccountSummary) {
    setInlineCreateAnchorId(null);
    setInlineCreateLabel("New ledger account");
    setActiveWorkView("account");
    setIsWorkbenchOpen(true);
    onEditAccount(account);
  }

  async function handleViewAccountActivity(account: LedgerAccountSummary) {
    setInlineCreateAnchorId(null);
    setInlineCreateLabel("New ledger account");
    setActiveWorkView("activity");
    setIsWorkbenchOpen(true);
    await onViewAccountActivity(account);
  }

  function handleCancelAccountCreate() {
    setInlineCreateAnchorId(null);
    setInlineCreateLabel("New ledger account");
    setIsWorkbenchOpen(false);
  }

  function handleCloseWorkbench() {
    if (accountMode === "create") {
      setInlineCreateAnchorId(null);
      setInlineCreateLabel("New ledger account");
    }

    setIsWorkbenchOpen(false);
  }

  function handleWorkWindowViewChange(view: ChartOfAccountsWorkWindowView) {
    if (view === "activity" && activity === null) {
      return;
    }

    setActiveWorkView(view);
  }

  return (
    <section className="coa-workspace coa-maintain-workspace">
      <header className="coa-maintain-toolbar">
        <div className="toolbar-title">
          <span>{companyCode} / Maintain</span>
          <strong>Chart of Accounts</strong>
        </div>

        <label className="toolbar-field coa-toolbar-search">
          <span>Find Account</span>
          <span className="coa-search-input">
            <Search size={15} />
            <input
              value={filters.search}
              onChange={(event) =>
                onFiltersChange({
                  ...filters,
                  search: event.target.value
                })
              }
              disabled={isBusy}
            />
          </span>
        </label>

        <label className="toolbar-field short">
          <span>Type</span>
          <select
            value={filters.type}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                type: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="">All</option>
            <option value="Asset">Asset</option>
            <option value="Liability">Liability</option>
            <option value="Equity">Equity</option>
            <option value="Revenue">Revenue</option>
            <option value="Expense">Expense</option>
          </select>
        </label>

        <label className="toolbar-field short">
          <span>Status</span>
          <select
            value={filters.status}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                status: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="">All</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
          </select>
        </label>

        <label className="toolbar-field short">
          <span>Role</span>
          <select
            value={filters.role}
            onChange={(event) =>
              handleSelectRangeRole(event.target.value)
            }
            disabled={isBusy}
          >
            <option value="">All</option>
            {activeRanges.map((range) => (
              <option key={range.role} value={range.role}>
                {range.role}
              </option>
            ))}
          </select>
        </label>

        <button
          className="icon-button"
          type="button"
          onClick={onRefresh}
          disabled={isBusy}
          title="Refresh chart of accounts"
        >
          <RefreshCw size={16} />
          Refresh
        </button>

        <button
          className="icon-button"
          type="button"
          onClick={() => void onBootstrapStandardChartOfAccounts()}
          disabled={isBusy}
          title="Load the standard provider-office chart of accounts"
        >
          <ListTree size={16} />
          Load COA
        </button>

        <button
          className="icon-button"
          type="button"
          onClick={() => void handleStartRangeAccountCreate()}
          disabled={isBusy}
          title="Start a new ledger account"
        >
          <Plus size={16} />
          New
        </button>

        <button
          className="icon-button"
          type="button"
          onClick={onSuggestAccountCode}
          disabled={isBusy || !canSuggestAccountCode}
          title="Suggest the next code for the selected range"
        >
          <Wand2 size={16} />
          Suggest
        </button>

        <button
          className={`icon-button${isWorkbenchOpen ? " primary" : ""}`}
          type="button"
          onClick={() => {
            setActiveWorkView("account");
            setIsWorkbenchOpen(true);
          }}
          disabled={isBusy}
          title="Open COA work window"
        >
          <PanelRightOpen size={16} />
          Window
        </button>

        <button
          className={`icon-button${isWorkbenchOpen && activeWorkView === "import" ? " primary" : ""}`}
          type="button"
          onClick={handleOpenImport}
          disabled={isBusy}
          title="Open COA import preview"
        >
          <FileInput size={16} />
          Import
        </button>

        <button
          className="icon-button primary"
          form={accountFormId}
          type="submit"
          disabled={
            isBusy
            || (!isWorkbenchOpen && !isInlineCreating)
            || (!isInlineCreating && activeWorkView !== "account")
            || !canSaveAccount
            || !canSubmitAccountForm
          }
          title={accountMode === "edit" ? "Save ledger account" : "Create ledger account"}
        >
          <Save size={16} />
          Save
        </button>
      </header>

      <div className="coa-health-strip" aria-label="Chart of accounts health">
        <span className={reconciliation === null ? "" : coaIssueCount === 0 ? "ok" : "attention"}>
          <small>COA health</small>
          <strong>{coaHealthText}</strong>
        </span>
        <span>
          <small>Checked accounts</small>
          <strong>{reconciliation?.accountCount ?? accounts.length}</strong>
        </span>
        <span>
          <small>Affected</small>
          <strong>{affectedAccountCount}</strong>
        </span>
        <span>
          <small>Repair actions</small>
          <strong>{coaActionCount}</strong>
        </span>
        <span className={
          rangeValidation === null
            ? ""
            : rangeValidation.issueCount === 0
              ? "ok"
              : "attention"
        }>
          <small>Range setup</small>
          <strong>{rangeValidationText}</strong>
        </span>
        <span>
          <small>Active ranges</small>
          <strong>{activeRanges.length}</strong>
        </span>
        <span className="wide" title={selectedRange === null ? "" : formatRangeRule(selectedRange)}>
          <small>Selected range</small>
          <strong>{selectedRange === null ? "None" : formatRangeRule(selectedRange)}</strong>
        </span>
      </div>

      <div className={`coa-classic-layout${isWorkbenchOpen ? " with-workbench" : ""}`}>
        {isInlineCreating && !isWorkbenchOpen && (
          <form
            className="hidden-submit-form"
            id={accountFormId}
            onSubmit={(event) => void handleInlineAccountFormSubmit(event)}
          />
        )}

        <ChartOfAccountsListPanel
          accountMode={accountMode}
          accountFormId={accountFormId}
          accountSaveErrors={accountSaveErrors}
          accountTreeRows={accountTreeRows}
          accountValue={accountValue}
          accounts={accounts}
          activeRanges={activeRanges}
          canSaveAccount={canSaveAccount}
          collapsedAccountIds={collapsedAccountIds}
          collapsedVisibleParentCount={collapsedVisibleParentCount}
          collapsibleAccountIds={collapsibleAccountIds}
          filters={filters}
          inlineCreateAnchorId={inlineCreateAnchorId}
          isBusy={isBusy}
          isParentTreeView={isParentTreeView}
          onCollapseAllTree={handleCollapseAllTree}
          onAccountChange={onAccountChange}
          onCancelInlineCreate={handleCancelAccountCreate}
          onEditAccount={handleEditAccount}
          onExpandAllTree={handleExpandAllTree}
          onFiltersChange={onFiltersChange}
          onRangeRoleSelect={handleSelectRangeRole}
          onSaveAccount={onSaveAccount}
          onStartInlineCreate={handleStartInlineCreate}
          onToggleAccountCollapse={toggleAccountCollapse}
          onToggleAccountStatus={onToggleAccountStatus}
          onToggleRangeSetup={handleToggleRangeSetup}
          onViewAccountActivity={handleViewAccountActivity}
          openVisibleParentCount={openVisibleParentCount}
          postingCount={postingCount}
          reconciliation={reconciliation}
          repairPlan={repairPlan}
          selectedRangeFacts={selectedRangeFacts}
          selectedRangeRole={selectedRangeRole}
          showRangeSetup={isWorkbenchOpen && activeWorkView === "ranges"}
          visibleAccounts={visibleAccounts}
        />

        {isWorkbenchOpen && (
          <ChartOfAccountsWorkWindow
            activeView={activeWorkView}
            activityDisabled={activity === null}
            title={getChartOfAccountsWorkWindowTitle(activeWorkView, accountMode, isInlineCreating)}
            subtitle={getChartOfAccountsWorkWindowSubtitle(
              activeWorkView,
              accountValue,
              selectedRange,
              activityAccount
            )}
            onViewChange={handleWorkWindowViewChange}
            onClose={handleCloseWorkbench}
          >
            <div className="coa-maintain-stack">
              {activeWorkView === "account" && (
                <ChartOfAccountsDetailsPanel
                  accountFormId={accountFormId}
                  accountLevelOptions={accountLevelOptions}
                  accountMode={accountMode}
                  accountSaveErrors={accountSaveErrors}
                  accountValue={accountValue}
                  canSaveAccount={canSaveAccount}
                  inlineCodePlaceholder={inlineCodePlaceholder}
                  inlineCreateParentDisplay={inlineCreateParentDisplay}
                  inlineCreateReady={inlineCreateReady}
                  inlineCreateStatusItems={inlineCreateStatusItems}
                  inlineNamePlaceholder={inlineNamePlaceholder}
                  isBusy={isBusy}
                  isInlineCreating={isInlineCreating}
                  onAccountChange={onAccountChange}
                  onCancelCreate={handleCancelAccountCreate}
                  onSaveAccount={onSaveAccount}
                  onSuggestAccountCode={onSuggestAccountCode}
                  parentAccountDisplay={parentAccountDisplay}
                  selectedRange={selectedRange}
                />
              )}

              {activeWorkView === "ranges" && (
                <ChartOfAccountsRangePanel
                  activeRangeCount={activeRanges.length}
                  canSaveRange={canSaveRange}
                  displayedRangeIssueGroups={displayedRangeIssueGroups}
                  displayedRangeValidationIssues={displayedRangeValidationIssues}
                  filtersRole={filters.role}
                  isBusy={isBusy}
                  onRangeChange={onRangeChange}
                  onRangeSelect={handleSelectRange}
                  onSaveRange={onSaveRange}
                  rangeSetupFacts={rangeSetupFacts}
                  rangeUsageByRole={rangeUsageByRole}
                  rangeValidation={rangeValidation}
                  rangeValidationIssuesByRole={rangeValidationIssuesByRole}
                  rangeValidationText={rangeValidationText}
                  rangeValue={rangeValue}
                  ranges={ranges}
                  selectedRange={selectedRange}
                  selectedRangeFacts={selectedRangeFacts}
                  selectedRangeIssues={selectedRangeIssues}
                  selectedRangeRole={selectedRangeRole}
                />
              )}

              {activeWorkView === "import" && (
                <ChartOfAccountsImportPanel
                  importDelimiter={importDelimiter}
                  importPreview={importPreview}
                  importPreviewStatus={importPreviewStatus}
                  importText={importText}
                  isBusy={isBusy}
                  onImportDelimiterChange={onImportDelimiterChange}
                  onImportTextChange={onImportTextChange}
                  onPreviewImport={onPreviewImport}
                  onUseImportTemplate={onUseImportTemplate}
                />
              )}

              {activeWorkView === "activity" && activity !== null && (
                <ChartOfAccountsActivityPanel
                  account={activityAccount}
                  activity={activity}
                  journalEntries={journalEntries}
                  onViewJournalEntry={onViewJournalEntry}
                />
              )}
            </div>
          </ChartOfAccountsWorkWindow>
        )}
      </div>
    </section>
  );
}

function getChartOfAccountsWorkWindowTitle(
  view: ChartOfAccountsWorkWindowView,
  accountMode: "create" | "edit",
  isCreatingAccount: boolean
): string {
  if (view === "ranges") {
    return "COA Range Setup";
  }

  if (view === "import") {
    return "COA Import Preview";
  }

  if (view === "activity") {
    return "Account Activity";
  }

  if (accountMode === "edit") {
    return "Account Details";
  }

  return isCreatingAccount ? "New Account" : "Account Details";
}

function getChartOfAccountsWorkWindowSubtitle(
  view: ChartOfAccountsWorkWindowView,
  accountValue: LedgerAccountEditorInput,
  selectedRange: AccountCodeRange | null,
  activityAccount: LedgerAccountSummary | undefined
): string {
  if (view === "ranges") {
    return selectedRange === null
      ? "Select and maintain controlled code ranges"
      : `${selectedRange.displayName} / ${selectedRange.rangeStart}-${selectedRange.rangeEnd}`;
  }

  if (view === "import") {
    return "Paste, preview, and validate chart of accounts rows";
  }

  if (view === "activity") {
    return activityAccount === undefined
      ? "Open account activity from the list"
      : `${activityAccount.displayCode} / ${activityAccount.name}`;
  }

  const code = accountValue.code.trim();
  const name = accountValue.name.trim();

  if (code !== "" && name !== "") {
    return `${code} / ${name}`;
  }

  return selectedRange === null
    ? "Select a range or child action to begin"
    : `${selectedRange.displayName} / code generated automatically`;
}
