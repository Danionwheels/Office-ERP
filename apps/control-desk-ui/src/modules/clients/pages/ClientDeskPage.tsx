import {
  Banknote,
  CheckCircle2,
  Download,
  FilePlus2,
  FileText,
  KeyRound,
  ListTree,
  Plus,
  Printer,
  ReceiptText,
  RefreshCw,
  ScrollText,
  Save,
  Send,
  Users
} from "lucide-react";
import { useEffect, useState } from "react";
import { ApiError } from "../../../shared/api/apiError";
import { AccountingWorkspace } from "../../accounting/components/AccountingWorkspace";
import { useAccountingWorkspace } from "../../accounting/hooks/useAccountingWorkspace";
import type {
  JournalEntrySourceDocument,
  JournalEntrySummary
} from "../../accounting/types/accountingTypes";
import { getJournalSourceDocumentFallbackLabel } from "../../accounting/utils/accountingSourceDocuments";
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
  listProductAccessCatalog,
  listProductModules,
  publishProductAccessCatalogCommand,
  replaceActiveClientContract,
  saveProductAccessCatalog,
  suspendClientContract
} from "../../contracts/api/contractApi";
import { ClientContractsPanel } from "../../contracts/components/ClientContractsPanel";
import type {
  ClientContract,
  ClientContractFormInput,
  ProductAccessCatalog,
  PublishedProductAccessCatalogCommand,
  PublishProductAccessCatalogCommandInput,
  ProductModule
} from "../../contracts/types/contractTypes";
import { findProductModule } from "../../contracts/utils/productModuleDisplay";
import {
  createCloudInstallationBootstrapPackage,
  createCloudInstallationSetupToken,
  getLatestCloudInstallationDiagnostics,
  getCloudInstallationStatus,
  issueCloudAppActivationToken,
  listCloudAppActivationIssues,
  listCloudInstallationBootstrapPackages,
  listCloudInstallationAuditEvents,
  listCloudOutboxMessages,
  publishCloudOutboxMessages,
  queueCloudInstallationSupportCommand,
  revokeCloudAppActivationIssue
} from "../../control-cloud/api/controlCloudApi";
import { CloudInstallationStatusPanel } from "../../control-cloud/components/CloudInstallationStatusPanel";
import { ProviderAccessPanel } from "../../control-cloud/components/ProviderAccessPanel";
import type {
  CloudAppActivationTokenFormInput,
  CloudAppActivationRevocationFormInput,
  CloudOutboxMessage,
  ControlCloudAuditEvent,
  ControlCloudConnectionState,
  CreateCloudInstallationProvisioningInput,
  CloudInstallationSupportCommandFormInput,
  ControlCloudInstallationStatus,
  IssuedSafarSuiteAppActivationToken,
  LocalServerBootstrapPackage,
  LocalServerBootstrapPackageSummary,
  LocalServerDiagnosticReport,
  LocalServerSetupToken,
  PublishCloudOutboxMessagesResult,
  QueuedCloudInstallationSupportCommand,
  SafarSuiteAppActivationIssue
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
import { ClientDashboardHome } from "../components/ClientDashboardHome";
import { ClientCreateForm } from "../components/ClientCreateForm";
import { ClientDeskShell } from "../components/ClientDeskShell";
import { ClientDetailPanel } from "../components/ClientDetailPanel";
import { ClientListPanel } from "../components/ClientListPanel";
import type {
  BillingDashboardStep,
  DashboardModule,
  JournalSourceDocumentTarget,
  ModuleCommandItem,
  PaymentDashboardStep
} from "../types/clientDashboardTypes";
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
import {
  getCloudDeploymentProfile,
  getDashboardMetrics,
  getDashboardNavigation,
  getDashboardNavigationItem,
  getDashboardWorkQueueItems
} from "../utils/clientDashboardModel";

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

type PendingJournalSourceOpen = {
  sourceDocument: JournalEntrySourceDocument;
  target: JournalSourceDocumentTarget;
};

type JournalSourceHydrationContext = {
  client?: ClientDetails | null;
  accountingProfile?: ClientAccountingProfile | null;
  clientStatement?: ClientStatement | null;
};

const defaultCloudConnectionState: ControlCloudConnectionState = {
  status: "notChecked",
  detail: "Control Cloud connection has not been checked for this deployment.",
  checkedAtUtc: null
};

export function ClientDeskPage() {
  const [clients, setClients] = useState<ClientLookup[]>([]);
  const [selectedClientId, setSelectedClientId] = useState("");
  const [selectedClient, setSelectedClient] = useState<ClientDetails | null>(null);
  const [accountingProfile, setAccountingProfile] = useState<ClientAccountingProfile | null>(null);
  const [accountingProfileMissing, setAccountingProfileMissing] = useState(false);
  const [contracts, setContracts] = useState<ClientContract[]>([]);
  const [productModules, setProductModules] = useState<ProductModule[]>([]);
  const [productAccessCatalog, setProductAccessCatalog] =
    useState<ProductAccessCatalog | null>(null);
  const [publishedAccessCatalogCommand, setPublishedAccessCatalogCommand] =
    useState<PublishedProductAccessCatalogCommand | null>(null);
  const [accessCatalogPublishForm, setAccessCatalogPublishForm] =
    useState<PublishProductAccessCatalogCommandInput>(createDefaultAccessCatalogPublishForm());
  const [chargeCodes, setChargeCodes] = useState<ChargeCodeLookup[]>([]);
  const [pendingJournalSourceOpen, setPendingJournalSourceOpen] =
    useState<PendingJournalSourceOpen | null>(null);
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
  const [cloudConnectionState, setCloudConnectionState] =
    useState<ControlCloudConnectionState>(defaultCloudConnectionState);
  const [clientDeployments, setClientDeployments] = useState<ClientDeployment[]>([]);
  const [deploymentForm, setDeploymentForm] = useState<ConfigureClientDeploymentInput>(
    createDefaultDeploymentForm()
  );
  const [setupTokenHours, setSetupTokenHours] = useState("72");
  const [cloudSetupToken, setCloudSetupToken] = useState<LocalServerSetupToken | null>(null);
  const [cloudBootstrapPackage, setCloudBootstrapPackage] =
    useState<LocalServerBootstrapPackage | null>(null);
  const [cloudBootstrapPackages, setCloudBootstrapPackages] =
    useState<LocalServerBootstrapPackageSummary[]>([]);
  const [supportCommandForm, setSupportCommandForm] =
    useState<CloudInstallationSupportCommandFormInput>(createDefaultSupportCommandForm());
  const [queuedSupportCommand, setQueuedSupportCommand] =
    useState<QueuedCloudInstallationSupportCommand | null>(null);
  const [appActivationForm, setAppActivationForm] =
    useState<CloudAppActivationTokenFormInput>(createDefaultAppActivationForm());
  const [issuedAppActivation, setIssuedAppActivation] =
    useState<IssuedSafarSuiteAppActivationToken | null>(null);
  const [appActivationIssues, setAppActivationIssues] =
    useState<SafarSuiteAppActivationIssue[]>([]);
  const [appActivationIssueSearch, setAppActivationIssueSearch] = useState("");
  const [appActivationRevocationForm, setAppActivationRevocationForm] =
    useState<CloudAppActivationRevocationFormInput>(createDefaultAppActivationRevocationForm());
  const [cloudOutboxMessages, setCloudOutboxMessages] = useState<CloudOutboxMessage[]>([]);
  const [latestCloudOutboxPublish, setLatestCloudOutboxPublish] =
    useState<PublishCloudOutboxMessagesResult | null>(null);
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
  const accounting = useAccountingWorkspace({
    runAction: runClientAction,
    setMessage,
    onLedgerSetupChanged: applyLedgerAccountCodeSuggestions,
    onShowAccounting: () => setActiveDashboardModule("accounting")
  });

  useEffect(() => {
    void refreshClients();
    void refreshChargeCodes();
    void refreshProductModules();
    void refreshProductAccessCatalog();
  }, []);

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

  async function refreshProductAccessCatalog() {
    await runClientAction(async () => {
      const nextCatalog = await listProductAccessCatalog();
      setProductAccessCatalog(nextCatalog);
    });
  }

  async function handleRefreshProductAccessCatalog() {
    await runClientAction(async () => {
      const nextCatalog = await listProductAccessCatalog();
      setProductAccessCatalog(nextCatalog);
      setMessage("Product access catalog refreshed.");
    });
  }

  async function handleSaveProductAccessCatalog(
    catalog: ProductAccessCatalog,
    requestedBy: string
  ) {
    await runClientAction(async () => {
      const savedCatalog = await saveProductAccessCatalog(catalog, requestedBy);
      setProductAccessCatalog(savedCatalog);
      setMessage("Product access catalog saved.");
    });
  }

  async function handlePublishProductAccessCatalog() {
    await runClientAction(async () => {
      const publishedCommand = await publishProductAccessCatalogCommand(accessCatalogPublishForm);
      setPublishedAccessCatalogCommand(publishedCommand);
      setProductAccessCatalog(publishedCommand.accessCatalog);
      setMessage(`Product access catalog command issued (${publishedCommand.commandId}).`);
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
      const sourceDocument = await accounting.loadJournalSourceDocument(entry);

      if (sourceDocument === null) {
        setMessage("That journal source could not be resolved.");
        return;
      }

      const resolvedTarget = getJournalSourceDocumentTargetFromResolved(sourceDocument);

      if (resolvedTarget === null) {
        if (sourceDocument.documentKind === "OpeningBalance") {
          setActiveDashboardModule("accounting");
          setMessage(sourceDocument.message ?? `Opened ${sourceDocument.label ?? "opening balance"}.`);
          return;
        }

        setMessage(sourceDocument.message ?? "That journal source could not be resolved.");
        return;
      }

      const isLoadedClientSource =
        sourceDocument.clientId === null
        || sourceDocument.clientId === undefined
        || sourceDocument.clientId === selectedClientId;

      if (!isLoadedClientSource && sourceDocument.clientId !== null && sourceDocument.clientId !== undefined) {
        setPendingJournalSourceOpen({
          sourceDocument,
          target: resolvedTarget
        });
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

  async function hydrateJournalSourceDocument(
    sourceDocument: JournalEntrySourceDocument,
    context: JournalSourceHydrationContext = {}
  ) {
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
      }, context);
      return;
    }

    if (sourceDocument.documentKind === "CreditNote") {
      const document = await getCreditNoteDocument(sourceDocument.documentId);

      applyHydratedInvoiceContext(document.invoice, {
        creditNote: document.creditNote,
        resetPaymentArtifacts: true
      }, context);
      return;
    }

    if (sourceDocument.documentKind === "Payment") {
      const document = await getInvoicePaymentDocument(sourceDocument.documentId);
      const payment = sourceDocument.sourceType === "PaymentReversal" && document.reversal !== null && document.reversal !== undefined
        ? mapReversedPaymentToRecordedPayment(document.reversal)
        : document.payment;

      applyHydratedInvoiceContext(document.invoice, { resetPaymentArtifacts: false }, context);
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
            ? context.accountingProfile?.accountsReceivableAccountId
              ?? accountingProfile?.accountsReceivableAccountId
              ?? ""
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
    },
    context: JournalSourceHydrationContext = {}
  ) {
    const hydratedClient = context.client ?? selectedClient;
    const hydratedAccountingProfile = context.accountingProfile ?? accountingProfile;
    const hydratedClientStatement = context.clientStatement ?? clientStatement;
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
      postingDate,
      accountsReceivableAccountId:
        current.accountsReceivableAccountId.trim() === ""
          ? hydratedAccountingProfile?.accountsReceivableAccountId ?? ""
          : current.accountsReceivableAccountId
    }));
    setPaymentForm(createDefaultPaymentForm(
      hydratedClient,
      invoice,
      hydratedAccountingProfile,
      hydratedAccountingProfile?.accountsReceivableAccountId
        ?? issueInvoiceForm.accountsReceivableAccountId
    ));
    setCreditApplicationForm(createDefaultCreditApplicationForm(
      hydratedClient,
      invoice,
      hydratedClientStatement
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
      setCloudConnectionState(defaultCloudConnectionState);
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
      const loadedAccountingProfile = await loadAccountingProfile(clientId);
      await loadLatestEntitlementSnapshot(clientId);
      await loadCloudOutboxMessages();
      await loadClientPortalInvitations(clientId, true);
      await hydratePendingJournalSourceOpen(
        clientId,
        client,
        loadedAccountingProfile,
        statement);
    });
  }

  async function hydratePendingJournalSourceOpen(
    clientId: string,
    client: ClientDetails,
    loadedAccountingProfile: ClientAccountingProfile | null,
    statement: ClientStatement
  ) {
    if (
      pendingJournalSourceOpen === null
      || pendingJournalSourceOpen.sourceDocument.clientId !== clientId
    ) {
      return;
    }

    await hydrateJournalSourceDocument(
      pendingJournalSourceOpen.sourceDocument,
      {
        client,
        accountingProfile: loadedAccountingProfile,
        clientStatement: statement
      });
    openJournalSourceDocumentTarget(
      pendingJournalSourceOpen.target,
      pendingJournalSourceOpen.sourceDocument);
    setPendingJournalSourceOpen(null);
  }

  async function refreshClientStatement(clientId = selectedClient?.clientId) {
    if (clientId === undefined) {
      return null;
    }

    const statement = await getClientStatement(clientId);
    setClientStatement(statement);

    return statement;
  }

  async function loadAccountingProfile(clientId: string): Promise<ClientAccountingProfile | null> {
    try {
      const profile = await getClientAccountingProfile(clientId);
      applyAccountingProfile(profile, clientId);
      return profile;
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
        return null;
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

    setCloudConnectionState(createCloudConnectionState(
      "checking",
      "Checking Control Cloud status..."
    ));

    try {
      const status = await getCloudInstallationStatus(clientId, normalizedInstallationId);
      setCloudInstallationStatus(status);
      setDeploymentForm((current) => mergeDeploymentStatus(current, status));
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud."
      ));

      return true;
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setCloudInstallationStatus(null);
        setCloudConnectionState(createCloudConnectionState(
          "connected",
          "Connected to Control Cloud; this installation was not found."
        ));

        return false;
      }

      const cloudIssue = toCloudConnectionIssue(caughtError);

      if (cloudIssue !== null) {
        setCloudInstallationStatus(null);
        setCloudConnectionState(cloudIssue);

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

    try {
      const auditEvents = await listCloudInstallationAuditEvents(
        clientId,
        normalizedInstallationId,
        50);
      setCloudAuditEvents(auditEvents);
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud history."
      ));
    } catch (caughtError) {
      const cloudIssue = toCloudConnectionIssue(caughtError);

      if (cloudIssue !== null) {
        setCloudAuditEvents([]);
        setCloudConnectionState(cloudIssue);

        return false;
      }

      throw caughtError;
    }

    return true;
  }

  async function loadCloudBootstrapPackages(
    clientId = selectedClient?.clientId,
    installationId = cloudInstallationId
  ): Promise<boolean> {
    const normalizedInstallationId = installationId.trim();

    if (clientId === undefined || normalizedInstallationId === "") {
      setCloudBootstrapPackages([]);

      return false;
    }

    try {
      const packages = await listCloudInstallationBootstrapPackages(
        clientId,
        normalizedInstallationId,
        20);
      setCloudBootstrapPackages(packages);
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud deployment package register."
      ));

      return true;
    } catch (caughtError) {
      const cloudIssue = toCloudConnectionIssue(caughtError);

      if (cloudIssue !== null) {
        setCloudBootstrapPackages([]);
        setCloudConnectionState(cloudIssue);

        return false;
      }

      throw caughtError;
    }
  }

  async function loadCloudAppActivationIssues(
    clientId = selectedClient?.clientId,
    installationId = cloudInstallationId,
    query = appActivationIssueSearch
  ): Promise<boolean> {
    if (clientId === undefined) {
      setAppActivationIssues([]);

      return false;
    }

    const activationIssues = await listCloudAppActivationIssues(
      clientId,
      {
        installationId: installationId.trim() || undefined,
        query: query.trim() || undefined,
        take: 50
      });
    setAppActivationIssues(activationIssues);

    return true;
  }

  async function loadCloudOutboxMessages(): Promise<boolean> {
    const messages = await listCloudOutboxMessages();
    setCloudOutboxMessages(messages);

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

    setCloudConnectionState(createCloudConnectionState(
      "checking",
      "Checking Control Cloud diagnostics..."
    ));

    try {
      const diagnostics = await getLatestCloudInstallationDiagnostics(
        clientId,
        normalizedInstallationId);
      setCloudDiagnosticsReport(diagnostics);
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud diagnostics."
      ));

      return true;
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setCloudDiagnosticsReport(null);
        setCloudConnectionState(createCloudConnectionState(
          "connected",
          "Connected to Control Cloud; no diagnostics report was found."
        ));

        return false;
      }

      const cloudIssue = toCloudConnectionIssue(caughtError);

      if (cloudIssue !== null) {
        setCloudDiagnosticsReport(null);
        setCloudConnectionState(cloudIssue);

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
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud portal invitations."
      ));
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setPortalInvitations([]);

        return;
      }

      if (suppressUnavailable && caughtError instanceof ApiError && caughtError.statusCode >= 500) {
        setPortalInvitations([]);
        setCloudConnectionState(
          toCloudConnectionIssue(caughtError)
          ?? createCloudConnectionState(
            "unavailable",
            "Control Cloud portal invitations are unavailable."
          ));

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

    await runCloudAction(async () => {
      const invitation = await inviteClientPortalContact(
        selectedClient.clientId,
        clientContactId
      );
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud portal invitations."
      ));
      setLatestPortalInvitation(invitation);
      upsertPortalInvitation(invitation);
      setMessage(`Portal invite created for ${invitation.email}.`);
    });
  }

  async function handleRefreshPortalInvitations() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      await loadClientPortalInvitations(selectedClient.clientId);
      setMessage("Portal invitations refreshed.");
    });
  }

  async function handleResendPortalInvitation(invitationId: string) {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      const invitation = await resendClientPortalInvitation(selectedClient.clientId, invitationId);
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud portal invitations."
      ));
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

    await runCloudAction(async () => {
      const invitation = await revokeClientPortalInvitation(selectedClient.clientId, invitationId);
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud portal invitations."
      ));
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
      await loadCloudOutboxMessages();
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
      await loadCloudOutboxMessages();
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
      await loadCloudOutboxMessages();
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
      await loadCloudOutboxMessages();
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
      await loadCloudOutboxMessages();
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
      await loadCloudOutboxMessages();
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
      await loadCloudOutboxMessages();
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
      await loadCloudOutboxMessages();
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

    await runCloudAction(async () => {
      const found = await loadCloudInstallationStatus();
      await loadCloudInstallationAuditEvents();
      await loadCloudBootstrapPackages();
      await loadCloudAppActivationIssues();
      await loadCloudOutboxMessages();
      setMessage(found
        ? "Cloud installation status refreshed."
        : "No cloud installation status found.");
    });
  }

  async function handleRefreshCloudAuditEvents() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      const found = await loadCloudInstallationAuditEvents();
      await loadCloudAppActivationIssues();
      setMessage(found
        ? "Cloud installation history refreshed."
        : "Select an installation before refreshing cloud history.");
    });
  }

  async function handleRefreshCloudBootstrapPackages() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      const found = await loadCloudBootstrapPackages();
      setMessage(found
        ? "Deployment package register refreshed."
        : "Select an installation before refreshing deployment packages.");
    });
  }

  async function handleRefreshCloudAppActivationIssues() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      await loadCloudAppActivationIssues();
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud app activation register."
      ));
      setMessage("App activation register refreshed.");
    });
  }

  async function handleRefreshCloudOutboxMessages() {
    await runClientAction(async () => {
      await loadCloudOutboxMessages();
      setMessage("Local cloud outbox refreshed.");
    });
  }

  async function handlePublishCloudOutboxMessages() {
    await runCloudAction(async () => {
      const publishResult = await publishCloudOutboxMessages(20);
      setLatestCloudOutboxPublish(publishResult);
      applyCloudOutboxPublishConnectionState(publishResult);
      await loadCloudOutboxMessages();

      if (publishResult.messages.length === 0) {
        setMessage("No local outbox messages are ready to publish.");
        return;
      }

      const summary =
        `Cloud outbox publish attempted: ${publishResult.publishedCount} sent, ${publishResult.failedCount} failed.`;

      if (publishResult.failedCount > 0) {
        setError(summary);
        return;
      }

      setMessage(summary);
    });
  }

  async function handleRefreshCloudDiagnostics() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
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

  function handleAppActivationValueChange(value: CloudAppActivationTokenFormInput) {
    setAppActivationForm(value);
    setIssuedAppActivation(null);
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
    setCloudConnectionState(defaultCloudConnectionState);
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

    await runCloudAction(async () => {
      const savedDeployment = await saveDeploymentForClient(selectedClient.clientId);
      const setupToken = await createCloudInstallationSetupToken(
        selectedClient.clientId,
        savedDeployment.installationId,
        toCloudProvisioningInput(toDeploymentForm(savedDeployment), setupTokenHours));
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud; setup token was created."
      ));
      setCloudSetupToken(setupToken);
      setCloudBootstrapPackage(null);
      await loadCloudBootstrapPackages(selectedClient.clientId, savedDeployment.installationId);
      await loadCloudInstallationAuditEvents(selectedClient.clientId, savedDeployment.installationId);
      setMessage("Cloud setup token created.");
    });
  }

  async function handleCreateCloudBootstrapPackage() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      const savedDeployment = await saveDeploymentForClient(selectedClient.clientId);
      const bootstrapPackage = await createCloudInstallationBootstrapPackage(
        selectedClient.clientId,
        savedDeployment.installationId,
        toCloudProvisioningInput(toDeploymentForm(savedDeployment), setupTokenHours));
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud; bootstrap package was created."
      ));
      setCloudBootstrapPackage(bootstrapPackage);
      setCloudSetupToken(null);
      await loadCloudBootstrapPackages(selectedClient.clientId, savedDeployment.installationId);
      await loadCloudInstallationAuditEvents(selectedClient.clientId, savedDeployment.installationId);
      setMessage("Cloud bootstrap package created.");
    });
  }

  async function handleQueueCloudSupportCommand() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
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
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud; support command was queued."
      ));
      setQueuedSupportCommand(queuedCommand);
      await loadCloudInstallationStatus(selectedClient.clientId, savedDeployment.installationId);
      await loadCloudInstallationAuditEvents(selectedClient.clientId, savedDeployment.installationId);
      setMessage(`${formatSupportCommandType(queuedCommand.commandType)} command queued.`);
    });
  }

  async function handleIssueCloudAppActivationToken() {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      const savedDeployment = await saveDeploymentForClient(selectedClient.clientId);
      const issuedActivation = await issueCloudAppActivationToken(
        selectedClient.clientId,
        savedDeployment.installationId,
        toCloudAppActivationTokenInput(appActivationForm));
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud; app activation token was issued."
      ));
      setIssuedAppActivation(issuedActivation);
      await loadCloudInstallationStatus(selectedClient.clientId, savedDeployment.installationId);
      await loadCloudInstallationAuditEvents(selectedClient.clientId, savedDeployment.installationId);
      await loadCloudAppActivationIssues(selectedClient.clientId, savedDeployment.installationId);
      setMessage("SafarSuite app activation token issued.");
    });
  }

  async function handleRevokeCloudAppActivationIssue(activationIssueId: string) {
    if (selectedClient === null) {
      return;
    }

    await runCloudAction(async () => {
      await revokeCloudAppActivationIssue(
        selectedClient.clientId,
        activationIssueId,
        {
          revokedBy: appActivationRevocationForm.revokedBy,
          reason: appActivationRevocationForm.reason
        });
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        "Connected to Control Cloud; app activation mapping was revoked."
      ));
      await loadCloudAppActivationIssues();
      await loadCloudInstallationAuditEvents();
      setIssuedAppActivation((current) =>
        current?.activationIssueId === activationIssueId ? null : current);
      setMessage("App activation mapping revoked. Import a fresh app request before issuing a replacement.");
    });
  }

  function handlePrepareReplacementAppActivationIssue(issue: SafarSuiteAppActivationIssue) {
    setAppActivationForm((current) => ({
      ...current,
      activationRequestId: "",
      replacesActivationIssueId: issue.activationIssueId,
      serverInstallationId: "",
      fingerprintHash: "",
      serverPublicKey: ""
    }));
    setIssuedAppActivation(null);
    setMessage("Replacement prepared. Import a fresh SafarSuite app request before issuing.");
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
      await loadCloudOutboxMessages();
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

  async function runCloudAction(action: () => Promise<void>) {
    await runClientAction(async () => {
      setCloudConnectionState(createCloudConnectionState(
        "checking",
        "Contacting Control Cloud..."
      ));

      try {
        await action();
      } catch (caughtError) {
        const cloudIssue = toCloudConnectionIssue(caughtError);

        if (cloudIssue !== null) {
          setCloudConnectionState(cloudIssue);
        }

        throw caughtError;
      }
    });
  }

  function applyCloudOutboxPublishConnectionState(result: PublishCloudOutboxMessagesResult) {
    const failedMessage = result.messages.find((item) => item.status.toLowerCase() === "failed");

    if (failedMessage !== undefined) {
      const detail = failedMessage.failureReason?.trim() || "Control Cloud publish failed.";
      const normalized = detail.toLowerCase();
      const status = normalized.includes("not configured") || normalized.includes("notconfigured")
        ? "notConfigured"
        : "unavailable";

      setCloudConnectionState(createCloudConnectionState(status, detail));
      return;
    }

    if (result.publishedCount > 0) {
      setCloudConnectionState(createCloudConnectionState(
        "connected",
        `${result.publishedCount} local outbox message(s) published to Control Cloud.`
      ));
    }
  }

  function applyLoadedClient(client: ClientDetails) {
    setSelectedClient(client);
    setLatestPortalInvitation(null);
    setPortalInvitations([]);
    setCloudConnectionState(defaultCloudConnectionState);
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
    setCloudConnectionState(defaultCloudConnectionState);
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
    setAppActivationForm(createDefaultAppActivationForm());
    setAppActivationRevocationForm(createDefaultAppActivationRevocationForm());
    setIssuedAppActivation(null);
    clearCloudProvisioningArtifacts();
    setClientStatement(null);
  }

  function clearCloudProvisioningArtifacts() {
    setCloudSetupToken(null);
    setCloudBootstrapPackage(null);
    setCloudBootstrapPackages([]);
    setQueuedSupportCommand(null);
    setIssuedAppActivation(null);
    setAppActivationIssues([]);
    setCloudAuditEvents([]);
    setCloudDiagnosticsReport(null);
    setCloudConnectionState(defaultCloudConnectionState);
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

  function focusClientQuickAdd() {
    setActiveDashboardModule("clients");
    window.requestAnimationFrame(() => {
      const codeField = document.querySelector<HTMLInputElement>(".client-window-create input");
      codeField?.focus();
      codeField?.select();
    });
  }

  async function handleRefreshActiveModuleCommand() {
    switch (activeDashboardModule) {
      case "clients":
        await refreshClients();
        return;
      case "contracts":
        await handleRefreshContractsCommand();
        return;
      case "accounting":
        await accounting.refreshAccountingSetup();
        return;
      case "billing":
        await handleRefreshBillingCommand();
        return;
      case "payments":
      case "statement":
        await handleRefreshClientStatement();
        return;
      case "entitlements":
        await handleRefreshLatestEntitlementSnapshot();
        return;
      case "cloud":
        await handleRefreshCloudInstallationStatus();
        return;
      case "dashboard":
      case "profile":
      default:
        if (selectedClient === null) {
          await refreshClients();
          return;
        }

        await loadClient(selectedClient.clientId);
    }
  }

  async function handleRefreshContractsCommand() {
    await runClientAction(async () => {
      const [nextProductModules, nextCatalog] = await Promise.all([
        listProductModules(),
        listProductAccessCatalog()
      ]);
      setProductModules(nextProductModules);
      setProductAccessCatalog(nextCatalog);

      if (selectedClient !== null) {
        const nextContracts = await listClientContracts(selectedClient.clientId);
        const nextActiveContract = getActiveContract(nextContracts);
        setContracts(sortContracts(nextContracts));
        await loadClientChargeRules(selectedClient.clientId, nextActiveContract?.contractId);
      }

      setMessage("Contracts refreshed.");
    });
  }

  async function handleRefreshBillingCommand() {
    await runClientAction(async () => {
      const nextChargeCodes = await listChargeCodes();
      setChargeCodes(sortChargeCodes(nextChargeCodes));

      if (selectedClient !== null) {
        await loadClientChargeRules(selectedClient.clientId, getActiveContract(contracts)?.contractId);
      }

      setMessage("Billing setup refreshed.");
    });
  }

  function getModuleCommandItems(): ModuleCommandItem[] {
    const clientCommandDisabled = isBusy || selectedClient === null;
    const cloudWriteDisabled =
      clientCommandDisabled || isCloudConnectionBlockingWrites(cloudConnectionState);
    const canGenerateInvoiceDraft =
      selectedClient !== null
      && invoiceDraftForm.contractId.trim() !== ""
      && invoiceDraftForm.invoiceNumber.trim() !== ""
      && invoiceDraftForm.issueDate !== ""
      && invoiceDraftForm.dueDate >= invoiceDraftForm.issueDate
      && invoiceDraftForm.billingDate !== ""
      && invoiceDraftForm.currencyCode.trim().length === 3;
    const canIssueInvoiceFromCommand =
      invoiceDraft !== null
      && issuedInvoice === null
      && invoiceDraft.status === "Draft"
      && issueInvoiceForm.postingDate !== ""
      && issueInvoiceForm.accountsReceivableAccountId.trim() !== "";
    const canRecordPaymentFromCommand =
      selectedClient !== null
      && issuedInvoice !== null
      && invoiceDraft !== null
      && invoiceDraft.balanceDue > 0;
    const refreshCommand: ModuleCommandItem = {
      key: "refresh",
      label: "Refresh",
      title: "Refresh this section",
      Icon: RefreshCw,
      onClick: handleRefreshActiveModuleCommand,
      disabled: isBusy
    };

    switch (activeDashboardModule) {
      case "clients":
        return [
          refreshCommand,
          {
            key: "new-client",
            label: "New",
            title: "Move to quick add",
            Icon: Plus,
            onClick: focusClientQuickAdd,
            disabled: isBusy,
            variant: "primary"
          }
        ];
      case "dashboard":
        return [
          refreshCommand,
          {
            key: "client-register",
            label: "Clients",
            title: "Open client register",
            Icon: Users,
            onClick: () => setActiveDashboardModule("clients")
          },
          {
            key: "statement",
            label: "Statement",
            title: "Open client statement",
            Icon: ScrollText,
            onClick: () => setActiveDashboardModule("statement"),
            disabled: clientCommandDisabled
          }
        ];
      case "profile":
        return [
          refreshCommand,
          {
            key: "save-profile",
            label: "Save",
            title: "Save client profile",
            Icon: Save,
            onClick: handleUpdateClient,
            disabled: clientCommandDisabled,
            variant: "primary"
          }
        ];
      case "contracts":
        return [
          refreshCommand,
          {
            key: "create-contract",
            label: "New",
            title: "Create contract from the current contract form",
            Icon: Plus,
            onClick: handleCreateContract,
            disabled: clientCommandDisabled,
            variant: "primary"
          },
          {
            key: "replace-contract",
            label: "Replace",
            title: "Replace the active contract from the current contract form",
            Icon: Send,
            onClick: handleReplaceActiveContract,
            disabled: clientCommandDisabled
          },
          {
            key: "catalog",
            label: "Catalog",
            title: "Refresh product access catalog",
            Icon: ListTree,
            onClick: handleRefreshProductAccessCatalog,
            disabled: isBusy
          }
        ];
      case "accounting":
        return [
          refreshCommand,
          {
            key: "reports",
            label: "Reports",
            title: "Refresh accounting reports",
            Icon: FileText,
            onClick: () => accounting.refreshAccountingReports(),
            disabled: isBusy
          },
          {
            key: "reconcile",
            label: "Reconcile",
            title: "Refresh chart of accounts reconciliation",
            Icon: CheckCircle2,
            onClick: () => accounting.refreshLedgerAccountReconciliation(),
            disabled: isBusy,
            variant: "primary"
          }
        ];
      case "billing":
        return [
          refreshCommand,
          {
            key: "save-accounting-profile",
            label: "Save",
            title: "Save client accounting profile",
            Icon: Save,
            onClick: handleSaveAccountingProfile,
            disabled: clientCommandDisabled
          },
          {
            key: "generate-draft",
            label: "Draft",
            title: "Generate invoice draft",
            Icon: FilePlus2,
            onClick: handleGenerateInvoiceDraft,
            disabled: isBusy || !canGenerateInvoiceDraft,
            variant: "primary"
          },
          {
            key: "issue-invoice",
            label: "Issue",
            title: "Issue the current invoice draft",
            Icon: Send,
            onClick: handleIssueInvoice,
            disabled: isBusy || !canIssueInvoiceFromCommand
          }
        ];
      case "payments":
        return [
          refreshCommand,
          {
            key: "cash-account",
            label: "Cash",
            title: "Create cash or bank account from the current form",
            Icon: Banknote,
            onClick: handleCreateCashAccount,
            disabled: clientCommandDisabled
          },
          {
            key: "record-payment",
            label: "Receipt",
            title: "Record the current invoice payment",
            Icon: ReceiptText,
            onClick: handleRecordInvoicePayment,
            disabled: isBusy || !canRecordPaymentFromCommand,
            variant: "primary"
          }
        ];
      case "entitlements":
        return [
          refreshCommand,
          {
            key: "issue-entitlement",
            label: "Issue",
            title: "Issue entitlement snapshot from paid invoice",
            Icon: KeyRound,
            onClick: handleIssueEntitlementSnapshot,
            disabled: isBusy || !canIssueCurrentEntitlementSnapshot,
            variant: "primary"
          }
        ];
      case "cloud":
        return [
          refreshCommand,
          {
            key: "save-deployment",
            label: "Save",
            title: "Save client deployment",
            Icon: Save,
            onClick: handleSaveClientDeployment,
            disabled: clientCommandDisabled,
            variant: "primary"
          },
          {
            key: "setup-token",
            label: "Token",
            title: "Create setup token",
            Icon: KeyRound,
            onClick: handleCreateCloudSetupToken,
            disabled: cloudWriteDisabled
          },
          {
            key: "bootstrap",
            label: "Bootstrap",
            title: "Create bootstrap package",
            Icon: Download,
            onClick: handleCreateCloudBootstrapPackage,
            disabled: cloudWriteDisabled
          }
        ];
      case "statement":
        return [
          refreshCommand,
          {
            key: "billing",
            label: "Billing",
            title: "Open billing workspace",
            Icon: FileText,
            onClick: () => setActiveDashboardModule("billing"),
            disabled: clientCommandDisabled
          },
          {
            key: "payments",
            label: "Payments",
            title: "Open payment workspace",
            Icon: Banknote,
            onClick: () => setActiveDashboardModule("payments"),
            disabled: clientCommandDisabled
          }
        ];
      default:
        return [refreshCommand];
    }
  }

  function getModuleOutputCommandItems(): ModuleCommandItem[] {
    return [
      {
        key: "print",
        label: "Print",
        title: "Print output is not available for this section yet",
        Icon: Printer,
        disabled: true
      },
      {
        key: "export",
        label: "Export",
        title: "Export output is not available for this section yet",
        Icon: Download,
        disabled: true
      }
    ];
  }

  function getModuleCommandStatus() {
    if (isBusy) {
      return "Working...";
    }

    if (activeDashboardModule === "clients") {
      return `${clients.length} clients`;
    }

    if (selectedClient === null) {
      return "Select a client";
    }

    switch (activeDashboardModule) {
      case "billing":
        return invoiceDraft === null
          ? "No invoice draft"
          : `${invoiceDraft.invoiceNumber} ${invoiceDraft.status}`;
      case "payments":
        return recordedPayment === null
          ? "No receipt posted"
          : `${recordedPayment.invoiceNumber} ${recordedPayment.paymentStatus}`;
      case "entitlements":
        return issuedEntitlementSnapshot !== null || latestEntitlementSnapshot !== null
          ? "Snapshot available"
          : "Snapshot not loaded";
      case "cloud":
        if (cloudConnectionState.status === "notConfigured") {
          return "Cloud not configured";
        }

        if (cloudConnectionState.status === "unavailable") {
          return "Cloud unavailable";
        }

        return cloudInstallationStatus?.installationStatus ?? "Cloud status not loaded";
      case "statement":
        return clientStatement === null ? "Statement not loaded" : "Statement loaded";
      default:
        return `${selectedClient.code} selected`;
    }
  }

  const activeContract = getActiveContract(contracts);
  const canIssueCurrentEntitlementSnapshot = canIssueEntitlementSnapshot(
    invoiceDraft,
    recordedPayment
  );
  const dashboardMetrics = getDashboardMetrics({
    activeContract,
    accountCodeRangeCount: accounting.accountCodeRanges.length,
    invoiceDraft,
    recordedPayment,
    issuedEntitlementSnapshot,
    latestEntitlementSnapshot,
    cloudInstallationStatus,
    clientStatement
  });
  const dashboardWorkQueueItems = getDashboardWorkQueueItems({
    clientCount: clients.length,
    selectedClient,
    activeContract,
    accountingProfile,
    productModules,
    chargeRules: clientChargeRules,
    invoiceDraft,
    issuedInvoice,
    recordedPayment,
    issuedEntitlementSnapshot,
    latestEntitlementSnapshot,
    latestEntitlementSnapshotMissing,
    cloudInstallationStatus,
    latestPortalInvitation,
    portalInvitations,
    clientStatement
  });
  const dashboardNavigation = getDashboardNavigation(dashboardMetrics, clients.length, selectedClient);
  const activeNavigationItem = getDashboardNavigationItem(
    dashboardNavigation,
    activeDashboardModule
  );
  const moduleCommandItems = getModuleCommandItems();
  const moduleOutputCommandItems = getModuleOutputCommandItems();
  const moduleCommandStatus = getModuleCommandStatus();

  return (
    <ClientDeskShell
      activeModule={activeDashboardModule}
      activeNavigationItem={activeNavigationItem}
      commandItems={moduleCommandItems}
      commandStatus={moduleCommandStatus}
      error={error}
      message={message}
      navigationItems={dashboardNavigation}
      outputCommandItems={moduleOutputCommandItems}
      selectedClient={selectedClient}
      onModuleChange={setActiveDashboardModule}
    >
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
              <ClientDashboardHome
                metrics={dashboardMetrics}
                selectedClient={selectedClient}
                workQueueItems={dashboardWorkQueueItems}
                onNavigate={setActiveDashboardModule}
              />
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
                accessCatalog={productAccessCatalog}
                publishedAccessCatalogCommand={publishedAccessCatalogCommand}
                accessCatalogPublishValue={accessCatalogPublishForm}
                chargeRules={clientChargeRules}
                latestSnapshot={latestEntitlementSnapshot}
                latestSnapshotMissing={latestEntitlementSnapshotMissing}
                canIssueEntitlementSnapshot={canIssueCurrentEntitlementSnapshot}
                value={contractForm}
                isBusy={isBusy || selectedClient === null}
                onChange={setContractForm}
                onAccessCatalogPublishValueChange={setAccessCatalogPublishForm}
                onRefreshAccessCatalog={handleRefreshProductAccessCatalog}
                onSaveAccessCatalog={handleSaveProductAccessCatalog}
                onPublishAccessCatalog={handlePublishProductAccessCatalog}
                onCreate={handleCreateContract}
                onReplaceActive={handleReplaceActiveContract}
                onSuspend={handleSuspendContract}
                onPrepareModuleBilling={handlePrepareModuleBilling}
                onResolveEntitlementReadiness={handleResolveEntitlementReadiness}
              />
            )}

            {activeDashboardModule === "accounting" && (
              <AccountingWorkspace
                workspace={accounting}
                isBusy={isBusy}
                onOpenSourceDocument={handleOpenJournalSourceDocument}
                getSourceDocumentLabel={getJournalSourceDocumentLabel}
                getSourceDocumentClientLabel={getJournalSourceDocumentClientLabel}
              />
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
                onViewJournalEntry={accounting.viewJournalEntryById}
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
                onViewJournalEntry={accounting.viewJournalEntryById}
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
              <div className="cloud-workspace">
                <CloudInstallationStatusPanel
                  client={selectedClient}
                  installationId={cloudInstallationId}
                  deployments={clientDeployments}
                  selectedDeploymentId={getSelectedDeploymentId(clientDeployments, cloudInstallationId)}
                  deploymentValue={deploymentForm}
                  setupTokenHours={setupTokenHours}
                  connectionState={cloudConnectionState}
                  status={cloudInstallationStatus}
                  setupToken={cloudSetupToken}
                  bootstrapPackage={cloudBootstrapPackage}
                  bootstrapPackages={cloudBootstrapPackages}
                  supportCommandValue={supportCommandForm}
                  queuedSupportCommand={queuedSupportCommand}
                  appActivationValue={appActivationForm}
                  issuedAppActivation={issuedAppActivation}
                  appActivationIssues={appActivationIssues}
                  appActivationIssueSearch={appActivationIssueSearch}
                  appActivationRevocationValue={appActivationRevocationForm}
                  outboxMessages={cloudOutboxMessages}
                  latestOutboxPublish={latestCloudOutboxPublish}
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
                  onRefreshBootstrapPackages={handleRefreshCloudBootstrapPackages}
                  onSupportCommandValueChange={handleSupportCommandValueChange}
                  onQueueSupportCommand={handleQueueCloudSupportCommand}
                  onAppActivationValueChange={handleAppActivationValueChange}
                  onIssueAppActivationToken={handleIssueCloudAppActivationToken}
                  onAppActivationIssueSearchChange={setAppActivationIssueSearch}
                  onRefreshAppActivationIssues={handleRefreshCloudAppActivationIssues}
                  onAppActivationRevocationValueChange={setAppActivationRevocationForm}
                  onRevokeAppActivationIssue={handleRevokeCloudAppActivationIssue}
                  onPrepareReplacementAppActivationIssue={handlePrepareReplacementAppActivationIssue}
                  onRefreshOutboxMessages={handleRefreshCloudOutboxMessages}
                  onPublishOutboxMessages={handlePublishCloudOutboxMessages}
                  onRefreshAuditEvents={handleRefreshCloudAuditEvents}
                  onRefreshDiagnostics={handleRefreshCloudDiagnostics}
                  onRefresh={handleRefreshCloudInstallationStatus}
                />
                <ProviderAccessPanel />
              </div>
            )}

            {activeDashboardModule === "statement" && (
              <ClientStatementPanel
                client={selectedClient}
                statement={clientStatement}
                isBusy={isBusy || selectedClient === null}
                onRefresh={handleRefreshClientStatement}
              />
            )}
    </ClientDeskShell>
  );
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

