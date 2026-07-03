import {
  AlertCircle,
  ArrowRight,
  Banknote,
  CheckCircle2,
  Cloud,
  FileText,
  KeyRound,
  LayoutDashboard,
  ListTree,
  ReceiptText,
  ScrollText,
  UserRound,
  Users,
  type LucideIcon
} from "lucide-react";
import { useEffect, useState } from "react";
import { ApiError } from "../../../shared/api/apiError";
import {
  closeAccountingPeriod,
  configureAccountingControlSettings,
  configureAccountCodeRange,
  createAccountingPeriod,
  createLedgerAccount as createAccountingLedgerAccount,
  getAccountingControlSettings,
  getAccountingPeriodCloseJournalPreview,
  getAccountingPeriodCloseReadiness,
  getJournalEntry,
  getJournalEntrySourceDocument,
  getLedgerAccountActivity,
  getLedgerAccountReconciliation,
  getLedgerAccountRepairPlan,
  getTrialBalance,
  listAccountCodeRanges,
  listAccountingPeriods,
  listJournalEntries,
  listLedgerAccounts,
  postManualJournalEntry,
  reopenAccountingPeriod,
  suggestLedgerAccountCode as suggestAccountingLedgerAccountCode,
  updateLedgerAccount,
  voidManualJournalEntry
} from "../../accounting/api/accountingApi";
import { AccountingControlsPanel } from "../../accounting/components/AccountingControlsPanel";
import { AccountingPeriodsPanel } from "../../accounting/components/AccountingPeriodsPanel";
import { ChartOfAccountsPanel } from "../../accounting/components/ChartOfAccountsPanel";
import { JournalWorkbenchPanel } from "../../accounting/components/JournalWorkbenchPanel";
import { LedgerAccountReconciliationPanel } from "../../accounting/components/LedgerAccountReconciliationPanel";
import { TrialBalancePanel } from "../../accounting/components/TrialBalancePanel";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  AccountingControlSettings,
  AccountingControlSettingsInput,
  AccountingPeriod,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  AccountingPeriodFormInput,
  JournalEntryFilters,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  LedgerAccountReconciliation,
  LedgerAccountRepairPlan,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  TrialBalance,
  TrialBalanceFilters,
  TrialBalanceLine
} from "../../accounting/types/accountingTypes";
import {
  createChargeCode,
  createClientChargeRule,
  createLedgerAccount,
  generateInvoiceDraft,
  getCreditNoteDocument,
  getInvoiceDocument,
  issueCreditNote,
  issueInvoice,
  listChargeCodes,
  listClientChargeRules,
  suggestLedgerAccountCode,
  voidInvoice
} from "../../billing/api/billingApi";
import { ClientBillingSetupPanel } from "../../billing/components/ClientBillingSetupPanel";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRule,
  ClientChargeRuleFormInput,
  IssueCreditNoteInput,
  IssuedCreditNote,
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccountFormInput,
  VoidedInvoice,
  VoidInvoiceInput
} from "../../billing/types/billingTypes";
import {
  createClientContract,
  listClientContracts,
  listProductModules,
  replaceActiveClientContract,
  suspendClientContract
} from "../../contracts/api/contractApi";
import { ClientContractsPanel } from "../../contracts/components/ClientContractsPanel";
import type {
  ClientContract,
  ClientContractFormInput,
  ProductModule
} from "../../contracts/types/contractTypes";
import { findProductModule } from "../../contracts/utils/productModuleDisplay";
import {
  createCloudInstallationBootstrapPackage,
  createCloudInstallationSetupToken,
  getLatestCloudInstallationDiagnostics,
  getCloudInstallationStatus,
  listCloudInstallationAuditEvents,
  queueCloudInstallationSupportCommand
} from "../../control-cloud/api/controlCloudApi";
import { CloudInstallationStatusPanel } from "../../control-cloud/components/CloudInstallationStatusPanel";
import type {
  ControlCloudAuditEvent,
  CreateCloudInstallationProvisioningInput,
  CloudInstallationSupportCommandFormInput,
  ControlCloudInstallationStatus,
  LocalServerBootstrapPackage,
  LocalServerDiagnosticReport,
  LocalServerDeploymentProfile,
  LocalServerSetupToken,
  QueuedCloudInstallationSupportCommand
} from "../../control-cloud/types/controlCloudTypes";
import {
  getLatestEntitlementSnapshot,
  issueEntitlementFromPaidInvoiceDefaults
} from "../../entitlements/api/entitlementApi";
import { EntitlementSnapshotPanel } from "../../entitlements/components/EntitlementSnapshotPanel";
import type {
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "../../entitlements/types/entitlementTypes";
import {
  applyClientCredit,
  approveInvoicePayment,
  getClientRefundDocument,
  getInvoicePaymentDocument,
  issueClientRefund,
  recordInvoicePayment,
  rejectInvoicePayment,
  reverseInvoicePayment
} from "../../payments/api/paymentApi";
import { PaymentReceiptPanel } from "../../payments/components/PaymentReceiptPanel";
import type {
  AppliedClientCredit,
  ApplyClientCreditInput,
  IssueClientRefundInput,
  IssuedClientRefund,
  RecordedInvoicePayment,
  RecordInvoicePaymentInput,
  ReversedInvoicePayment
} from "../../payments/types/paymentTypes";
import { getClientStatement } from "../../statements/api/statementApi";
import { ClientStatementPanel } from "../../statements/components/ClientStatementPanel";
import type { ClientStatement } from "../../statements/types/statementTypes";
import {
  activateClient,
  addClientContact,
  addClientSupportNote,
  configureClientDeployment,
  configureClientAccountingProfile,
  createClient,
  getClient,
  getClientAccountingProfile,
  inviteClientPortalContact,
  listClientDeployments,
  listClientPortalInvitations,
  listClients,
  resendClientPortalInvitation,
  revokeClientPortalInvitation,
  suspendClient,
  updateClient
} from "../api/clientApi";
import { ClientCreateForm } from "../components/ClientCreateForm";
import { ClientDetailPanel } from "../components/ClientDetailPanel";
import { ClientListPanel } from "../components/ClientListPanel";
import type {
  AddClientContactInput,
  AddClientSupportNoteInput,
  ClientAccountingProfile,
  ClientDeployment,
  ClientDetails,
  ClientLookup,
  ClientPortalInvitation,
  ConfigureClientAccountingProfileInput,
  ConfigureClientDeploymentInput,
  CreateClientInput,
  UpdateClientInput
} from "../types/clientTypes";

const emptyCreateForm: CreateClientInput = {
  code: "",
  legalName: "",
  displayName: ""
};

const emptyEditForm: UpdateClientInput = {
  legalName: "",
  displayName: ""
};

const emptyNoteForm: AddClientSupportNoteInput = {
  text: "",
  createdBy: "Control Desk"
};

const emptyContactForm: AddClientContactInput = {
  role: "Billing",
  fullName: "",
  jobTitle: "",
  email: "",
  phone: "",
  isPrimary: true
};

const accountingCompanyCode = "MAIN";

const defaultLedgerAccountFilters: LedgerAccountFilters = {
  companyCode: accountingCompanyCode,
  search: "",
  type: "",
  status: "",
  posting: "",
  role: "",
  viewMode: "default",
  level: ""
};

const emptyAccountCodeRangeForm: AccountCodeRangeFormInput = {
  displayName: "",
  searchPrefix: "",
  rangeStart: "",
  rangeEnd: "",
  codeLength: "",
  accountType: "Asset",
  normalBalance: "Debit",
  isPostingAccount: true,
  parentCode: "",
  isActive: true
};

const emptyLedgerAccountEditorForm: LedgerAccountEditorInput = {
  code: "",
  name: "",
  type: "Asset",
  normalBalance: "Debit",
  level: "Detail",
  parentAccountId: "",
  isPostingAccount: true,
  status: "Active"
};

const defaultAccountingControlSettingsForm: AccountingControlSettingsInput = {
  companyCode: accountingCompanyCode,
  baseCurrencyCode: "PKR",
  retainedEarningsAccountId: "",
  incomeSummaryAccountId: "",
  roundingAccountId: ""
};

const defaultJournalEntryFilters: JournalEntryFilters = {
  fromDate: "",
  toDate: "",
  sourceType: ""
};

function createDefaultTrialBalanceFilters(): TrialBalanceFilters {
  return {
    asOfDate: toDateInputValue(new Date()),
    currencyCode: "PKR"
  };
}

type DashboardModule =
  | "dashboard"
  | "clients"
  | "profile"
  | "contracts"
  | "accounting"
  | "billing"
  | "payments"
  | "entitlements"
  | "cloud"
  | "statement";

type BillingDashboardStep = "accounting" | "rules" | "draft" | "issue";

type PaymentDashboardStep = "readiness" | "cash" | "receipt" | "settlement" | "refund" | "result";

type JournalSourceDocumentTarget =
  | { module: "billing"; step: BillingDashboardStep; label: string }
  | { module: "payments"; step: PaymentDashboardStep; label: string };

export function ClientDeskPage() {
  const [clients, setClients] = useState<ClientLookup[]>([]);
  const [selectedClientId, setSelectedClientId] = useState("");
  const [selectedClient, setSelectedClient] = useState<ClientDetails | null>(null);
  const [accountingProfile, setAccountingProfile] = useState<ClientAccountingProfile | null>(null);
  const [accountingProfileMissing, setAccountingProfileMissing] = useState(false);
  const [contracts, setContracts] = useState<ClientContract[]>([]);
  const [productModules, setProductModules] = useState<ProductModule[]>([]);
  const [chargeCodes, setChargeCodes] = useState<ChargeCodeLookup[]>([]);
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
  const [focusedJournalEntryId, setFocusedJournalEntryId] = useState("");
  const [focusedJournalEntry, setFocusedJournalEntry] = useState<JournalEntrySummary | null>(null);
  const [journalSourceDocumentsById, setJournalSourceDocumentsById] =
    useState<Record<string, JournalEntrySourceDocument>>({});
  const [ledgerAccountActivity, setLedgerAccountActivity] =
    useState<LedgerAccountActivity | null>(null);
  const [trialBalance, setTrialBalance] = useState<TrialBalance | null>(null);
  const [selectedAccountCodeRangeRole, setSelectedAccountCodeRangeRole] = useState("");
  const [accountCodeRangeForm, setAccountCodeRangeForm] =
    useState<AccountCodeRangeFormInput>(emptyAccountCodeRangeForm);
  const [selectedLedgerAccountId, setSelectedLedgerAccountId] = useState("");
  const [ledgerAccountEditorForm, setLedgerAccountEditorForm] =
    useState<LedgerAccountEditorInput>(emptyLedgerAccountEditorForm);
  const [ledgerAccountFilters, setLedgerAccountFilters] = useState<LedgerAccountFilters>(
    defaultLedgerAccountFilters
  );
  const [journalEntryFilters, setJournalEntryFilters] = useState<JournalEntryFilters>(
    defaultJournalEntryFilters
  );
  const [trialBalanceFilters, setTrialBalanceFilters] = useState<TrialBalanceFilters>(
    createDefaultTrialBalanceFilters()
  );
  const [accountingControlSettingsForm, setAccountingControlSettingsForm] =
    useState<AccountingControlSettingsInput>(createDefaultAccountingControlSettingsForm());
  const [accountingPeriodForm, setAccountingPeriodForm] = useState<AccountingPeriodFormInput>(
    createDefaultAccountingPeriodForm()
  );
  const [manualJournalEntryForm, setManualJournalEntryForm] =
    useState<ManualJournalEntryInput>(createDefaultManualJournalEntryForm());
  const [createForm, setCreateForm] = useState<CreateClientInput>(emptyCreateForm);
  const [editForm, setEditForm] = useState<UpdateClientInput>(emptyEditForm);
  const [contactForm, setContactForm] = useState<AddClientContactInput>(emptyContactForm);
  const [noteForm, setNoteForm] = useState<AddClientSupportNoteInput>(emptyNoteForm);
  const [contractForm, setContractForm] = useState<ClientContractFormInput>(
    createDefaultContractForm()
  );
  const [receivableAccountForm, setReceivableAccountForm] = useState<LedgerAccountFormInput>(
    createDefaultReceivableAccountForm()
  );
  const [revenueAccountForm, setRevenueAccountForm] = useState<LedgerAccountFormInput>(
    createDefaultRevenueAccountForm()
  );
  const [cashAccountForm, setCashAccountForm] = useState<LedgerAccountFormInput>(
    createDefaultCashAccountForm()
  );
  const [accountingProfileForm, setAccountingProfileForm] =
    useState<ConfigureClientAccountingProfileInput>(createDefaultAccountingProfileForm());
  const [chargeCodeForm, setChargeCodeForm] = useState<ChargeCodeFormInput>(
    createDefaultChargeCodeForm()
  );
  const [chargeRuleForm, setChargeRuleForm] = useState<ClientChargeRuleFormInput>(
    createDefaultChargeRuleForm()
  );
  const [invoiceDraftForm, setInvoiceDraftForm] = useState<InvoiceDraftFormInput>(
    createDefaultInvoiceDraftForm()
  );
  const [issueInvoiceForm, setIssueInvoiceForm] = useState<IssueInvoiceFormInput>(
    createDefaultIssueInvoiceForm()
  );
  const [latestChargeRule, setLatestChargeRule] = useState<ClientChargeRule | null>(null);
  const [clientChargeRules, setClientChargeRules] = useState<ClientChargeRule[]>([]);
  const [invoiceDraft, setInvoiceDraft] = useState<InvoiceDraft | null>(null);
  const [issuedInvoice, setIssuedInvoice] = useState<IssuedInvoice | null>(null);
  const [voidedInvoice, setVoidedInvoice] = useState<VoidedInvoice | null>(null);
  const [issuedCreditNote, setIssuedCreditNote] = useState<IssuedCreditNote | null>(null);
  const [paymentForm, setPaymentForm] = useState<RecordInvoicePaymentInput>(
    createDefaultPaymentForm()
  );
  const [refundForm, setRefundForm] = useState<IssueClientRefundInput>(
    createDefaultRefundForm()
  );
  const [creditApplicationForm, setCreditApplicationForm] = useState<ApplyClientCreditInput>(
    createDefaultCreditApplicationForm()
  );
  const [recordedPayment, setRecordedPayment] = useState<RecordedInvoicePayment | null>(null);
  const [issuedRefund, setIssuedRefund] = useState<IssuedClientRefund | null>(null);
  const [appliedCredit, setAppliedCredit] = useState<AppliedClientCredit | null>(null);
  const [latestEntitlementSnapshot, setLatestEntitlementSnapshot] =
    useState<EntitlementSnapshot | null>(null);
  const [latestEntitlementSnapshotMissing, setLatestEntitlementSnapshotMissing] = useState(false);
  const [issuedEntitlementSnapshot, setIssuedEntitlementSnapshot] =
    useState<IssuedEntitlementSnapshot | null>(null);
  const [cloudInstallationId, setCloudInstallationId] = useState("");
  const [cloudInstallationStatus, setCloudInstallationStatus] =
    useState<ControlCloudInstallationStatus | null>(null);
  const [clientDeployments, setClientDeployments] = useState<ClientDeployment[]>([]);
  const [deploymentForm, setDeploymentForm] = useState<ConfigureClientDeploymentInput>(
    createDefaultDeploymentForm()
  );
  const [setupTokenHours, setSetupTokenHours] = useState("72");
  const [cloudSetupToken, setCloudSetupToken] = useState<LocalServerSetupToken | null>(null);
  const [cloudBootstrapPackage, setCloudBootstrapPackage] =
    useState<LocalServerBootstrapPackage | null>(null);
  const [supportCommandForm, setSupportCommandForm] =
    useState<CloudInstallationSupportCommandFormInput>(createDefaultSupportCommandForm());
  const [queuedSupportCommand, setQueuedSupportCommand] =
    useState<QueuedCloudInstallationSupportCommand | null>(null);
  const [cloudAuditEvents, setCloudAuditEvents] = useState<ControlCloudAuditEvent[]>([]);
  const [cloudDiagnosticsReport, setCloudDiagnosticsReport] =
    useState<LocalServerDiagnosticReport | null>(null);
  const [latestPortalInvitation, setLatestPortalInvitation] =
    useState<ClientPortalInvitation | null>(null);
  const [portalInvitations, setPortalInvitations] = useState<ClientPortalInvitation[]>([]);
  const [clientStatement, setClientStatement] = useState<ClientStatement | null>(null);
  const [activeDashboardModule, setActiveDashboardModule] =
    useState<DashboardModule>("dashboard");
  const [preferredBillingStep, setPreferredBillingStep] =
    useState<BillingDashboardStep>("accounting");
  const [preferredPaymentStep, setPreferredPaymentStep] =
    useState<PaymentDashboardStep>("readiness");
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    void refreshClients();
    void refreshChargeCodes();
    void refreshProductModules();
  }, []);

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
    if (selectedClientId !== "") {
      void loadClient(selectedClientId);
    }
  }, [selectedClientId]);

  async function refreshClients(nextSelectedClientId = selectedClientId) {
    await runClientAction(() => loadClientList(nextSelectedClientId));
  }

  async function refreshChargeCodes() {
    await runClientAction(async () => {
      const nextChargeCodes = await listChargeCodes();
      setChargeCodes(sortChargeCodes(nextChargeCodes));
    });
  }

  async function refreshProductModules() {
    await runClientAction(async () => {
      const nextProductModules = await listProductModules();
      setProductModules(nextProductModules);
      setContractForm((current) => {
        if (current.moduleCodes.trim().toUpperCase() !== "CONTROL_DESK") {
          return current;
        }

        return {
          ...current,
          moduleCodes: defaultContractModuleCodes(nextProductModules)
        };
      });
    });
  }

  function handleLedgerAccountFiltersChange(filters: LedgerAccountFilters) {
    setLedgerAccountFilters(withAccountingCompanyCode(filters));
  }

  async function refreshAccountingSetup(filters = ledgerAccountFilters) {
    await runClientAction(async () => {
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
    await runClientAction(async () => {
      const settings = await getAccountingControlSettings(accountingCompanyCode);

      setAccountingControlSettings(settings);
      setAccountingControlSettingsForm(toAccountingControlSettingsForm(settings));
    });
  }

  async function handleSaveAccountingControls() {
    await runClientAction(async () => {
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

  async function refreshAccountingPeriods() {
    await runClientAction(async () => {
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
    await runClientAction(async () => {
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
    await runClientAction(async () => {
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
    await runClientAction(async () => {
      const readiness = await getAccountingPeriodCloseReadiness(period.accountingPeriodId);
      setAccountingPeriodReadiness(readiness);
      setMessage(readiness.canClose
        ? `${readiness.period.name} is ready to close.`
        : `${readiness.period.name} has close blockers.`);
    });
  }

  async function handlePreviewAccountingCloseJournal(period: AccountingPeriod) {
    await runClientAction(async () => {
      const preview = await getAccountingPeriodCloseJournalPreview(period.accountingPeriodId);
      setAccountingPeriodCloseJournalPreview(preview);
      setMessage(preview.canGenerate
        ? `${preview.period.name} close journal preview is ready.`
        : `${preview.period.name} close journal preview has blockers.`);
    });
  }

  async function handleCloseAccountingPeriod(period: AccountingPeriod) {
    await runClientAction(async () => {
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
      setMessage(`Accounting period ${closedPeriod.name} closed with close artifact.`);
    });
  }

  async function handleReopenAccountingPeriod(period: AccountingPeriod) {
    await runClientAction(async () => {
      const reopenedPeriod = await reopenAccountingPeriod(period.accountingPeriodId);
      const periods = await listAccountingPeriods(accountingCompanyCode);

      setAccountingPeriods(periods);
      setAccountingPeriodReadiness(await getAccountingPeriodCloseReadiness(reopenedPeriod.accountingPeriodId));
      setAccountingPeriodCloseJournalPreview(null);
      setMessage(`Accounting period ${reopenedPeriod.name} reopened.`);
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
    await runClientAction(async () => {
      const entries = await listJournalEntries(filters);
      setJournalEntries(entries);
    });
  }

  async function refreshTrialBalance(filters = trialBalanceFilters) {
    await runClientAction(async () => {
      const balance = await getTrialBalance(filters);
      setTrialBalance(balance);
    });
  }

  async function handleSaveAccountCodeRange() {
    if (selectedAccountCodeRangeRole === "") {
      return;
    }

    await runClientAction(async () => {
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
      await applyLedgerAccountCodeSuggestions();
      setMessage("Accounting setup range saved.");
    });
  }

  async function handleNewLedgerAccount() {
    const selectedRange = accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null;

    setSelectedLedgerAccountId("");
    setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(selectedRange));

    if (selectedRange === null) {
      return;
    }

    await runClientAction(async () => {
      const suggestion = await suggestAccountingLedgerAccountCode(
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

    await runClientAction(async () => {
      const selectedRange =
        accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null;
      const suggestion = await suggestAccountingLedgerAccountCode(
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
    await runClientAction(async () => {
      if (selectedLedgerAccountId === "") {
        await createAccountingLedgerAccount(ledgerAccountEditorForm);
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
        const updatedAccount = accounts.find((account) => account.ledgerAccountId === selectedLedgerAccountId);
        if (updatedAccount === undefined) {
          setSelectedLedgerAccountId("");
          setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(selectedRange));
        } else {
          setLedgerAccountEditorForm(toLedgerAccountEditorForm(updatedAccount));
        }
      }

      await applyLedgerAccountCodeSuggestions();
      setMessage(selectedLedgerAccountId === "" ? "Ledger account created." : "Ledger account saved.");
    });
  }

  async function handleToggleLedgerAccountStatus(account: LedgerAccountSummary) {
    await runClientAction(async () => {
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
        const updatedAccount = accounts.find((item) => item.ledgerAccountId === account.ledgerAccountId);
        if (updatedAccount === undefined) {
          setSelectedLedgerAccountId("");
          setLedgerAccountEditorForm(createDefaultLedgerAccountEditorForm(
            accountCodeRanges.find((range) => range.role === selectedAccountCodeRangeRole) ?? null));
        } else {
          setLedgerAccountEditorForm(toLedgerAccountEditorForm(updatedAccount));
        }
      }

      setMessage(nextStatus === "Active" ? "Ledger account reactivated." : "Ledger account deactivated.");
    });
  }

  async function handleViewLedgerAccountActivity(account: LedgerAccountSummary) {
    await runClientAction(async () => {
      const activity = await getLedgerAccountActivity(account.ledgerAccountId);
      setLedgerAccountActivity(activity);
      setMessage(`Loaded ${account.displayCode} activity.`);
    });
  }

  async function handleViewTrialBalanceAccountActivity(line: TrialBalanceLine) {
    await runClientAction(async () => {
      const activity = await getLedgerAccountActivity(line.ledgerAccountId);
      setLedgerAccountActivity(activity);
      setMessage(`Loaded ${line.code} activity from trial balance.`);
    });
  }

  async function handleViewJournalEntryById(journalEntryId: string) {
    const normalizedJournalEntryId = journalEntryId.trim();

    if (normalizedJournalEntryId === "") {
      return;
    }

    await runClientAction(async () => {
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
      setActiveDashboardModule("accounting");
      setMessage(`Opened journal ${entry.sourceReference ?? entry.journalEntryId}.`);
    });
  }

  async function handleFocusJournalEntry(journalEntryId: string) {
    if (journalEntryId === "" || focusedJournalEntryId === journalEntryId) {
      setFocusedJournalEntryId("");
      setFocusedJournalEntry(null);
      return;
    }

    await runClientAction(async () => {
      const entry = await getJournalEntry(journalEntryId);

      setFocusedJournalEntryId(entry.journalEntryId);
      setFocusedJournalEntry(entry);
      await loadJournalSourceDocument(entry);
      setMessage(`Focused journal ${entry.sourceReference ?? entry.journalEntryId}.`);
    });
  }

  function getJournalSourceDocumentLabel(entry: JournalEntrySummary) {
    return getJournalSourceDocumentTarget(entry)?.label ?? getJournalSourceDocumentFallbackLabel(entry);
  }

  async function handleOpenJournalSourceDocument(entry: JournalEntrySummary) {
    const target = getJournalSourceDocumentTarget(entry);

    if (target !== null) {
      openJournalSourceDocumentTarget(target);
      return;
    }

    await runClientAction(async () => {
      const sourceDocument = await getJournalEntrySourceDocument(entry.journalEntryId);
      const resolvedTarget = getJournalSourceDocumentTargetFromResolved(sourceDocument);
      rememberJournalSourceDocument(sourceDocument);

      if (resolvedTarget === null) {
        setMessage(sourceDocument.message ?? "That journal source could not be resolved.");
        return;
      }

      const isLoadedClientSource =
        sourceDocument.clientId === null
        || sourceDocument.clientId === undefined
        || sourceDocument.clientId === selectedClientId;

      if (!isLoadedClientSource && sourceDocument.clientId !== null && sourceDocument.clientId !== undefined) {
        setSelectedClientId(sourceDocument.clientId);
        openJournalSourceDocumentTarget(resolvedTarget, sourceDocument);
        return;
      }

      await hydrateJournalSourceDocument(sourceDocument);
      openJournalSourceDocumentTarget(resolvedTarget, sourceDocument);
    });
  }

  function openJournalSourceDocumentTarget(
    target: JournalSourceDocumentTarget,
    sourceDocument?: JournalEntrySourceDocument
  ) {
    if (target.module === "billing") {
      setPreferredBillingStep(target.step);
    } else {
      setPreferredPaymentStep(target.step);
    }

    setError("");
    setActiveDashboardModule(target.module);
    setMessage(sourceDocument?.status === null || sourceDocument?.status === undefined
      ? `Opened ${target.label}.`
      : `Opened ${target.label} (${sourceDocument.status}).`);
  }

  async function hydrateJournalSourceDocument(sourceDocument: JournalEntrySourceDocument) {
    if (
      !sourceDocument.isResolved
      || sourceDocument.documentKind === null
      || sourceDocument.documentKind === undefined
      || sourceDocument.documentId === null
      || sourceDocument.documentId === undefined
    ) {
      return;
    }

    if (sourceDocument.documentKind === "Invoice") {
      const document = await getInvoiceDocument(sourceDocument.documentId);

      applyHydratedInvoiceContext(document.invoice, {
        issuedInvoice: document.issuedInvoice ?? null,
        voidedInvoice: document.voidedInvoice ?? null,
        creditNote: document.creditNote ?? null,
        resetPaymentArtifacts: true
      });
      return;
    }

    if (sourceDocument.documentKind === "CreditNote") {
      const document = await getCreditNoteDocument(sourceDocument.documentId);

      applyHydratedInvoiceContext(document.invoice, {
        creditNote: document.creditNote,
        resetPaymentArtifacts: true
      });
      return;
    }

    if (sourceDocument.documentKind === "Payment") {
      const document = await getInvoicePaymentDocument(sourceDocument.documentId);
      const payment = sourceDocument.sourceType === "PaymentReversal" && document.reversal !== null && document.reversal !== undefined
        ? mapReversedPaymentToRecordedPayment(document.reversal)
        : document.payment;

      applyHydratedInvoiceContext(document.invoice, { resetPaymentArtifacts: false });
      setRecordedPayment(payment);
      setIssuedRefund(null);
      setAppliedCredit(null);
      setIssuedEntitlementSnapshot(null);
      return;
    }

    if (sourceDocument.documentKind === "ClientRefund") {
      const document = await getClientRefundDocument(sourceDocument.documentId);

      setIssuedRefund(document.refund);
      setRecordedPayment(null);
      setAppliedCredit(null);
      setRefundForm((current) => ({
        ...current,
        clientId: document.refund.clientId,
        method: document.refund.method,
        reference: document.refund.reference,
        amount: document.refund.amount.toFixed(2),
        currencyCode: document.refund.currencyCode,
        refundedOn: document.refund.refundedOn,
        postingDate: document.refund.postingDate,
        accountsReceivableAccountId:
          current.accountsReceivableAccountId.trim() === ""
            ? accountingProfile?.accountsReceivableAccountId ?? ""
            : current.accountsReceivableAccountId
      }));
    }
  }

  function applyHydratedInvoiceContext(
    invoice: InvoiceDraft,
    options: {
      issuedInvoice?: IssuedInvoice | null;
      voidedInvoice?: VoidedInvoice | null;
      creditNote?: IssuedCreditNote | null;
      resetPaymentArtifacts: boolean;
    }
  ) {
    const postingDate =
      options.issuedInvoice?.postingDate
      ?? options.voidedInvoice?.voidDate
      ?? options.creditNote?.creditDate
      ?? invoice.issueDate;

    setInvoiceDraft(invoice);
    setInvoiceDraftForm({
      contractId: invoice.contractId,
      invoiceNumber: invoice.invoiceNumber,
      issueDate: invoice.issueDate,
      dueDate: invoice.dueDate,
      billingDate: invoice.billingDate,
      currencyCode: invoice.currencyCode
    });
    setIssuedInvoice(options.issuedInvoice ?? null);
    setVoidedInvoice(options.voidedInvoice ?? null);
    setIssuedCreditNote(options.creditNote ?? null);
    setIssueInvoiceForm((current) => ({
      ...current,
      postingDate
    }));
    setPaymentForm(createDefaultPaymentForm(
      selectedClient,
      invoice,
      accountingProfile,
      issueInvoiceForm.accountsReceivableAccountId
    ));
    setCreditApplicationForm(createDefaultCreditApplicationForm(
      selectedClient,
      invoice,
      clientStatement
    ));

    if (options.resetPaymentArtifacts) {
      setRecordedPayment(null);
      setIssuedRefund(null);
      setAppliedCredit(null);
      setIssuedEntitlementSnapshot(null);
    }
  }

  function getJournalSourceDocumentTargetFromResolved(
    sourceDocument: JournalEntrySourceDocument
  ): JournalSourceDocumentTarget | null {
    if (
      !sourceDocument.isResolved
      || sourceDocument.label === null
      || sourceDocument.label === undefined
    ) {
      return null;
    }

    if (sourceDocument.dashboardModule === "billing") {
      const step = toBillingDashboardStep(sourceDocument.dashboardStep);

      return step === null
        ? null
        : {
            module: "billing",
            step,
            label: sourceDocument.label
          };
    }

    if (sourceDocument.dashboardModule === "payments") {
      const step = toPaymentDashboardStep(sourceDocument.dashboardStep);

      return step === null
        ? null
        : {
            module: "payments",
            step,
            label: sourceDocument.label
          };
    }

    return null;
  }

  function getJournalSourceDocumentTarget(entry: JournalEntrySummary): JournalSourceDocumentTarget | null {
    if (
      entry.sourceType === "BillingInvoice"
      && (
        journalEntryIdMatches(entry, issuedInvoice?.journalEntryId)
        || referencesMatch(entry.sourceReference, issuedInvoice?.invoiceNumber)
        || referencesMatch(entry.sourceReference, invoiceDraft?.invoiceNumber)
      )
    ) {
      return {
        module: "billing",
        step: "issue",
        label: `invoice ${sourceDocumentReference(entry, issuedInvoice?.invoiceNumber ?? invoiceDraft?.invoiceNumber)}`
      };
    }

    if (
      entry.sourceType === "BillingInvoiceVoid"
      && (
        journalEntryIdMatches(entry, voidedInvoice?.reversalJournalEntryId)
        || referencesMatch(entry.sourceReference, voidedInvoice?.invoiceNumber)
        || referencesMatch(entry.sourceReference, invoiceDraft?.invoiceNumber)
      )
    ) {
      return {
        module: "billing",
        step: "issue",
        label: `voided invoice ${sourceDocumentReference(entry, voidedInvoice?.invoiceNumber ?? invoiceDraft?.invoiceNumber)}`
      };
    }

    if (
      entry.sourceType === "BillingCreditNote"
      && (
        journalEntryIdMatches(entry, issuedCreditNote?.journalEntryId)
        || referencesMatch(entry.sourceReference, issuedCreditNote?.creditNoteNumber)
      )
    ) {
      return {
        module: "billing",
        step: "issue",
        label: `credit note ${sourceDocumentReference(entry, issuedCreditNote?.creditNoteNumber)}`
      };
    }

    if (
      entry.sourceType === "PaymentReceipt"
      && journalEntryIdMatches(entry, recordedPayment?.journalEntryId)
    ) {
      return {
        module: "payments",
        step: "result",
        label: `payment ${sourceDocumentReference(entry, recordedPayment?.invoiceNumber)}`
      };
    }

    if (
      entry.sourceType === "PaymentReversal"
      && journalEntryIdMatches(entry, recordedPayment?.journalEntryId)
    ) {
      return {
        module: "payments",
        step: "result",
        label: `payment reversal ${sourceDocumentReference(entry, recordedPayment?.invoiceNumber)}`
      };
    }

    if (
      entry.sourceType === "ClientRefund"
      && (
        journalEntryIdMatches(entry, issuedRefund?.journalEntryId)
        || referencesMatch(entry.sourceReference, issuedRefund?.reference)
      )
    ) {
      return {
        module: "payments",
        step: "refund",
        label: `refund ${sourceDocumentReference(entry, issuedRefund?.reference)}`
      };
    }

    return null;
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

  function getJournalSourceDocumentClientLabel(sourceDocument: JournalEntrySourceDocument): string {
    if (sourceDocument.clientId === null || sourceDocument.clientId === undefined) {
      return "-";
    }

    if (selectedClient !== null && sourceDocument.clientId === selectedClient.clientId) {
      return `${selectedClient.code} ${selectedClient.displayName}`;
    }

    const client = clients.find((candidate) => candidate.clientId === sourceDocument.clientId);

    return client === undefined
      ? sourceDocument.clientId
      : `${client.code} ${client.displayName}`;
  }

  async function handleViewJournalEntryFromActivity(line: LedgerAccountActivityLine) {
    await runClientAction(async () => {
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
      setMessage(`Focused journal ${entry.sourceReference ?? entry.journalEntryId}.`);
    });
  }

  async function handlePostManualJournalEntry() {
    await runClientAction(async () => {
      const postedEntry = await postManualJournalEntry(manualJournalEntryForm);
      const [entries, balance] = await Promise.all([
        listJournalEntries(journalEntryFilters),
        getTrialBalance(trialBalanceFilters)
      ]);

      setJournalEntries(entries);
      setTrialBalance(balance);
      setManualJournalEntryForm(createDefaultManualJournalEntryForm());
      setFocusedJournalEntryId(postedEntry.journalEntryId);
      setFocusedJournalEntry(postedEntry);
      await refreshSelectedLedgerAccountActivity();
      setMessage(`Manual journal ${postedEntry.sourceReference ?? postedEntry.journalEntryId} posted.`);
    });
  }

  async function handleVoidManualJournalEntry(entry: JournalEntrySummary) {
    await runClientAction(async () => {
      const result = await voidManualJournalEntry(entry.journalEntryId, {
        voidDate: toDateInputValue(new Date()),
        reason: "Voided from GL workbench"
      });
      const [entries, balance, reversalEntry] = await Promise.all([
        listJournalEntries(journalEntryFilters),
        getTrialBalance(trialBalanceFilters),
        getJournalEntry(result.reversalJournalEntryId)
      ]);

      setJournalEntries(entries);
      setTrialBalance(balance);
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

    const activity = await getLedgerAccountActivity(ledgerAccountActivity.ledgerAccountId);
    setLedgerAccountActivity(activity);
  }

  async function loadClientList(nextSelectedClientId = selectedClientId) {
    const clientList = await listClients();
    setClients(clientList);

    if (clientList.length === 0) {
      setSelectedClientId("");
      setSelectedClient(null);
      setAccountingProfile(null);
      setAccountingProfileMissing(false);
      setContracts([]);
      setCloudInstallationId("");
      setCloudInstallationStatus(null);
      setClientDeployments([]);
      setDeploymentForm(createDefaultDeploymentForm());
      clearCloudProvisioningArtifacts();
      setLatestPortalInvitation(null);
      setPortalInvitations([]);
      setClientStatement(null);
      resetBillingForms();
      return;
    }

    const selectedExists = clientList.some((client) => client.clientId === nextSelectedClientId);
    setSelectedClientId(selectedExists ? nextSelectedClientId : clientList[0].clientId);
  }

  async function loadClient(clientId: string) {
    await runClientAction(async () => {
      const [client, clientContracts, statement, deployments] = await Promise.all([
        getClient(clientId),
        listClientContracts(clientId),
        getClientStatement(clientId),
        listClientDeployments(clientId)
      ]);

      applyLoadedClient(client);
      applyLoadedDeployments(client, deployments);
      setContracts(clientContracts);
      setClientStatement(statement);
      setContractForm(createDefaultContractForm(client.code, productModules));
      applyBillingDefaults(client, getActiveContract(clientContracts));
      await applyLedgerAccountCodeSuggestions();
      await loadClientChargeRules(clientId, getActiveContract(clientContracts)?.contractId);
      await loadAccountingProfile(clientId);
      await loadLatestEntitlementSnapshot(clientId);
      await loadClientPortalInvitations(clientId, true);
    });
  }

  async function refreshClientStatement(clientId = selectedClient?.clientId) {
    if (clientId === undefined) {
      return null;
    }

    const statement = await getClientStatement(clientId);
    setClientStatement(statement);

    return statement;
  }

  async function loadAccountingProfile(clientId: string) {
    try {
      const profile = await getClientAccountingProfile(clientId);
      applyAccountingProfile(profile, clientId);
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setAccountingProfile(null);
        setAccountingProfileMissing(true);
        setAccountingProfileForm((current) => ({
          ...current,
          accountsReceivableAccountId: ""
        }));
        setRefundForm((current) => ({
          ...current,
          clientId,
          accountsReceivableAccountId: ""
        }));
        setCreditApplicationForm((current) => ({
          ...current,
          clientId
        }));
        return;
      }

      throw caughtError;
    }
  }

  function applyAccountingProfile(profile: ClientAccountingProfile, clientId: string) {
    setAccountingProfile(profile);
    setAccountingProfileMissing(false);
    setAccountingProfileForm(toAccountingProfileForm(profile));
    setIssueInvoiceForm((current) => ({
      ...current,
      accountsReceivableAccountId:
        current.accountsReceivableAccountId.trim() === ""
          ? profile.accountsReceivableAccountId
          : current.accountsReceivableAccountId
    }));
    setPaymentForm((current) => ({
      ...current,
      accountsReceivableAccountId: profile.accountsReceivableAccountId
    }));
    setRefundForm((current) => ({
      ...current,
      clientId,
      accountsReceivableAccountId: profile.accountsReceivableAccountId,
      currencyCode: profile.defaultCurrencyCode
    }));
    setCreditApplicationForm((current) => ({
      ...current,
      clientId,
      currencyCode: profile.defaultCurrencyCode
    }));
  }

  async function applyLedgerAccountCodeSuggestions() {
    try {
      const [receivable, revenue, cashBank] = await Promise.all([
        suggestLedgerAccountCode("ClientReceivable"),
        suggestLedgerAccountCode("SubscriptionRevenue"),
        suggestLedgerAccountCode("CashBank")
      ]);

      setReceivableAccountForm((current) => ({
        ...current,
        code: receivable.suggestedCode,
        type: receivable.type,
        normalBalance: receivable.normalBalance,
        isPostingAccount: receivable.isPostingAccount
      }));
      setRevenueAccountForm((current) => ({
        ...current,
        code: revenue.suggestedCode,
        type: revenue.type,
        normalBalance: revenue.normalBalance,
        isPostingAccount: revenue.isPostingAccount
      }));
      setCashAccountForm((current) => ({
        ...current,
        code: cashBank.suggestedCode,
        type: cashBank.type,
        normalBalance: cashBank.normalBalance,
        isPostingAccount: cashBank.isPostingAccount
      }));
    } catch {
      // Keep the workbook-derived local fallbacks when the API is not available yet.
    }
  }

  async function loadLatestEntitlementSnapshot(clientId: string) {
    try {
      const snapshot = await getLatestEntitlementSnapshot(clientId);
      setLatestEntitlementSnapshot(snapshot);
      setLatestEntitlementSnapshotMissing(false);
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setLatestEntitlementSnapshot(null);
        setLatestEntitlementSnapshotMissing(true);
        return;
      }

      throw caughtError;
    }
  }

  async function loadClientChargeRules(clientId: string, contractId?: string | null) {
    const chargeRules = await listClientChargeRules(clientId, contractId);
    setClientChargeRules(sortClientChargeRules(chargeRules));
  }

  async function loadCloudInstallationStatus(
    clientId = selectedClient?.clientId,
    installationId = cloudInstallationId
  ): Promise<boolean> {
    const normalizedInstallationId = installationId.trim();

    if (clientId === undefined || normalizedInstallationId === "") {
      setCloudInstallationStatus(null);

      return false;
    }

    try {
      const status = await getCloudInstallationStatus(clientId, normalizedInstallationId);
      setCloudInstallationStatus(status);
      setDeploymentForm((current) => mergeDeploymentStatus(current, status));

      return true;
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setCloudInstallationStatus(null);

        return false;
      }

      throw caughtError;
    }
  }

  async function loadCloudInstallationAuditEvents(
    clientId = selectedClient?.clientId,
    installationId = cloudInstallationId
  ): Promise<boolean> {
    const normalizedInstallationId = installationId.trim();

    if (clientId === undefined || normalizedInstallationId === "") {
      setCloudAuditEvents([]);

      return false;
    }

    const auditEvents = await listCloudInstallationAuditEvents(
      clientId,
      normalizedInstallationId,
      50);
    setCloudAuditEvents(auditEvents);

    return true;
  }

  async function loadCloudInstallationDiagnostics(
    clientId = selectedClient?.clientId,
    installationId = cloudInstallationId
  ): Promise<boolean> {
    const normalizedInstallationId = installationId.trim();

    if (clientId === undefined || normalizedInstallationId === "") {
      setCloudDiagnosticsReport(null);

      return false;
    }

    try {
      const diagnostics = await getLatestCloudInstallationDiagnostics(
        clientId,
        normalizedInstallationId);
      setCloudDiagnosticsReport(diagnostics);

      return true;
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setCloudDiagnosticsReport(null);

        return false;
      }

      throw caughtError;
    }
  }

  async function loadClientPortalInvitations(
    clientId = selectedClient?.clientId,
    suppressUnavailable = false
  ) {
    if (clientId === undefined) {
      setPortalInvitations([]);

      return;
    }

    try {
      const invitations = await listClientPortalInvitations(clientId);
      setPortalInvitations(invitations);
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setPortalInvitations([]);

        return;
      }

      if (suppressUnavailable && caughtError instanceof ApiError && caughtError.statusCode >= 500) {
        setPortalInvitations([]);

        return;
      }

      throw caughtError;
    }
  }

  async function handleCreateClient() {
    await runClientAction(async () => {
      const createdClient = await createClient(createForm);
      setCreateForm(emptyCreateForm);
      await loadClientList(createdClient.clientId);
      setMessage("Client created.");
    });
  }

  async function handleUpdateClient() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const updatedClient = await updateClient(selectedClient.clientId, editForm);
      applyLoadedClient(updatedClient);
      setClients((current) =>
        current.map((client) =>
          client.clientId === updatedClient.clientId ? toClientLookup(updatedClient) : client
        )
      );
      setMessage("Client saved.");
    });
  }

  async function handleActivateClient() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const updatedClient = await activateClient(selectedClient.clientId);
      applyLoadedClient(updatedClient);
      updateClientListRow(updatedClient);
      setMessage("Client activated.");
    });
  }

  async function handleSuspendClient() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const updatedClient = await suspendClient(selectedClient.clientId);
      applyLoadedClient(updatedClient);
      updateClientListRow(updatedClient);
      setMessage("Client suspended.");
    });
  }

  async function handleAddContact() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      await addClientContact(selectedClient.clientId, contactForm);
      const refreshedClient = await getClient(selectedClient.clientId);
      applyLoadedClient(refreshedClient);
      setContactForm({
        ...emptyContactForm,
        role: contactForm.role
      });
      setMessage("Contact added.");
    });
  }

  async function handleInvitePortalContact(clientContactId: string) {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const invitation = await inviteClientPortalContact(
        selectedClient.clientId,
        clientContactId
      );
      setLatestPortalInvitation(invitation);
      upsertPortalInvitation(invitation);
      setMessage(`Portal invite created for ${invitation.email}.`);
    });
  }

  async function handleRefreshPortalInvitations() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      await loadClientPortalInvitations(selectedClient.clientId);
      setMessage("Portal invitations refreshed.");
    });
  }

  async function handleResendPortalInvitation(invitationId: string) {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const invitation = await resendClientPortalInvitation(selectedClient.clientId, invitationId);
      setLatestPortalInvitation(invitation);
      upsertPortalInvitation(invitation);
      setMessage(`Portal invite resent to ${invitation.email}.`);
    });
  }

  async function handleRevokePortalInvitation(invitationId: string) {
    if (selectedClient === null) {
      return;
    }

    if (!confirmPortalAction("Revoke this portal invitation? The current invite link will stop working.")) {
      return;
    }

    await runClientAction(async () => {
      const invitation = await revokeClientPortalInvitation(selectedClient.clientId, invitationId);
      upsertPortalInvitation(invitation);
      setMessage(`Portal invite revoked for ${invitation.email}.`);
    });
  }

  async function handleAddNote() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const note = await addClientSupportNote(selectedClient.clientId, noteForm);
      setSelectedClient({
        ...selectedClient,
        supportNotes: [note, ...selectedClient.supportNotes]
      });
      setNoteForm({
        ...emptyNoteForm,
        createdBy: noteForm.createdBy
      });
      setMessage("Note added.");
    });
  }

  async function handleCreateContract() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const contract = await createClientContract(selectedClient.clientId, contractForm);
      const nextContracts = sortContracts([contract, ...contracts]);
      const activeContract = getActiveContract(nextContracts);
      setContracts(nextContracts);
      setContractForm(createDefaultContractForm(selectedClient.code, productModules));
      applyBillingContractDefaults(selectedClient, activeContract);
      await loadClientChargeRules(selectedClient.clientId, activeContract?.contractId);
      setMessage("Contract created.");
    });
  }

  async function handleReplaceActiveContract() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const result = await replaceActiveClientContract(selectedClient.clientId, contractForm);
      const refreshedContracts = await listClientContracts(selectedClient.clientId);
      const activeContract = getActiveContract(refreshedContracts);
      setContracts(refreshedContracts);
      setContractForm(createDefaultContractForm(selectedClient.code, productModules));
      applyBillingContractDefaults(selectedClient, activeContract);
      await loadClientChargeRules(selectedClient.clientId, activeContract?.contractId);
      setMessage(result.suspendedContract === null ? "Contract activated." : "Active contract replaced.");
    });
  }

  async function handleSuspendContract(contractId: string) {
    await runClientAction(async () => {
      const contract = await suspendClientContract(contractId);
      const nextContracts = sortContracts(
        contracts.map((item) => (item.contractId === contract.contractId ? contract : item))
      );
      const activeContract = getActiveContract(nextContracts);
      setContracts(nextContracts);
      applyBillingContractDefaults(selectedClient, activeContract);
      if (selectedClient !== null) {
        await loadClientChargeRules(selectedClient.clientId, activeContract?.contractId);
      }
      setMessage("Contract suspended.");
    });
  }

  async function handleCreateReceivableAccount() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const account = await createLedgerAccount(receivableAccountForm);
      const nextAccountingProfile = {
        ...accountingProfileForm,
        accountsReceivableAccountId: account.ledgerAccountId
      };
      setAccountingProfileForm(nextAccountingProfile);
      const profile = await configureClientAccountingProfile(
        selectedClient.clientId,
        nextAccountingProfile
      );
      applyAccountingProfile(profile, selectedClient.clientId);
      setMessage("AR account created and linked.");
    });
  }

  async function handleCreateRevenueAccount() {
    await runClientAction(async () => {
      const account = await createLedgerAccount(revenueAccountForm);
      setChargeCodeForm((current) => ({
        ...current,
        revenueAccountId: account.ledgerAccountId
      }));
      setMessage("Revenue account created.");
    });
  }

  async function handleCreateCashAccount() {
    await runClientAction(async () => {
      const account = await createLedgerAccount(cashAccountForm);
      setPaymentForm((current) => ({
        ...current,
        cashOrBankAccountId: account.ledgerAccountId
      }));
      setRefundForm((current) => ({
        ...current,
        cashOrBankAccountId: account.ledgerAccountId
      }));
      setMessage("Cash or bank account created.");
    });
  }

  async function handleSaveAccountingProfile() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const profile = await configureClientAccountingProfile(
        selectedClient.clientId,
        accountingProfileForm
      );
      applyAccountingProfile(profile, selectedClient.clientId);
      setMessage("Accounting profile saved.");
    });
  }

  async function handleCreateChargeCode() {
    await runClientAction(async () => {
      const chargeCode = await createChargeCode(chargeCodeForm);
      setChargeCodes((current) => sortChargeCodes([chargeCode, ...current]));
      setChargeRuleForm((current) => ({
        ...current,
        chargeCodeId: chargeCode.chargeCodeId,
        unitPriceAmount: chargeCode.defaultUnitPriceAmount.toFixed(2),
        currencyCode: chargeCode.currencyCode,
        descriptionOverride: chargeCode.name
      }));
      setMessage("Charge code created.");
    });
  }

  async function handleCreateChargeRule() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const chargeRule = await createClientChargeRule(selectedClient.clientId, chargeRuleForm);
      setLatestChargeRule(chargeRule);
      await loadClientChargeRules(selectedClient.clientId, getActiveContract(contracts)?.contractId);
      setMessage("Charge rule added.");
    });
  }

  function handlePrepareModuleBilling(moduleCode: string) {
    if (selectedClient === null) {
      return;
    }

    const activeContract = getActiveContract(contracts);

    if (activeContract === null) {
      return;
    }

    const productModule = findProductModule(productModules, moduleCode);
    const billingDefaults = productModule?.billingDefaults;
    const displayName = productModule?.displayName ?? moduleCode;

    if (billingDefaults !== null && billingDefaults !== undefined) {
      const existingChargeCode = chargeCodes.find(
        (chargeCode) => chargeCode.code === billingDefaults.chargeCode
      ) ?? null;

      setChargeCodeForm((current) => ({
        ...current,
        code: billingDefaults.chargeCode,
        name: billingDefaults.chargeName,
        description: billingDefaults.description,
        defaultUnitPriceAmount: billingDefaults.defaultUnitPriceAmount.toFixed(2),
        currencyCode: billingDefaults.currencyCode
      }));
      setChargeRuleForm((current) => ({
        ...current,
        contractId: activeContract.contractId,
        chargeCodeId: existingChargeCode?.chargeCodeId ?? "",
        productModuleCode: productModule?.moduleCode ?? moduleCode,
        descriptionOverride: billingDefaults.description,
        unitPriceAmount: billingDefaults.defaultUnitPriceAmount.toFixed(2),
        currencyCode: billingDefaults.currencyCode,
        billingCycle: billingDefaults.billingCycle,
        billingDayOfMonth: activeContract.billingDayOfMonth.toString(),
        effectiveStartsOn: activeContract.startsOn,
        effectiveEndsOn: activeContract.endsOn
      }));
      setMessage(existingChargeCode === null
        ? `${displayName} billing prepared. Create the charge code first.`
        : `${displayName} billing prepared. Add the charge rule.`);
    } else {
      setChargeRuleForm((current) => ({
        ...current,
        contractId: activeContract.contractId,
        chargeCodeId: "",
        productModuleCode: productModule?.moduleCode ?? moduleCode,
        currencyCode: activeContract.currencyCode,
        billingCycle: activeContract.billingCycle,
        billingDayOfMonth: activeContract.billingDayOfMonth.toString(),
        effectiveStartsOn: activeContract.startsOn,
        effectiveEndsOn: activeContract.endsOn
      }));
      setMessage(`${displayName} billing prepared. Fill the charge code and price.`);
    }

    setError("");
    setPreferredBillingStep("rules");
    setActiveDashboardModule("billing");
  }

  async function handleGenerateInvoiceDraft() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const draft = await generateInvoiceDraft(selectedClient.clientId, invoiceDraftForm);
      setInvoiceDraft(draft);
      setIssuedInvoice(null);
      setVoidedInvoice(null);
      setIssuedCreditNote(null);
      setIssueInvoiceForm({
        postingDate: draft.issueDate,
        accountsReceivableAccountId: ""
      });
      setPaymentForm(createDefaultPaymentForm(
        selectedClient,
        draft,
        accountingProfile,
        issueInvoiceForm.accountsReceivableAccountId
      ));
      setCreditApplicationForm(createDefaultCreditApplicationForm(
        selectedClient,
        draft,
        clientStatement
      ));
      setRecordedPayment(null);
      await refreshClientStatement(selectedClient.clientId);
      setMessage("Invoice draft generated.");
    });
  }

  async function handleIssueInvoice() {
    if (invoiceDraft === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Issue invoice ${invoiceDraft.invoiceNumber} for ${formatAccountingAmount(
        invoiceDraft.totalAmount,
        invoiceDraft.currencyCode
      )}? This will post GL entries and queue cloud sync.`
    )) {
      return;
    }

    await runClientAction(async () => {
      const issued = await issueInvoice(invoiceDraft.invoiceId, issueInvoiceForm);
      setIssuedInvoice(issued);
      setVoidedInvoice(null);
      setIssuedCreditNote(null);
      const nextInvoiceDraft = {
        ...invoiceDraft,
        status: issued.invoiceStatus
      };
      setInvoiceDraft(nextInvoiceDraft);
      setPaymentForm(createDefaultPaymentForm(
        selectedClient,
        nextInvoiceDraft,
        accountingProfile,
        issueInvoiceForm.accountsReceivableAccountId
      ));
      setRecordedPayment(null);
      const statement = await refreshClientStatement(selectedClient?.clientId);
      setCreditApplicationForm(createDefaultCreditApplicationForm(
        selectedClient,
        nextInvoiceDraft,
        statement ?? clientStatement
      ));
      setMessage("Invoice issued.");
    });
  }

  async function handleVoidInvoice(input: VoidInvoiceInput) {
    if (invoiceDraft === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Void invoice ${invoiceDraft.invoiceNumber}? This will post a reversal journal.`
    )) {
      return;
    }

    await runClientAction(async () => {
      const voided = await voidInvoice(invoiceDraft.invoiceId, input);
      setVoidedInvoice(voided);
      setIssuedCreditNote(null);
      setIssuedInvoice((current) =>
        current === null
          ? current
          : {
              ...current,
              invoiceStatus: voided.invoiceStatus
            }
      );
      setInvoiceDraft({
        ...invoiceDraft,
        status: voided.invoiceStatus,
        balanceDue: 0
      });
      setRecordedPayment(null);
      setAppliedCredit(null);
      await refreshClientStatement(selectedClient?.clientId);
      setMessage("Invoice voided.");
    });
  }

  async function handleIssueCreditNote(input: IssueCreditNoteInput) {
    if (invoiceDraft === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Issue credit note ${input.creditNoteNumber} for invoice ${invoiceDraft.invoiceNumber}? This will reverse the posted invoice journal.`
    )) {
      return;
    }

    await runClientAction(async () => {
      const creditNote = await issueCreditNote(invoiceDraft.invoiceId, input);
      setIssuedCreditNote(creditNote);
      setIssuedRefund(null);
      setAppliedCredit(null);
      setRefundForm((current) => ({
        ...current,
        clientId: selectedClient?.clientId ?? current.clientId,
        reference: defaultRefundReference(selectedClient?.code, new Date()),
        amount: creditNote.amount.toFixed(2),
        currencyCode: creditNote.currencyCode,
        accountsReceivableAccountId:
          current.accountsReceivableAccountId.trim() === ""
            ? accountingProfile?.accountsReceivableAccountId ?? ""
            : current.accountsReceivableAccountId
      }));
      const statement = await refreshClientStatement(selectedClient?.clientId);
      setCreditApplicationForm(createDefaultCreditApplicationForm(
        selectedClient,
        invoiceDraft,
        statement ?? clientStatement
      ));
      setMessage("Credit note issued.");
    });
  }

  async function handleApplyClientCredit() {
    if (selectedClient === null || invoiceDraft === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Apply ${formatAccountingAmount(
        Number(creditApplicationForm.amount),
        creditApplicationForm.currencyCode
      )} client credit to invoice ${invoiceDraft.invoiceNumber}?`
    )) {
      return;
    }

    await runClientAction(async () => {
      const application = await applyClientCredit(creditApplicationForm);
      const refreshedStatement = await getClientStatement(selectedClient.clientId);
      const remainingCredit = getUnappliedStatementCredit(
        refreshedStatement,
        application.currencyCode
      );
      const nextInvoiceDraft = {
        ...invoiceDraft,
        status: application.invoiceStatus,
        balanceDue: application.invoiceBalanceAfter
      };

      setAppliedCredit(application);
      setIssuedRefund(null);
      setInvoiceDraft(nextInvoiceDraft);
      setClientStatement(refreshedStatement);
      setCreditApplicationForm((current) => ({
        ...current,
        reference: defaultCreditApplicationReference(selectedClient.code, new Date()),
        amount: Math.min(remainingCredit.availableCredit, application.invoiceBalanceAfter) > 0
          ? Math.min(remainingCredit.availableCredit, application.invoiceBalanceAfter).toFixed(2)
          : "0.00",
        currencyCode: remainingCredit.currencyCode,
        appliedOn: toDateInputValue(new Date())
      }));
      setRefundForm((current) => ({
        ...current,
        amount: getStatementCredit(refreshedStatement, application.currencyCode).availableCredit.toFixed(2)
      }));
      setMessage("Client credit applied.");
    });
  }

  async function handleIssueClientRefund() {
    if (selectedClient === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Issue refund ${formatAccountingAmount(
        Number(refundForm.amount),
        refundForm.currencyCode
      )}? This will post cash/bank and AR entries.`
    )) {
      return;
    }

    await runClientAction(async () => {
      const refund = await issueClientRefund(refundForm);
      const refreshedStatement = await getClientStatement(selectedClient.clientId);
      const remainingCredit = getStatementCredit(refreshedStatement, refund.currencyCode);

      setIssuedRefund(refund);
      setClientStatement(refreshedStatement);
      setRefundForm((current) => ({
        ...current,
        reference: defaultRefundReference(selectedClient.code, new Date()),
        amount: remainingCredit.availableCredit > 0
          ? remainingCredit.availableCredit.toFixed(2)
          : "0.00",
        currencyCode: remainingCredit.currencyCode,
        refundedOn: toDateInputValue(new Date()),
        postingDate: toDateInputValue(new Date())
      }));
      setCreditApplicationForm(createDefaultCreditApplicationForm(
        selectedClient,
        invoiceDraft,
        refreshedStatement
      ));
      setMessage("Client refund issued.");
    });
  }

  async function handleRecordInvoicePayment() {
    const paymentAction =
      paymentForm.method === "BankTransfer" ? "record for review" : "post to GL";
    const invoiceReference = invoiceDraft?.invoiceNumber ?? paymentForm.invoiceId;

    if (!confirmAccountingAction(
      `Record ${formatAccountingAmount(
        Number(paymentForm.amount),
        paymentForm.currencyCode
      )} payment for invoice ${invoiceReference}? This will ${paymentAction}.`
    )) {
      return;
    }

    await runClientAction(async () => {
      const payment = await recordInvoicePayment(paymentForm);
      setRecordedPayment(payment);
      setInvoiceDraft((current) =>
        current === null
          ? current
          : {
              ...current,
              status: payment.invoiceStatus,
              balanceDue: payment.balanceDue
            }
      );
      setPaymentForm((current) => ({
        ...current,
        amount: payment.balanceDue > 0 ? payment.balanceDue.toFixed(2) : "0.00",
        reference: defaultReceiptReference(selectedClient?.code, new Date())
      }));
      setIssuedEntitlementSnapshot(null);
      setIssuedCreditNote(null);
      setAppliedCredit(null);
      await refreshClientStatement(selectedClient?.clientId);
      setMessage(payment.paymentStatus === "PendingReview" ? "Payment recorded for review." : "Payment recorded.");
    });
  }

  async function handleApproveInvoicePayment(decisionNote: string) {
    if (recordedPayment === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Approve payment ${formatAccountingAmount(
        recordedPayment.amount,
        recordedPayment.currencyCode
      )} for invoice ${recordedPayment.invoiceNumber}? This will post GL entries.`
    )) {
      return;
    }

    await runClientAction(async () => {
      const payment = await approveInvoicePayment(recordedPayment.paymentId, {
        cashOrBankAccountId: paymentForm.cashOrBankAccountId,
        accountsReceivableAccountId: paymentForm.accountsReceivableAccountId,
        postingDate: paymentForm.postingDate,
        decisionNote
      });

      setRecordedPayment(payment);
      setInvoiceDraft((current) =>
        current === null
          ? current
          : {
              ...current,
              status: payment.invoiceStatus,
              balanceDue: payment.balanceDue
            }
      );
      setPaymentForm((current) => ({
        ...current,
        amount: payment.balanceDue > 0 ? payment.balanceDue.toFixed(2) : "0.00",
        reference: defaultReceiptReference(selectedClient?.code, new Date())
      }));
      setIssuedEntitlementSnapshot(null);
      setAppliedCredit(null);
      await refreshClientStatement(selectedClient?.clientId);
      setMessage("Payment approved and posted.");
    });
  }

  async function handleRejectInvoicePayment(decisionNote: string) {
    if (recordedPayment === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Reject payment ${formatAccountingAmount(
        recordedPayment.amount,
        recordedPayment.currencyCode
      )} for invoice ${recordedPayment.invoiceNumber}?`
    )) {
      return;
    }

    await runClientAction(async () => {
      const rejected = await rejectInvoicePayment(recordedPayment.paymentId, decisionNote);
      setRecordedPayment((current) =>
        current === null
          ? current
          : {
              ...current,
              paymentStatus: rejected.paymentStatus,
              decisionNote: rejected.decisionNote
            }
      );
      await refreshClientStatement(selectedClient?.clientId);
      setMessage("Payment rejected.");
    });
  }

  async function handleReverseInvoicePayment(decisionNote: string, reversalDate: string) {
    if (recordedPayment === null) {
      return;
    }

    if (!confirmAccountingAction(
      `Reverse payment ${formatAccountingAmount(
        recordedPayment.amount,
        recordedPayment.currencyCode
      )} for invoice ${recordedPayment.invoiceNumber}? This will post a reversal journal.`
    )) {
      return;
    }

    await runClientAction(async () => {
      const reversal = await reverseInvoicePayment(recordedPayment.paymentId, {
        decisionNote,
        reversalDate
      });
      const payment = mapReversedPaymentToRecordedPayment(reversal);

      setRecordedPayment(payment);
      setInvoiceDraft((current) =>
        current === null
          ? current
          : {
              ...current,
              status: payment.invoiceStatus,
              balanceDue: payment.balanceDue
            }
      );
      setPaymentForm((current) => ({
        ...current,
        amount: payment.balanceDue > 0 ? payment.balanceDue.toFixed(2) : "0.00",
        reference: defaultReceiptReference(selectedClient?.code, new Date())
      }));
      setIssuedEntitlementSnapshot(null);
      setAppliedCredit(null);
      await refreshClientStatement(selectedClient?.clientId);
      setMessage("Payment reversed.");
    });
  }

  async function handleRefreshClientStatement() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      await refreshClientStatement(selectedClient.clientId);
      setMessage("Statement refreshed.");
    });
  }

  async function handleRefreshLatestEntitlementSnapshot() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      await loadLatestEntitlementSnapshot(selectedClient.clientId);
      setMessage("Latest entitlement refreshed.");
    });
  }

  async function handleRefreshCloudInstallationStatus() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const found = await loadCloudInstallationStatus();
      await loadCloudInstallationAuditEvents();
      setMessage(found
        ? "Cloud installation status refreshed."
        : "No cloud installation status found.");
    });
  }

  async function handleRefreshCloudAuditEvents() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const found = await loadCloudInstallationAuditEvents();
      setMessage(found
        ? "Cloud installation history refreshed."
        : "Select an installation before refreshing cloud history.");
    });
  }

  async function handleRefreshCloudDiagnostics() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const found = await loadCloudInstallationDiagnostics();
      setMessage(found
        ? "Cloud diagnostics refreshed."
        : "No diagnostics report found for this installation.");
    });
  }

  function handleCloudInstallationIdChange(value: string) {
    setCloudInstallationId(value);
    setDeploymentForm((current) => ({
      ...current,
      installationId: value
    }));
    clearCloudProvisioningArtifacts();
  }

  function handleDeploymentValueChange(value: ConfigureClientDeploymentInput) {
    setDeploymentForm(value);
    setCloudInstallationId(value.installationId);
    clearCloudProvisioningArtifacts();
  }

  function handleSupportCommandValueChange(value: CloudInstallationSupportCommandFormInput) {
    setSupportCommandForm(value);
    setQueuedSupportCommand(null);
  }

  function handleSelectClientDeployment(clientDeploymentId: string) {
    if (selectedClient === null) {
      return;
    }

    const deployment = clientDeployments.find((item) => item.clientDeploymentId === clientDeploymentId) ?? null;
    const nextForm = deployment === null
      ? createDefaultDeploymentForm(selectedClient)
      : toDeploymentForm(deployment);

    setDeploymentForm(nextForm);
    setCloudInstallationId(nextForm.installationId);
    setCloudInstallationStatus(null);
    clearCloudProvisioningArtifacts();
  }

  async function handleSaveClientDeployment() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      await saveDeploymentForClient(selectedClient.clientId);
      setMessage("Client deployment saved.");
    });
  }

  async function handleCreateCloudSetupToken() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const savedDeployment = await saveDeploymentForClient(selectedClient.clientId);
      const setupToken = await createCloudInstallationSetupToken(
        selectedClient.clientId,
        savedDeployment.installationId,
        toCloudProvisioningInput(toDeploymentForm(savedDeployment), setupTokenHours));
      setCloudSetupToken(setupToken);
      setCloudBootstrapPackage(null);
      await loadCloudInstallationAuditEvents(selectedClient.clientId, savedDeployment.installationId);
      setMessage("Cloud setup token created.");
    });
  }

  async function handleCreateCloudBootstrapPackage() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const savedDeployment = await saveDeploymentForClient(selectedClient.clientId);
      const bootstrapPackage = await createCloudInstallationBootstrapPackage(
        selectedClient.clientId,
        savedDeployment.installationId,
        toCloudProvisioningInput(toDeploymentForm(savedDeployment), setupTokenHours));
      setCloudBootstrapPackage(bootstrapPackage);
      setCloudSetupToken(null);
      await loadCloudInstallationAuditEvents(selectedClient.clientId, savedDeployment.installationId);
      setMessage("Cloud bootstrap package created.");
    });
  }

  async function handleQueueCloudSupportCommand() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const savedDeployment = await saveDeploymentForClient(selectedClient.clientId);
      const queuedCommand = await queueCloudInstallationSupportCommand(
        selectedClient.clientId,
        savedDeployment.installationId,
        {
          commandType: supportCommandForm.commandType,
          reason: supportCommandForm.reason,
          requestedBy: supportCommandForm.requestedBy,
          expiresInHours: parseSupportCommandHours(supportCommandForm.expiresInHours)
        });
      setQueuedSupportCommand(queuedCommand);
      await loadCloudInstallationStatus(selectedClient.clientId, savedDeployment.installationId);
      await loadCloudInstallationAuditEvents(selectedClient.clientId, savedDeployment.installationId);
      setMessage(`${formatSupportCommandType(queuedCommand.commandType)} command queued.`);
    });
  }

  async function saveDeploymentForClient(clientId: string): Promise<ClientDeployment> {
    const savedDeployment = await configureClientDeployment(clientId, deploymentForm);
    const deployments = await listClientDeployments(clientId);

    setClientDeployments(sortClientDeployments(deployments));
    setDeploymentForm(toDeploymentForm(savedDeployment));
    setCloudInstallationId(savedDeployment.installationId);

    return savedDeployment;
  }

  async function handleIssueEntitlementSnapshot() {
    if (invoiceDraft === null) {
      return;
    }

    await runClientAction(async () => {
      const snapshot = await issueEntitlementFromPaidInvoiceDefaults(invoiceDraft.invoiceId);
      setIssuedEntitlementSnapshot(snapshot);
      setLatestEntitlementSnapshot(snapshot);
      setLatestEntitlementSnapshotMissing(false);
      setMessage("Entitlement snapshot issued.");
    });
  }

  async function handleResolveEntitlementReadiness() {
    setActiveDashboardModule("entitlements");

    if (selectedClient === null) {
      return;
    }

    if (!canIssueEntitlementSnapshot(invoiceDraft, recordedPayment)) {
      setError("");
      setMessage("Open entitlements. A paid invoice is required before issuing a new snapshot.");
      return;
    }

    await handleIssueEntitlementSnapshot();
  }

  async function runClientAction(action: () => Promise<void>) {
    setIsBusy(true);
    setError("");
    setMessage("");

    try {
      await action();
    } catch (caughtError) {
      setError(formatError(caughtError));
    } finally {
      setIsBusy(false);
    }
  }

  function applyLoadedClient(client: ClientDetails) {
    setSelectedClient(client);
    setLatestPortalInvitation(null);
    setPortalInvitations([]);
    setEditForm({
      legalName: client.legalName,
      displayName: client.displayName
    });
  }

  function applyLoadedDeployments(client: ClientDetails, deployments: ClientDeployment[]) {
    const sortedDeployments = sortClientDeployments(deployments);
    const primaryDeployment = getPrimaryDeployment(sortedDeployments);
    const nextForm = primaryDeployment === null
      ? createDefaultDeploymentForm(client)
      : toDeploymentForm(primaryDeployment);

    setClientDeployments(sortedDeployments);
    setDeploymentForm(nextForm);
    setCloudInstallationId(nextForm.installationId);
    setCloudInstallationStatus(null);
    clearCloudProvisioningArtifacts();
  }

  function upsertPortalInvitation(invitation: ClientPortalInvitation) {
    setPortalInvitations((current) => {
      const exists = current.some((item) => item.invitationId === invitation.invitationId);
      const next = exists
        ? current.map((item) => (item.invitationId === invitation.invitationId ? invitation : item))
        : [invitation, ...current];

      return [...next].sort((left, right) => right.invitedAtUtc.localeCompare(left.invitedAtUtc));
    });
  }

  function updateClientListRow(client: ClientDetails) {
    setClients((current) =>
      current.map((item) => (item.clientId === client.clientId ? toClientLookup(client) : item))
    );
  }

  function resetBillingForms() {
    setReceivableAccountForm(createDefaultReceivableAccountForm());
    setRevenueAccountForm(createDefaultRevenueAccountForm());
    setCashAccountForm(createDefaultCashAccountForm());
    setAccountingProfileForm(createDefaultAccountingProfileForm());
    setChargeCodeForm(createDefaultChargeCodeForm());
    setChargeRuleForm(createDefaultChargeRuleForm());
    setInvoiceDraftForm(createDefaultInvoiceDraftForm());
    setIssueInvoiceForm(createDefaultIssueInvoiceForm());
    setPaymentForm(createDefaultPaymentForm());
    setRefundForm(createDefaultRefundForm());
    setCreditApplicationForm(createDefaultCreditApplicationForm());
    setLatestChargeRule(null);
    setClientChargeRules([]);
    setInvoiceDraft(null);
    setIssuedInvoice(null);
    setVoidedInvoice(null);
    setIssuedCreditNote(null);
    setRecordedPayment(null);
    setIssuedRefund(null);
    setAppliedCredit(null);
    setLatestEntitlementSnapshot(null);
    setLatestEntitlementSnapshotMissing(false);
    setIssuedEntitlementSnapshot(null);
    setCloudInstallationStatus(null);
    setClientDeployments([]);
    setDeploymentForm(createDefaultDeploymentForm());
    clearCloudProvisioningArtifacts();
  }

  function applyBillingDefaults(client: ClientDetails, contract: ClientContract | null) {
    setReceivableAccountForm(createDefaultReceivableAccountForm(client));
    setRevenueAccountForm(createDefaultRevenueAccountForm(client));
    setCashAccountForm(createDefaultCashAccountForm(client));
    setAccountingProfileForm(createDefaultAccountingProfileForm(client, contract));
    setChargeCodeForm(createDefaultChargeCodeForm(client, contract));
    setChargeRuleForm(createDefaultChargeRuleForm(contract));
    setInvoiceDraftForm(createDefaultInvoiceDraftForm(client, contract));
    setIssueInvoiceForm(createDefaultIssueInvoiceForm());
    setPaymentForm(createDefaultPaymentForm(client));
    setRefundForm(createDefaultRefundForm(client));
    setCreditApplicationForm(createDefaultCreditApplicationForm(client));
    setLatestChargeRule(null);
    setClientChargeRules([]);
    setInvoiceDraft(null);
    setIssuedInvoice(null);
    setVoidedInvoice(null);
    setIssuedCreditNote(null);
    setRecordedPayment(null);
    setIssuedRefund(null);
    setAppliedCredit(null);
    setLatestEntitlementSnapshot(null);
    setLatestEntitlementSnapshotMissing(false);
    setIssuedEntitlementSnapshot(null);
    setCloudInstallationStatus(null);
    setClientDeployments([]);
    setDeploymentForm(createDefaultDeploymentForm(client));
    clearCloudProvisioningArtifacts();
    setClientStatement(null);
  }

  function clearCloudProvisioningArtifacts() {
    setCloudSetupToken(null);
    setCloudBootstrapPackage(null);
    setQueuedSupportCommand(null);
    setCloudAuditEvents([]);
    setCloudDiagnosticsReport(null);
  }

  function applyBillingContractDefaults(
    client: ClientDetails | null,
    contract: ClientContract | null
  ) {
    if (client === null) {
      return;
    }

    setChargeRuleForm((current) => ({
      ...current,
      contractId: contract?.contractId ?? "",
      unitPriceAmount: contract?.recurringAmount.toFixed(2) ?? current.unitPriceAmount,
      currencyCode: contract?.currencyCode ?? current.currencyCode,
      billingCycle: contract?.billingCycle ?? current.billingCycle,
      billingDayOfMonth: contract?.billingDayOfMonth.toString() ?? current.billingDayOfMonth,
      effectiveStartsOn: contract?.startsOn ?? current.effectiveStartsOn,
      effectiveEndsOn: contract?.endsOn ?? current.effectiveEndsOn
    }));
    setInvoiceDraftForm(createDefaultInvoiceDraftForm(client, contract));
    setIssueInvoiceForm(createDefaultIssueInvoiceForm());
    setPaymentForm(createDefaultPaymentForm(client));
    setRefundForm(createDefaultRefundForm(client));
    setCreditApplicationForm(createDefaultCreditApplicationForm(client));
    setInvoiceDraft(null);
    setIssuedInvoice(null);
    setVoidedInvoice(null);
    setIssuedCreditNote(null);
    setRecordedPayment(null);
    setIssuedRefund(null);
    setAppliedCredit(null);
    setIssuedEntitlementSnapshot(null);
  }

  const activeContract = getActiveContract(contracts);
  const canIssueCurrentEntitlementSnapshot = canIssueEntitlementSnapshot(
    invoiceDraft,
    recordedPayment
  );
  const clientReadinessItems = getClientReadinessItems({
    activeContract,
    accountingProfile,
    productModules,
    chargeRules: clientChargeRules,
    issuedEntitlementSnapshot,
    latestEntitlementSnapshot,
    latestEntitlementSnapshotMissing,
    cloudInstallationStatus,
    latestPortalInvitation,
    portalInvitations
  });
  const dashboardMetrics = getDashboardMetrics({
    activeContract,
    accountCodeRangeCount: accountCodeRanges.length,
    invoiceDraft,
    recordedPayment,
    issuedEntitlementSnapshot,
    latestEntitlementSnapshot,
    cloudInstallationStatus,
    clientStatement
  });
  const dashboardNavigation = getDashboardNavigation(dashboardMetrics, clients.length, selectedClient);
  const activeNavigationItem = getDashboardNavigationItem(
    dashboardNavigation,
    activeDashboardModule
  );

  return (
    <div className="client-desk control-desk-shell">
      <aside className="control-sidebar" aria-label="Client control navigation">
        <div className="sidebar-brand">
          <div>
            <span>SafarSuite</span>
            <h1>Control Desk</h1>
          </div>
          {selectedClient !== null && (
            <span className={`status-pill ${selectedClient.status.toLowerCase()}`}>
              {selectedClient.status}
            </span>
          )}
        </div>

        <nav className="module-sidebar-nav" aria-label="Client modules">
          {dashboardNavigation.map((item) => (
            <button
              aria-current={activeDashboardModule === item.module ? "page" : undefined}
              className={`module-nav-item ${item.tone}${
                activeDashboardModule === item.module ? " active" : ""
              }`}
              key={item.module}
              type="button"
              onClick={() => setActiveDashboardModule(item.module)}
            >
              <item.Icon size={18} />
              <span>
                <strong>{item.label}</strong>
                <small>{item.summary}</small>
              </span>
            </button>
          ))}
        </nav>

      </aside>

      <main className="control-main-window">
        <div className="status-line" aria-live="polite">
          {error !== "" && (
            <span className="status-error">
              <AlertCircle size={16} />
              {error}
            </span>
          )}
          {message !== "" && (
            <span className="status-success">
              <CheckCircle2 size={16} />
              {message}
            </span>
          )}
        </div>

        <section className="module-window">
          <header className="module-window-header">
            <div>
              <span>{selectedClient?.code ?? "No client selected"}</span>
              <h1>{activeNavigationItem.label}</h1>
              <p>{activeNavigationItem.description}</p>
            </div>
            {selectedClient !== null && (
              <div className="module-window-client">
                <span>{selectedClient.displayName}</span>
                <strong>{selectedClient.legalName}</strong>
              </div>
            )}
          </header>

          {selectedClient !== null && (
            <ClientReadinessStrip
              items={clientReadinessItems}
              onNavigate={setActiveDashboardModule}
            />
          )}

          <div className="module-window-body">
            {activeDashboardModule === "clients" && (
              <section className="client-window-strip client-window-module" aria-label="Client workspace">
                <ClientListPanel
                  clients={clients}
                  selectedClientId={selectedClientId}
                  isBusy={isBusy}
                  onSelect={setSelectedClientId}
                  onRefresh={() => refreshClients()}
                />

                <section className="client-window-create" aria-label="Create client">
                  <div className="client-window-create-heading">
                    <span>New client</span>
                    <strong>Quick add</strong>
                  </div>
                  <ClientCreateForm
                    value={createForm}
                    isBusy={isBusy}
                    onChange={setCreateForm}
                    onSubmit={handleCreateClient}
                  />
                </section>
              </section>
            )}

            {activeDashboardModule === "dashboard" && (
              <section className="client-stat-window">
                <div className="client-dashboard-heading">
                  <div>
                    <span>{selectedClient?.code ?? "No client selected"}</span>
                    <h2>{selectedClient?.displayName ?? "Select a client"}</h2>
                  </div>
                  {selectedClient !== null && (
                    <span className={`status-pill large ${selectedClient.status.toLowerCase()}`}>
                      {selectedClient.status}
                    </span>
                  )}
                </div>

                <div className="dashboard-metrics stat-action-grid">
                  {dashboardMetrics.map((metric) => (
                    <button
                      className={`dashboard-metric stat-action ${metric.tone}`}
                      key={metric.label}
                      type="button"
                      onClick={() => setActiveDashboardModule(metric.module)}
                    >
                      <metric.Icon size={20} />
                      <div>
                        <span>{metric.label}</span>
                        <strong>{metric.value}</strong>
                        <small>{metric.summary}</small>
                      </div>
                      <ArrowRight className="stat-action-arrow" size={16} />
                    </button>
                  ))}
                </div>
              </section>
            )}

            {activeDashboardModule === "profile" && (
              <ClientDetailPanel
                client={selectedClient}
                accountingProfile={accountingProfile}
                accountingProfileMissing={accountingProfileMissing}
                editValue={editForm}
                contactValue={contactForm}
                noteValue={noteForm}
                latestPortalInvitation={latestPortalInvitation}
                portalInvitations={portalInvitations}
                isBusy={isBusy}
                onEditChange={setEditForm}
                onContactChange={setContactForm}
                onNoteChange={setNoteForm}
                onSave={handleUpdateClient}
                onActivate={handleActivateClient}
                onSuspend={handleSuspendClient}
                onAddContact={handleAddContact}
                onInvitePortalContact={handleInvitePortalContact}
                onRefreshPortalInvitations={handleRefreshPortalInvitations}
                onResendPortalInvitation={handleResendPortalInvitation}
                onRevokePortalInvitation={handleRevokePortalInvitation}
                onAddNote={handleAddNote}
              />
            )}

            {activeDashboardModule === "contracts" && (
              <ClientContractsPanel
                contracts={contracts}
                productModules={productModules}
                chargeRules={clientChargeRules}
                latestSnapshot={latestEntitlementSnapshot}
                latestSnapshotMissing={latestEntitlementSnapshotMissing}
                canIssueEntitlementSnapshot={canIssueCurrentEntitlementSnapshot}
                value={contractForm}
                isBusy={isBusy || selectedClient === null}
                onChange={setContractForm}
                onCreate={handleCreateContract}
                onReplaceActive={handleReplaceActiveContract}
                onSuspend={handleSuspendContract}
                onPrepareModuleBilling={handlePrepareModuleBilling}
                onResolveEntitlementReadiness={handleResolveEntitlementReadiness}
              />
            )}

            {activeDashboardModule === "accounting" && (
              <>
                <ChartOfAccountsPanel
                  accounts={ledgerAccounts}
                  ranges={accountCodeRanges}
                  filters={ledgerAccountFilters}
                  selectedRangeRole={selectedAccountCodeRangeRole}
                  rangeValue={accountCodeRangeForm}
                  accountMode={selectedLedgerAccountId === "" ? "create" : "edit"}
                  accountValue={ledgerAccountEditorForm}
                  activity={ledgerAccountActivity}
                  journalEntries={journalEntries}
                  isBusy={isBusy}
                  onFiltersChange={handleLedgerAccountFiltersChange}
                  onRangeSelect={handleSelectAccountCodeRange}
                  onRangeChange={setAccountCodeRangeForm}
                  onSaveRange={handleSaveAccountCodeRange}
                  onAccountChange={setLedgerAccountEditorForm}
                  onNewAccount={handleNewLedgerAccount}
                  onEditAccount={handleEditLedgerAccount}
                  onSaveAccount={handleSaveLedgerAccount}
                  onToggleAccountStatus={handleToggleLedgerAccountStatus}
                  onViewAccountActivity={handleViewLedgerAccountActivity}
                  onViewJournalEntry={handleViewJournalEntryFromActivity}
                  onSuggestAccountCode={handleSuggestAccountingLedgerAccountCode}
                  onRefresh={() => refreshAccountingSetup()}
                />
                <AccountingControlsPanel
                  settings={accountingControlSettings}
                  value={accountingControlSettingsForm}
                  accounts={ledgerAccounts}
                  isBusy={isBusy}
                  onValueChange={setAccountingControlSettingsForm}
                  onSave={handleSaveAccountingControls}
                  onRefresh={() => refreshAccountingControls()}
                />
                <AccountingPeriodsPanel
                  periods={accountingPeriods}
                  readiness={accountingPeriodReadiness}
                  closeJournalPreview={accountingPeriodCloseJournalPreview}
                  value={accountingPeriodForm}
                  isBusy={isBusy}
                  onValueChange={setAccountingPeriodForm}
                  onPrepareNext={handlePrepareNextAccountingPeriod}
                  onCreate={handleCreateAccountingPeriod}
                  onCheckReadiness={handleCheckAccountingPeriodReadiness}
                  onPreviewCloseJournal={handlePreviewAccountingCloseJournal}
                  onClose={handleCloseAccountingPeriod}
                  onReopen={handleReopenAccountingPeriod}
                  onRefresh={() => refreshAccountingPeriods()}
                />
                <LedgerAccountReconciliationPanel
                  reconciliation={ledgerAccountReconciliation}
                  repairPlan={ledgerAccountRepairPlan}
                  isBusy={isBusy}
                  onRefresh={() => refreshLedgerAccountReconciliation()}
                />
                <JournalWorkbenchPanel
                  accounts={ledgerAccounts}
                  periods={accountingPeriods}
                  entries={journalEntries}
                  filters={journalEntryFilters}
                  value={manualJournalEntryForm}
                  focusedJournalEntryId={focusedJournalEntryId}
                  focusedJournalEntry={focusedJournalEntry}
                  sourceDocumentsByJournalEntryId={journalSourceDocumentsById}
                  isBusy={isBusy}
                  onFiltersChange={setJournalEntryFilters}
                  onValueChange={setManualJournalEntryForm}
                  onFocusJournalEntry={handleFocusJournalEntry}
                  onPost={handlePostManualJournalEntry}
                  onVoidEntry={handleVoidManualJournalEntry}
                  onOpenSourceDocument={handleOpenJournalSourceDocument}
                  getSourceDocumentLabel={getJournalSourceDocumentLabel}
                  getSourceDocumentClientLabel={getJournalSourceDocumentClientLabel}
                  onRefresh={() => refreshJournalEntries()}
                />
                <TrialBalancePanel
                  balance={trialBalance}
                  filters={trialBalanceFilters}
                  isBusy={isBusy}
                  onFiltersChange={setTrialBalanceFilters}
                  onViewAccountActivity={handleViewTrialBalanceAccountActivity}
                  onRefresh={() => refreshTrialBalance()}
                />
              </>
            )}

            {activeDashboardModule === "billing" && (
              <ClientBillingSetupPanel
                client={selectedClient}
                contracts={contracts}
                productModules={productModules}
                initialStep={preferredBillingStep}
                accountingProfile={accountingProfile}
                accountingProfileMissing={accountingProfileMissing}
                chargeCodes={chargeCodes}
                receivableAccountValue={receivableAccountForm}
                revenueAccountValue={revenueAccountForm}
                accountingProfileValue={accountingProfileForm}
                chargeCodeValue={chargeCodeForm}
                chargeRuleValue={chargeRuleForm}
                invoiceDraftValue={invoiceDraftForm}
                issueInvoiceValue={issueInvoiceForm}
                latestChargeRule={latestChargeRule}
                invoiceDraft={invoiceDraft}
                issuedInvoice={issuedInvoice}
                voidedInvoice={voidedInvoice}
                issuedCreditNote={issuedCreditNote}
                isBusy={isBusy || selectedClient === null}
                onReceivableAccountChange={setReceivableAccountForm}
                onRevenueAccountChange={setRevenueAccountForm}
                onAccountingProfileChange={setAccountingProfileForm}
                onChargeCodeChange={setChargeCodeForm}
                onChargeRuleChange={setChargeRuleForm}
                onInvoiceDraftChange={setInvoiceDraftForm}
                onIssueInvoiceChange={setIssueInvoiceForm}
                onCreateReceivableAccount={handleCreateReceivableAccount}
                onCreateRevenueAccount={handleCreateRevenueAccount}
                onSaveAccountingProfile={handleSaveAccountingProfile}
                onCreateChargeCode={handleCreateChargeCode}
                onRefreshChargeCodes={refreshChargeCodes}
                onCreateChargeRule={handleCreateChargeRule}
                onGenerateInvoiceDraft={handleGenerateInvoiceDraft}
                onIssueInvoice={handleIssueInvoice}
                onVoidInvoice={handleVoidInvoice}
                onIssueCreditNote={handleIssueCreditNote}
                onViewJournalEntry={handleViewJournalEntryById}
              />
            )}

            {activeDashboardModule === "payments" && (
              <PaymentReceiptPanel
                invoiceDraft={invoiceDraft}
                issuedInvoice={issuedInvoice}
                initialStep={preferredPaymentStep}
                accountingProfile={accountingProfile}
                cashAccountValue={cashAccountForm}
                paymentValue={paymentForm}
                refundValue={refundForm}
                creditApplicationValue={creditApplicationForm}
                recordedPayment={recordedPayment}
                issuedRefund={issuedRefund}
                appliedCredit={appliedCredit}
                clientStatement={clientStatement}
                isBusy={isBusy || selectedClient === null}
                onCashAccountChange={setCashAccountForm}
                onPaymentChange={setPaymentForm}
                onRefundChange={setRefundForm}
                onCreditApplicationChange={setCreditApplicationForm}
                onCreateCashAccount={handleCreateCashAccount}
                onRecordPayment={handleRecordInvoicePayment}
                onIssueRefund={handleIssueClientRefund}
                onApplyCredit={handleApplyClientCredit}
                onApprovePayment={handleApproveInvoicePayment}
                onRejectPayment={handleRejectInvoicePayment}
                onReversePayment={handleReverseInvoicePayment}
                onViewJournalEntry={handleViewJournalEntryById}
              />
            )}

            {activeDashboardModule === "entitlements" && (
              <EntitlementSnapshotPanel
                invoiceDraft={invoiceDraft}
                recordedPayment={recordedPayment}
                productModules={productModules}
                latestSnapshot={latestEntitlementSnapshot}
                latestSnapshotMissing={latestEntitlementSnapshotMissing}
                issuedSnapshot={issuedEntitlementSnapshot}
                isBusy={isBusy || selectedClient === null}
                onIssueFromPaidInvoice={handleIssueEntitlementSnapshot}
                onRefreshLatest={handleRefreshLatestEntitlementSnapshot}
              />
            )}

            {activeDashboardModule === "cloud" && (
              <CloudInstallationStatusPanel
                client={selectedClient}
                installationId={cloudInstallationId}
                deployments={clientDeployments}
                selectedDeploymentId={getSelectedDeploymentId(clientDeployments, cloudInstallationId)}
                deploymentValue={deploymentForm}
                setupTokenHours={setupTokenHours}
                status={cloudInstallationStatus}
                setupToken={cloudSetupToken}
                bootstrapPackage={cloudBootstrapPackage}
                supportCommandValue={supportCommandForm}
                queuedSupportCommand={queuedSupportCommand}
                auditEvents={cloudAuditEvents}
                diagnosticsReport={cloudDiagnosticsReport}
                isBusy={isBusy || selectedClient === null}
                onInstallationIdChange={handleCloudInstallationIdChange}
                onDeploymentValueChange={handleDeploymentValueChange}
                onSetupTokenHoursChange={setSetupTokenHours}
                onDeploymentSelect={handleSelectClientDeployment}
                onSaveDeployment={handleSaveClientDeployment}
                onCreateSetupToken={handleCreateCloudSetupToken}
                onCreateBootstrapPackage={handleCreateCloudBootstrapPackage}
                onSupportCommandValueChange={handleSupportCommandValueChange}
                onQueueSupportCommand={handleQueueCloudSupportCommand}
                onRefreshAuditEvents={handleRefreshCloudAuditEvents}
                onRefreshDiagnostics={handleRefreshCloudDiagnostics}
                onRefresh={handleRefreshCloudInstallationStatus}
              />
            )}

            {activeDashboardModule === "statement" && (
              <ClientStatementPanel
                client={selectedClient}
                statement={clientStatement}
                isBusy={isBusy || selectedClient === null}
                onRefresh={handleRefreshClientStatement}
              />
            )}
          </div>
        </section>
      </main>
    </div>
  );
}

function ClientReadinessStrip({
  items,
  onNavigate
}: {
  items: ClientReadinessItem[];
  onNavigate: (module: DashboardModule) => void;
}) {
  return (
    <section className="client-readiness-strip" aria-label="Client readiness">
      {items.map((item) => (
        <button
          className={`client-readiness-item ${item.tone}`}
          key={item.key}
          type="button"
          onClick={() => onNavigate(item.module)}
          title={item.label}
        >
          <item.Icon size={16} />
          <span>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
            <em>{item.summary}</em>
          </span>
        </button>
      ))}
    </section>
  );
}

type DashboardMetric = {
  label: string;
  value: string;
  summary: string;
  tone: "neutral" | "ready" | "warning";
  Icon: LucideIcon;
  module: DashboardModule;
};

type DashboardNavigationItem = {
  module: DashboardModule;
  label: string;
  summary: string;
  description: string;
  tone: DashboardMetric["tone"];
  Icon: LucideIcon;
};

type ClientReadinessItem = {
  key: string;
  label: string;
  value: string;
  summary: string;
  tone: DashboardMetric["tone"];
  Icon: LucideIcon;
  module: DashboardModule;
};

type DashboardMetricInput = {
  activeContract: ClientContract | null;
  accountCodeRangeCount: number;
  invoiceDraft: InvoiceDraft | null;
  recordedPayment: RecordedInvoicePayment | null;
  issuedEntitlementSnapshot: IssuedEntitlementSnapshot | null;
  latestEntitlementSnapshot: EntitlementSnapshot | null;
  cloudInstallationStatus: ControlCloudInstallationStatus | null;
  clientStatement: ClientStatement | null;
};

type ClientReadinessInput = {
  activeContract: ClientContract | null;
  accountingProfile: ClientAccountingProfile | null;
  productModules: ProductModule[];
  chargeRules: ClientChargeRule[];
  issuedEntitlementSnapshot: IssuedEntitlementSnapshot | null;
  latestEntitlementSnapshot: EntitlementSnapshot | null;
  latestEntitlementSnapshotMissing: boolean;
  cloudInstallationStatus: ControlCloudInstallationStatus | null;
  latestPortalInvitation: ClientPortalInvitation | null;
  portalInvitations: ClientPortalInvitation[];
};

function getClientReadinessItems({
  activeContract,
  accountingProfile,
  productModules,
  chargeRules,
  issuedEntitlementSnapshot,
  latestEntitlementSnapshot,
  latestEntitlementSnapshotMissing,
  cloudInstallationStatus,
  latestPortalInvitation,
  portalInvitations
}: ClientReadinessInput): ClientReadinessItem[] {
  const contractStatus = activeContract?.status ?? "Missing";
  const contractIsReady = activeContract?.status.toLowerCase() === "active";

  return [
    {
      key: "contract",
      label: "Contract",
      value: contractStatus,
      summary: activeContract === null
        ? "Create agreement"
        : `${activeContract.allowedDevices} devices, ${activeContract.allowedBranches} branches`,
      tone: contractIsReady ? "ready" : "warning",
      Icon: FileText,
      module: "contracts"
    },
    getBillingReadinessItem(activeContract, accountingProfile, productModules, chargeRules),
    getEntitlementReadinessItem(
      activeContract,
      issuedEntitlementSnapshot ?? latestEntitlementSnapshot,
      latestEntitlementSnapshotMissing
    ),
    getCloudReadinessItem(cloudInstallationStatus),
    getPortalReadinessItem(latestPortalInvitation, portalInvitations)
  ];
}

function getBillingReadinessItem(
  activeContract: ClientContract | null,
  accountingProfile: ClientAccountingProfile | null,
  productModules: ProductModule[],
  chargeRules: ClientChargeRule[]
): ClientReadinessItem {
  if (accountingProfile === null) {
    return {
      key: "billing",
      label: "Billing",
      value: "Not linked",
      summary: "Accounting profile",
      tone: "warning",
      Icon: ReceiptText,
      module: "billing"
    };
  }

  if (activeContract === null) {
    return {
      key: "billing",
      label: "Billing",
      value: accountingProfile.defaultCurrencyCode,
      summary: "Needs contract",
      tone: "warning",
      Icon: ReceiptText,
      module: "billing"
    };
  }

  const paidAddOnCodes = getPaidAddOnModuleCodes(activeContract, productModules);
  const billedModuleCodes = getBilledModuleCodes(chargeRules, activeContract);
  const missingCount = paidAddOnCodes.filter((moduleCode) => !billedModuleCodes.has(moduleCode)).length;

  if (missingCount > 0) {
    return {
      key: "billing",
      label: "Billing",
      value: `${missingCount} missing`,
      summary: "Paid add-on rules",
      tone: "warning",
      Icon: ReceiptText,
      module: "billing"
    };
  }

  return {
    key: "billing",
    label: "Billing",
    value: "Ready",
    summary: paidAddOnCodes.length === 0 ? "Base plan" : `${paidAddOnCodes.length} add-ons covered`,
    tone: "ready",
    Icon: ReceiptText,
    module: "billing"
  };
}

function getEntitlementReadinessItem(
  activeContract: ClientContract | null,
  snapshot: EntitlementSnapshot | null,
  latestEntitlementSnapshotMissing: boolean
): ClientReadinessItem {
  if (activeContract === null) {
    return {
      key: "entitlement",
      label: "Entitlement",
      value: "Blocked",
      summary: "Needs contract",
      tone: "warning",
      Icon: KeyRound,
      module: "entitlements"
    };
  }

  if (snapshot === null) {
    return {
      key: "entitlement",
      label: "Entitlement",
      value: latestEntitlementSnapshotMissing ? "Missing" : "Not loaded",
      summary: "Snapshot required",
      tone: "warning",
      Icon: KeyRound,
      module: "entitlements"
    };
  }

  const contractModuleCodes = getEnabledModuleCodes(activeContract.modules);
  const snapshotModuleCodes = getEnabledModuleCodes(snapshot.modules);
  const hasContractMismatch = snapshot.contractId !== activeContract.contractId;
  const hasLimitMismatch =
    snapshot.allowedDevices !== activeContract.allowedDevices
    || snapshot.allowedBranches !== activeContract.allowedBranches;
  const hasModuleMismatch =
    contractModuleCodes.some((moduleCode) => !snapshotModuleCodes.includes(moduleCode))
    || snapshotModuleCodes.some((moduleCode) => !contractModuleCodes.includes(moduleCode));

  if (hasContractMismatch || hasLimitMismatch || hasModuleMismatch) {
    const differences = [
      hasContractMismatch ? "contract" : null,
      hasLimitMismatch ? "limits" : null,
      hasModuleMismatch ? "modules" : null
    ].filter((item): item is string => item !== null);

    return {
      key: "entitlement",
      label: "Entitlement",
      value: "Out of sync",
      summary: differences.join(", "),
      tone: "warning",
      Icon: KeyRound,
      module: "entitlements"
    };
  }

  return {
    key: "entitlement",
    label: "Entitlement",
    value: snapshot.status,
    summary: `${snapshotModuleCodes.length} modules aligned`,
    tone: snapshot.status.toLowerCase() === "active" ? "ready" : "warning",
    Icon: KeyRound,
    module: "entitlements"
  };
}

function getCloudReadinessItem(
  cloudInstallationStatus: ControlCloudInstallationStatus | null
): ClientReadinessItem {
  const cloudHeartbeat = cloudInstallationStatus?.latestHeartbeat ?? null;
  const deploymentProfile = getCloudDeploymentProfile(cloudInstallationStatus);
  const deploymentSummary = formatCloudDeploymentSummary(deploymentProfile);
  const cloudStatus = cloudHeartbeat?.licenseStatus
    ?? cloudInstallationStatus?.installationStatus
    ?? "Not loaded";
  const normalizedCloudStatus = cloudStatus.toLowerCase();
  const cloudReady =
    normalizedCloudStatus === "active"
    || normalizedCloudStatus === "healthy"
    || normalizedCloudStatus === "registered";

  return {
    key: "cloud",
    label: "Cloud",
    value: cloudStatus,
    summary: cloudHeartbeat === null
      ? deploymentSummary
      : `${deploymentSummary} / ${formatDashboardDateTime(cloudHeartbeat.receivedAtUtc)}`,
    tone: cloudReady ? "ready" : cloudInstallationStatus === null ? "neutral" : "warning",
    Icon: Cloud,
    module: "cloud"
  };
}

function getPortalReadinessItem(
  latestPortalInvitation: ClientPortalInvitation | null,
  portalInvitations: ClientPortalInvitation[]
): ClientReadinessItem {
  const invitation = getLatestPortalInvitation(latestPortalInvitation, portalInvitations);

  if (invitation === null) {
    return {
      key: "portal",
      label: "Portal",
      value: "No invite",
      summary: "Client access",
      tone: "neutral",
      Icon: UserRound,
      module: "profile"
    };
  }

  const status = invitation.status.toLowerCase();
  const isReady = status === "accepted";
  const isWarning = status === "revoked" || status === "expired";

  return {
    key: "portal",
    label: "Portal",
    value: invitation.status,
    summary: invitation.email,
    tone: isReady ? "ready" : isWarning ? "warning" : "neutral",
    Icon: UserRound,
    module: "profile"
  };
}

function getDashboardMetrics({
  activeContract,
  accountCodeRangeCount,
  invoiceDraft,
  recordedPayment,
  issuedEntitlementSnapshot,
  latestEntitlementSnapshot,
  cloudInstallationStatus,
  clientStatement
}: DashboardMetricInput): DashboardMetric[] {
  const entitlementSnapshot = issuedEntitlementSnapshot ?? latestEntitlementSnapshot;
  const cloudHeartbeat = cloudInstallationStatus?.latestHeartbeat ?? null;
  const deploymentProfile = getCloudDeploymentProfile(cloudInstallationStatus);
  const deploymentSummary = formatCloudDeploymentSummary(deploymentProfile);
  const cloudStatus = cloudHeartbeat?.licenseStatus
    ?? cloudInstallationStatus?.installationStatus
    ?? "Not loaded";
  const normalizedCloudStatus = cloudStatus.toLowerCase();
  const primaryStatementSummary = clientStatement?.currencySummaries[0] ?? null;

  return [
    {
      label: "Contract",
      value: activeContract === null ? "Missing" : activeContract.status,
      summary: "Agreement, pricing, and allowances",
      tone: activeContract?.status.toLowerCase() === "active" ? "ready" : "warning",
      Icon: FileText,
      module: "contracts"
    },
    {
      label: "Accounting",
      value: accountCodeRangeCount === 0 ? "Not loaded" : `${accountCodeRangeCount} ranges`,
      summary: "COA setup and ledger register",
      tone: accountCodeRangeCount === 0 ? "warning" : "ready",
      Icon: Banknote,
      module: "accounting"
    },
    {
      label: "Invoice",
      value: invoiceDraft === null
        ? "No draft"
        : `${invoiceDraft.status} ${invoiceDraft.balanceDue.toFixed(2)} ${invoiceDraft.currencyCode}`,
      summary: "Draft, issue, and receivable state",
      tone: invoiceDraft?.status.toLowerCase() === "paid" ? "ready" : "neutral",
      Icon: ReceiptText,
      module: "billing"
    },
    {
      label: "Payment",
      value: recordedPayment === null ? "Pending" : recordedPayment.paymentStatus,
      summary: "Receipt posting and balance",
      tone: recordedPayment?.paymentStatus.toLowerCase() === "approved" ? "ready" : "neutral",
      Icon: CheckCircle2,
      module: "payments"
    },
    {
      label: "Entitlement",
      value: entitlementSnapshot === null ? "Not issued" : entitlementSnapshot.status,
      summary: "Cloud access snapshot",
      tone: entitlementSnapshot?.status.toLowerCase() === "active" ? "ready" : "neutral",
      Icon: KeyRound,
      module: "entitlements"
    },
    {
      label: "Cloud",
      value: cloudStatus,
      summary: cloudHeartbeat === null
        ? deploymentSummary
        : `${deploymentSummary} / ${formatDashboardDateTime(cloudHeartbeat.receivedAtUtc)}`,
      tone:
        normalizedCloudStatus === "active"
          || normalizedCloudStatus === "healthy"
          || normalizedCloudStatus === "registered"
          ? "ready"
          : cloudInstallationStatus === null
            ? "neutral"
            : "warning",
      Icon: Cloud,
      module: "cloud"
    },
    {
      label: "Statement",
      value: primaryStatementSummary === null
        ? "No balance"
        : `${primaryStatementSummary.balanceDue.toFixed(2)} ${primaryStatementSummary.currencyCode}`,
      summary: "Invoices, receipts, and GL trail",
      tone: primaryStatementSummary !== null && primaryStatementSummary.balanceDue === 0 ? "ready" : "neutral",
      Icon: ScrollText,
      module: "statement"
    }
  ];
}

function getDashboardNavigation(
  metrics: DashboardMetric[],
  clientCount: number,
  selectedClient: ClientDetails | null
): DashboardNavigationItem[] {
  const contractMetric = findDashboardMetric(metrics, "Contract");
  const accountingMetric = findDashboardMetric(metrics, "Accounting");
  const invoiceMetric = findDashboardMetric(metrics, "Invoice");
  const paymentMetric = findDashboardMetric(metrics, "Payment");
  const entitlementMetric = findDashboardMetric(metrics, "Entitlement");
  const cloudMetric = findDashboardMetric(metrics, "Cloud");
  const statementMetric = findDashboardMetric(metrics, "Statement");
  const selectedClientStatus = selectedClient?.status ?? "No client";

  return [
    {
      module: "dashboard",
      label: "Dashboard",
      summary: "Current stats",
      description: "Current operational status for the selected client.",
      tone: "neutral",
      Icon: LayoutDashboard
    },
    {
      module: "clients",
      label: "Clients",
      summary: `${clientCount} total`,
      description: "Select, refresh, and quick add clients.",
      tone: selectedClient === null ? "warning" : "neutral",
      Icon: Users
    },
    {
      module: "profile",
      label: "Profile",
      summary: selectedClientStatus,
      description: "Client profile, contacts, support notes, and lifecycle actions.",
      tone: selectedClient?.status.toLowerCase() === "active" ? "ready" : "neutral",
      Icon: UserRound
    },
    {
      module: "contracts",
      label: "Contracts",
      summary: contractMetric.value,
      description: "Agreement terms, allowed modules, devices, branches, and contract replacement.",
      tone: contractMetric.tone,
      Icon: FileText
    },
    {
      module: "accounting",
      label: "Accounting",
      summary: accountingMetric.value,
      description: "Chart of accounts, code ranges, and ledger setup.",
      tone: accountingMetric.tone,
      Icon: ListTree
    },
    {
      module: "billing",
      label: "Billing",
      summary: invoiceMetric.value,
      description: "Accounting profile, charge rules, invoice drafts, and invoice issue.",
      tone: invoiceMetric.tone,
      Icon: ReceiptText
    },
    {
      module: "payments",
      label: "Payments",
      summary: paymentMetric.value,
      description: "Cash or bank account setup and invoice payment receipt.",
      tone: paymentMetric.tone,
      Icon: Banknote
    },
    {
      module: "entitlements",
      label: "Entitlements",
      summary: entitlementMetric.value,
      description: "Issue and refresh the latest cloud entitlement snapshot.",
      tone: entitlementMetric.tone,
      Icon: KeyRound
    },
    {
      module: "cloud",
      label: "Cloud",
      summary: cloudMetric.value,
      description: "Control Cloud heartbeat, license, entitlement, and command status.",
      tone: cloudMetric.tone,
      Icon: Cloud
    },
    {
      module: "statement",
      label: "Statement",
      summary: statementMetric.value,
      description: "Client invoices, payments, receivable balance, and journal postings.",
      tone: statementMetric.tone,
      Icon: ScrollText
    }
  ];
}

function getDashboardNavigationItem(
  items: DashboardNavigationItem[],
  module: DashboardModule
): DashboardNavigationItem {
  return items.find((item) => item.module === module) ?? items[0] ?? {
    module: "dashboard",
    label: "Dashboard",
    summary: "Current stats",
    description: "Current operational status for the selected client.",
    tone: "neutral",
    Icon: LayoutDashboard
  };
}

function findDashboardMetric(metrics: DashboardMetric[], label: string): DashboardMetric {
  return metrics.find((metric) => metric.label === label) ?? {
    label,
    value: "Unknown",
    summary: "No signal",
    tone: "neutral",
    Icon: LayoutDashboard,
    module: "dashboard"
  };
}

function createDefaultReceivableAccountForm(client?: ClientDetails): LedgerAccountFormInput {
  return {
    code: "151000001",
    name: client === undefined
      ? "Accounts Receivable"
      : `Accounts Receivable - ${client.displayName}`,
    type: "Asset",
    normalBalance: "Debit",
    parentAccountId: "",
    isPostingAccount: true
  };
}

function createDefaultRevenueAccountForm(client?: ClientDetails): LedgerAccountFormInput {
  return {
    code: "41000",
    name: client === undefined
      ? "Subscription Revenue"
      : `Subscription Revenue - ${client.displayName}`,
    type: "Revenue",
    normalBalance: "Credit",
    parentAccountId: "",
    isPostingAccount: true
  };
}

function createDefaultCashAccountForm(client?: ClientDetails): LedgerAccountFormInput {
  return {
    code: "14110",
    name: client === undefined ? "Cash or Bank" : `Cash/Bank - ${client.displayName}`,
    type: "Asset",
    normalBalance: "Debit",
    parentAccountId: "",
    isPostingAccount: true
  };
}

function createDefaultLedgerAccountEditorForm(
  range?: AccountCodeRange | null
): LedgerAccountEditorInput {
  return {
    code: "",
    name: "",
    type: range?.accountType ?? "Asset",
    normalBalance: range?.normalBalance ?? "Debit",
    level: getDefaultLedgerAccountLevel(range),
    parentAccountId: "",
    isPostingAccount: range?.isPostingAccount ?? true,
    status: "Active"
  };
}

function getDefaultLedgerAccountLevel(
  range?: AccountCodeRange | null,
  isPostingAccount = range?.isPostingAccount ?? true
): string {
  if (range !== null && range !== undefined) {
    if (hasLedgerRangeIntent(range, "Header")) {
      return "Header";
    }

    if (hasLedgerRangeIntent(range, "Total")) {
      return "Total";
    }

    if (hasLedgerRangeIntent(range, "Control")) {
      return "Control";
    }

    if (hasLedgerRangeIntent(range, "Master")) {
      return "Master";
    }

    if ((range.parentCode ?? "").trim() !== "") {
      return "Subsidiary";
    }
  }

  return isPostingAccount ? "Detail" : "Master";
}

function hasLedgerRangeIntent(range: AccountCodeRange, intent: string): boolean {
  const normalizedIntent = intent.toLowerCase();

  return range.role.toLowerCase().includes(normalizedIntent)
    || range.displayName.toLowerCase().includes(normalizedIntent);
}

function createDefaultManualJournalEntryForm(value = new Date()): ManualJournalEntryInput {
  return {
    entryDate: toDateInputValue(value),
    currencyCode: "PKR",
    sourceReference: defaultManualJournalReference(value),
    memo: "",
    lines: [
      {
        ledgerAccountId: "",
        debit: "",
        credit: "",
        description: ""
      },
      {
        ledgerAccountId: "",
        debit: "",
        credit: "",
        description: ""
      }
    ]
  };
}

function createDefaultAccountingPeriodForm(
  periods: AccountingPeriod[] = [],
  companyCode = accountingCompanyCode
): AccountingPeriodFormInput {
  const nextPeriod = getNextMonthlyPeriod(periods);

  return {
    companyCode: normalizeAccountingCompanyCode(companyCode),
    name: formatAccountingPeriodName(nextPeriod.startsOn),
    startsOn: nextPeriod.startsOn,
    endsOn: nextPeriod.endsOn
  };
}

function createDefaultAccountingControlSettingsForm(
  companyCode = accountingCompanyCode
): AccountingControlSettingsInput {
  return {
    ...defaultAccountingControlSettingsForm,
    companyCode: normalizeAccountingCompanyCode(companyCode)
  };
}

function toAccountingControlSettingsForm(
  settings: AccountingControlSettings
): AccountingControlSettingsInput {
  return {
    companyCode: normalizeAccountingCompanyCode(settings.companyCode),
    baseCurrencyCode: settings.baseCurrencyCode,
    retainedEarningsAccountId: settings.retainedEarningsAccountId ?? "",
    incomeSummaryAccountId: settings.incomeSummaryAccountId ?? "",
    roundingAccountId: settings.roundingAccountId ?? ""
  };
}

function getNextMonthlyPeriod(periods: AccountingPeriod[]): { startsOn: string; endsOn: string } {
  if (periods.length === 0) {
    const today = new Date();
    const startsOn = new Date(today.getFullYear(), today.getMonth(), 1);
    const endsOn = new Date(today.getFullYear(), today.getMonth() + 1, 0);

    return {
      startsOn: toDateInputValue(startsOn),
      endsOn: toDateInputValue(endsOn)
    };
  }

  const latestPeriod = [...periods].sort((left, right) => right.endsOn.localeCompare(left.endsOn))[0];
  const startsOn = addDays(parseDateInput(latestPeriod.endsOn), 1);
  const endsOn = new Date(startsOn.getFullYear(), startsOn.getMonth() + 1, 0);

  return {
    startsOn: toDateInputValue(startsOn),
    endsOn: toDateInputValue(endsOn)
  };
}

function formatAccountingPeriodName(startsOn: string): string {
  return parseDateInput(startsOn).toLocaleString("en-US", {
    month: "short",
    year: "numeric"
  });
}

function createDefaultAccountingProfileForm(
  client?: ClientDetails,
  contract?: ClientContract | null
): ConfigureClientAccountingProfileInput {
  return {
    accountsReceivableAccountId: "",
    defaultCurrencyCode: contract?.currencyCode ?? "PKR",
    cloudCustomerId: client?.code ?? ""
  };
}

function createDefaultDeploymentForm(client?: ClientDetails): ConfigureClientDeploymentInput {
  const installationId = createDefaultInstallationId(client?.code);

  return {
    installationId,
    displayName: client === undefined ? "Main office" : `${client.displayName} main`,
    bootstrapMode: "OnlineBootstrap",
    clientDeploymentMode: "OfflineLocal",
    siteId: "main",
    siteRole: "Standalone",
    parentSiteId: "",
    branchCode: "",
    syncTopologyId: "",
    localServerVersion: "latest",
    safarSuiteAppVersion: "latest",
    isPrimary: true
  };
}

function createDefaultSupportCommandForm(): CloudInstallationSupportCommandFormInput {
  return {
    commandType: "request_diagnostics",
    reason: "Support review",
    requestedBy: "SafarSuite Control Desk",
    expiresInHours: "72"
  };
}

function createDefaultChargeCodeForm(
  client?: ClientDetails,
  contract?: ClientContract | null
): ChargeCodeFormInput {
  return {
    code: defaultCode("SUB", client?.code),
    name: "SafarSuite subscription",
    description: client === undefined ? "" : `${client.displayName} subscription`,
    defaultUnitPriceAmount: contract?.recurringAmount.toFixed(2) ?? "0.00",
    currencyCode: contract?.currencyCode ?? "PKR",
    revenueAccountId: "",
    taxAccountId: ""
  };
}

function createDefaultChargeRuleForm(
  contract?: ClientContract | null
): ClientChargeRuleFormInput {
  const today = new Date();
  const nextYear = new Date(today);
  nextYear.setFullYear(nextYear.getFullYear() + 1);

  return {
    contractId: contract?.contractId ?? "",
    chargeCodeId: "",
    productModuleCode: "",
    descriptionOverride: "",
    unitPriceAmount: contract?.recurringAmount.toFixed(2) ?? "0.00",
    currencyCode: contract?.currencyCode ?? "PKR",
    quantity: "1",
    taxPercent: "0",
    billingCycle: contract?.billingCycle ?? "Monthly",
    billingDayOfMonth: contract?.billingDayOfMonth.toString() ?? "1",
    effectiveStartsOn: contract?.startsOn ?? toDateInputValue(today),
    effectiveEndsOn: contract?.endsOn ?? toDateInputValue(nextYear)
  };
}

function createDefaultInvoiceDraftForm(
  client?: ClientDetails,
  contract?: ClientContract | null
): InvoiceDraftFormInput {
  const today = new Date();
  const dueDate = addDays(today, 15);

  return {
    contractId: contract?.contractId ?? "",
    invoiceNumber: defaultInvoiceNumber(client?.code, today),
    issueDate: toDateInputValue(today),
    dueDate: toDateInputValue(dueDate),
    billingDate: toDateInputValue(today),
    currencyCode: contract?.currencyCode ?? "PKR"
  };
}

function createDefaultIssueInvoiceForm(date = toDateInputValue(new Date())): IssueInvoiceFormInput {
  return {
    postingDate: date,
    accountsReceivableAccountId: ""
  };
}

function createDefaultPaymentForm(
  client?: ClientDetails | null,
  invoice?: InvoiceDraft | null,
  accountingProfile?: ClientAccountingProfile | null,
  receivableOverride = ""
): RecordInvoicePaymentInput {
  const today = new Date();
  const invoiceBalance = invoice?.balanceDue ?? 0;
  const invoiceCurrency = invoice?.currencyCode ?? accountingProfile?.defaultCurrencyCode ?? "PKR";

  return {
    invoiceId: invoice?.invoiceId ?? "",
    method: "ManualCash",
    reference: defaultReceiptReference(client?.code, today),
    amount: invoiceBalance > 0 ? invoiceBalance.toFixed(2) : "0.00",
    currencyCode: invoiceCurrency,
    receivedOn: toDateInputValue(today),
    cashOrBankAccountId: "",
    accountsReceivableAccountId:
      receivableOverride.trim() === ""
        ? accountingProfile?.accountsReceivableAccountId ?? ""
        : receivableOverride.trim(),
    postingDate: toDateInputValue(today)
  };
}

function createDefaultRefundForm(
  client?: ClientDetails | null,
  accountingProfile?: ClientAccountingProfile | null,
  cashOrBankAccountId = "",
  creditAmount = 0,
  currencyCode?: string
): IssueClientRefundInput {
  const today = new Date();
  const refundCurrency = currencyCode ?? accountingProfile?.defaultCurrencyCode ?? "PKR";

  return {
    clientId: client?.clientId ?? "",
    method: "BankTransfer",
    reference: defaultRefundReference(client?.code, today),
    amount: creditAmount > 0 ? creditAmount.toFixed(2) : "0.00",
    currencyCode: refundCurrency,
    refundedOn: toDateInputValue(today),
    cashOrBankAccountId,
    accountsReceivableAccountId: accountingProfile?.accountsReceivableAccountId ?? "",
    postingDate: toDateInputValue(today),
    note: ""
  };
}

function createDefaultCreditApplicationForm(
  client?: ClientDetails | null,
  invoice?: InvoiceDraft | null,
  statement?: ClientStatement | null
): ApplyClientCreditInput {
  const today = new Date();
  const invoiceCurrency = invoice?.currencyCode ?? statement?.currencySummaries[0]?.currencyCode ?? "PKR";
  const unappliedCredit = statement === null || statement === undefined
    ? { currencyCode: invoiceCurrency, availableCredit: 0 }
    : getUnappliedStatementCredit(statement, invoiceCurrency);
  const applyAmount = Math.min(unappliedCredit.availableCredit, invoice?.balanceDue ?? 0);

  return {
    clientId: client?.clientId ?? "",
    invoiceId: invoice?.invoiceId ?? "",
    reference: defaultCreditApplicationReference(client?.code, today),
    amount: applyAmount > 0 ? applyAmount.toFixed(2) : "0.00",
    currencyCode: unappliedCredit.currencyCode,
    appliedOn: toDateInputValue(today),
    note: ""
  };
}

function mapReversedPaymentToRecordedPayment(
  payment: ReversedInvoicePayment
): RecordedInvoicePayment {
  return {
    paymentId: payment.paymentId,
    invoiceId: payment.invoiceId,
    invoiceNumber: payment.invoiceNumber,
    invoiceStatus: payment.invoiceStatus,
    paymentStatus: payment.paymentStatus,
    amount: payment.amount,
    balanceDue: payment.balanceDue,
    currencyCode: payment.currencyCode,
    journalEntryId: payment.reversalJournalEntryId,
    journalEntryStatus: payment.reversalJournalEntryStatus,
    postingDate: payment.reversalDate,
    totalDebit: payment.totalDebit,
    totalCredit: payment.totalCredit,
    journalLines: payment.journalLines
  };
}

function journalEntryIdMatches(
  entry: JournalEntrySummary,
  journalEntryId: string | null | undefined
): boolean {
  return journalEntryId !== null
    && journalEntryId !== undefined
    && journalEntryId.trim() !== ""
    && entry.journalEntryId === journalEntryId;
}

function referencesMatch(
  first: string | null | undefined,
  second: string | null | undefined
): boolean {
  const normalizedFirst = normalizeReference(first);
  const normalizedSecond = normalizeReference(second);

  return normalizedFirst !== "" && normalizedFirst === normalizedSecond;
}

function getJournalSourceDocumentFallbackLabel(entry: JournalEntrySummary): string | null {
  const reference = entry.sourceReference?.trim();

  if (reference === undefined || reference === "") {
    return null;
  }

  switch (entry.sourceType) {
    case "BillingInvoice":
      return `invoice ${reference}`;
    case "BillingInvoiceVoid":
      return `voided invoice ${reference}`;
    case "BillingCreditNote":
      return `credit note ${reference}`;
    case "PaymentReceipt":
      return `payment ${reference}`;
    case "PaymentReversal":
      return `payment reversal ${reference}`;
    case "ClientRefund":
      return `refund ${reference}`;
    default:
      return null;
  }
}

function toBillingDashboardStep(value: string | null | undefined): BillingDashboardStep | null {
  return value === "accounting"
    || value === "rules"
    || value === "draft"
    || value === "issue"
    ? value
    : null;
}

function toPaymentDashboardStep(value: string | null | undefined): PaymentDashboardStep | null {
  return value === "readiness"
    || value === "cash"
    || value === "receipt"
    || value === "settlement"
    || value === "refund"
    || value === "result"
    ? value
    : null;
}

function sourceDocumentReference(
  entry: JournalEntrySummary,
  fallback: string | null | undefined
): string {
  return entry.sourceReference?.trim() || fallback?.trim() || "source";
}

function normalizeReference(value: string | null | undefined): string {
  return value?.trim().toUpperCase() ?? "";
}

function toAccountingProfileForm(
  profile: ClientAccountingProfile
): ConfigureClientAccountingProfileInput {
  return {
    accountsReceivableAccountId: profile.accountsReceivableAccountId,
    defaultCurrencyCode: profile.defaultCurrencyCode,
    cloudCustomerId: profile.cloudCustomerId ?? ""
  };
}

function toAccountCodeRangeForm(range: AccountCodeRange): AccountCodeRangeFormInput {
  return {
    displayName: range.displayName,
    searchPrefix: range.searchPrefix,
    rangeStart: range.rangeStart,
    rangeEnd: range.rangeEnd,
    codeLength: range.codeLength.toString(),
    accountType: range.accountType,
    normalBalance: range.normalBalance,
    isPostingAccount: range.isPostingAccount,
    parentCode: range.parentCode ?? "",
    isActive: range.isActive
  };
}

function toLedgerAccountEditorForm(account: LedgerAccountSummary): LedgerAccountEditorInput {
  return {
    code: account.code,
    name: account.name,
    type: account.type,
    normalBalance: account.normalBalance,
    level: account.level ?? getDefaultLedgerAccountLevel(null, account.isPostingAccount),
    parentAccountId: account.parentAccountId ?? "",
    isPostingAccount: account.isPostingAccount,
    status: account.status
  };
}

function toDeploymentForm(deployment: ClientDeployment): ConfigureClientDeploymentInput {
  return {
    installationId: deployment.installationId,
    displayName: deployment.displayName,
    bootstrapMode: deployment.bootstrapMode,
    clientDeploymentMode: deployment.clientDeploymentMode,
    siteId: deployment.siteId,
    siteRole: deployment.siteRole,
    parentSiteId: deployment.parentSiteId ?? "",
    branchCode: deployment.branchCode ?? "",
    syncTopologyId: deployment.syncTopologyId ?? "",
    localServerVersion: deployment.localServerVersion,
    safarSuiteAppVersion: deployment.safarSuiteAppVersion,
    isPrimary: deployment.isPrimary
  };
}

function toCloudProvisioningInput(
  deployment: ConfigureClientDeploymentInput,
  setupTokenHours: string
): CreateCloudInstallationProvisioningInput {
  return {
    expiresInHours: parseSetupTokenHours(setupTokenHours),
    createdBy: "SafarSuite Control Desk",
    bootstrapMode: deployment.bootstrapMode,
    clientDeploymentMode: deployment.clientDeploymentMode,
    siteId: deployment.siteId,
    siteRole: deployment.siteRole,
    parentSiteId: deployment.parentSiteId,
    branchCode: deployment.branchCode,
    syncTopologyId: deployment.syncTopologyId,
    localServerVersion: deployment.localServerVersion,
    safarSuiteAppVersion: deployment.safarSuiteAppVersion
  };
}

function parseSetupTokenHours(value: string): number {
  const parsed = Number.parseInt(value, 10);

  if (!Number.isFinite(parsed)) {
    return 72;
  }

  return Math.min(168, Math.max(1, parsed));
}

function parseSupportCommandHours(value: string): number {
  return parseSetupTokenHours(value);
}

function createDefaultContractForm(
  clientCode = "",
  productModules: ProductModule[] = []
): ClientContractFormInput {
  const startsOn = new Date();
  const endsOn = new Date(startsOn);
  endsOn.setFullYear(endsOn.getFullYear() + 1);

  return {
    contractNumber: defaultContractNumber(clientCode, startsOn),
    startsOn: toDateInputValue(startsOn),
    endsOn: toDateInputValue(endsOn),
    recurringAmount: "0.00",
    currencyCode: "PKR",
    billingCycle: "Monthly",
    billingDayOfMonth: "1",
    allowedDevices: "1",
    allowedBranches: "1",
    moduleCodes: defaultContractModuleCodes(productModules)
  };
}

function defaultContractModuleCodes(productModules: ProductModule[]): string {
  const activeModules = productModules.filter((module) => module.isActive);

  if (activeModules.length === 0) {
    return "CONTROL_DESK";
  }

  const includedModules = activeModules.filter(
    (module) => module.commercialMode === "IncludedForAll"
  );
  const defaultModules = includedModules.length > 0
    ? includedModules
    : activeModules.length === 1
      ? activeModules
      : [];

  return defaultModules.map((module) => module.moduleCode).join(", ");
}

function defaultContractNumber(clientCode: string, value: Date): string {
  const datePart = toDateInputValue(value).replaceAll("-", "");
  const timePart = [value.getHours(), value.getMinutes(), value.getSeconds()]
    .map((item) => item.toString().padStart(2, "0"))
    .join("");
  const prefix = clientCode.trim() === "" ? "CONTRACT" : clientCode.trim().toUpperCase();

  return `${prefix}-${datePart}-${timePart}`.slice(0, 40);
}

function defaultInvoiceNumber(clientCode: string | undefined, value: Date): string {
  const datePart = toDateInputValue(value).replaceAll("-", "");
  const timePart = [value.getHours(), value.getMinutes(), value.getSeconds()]
    .map((item) => item.toString().padStart(2, "0"))
    .join("");
  const prefix = clientCode?.trim() === "" || clientCode === undefined
    ? "INV"
    : `INV-${clientCode.trim().toUpperCase()}`;

  return `${prefix}-${datePart}-${timePart}`.slice(0, 40);
}

function defaultReceiptReference(clientCode: string | undefined, value: Date): string {
  const datePart = toDateInputValue(value).replaceAll("-", "");
  const timePart = [value.getHours(), value.getMinutes(), value.getSeconds()]
    .map((item) => item.toString().padStart(2, "0"))
    .join("");
  const prefix = clientCode?.trim() === "" || clientCode === undefined
    ? "RCPT"
    : `RCPT-${clientCode.trim().toUpperCase()}`;

  return `${prefix}-${datePart}-${timePart}`.slice(0, 40);
}

function defaultManualJournalReference(value: Date): string {
  const datePart = toDateInputValue(value).replaceAll("-", "");
  const timePart = [value.getHours(), value.getMinutes(), value.getSeconds()]
    .map((item) => item.toString().padStart(2, "0"))
    .join("");

  return `JE-${datePart}-${timePart}`.slice(0, 40);
}

function defaultRefundReference(clientCode: string | undefined, value: Date): string {
  const datePart = toDateInputValue(value).replaceAll("-", "");
  const timePart = [value.getHours(), value.getMinutes(), value.getSeconds()]
    .map((item) => item.toString().padStart(2, "0"))
    .join("");
  const prefix = clientCode?.trim() === "" || clientCode === undefined
    ? "RFND"
    : `RFND-${clientCode.trim().toUpperCase()}`;

  return `${prefix}-${datePart}-${timePart}`.slice(0, 40);
}

function defaultCreditApplicationReference(clientCode: string | undefined, value: Date): string {
  const datePart = toDateInputValue(value).replaceAll("-", "");
  const timePart = [value.getHours(), value.getMinutes(), value.getSeconds()]
    .map((item) => item.toString().padStart(2, "0"))
    .join("");
  const prefix = clientCode?.trim() === "" || clientCode === undefined
    ? "SETT"
    : `SETT-${clientCode.trim().toUpperCase()}`;

  return `${prefix}-${datePart}-${timePart}`.slice(0, 40);
}

function getStatementCredit(
  statement: ClientStatement,
  preferredCurrencyCode: string
): { currencyCode: string; availableCredit: number } {
  const preferredCurrency = preferredCurrencyCode.trim().toUpperCase();
  const preferredSummary = statement.currencySummaries.find((summary) =>
    summary.currencyCode.toUpperCase() === preferredCurrency
  );
  const creditSummary =
    preferredSummary !== undefined && preferredSummary.balanceDue < 0
      ? preferredSummary
      : statement.currencySummaries.find((summary) => summary.balanceDue < 0);
  const currencyCode =
    creditSummary?.currencyCode
      ?? preferredSummary?.currencyCode
      ?? (preferredCurrency === "" ? "PKR" : preferredCurrency);
  const balanceDue = creditSummary?.balanceDue ?? preferredSummary?.balanceDue ?? 0;

  return {
    currencyCode,
    availableCredit: balanceDue < 0 ? Math.abs(balanceDue) : 0
  };
}

function getUnappliedStatementCredit(
  statement: ClientStatement,
  preferredCurrencyCode: string
): { currencyCode: string; availableCredit: number } {
  const preferredCurrency = preferredCurrencyCode.trim().toUpperCase();
  const preferredSummary = statement.currencySummaries.find((summary) =>
    summary.currencyCode.toUpperCase() === preferredCurrency
  );
  const creditSummary =
    preferredSummary !== undefined && preferredSummary.availableCredit > 0
      ? preferredSummary
      : statement.currencySummaries.find((summary) => summary.availableCredit > 0);
  const currencyCode =
    creditSummary?.currencyCode
      ?? preferredSummary?.currencyCode
      ?? (preferredCurrency === "" ? "PKR" : preferredCurrency);

  return {
    currencyCode,
    availableCredit: creditSummary?.availableCredit ?? preferredSummary?.availableCredit ?? 0
  };
}

function confirmAccountingAction(message: string): boolean {
  return typeof window === "undefined" || window.confirm(message);
}

function confirmPortalAction(message: string): boolean {
  return typeof window === "undefined" || window.confirm(message);
}

function formatAccountingAmount(amount: number, currencyCode: string): string {
  const safeAmount = Number.isFinite(amount) ? amount : 0;
  const normalizedCurrency = currencyCode.trim().toUpperCase() === ""
    ? "PKR"
    : currencyCode.trim().toUpperCase();

  return `${safeAmount.toFixed(2)} ${normalizedCurrency}`;
}

function formatDashboardDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function getCloudDeploymentProfile(
  status: ControlCloudInstallationStatus | null
): LocalServerDeploymentProfile | null {
  return status?.deploymentProfile ?? status?.latestHeartbeat?.deploymentProfile ?? null;
}

function mergeDeploymentStatus(
  current: ConfigureClientDeploymentInput,
  status: ControlCloudInstallationStatus
): ConfigureClientDeploymentInput {
  const deploymentProfile = getCloudDeploymentProfile(status);

  if (deploymentProfile === null) {
    return current;
  }

  return {
    ...current,
    installationId: status.installationId,
    bootstrapMode: deploymentProfile.bootstrapMode,
    clientDeploymentMode: deploymentProfile.clientDeploymentMode,
    siteId: deploymentProfile.siteId,
    siteRole: deploymentProfile.siteRole,
    parentSiteId: deploymentProfile.parentSiteId ?? "",
    branchCode: deploymentProfile.branchCode ?? "",
    syncTopologyId: deploymentProfile.syncTopologyId ?? "",
    localServerVersion: status.latestHeartbeat?.localServerVersion ?? current.localServerVersion
  };
}

function formatCloudDeploymentSummary(profile: LocalServerDeploymentProfile | null): string {
  if (profile === null) {
    return "Install status";
  }

  const role = profile.siteRole.trim();
  const site = profile.branchCode?.trim() || profile.siteId.trim();
  const mode = profile.clientDeploymentMode.trim();

  if (role !== "" && site !== "") {
    return `${role} ${site}`;
  }

  return mode === "" ? "Install status" : mode;
}

function formatSupportCommandType(commandType: string): string {
  return commandType
    .split("_")
    .filter((part) => part.trim() !== "")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function defaultCode(prefix: string, clientCode: string | undefined): string {
  const suffix = cleanCodeSegment(clientCode, "NEW");

  return `${prefix}-${suffix}`.slice(0, 32);
}

function cleanCodeSegment(value: string | undefined, fallback: string): string {
  const cleaned = value?.trim().toUpperCase().replace(/[^A-Z0-9-]/g, "") ?? "";

  return cleaned === "" ? fallback : cleaned;
}

function createDefaultInstallationId(clientCode: string | undefined): string {
  const suffix = clientCode?.trim().toLowerCase()
    .replace(/[^a-z0-9-]/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "");

  return `${suffix === "" || suffix === undefined ? "office" : suffix}-main`.slice(0, 160);
}

function addDays(value: Date, days: number): Date {
  const next = new Date(value);
  next.setDate(next.getDate() + days);

  return next;
}

function parseDateInput(value: string): Date {
  const [year, month, day] = value.split("-").map((part) => Number(part));

  return new Date(year, month - 1, day);
}

function toDateInputValue(value: Date): string {
  const year = value.getFullYear();
  const month = (value.getMonth() + 1).toString().padStart(2, "0");
  const day = value.getDate().toString().padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function sortContracts(contracts: ClientContract[]): ClientContract[] {
  return [...contracts].sort((left, right) =>
    right.createdAtUtc.localeCompare(left.createdAtUtc)
  );
}

function sortChargeCodes(chargeCodes: ChargeCodeLookup[]): ChargeCodeLookup[] {
  return [...chargeCodes].sort((left, right) => left.code.localeCompare(right.code));
}

function sortAccountCodeRanges(ranges: AccountCodeRange[]): AccountCodeRange[] {
  return [...ranges].sort((left, right) => {
    const rangeOrder = left.rangeStart.localeCompare(right.rangeStart);

    return rangeOrder !== 0 ? rangeOrder : left.role.localeCompare(right.role);
  });
}

function withAccountingCompanyCode(filters: LedgerAccountFilters): LedgerAccountFilters {
  return {
    ...filters,
    companyCode: accountingCompanyCode
  };
}

function normalizeAccountingCompanyCode(companyCode?: string): string {
  if (companyCode?.trim().toUpperCase() === accountingCompanyCode) {
    return accountingCompanyCode;
  }

  return accountingCompanyCode;
}

function sortClientChargeRules(chargeRules: ClientChargeRule[]): ClientChargeRule[] {
  return [...chargeRules].sort((left, right) => {
    const moduleOrder = (left.productModuleCode ?? "").localeCompare(right.productModuleCode ?? "");

    if (moduleOrder !== 0) {
      return moduleOrder;
    }

    const startOrder = left.effectiveStartsOn.localeCompare(right.effectiveStartsOn);

    if (startOrder !== 0) {
      return startOrder;
    }

    return left.clientChargeRuleId.localeCompare(right.clientChargeRuleId);
  });
}

function sortClientDeployments(deployments: ClientDeployment[]): ClientDeployment[] {
  return [...deployments].sort((left, right) => {
    if (left.isPrimary !== right.isPrimary) {
      return left.isPrimary ? -1 : 1;
    }

    const nameOrder = left.displayName.localeCompare(right.displayName);

    return nameOrder !== 0
      ? nameOrder
      : left.installationId.localeCompare(right.installationId);
  });
}

function getPrimaryDeployment(deployments: ClientDeployment[]): ClientDeployment | null {
  return deployments.find((deployment) => deployment.isPrimary) ?? deployments[0] ?? null;
}

function getSelectedDeploymentId(deployments: ClientDeployment[], installationId: string): string {
  return deployments.find((deployment) =>
    deployment.installationId.toLowerCase() === installationId.trim().toLowerCase()
  )?.clientDeploymentId ?? "";
}

function getPaidAddOnModuleCodes(
  contract: ClientContract,
  productModules: ProductModule[]
): string[] {
  return getEnabledModuleCodes(contract.modules).filter((moduleCode) =>
    findProductModule(productModules, moduleCode)?.commercialMode === "PaidAddOn"
  );
}

function getBilledModuleCodes(
  chargeRules: ClientChargeRule[],
  contract: ClientContract
): Set<string> {
  return new Set(
    chargeRules
      .filter((rule) => rule.status.toLowerCase() === "active")
      .filter((rule) => rule.contractId === undefined
        || rule.contractId === null
        || rule.contractId === contract.contractId)
      .map((rule) => normalizeOptionalModuleCode(rule.productModuleCode))
      .filter((moduleCode): moduleCode is string => moduleCode !== null)
  );
}

function getEnabledModuleCodes(modules: Array<{ moduleCode: string; isEnabled: boolean }>): string[] {
  const seen = new Set<string>();

  return modules
    .filter((module) => module.isEnabled)
    .map((module) => normalizeModuleCode(module.moduleCode))
    .filter((moduleCode) => {
      if (moduleCode === "" || seen.has(moduleCode)) {
        return false;
      }

      seen.add(moduleCode);
      return true;
    });
}

function getLatestPortalInvitation(
  latestPortalInvitation: ClientPortalInvitation | null,
  portalInvitations: ClientPortalInvitation[]
): ClientPortalInvitation | null {
  const invitations = latestPortalInvitation === null
    ? portalInvitations
    : [latestPortalInvitation, ...portalInvitations];

  return invitations
    .filter((invitation, index, source) =>
      source.findIndex((item) => item.invitationId === invitation.invitationId) === index
    )
    .sort((left, right) => right.invitedAtUtc.localeCompare(left.invitedAtUtc))[0]
    ?? null;
}

function normalizeOptionalModuleCode(value: string | null | undefined): string | null {
  if (value === null || value === undefined) {
    return null;
  }

  const normalizedModuleCode = normalizeModuleCode(value);

  return normalizedModuleCode === "" ? null : normalizedModuleCode;
}

function normalizeModuleCode(value: string): string {
  return value.trim().toUpperCase();
}

function canIssueEntitlementSnapshot(
  invoiceDraft: InvoiceDraft | null,
  recordedPayment: RecordedInvoicePayment | null
): boolean {
  return invoiceDraft !== null
    && invoiceDraft.status.toLowerCase() === "paid"
    && recordedPayment !== null;
}

function getActiveContract(contracts: ClientContract[]): ClientContract | null {
  return contracts.find((contract) => contract.status.toLowerCase() === "active")
    ?? contracts[0]
    ?? null;
}

function toClientLookup(client: ClientDetails): ClientLookup {
  return {
    clientId: client.clientId,
    code: client.code,
    legalName: client.legalName,
    displayName: client.displayName,
    status: client.status
  };
}

function formatError(caughtError: unknown): string {
  if (caughtError instanceof ApiError) {
    const details = caughtError.errors.map((error) => error.message).join(" ");
    return details === "" ? caughtError.message : details;
  }

  if (caughtError instanceof Error) {
    return caughtError.message;
  }

  return "Unexpected error.";
}
