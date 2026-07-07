import { useEffect, useState } from "react";
import { ApiError, type ApiErrorItem } from "../../../shared/api/apiError";
import {
  applyLedgerAccountRepairAction,
  bootstrapStandardChartOfAccounts,
  closeAccountingPeriod,
  configureAccountingControlSettings,
  configureAccountCodeRange,
  configureDefaultAccountingControlSettings,
  configureOpeningBalanceProfile,
  configureVoucherNumberingRule,
  createAccountingPeriod,
  createLedgerAccount,
  getAccountingControlSettings,
  getAccountCodeRangeValidation,
  getAccountingPeriodCloseJournalPreview,
  getAccountingPeriodCloseReadiness,
  getBalanceSheet,
  getJournalEntry,
  getJournalEntrySourceDocument,
  getLedgerAccountActivity,
  getLedgerAccountReconciliation,
  getLedgerAccountRepairPlan,
  getOpeningBalanceProfile,
  getProfitAndLossStatement,
  getTrialBalance,
  listAccountCodeRanges,
  listAccountingPeriods,
  listJournalEntries,
  listLedgerAccounts,
  listVoucherNumberingRules,
  postManualJournalEntry,
  postOpeningBalanceImport,
  previewChartOfAccountsImportText,
  previewJournalVoucherNumber,
  previewOpeningBalanceImport,
  previewOpeningBalanceImportText,
  reopenAccountingPeriod,
  suggestLedgerAccountCode,
  updateLedgerAccount,
  voidManualJournalEntry
} from "../api/accountingApi";
import {
  accountingCompanyCode,
  accountingCurrencyCode
} from "../constants/accountingConstants";
import type {
  AccountCodeRange,
  AccountCodeRangeValidation,
  AccountingPeriod,
  AccountingControlSettings,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  BalanceSheet,
  BalanceSheetFilters,
  BalanceSheetLine,
  ChartOfAccountsImportTextPreview,
  JournalEntryFilters,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  JournalVoucherNumberPreview,
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  LedgerAccountCodeSuggestion,
  LedgerAccountCreateContext,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  LedgerAccountReconciliation,
  LedgerAccountRepairAction,
  LedgerAccountRepairPlan,
  LedgerAccountSummary,
  OpeningBalanceImportInput,
  OpeningBalanceImportPreview,
  OpeningBalanceImportTemplateFormat,
  OpeningBalanceImportTextPreview,
  OpeningBalanceProfile,
  ProfitAndLossStatement,
  ProfitAndLossStatementFilters,
  ProfitAndLossStatementLine,
  TrialBalance,
  TrialBalanceFilters,
  TrialBalanceLine,
  VoucherNumberingRule,
  VoucherNumberingRuleInput
} from "../types/accountingTypes";
import { toDateInputValue } from "../utils/accountingDates";
import {
  createDefaultAccountingControlSettingsForm,
  createDefaultAccountingPeriodForm,
  createDefaultBalanceSheetFilters,
  createDefaultLedgerAccountEditorForm,
  createDefaultManualJournalEntryForm,
  createDefaultOpeningBalanceImportForm,
  createDefaultProfitAndLossFilters,
  createDefaultTrialBalanceFilters,
  defaultJournalEntryFilters,
  defaultLedgerAccountFilters,
  emptyAccountCodeRangeForm,
  getDefaultLedgerAccountLevel,
  sortAccountCodeRanges,
  toAccountCodeRangeForm,
  toAccountingControlSettingsForm,
  toBalanceSheetActivityFilters,
  toLedgerAccountActivityFilters,
  toLedgerAccountEditorForm,
  toProfitAndLossActivityFilters,
  withAccountingCompanyCode
} from "../utils/accountingForms";
import { getJournalSourceDocumentFallbackLabel } from "../utils/accountingSourceDocuments";

export type AccountingWorkspaceArea =
  | "setup"
  | "controls"
  | "periods"
  | "journal"
  | "reports"
  | "reconcile";

export type AccountingReportArea = "trialBalance" | "profitAndLoss" | "balanceSheet";

type UseAccountingWorkspaceOptions = {
  runAction: (action: () => Promise<void>) => Promise<void>;
  setMessage: (message: string) => void;
  onLedgerSetupChanged?: () => Promise<void>;
  onShowAccounting?: () => void;
};

type AccountingReportRefreshFilters = {
  journalFilters: JournalEntryFilters;
  trialBalanceFilters: TrialBalanceFilters;
  profitAndLossFilters: ProfitAndLossStatementFilters;
  balanceSheetFilters: BalanceSheetFilters;
};

const openingBalanceProfileStorageKey =
  "safarsuite:accounting:opening-balance-profile:v1";

type OpeningBalanceProfileSnapshot = Pick<
  OpeningBalanceImportInput,
  | "entryDate"
  | "currencyCode"
  | "profileFromDate"
  | "profileToDate"
  | "profileStatus"
  | "transactionsAllowed"
  | "profitAndLossCarryForwardAccountId"
>;