function createDefaultAppActivationForm(): CloudAppActivationTokenFormInput {
  return {
    activationRequestId: "",
    replacesActivationIssueId: "",
    serverInstallationId: "",
    fingerprintHash: "",
    serverPublicKey: "",
    requestedBy: "SafarSuite Control Desk"
  };
}

function createDefaultAppActivationRevocationForm(): CloudAppActivationRevocationFormInput {
  return {
    revokedBy: "SafarSuite Control Desk",
    reason: "Rotate app activation mapping"
  };
}

function createDefaultAccessCatalogPublishForm(): PublishProductAccessCatalogCommandInput {
  return {
    activationRequestId: "",
    expiresInHours: "2",
    requestedBy: "Control Desk"
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

function toCloudAppActivationTokenInput(
  value: CloudAppActivationTokenFormInput
) {
  const activationRequestId = value.activationRequestId.trim();
  const replacesActivationIssueId = value.replacesActivationIssueId.trim();

  return {
    activationRequestId: activationRequestId === "" ? null : activationRequestId,
    replacesActivationIssueId: replacesActivationIssueId === "" ? null : replacesActivationIssueId,
    serverInstallationId: value.serverInstallationId.trim(),
    fingerprintHash: value.fingerprintHash.trim(),
    serverPublicKey: value.serverPublicKey.trim(),
    requestedBy: value.requestedBy.trim()
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

function canIssueEntitlementSnapshot(
  invoiceDraft: InvoiceDraft | null,
  recordedPayment: RecordedInvoicePayment | null
): boolean {
  return invoiceDraft !== null
    && invoiceDraft.status.toLowerCase() === "paid"
    && recordedPayment !== null;
}

function createCloudConnectionState(
  status: ControlCloudConnectionState["status"],
  detail: string
): ControlCloudConnectionState {
  return {
    status,
    detail,
    checkedAtUtc: status === "notChecked" ? null : new Date().toISOString()
  };
}

function toCloudConnectionIssue(caughtError: unknown): ControlCloudConnectionState | null {
  if (!(caughtError instanceof ApiError)) {
    return null;
  }

  const detail = formatError(caughtError);
  const normalized = `${caughtError.message} ${detail}`.toLowerCase();
  const isCloudDependencyError =
    caughtError.statusCode === 503
    || (caughtError.statusCode >= 500 && normalized.includes("control cloud"));

  if (!isCloudDependencyError) {
    return null;
  }

  const status = normalized.includes("not configured") || normalized.includes("notconfigured")
    ? "notConfigured"
    : "unavailable";
  const fallbackDetail = status === "notConfigured"
    ? "Control Cloud endpoint is not configured."
    : "Control Cloud is unavailable.";

  return createCloudConnectionState(status, detail.trim() || fallbackDetail);
}

function isCloudConnectionBlockingWrites(state: ControlCloudConnectionState): boolean {
  return state.status === "unavailable" || state.status === "notConfigured";
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
