import { useEffect, useState } from "react";
import {
  closeAccountingPeriod,
  configureAccountingControlSettings,
  configureAccountCodeRange,
  configureDefaultAccountingControlSettings,
  createAccountingPeriod,
  createLedgerAccount,
  getAccountingControlSettings,
  getAccountingPeriodCloseJournalPreview,
  getAccountingPeriodCloseReadiness,
  getBalanceSheet,
  getJournalEntry,
  getJournalEntrySourceDocument,
  getLedgerAccountActivity,
  getLedgerAccountReconciliation,
  getLedgerAccountRepairPlan,
  getProfitAndLossStatement,
  getTrialBalance,
  listAccountCodeRanges,
  listAccountingPeriods,
  listJournalEntries,
  listLedgerAccounts,
  postManualJournalEntry,
  previewJournalVoucherNumber,
  previewOpeningBalanceImport,
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
  AccountingPeriod,
  AccountingControlSettings,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  BalanceSheet,
  BalanceSheetFilters,
  BalanceSheetLine,
  JournalEntryFilters,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  JournalVoucherNumberPreview,
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  LedgerAccountFilters,
  LedgerAccountReconciliation,
  LedgerAccountRepairPlan,
  LedgerAccountSummary,
  OpeningBalanceImportPreview,
  ProfitAndLossStatement,
  ProfitAndLossStatementFilters,
  ProfitAndLossStatementLine,
  TrialBalance,
  TrialBalanceFilters,
  TrialBalanceLine
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
  const [accountingControlSettings, setAccountingControlSettings] =
    useState<AccountingControlSettings | null>(null);
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
  const [ledgerAccountFilters, setLedgerAccountFilters] = useState(defaultLedgerAccountFilters);
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
  const [accountingPeriodForm, setAccountingPeriodForm] = useState(
    createDefaultAccountingPeriodForm()
  );
  const [manualJournalEntryForm, setManualJournalEntryForm] =
    useState(createDefaultManualJournalEntryForm());
  const [openingBalanceImportForm, setOpeningBalanceImportForm] =
    useState(createDefaultOpeningBalanceImportForm());
  const [openingBalanceImportPreview, setOpeningBalanceImportPreview] =
    useState<OpeningBalanceImportPreview | null>(null);

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

  function handleLedgerAccountFiltersChange(filters: LedgerAccountFilters) {
    setLedgerAccountFilters(withAccountingCompanyCode(filters));
  }

  async function refreshAccountingSetup(filters = ledgerAccountFilters) {
    await runAction(async () => {
      const accountingFilters = withAccountingCompanyCode(filters);
      const [accounts, ranges, reconciliation, repairPlan] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode)
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
      const settings = await getAccountingControlSettings(accountingCompanyCode);

      setAccountingControlSettings(settings);
      setAccountingControlSettingsForm(toAccountingControlSettingsForm(settings));
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
      const [accounts, ranges, reconciliation, repairPlan] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode)
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
      setSelectedAccountCodeRangeRole(selectedRange?.role ?? "");
      setAccountCodeRangeForm(
        selectedRange === null ? emptyAccountCodeRangeForm : toAccountCodeRangeForm(selectedRange)
      );
      await onLedgerSetupChanged?.();
      setMessage("Default MAIN GL controls configured.");
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
      const [reconciliation, repairPlan] = await Promise.all([
        getLedgerAccountReconciliation(accountingCompanyCode),
        getLedgerAccountRepairPlan(accountingCompanyCode)
      ]);

      setLedgerAccountReconciliation(reconciliation);
      setLedgerAccountRepairPlan(repairPlan);
      setMessage(
        reconciliation.issueCount === 0
          ? "COA reconciliation passed."
          : `COA reconciliation found ${reconciliation.issueCount} issue(s) and ${repairPlan.actionCount} repair action(s).`
      );
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
      const [accounts, ranges, reconciliation] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode)
      ]);
      const sortedRanges = sortAccountCodeRanges(ranges);
      const selectedRange =
        sortedRanges.find((range) => range.role === savedRange.role) ?? savedRange;

      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setAccountCodeRanges(sortedRanges);
      setSelectedAccountCodeRangeRole(selectedRange.role);
      setAccountCodeRangeForm(toAccountCodeRangeForm(selectedRange));
      await onLedgerSetupChanged?.();
      setMessage("Accounting setup range saved.");
    });
  }

  async function handleNewLedgerAccount() {
    const selectedRange =
      accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null;

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
      setMessage(`Prepared ${suggestion.displayCode}.`);
    });
  }

  function handleEditLedgerAccount(account: LedgerAccountSummary) {
    setSelectedLedgerAccountId(account.ledgerAccountId);
    setLedgerAccountEditorForm(toLedgerAccountEditorForm(account));
  }

  async function handleSuggestAccountingLedgerAccountCode() {
    if (selectedAccountCodeRangeRole === "") {
      return;
    }

    await runAction(async () => {
      const selectedRange =
        accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null;
      const suggestion = await suggestLedgerAccountCode(
        selectedAccountCodeRangeRole,
        accountingCompanyCode
      );

      setLedgerAccountEditorForm((current) => ({
        ...current,
        code: suggestion.suggestedCode,
        type: suggestion.type,
        normalBalance: suggestion.normalBalance,
        level: getDefaultLedgerAccountLevel(selectedRange, suggestion.isPostingAccount),
        isPostingAccount: suggestion.isPostingAccount,
        status: "Active"
      }));
      setMessage(`Suggested ${suggestion.displayCode}.`);
    });
  }

  async function handleSaveLedgerAccount() {
    await runAction(async () => {
      if (selectedLedgerAccountId === "") {
        await createLedgerAccount(ledgerAccountEditorForm);
      } else {
        await updateLedgerAccount(selectedLedgerAccountId, ledgerAccountEditorForm);
      }

      const accountingFilters = withAccountingCompanyCode(ledgerAccountFilters);
      const [accounts, ranges, reconciliation] = await Promise.all([
        listLedgerAccounts(accountingFilters),
        listAccountCodeRanges(accountingCompanyCode),
        getLedgerAccountReconciliation(accountingCompanyCode)
      ]);
      const sortedRanges = sortAccountCodeRanges(ranges);
      const selectedRange =
        sortedRanges.find((range) => range.role === selectedAccountCodeRangeRole)
        ?? sortedRanges[0]
        ?? null;

      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);
      setAccountCodeRanges(sortedRanges);
      setSelectedAccountCodeRangeRole(selectedRange?.role ?? "");
      setAccountCodeRangeForm(
        selectedRange === null ? emptyAccountCodeRangeForm : toAccountCodeRangeForm(selectedRange)
      );

      if (selectedLedgerAccountId === "") {
        setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(selectedRange));
      } else {
        const updatedAccount = accounts.find((account) =>
          account.ledgerAccountId === selectedLedgerAccountId
        );

        if (updatedAccount === undefined) {
          setSelectedLedgerAccountId("");
          setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(selectedRange));
        } else {
          setLedgerAccountEditorForm(toLedgerAccountEditorForm(updatedAccount));
        }
      }

      await onLedgerSetupChanged?.();
      setMessage(selectedLedgerAccountId === "" ? "Ledger account created." : "Ledger account saved.");
    });
  }

  async function handleToggleLedgerAccountStatus(account: LedgerAccountSummary) {
    await runAction(async () => {
      const nextStatus = account.status === "Active" ? "Inactive" : "Active";

      await updateLedgerAccount(account.ledgerAccountId, {
        ...toLedgerAccountEditorForm(account),
        status: nextStatus
      });

      const [accounts, reconciliation] = await Promise.all([
        listLedgerAccounts(withAccountingCompanyCode(ledgerAccountFilters)),
        getLedgerAccountReconciliation(accountingCompanyCode)
      ]);
      setLedgerAccounts(accounts);
      setLedgerAccountReconciliation(reconciliation);

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
      setOpeningBalanceImportForm((current) => ({
        ...current,
        sourceReference: preview.sourceReference
      }));
      setMessage(preview.canPost
        ? `Opening balance dry-run ${preview.sourceReference} is balanced.`
        : `Opening balance dry-run has ${preview.blockers.length} blocker(s).`);
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
    accountingControlSettings,
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
    ledgerAccountFilters,
    journalEntryFilters,
    trialBalanceFilters,
    profitAndLossFilters,
    balanceSheetFilters,
    accountingControlSettingsForm,
    accountingPeriodForm,
    manualJournalEntryForm,
    openingBalanceImportForm,
    openingBalanceImportPreview,
    openPeriodCount,
    postingAccountCount,
    handleLedgerAccountFiltersChange,
    setAccountCodeRangeForm,
    setLedgerAccountEditorForm,
    setJournalEntryFilters,
    setTrialBalanceFilters,
    setProfitAndLossFilters,
    setBalanceSheetFilters,
    setAccountingControlSettingsForm,
    setAccountingPeriodForm,
    setManualJournalEntryForm,
    setOpeningBalanceImportForm,
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
    handlePrepareNextAccountingPeriod,
    handleCreateAccountingPeriod,
    handleCheckAccountingPeriodReadiness,
    handlePreviewAccountingCloseJournal,
    handleCloseAccountingPeriod,
    handleReopenAccountingPeriod,
    handleSelectAccountCodeRange,
    handleSaveAccountCodeRange,
    handleNewLedgerAccount,
    handleEditLedgerAccount,
    handleSuggestAccountingLedgerAccountCode,
    handleSaveLedgerAccount,
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
    handleVoidManualJournalEntry,
    rememberJournalSourceDocument,
    loadJournalSourceDocument
  };
}