export function useAccountingWorkspace({
  runAction,
  setMessage,
  onLedgerSetupChanged,
  onShowAccounting
}: UseAccountingWorkspaceOptions) {
  const [activeArea, setActiveArea] = useState<AccountingWorkspaceArea>("setup");
  const [activeReportArea, setActiveReportArea] =
    useState<AccountingReportArea>("trialBalance");
  const [ledgerAccounts, setLedgerAccounts] = useState<LedgerAccountSummary[]>([]);
  const [ledgerAccountReconciliation, setLedgerAccountReconciliation] =
    useState<LedgerAccountReconciliation | null>(null);
  const [ledgerAccountRepairPlan, setLedgerAccountRepairPlan] =
    useState<LedgerAccountRepairPlan | null>(null);
  const [accountCodeRanges, setAccountCodeRanges] = useState<AccountCodeRange[]>([]);
  const [accountCodeRangeValidation, setAccountCodeRangeValidation] =
    useState<AccountCodeRangeValidation | null>(null);
  const [accountingControlSettings, setAccountingControlSettings] =
    useState<AccountingControlSettings | null>(null);
  const [voucherNumberingRules, setVoucherNumberingRules] =
    useState<VoucherNumberingRule[]>([]);
  const [accountingPeriods, setAccountingPeriods] = useState<AccountingPeriod[]>([]);
  const [accountingPeriodReadiness, setAccountingPeriodReadiness] =
    useState<AccountingPeriodCloseReadiness | null>(null);
  const [accountingPeriodCloseJournalPreview, setAccountingPeriodCloseJournalPreview] =
    useState<AccountingPeriodCloseJournalPreview | null>(null);
  const [journalEntries, setJournalEntries] = useState<JournalEntrySummary[]>([]);
  const [manualJournalVoucherPreview, setManualJournalVoucherPreview] =
    useState<JournalVoucherNumberPreview | null>(null);
  const [focusedJournalEntryId, setFocusedJournalEntryId] = useState("");
  const [focusedJournalEntry, setFocusedJournalEntry] = useState<JournalEntrySummary | null>(null);
  const [journalSourceDocumentsById, setJournalSourceDocumentsById] =
    useState<Record<string, JournalEntrySourceDocument>>({});
  const [ledgerAccountActivity, setLedgerAccountActivity] =
    useState<LedgerAccountActivity | null>(null);
  const [trialBalance, setTrialBalance] = useState<TrialBalance | null>(null);
  const [profitAndLossStatement, setProfitAndLossStatement] =
    useState<ProfitAndLossStatement | null>(null);
  const [balanceSheet, setBalanceSheet] = useState<BalanceSheet | null>(null);
  const [selectedAccountCodeRangeRole, setSelectedAccountCodeRangeRole] = useState("");
  const [accountCodeRangeForm, setAccountCodeRangeForm] =
    useState(emptyAccountCodeRangeForm);
  const [selectedLedgerAccountId, setSelectedLedgerAccountId] = useState("");
  const [ledgerAccountEditorForm, setLedgerAccountEditorForm] =
    useState(createDefaultLedgerAccountEditorForm());
  const [ledgerAccountSaveErrors, setLedgerAccountSaveErrors] = useState<ApiErrorItem[]>([]);
  const [ledgerAccountFilters, setLedgerAccountFilters] = useState(defaultLedgerAccountFilters);
  const [chartOfAccountsImportText, setChartOfAccountsImportText] = useState("");
  const [chartOfAccountsImportDelimiter, setChartOfAccountsImportDelimiter] =
    useState("comma");
  const [chartOfAccountsImportPreview, setChartOfAccountsImportPreview] =
    useState<ChartOfAccountsImportTextPreview | null>(null);
  const [journalEntryFilters, setJournalEntryFilters] = useState(defaultJournalEntryFilters);
  const [trialBalanceFilters, setTrialBalanceFilters] = useState(
    createDefaultTrialBalanceFilters()
  );
  const [profitAndLossFilters, setProfitAndLossFilters] = useState(
    createDefaultProfitAndLossFilters()
  );
  const [balanceSheetFilters, setBalanceSheetFilters] = useState(
    createDefaultBalanceSheetFilters()
  );
  const [accountingControlSettingsForm, setAccountingControlSettingsForm] =
    useState(createDefaultAccountingControlSettingsForm());
  const [voucherNumberingRuleForms, setVoucherNumberingRuleForms] =
    useState<Record<string, VoucherNumberingRuleInput>>({});
  const [accountingPeriodForm, setAccountingPeriodForm] = useState(
    createDefaultAccountingPeriodForm()
  );
  const [manualJournalEntryForm, setManualJournalEntryForm] =
    useState(createDefaultManualJournalEntryForm());
  const [openingBalanceImportForm, setOpeningBalanceImportForm] =
    useState(createOpeningBalanceImportFormWithSavedProfile);
  const [openingBalanceImportPreview, setOpeningBalanceImportPreview] =
    useState<OpeningBalanceImportPreview | null>(null);
  const [openingBalanceImportText, setOpeningBalanceImportText] = useState("");
  const [openingBalanceImportDelimiter, setOpeningBalanceImportDelimiter] =
    useState("comma");
  const [openingBalanceImportTextPreview, setOpeningBalanceImportTextPreview] =
    useState<OpeningBalanceImportTextPreview | null>(null);

  useEffect(() => {
    void refreshAccountingSetup(ledgerAccountFilters);
  }, [ledgerAccountFilters]);

  useEffect(() => {
    void refreshAccountingPeriods();
    void refreshAccountingControls();
  }, []);

  useEffect(() => {
    void refreshJournalEntries(journalEntryFilters);
  }, [journalEntryFilters]);

  useEffect(() => {
    void refreshTrialBalance(trialBalanceFilters);
  }, [trialBalanceFilters]);

  useEffect(() => {
    void refreshProfitAndLossStatement(profitAndLossFilters);
  }, [profitAndLossFilters]);

  useEffect(() => {
    void refreshBalanceSheet(balanceSheetFilters);
  }, [balanceSheetFilters]);

  useEffect(() => {
    saveOpeningBalanceProfileSnapshot(openingBalanceImportForm);
  }, [
    openingBalanceImportForm.entryDate,
    openingBalanceImportForm.currencyCode,
    openingBalanceImportForm.profileFromDate,
    openingBalanceImportForm.profileToDate,
    openingBalanceImportForm.profileStatus,
    openingBalanceImportForm.transactionsAllowed,
    openingBalanceImportForm.profitAndLossCarryForwardAccountId
  ]);

  function handleLedgerAccountFiltersChange(filters: LedgerAccountFilters) {
    setLedgerAccountFilters(withAccountingCompanyCode(filters));
  }

  function handleLedgerAccountEditorFormChange(value: LedgerAccountEditorInput) {
    setLedgerAccountSaveErrors([]);
    setLedgerAccountEditorForm(value);
  }

  async function refreshAccountingSetup(filters = ledgerAccountFilters) {
    await runAction(async () => {
      const accountingFilters = withAccountingCompanyCode(filters);
      const [accounts, ranges, reconciliation, repairPlan, rangeValidation] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);
      const sortedRanges = sortAccountCodeRanges(ranges);
      const selectedRange =
        sortedRanges.find((range) => range.role === selectedAccountCodeRangeRole)
        ?? sortedRanges[0]
        ?? null;

      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRanges(sortedRanges);
      setAccountCodeRangeValidation(rangeValidation);
      setSelectedAccountCodeRangeRole(selectedRange?.role ?? "");
      setAccountCodeRangeForm(
        selectedRange === null ? emptyAccountCodeRangeForm : toAccountCodeRangeForm(selectedRange)
      );

      if (selectedLedgerAccountId === "") {
        setLedgerAccountEditorForm((current) =>
          current.code.trim() !== "" || current.name.trim() !== ""
            ? current
            : createDefaultLedgerAccountEditorForm(selectedRange));
      }
    });
  }

  async function refreshAccountingControls() {
    await runAction(async () => {
      const [settings, voucherRules, openingBalanceProfile] = await Promise.all([
        getAccountingControlSettings(accountingCompanyCode),
        listVoucherNumberingRules(accountingCompanyCode),
        getOpeningBalanceProfile(accountingCompanyCode)
      ]);

      setAccountingControlSettings(settings);
      setAccountingControlSettingsForm(toAccountingControlSettingsForm(settings));
      setVoucherNumberingRules(voucherRules);
      setVoucherNumberingRuleForms(toVoucherNumberingRuleForms(voucherRules));
      setOpeningBalanceImportForm((current) =>
        applyOpeningBalanceProfileToForm(current, openingBalanceProfile)
      );
    });
  }

  async function handleSaveAccountingControls() {
    await runAction(async () => {
      const settings = await configureAccountingControlSettings({
        ...accountingControlSettingsForm,
        companyCode: accountingCompanyCode
      });

      setAccountingControlSettings(settings);
      setAccountingControlSettingsForm(toAccountingControlSettingsForm(settings));
      setMessage(settings.isConfigured
        ? "GL controls configured."
        : "GL controls saved as partial.");
    });
  }

  async function handleUseDefaultAccountingControls() {
    await runAction(async () => {
      const accountingFilters = withAccountingCompanyCode(ledgerAccountFilters);
      const settings = await configureDefaultAccountingControlSettings(accountingCompanyCode);
      const [accounts, ranges, reconciliation, repairPlan, rangeValidation] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);
      const sortedRanges = sortAccountCodeRanges(ranges);
      const selectedRange =
        sortedRanges.find((range) => range.role === selectedAccountCodeRangeRole)
        ?? sortedRanges[0]
        ?? null;

      setAccountingControlSettings(settings);
      setAccountingControlSettingsForm(toAccountingControlSettingsForm(settings));
      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRanges(sortedRanges);
      setAccountCodeRangeValidation(rangeValidation);
      setSelectedAccountCodeRangeRole(selectedRange?.role ?? "");
      setAccountCodeRangeForm(
        selectedRange === null ? emptyAccountCodeRangeForm : toAccountCodeRangeForm(selectedRange)
      );
      await onLedgerSetupChanged?.();
      setMessage("Default MAIN GL controls configured.");
    });
  }

  async function handleBootstrapStandardChartOfAccounts() {
    await runAction(async () => {
      const accountingFilters = withAccountingCompanyCode(ledgerAccountFilters);
      const bootstrap = await bootstrapStandardChartOfAccounts(accountingCompanyCode);
      const [accounts, ranges, reconciliation, repairPlan, rangeValidation] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);
      const sortedRanges = sortAccountCodeRanges(ranges);
      const selectedRange =
        sortedRanges.find((range) => range.role === selectedAccountCodeRangeRole)
        ?? sortedRanges[0]
        ?? null;

      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRanges(sortedRanges);
      setAccountCodeRangeValidation(rangeValidation);
      setSelectedAccountCodeRangeRole(selectedRange?.role ?? "");
      setAccountCodeRangeForm(
        selectedRange === null ? emptyAccountCodeRangeForm : toAccountCodeRangeForm(selectedRange)
      );
      setSelectedLedgerAccountId("");
      setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(selectedRange));
      await onLedgerSetupChanged?.();
      setMessage(
        `Standard COA loaded: ${bootstrap.createdCount} created, ${bootstrap.reusedCount} reused.`
      );
    });
  }

  function handleVoucherNumberingRuleFormChange(
    sourceType: string,
    value: VoucherNumberingRuleInput
  ) {
    setVoucherNumberingRuleForms((current) => ({
      ...current,
      [sourceType]: value
    }));
  }

  async function handleSaveVoucherNumberingRule(sourceType: string) {
    const value = voucherNumberingRuleForms[sourceType];

    if (value === undefined) {
      return;
    }

    await runAction(async () => {
      const updatedRule = await configureVoucherNumberingRule(
        sourceType,
        accountingCompanyCode,
        value);

      setVoucherNumberingRules((current) =>
        current.map((rule) => rule.sourceType === updatedRule.sourceType ? updatedRule : rule));
      setVoucherNumberingRuleForms((current) => ({
        ...current,
        [updatedRule.sourceType]: toVoucherNumberingRuleForm(updatedRule)
      }));
      setMessage(`${updatedRule.sourceType} voucher numbering saved.`);
    });
  }

  async function refreshAccountingPeriods() {
    await runAction(async () => {
      const periods = await listAccountingPeriods(accountingCompanyCode);

      setAccountingPeriods(periods);
      setAccountingPeriodReadiness((current) =>
        current !== null
          && periods.some((period) => period.accountingPeriodId === current.period.accountingPeriodId)
          ? current
          : null);
      setAccountingPeriodCloseJournalPreview((current) =>
        current !== null
          && periods.some((period) => period.accountingPeriodId === current.period.accountingPeriodId)
          ? current
          : null);
      setAccountingPeriodForm((current) => {
        if (
          current.companyCode.trim().toUpperCase() === accountingCompanyCode
          && current.startsOn.trim() !== ""
          && current.endsOn.trim() !== ""
        ) {
          return current;
        }

        return createDefaultAccountingPeriodForm(periods, accountingCompanyCode);
      });
    });
  }

  async function refreshLedgerAccountReconciliation() {
    await runAction(async () => {
      const [reconciliation, repairPlan, rangeValidation] = await Promise.all([
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);

      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRangeValidation(rangeValidation);
      setMessage(
        reconciliation.issueCount === 0
          ? "COA reconciliation passed."
          : `COA reconciliation found ${reconciliation.issueCount} issue(s) and ${repairPlan.actionCount} repair action(s).`
      );
    });
  }

  async function handleApplyLedgerAccountRepairAction(
    ledgerAccountId: string,
    action: LedgerAccountRepairAction
  ) {
    await runAction(async () => {
      const appliedRepair = await applyLedgerAccountRepairAction(ledgerAccountId, {
        companyCode: accountingCompanyCode,
        issueCode: action.issueCode,
        actionCode: action.actionCode,
        confirmed: true
      });
      const [accounts, reconciliation, repairPlan, rangeValidation] = await Promise.all([
        listLedgerAccounts(withAccountingCompanyCode(ledgerAccountFilters)),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);

      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRangeValidation(rangeValidation);

      if (selectedLedgerAccountId === appliedRepair.ledgerAccountId) {
        const updatedAccount = accounts.find((account) =>
          account.ledgerAccountId === appliedRepair.ledgerAccountId
        );

        if (updatedAccount !== undefined) {
          setLedgerAccountEditorForm(toLedgerAccountEditorForm(updatedAccount));
        }
      }

      await onLedgerSetupChanged?.();
      setMessage(`Applied repair: ${appliedRepair.appliedAction.title}.`);
    });
  }

  function handlePrepareNextAccountingPeriod() {
    setAccountingPeriodForm(createDefaultAccountingPeriodForm(
      accountingPeriods,
      accountingCompanyCode
    ));
  }

  async function handleCreateAccountingPeriod() {
    await runAction(async () => {
      const createdPeriod = await createAccountingPeriod({
        ...accountingPeriodForm,
        companyCode: accountingCompanyCode
      });
      const periods = await listAccountingPeriods(accountingCompanyCode);

      setAccountingPeriods(periods);
      setAccountingPeriodReadiness(null);
      setAccountingPeriodCloseJournalPreview(null);
      setAccountingPeriodForm(createDefaultAccountingPeriodForm(periods, accountingCompanyCode));
      setMessage(`Accounting period ${createdPeriod.name} opened.`);
    });
  }

  async function handleCheckAccountingPeriodReadiness(period: AccountingPeriod) {
    await runAction(async () => {
      const readiness = await getAccountingPeriodCloseReadiness(period.accountingPeriodId);

      setAccountingPeriodReadiness(readiness);
      setMessage(readiness.canClose
        ? `${readiness.period.name} is ready to close.`
        : `${readiness.period.name} has close blockers.`);
    });
  }

  async function handlePreviewAccountingCloseJournal(period: AccountingPeriod) {
    await runAction(async () => {
      const preview = await getAccountingPeriodCloseJournalPreview(period.accountingPeriodId);

      setAccountingPeriodCloseJournalPreview(preview);
      setMessage(preview.canGenerate
        ? `${preview.period.name} close journal preview is ready.`
        : `${preview.period.name} close journal preview has blockers.`);
    });
  }

  async function handleCloseAccountingPeriod(period: AccountingPeriod) {
    await runAction(async () => {
      const readiness = await getAccountingPeriodCloseReadiness(period.accountingPeriodId);

      setAccountingPeriodReadiness(readiness);

      if (!readiness.canClose) {
        setMessage(`${readiness.period.name} has close blockers.`);
        return;
      }

      const closedPeriod = await closeAccountingPeriod(period.accountingPeriodId);
      const periods = await listAccountingPeriods(accountingCompanyCode);

      setAccountingPeriods(periods);
      setAccountingPeriodReadiness(null);
      setAccountingPeriodCloseJournalPreview(null);
      setAccountingPeriodForm(createDefaultAccountingPeriodForm(periods, accountingCompanyCode));
      await loadAccountingReportSnapshot(
        getPeriodReportFilters(closedPeriod),
        { focusBalanceSheet: true });
      setMessage(
        `Accounting period ${closedPeriod.name} closed; reports refreshed through ${closedPeriod.endsOn}.`
      );
    });
  }

  async function handleReopenAccountingPeriod(period: AccountingPeriod) {
    await runAction(async () => {
      const reopenedPeriod = await reopenAccountingPeriod(period.accountingPeriodId);
      const [periods, readiness] = await Promise.all([
        listAccountingPeriods(accountingCompanyCode),
        getAccountingPeriodCloseReadiness(reopenedPeriod.accountingPeriodId)
      ]);

      setAccountingPeriods(periods);
      setAccountingPeriodReadiness(readiness);
      setAccountingPeriodCloseJournalPreview(null);
      await loadAccountingReportSnapshot(
        getPeriodReportFilters(reopenedPeriod),
        { focusBalanceSheet: true });
      setMessage(
        `Accounting period ${reopenedPeriod.name} reopened; reports refreshed through ${reopenedPeriod.endsOn}.`
      );
    });
  }

  function handleSelectAccountCodeRange(range: AccountCodeRange) {
    setSelectedAccountCodeRangeRole(range.role);
    setAccountCodeRangeForm(toAccountCodeRangeForm(range));

    if (selectedLedgerAccountId === "") {
      setLedgerAccountSaveErrors([]);
      setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(range));
    }
  }

  async function refreshJournalEntries(filters = journalEntryFilters) {
    await runAction(async () => {
      const entries = await listJournalEntries(filters);
      setJournalEntries(entries);
    });
  }

  async function refreshTrialBalance(filters = trialBalanceFilters) {
    await runAction(async () => {
      const balance = await getTrialBalance(filters);
      setTrialBalance(balance);
    });
  }

  async function refreshProfitAndLossStatement(filters = profitAndLossFilters) {
    await runAction(async () => {
      const statement = await getProfitAndLossStatement(filters);
      setProfitAndLossStatement(statement);
    });
  }

  async function refreshBalanceSheet(filters = balanceSheetFilters) {
    await runAction(async () => {
      const statement = await getBalanceSheet(filters);
      setBalanceSheet(statement);
    });
  }

  async function refreshAccountingReports() {
    await runAction(async () => {
      await loadAccountingReportSnapshot(getCurrentReportFilters());
      setMessage("Accounting reports refreshed.");
    });
  }

  async function loadAccountingReportSnapshot(
    filters: AccountingReportRefreshFilters,
    options: { focusBalanceSheet?: boolean } = {}
  ) {
    const [entries, balance, profitAndLoss, position] = await Promise.all([
      listJournalEntries(filters.journalFilters),
      getTrialBalance(filters.trialBalanceFilters),
      getProfitAndLossStatement(filters.profitAndLossFilters),
      getBalanceSheet(filters.balanceSheetFilters)
    ]);

    setJournalEntryFilters(filters.journalFilters);
    setTrialBalanceFilters(filters.trialBalanceFilters);
    setProfitAndLossFilters(filters.profitAndLossFilters);
    setBalanceSheetFilters(filters.balanceSheetFilters);
    setJournalEntries(entries);
    setTrialBalance(balance);
    setProfitAndLossStatement(profitAndLoss);
    setBalanceSheet(position);

    if (options.focusBalanceSheet === true) {
      setActiveArea("reports");
      setActiveReportArea("balanceSheet");
    }
  }

  function getCurrentReportFilters(): AccountingReportRefreshFilters {
    return {
      journalFilters: journalEntryFilters,
      trialBalanceFilters,
      profitAndLossFilters,
      balanceSheetFilters
    };
  }

  function getPeriodReportFilters(period: AccountingPeriod): AccountingReportRefreshFilters {
    const currencyCode = getReportCurrencyCode();

    return {
      journalFilters: {
        fromDate: period.startsOn,
        toDate: period.endsOn,
        sourceType: ""
      },
      trialBalanceFilters: {
        fromDate: period.startsOn,
        asOfDate: period.endsOn,
        currencyCode
      },
      profitAndLossFilters: {
        fromDate: period.startsOn,
        toDate: period.endsOn,
        currencyCode
      },
      balanceSheetFilters: {
        asOfDate: period.endsOn,
        currencyCode
      }
    };
  }

  function getReportCurrencyCode(): string {
    const configuredCurrencyCode = accountingControlSettings?.baseCurrencyCode.trim().toUpperCase();

    return configuredCurrencyCode === undefined || configuredCurrencyCode === ""
      ? accountingCurrencyCode
      : configuredCurrencyCode;
  }

  async function handleSaveAccountCodeRange() {
    if (selectedAccountCodeRangeRole === "") {
      return;
    }

    await runAction(async () => {
      const accountingFilters = withAccountingCompanyCode(ledgerAccountFilters);
      const savedRange = await configureAccountCodeRange(
        accountingCompanyCode,
        selectedAccountCodeRangeRole,
        accountCodeRangeForm
      );
      const [accounts, ranges, reconciliation, repairPlan, rangeValidation] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);
      const sortedRanges = sortAccountCodeRanges(ranges);
      const selectedRange =
        sortedRanges.find((range) => range.role === savedRange.role) ?? savedRange;

      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRanges(sortedRanges);
      setAccountCodeRangeValidation(rangeValidation);
      setSelectedAccountCodeRangeRole(selectedRange.role);
      setAccountCodeRangeForm(toAccountCodeRangeForm(selectedRange));
      await onLedgerSetupChanged?.();
      setMessage("Accounting setup range saved.");
    });
  }

  async function handleNewLedgerAccount() {
    const selectedRange =
      accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null;

    setLedgerAccountSaveErrors([]);
    setSelectedLedgerAccountId("");
    setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(selectedRange));

    if (selectedRange === null) {
      return;
    }

    await runAction(async () => {
      const suggestion = await suggestLedgerAccountCode(
        selectedRange.role,
        accountingCompanyCode
      );

      setSelectedLedgerAccountId("");
      setLedgerAccountEditorForm((current) => ({
        ...current,
        code: suggestion.suggestedCode,
        type: suggestion.type,
        normalBalance: suggestion.normalBalance,
        level: getDefaultLedgerAccountLevel(selectedRange, suggestion.isPostingAccount),
        isPostingAccount: suggestion.isPostingAccount,
        status: "Active"
      }));
      setMessage(formatLedgerAccountCodeSuggestionMessage("Prepared", suggestion));
    });
  }

  async function handleStartLedgerAccountCreate(context: LedgerAccountCreateContext) {
    const selectedRange =
      accountCodeRanges.find((range) => range.role === context.rangeRole) ?? null;
    const parentAccountId = context.parentAccountId?.trim() ?? "";

    if (selectedRange === null) {
      setMessage("Select an account range before adding a ledger account.");
      return;
    }

    setLedgerAccountSaveErrors([]);
    setSelectedLedgerAccountId("");
    setSelectedAccountCodeRangeRole(selectedRange.role);
    setAccountCodeRangeForm(toAccountCodeRangeForm(selectedRange));
    const defaultForm = createDefaultLedgerAccountEditorForm(selectedRange);

    setLedgerAccountEditorForm({
      ...defaultForm,
      parentAccountId,
      level: context.level ?? defaultForm.level,
      isPostingAccount: context.isPostingAccount ?? defaultForm.isPostingAccount,
      status: "Active"
    });

    await runAction(async () => {
      const suggestion = await suggestLedgerAccountCode(
        selectedRange.role,
        accountingCompanyCode,
        parentAccountId === "" ? undefined : parentAccountId
      );
      const defaultForm = createDefaultLedgerAccountEditorForm(selectedRange);

      setSelectedLedgerAccountId("");
      setSelectedAccountCodeRangeRole(selectedRange.role);
      setAccountCodeRangeForm(toAccountCodeRangeForm(selectedRange));
      setLedgerAccountEditorForm({
        ...defaultForm,
        code: suggestion.suggestedCode,
        type: suggestion.type,
        normalBalance: suggestion.normalBalance,
        parentAccountId: suggestion.parentAccountId ?? parentAccountId,
        level: context.level ?? getDefaultLedgerAccountLevel(selectedRange, suggestion.isPostingAccount),
        isPostingAccount: context.isPostingAccount ?? suggestion.isPostingAccount,
        status: "Active"
      });
      setMessage(formatLedgerAccountCodeSuggestionMessage("Prepared", suggestion));
    });
  }

  function handleEditLedgerAccount(account: LedgerAccountSummary) {
    setLedgerAccountSaveErrors([]);
    setSelectedLedgerAccountId(account.ledgerAccountId);
    setLedgerAccountEditorForm(toLedgerAccountEditorForm(account));
  }

  async function handleSuggestAccountingLedgerAccountCode() {
    if (selectedAccountCodeRangeRole === "") {
      return;
    }

    await runAction(async () => {
      setLedgerAccountSaveErrors([]);
      const selectedRange =
        accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null;
      const parentAccountId = ledgerAccountEditorForm.parentAccountId.trim();
      const suggestion = await suggestLedgerAccountCode(
        selectedAccountCodeRangeRole,
        accountingCompanyCode,
        parentAccountId === "" ? undefined : parentAccountId
      );

      setLedgerAccountEditorForm((current) => ({
        ...current,
        code: suggestion.suggestedCode,
        type: suggestion.type,
        normalBalance: suggestion.normalBalance,
        parentAccountId: suggestion.parentAccountId ?? current.parentAccountId,
        level: current.parentAccountId.trim() !== "" && current.level.trim() !== ""
          ? current.level
          : getDefaultLedgerAccountLevel(selectedRange, suggestion.isPostingAccount),
        isPostingAccount: suggestion.isPostingAccount,
        status: "Active"
      }));
      setMessage(formatLedgerAccountCodeSuggestionMessage("Suggested", suggestion));
    });
  }

  async function handleSaveLedgerAccount() {
    setLedgerAccountSaveErrors([]);

    await runAction(async () => {
      const wasCreate = selectedLedgerAccountId === "";
      let savedLedgerAccountId = selectedLedgerAccountId;

      try {
        if (wasCreate) {
          const createdAccount = await createLedgerAccount(ledgerAccountEditorForm);
          savedLedgerAccountId = createdAccount.ledgerAccountId;
        } else {
          await updateLedgerAccount(selectedLedgerAccountId, ledgerAccountEditorForm);
        }
      } catch (caughtError) {
        setLedgerAccountSaveErrors(toLedgerAccountSaveErrors(caughtError));
        throw caughtError;
      }

      const accountingFilters = withAccountingCompanyCode(ledgerAccountFilters);
      const allAccountingFilters = withAccountingCompanyCode(defaultLedgerAccountFilters);
      const [filteredAccounts, allAccounts, ranges, reconciliation, repairPlan, rangeValidation] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listLedgerAccounts(allAccountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);
      const savedAccount =
        allAccounts.find((account) => account.ledgerAccountId === savedLedgerAccountId) ?? null;
      const shouldShowAllAccounts =
        savedAccount !== null
        && !filteredAccounts.some((account) => account.ledgerAccountId === savedAccount.ledgerAccountId);
      const accounts = shouldShowAllAccounts ? allAccounts : filteredAccounts;
      const savedAccountVisibilityFilters = withAccountingCompanyCode({
        ...defaultLedgerAccountFilters,
        companyCode: ledgerAccountFilters.companyCode,
        viewMode: ledgerAccountFilters.viewMode
      });
      const sortedRanges = sortAccountCodeRanges(ranges);
      const savedAccountRangeRole = savedAccount?.rangeRole ?? "";
      const selectedRange =
        sortedRanges.find((range) => savedAccountRangeRole !== "" && range.role === savedAccountRangeRole)
        ?? sortedRanges.find((range) => range.role === selectedAccountCodeRangeRole)
        ?? sortedRanges[0]
        ?? null;

      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRanges(sortedRanges);
      setAccountCodeRangeValidation(rangeValidation);
      setSelectedAccountCodeRangeRole(selectedRange?.role ?? "");
      setAccountCodeRangeForm(
        selectedRange === null ? emptyAccountCodeRangeForm : toAccountCodeRangeForm(selectedRange)
      );

      if (shouldShowAllAccounts) {
        setLedgerAccountFilters(savedAccountVisibilityFilters);
      }

      if (savedAccount === null) {
        setSelectedLedgerAccountId("");
        setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(selectedRange));
      } else {
        setSelectedLedgerAccountId(savedAccount.ledgerAccountId);
        setLedgerAccountEditorForm(toLedgerAccountEditorForm(savedAccount));
      }

      await onLedgerSetupChanged?.();
      setLedgerAccountSaveErrors([]);
      setMessage(wasCreate ? "Ledger account created and selected." : "Ledger account saved.");
    });
  }

  async function handlePreviewChartOfAccountsImport() {
    await runAction(async () => {
      const preview = await previewChartOfAccountsImportText(
        accountingCompanyCode,
        chartOfAccountsImportText,
        chartOfAccountsImportDelimiter);

      setChartOfAccountsImportPreview(preview);
      setMessage(
        `COA preview: ${preview.insertCount} insert, ${preview.updateCount} update, ${preview.rejectCount} reject.`
      );
    });
  }

  function handleUseChartOfAccountsImportTemplate() {
    setChartOfAccountsImportDelimiter("comma");
    setChartOfAccountsImportPreview(null);
    setChartOfAccountsImportText([
      "Acc Type,Account Code,Parent Code,Account Name,CUR",
      "Header,10000,,ASSETS,PKR",
      "Control,15100,10000,ACCOUNTS RECEIVABLE,PKR",
      "Subsidiary,15100-0001,15100,Demo Client Receivable,PKR",
      "Total,19999,10000,TOTAL ASSETS,PKR"
    ].join("\n"));
  }

  async function handleToggleLedgerAccountStatus(account: LedgerAccountSummary) {
    await runAction(async () => {
      const nextStatus = account.status === "Active" ? "Inactive" : "Active";

      await updateLedgerAccount(account.ledgerAccountId, {
        ...toLedgerAccountEditorForm(account),
        status: nextStatus
      });

      const [accounts, reconciliation, repairPlan, rangeValidation] = await Promise.all([
        listLedgerAccounts(withAccountingCompanyCode(ledgerAccountFilters)),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);
      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setAccountCodeRangeValidation(rangeValidation);

      if (selectedLedgerAccountId === account.ledgerAccountId) {
        const updatedAccount = accounts.find((item) =>
          item.ledgerAccountId === account.ledgerAccountId
        );

        if (updatedAccount === undefined) {
          setSelectedLedgerAccountId("");
          setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(
            accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null
          ));
        } else {
          setLedgerAccountEditorForm(toLedgerAccountEditorForm(updatedAccount));
        }
      }

      setMessage(nextStatus === "Active"
        ? "Ledger account reactivated."
        : "Ledger account deactivated.");
    });
  }

  async function handleViewLedgerAccountActivity(account: LedgerAccountSummary) {
    await runAction(async () => {
      const activity = await getLedgerAccountActivity(
        account.ledgerAccountId,
        toLedgerAccountActivityFilters(trialBalanceFilters));

      setLedgerAccountActivity(activity);
      setMessage(`Loaded ${account.displayCode} activity.`);
    });
  }

  async function handleViewTrialBalanceAccountActivity(line: TrialBalanceLine) {
    await runAction(async () => {
      const activity = await getLedgerAccountActivity(
        line.ledgerAccountId,
        toLedgerAccountActivityFilters(trialBalanceFilters));

      setLedgerAccountActivity(activity);
      setActiveArea("setup");
      setMessage(`Loaded ${line.code} activity from trial balance.`);
    });
  }

  async function handleViewProfitAndLossAccountActivity(line: ProfitAndLossStatementLine) {
    await runAction(async () => {
      const activity = await getLedgerAccountActivity(
        line.ledgerAccountId,
        toProfitAndLossActivityFilters(profitAndLossFilters));

      setLedgerAccountActivity(activity);
      setActiveArea("setup");
      setMessage(`Loaded ${line.code} activity from profit and loss.`);
    });
  }

  async function handleViewBalanceSheetAccountActivity(line: BalanceSheetLine) {
    if (line.ledgerAccountId === null || line.ledgerAccountId === undefined) {
      return;
    }

    const ledgerAccountId = line.ledgerAccountId;

    await runAction(async () => {
      const activity = await getLedgerAccountActivity(
        ledgerAccountId,
        toBalanceSheetActivityFilters(balanceSheetFilters));

      setLedgerAccountActivity(activity);
      setActiveArea("setup");
      setMessage(`Loaded ${line.code} activity from balance sheet.`);
    });
  }

  async function viewJournalEntryById(journalEntryId: string) {
    const normalizedJournalEntryId = journalEntryId.trim();

    if (normalizedJournalEntryId === "") {
      return;
    }

    await runAction(async () => {
      const entry = await getJournalEntry(normalizedJournalEntryId);

      setFocusedJournalEntryId(entry.journalEntryId);
      setFocusedJournalEntry(entry);
      await loadJournalSourceDocument(entry);
      setJournalEntryFilters((current) => ({
        ...current,
        fromDate: entry.entryDate,
        toDate: entry.entryDate,
        sourceType: ""
      }));
      setActiveArea("journal");
      onShowAccounting?.();
      setMessage(`Opened journal ${entry.sourceReference ?? entry.journalEntryId}.`);
    });
  }

  async function handleFocusJournalEntry(journalEntryId: string) {
    if (journalEntryId === "" || focusedJournalEntryId === journalEntryId) {
      setFocusedJournalEntryId("");
      setFocusedJournalEntry(null);
      return;
    }

    await runAction(async () => {
      const entry = await getJournalEntry(journalEntryId);

      setFocusedJournalEntryId(entry.journalEntryId);
      setFocusedJournalEntry(entry);
      await loadJournalSourceDocument(entry);
      setMessage(`Focused journal ${entry.sourceReference ?? entry.journalEntryId}.`);
    });
  }

  async function loadJournalSourceDocument(
    entry: JournalEntrySummary
  ): Promise<JournalEntrySourceDocument | null> {
    if (getJournalSourceDocumentFallbackLabel(entry) === null) {
      return null;
    }

    const sourceDocument = await getJournalEntrySourceDocument(entry.journalEntryId);
    rememberJournalSourceDocument(sourceDocument);

    return sourceDocument;
  }

  function rememberJournalSourceDocument(sourceDocument: JournalEntrySourceDocument) {
    setJournalSourceDocumentsById((current) => ({
      ...current,
      [sourceDocument.journalEntryId]: sourceDocument
    }));
  }

  async function handleViewJournalEntryFromActivity(line: LedgerAccountActivityLine) {
    await runAction(async () => {
      const entry = await getJournalEntry(line.journalEntryId);

      setFocusedJournalEntryId(entry.journalEntryId);
      setFocusedJournalEntry(entry);
      await loadJournalSourceDocument(entry);
      setJournalEntryFilters((current) => ({
        ...current,
        fromDate: line.entryDate,
        toDate: line.entryDate,
        sourceType: ""
      }));
      setActiveArea("journal");
      setMessage(`Focused journal ${entry.sourceReference ?? entry.journalEntryId}.`);
    });
  }

  async function handlePostManualJournalEntry() {
    await runAction(async () => {
      const postedEntry = await postManualJournalEntry(manualJournalEntryForm);
      const [entries, balance, profitAndLoss, position] = await Promise.all([
        listJournalEntries(journalEntryFilters),
        getTrialBalance(trialBalanceFilters),
        getProfitAndLossStatement(profitAndLossFilters),
        getBalanceSheet(balanceSheetFilters)
      ]);

      setJournalEntries(entries);
      setTrialBalance(balance);
      setProfitAndLossStatement(profitAndLoss);
      setBalanceSheet(position);
      setManualJournalEntryForm(createDefaultManualJournalEntryForm());
      setManualJournalVoucherPreview(null);
      setFocusedJournalEntryId(postedEntry.journalEntryId);
      setFocusedJournalEntry(postedEntry);
      await refreshSelectedLedgerAccountActivity();
      setMessage(`Manual journal ${postedEntry.sourceReference ?? postedEntry.journalEntryId} posted.`);
    });
  }

  async function handleSuggestManualJournalVoucherNumber() {
    await runAction(async () => {
      const preview = await previewJournalVoucherNumber("Manual", manualJournalEntryForm.entryDate);

      setManualJournalVoucherPreview(preview);
      setManualJournalEntryForm((current) => ({
        ...current,
        sourceReference: preview.reference
      }));
      setMessage(`Prepared voucher ${preview.reference}.`);
    });
  }

  async function handlePreviewOpeningBalanceImport() {
    await runAction(async () => {
      const preview = await previewOpeningBalanceImport(openingBalanceImportForm);

      setOpeningBalanceImportPreview(preview);
      setOpeningBalanceImportTextPreview(null);
      setOpeningBalanceImportForm((current) => ({
        ...current,
        sourceReference: preview.sourceReference
      }));
      setMessage(preview.canPost
        ? `Opening balance dry-run ${preview.sourceReference} is balanced.`
        : `Opening balance dry-run has ${preview.blockers.length} blocker(s).`);
    });
  }

  async function handlePreviewOpeningBalanceImportText() {
    await runAction(async () => {
      const textPreview = await previewOpeningBalanceImportText(
        openingBalanceImportForm,
        openingBalanceImportText,
        openingBalanceImportDelimiter);

      setOpeningBalanceImportTextPreview(textPreview);
      setOpeningBalanceImportPreview(textPreview.preview);
      setOpeningBalanceImportForm((current) => ({
        ...current,
        sourceReference: textPreview.preview.sourceReference,
        lines: textPreview.preview.lines.map((line) => ({
          accountCode: line.accountCode,
          debit: line.debit === 0 ? "" : line.debit.toString(),
          credit: line.credit === 0 ? "" : line.credit.toString(),
          description: line.description ?? ""
        }))
      }));
      setMessage(textPreview.preview.canPost
        ? `Parsed ${textPreview.parsedLineCount} opening balance line(s).`
        : `Parsed text has ${textPreview.preview.blockers.length} blocker(s).`);
    });
  }

  function handleUseOpeningBalanceImportTemplate(format: OpeningBalanceImportTemplateFormat = "standard") {
    setOpeningBalanceImportDelimiter("comma");
    setOpeningBalanceImportText(createOpeningBalanceImportTemplate(format));
    setOpeningBalanceImportPreview(null);
    setOpeningBalanceImportTextPreview(null);
  }

  async function handleSaveOpeningBalanceProfile() {
    await runAction(async () => {
      const profile = await configureOpeningBalanceProfile(
        accountingCompanyCode,
        openingBalanceImportForm);

      setOpeningBalanceImportForm((current) =>
        applyOpeningBalanceProfileToForm(current, profile)
      );
      setMessage("Opening balance profile saved.");
    });
  }

  async function handlePostOpeningBalanceImport() {
    await runAction(async () => {
      const postedEntry = await postOpeningBalanceImport(openingBalanceImportForm);
      const [entries, balance, profitAndLoss, position] = await Promise.all([
        listJournalEntries(journalEntryFilters),
        getTrialBalance(trialBalanceFilters),
        getProfitAndLossStatement(profitAndLossFilters),
        getBalanceSheet(balanceSheetFilters)
      ]);

      setJournalEntries(entries);
      setTrialBalance(balance);
      setProfitAndLossStatement(profitAndLoss);
      setBalanceSheet(position);
      setOpeningBalanceImportForm((current) =>
        createOpeningBalanceImportFormAfterPost(current)
      );
      setOpeningBalanceImportPreview(null);
      setOpeningBalanceImportText("");
      setOpeningBalanceImportTextPreview(null);
      setFocusedJournalEntryId(postedEntry.journalEntryId);
      setFocusedJournalEntry(postedEntry);
      await loadJournalSourceDocument(postedEntry);
      await refreshSelectedLedgerAccountActivity();
      setMessage(`Opening balance journal ${postedEntry.sourceReference ?? postedEntry.journalEntryId} posted.`);
    });
  }

  async function handleVoidManualJournalEntry(entry: JournalEntrySummary) {
    await runAction(async () => {
      const result = await voidManualJournalEntry(entry.journalEntryId, {
        voidDate: toDateInputValue(new Date()),
        reason: "Voided from GL workbench"
      });
      const [entries, balance, profitAndLoss, position, reversalEntry] = await Promise.all([
        listJournalEntries(journalEntryFilters),
        getTrialBalance(trialBalanceFilters),
        getProfitAndLossStatement(profitAndLossFilters),
        getBalanceSheet(balanceSheetFilters),
        getJournalEntry(result.reversalJournalEntryId)
      ]);

      setJournalEntries(entries);
      setTrialBalance(balance);
      setProfitAndLossStatement(profitAndLoss);
      setBalanceSheet(position);
      setFocusedJournalEntryId(result.reversalJournalEntryId);
      setFocusedJournalEntry(reversalEntry);
      await refreshSelectedLedgerAccountActivity();
      setMessage(`Manual journal reversal ${result.reversalJournalEntryId} posted.`);
    });
  }

  async function refreshSelectedLedgerAccountActivity() {
    if (ledgerAccountActivity === null) {
      return;
    }

    const activity = await getLedgerAccountActivity(
      ledgerAccountActivity.ledgerAccountId,
      toLedgerAccountActivityFilters(trialBalanceFilters));

    setLedgerAccountActivity(activity);
  }

  const openPeriodCount = accountingPeriods.filter((period) =>
    period.status.toLowerCase() === "open"
  ).length;
  const postingAccountCount = ledgerAccounts.filter((account) =>
    account.isPostingAccount && account.status === "Active"
  ).length;

  return {
    activeArea,
    setActiveArea,
    activeReportArea,
    setActiveReportArea,
    ledgerAccounts,
    ledgerAccountReconciliation,
    ledgerAccountRepairPlan,
    accountCodeRanges,
    accountCodeRangeValidation,
    accountingControlSettings,
    voucherNumberingRules,
    accountingPeriods,
    accountingPeriodReadiness,
    accountingPeriodCloseJournalPreview,
    journalEntries,
    manualJournalVoucherPreview,
    focusedJournalEntryId,
    focusedJournalEntry,
    journalSourceDocumentsById,
    ledgerAccountActivity,
    trialBalance,
    profitAndLossStatement,
    balanceSheet,
    selectedAccountCodeRangeRole,
    accountCodeRangeForm,
    selectedLedgerAccountId,
    ledgerAccountEditorForm,
    ledgerAccountSaveErrors,
    ledgerAccountFilters,
    chartOfAccountsImportText,
    chartOfAccountsImportDelimiter,
    chartOfAccountsImportPreview,
    journalEntryFilters,
    trialBalanceFilters,
    profitAndLossFilters,
    balanceSheetFilters,
    accountingControlSettingsForm,
    voucherNumberingRuleForms,
    accountingPeriodForm,
    manualJournalEntryForm,
    openingBalanceImportForm,
    openingBalanceImportPreview,
    openingBalanceImportText,
    openingBalanceImportDelimiter,
    openingBalanceImportTextPreview,
    openPeriodCount,
    postingAccountCount,
    handleLedgerAccountFiltersChange,
    setAccountCodeRangeForm,
    handleLedgerAccountEditorFormChange,
    setChartOfAccountsImportText,
    setChartOfAccountsImportDelimiter,
    setJournalEntryFilters,
    setTrialBalanceFilters,
    setProfitAndLossFilters,
    setBalanceSheetFilters,
    setAccountingControlSettingsForm,
    handleVoucherNumberingRuleFormChange,
    setAccountingPeriodForm,
    setManualJournalEntryForm,
    setOpeningBalanceImportForm,
    setOpeningBalanceImportText,
    setOpeningBalanceImportDelimiter,
    refreshAccountingSetup,
    refreshAccountingControls,
    refreshAccountingPeriods,
    refreshLedgerAccountReconciliation,
    refreshJournalEntries,
    refreshAccountingReports,
    refreshTrialBalance,
    refreshProfitAndLossStatement,
    refreshBalanceSheet,
    handleSaveAccountingControls,
    handleUseDefaultAccountingControls,
    handleBootstrapStandardChartOfAccounts,
    handleSaveVoucherNumberingRule,
    handlePrepareNextAccountingPeriod,
    handleCreateAccountingPeriod,
    handleCheckAccountingPeriodReadiness,
    handlePreviewAccountingCloseJournal,
    handleCloseAccountingPeriod,
    handleReopenAccountingPeriod,
    handleSelectAccountCodeRange,
    handleSaveAccountCodeRange,
    handleNewLedgerAccount,
    handleStartLedgerAccountCreate,
    handleEditLedgerAccount,
    handleSuggestAccountingLedgerAccountCode,
    handleSaveLedgerAccount,
    handleApplyLedgerAccountRepairAction,
    handlePreviewChartOfAccountsImport,
    handleUseChartOfAccountsImportTemplate,
    handleToggleLedgerAccountStatus,
    handleViewLedgerAccountActivity,
    handleViewTrialBalanceAccountActivity,
    handleViewProfitAndLossAccountActivity,
    handleViewBalanceSheetAccountActivity,
    viewJournalEntryById,
    handleFocusJournalEntry,
    handleViewJournalEntryFromActivity,
    handlePostManualJournalEntry,
    handleSuggestManualJournalVoucherNumber,
    handlePreviewOpeningBalanceImport,
    handlePreviewOpeningBalanceImportText,
    handleUseOpeningBalanceImportTemplate,
    handleSaveOpeningBalanceProfile,
    handlePostOpeningBalanceImport,
    handleVoidManualJournalEntry,
    rememberJournalSourceDocument,
    loadJournalSourceDocument
  };
}

function createOpeningBalanceImportFormWithSavedProfile(): OpeningBalanceImportInput {
  const defaultForm = createDefaultOpeningBalanceImportForm();
  const savedProfile = readOpeningBalanceProfileSnapshot(defaultForm);

  return savedProfile === null
    ? defaultForm
    : {
        ...defaultForm,
        ...savedProfile
      };
}

function createOpeningBalanceImportFormAfterPost(
  current: OpeningBalanceImportInput
): OpeningBalanceImportInput {
  const defaultForm = createDefaultOpeningBalanceImportForm();

  return {
    ...defaultForm,
    entryDate: current.entryDate,
    currencyCode: current.currencyCode,
    memo: current.memo,
    profileFromDate: current.profileFromDate,
    profileToDate: current.profileToDate,
    profileStatus: current.profileStatus,
    transactionsAllowed: current.transactionsAllowed,
    profitAndLossCarryForwardAccountId: current.profitAndLossCarryForwardAccountId
  };
}

function applyOpeningBalanceProfileToForm(
  current: OpeningBalanceImportInput,
  profile: OpeningBalanceProfile
): OpeningBalanceImportInput {
  return {
    ...current,
    profileFromDate: profile.fiscalYearFrom,
    profileToDate: profile.fiscalYearTo,
    profileStatus: normalizeOpeningBalanceProfileStatus(profile.status),
    transactionsAllowed: profile.transactionsAllowed,
    profitAndLossCarryForwardAccountId: profile.profitAndLossCarryForwardAccountId ?? ""
  };
}

function normalizeOpeningBalanceProfileStatus(value: string): "open" | "closed" {
  return value.trim().toLowerCase() === "closed" ? "closed" : "open";
}

function saveOpeningBalanceProfileSnapshot(value: OpeningBalanceImportInput) {
  if (typeof window === "undefined") {
    return;
  }

  const snapshot: OpeningBalanceProfileSnapshot = {
    entryDate: value.entryDate,
    currencyCode: value.currencyCode,
    profileFromDate: value.profileFromDate,
    profileToDate: value.profileToDate,
    profileStatus: value.profileStatus,
    transactionsAllowed: value.transactionsAllowed,
    profitAndLossCarryForwardAccountId: value.profitAndLossCarryForwardAccountId
  };

  try {
    window.localStorage.setItem(
      openingBalanceProfileStorageKey,
      JSON.stringify(snapshot)
    );
  } catch {
    // Browser storage is a convenience here; posting should not depend on it.
  }
}

function readOpeningBalanceProfileSnapshot(
  fallback: OpeningBalanceImportInput
): OpeningBalanceProfileSnapshot | null {
  if (typeof window === "undefined") {
    return null;
  }

  try {
    const rawValue = window.localStorage.getItem(openingBalanceProfileStorageKey);

    if (rawValue === null) {
      return null;
    }

    const parsedValue = JSON.parse(rawValue) as Partial<OpeningBalanceProfileSnapshot>;

    return {
      entryDate: normalizeDateSnapshotValue(parsedValue.entryDate, fallback.entryDate),
      currencyCode: normalizeTextSnapshotValue(parsedValue.currencyCode, fallback.currencyCode),
      profileFromDate: normalizeDateSnapshotValue(
        parsedValue.profileFromDate,
        fallback.profileFromDate
      ),
      profileToDate: normalizeDateSnapshotValue(
        parsedValue.profileToDate,
        fallback.profileToDate
      ),
      profileStatus: parsedValue.profileStatus === "closed" ? "closed" : "open",
      transactionsAllowed: typeof parsedValue.transactionsAllowed === "boolean"
        ? parsedValue.transactionsAllowed
        : fallback.transactionsAllowed,
      profitAndLossCarryForwardAccountId: normalizeTextSnapshotValue(
        parsedValue.profitAndLossCarryForwardAccountId,
        fallback.profitAndLossCarryForwardAccountId
      )
    };
  } catch {
    return null;
  }
}

function normalizeDateSnapshotValue(value: unknown, fallback: string): string {
  return typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? value
    : fallback;
}

function normalizeTextSnapshotValue(value: unknown, fallback: string): string {
  return typeof value === "string" ? value : fallback;
}

function createOpeningBalanceImportTemplate(format: OpeningBalanceImportTemplateFormat): string {
  switch (format) {
    case "legacy-access":
      return "ACC_CODE,DAMT,CAMT,REMARKS,CUR\n";
    case "legacy-sql":
      return "DCO_COA3_CODE,DCO_DBT_AMT,DCO_CRD_AMT,DCO_CUR_CODE,DCO_DP_CODE\n";
    case "standard":
    default:
      return "accountCode,debit,credit,description\n";
  }
}

function formatLedgerAccountCodeSuggestionMessage(
  verb: string,
  suggestion: LedgerAccountCodeSuggestion
): string {
  const parentCode = suggestion.parentAccountCode?.trim() ?? suggestion.parentCode?.trim() ?? "";
  const parentName = suggestion.parentAccountName?.trim() ?? "";

  if (parentCode === "") {
    return `${verb} ${suggestion.displayCode}.`;
  }

  return parentName === ""
    ? `${verb} ${suggestion.displayCode} under ${parentCode}.`
    : `${verb} ${suggestion.displayCode} under ${parentCode} / ${parentName}.`;
}

function toLedgerAccountSaveErrors(caughtError: unknown): ApiErrorItem[] {
  if (caughtError instanceof ApiError) {
    return caughtError.errors.length > 0
      ? caughtError.errors
      : [{ code: "error", message: caughtError.message, target: null }];
  }

  if (caughtError instanceof Error) {
    return [{ code: "error", message: caughtError.message, target: null }];
  }

  return [{ code: "error", message: "Ledger account could not be saved.", target: null }];
}

function toVoucherNumberingRuleForms(
  rules: VoucherNumberingRule[]
): Record<string, VoucherNumberingRuleInput> {
  return rules.reduce<Record<string, VoucherNumberingRuleInput>>((forms, rule) => {
    forms[rule.sourceType] = toVoucherNumberingRuleForm(rule);
    return forms;
  }, {});
}

function toVoucherNumberingRuleForm(rule: VoucherNumberingRule): VoucherNumberingRuleInput {
  return {
    prefix: rule.prefix,
    numberPaddingWidth: rule.numberPaddingWidth.toString(),
    isActive: rule.isActive
  };
}
