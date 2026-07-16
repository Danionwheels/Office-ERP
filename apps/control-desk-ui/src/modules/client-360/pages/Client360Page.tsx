import {
  AlertCircle,
  ArrowRight,
  Banknote,
  Building2,
  CheckCircle2,
  ChevronDown,
  Cloud,
  FileText,
  GitCompareArrows,
  KeyRound,
  Layers3,
  NotebookText,
  PauseCircle,
  Plus,
  RefreshCw,
  Receipt,
  Save,
  Search,
  Send,
  Server,
  ShieldCheck,
  Trash2,
  WalletCards
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { ApiError } from "../../../shared/api/apiError";
import { listLedgerAccounts } from "../../accounting/api/accountingApi";
import { accountingCompanyCode } from "../../accounting/constants/accountingConstants";
import type { LedgerAccountSummary } from "../../accounting/types/accountingTypes";
import { addDays, toDateInputValue } from "../../accounting/utils/accountingDates";
import { generateInvoiceDraft, issueInvoice } from "../../billing/api/billingApi";
import type {
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueInvoiceFormInput
} from "../../billing/types/billingTypes";
import {
  activateClient,
  addClientContact,
  addClientSupportNote,
  configureClientAccountingProfile,
  configureClientDeployment,
  createClient,
  getClient,
  getClientAccountingProfile,
  inviteClientPortalContact,
  listClientDeployments,
  listClientPortalInvitations,
  listClientPage,
  resendClientPortalInvitation,
  revokeClientPortalInvitation,
  suspendClient,
  updateClient
} from "../../clients/api/clientApi";
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
} from "../../clients/types/clientTypes";
import {
  createClientContract,
  listClientContracts,
  listProductModules,
  replaceActiveClientContract
} from "../../contracts/api/contractApi";
import type {
  ClientContract,
  ClientContractFormInput,
  ProductModule
} from "../../contracts/types/contractTypes";
import { moduleCodesFromText } from "../../contracts/utils/contractWorkspaceModel";
import {
  getCloudInstallationStatus,
  listCloudOutboxMessagePage,
  publishCloudOutboxMessages
} from "../../control-cloud/api/controlCloudApi";
import { EntitlementReconciliationTable } from "../../control-cloud/components/shared/EntitlementReconciliationTable";
import type {
  CloudOutboxMessage,
  CloudOutboxMessageRegisterSummary,
  ControlCloudInstallationStatus,
  PublishCloudOutboxMessagesResult
} from "../../control-cloud/types/controlCloudTypes";
import {
  getLatestEntitlementSnapshot,
  issueEntitlementFromPaidInvoiceDefaults
} from "../../entitlements/api/entitlementApi";
import type { EntitlementSnapshot } from "../../entitlements/types/entitlementTypes";
import { recordInvoicePayment } from "../../payments/api/paymentApi";
import type { RecordInvoicePaymentInput } from "../../payments/types/paymentTypes";
import { getClientStatement, loadMoreClientStatement } from "../../statements/api/statementApi";
import type {
  ClientStatement,
  ClientStatementInvoice,
  ClientStatementPayment,
  ClientStatementRegister
} from "../../statements/types/statementTypes";

export type Client360Tab =
  | "overview"
  | "setup"
  | "billing"
  | "payments"
  | "vouchers"
  | "access"
  | "cloud"
  | "notes";

export type Client360LaunchTarget = {
  clientId: string;
  tab: Client360Tab;
  sequence: number;
};

type Client360PageProps = {
  launchTarget: Client360LaunchTarget | null;
};

type ClientWorkspace = {
  client: ClientDetails;
  accountingProfile: ClientAccountingProfile | null;
  contracts: ClientContract[];
  statement: ClientStatement | null;
  entitlement: EntitlementSnapshot | null;
  deployments: ClientDeployment[];
  portalInvitations: ClientPortalInvitation[];
  cloudStatus: ControlCloudInstallationStatus | null;
};

type VoucherRow = {
  id: string;
  date: string;
  type: string;
  reference: string;
  status: string;
  amount: number;
  currencyCode: string;
  source: string;
};

type NextRequiredAction = {
  label: string;
  detail: string;
  tab: Client360Tab;
  tone: "ready" | "warning" | "done";
};

type ClientProgressEvent = {
  key: string;
  label: string;
  detail: string;
  occurredAtUtc: string | null;
};

type ClientProgressTrail = {
  lastDone: ClientProgressEvent | null;
  recentEvents: ClientProgressEvent[];
};

const tabs: Array<{ key: Client360Tab; label: string }> = [
  { key: "overview", label: "Overview" },
  { key: "setup", label: "Setup" },
  { key: "billing", label: "Billing" },
  { key: "payments", label: "Payments" },
  { key: "vouchers", label: "Vouchers" },
  { key: "access", label: "Access" },
  { key: "cloud", label: "Cloud" },
  { key: "notes", label: "Notes" }
];

const defaultAccessApprovalReason =
  "Paid invoice and active contract verified in Control Desk.";

export function Client360Page({ launchTarget }: Client360PageProps) {
  const [clients, setClients] = useState<ClientLookup[]>([]);
  const [clientSearch, setClientSearch] = useState("");
  const [clientFilteredCount, setClientFilteredCount] = useState(0);
  const [clientNextCursor, setClientNextCursor] = useState<string | null>(null);
  const [selectedClientId, setSelectedClientId] = useState("");
  const [workspace, setWorkspace] = useState<ClientWorkspace | null>(null);
  const [outboxMessages, setOutboxMessages] = useState<CloudOutboxMessage[]>([]);
  const [outboxSummary, setOutboxSummary] =
    useState<CloudOutboxMessageRegisterSummary | null>(null);
  const [outboxNextCursor, setOutboxNextCursor] = useState<string | null>(null);
  const [isLoadingOlderOutbox, setIsLoadingOlderOutbox] = useState(false);
  const [latestPublishResult, setLatestPublishResult] =
    useState<PublishCloudOutboxMessagesResult | null>(null);
  const [cashAccounts, setCashAccounts] = useState<LedgerAccountSummary[]>([]);
  const [receivableAccounts, setReceivableAccounts] = useState<LedgerAccountSummary[]>([]);
  const [productModules, setProductModules] = useState<ProductModule[]>([]);
  const [createForm, setCreateForm] = useState<CreateClientInput>(
    () => createDefaultCreateClientForm()
  );
  const [editForm, setEditForm] = useState<UpdateClientInput>(
    () => createDefaultUpdateClientForm()
  );
  const [contactForm, setContactForm] = useState<AddClientContactInput>(
    () => createDefaultContactForm()
  );
  const [supportNoteForm, setSupportNoteForm] = useState<AddClientSupportNoteInput>(
    () => createDefaultSupportNoteForm()
  );
  const [accountingProfileForm, setAccountingProfileForm] =
    useState<ConfigureClientAccountingProfileInput>(() => createDefaultAccountingProfileForm());
  const [contractForm, setContractForm] = useState<ClientContractFormInput>(
    () => createDefaultContractForm()
  );
  const [deploymentForm, setDeploymentForm] = useState<ConfigureClientDeploymentInput>(
    () => createDefaultDeploymentForm()
  );
  const [invoiceDraft, setInvoiceDraft] = useState<InvoiceDraft | null>(null);
  const [invoiceDraftForm, setInvoiceDraftForm] = useState<InvoiceDraftFormInput>(
    () => createDefaultInvoiceDraftForm()
  );
  const [issueInvoiceForm, setIssueInvoiceForm] = useState<IssueInvoiceFormInput>(
    () => createDefaultIssueInvoiceForm()
  );
  const [paymentForm, setPaymentForm] = useState<RecordInvoicePaymentInput>(
    () => createDefaultPaymentForm()
  );
  const [accessInvoiceId, setAccessInvoiceId] = useState("");
  const [accessApprovalReason, setAccessApprovalReason] = useState(
    defaultAccessApprovalReason
  );
  const [isAccessScheduled, setIsAccessScheduled] = useState(false);
  const [accessEffectiveFrom, setAccessEffectiveFrom] = useState(
    createDefaultAccessEffectiveFrom
  );
  const [activeTab, setActiveTab] = useState<Client360Tab>("overview");
  const [isLoadingClients, setIsLoadingClients] = useState(true);
  const [isLoadingOlderClients, setIsLoadingOlderClients] = useState(false);
  const [isLoadingWorkspace, setIsLoadingWorkspace] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [isActionBusy, setIsActionBusy] = useState(false);
  const [isCreatePanelOpen, setIsCreatePanelOpen] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  useEffect(() => {
    void loadClients(launchTarget?.clientId, false, "");
    void loadPostingAccounts();
    void loadProductModuleOptions();
  }, []);

  useEffect(() => {
    if (launchTarget === null) {
      return;
    }

    setActiveTab(launchTarget.tab);
    setSelectedClientId(launchTarget.clientId);

    if (!clients.some((client) => client.clientId === launchTarget.clientId)) {
      void loadClients(launchTarget.clientId, false, clientSearch);
    }
  }, [launchTarget?.sequence]);

  useEffect(() => {
    if (selectedClientId === "") {
      setWorkspace(null);
      return;
    }

    if (workspace?.client.clientId === selectedClientId) {
      return;
    }

    void loadClientWorkspace(selectedClientId);
  }, [selectedClientId]);

  const selectedOutboxMessages = outboxMessages;

  const activeContract = useMemo(
    () => findActiveContract(workspace?.contracts ?? []),
    [workspace?.contracts]
  );

  const primaryDeployment = useMemo(
    () => findPrimaryDeployment(workspace?.deployments ?? []),
    [workspace?.deployments]
  );

  const vouchers = useMemo(
    () => createVoucherRows(workspace?.statement ?? null),
    [workspace?.statement]
  );

  const setupGaps = useMemo(
    () => createSetupGaps(workspace, activeContract, primaryDeployment),
    [activeContract, primaryDeployment, workspace]
  );

  const receiptInvoice = useMemo(
    () => findOpenInvoice(workspace?.statement ?? null),
    [workspace?.statement]
  );

  const paidInvoices = useMemo(
    () => findPaidInvoices(workspace?.statement ?? null),
    [workspace?.statement]
  );

  const nextRequiredAction = useMemo(
    () =>
      createNextRequiredAction({
        activeContract,
        invoiceDraft,
        messages: selectedOutboxMessages,
        paidInvoices,
        primaryDeployment,
        receiptInvoice,
        workspace
      }),
    [
      activeContract,
      invoiceDraft,
      paidInvoices,
      primaryDeployment,
      receiptInvoice,
      selectedOutboxMessages,
      workspace
    ]
  );

  const progressTrail = useMemo(
    () =>
      createClientProgressTrail({
        messages: selectedOutboxMessages,
        vouchers,
        workspace
      }),
    [selectedOutboxMessages, vouchers, workspace]
  );

  useEffect(() => {
    if (workspace === null) {
      setInvoiceDraft(null);
      setInvoiceDraftForm(createDefaultInvoiceDraftForm());
      setIssueInvoiceForm(createDefaultIssueInvoiceForm());
      setPaymentForm(createDefaultPaymentForm());
      setSupportNoteForm(createDefaultSupportNoteForm());
      setEditForm(createDefaultUpdateClientForm());
      setAccessInvoiceId("");
      setAccessApprovalReason(defaultAccessApprovalReason);
      return;
    }

    setInvoiceDraft(null);
    setEditForm(toClientEditForm(workspace.client));
    setSupportNoteForm(createDefaultSupportNoteForm());
    setContactForm(createDefaultContactForm(workspace.client));
    setAccountingProfileForm(
      workspace.accountingProfile === null
        ? createDefaultAccountingProfileForm(
            workspace.client,
            activeContract,
            receivableAccounts[0]?.ledgerAccountId ?? ""
          )
        : toAccountingProfileForm(workspace.accountingProfile)
    );
    setContractForm(createDefaultContractForm(workspace.client.code, productModules));
    setDeploymentForm(
      primaryDeployment === null
        ? createDefaultDeploymentForm(workspace.client)
        : toDeploymentForm(primaryDeployment)
    );
    setInvoiceDraftForm(createDefaultInvoiceDraftForm(workspace.client, activeContract));
    setIssueInvoiceForm(createDefaultIssueInvoiceForm(undefined, workspace.accountingProfile));
    setPaymentForm(
      createDefaultPaymentForm({
        client: workspace.client,
        invoice: receiptInvoice,
        draft: null,
        accountingProfile: workspace.accountingProfile,
        cashAccountId: cashAccounts[0]?.ledgerAccountId ?? ""
      })
    );
    setAccessInvoiceId((current) =>
      paidInvoices.some((invoice) => invoice.invoiceId === current)
        ? current
        : paidInvoices[0]?.invoiceId ?? ""
    );
    setAccessApprovalReason(defaultAccessApprovalReason);
  }, [
    activeContract?.contractId,
    cashAccounts,
    paidInvoices,
    primaryDeployment?.clientDeploymentId,
    productModules,
    receivableAccounts,
    receiptInvoice?.invoiceId,
    workspace?.accountingProfile?.accountsReceivableAccountId,
    workspace?.client.clientId
  ]);

  async function loadClients(
    preferredClientId?: string,
    append = false,
    search = clientSearch
  ) {
    if (append) {
      setIsLoadingOlderClients(true);
    } else {
      setIsLoadingClients(true);
    }
    setError("");

    try {
      const page = await listClientPage({
        search,
        sort: "code",
        direction: "asc",
        take: 50,
        cursor: append ? clientNextCursor ?? undefined : undefined
      });
      let nextClients = append
        ? mergeClientLookups(clients, page.clients)
        : page.clients;
      const preferredClient = preferredClientId ?? selectedClientId;

      if (preferredClient !== ""
        && !nextClients.some((client) => client.clientId === preferredClient)) {
        const details = await optionalNonCriticalRequest(() => getClient(preferredClient));

        if (details !== null) {
          nextClients = mergeClientLookups(nextClients, [toClientLookup(details)]);
        }
      }

      setClients(nextClients);
      setClientFilteredCount(page.filteredCount);
      setClientNextCursor(page.nextCursor ?? null);

      const nextSelectedClientId =
        nextClients.find((client) => client.clientId === preferredClient)?.clientId
        ?? nextClients[0]?.clientId
        ?? "";

      setSelectedClientId(nextSelectedClientId);

      if (nextSelectedClientId === "") {
        setWorkspace(null);
      }
    } catch (caughtError) {
      setError(formatError(caughtError, "Clients could not be loaded."));
    } finally {
      setIsLoadingClients(false);
      setIsLoadingOlderClients(false);
    }
  }

  async function handleClientSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await loadClients(selectedClientId, false, clientSearch);
  }

  async function loadClientWorkspace(clientId: string) {
    setIsLoadingWorkspace(true);
    setError("");
    setNotice("");

    try {
      const [
        client,
        accountingProfile,
        contracts,
        statement,
        entitlement,
        deployments,
        portalInvitations
      ] = await Promise.all([
        getClient(clientId),
        optionalRequest(() => getClientAccountingProfile(clientId)),
        optionalRequest(() => listClientContracts(clientId), []),
        optionalRequest(() => getClientStatement(clientId)),
        optionalRequest(() => getLatestEntitlementSnapshot(clientId)),
        optionalRequest(() => listClientDeployments(clientId), []),
        optionalNonCriticalRequest(() => listClientPortalInvitations(clientId))
          .then((invitations) => invitations ?? [])
      ]);

      const primary = findPrimaryDeployment(deployments);
      const cloudStatus =
        primary === null
          ? null
          : await optionalNonCriticalRequest(() =>
              getCloudInstallationStatus(clientId, primary.installationId)
            );

      setWorkspace({
        client,
        accountingProfile,
        contracts,
        statement,
        entitlement,
        deployments,
        portalInvitations,
        cloudStatus
      });

      await refreshOutboxMessages(clientId);
    } catch (caughtError) {
      setError(formatError(caughtError, "Client workspace could not be loaded."));
      setWorkspace(null);
    } finally {
      setIsLoadingWorkspace(false);
    }
  }

  async function handleLoadMoreStatement(register: ClientStatementRegister) {
    if (workspace?.statement === null || workspace === null) {
      return;
    }

    const clientId = workspace.client.clientId;
    setIsActionBusy(true);
    setError("");

    try {
      const statement = await loadMoreClientStatement(clientId, workspace.statement, register);
      setWorkspace((current) => current?.client.clientId === clientId
        ? { ...current, statement }
        : current);
    } catch (caughtError) {
      setError(formatError(caughtError, "Older financial records could not be loaded."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function refreshOutboxMessages(clientId = selectedClientId) {
    if (clientId === "") {
      setOutboxMessages([]);
      setOutboxSummary(null);
      setOutboxNextCursor(null);

      return;
    }

    try {
      const page = await listCloudOutboxMessagePage({ clientId, take: 100 });
      setOutboxMessages(page.messages);
      setOutboxSummary(page.summary);
      setOutboxNextCursor(page.nextCursor);
    } catch {
      setOutboxMessages([]);
      setOutboxSummary(null);
      setOutboxNextCursor(null);
    }
  }

  async function loadOlderOutboxMessages() {
    if (selectedClientId === "" || outboxNextCursor === null || isLoadingOlderOutbox) {
      return;
    }

    setIsLoadingOlderOutbox(true);

    try {
      const page = await listCloudOutboxMessagePage({
        clientId: selectedClientId,
        take: 100,
        cursor: outboxNextCursor
      });
      setOutboxMessages((current) => [...current, ...page.messages]);
      setOutboxSummary(page.summary);
      setOutboxNextCursor(page.nextCursor);
    } catch (caughtError) {
      setError(formatError(caughtError, "Older cloud updates could not be loaded."));
    } finally {
      setIsLoadingOlderOutbox(false);
    }
  }

  async function loadPostingAccounts() {
    const cashRoleAccounts = await optionalNonCriticalRequest(() =>
      listLedgerAccounts({
        companyCode: accountingCompanyCode,
        search: "",
        type: "Asset",
        status: "Active",
        posting: "posting",
        role: "CashBank",
        viewMode: "flat",
        level: ""
      })
    );

    const assetAccounts = await optionalNonCriticalRequest(() =>
      listLedgerAccounts({
        companyCode: accountingCompanyCode,
        search: "",
        type: "Asset",
        status: "Active",
        posting: "posting",
        role: "",
        viewMode: "flat",
        level: ""
      })
    );

    setCashAccounts(
      cashRoleAccounts !== null && cashRoleAccounts.length > 0
        ? cashRoleAccounts
        : assetAccounts ?? []
    );

    const receivableRoleAccounts = await optionalNonCriticalRequest(() =>
      listLedgerAccounts({
        companyCode: accountingCompanyCode,
        search: "",
        type: "Asset",
        status: "Active",
        posting: "posting",
        role: "ClientReceivable",
        viewMode: "flat",
        level: ""
      })
    );

    setReceivableAccounts(
      receivableRoleAccounts !== null && receivableRoleAccounts.length > 0
        ? receivableRoleAccounts
        : assetAccounts ?? []
    );
  }

  async function loadProductModuleOptions() {
    const modules = await optionalNonCriticalRequest(() => listProductModules());
    setProductModules(modules ?? []);
  }

  async function handleRefresh() {
    if (selectedClientId === "") {
      await loadClients();
      return;
    }

    await loadClientWorkspace(selectedClientId);
  }

  async function handleCreateClient() {
    const displayName = createForm.displayName.trim() === ""
      ? createForm.legalName
      : createForm.displayName;

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const createdClient = await createClient({
        ...createForm,
        code: createForm.code.trim(),
        legalName: createForm.legalName.trim(),
        displayName: displayName.trim()
      });

      setCreateForm(createDefaultCreateClientForm());
      setIsCreatePanelOpen(false);
      setActiveTab("setup");
      setClientSearch("");
      await loadClients(createdClient.clientId, false, "");
      await loadClientWorkspace(createdClient.clientId);
      setNotice(`${createdClient.displayName || createdClient.legalName} created. Continue setup here.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Client could not be created."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleUpdateClient() {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const updatedClient = await updateClient(workspace.client.clientId, {
        legalName: editForm.legalName.trim(),
        displayName: editForm.displayName.trim()
      });
      setEditForm(toClientEditForm(updatedClient));
      setClients((current) =>
        current.map((client) =>
          client.clientId === updatedClient.clientId ? toClientLookup(updatedClient) : client
        )
      );
      await loadClientWorkspace(updatedClient.clientId);
      setNotice("Client master record saved.");
    } catch (caughtError) {
      setError(formatError(caughtError, "Client master record could not be saved."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleActivateClient() {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const updatedClient = await activateClient(workspace.client.clientId);
      setClients((current) =>
        current.map((client) =>
          client.clientId === updatedClient.clientId ? toClientLookup(updatedClient) : client
        )
      );
      await loadClientWorkspace(updatedClient.clientId);
      setActiveTab("setup");
      setNotice("Client activated.");
    } catch (caughtError) {
      setError(formatError(caughtError, "Client could not be activated."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleSuspendClient() {
    if (workspace === null) {
      return;
    }

    if (!window.confirm(`Suspend ${workspace.client.displayName || workspace.client.legalName}?`)) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const updatedClient = await suspendClient(workspace.client.clientId);
      setClients((current) =>
        current.map((client) =>
          client.clientId === updatedClient.clientId ? toClientLookup(updatedClient) : client
        )
      );
      await loadClientWorkspace(updatedClient.clientId);
      setActiveTab("setup");
      setNotice("Client suspended.");
    } catch (caughtError) {
      setError(formatError(caughtError, "Client could not be suspended."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handlePublishOutbox() {
    setIsPublishing(true);
    setError("");
    setNotice("");

    try {
      const result = await publishCloudOutboxMessages(20);
      setLatestPublishResult(result);

      if (selectedClientId !== "") {
        await loadClientWorkspace(selectedClientId);
      }

      setNotice(
        `${result.publishedCount} cloud message${result.publishedCount === 1 ? "" : "s"} published.`
      );
    } catch (caughtError) {
      setError(formatError(caughtError, "Cloud messages could not be published."));
    } finally {
      setIsPublishing(false);
    }
  }

  async function handleAddContact() {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const contact = await addClientContact(workspace.client.clientId, contactForm);
      await loadClientWorkspace(workspace.client.clientId);
      setContactForm(createDefaultContactForm(workspace.client));
      setNotice(`${contact.fullName} added.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Contact could not be added."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleSaveAccountingProfile() {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const profile = await configureClientAccountingProfile(
        workspace.client.clientId,
        accountingProfileForm
      );
      await loadClientWorkspace(workspace.client.clientId);
      setAccountingProfileForm(toAccountingProfileForm(profile));
      setNotice("Accounting profile saved.");
    } catch (caughtError) {
      setError(formatError(caughtError, "Accounting profile could not be saved."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleCreateContract() {
    if (workspace === null) {
      return;
    }

    if (activeContract !== null && !window.confirm(
      `Approve contract revision ${activeContract.revisionNumber + 1} to replace revision ${activeContract.revisionNumber}?\n\n${contractForm.approvalReason.trim()}`
    )) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const contract = activeContract === null
        ? await createClientContract(workspace.client.clientId, contractForm)
        : (await replaceActiveClientContract(workspace.client.clientId, contractForm)).activeContract;
      await loadClientWorkspace(workspace.client.clientId);
      setContractForm(createDefaultContractForm(workspace.client.code, productModules));
      setActiveTab("billing");
      setNotice(`Contract ${contract.contractNumber} revision ${contract.revisionNumber} approved.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Contract could not be created."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleSaveDeployment() {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const deployment = await configureClientDeployment(
        workspace.client.clientId,
        deploymentForm
      );
      await loadClientWorkspace(workspace.client.clientId);
      setDeploymentForm(toDeploymentForm(deployment));
      setActiveTab("cloud");
      setNotice(`${deployment.displayName} saved.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Deployment could not be saved."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleAddSupportNote() {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      await addClientSupportNote(workspace.client.clientId, supportNoteForm);
      await loadClientWorkspace(workspace.client.clientId);
      setSupportNoteForm(createDefaultSupportNoteForm(supportNoteForm.createdBy));
      setNotice("Support note added.");
    } catch (caughtError) {
      setError(formatError(caughtError, "Support note could not be added."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleInvitePortalContact(clientContactId: string) {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const invitation = await inviteClientPortalContact(
        workspace.client.clientId,
        clientContactId
      );
      await loadClientWorkspace(workspace.client.clientId);
      setActiveTab("notes");
      setNotice(`Portal invitation sent to ${invitation.email}.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Portal invitation could not be sent."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleResendPortalInvitation(invitationId: string) {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const invitation = await resendClientPortalInvitation(
        workspace.client.clientId,
        invitationId
      );
      await loadClientWorkspace(workspace.client.clientId);
      setActiveTab("notes");
      setNotice(`Portal invitation resent to ${invitation.email}.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Portal invitation could not be resent."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleRevokePortalInvitation(invitationId: string) {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const invitation = await revokeClientPortalInvitation(
        workspace.client.clientId,
        invitationId
      );
      await loadClientWorkspace(workspace.client.clientId);
      setActiveTab("notes");
      setNotice(`Portal invitation revoked for ${invitation.email}.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Portal invitation could not be revoked."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleGenerateInvoiceDraft() {
    if (workspace === null) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const draft = await generateInvoiceDraft(workspace.client.clientId, invoiceDraftForm);
      setInvoiceDraft(draft);
      setIssueInvoiceForm(createDefaultIssueInvoiceForm(draft.issueDate, workspace.accountingProfile));
      setPaymentForm(
        createDefaultPaymentForm({
          client: workspace.client,
          invoice: null,
          draft,
          accountingProfile: workspace.accountingProfile,
          cashAccountId: cashAccounts[0]?.ledgerAccountId ?? paymentForm.cashOrBankAccountId
        })
      );
      await loadClientWorkspace(workspace.client.clientId);
      setNotice(`Invoice voucher ${draft.invoiceNumber} drafted.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Invoice voucher could not be drafted."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleIssueInvoice() {
    if (workspace === null || invoiceDraft === null) {
      return;
    }

    if (!window.confirm(`Issue invoice ${invoiceDraft.invoiceNumber}?`)) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const issued = await issueInvoice(invoiceDraft.invoiceId, issueInvoiceForm);
      const issuedDraft = {
        ...invoiceDraft,
        status: issued.invoiceStatus
      };

      setInvoiceDraft(issuedDraft);
      setPaymentForm(
        createDefaultPaymentForm({
          client: workspace.client,
          invoice: null,
          draft: issuedDraft,
          accountingProfile: workspace.accountingProfile,
          cashAccountId: cashAccounts[0]?.ledgerAccountId ?? paymentForm.cashOrBankAccountId
        })
      );
      await loadClientWorkspace(workspace.client.clientId);
      setActiveTab("payments");
      setNotice(`Invoice voucher ${issued.invoiceNumber} issued.`);
    } catch (caughtError) {
      setError(formatError(caughtError, "Invoice voucher could not be issued."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleRecordReceipt() {
    if (workspace === null) {
      return;
    }

    if (!window.confirm(`Record receipt ${paymentForm.reference}?`)) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const payment = await recordInvoicePayment(paymentForm);
      setInvoiceDraft((current) =>
        current === null || current.invoiceId !== payment.invoiceId
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
        reference: defaultReceiptReference(workspace.client.code, new Date())
      }));
      await loadClientWorkspace(workspace.client.clientId);
      setActiveTab(payment.invoiceStatus.toLowerCase() === "paid" ? "access" : "vouchers");
      setNotice(
        payment.paymentStatus === "PendingReview"
          ? `Receipt ${payment.invoiceNumber} recorded for review.`
          : `Receipt ${payment.invoiceNumber} recorded.`
      );
    } catch (caughtError) {
      setError(formatError(caughtError, "Receipt voucher could not be recorded."));
    } finally {
      setIsActionBusy(false);
    }
  }

  async function handleIssueAccessRenewal() {
    if (
      workspace === null
      || accessInvoiceId === ""
      || accessApprovalReason.trim() === ""
    ) {
      return;
    }

    const effectiveFromUtc = isAccessScheduled
      ? toUtcIsoString(accessEffectiveFrom)
      : null;

    if (isAccessScheduled && effectiveFromUtc === null) {
      setError("A valid future effective time is required for a scheduled access change.");
      return;
    }

    const invoice = paidInvoices.find((item) => item.invoiceId === accessInvoiceId);
    const effectiveText = effectiveFromUtc === null
      ? "Effective immediately"
      : `Effective ${formatDateTime(effectiveFromUtc)}`;

    if (!window.confirm(
      `Approve access renewal from ${invoice?.invoiceNumber ?? "paid invoice"}?\n\n${effectiveText}\n${accessApprovalReason.trim()}`
    )) {
      return;
    }

    setIsActionBusy(true);
    setError("");
    setNotice("");

    try {
      const snapshot = await issueEntitlementFromPaidInvoiceDefaults(
        accessInvoiceId,
        accessApprovalReason.trim(),
        effectiveFromUtc
      );
      await loadClientWorkspace(workspace.client.clientId);
      setActiveTab("cloud");
      setIsAccessScheduled(false);
      setNotice(
        effectiveFromUtc === null
          ? `Access renewal issued from ${snapshot.invoiceNumber}.`
          : `Access revision ${snapshot.entitlementVersion} scheduled for ${formatDateTime(snapshot.effectiveFromUtc)}.`
      );
    } catch (caughtError) {
      setError(formatError(caughtError, "Access renewal could not be issued."));
    } finally {
      setIsActionBusy(false);
    }
  }

  const summary = workspace?.statement?.currencySummaries[0] ?? null;
  const latestInvoice = workspace?.statement?.invoices[0] ?? null;
  const latestPayment = workspace?.statement?.payments[0] ?? null;
  const openInvoiceCount = workspace?.statement?.currencySummaries.reduce(
    (total, item) => total + item.openInvoiceCount,
    0
  ) ?? 0;
  const pendingCloudCount = outboxSummary === null
    ? selectedOutboxMessages.filter((message) => message.status !== "Sent").length
    : outboxSummary.pendingCount + outboxSummary.failedCount;
  const failedCloudCount = outboxSummary?.failedCount
    ?? selectedOutboxMessages.filter((message) => message.status === "Failed").length;
  const isBusy = isLoadingClients || isLoadingWorkspace;

  return (
    <section className="client360-workspace">
      <div className="client360-command-strip">
        <form className="client360-client-search" onSubmit={handleClientSearch}>
          <label className="form-field">
            <span>Find Client</span>
            <input
              maxLength={128}
              placeholder="Code or name"
              value={clientSearch}
              onChange={(event) => setClientSearch(event.target.value)}
            />
          </label>
          <button
            className="icon-button"
            disabled={isLoadingClients}
            title="Search clients"
            type="submit"
          >
            <Search size={16} />
          </button>
        </form>

        <label className="form-field">
          <span>Client</span>
          <select
            disabled={isLoadingClients || clients.length === 0}
            value={selectedClientId}
            onChange={(event) => setSelectedClientId(event.target.value)}
          >
            {clients.length === 0 ? (
              <option value="">No clients</option>
            ) : (
              clients.map((client) => (
                <option key={client.clientId} value={client.clientId}>
                  {client.code} - {client.displayName || client.legalName}
                </option>
              ))
            )}
          </select>
          <small>{clientFilteredCount} matches / {clients.length} available</small>
        </label>

        <div className="client360-command-actions">
          {clientNextCursor !== null && (
            <button
              className="icon-button"
              disabled={isLoadingOlderClients}
              onClick={() => loadClients(selectedClientId, true, clientSearch)}
              title="Load more clients"
              type="button"
            >
              <ChevronDown size={16} />
            </button>
          )}
          <button
            className={isCreatePanelOpen ? "icon-button primary" : "icon-button"}
            disabled={isBusy || isActionBusy}
            onClick={() => setIsCreatePanelOpen((current) => !current)}
            title="Create a new client"
            type="button"
          >
            <Plus size={16} />
            New Client
          </button>
          <button
            className="icon-button"
            disabled={isBusy}
            onClick={handleRefresh}
            title="Refresh client workspace"
            type="button"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
          <button
            className="icon-button primary"
            disabled={isPublishing || pendingCloudCount === 0}
            onClick={handlePublishOutbox}
            title="Publish pending cloud messages"
            type="button"
          >
            <Send size={16} />
            Send Cloud
          </button>
        </div>
      </div>

      {(isCreatePanelOpen || (!isLoadingClients && clients.length === 0)) && (
        <NewClientPanel
          canCancel={clients.length > 0}
          createForm={createForm}
          isActionBusy={isActionBusy}
          onCancel={() => setIsCreatePanelOpen(false)}
          onCreate={handleCreateClient}
          onCreateFormChange={setCreateForm}
        />
      )}

      {(error !== "" || notice !== "") && (
        <div className="client360-message-row">
          {error !== "" && (
            <span className="status-error" role="alert">
              <AlertCircle size={16} />
              {error}
            </span>
          )}
          {notice !== "" && (
            <span className="status-success">
              <CheckCircle2 size={16} />
              {notice}
            </span>
          )}
        </div>
      )}

      {workspace === null ? (
        <EmptyWorkspace isLoading={isBusy} />
      ) : (
        <>
          <header className="client360-client-header">
            <div>
              <span>{workspace.client.code}</span>
              <h2>{workspace.client.displayName || workspace.client.legalName}</h2>
              <small>{workspace.client.legalName}</small>
            </div>
            <div className="client360-header-badges">
              <StatusPill status={workspace.client.status} />
              <span>{workspace.client.contacts.length} contacts</span>
              <span>{workspace.deployments.length} deployments</span>
            </div>
          </header>

          <div className="client360-status-grid" aria-label="Client status">
            <StatusCard
              icon={<Building2 size={18} />}
              label="Client"
              value={workspace.client.status}
              detail={workspace.client.contacts[0]?.fullName ?? "No primary contact"}
            />
            <StatusCard
              icon={<WalletCards size={18} />}
              label="Balance"
              value={summary === null ? "No statement" : formatMoney(summary.balanceDue, summary.currencyCode)}
              detail={`${openInvoiceCount} open invoice${openInvoiceCount === 1 ? "" : "s"}`}
            />
            <StatusCard
              icon={<FileText size={18} />}
              label="Contract"
              value={activeContract?.status ?? "No active contract"}
              detail={
                activeContract === null
                  ? "Contract needed"
                  : `${formatMoney(activeContract.recurringAmount, activeContract.currencyCode)} ${activeContract.billingCycle}`
              }
            />
            <StatusCard
              icon={<ShieldCheck size={18} />}
              label="Access"
              value={workspace.entitlement?.status ?? "No entitlement"}
              detail={workspace.entitlement === null ? "Not issued" : `Paid until ${formatDate(workspace.entitlement.paidUntil)}`}
            />
            <StatusCard
              icon={<Server size={18} />}
              label="Installation"
              value={primaryDeployment?.displayName ?? "No deployment"}
              detail={primaryDeployment?.installationId ?? "Deployment needed"}
            />
            <StatusCard
              icon={<Cloud size={18} />}
              label="Cloud"
              value={workspace.cloudStatus?.installationStatus ?? "Not registered"}
              detail={
                workspace.cloudStatus?.latestHeartbeat === null || workspace.cloudStatus?.latestHeartbeat === undefined
                  ? `${pendingCloudCount} pending message${pendingCloudCount === 1 ? "" : "s"}`
                  : `Heartbeat ${formatDateTime(workspace.cloudStatus.latestHeartbeat.receivedAtUtc)}`
              }
              tone={failedCloudCount > 0 ? "danger" : pendingCloudCount > 0 ? "warning" : "normal"}
            />
          </div>

          <div className="client360-tabs" role="tablist" aria-label="Client workspace tabs">
            {tabs.map((tab) => (
              <button
                aria-selected={activeTab === tab.key}
                className={activeTab === tab.key ? "active" : ""}
                key={tab.key}
                onClick={() => setActiveTab(tab.key)}
                role="tab"
                type="button"
              >
                {tab.label}
              </button>
            ))}
          </div>

          <div className="client360-main-panel" role="tabpanel">
            {activeTab === "overview" && (
              <OverviewTab
                latestInvoice={latestInvoice}
                latestPayment={latestPayment}
                progressTrail={progressTrail}
                setupGaps={setupGaps}
                vouchers={vouchers}
                workspace={workspace}
                nextRequiredAction={nextRequiredAction}
                onOpenNextAction={(tab) => setActiveTab(tab)}
              />
            )}
            {activeTab === "setup" && (
              <SetupTab
                activeContract={activeContract}
                accountingProfile={workspace.accountingProfile}
                accountingProfileForm={accountingProfileForm}
                contactForm={contactForm}
                contractForm={contractForm}
                deploymentForm={deploymentForm}
                editForm={editForm}
                isActionBusy={isActionBusy}
                primaryDeployment={primaryDeployment}
                productModules={productModules}
                receivableAccounts={receivableAccounts}
                setupGaps={setupGaps}
                workspace={workspace}
                onAccountingProfileFormChange={setAccountingProfileForm}
                onActivateClient={handleActivateClient}
                onAddContact={handleAddContact}
                onContactFormChange={setContactForm}
                onContractFormChange={setContractForm}
                onCreateContract={handleCreateContract}
                onDeploymentFormChange={setDeploymentForm}
                onEditFormChange={setEditForm}
                onSaveAccountingProfile={handleSaveAccountingProfile}
                onSaveDeployment={handleSaveDeployment}
                onSuspendClient={handleSuspendClient}
                onUpdateClient={handleUpdateClient}
              />
            )}
            {activeTab === "billing" && (
              <BillingTab
                activeContract={activeContract}
                accountingProfile={workspace.accountingProfile}
                contracts={workspace.contracts}
                invoiceDraft={invoiceDraft}
                invoiceDraftForm={invoiceDraftForm}
                issueInvoiceForm={issueInvoiceForm}
                isActionBusy={isActionBusy}
                statement={workspace.statement}
                onGenerateInvoiceDraft={handleGenerateInvoiceDraft}
                onIssueInvoice={handleIssueInvoice}
                onInvoiceDraftFormChange={setInvoiceDraftForm}
                onIssueInvoiceFormChange={setIssueInvoiceForm}
                onLoadMoreStatement={handleLoadMoreStatement}
                onOpenVouchers={() => setActiveTab("vouchers")}
              />
            )}
            {activeTab === "payments" && (
              <PaymentsTab
                accountingProfile={workspace.accountingProfile}
                cashAccounts={cashAccounts}
                isActionBusy={isActionBusy}
                paymentForm={paymentForm}
                payments={workspace.statement?.payments ?? []}
                statement={workspace.statement}
                onPaymentFormChange={setPaymentForm}
                onRecordReceipt={handleRecordReceipt}
                onLoadMoreStatement={handleLoadMoreStatement}
                onOpenVouchers={() => setActiveTab("vouchers")}
              />
            )}
            {activeTab === "vouchers" && (
              <VouchersTab
                isActionBusy={isActionBusy}
                statement={workspace.statement}
                vouchers={vouchers}
                onLoadMoreStatement={handleLoadMoreStatement}
              />
            )}
            {activeTab === "access" && (
              <AccessTab
                entitlement={workspace.entitlement}
                contract={activeContract}
                paidInvoices={paidInvoices}
                selectedInvoiceId={accessInvoiceId}
                approvalReason={accessApprovalReason}
                effectiveFrom={accessEffectiveFrom}
                isScheduled={isAccessScheduled}
                isActionBusy={isActionBusy}
                statement={workspace.statement}
                onIssueAccessRenewal={handleIssueAccessRenewal}
                onApprovalReasonChange={setAccessApprovalReason}
                onEffectiveFromChange={setAccessEffectiveFrom}
                onScheduledChange={setIsAccessScheduled}
                onSelectedInvoiceChange={setAccessInvoiceId}
                onLoadMoreStatement={handleLoadMoreStatement}
              />
            )}
            {activeTab === "cloud" && (
              <CloudTab
                cloudStatus={workspace.cloudStatus}
                deployments={workspace.deployments}
                latestPublishResult={latestPublishResult}
                messages={selectedOutboxMessages}
                summary={outboxSummary}
                hasMoreMessages={outboxNextCursor !== null}
                isLoadingOlderMessages={isLoadingOlderOutbox}
                onPublish={handlePublishOutbox}
                onLoadOlderMessages={loadOlderOutboxMessages}
                isPublishing={isPublishing}
              />
            )}
            {activeTab === "notes" && (
              <NotesTab
                isActionBusy={isActionBusy}
                supportNoteForm={supportNoteForm}
                workspace={workspace}
                onAddSupportNote={handleAddSupportNote}
                onInvitePortalContact={handleInvitePortalContact}
                onResendPortalInvitation={handleResendPortalInvitation}
                onRevokePortalInvitation={handleRevokePortalInvitation}
                onSupportNoteFormChange={setSupportNoteForm}
              />
            )}
          </div>
        </>
      )}
    </section>
  );
}

function NewClientPanel({
  canCancel,
  createForm,
  isActionBusy,
  onCancel,
  onCreate,
  onCreateFormChange
}: {
  canCancel: boolean;
  createForm: CreateClientInput;
  isActionBusy: boolean;
  onCancel: () => void;
  onCreate: () => void;
  onCreateFormChange: (value: CreateClientInput) => void;
}) {
  const canCreateClient =
    createForm.code.trim() !== "" && createForm.legalName.trim() !== "";

  return (
    <section className="client360-panel client360-create-panel">
      <div className="client360-panel-header action">
        <div>
          <span>Client master</span>
          <h3>New Client</h3>
        </div>
        <div className="client360-action-buttons">
          {canCancel && (
            <button
              className="icon-button"
              disabled={isActionBusy}
              onClick={onCancel}
              type="button"
            >
              Cancel
            </button>
          )}
          <button
            className="icon-button primary"
            disabled={isActionBusy || !canCreateClient}
            onClick={onCreate}
            type="button"
          >
            <Plus size={16} />
            Create
          </button>
        </div>
      </div>

      <div className="client360-form-grid create">
        <label className="form-field">
          <span>Code</span>
          <input
            disabled={isActionBusy}
            maxLength={30}
            value={createForm.code}
            onChange={(event) =>
              onCreateFormChange({
                ...createForm,
                code: event.target.value.toUpperCase()
              })
            }
          />
        </label>

        <label className="form-field">
          <span>Legal Name</span>
          <input
            disabled={isActionBusy}
            value={createForm.legalName}
            onChange={(event) =>
              onCreateFormChange({
                ...createForm,
                legalName: event.target.value
              })
            }
          />
        </label>

        <label className="form-field">
          <span>Display Name</span>
          <input
            disabled={isActionBusy}
            value={createForm.displayName}
            onChange={(event) =>
              onCreateFormChange({
                ...createForm,
                displayName: event.target.value
              })
            }
          />
        </label>
      </div>
    </section>
  );
}

function OverviewTab({
  latestInvoice,
  latestPayment,
  nextRequiredAction,
  progressTrail,
  setupGaps,
  vouchers,
  workspace,
  onOpenNextAction
}: {
  latestInvoice: ClientStatementInvoice | null;
  latestPayment: ClientStatementPayment | null;
  nextRequiredAction: NextRequiredAction;
  progressTrail: ClientProgressTrail;
  setupGaps: string[];
  vouchers: VoucherRow[];
  workspace: ClientWorkspace;
  onOpenNextAction: (tab: Client360Tab) => void;
}) {
  return (
    <div className="client360-overview-grid">
      <section className="client360-panel">
        <PanelHeader icon={<Layers3 size={17} />} title="Chain" eyebrow="Client flow" />
        <div className="client360-chain">
          {[
            ["Client", workspace.client.status],
            ["Contract", findActiveContract(workspace.contracts)?.status ?? "Missing"],
            ["Invoice", latestInvoice?.status ?? "No invoice"],
            ["Payment", latestPayment?.status ?? "No payment"],
            ["Access", workspace.entitlement?.status ?? "No entitlement"],
            ["Cloud", workspace.cloudStatus?.installationStatus ?? "Not registered"]
          ].map(([label, value], index, items) => (
            <div className="client360-chain-item" key={label}>
              <span>{label}</span>
              <strong>{value}</strong>
              {index < items.length - 1 && <ArrowRight size={15} />}
            </div>
          ))}
        </div>
      </section>

      <section className={`client360-panel client360-next-action ${nextRequiredAction.tone}`}>
        <div className="client360-next-action-copy">
          <span>Next required action</span>
          <h3>{nextRequiredAction.label}</h3>
          <p>{nextRequiredAction.detail}</p>
        </div>
        <button
          className="icon-button primary"
          disabled={nextRequiredAction.tone === "done"}
          onClick={() => onOpenNextAction(nextRequiredAction.tab)}
          type="button"
        >
          <ArrowRight size={16} />
          Open
        </button>
      </section>

      <section className="client360-panel client360-progress-panel wide">
        <PanelHeader icon={<CheckCircle2 size={17} />} title="Progress Trail" eyebrow="Client evidence" />
        <div className="client360-progress-pair">
          <ProgressCard
            label="Last done"
            event={progressTrail.lastDone}
            fallback="No completed client event"
            tone="done"
          />
          <div className={`client360-progress-card ${nextRequiredAction.tone}`}>
            <span>Next pending</span>
            <strong>{nextRequiredAction.label}</strong>
            <small>{nextRequiredAction.detail}</small>
          </div>
        </div>
        <div className="client360-progress-list">
          {progressTrail.recentEvents.length === 0 ? (
            <div className="client360-empty compact">No recent trail</div>
          ) : (
            progressTrail.recentEvents.slice(0, 5).map((event) => (
              <div className="client360-progress-row" key={event.key}>
                <span>
                  <strong>{event.label}</strong>
                  <small>{event.detail}</small>
                </span>
                <time>{formatProgressTime(event.occurredAtUtc)}</time>
              </div>
            ))
          )}
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<AlertCircle size={17} />} title="Setup Gaps" eyebrow="Readiness" />
        {setupGaps.length === 0 ? (
          <div className="client360-empty compact">Ready</div>
        ) : (
          <ul className="client360-gap-list">
            {setupGaps.map((gap) => (
              <li key={gap}>
                <AlertCircle size={15} />
                {gap}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Receipt size={17} />} title="Recent Vouchers" eyebrow="Commercial" />
        <VoucherMiniList vouchers={vouchers.slice(0, 5)} />
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<NotebookText size={17} />} title="Contacts & Notes" eyebrow="Relationship" />
        <div className="client360-contact-list">
          {workspace.client.contacts.slice(0, 3).map((contact) => (
            <div className="client360-contact-row" key={contact.clientContactId}>
              <strong>{contact.fullName}</strong>
              <span>{contact.role}</span>
              <small>{contact.email ?? contact.phone ?? "No contact detail"}</small>
            </div>
          ))}
          {workspace.client.contacts.length === 0 && (
            <div className="client360-empty compact">No contacts</div>
          )}
        </div>
      </section>

    </div>
  );
}

function SetupTab({
  activeContract,
  accountingProfile,
  accountingProfileForm,
  contactForm,
  contractForm,
  deploymentForm,
  editForm,
  isActionBusy,
  primaryDeployment,
  productModules,
  receivableAccounts,
  setupGaps,
  workspace,
  onAccountingProfileFormChange,
  onActivateClient,
  onAddContact,
  onContactFormChange,
  onContractFormChange,
  onCreateContract,
  onDeploymentFormChange,
  onEditFormChange,
  onSaveAccountingProfile,
  onSaveDeployment,
  onSuspendClient,
  onUpdateClient
}: {
  activeContract: ClientContract | null;
  accountingProfile: ClientAccountingProfile | null;
  accountingProfileForm: ConfigureClientAccountingProfileInput;
  contactForm: AddClientContactInput;
  contractForm: ClientContractFormInput;
  deploymentForm: ConfigureClientDeploymentInput;
  editForm: UpdateClientInput;
  isActionBusy: boolean;
  primaryDeployment: ClientDeployment | null;
  productModules: ProductModule[];
  receivableAccounts: LedgerAccountSummary[];
  setupGaps: string[];
  workspace: ClientWorkspace;
  onAccountingProfileFormChange: (value: ConfigureClientAccountingProfileInput) => void;
  onActivateClient: () => void;
  onAddContact: () => void;
  onContactFormChange: (value: AddClientContactInput) => void;
  onContractFormChange: (value: ClientContractFormInput) => void;
  onCreateContract: () => void;
  onDeploymentFormChange: (value: ConfigureClientDeploymentInput) => void;
  onEditFormChange: (value: UpdateClientInput) => void;
  onSaveAccountingProfile: () => void;
  onSaveDeployment: () => void;
  onSuspendClient: () => void;
  onUpdateClient: () => void;
}) {
  const clientStatus = workspace.client.status.trim().toLowerCase();
  const canSaveClient = editForm.legalName.trim() !== "";
  const canActivateClient = clientStatus !== "active";
  const canSuspendClient = clientStatus !== "suspended";
  const canAddContact = contactForm.role.trim() !== "" && contactForm.fullName.trim() !== "";
  const canSaveProfile =
    accountingProfileForm.accountsReceivableAccountId.trim() !== ""
    && accountingProfileForm.defaultCurrencyCode.trim().length === 3;
  const canCreateContract =
    contractForm.contractNumber.trim() !== ""
    && contractForm.startsOn !== ""
    && contractForm.endsOn !== ""
    && Number(contractForm.recurringAmount) >= 0
    && contractForm.currencyCode.trim().length === 3
    && Number(contractForm.billingDayOfMonth) >= 1
    && Number(contractForm.allowedDevices) >= 1
    && Number(contractForm.allowedBranches) >= 1
    && isOptionalLimitValid(contractForm.allowedNamedUsers)
    && isOptionalLimitValid(contractForm.allowedConcurrentUsers)
    && isUserLimitOrderValid(
      contractForm.allowedNamedUsers,
      contractForm.allowedConcurrentUsers
    )
    && contractForm.featureLimits.every((limit) =>
      limit.moduleCode.trim() !== ""
      && limit.featureCode.trim() !== ""
      && Number(limit.limitValue) >= 0
      && limit.unit.trim() !== "")
    && contractForm.approvalReason.trim() !== "";
  const canSaveDeployment =
    deploymentForm.installationId.trim() !== ""
    && deploymentForm.displayName.trim() !== ""
    && deploymentForm.siteId.trim() !== ""
    && deploymentForm.siteRole.trim() !== ""
    && deploymentForm.localServerVersion.trim() !== "";

  return (
    <div className="client360-setup-grid">
      <section className="client360-panel wide">
        <PanelHeader icon={<AlertCircle size={17} />} title="Setup Gaps" eyebrow="Readiness" />
        {setupGaps.length === 0 ? (
          <div className="client360-empty compact">Ready</div>
        ) : (
          <ul className="client360-gap-list">
            {setupGaps.map((gap) => (
              <li key={gap}>
                <AlertCircle size={15} />
                {gap}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="client360-panel wide">
        <div className="client360-panel-header action">
          <div>
            <span>Client master</span>
            <h3>Master Record</h3>
          </div>
          <StatusPill status={workspace.client.status} />
        </div>

        <div className="client360-form-grid master">
          <label className="form-field">
            <span>Code</span>
            <input readOnly value={workspace.client.code} />
          </label>

          <label className="form-field">
            <span>Legal Name</span>
            <input
              disabled={isActionBusy}
              value={editForm.legalName}
              onChange={(event) =>
                onEditFormChange({
                  ...editForm,
                  legalName: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Display Name</span>
            <input
              disabled={isActionBusy}
              value={editForm.displayName}
              onChange={(event) =>
                onEditFormChange({
                  ...editForm,
                  displayName: event.target.value
                })
              }
            />
          </label>
        </div>

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact label="Created" value={formatDate(workspace.client.createdAtUtc)} />
            <Fact
              label="Activated"
              value={
                workspace.client.activatedAtUtc === null || workspace.client.activatedAtUtc === undefined
                  ? "Not yet"
                  : formatDate(workspace.client.activatedAtUtc)
              }
            />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button"
              disabled={isActionBusy || !canActivateClient}
              onClick={onActivateClient}
              type="button"
            >
              <CheckCircle2 size={16} />
              Activate
            </button>
            <button
              className="icon-button danger"
              disabled={isActionBusy || !canSuspendClient}
              onClick={onSuspendClient}
              type="button"
            >
              <PauseCircle size={16} />
              Suspend
            </button>
            <button
              className="icon-button primary"
              disabled={isActionBusy || !canSaveClient}
              onClick={onUpdateClient}
              type="button"
            >
              <Save size={16} />
              Save
            </button>
          </div>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Building2 size={17} />} title="Contact" eyebrow="People" />
        <div className="client360-form-grid setup">
          <label className="form-field">
            <span>Role</span>
            <select
              disabled={isActionBusy}
              value={contactForm.role}
              onChange={(event) =>
                onContactFormChange({ ...contactForm, role: event.target.value })
              }
            >
              <option value="Billing">Billing</option>
              <option value="Support">Support</option>
              <option value="Owner">Owner</option>
              <option value="Technical">Technical</option>
            </select>
          </label>

          <label className="form-field">
            <span>Full Name</span>
            <input
              disabled={isActionBusy}
              value={contactForm.fullName}
              onChange={(event) =>
                onContactFormChange({ ...contactForm, fullName: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Email</span>
            <input
              disabled={isActionBusy}
              type="email"
              value={contactForm.email}
              onChange={(event) =>
                onContactFormChange({ ...contactForm, email: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Phone</span>
            <input
              disabled={isActionBusy}
              value={contactForm.phone}
              onChange={(event) =>
                onContactFormChange({ ...contactForm, phone: event.target.value })
              }
            />
          </label>
        </div>

        <label className="client360-checkbox-row">
          <input
            checked={contactForm.isPrimary}
            disabled={isActionBusy}
            type="checkbox"
            onChange={(event) =>
              onContactFormChange({ ...contactForm, isPrimary: event.target.checked })
            }
          />
          <span>Primary contact</span>
        </label>

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact label="Current" value={String(workspace.client.contacts.length)} />
            <Fact
              label="Primary"
              value={workspace.client.contacts.find((contact) => contact.isPrimary)?.fullName ?? "None"}
            />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button primary"
              disabled={isActionBusy || !canAddContact}
              onClick={onAddContact}
              type="button"
            >
              <CheckCircle2 size={16} />
              Add
            </button>
          </div>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<WalletCards size={17} />} title="Accounting Profile" eyebrow="Posting" />
        <div className="client360-form-grid setup">
          <label className="form-field">
            <span>Receivable Account</span>
            <select
              disabled={isActionBusy || receivableAccounts.length === 0}
              value={accountingProfileForm.accountsReceivableAccountId}
              onChange={(event) =>
                onAccountingProfileFormChange({
                  ...accountingProfileForm,
                  accountsReceivableAccountId: event.target.value
                })
              }
            >
              {receivableAccounts.length === 0 ? (
                <option value="">No account</option>
              ) : (
                receivableAccounts.map((account) => (
                  <option key={account.ledgerAccountId} value={account.ledgerAccountId}>
                    {account.displayCode} - {account.name}
                  </option>
                ))
              )}
            </select>
          </label>

          <label className="form-field">
            <span>Currency</span>
            <input
              disabled={isActionBusy}
              maxLength={3}
              value={accountingProfileForm.defaultCurrencyCode}
              onChange={(event) =>
                onAccountingProfileFormChange({
                  ...accountingProfileForm,
                  defaultCurrencyCode: event.target.value.toUpperCase()
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Cloud Customer</span>
            <input
              disabled={isActionBusy}
              value={accountingProfileForm.cloudCustomerId}
              onChange={(event) =>
                onAccountingProfileFormChange({
                  ...accountingProfileForm,
                  cloudCustomerId: event.target.value
                })
              }
            />
          </label>
        </div>

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact label="Status" value={accountingProfile === null ? "Missing" : "Saved"} />
            <Fact label="Currency" value={accountingProfile?.defaultCurrencyCode ?? "Not set"} />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button primary"
              disabled={isActionBusy || !canSaveProfile}
              onClick={onSaveAccountingProfile}
              type="button"
            >
              <CheckCircle2 size={16} />
              Save
            </button>
          </div>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<FileText size={17} />} title="Contract" eyebrow="Commercial" />
        <div className="client360-form-grid setup contract">
          <label className="form-field">
            <span>Contract No.</span>
            <input
              disabled={isActionBusy}
              maxLength={40}
              value={contractForm.contractNumber}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, contractNumber: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Recurring</span>
            <input
              disabled={isActionBusy}
              min="0"
              step="0.01"
              type="number"
              value={contractForm.recurringAmount}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, recurringAmount: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Currency</span>
            <input
              disabled={isActionBusy}
              maxLength={3}
              value={contractForm.currencyCode}
              onChange={(event) =>
                onContractFormChange({
                  ...contractForm,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Cycle</span>
            <select
              disabled={isActionBusy}
              value={contractForm.billingCycle}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, billingCycle: event.target.value })
              }
            >
              <option value="Monthly">Monthly</option>
              <option value="Quarterly">Quarterly</option>
              <option value="Yearly">Yearly</option>
            </select>
          </label>

          <label className="form-field">
            <span>Billing Day</span>
            <input
              disabled={isActionBusy}
              max="31"
              min="1"
              type="number"
              value={contractForm.billingDayOfMonth}
              onChange={(event) =>
                onContractFormChange({
                  ...contractForm,
                  billingDayOfMonth: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Starts</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={contractForm.startsOn}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, startsOn: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Ends</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={contractForm.endsOn}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, endsOn: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Devices</span>
            <input
              disabled={isActionBusy}
              min="1"
              type="number"
              value={contractForm.allowedDevices}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, allowedDevices: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Branches</span>
            <input
              disabled={isActionBusy}
              min="1"
              type="number"
              value={contractForm.allowedBranches}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, allowedBranches: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Named Users</span>
            <input
              disabled={isActionBusy}
              min="0"
              placeholder="No cap"
              type="number"
              value={contractForm.allowedNamedUsers}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, allowedNamedUsers: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Concurrent Users</span>
            <input
              disabled={isActionBusy}
              min="0"
              placeholder="No cap"
              type="number"
              value={contractForm.allowedConcurrentUsers}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, allowedConcurrentUsers: event.target.value })
              }
            />
          </label>

          <label className="form-field wide">
            <span>Modules</span>
            <input
              disabled={isActionBusy}
              value={contractForm.moduleCodes}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, moduleCodes: event.target.value })
              }
            />
          </label>

          <label className="form-field wide">
            <span>Approval Reason</span>
            <input
              disabled={isActionBusy}
              maxLength={1000}
              value={contractForm.approvalReason}
              onChange={(event) =>
                onContractFormChange({ ...contractForm, approvalReason: event.target.value })
              }
            />
          </label>
        </div>

        <fieldset className="contract-feature-limits" disabled={isActionBusy}>
          <legend>Feature Limits</legend>
          <div className="contract-feature-limit-heading">
            <span>{contractForm.featureLimits.length} configured</span>
            <button
              className="mini-button"
              type="button"
              onClick={() => onContractFormChange({
                ...contractForm,
                featureLimits: [
                  ...contractForm.featureLimits,
                  {
                    moduleCode: moduleCodesFromText(contractForm.moduleCodes)[0]
                      ?? productModules.find((module) => module.isActive)?.moduleCode
                      ?? "",
                    featureCode: "",
                    limitValue: "0",
                    unit: "COUNT"
                  }
                ]
              })}
            >
              <Plus size={14} />
              Add
            </button>
          </div>
          {contractForm.featureLimits.length === 0 ? (
            <div className="client360-empty compact">No feature-specific limits</div>
          ) : (
            <div className="contract-feature-limit-list">
              {contractForm.featureLimits.map((limit, index) => (
                <div className="contract-feature-limit-row" key={`${index}-${limit.featureCode}`}>
                  <label className="form-field">
                    <span>Module</span>
                    <select
                      value={limit.moduleCode}
                      onChange={(event) => onContractFormChange({
                        ...contractForm,
                        featureLimits: contractForm.featureLimits.map((item, itemIndex) =>
                          itemIndex === index ? { ...item, moduleCode: event.target.value } : item)
                      })}
                    >
                      {productModules.filter((module) => module.isActive).map((module) => (
                        <option key={module.moduleCode} value={module.moduleCode}>
                          {module.displayName}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="form-field">
                    <span>Feature Code</span>
                    <input
                      maxLength={64}
                      value={limit.featureCode}
                      onChange={(event) => onContractFormChange({
                        ...contractForm,
                        featureLimits: contractForm.featureLimits.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, featureCode: event.target.value.toUpperCase() }
                            : item)
                      })}
                    />
                  </label>
                  <label className="form-field">
                    <span>Limit</span>
                    <input
                      min="0"
                      type="number"
                      value={limit.limitValue}
                      onChange={(event) => onContractFormChange({
                        ...contractForm,
                        featureLimits: contractForm.featureLimits.map((item, itemIndex) =>
                          itemIndex === index ? { ...item, limitValue: event.target.value } : item)
                      })}
                    />
                  </label>
                  <label className="form-field">
                    <span>Unit</span>
                    <input
                      maxLength={32}
                      value={limit.unit}
                      onChange={(event) => onContractFormChange({
                        ...contractForm,
                        featureLimits: contractForm.featureLimits.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, unit: event.target.value.toUpperCase() }
                            : item)
                      })}
                    />
                  </label>
                  <button
                    className="mini-button danger"
                    type="button"
                    title="Remove feature limit"
                    onClick={() => onContractFormChange({
                      ...contractForm,
                      featureLimits: contractForm.featureLimits.filter((_, itemIndex) => itemIndex !== index)
                    })}
                  >
                    <Trash2 size={14} />
                    Remove
                  </button>
                </div>
              ))}
            </div>
          )}
        </fieldset>

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact
              label="Active"
              value={activeContract === null
                ? "Missing"
                : `${activeContract.contractNumber} / revision ${activeContract.revisionNumber}`}
            />
            <Fact label="Modules" value={String(productModules.filter((module) => module.isActive).length)} />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button primary"
              disabled={isActionBusy || !canCreateContract}
              onClick={onCreateContract}
              type="button"
            >
              <CheckCircle2 size={16} />
              {activeContract === null ? "Create" : "Replace"}
            </button>
          </div>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Server size={17} />} title="Deployment" eyebrow="Local server" />
        <div className="client360-form-grid setup deployment">
          <label className="form-field">
            <span>Installation</span>
            <input
              disabled={isActionBusy}
              value={deploymentForm.installationId}
              onChange={(event) =>
                onDeploymentFormChange({ ...deploymentForm, installationId: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Name</span>
            <input
              disabled={isActionBusy}
              value={deploymentForm.displayName}
              onChange={(event) =>
                onDeploymentFormChange({ ...deploymentForm, displayName: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Bootstrap</span>
            <select
              disabled={isActionBusy}
              value={deploymentForm.bootstrapMode}
              onChange={(event) =>
                onDeploymentFormChange({ ...deploymentForm, bootstrapMode: event.target.value })
              }
            >
              <option value="OnlineBootstrap">Online Bootstrap</option>
              <option value="OfflinePackage">Offline Package</option>
            </select>
          </label>

          <label className="form-field">
            <span>Mode</span>
            <select
              disabled={isActionBusy}
              value={deploymentForm.clientDeploymentMode}
              onChange={(event) =>
                onDeploymentFormChange({
                  ...deploymentForm,
                  clientDeploymentMode: event.target.value
                })
              }
            >
              <option value="OfflineLocal">Offline Local</option>
              <option value="CloudConnected">Cloud Connected</option>
            </select>
          </label>

          <label className="form-field">
            <span>Site</span>
            <input
              disabled={isActionBusy}
              value={deploymentForm.siteId}
              onChange={(event) =>
                onDeploymentFormChange({ ...deploymentForm, siteId: event.target.value })
              }
            />
          </label>

          <label className="form-field">
            <span>Role</span>
            <select
              disabled={isActionBusy}
              value={deploymentForm.siteRole}
              onChange={(event) =>
                onDeploymentFormChange({ ...deploymentForm, siteRole: event.target.value })
              }
            >
              <option value="Standalone">Standalone</option>
              <option value="HeadOffice">Head Office</option>
              <option value="Branch">Branch</option>
            </select>
          </label>

          <label className="form-field">
            <span>Local Server</span>
            <input
              disabled={isActionBusy}
              value={deploymentForm.localServerVersion}
              onChange={(event) =>
                onDeploymentFormChange({
                  ...deploymentForm,
                  localServerVersion: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>App Version</span>
            <input
              disabled={isActionBusy}
              value={deploymentForm.safarSuiteAppVersion}
              onChange={(event) =>
                onDeploymentFormChange({
                  ...deploymentForm,
                  safarSuiteAppVersion: event.target.value
                })
              }
            />
          </label>
        </div>

        <label className="client360-checkbox-row">
          <input
            checked={deploymentForm.isPrimary}
            disabled={isActionBusy}
            type="checkbox"
            onChange={(event) =>
              onDeploymentFormChange({ ...deploymentForm, isPrimary: event.target.checked })
            }
          />
          <span>Primary deployment</span>
        </label>

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact label="Current" value={primaryDeployment?.displayName ?? "Missing"} />
            <Fact label="Installation" value={primaryDeployment?.installationId ?? "Not set"} />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button primary"
              disabled={isActionBusy || !canSaveDeployment}
              onClick={onSaveDeployment}
              type="button"
            >
              <CheckCircle2 size={16} />
              Save
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}

function BillingTab({
  activeContract,
  accountingProfile,
  contracts,
  invoiceDraft,
  invoiceDraftForm,
  issueInvoiceForm,
  isActionBusy,
  statement,
  onGenerateInvoiceDraft,
  onIssueInvoice,
  onInvoiceDraftFormChange,
  onIssueInvoiceFormChange,
  onLoadMoreStatement,
  onOpenVouchers
}: {
  activeContract: ClientContract | null;
  accountingProfile: ClientAccountingProfile | null;
  contracts: ClientContract[];
  invoiceDraft: InvoiceDraft | null;
  invoiceDraftForm: InvoiceDraftFormInput;
  issueInvoiceForm: IssueInvoiceFormInput;
  isActionBusy: boolean;
  statement: ClientStatement | null;
  onGenerateInvoiceDraft: () => void;
  onIssueInvoice: () => void;
  onInvoiceDraftFormChange: (value: InvoiceDraftFormInput) => void;
  onIssueInvoiceFormChange: (value: IssueInvoiceFormInput) => void;
  onLoadMoreStatement: (register: ClientStatementRegister) => void;
  onOpenVouchers: () => void;
}) {
  const canGenerateDraft =
    activeContract !== null
    && invoiceDraftForm.contractId.trim() !== ""
    && invoiceDraftForm.invoiceNumber.trim() !== ""
    && invoiceDraftForm.issueDate !== ""
    && invoiceDraftForm.dueDate !== ""
    && invoiceDraftForm.billingDate !== ""
    && invoiceDraftForm.currencyCode.trim().length === 3;
  const canIssueDraft =
    invoiceDraft !== null
    && invoiceDraft.status.toLowerCase() === "draft"
    && issueInvoiceForm.postingDate !== ""
    && issueInvoiceForm.accountsReceivableAccountId.trim() !== "";

  return (
    <div className="client360-two-column">
      <section className="client360-panel wide">
        <div className="client360-panel-header action">
          <div>
            <span>Invoice voucher</span>
            <h3>Draft & Issue</h3>
          </div>
          {invoiceDraft !== null && <StatusPill status={invoiceDraft.status} />}
        </div>

        <div className="client360-form-grid invoice">
          <label className="form-field">
            <span>Contract</span>
            <select
              disabled={activeContract === null || isActionBusy}
              value={invoiceDraftForm.contractId}
              onChange={(event) =>
                onInvoiceDraftFormChange({
                  ...invoiceDraftForm,
                  contractId: event.target.value
                })
              }
            >
              {activeContract === null ? (
                <option value="">No active contract</option>
              ) : (
                <option value={activeContract.contractId}>
                  {activeContract.contractNumber}
                </option>
              )}
            </select>
          </label>

          <label className="form-field">
            <span>Invoice No.</span>
            <input
              disabled={isActionBusy}
              maxLength={40}
              value={invoiceDraftForm.invoiceNumber}
              onChange={(event) =>
                onInvoiceDraftFormChange({
                  ...invoiceDraftForm,
                  invoiceNumber: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Issue Date</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={invoiceDraftForm.issueDate}
              onChange={(event) =>
                onInvoiceDraftFormChange({
                  ...invoiceDraftForm,
                  issueDate: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Due Date</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={invoiceDraftForm.dueDate}
              onChange={(event) =>
                onInvoiceDraftFormChange({
                  ...invoiceDraftForm,
                  dueDate: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Billing Date</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={invoiceDraftForm.billingDate}
              onChange={(event) =>
                onInvoiceDraftFormChange({
                  ...invoiceDraftForm,
                  billingDate: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Currency</span>
            <input
              disabled={isActionBusy}
              maxLength={3}
              value={invoiceDraftForm.currencyCode}
              onChange={(event) =>
                onInvoiceDraftFormChange({
                  ...invoiceDraftForm,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Posting Date</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={issueInvoiceForm.postingDate}
              onChange={(event) =>
                onIssueInvoiceFormChange({
                  ...issueInvoiceForm,
                  postingDate: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Receivable Account</span>
            <input
              disabled={isActionBusy}
              value={issueInvoiceForm.accountsReceivableAccountId}
              onChange={(event) =>
                onIssueInvoiceFormChange({
                  ...issueInvoiceForm,
                  accountsReceivableAccountId: event.target.value
                })
              }
            />
          </label>
        </div>

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact
              label="Draft"
              value={
                invoiceDraft === null
                  ? "None"
                  : `${invoiceDraft.invoiceNumber} / ${formatMoney(invoiceDraft.totalAmount, invoiceDraft.currencyCode)}`
              }
            />
            <Fact
              label="Profile"
              value={accountingProfile === null ? "Missing" : accountingProfile.defaultCurrencyCode}
            />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button"
              disabled={isActionBusy || !canGenerateDraft}
              onClick={onGenerateInvoiceDraft}
              type="button"
            >
              <FileText size={16} />
              Draft
            </button>
            <button
              className="icon-button primary"
              disabled={isActionBusy || !canIssueDraft}
              onClick={onIssueInvoice}
              type="button"
            >
              <Receipt size={16} />
              Issue
            </button>
          </div>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<FileText size={17} />} title="Contract" eyebrow="Agreement" />
        {activeContract === null ? (
          <div className="client360-empty">No active contract</div>
        ) : (
          <div className="client360-fact-grid">
            <Fact label="Number" value={activeContract.contractNumber} />
            <Fact label="Revision" value={`#${activeContract.revisionNumber}`} />
            <Fact label="Product Catalog" value={`#${activeContract.productCatalogRevisionNumber}`} />
            <Fact
              label="Supersedes"
              value={activeContract.supersedesContractId?.slice(0, 8) ?? "Root revision"}
            />
            <Fact label="Status" value={activeContract.status} />
            <Fact label="Effective From" value={formatDate(activeContract.startsOn)} />
            <Fact label="Effective Until" value={formatDate(activeContract.endsOn)} />
            <Fact label="Cycle" value={activeContract.billingCycle} />
            <Fact label="Billing Day" value={String(activeContract.billingDayOfMonth)} />
            <Fact label="Devices" value={String(activeContract.allowedDevices)} />
            <Fact label="Branches" value={String(activeContract.allowedBranches)} />
            <Fact label="Named Users" value={activeContract.allowedNamedUsers?.toString() ?? "No cap"} />
            <Fact label="Concurrent Users" value={activeContract.allowedConcurrentUsers?.toString() ?? "No cap"} />
            <Fact
              label="Recurring"
              value={formatMoney(activeContract.recurringAmount, activeContract.currencyCode)}
            />
            <Fact label="Approved By" value={activeContract.approvedBy} />
            <Fact label="Approved At" value={formatDateTime(activeContract.approvedAtUtc)} />
            <Fact label="Approval Reason" value={activeContract.approvalReason} />
            <Fact label="Modules" value={String(activeContract.modules.filter((item) => item.isEnabled).length)} />
            <Fact label="Feature Limits" value={String((activeContract.featureLimits ?? []).length)} />
          </div>
        )}
      </section>

      <section className="client360-panel wide">
        <PanelHeader icon={<FileText size={17} />} title="Contract Revisions" eyebrow="Commercial history" />
        {contracts.length === 0 ? (
          <div className="client360-empty">No contract history</div>
        ) : (
          <div className="client360-summary-list">
            {contracts.map((contract) => (
              <div
                className="client360-summary-row client360-contract-revision-row"
                key={contract.contractId}
              >
                <strong>Revision {contract.revisionNumber}</strong>
                <span>{contract.contractNumber} - {contract.status}</span>
                <span>Product catalog #{contract.productCatalogRevisionNumber}</span>
                <span>{formatDate(contract.startsOn)} to {formatDate(contract.endsOn)}</span>
                <span>{contract.approvedBy} - {formatDateTime(contract.approvedAtUtc)}</span>
              </div>
            ))}
          </div>
        )}
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Banknote size={17} />} title="Statement" eyebrow="Receivable" />
        {statement === null || statement.currencySummaries.length === 0 ? (
          <div className="client360-empty">No statement activity</div>
        ) : (
          <div className="client360-summary-list">
            {statement.currencySummaries.map((summary) => (
              <div className="client360-summary-row" key={summary.currencyCode}>
                <strong>{summary.currencyCode}</strong>
                <span>{formatMoney(summary.totalInvoiced, summary.currencyCode)} invoiced</span>
                <span>{formatMoney(summary.totalPaid, summary.currencyCode)} paid</span>
                <span>{formatMoney(summary.balanceDue, summary.currencyCode)} due</span>
              </div>
            ))}
          </div>
        )}
      </section>

      <section className="client360-panel wide">
        <PanelHeader icon={<Receipt size={17} />} title="Invoices" eyebrow="Issued" />
        <InvoiceTable invoices={statement?.invoices ?? []} />
        <StatementRegisterContinuation
          isBusy={isActionBusy}
          register="invoices"
          statement={statement}
          onLoadMore={onLoadMoreStatement}
        />
      </section>

      <div className="client360-action-band wide">
        <span>Invoice proof</span>
        <button className="icon-button" onClick={onOpenVouchers} type="button">
          <Receipt size={16} />
          Voucher Register
        </button>
      </div>
    </div>
  );
}

function PaymentsTab({
  accountingProfile,
  cashAccounts,
  isActionBusy,
  paymentForm,
  payments,
  statement,
  onPaymentFormChange,
  onRecordReceipt,
  onLoadMoreStatement,
  onOpenVouchers
}: {
  accountingProfile: ClientAccountingProfile | null;
  cashAccounts: LedgerAccountSummary[];
  isActionBusy: boolean;
  paymentForm: RecordInvoicePaymentInput;
  payments: ClientStatementPayment[];
  statement: ClientStatement | null;
  onPaymentFormChange: (value: RecordInvoicePaymentInput) => void;
  onRecordReceipt: () => void;
  onLoadMoreStatement: (register: ClientStatementRegister) => void;
  onOpenVouchers: () => void;
}) {
  const totalPaid =
    statement?.currencySummaries.reduce((total, item) => total + item.totalPaid, 0) ?? 0;
  const currencyCode = statement?.currencySummaries[0]?.currencyCode ?? "";
  const openInvoices = statement?.invoices.filter((invoice) =>
    invoice.balanceDue > 0 && ["Issued", "PartiallyPaid"].includes(invoice.status)
  ) ?? [];
  const selectedInvoice = openInvoices.find((invoice) => invoice.invoiceId === paymentForm.invoiceId) ?? null;
  const paymentAmount = Number(paymentForm.amount);
  const canRecordReceipt =
    paymentForm.invoiceId.trim() !== ""
    && paymentForm.method.trim() !== ""
    && paymentForm.reference.trim() !== ""
    && Number.isFinite(paymentAmount)
    && paymentAmount > 0
    && paymentForm.currencyCode.trim().length === 3
    && paymentForm.receivedOn !== ""
    && paymentForm.postingDate !== ""
    && paymentForm.cashOrBankAccountId.trim() !== ""
    && paymentForm.accountsReceivableAccountId.trim() !== "";

  return (
    <div className="client360-panel-stack">
      <section className="client360-panel">
        <div className="client360-panel-header action">
          <div>
            <span>Receipt voucher</span>
            <h3>Record Receipt</h3>
          </div>
          <StatusPill status={selectedInvoice === null ? "Draft" : selectedInvoice.status} />
        </div>

        <div className="client360-form-grid receipt">
          <label className="form-field">
            <span>Invoice</span>
            <select
              disabled={isActionBusy || openInvoices.length === 0}
              value={paymentForm.invoiceId}
              onChange={(event) => {
                const invoice = openInvoices.find((item) => item.invoiceId === event.target.value);

                onPaymentFormChange({
                  ...paymentForm,
                  invoiceId: event.target.value,
                  amount: invoice === undefined ? paymentForm.amount : invoice.balanceDue.toFixed(2),
                  currencyCode: invoice?.currencyCode ?? paymentForm.currencyCode
                });
              }}
            >
              {openInvoices.length === 0 ? (
                <option value="">No open invoices</option>
              ) : (
                openInvoices.map((invoice) => (
                  <option key={invoice.invoiceId} value={invoice.invoiceId}>
                    {invoice.invoiceNumber} - {formatMoney(invoice.balanceDue, invoice.currencyCode)}
                  </option>
                ))
              )}
            </select>
          </label>

          <label className="form-field">
            <span>Method</span>
            <select
              disabled={isActionBusy}
              value={paymentForm.method}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  method: event.target.value
                })
              }
            >
              <option value="ManualCash">Manual Cash</option>
              <option value="BankTransfer">Bank Transfer</option>
              <option value="Cheque">Cheque</option>
              <option value="Card">Card</option>
            </select>
          </label>

          <label className="form-field">
            <span>Reference</span>
            <input
              disabled={isActionBusy}
              maxLength={60}
              value={paymentForm.reference}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  reference: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Amount</span>
            <input
              disabled={isActionBusy}
              min="0"
              step="0.01"
              type="number"
              value={paymentForm.amount}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  amount: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Currency</span>
            <input
              disabled={isActionBusy}
              maxLength={3}
              value={paymentForm.currencyCode}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Received On</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={paymentForm.receivedOn}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  receivedOn: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Posting Date</span>
            <input
              disabled={isActionBusy}
              type="date"
              value={paymentForm.postingDate}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  postingDate: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Cash/Bank Account</span>
            <select
              disabled={isActionBusy || cashAccounts.length === 0}
              value={paymentForm.cashOrBankAccountId}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  cashOrBankAccountId: event.target.value
                })
              }
            >
              {cashAccounts.length === 0 ? (
                <option value="">No cash account</option>
              ) : (
                cashAccounts.map((account) => (
                  <option key={account.ledgerAccountId} value={account.ledgerAccountId}>
                    {account.displayCode} - {account.name}
                  </option>
                ))
              )}
            </select>
          </label>

          <label className="form-field">
            <span>Receivable Account</span>
            <input
              disabled={isActionBusy}
              value={paymentForm.accountsReceivableAccountId}
              onChange={(event) =>
                onPaymentFormChange({
                  ...paymentForm,
                  accountsReceivableAccountId: event.target.value
                })
              }
            />
          </label>
        </div>

        <StatementRegisterContinuation
          isBusy={isActionBusy}
          register="invoices"
          statement={statement}
          onLoadMore={onLoadMoreStatement}
        />

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact
              label="Invoice Balance"
              value={
                selectedInvoice === null
                  ? "No invoice"
                  : formatMoney(selectedInvoice.balanceDue, selectedInvoice.currencyCode)
              }
            />
            <Fact
              label="Profile"
              value={accountingProfile === null ? "Missing" : accountingProfile.defaultCurrencyCode}
            />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button primary"
              disabled={isActionBusy || !canRecordReceipt}
              onClick={onRecordReceipt}
              type="button"
            >
              <Receipt size={16} />
              Record
            </button>
          </div>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Banknote size={17} />} title="Payment Position" eyebrow="Cash" />
        <div className="client360-fact-grid">
          <Fact label="Payments" value={String(payments.length)} />
          <Fact label="Total Paid" value={currencyCode === "" ? "0" : formatMoney(totalPaid, currencyCode)} />
          <Fact
            label="Pending Review"
            value={String(payments.filter((payment) => payment.status === "PendingReview").length)}
          />
          <Fact
            label="Latest"
            value={payments[0] === undefined ? "None" : formatDate(payments[0].receivedOn)}
          />
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Receipt size={17} />} title="Receipts" eyebrow="Voucher view" />
        <PaymentTable payments={payments} />
        <StatementRegisterContinuation
          isBusy={isActionBusy}
          register="payments"
          statement={statement}
          onLoadMore={onLoadMoreStatement}
        />
      </section>

      <div className="client360-action-band">
        <span>Receipt proof</span>
        <button className="icon-button" onClick={onOpenVouchers} type="button">
          <Receipt size={16} />
          Voucher Register
        </button>
      </div>
    </div>
  );
}

function VouchersTab({
  isActionBusy,
  statement,
  vouchers,
  onLoadMoreStatement
}: {
  isActionBusy: boolean;
  statement: ClientStatement | null;
  vouchers: VoucherRow[];
  onLoadMoreStatement: (register: ClientStatementRegister) => void;
}) {
  return (
    <section className="client360-panel">
      <PanelHeader icon={<Receipt size={17} />} title="Voucher Register" eyebrow="Simple documents" />
      <VoucherTable vouchers={vouchers} />
      <div className="client360-register-continuations">
        {(["invoices", "payments", "lines"] as ClientStatementRegister[]).map((register) => (
          <StatementRegisterContinuation
            isBusy={isActionBusy}
            key={register}
            register={register}
            statement={statement}
            onLoadMore={onLoadMoreStatement}
          />
        ))}
      </div>
    </section>
  );
}

function AccessTab({
  entitlement,
  contract,
  paidInvoices,
  selectedInvoiceId,
  approvalReason,
  effectiveFrom,
  isScheduled,
  isActionBusy,
  statement,
  onIssueAccessRenewal,
  onApprovalReasonChange,
  onEffectiveFromChange,
  onScheduledChange,
  onSelectedInvoiceChange,
  onLoadMoreStatement
}: {
  entitlement: EntitlementSnapshot | null;
  contract: ClientContract | null;
  paidInvoices: ClientStatementInvoice[];
  selectedInvoiceId: string;
  approvalReason: string;
  effectiveFrom: string;
  isScheduled: boolean;
  isActionBusy: boolean;
  statement: ClientStatement | null;
  onIssueAccessRenewal: () => void;
  onApprovalReasonChange: (reason: string) => void;
  onEffectiveFromChange: (value: string) => void;
  onScheduledChange: (value: boolean) => void;
  onSelectedInvoiceChange: (invoiceId: string) => void;
  onLoadMoreStatement: (register: ClientStatementRegister) => void;
}) {
  const selectedInvoice =
    paidInvoices.find((invoice) => invoice.invoiceId === selectedInvoiceId) ?? null;
  const scheduledAt = effectiveFrom === "" ? Number.NaN : new Date(effectiveFrom).getTime();
  const scheduleReady = !isScheduled
    || (!Number.isNaN(scheduledAt) && scheduledAt > Date.now());

  return (
    <div className="client360-two-column">
      <section className="client360-panel wide">
        <div className="client360-panel-header action">
          <div>
            <span>Access renewal</span>
            <h3>Issue From Paid Invoice</h3>
          </div>
          <StatusPill status={selectedInvoice === null ? "Draft" : selectedInvoice.status} />
        </div>

        <div className="client360-form-grid access">
          <label className="form-field">
            <span>Paid Invoice</span>
            <select
              disabled={isActionBusy || paidInvoices.length === 0}
              value={selectedInvoiceId}
              onChange={(event) => onSelectedInvoiceChange(event.target.value)}
            >
              {paidInvoices.length === 0 ? (
                <option value="">No paid invoice</option>
              ) : (
                paidInvoices.map((invoice) => (
                  <option key={invoice.invoiceId} value={invoice.invoiceId}>
                    {invoice.invoiceNumber} - {formatMoney(invoice.totalAmount, invoice.currencyCode)}
                  </option>
                ))
              )}
            </select>
          </label>

          <label className="form-field wide">
            <span>Approval Reason</span>
            <input
              disabled={isActionBusy}
              maxLength={1000}
              type="text"
              value={approvalReason}
              onChange={(event) => onApprovalReasonChange(event.target.value)}
            />
          </label>

          <label className="checkbox-field client360-schedule-toggle">
            <input
              checked={isScheduled}
              disabled={isActionBusy}
              onChange={(event) => onScheduledChange(event.target.checked)}
              type="checkbox"
            />
            <span>Schedule change</span>
          </label>

          {isScheduled && (
            <label className="form-field">
              <span>Effective From</span>
              <input
                disabled={isActionBusy}
                min={toLocalDateTimeInputValue(new Date())}
                onChange={(event) => onEffectiveFromChange(event.target.value)}
                type="datetime-local"
                value={effectiveFrom}
              />
            </label>
          )}

          <Fact
            label="Paid Until"
            value={entitlement === null ? "No renewal" : formatDate(entitlement.paidUntil)}
          />
          <Fact
            label="Offline Valid"
            value={entitlement === null ? "No renewal" : formatDate(entitlement.offlineValidUntil)}
          />
        </div>

        <StatementRegisterContinuation
          isBusy={isActionBusy}
          register="invoices"
          statement={statement}
          onLoadMore={onLoadMoreStatement}
        />

        <div className="client360-action-panel-footer">
          <div className="client360-inline-facts">
            <Fact
              label="Source"
              value={selectedInvoice === null ? "No invoice" : selectedInvoice.invoiceNumber}
            />
            <Fact
              label="Amount"
              value={
                selectedInvoice === null
                  ? "0"
                  : formatMoney(selectedInvoice.totalAmount, selectedInvoice.currencyCode)
              }
            />
          </div>
          <div className="client360-action-buttons">
            <button
              className="icon-button primary"
              disabled={
                isActionBusy
                || selectedInvoiceId === ""
                || approvalReason.trim() === ""
                || !scheduleReady
              }
              onClick={onIssueAccessRenewal}
              type="button"
            >
              <KeyRound size={16} />
              Issue Renewal
            </button>
          </div>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<KeyRound size={17} />} title="Entitlement" eyebrow="Current access" />
        {entitlement === null ? (
          <div className="client360-empty">No entitlement issued</div>
        ) : (
          <div className="client360-fact-grid">
            <Fact label="Status" value={entitlement.status} />
            <Fact
              label="Access Revision"
              value={`#${entitlement.entitlementVersion} / ${entitlement.clientAccessRevisionId.slice(0, 8)}`}
            />
            <Fact label="Contract Revision" value={`#${entitlement.contractRevisionNumber}`} />
            <Fact label="Product Catalog" value={`#${entitlement.productCatalogRevisionNumber}`} />
            <Fact label="Approved By" value={entitlement.approvedBy} />
            <Fact label="Approved At" value={formatDateTime(entitlement.approvedAtUtc)} />
            <Fact label="Effective From" value={formatDateTime(entitlement.effectiveFromUtc)} />
            <Fact label="Approval Reason" value={entitlement.approvalReason} />
            <Fact label="Paid Until" value={formatDate(entitlement.paidUntil)} />
            <Fact label="Grace Until" value={formatDate(entitlement.graceUntil)} />
            <Fact label="Offline Valid" value={formatDate(entitlement.offlineValidUntil)} />
            <Fact label="Devices" value={String(entitlement.allowedDevices)} />
            <Fact label="Branches" value={String(entitlement.allowedBranches)} />
            <Fact label="Named Users" value={entitlement.allowedNamedUsers?.toString() ?? "No cap"} />
            <Fact label="Concurrent Users" value={entitlement.allowedConcurrentUsers?.toString() ?? "No cap"} />
            <Fact label="Issued" value={formatDateTime(entitlement.issuedAtUtc)} />
            <Fact label="Modules" value={String(entitlement.modules.filter((module) => module.isEnabled).length)} />
          </div>
        )}
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Layers3 size={17} />} title="Modules" eyebrow="Allowed surface" />
        <ModuleList
          moduleCodes={
            entitlement?.modules
              .filter((module) => module.isEnabled)
              .map((module) => module.moduleCode)
            ?? contract?.modules
              .filter((module) => module.isEnabled)
              .map((module) => module.moduleCode)
            ?? []
          }
        />
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Layers3 size={17} />} title="Feature Limits" eyebrow="Enforced quantities" />
        {(entitlement?.featureLimits ?? contract?.featureLimits ?? []).length === 0 ? (
          <div className="client360-empty compact">No feature-specific limits</div>
        ) : (
          <div className="client360-mini-list">
            {(entitlement?.featureLimits ?? contract?.featureLimits ?? []).map((limit) => (
              <div className="client360-mini-row" key={`${limit.moduleCode}-${limit.featureCode}`}>
                <strong>{limit.moduleCode}.{limit.featureCode}</strong>
                <span>{limit.limitValue} {limit.unit}</span>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function CloudTab({
  cloudStatus,
  deployments,
  latestPublishResult,
  messages,
  summary,
  hasMoreMessages,
  isLoadingOlderMessages,
  onPublish,
  onLoadOlderMessages,
  isPublishing
}: {
  cloudStatus: ControlCloudInstallationStatus | null;
  deployments: ClientDeployment[];
  latestPublishResult: PublishCloudOutboxMessagesResult | null;
  messages: CloudOutboxMessage[];
  summary: CloudOutboxMessageRegisterSummary | null;
  hasMoreMessages: boolean;
  isLoadingOlderMessages: boolean;
  onPublish: () => void;
  onLoadOlderMessages: () => void;
  isPublishing: boolean;
}) {
  const primaryDeployment = findPrimaryDeployment(deployments);
  const pendingCount = summary === null
    ? messages.filter((message) => message.status !== "Sent").length
    : summary.pendingCount + summary.failedCount;
  const failedCount = summary?.failedCount
    ?? messages.filter((message) => message.status === "Failed").length;
  const totalCount = summary?.totalCount ?? messages.length;
  const readyCount = summary?.readyForPublishingCount ?? pendingCount;
  const lastMessage = findLatestOutboxMessage(messages);
  const latestHeartbeat = cloudStatus?.latestHeartbeat ?? null;
  const reconciliation = cloudStatus?.reconciliation ?? null;
  const localEntitlementVersion = latestHeartbeat?.entitlementVersion ?? null;
  const cloudEntitlementVersion = cloudStatus?.latestEntitlementVersion ?? null;
  const localAccessStatus =
    reconciliation !== null
      ? reconciliation.state
      : cloudStatus === null
      ? "No status"
      : cloudEntitlementVersion === null
        ? "No access"
        : localEntitlementVersion === null
          ? "Not reported"
          : localEntitlementVersion >= cloudEntitlementVersion
            ? "Current"
            : `Waiting for v${cloudEntitlementVersion}`;
  const publishResultText =
    latestPublishResult === null
      ? "No send"
      : `${latestPublishResult.publishedCount} sent / ${latestPublishResult.failedCount} failed`;
  const latestSentAt =
    latestPublishResult?.messages.find((message) => message.sentAtUtc !== null)?.sentAtUtc
    ?? lastMessage?.sentAtUtc
    ?? null;

  return (
    <div className="client360-panel-stack">
      <section className="client360-panel">
        <div className="client360-panel-header action">
          <div>
            <span>Cloud checkpoint</span>
            <h3>Send & Local Pull</h3>
          </div>
          <button
            className="icon-button primary"
            disabled={isPublishing || readyCount === 0}
            onClick={onPublish}
            type="button"
          >
            <Send size={16} />
            Send to Cloud
          </button>
        </div>

        <div className="client360-cloud-summary-grid">
          <CloudSignal
            icon={<Cloud size={17} />}
            label="Outbox"
            value={`${pendingCount} pending`}
            detail={
              failedCount === 0
                ? `${totalCount} client updates`
                : `${failedCount} failed update${failedCount === 1 ? "" : "s"}`
            }
            tone={failedCount > 0 ? "danger" : pendingCount > 0 ? "warning" : "ready"}
          />
          <CloudSignal
            icon={<Send size={17} />}
            label="Last Send"
            value={publishResultText}
            detail={latestSentAt === null ? "No cloud receipt" : formatDateTime(latestSentAt)}
            tone={latestPublishResult?.failedCount ? "danger" : "ready"}
          />
          <CloudSignal
            icon={<Server size={17} />}
            label="Last Seen"
            value={
              latestHeartbeat === null
                ? "No heartbeat"
                : formatDateTime(latestHeartbeat.receivedAtUtc)
            }
            detail={latestHeartbeat?.heartbeatStatus ?? "Waiting for local server"}
            tone={latestHeartbeat === null ? "warning" : "ready"}
          />
          <CloudSignal
            icon={<KeyRound size={17} />}
            label="Local Access"
            value={localAccessStatus}
            detail={
              cloudEntitlementVersion === null
                ? "No cloud access version"
                : `Cloud v${cloudEntitlementVersion} / Local v${localEntitlementVersion ?? "-"}`
            }
            tone={localAccessStatus === "Current" || localAccessStatus === "InSync" ? "ready" : "warning"}
          />
          <CloudSignal
            icon={<RefreshCw size={17} />}
            label="Commands"
            value={`${cloudStatus?.commandStatus.pendingCommandCount ?? 0} pending`}
            detail={cloudStatus?.commandStatus.latestCommandType ?? "No support command"}
            tone={(cloudStatus?.commandStatus.pendingCommandCount ?? 0) > 0 ? "warning" : "ready"}
          />
        </div>
      </section>

      <section className="client360-panel wide">
        <PanelHeader
          icon={<GitCompareArrows size={17} />}
          title="Access Reconciliation"
          eyebrow="Control loop"
        />
        <EntitlementReconciliationTable reconciliation={reconciliation} />
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Server size={17} />} title="Deployment" eyebrow="Local server" />
        {primaryDeployment === null ? (
          <div className="client360-empty">No deployment configured</div>
        ) : (
          <div className="client360-fact-grid">
            <Fact label="Name" value={primaryDeployment.displayName} />
            <Fact label="Installation" value={primaryDeployment.installationId} />
            <Fact label="Mode" value={primaryDeployment.clientDeploymentMode} />
            <Fact label="Site" value={primaryDeployment.siteId} />
            <Fact label="Role" value={primaryDeployment.siteRole} />
            <Fact label="Local Server" value={primaryDeployment.localServerVersion} />
          </div>
        )}
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Cloud size={17} />} title="Cloud Status" eyebrow="Runtime" />
        {cloudStatus === null ? (
          <div className="client360-empty">No cloud status</div>
        ) : (
          <div className="client360-fact-grid">
            <Fact label="Status" value={cloudStatus.installationStatus} />
            <Fact label="Registered" value={formatDateTime(cloudStatus.registeredAtUtc)} />
            <Fact label="Entitlement Version" value={String(cloudStatus.latestEntitlementVersion)} />
            <Fact
              label="Access Revision"
              value={
                cloudStatus.latestEntitlement === null
                  ? "Not signed"
                  : cloudStatus.latestEntitlement.clientAccessRevisionId.slice(0, 8)
              }
            />
            <Fact
              label="Contract Revision"
              value={
                cloudStatus.latestEntitlement === null
                  ? "Not signed"
                  : cloudStatus.latestEntitlement.contractRevisionNumber > 0
                    ? `#${cloudStatus.latestEntitlement.contractRevisionNumber}`
                    : "Historical"
              }
            />
            <Fact
              label="Product Catalog"
              value={
                cloudStatus.latestEntitlement === null
                  ? "Not signed"
                  : cloudStatus.latestEntitlement.productCatalogRevisionNumber > 0
                    ? `#${cloudStatus.latestEntitlement.productCatalogRevisionNumber}`
                    : "Historical"
              }
            />
            <Fact
              label="Named Users"
              value={cloudStatus.latestEntitlement?.allowedNamedUsers?.toString() ?? "No cap"}
            />
            <Fact
              label="Concurrent Users"
              value={cloudStatus.latestEntitlement?.allowedConcurrentUsers?.toString() ?? "No cap"}
            />
            <Fact
              label="Feature Limits"
              value={String(cloudStatus.latestEntitlement?.featureLimitCount ?? 0)}
            />
            <Fact
              label="Heartbeat"
              value={
                cloudStatus.latestHeartbeat === null
                  ? "No heartbeat"
                  : formatDateTime(cloudStatus.latestHeartbeat.receivedAtUtc)
              }
            />
            <Fact
              label="License"
              value={cloudStatus.latestHeartbeat?.licenseStatus ?? "No heartbeat"}
            />
            <Fact
              label="Commands"
              value={`${cloudStatus.commandStatus.pendingCommandCount} pending`}
            />
          </div>
        )}
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Cloud size={17} />} title="Cloud Updates" eyebrow="Outbox" />
        <OutboxTable
          messages={messages}
          hasMore={hasMoreMessages}
          isLoadingOlder={isLoadingOlderMessages}
          onLoadOlder={onLoadOlderMessages}
        />
      </section>
    </div>
  );
}

function NotesTab({
  isActionBusy,
  supportNoteForm,
  workspace,
  onAddSupportNote,
  onInvitePortalContact,
  onResendPortalInvitation,
  onRevokePortalInvitation,
  onSupportNoteFormChange
}: {
  isActionBusy: boolean;
  supportNoteForm: AddClientSupportNoteInput;
  workspace: ClientWorkspace;
  onAddSupportNote: () => void;
  onInvitePortalContact: (clientContactId: string) => void;
  onResendPortalInvitation: (invitationId: string) => void;
  onRevokePortalInvitation: (invitationId: string) => void;
  onSupportNoteFormChange: (value: AddClientSupportNoteInput) => void;
}) {
  const canAddNote =
    supportNoteForm.text.trim() !== ""
    && supportNoteForm.createdBy.trim() !== "";
  const portalInvitations = [...workspace.portalInvitations].sort(
    (first, second) =>
      new Date(second.invitedAtUtc).getTime() - new Date(first.invitedAtUtc).getTime()
  );

  return (
    <div className="client360-two-column">
      <section className="client360-panel wide">
        <div className="client360-panel-header action">
          <div>
            <span>Support note</span>
            <h3>Add Note</h3>
          </div>
          <button
            className="icon-button primary"
            disabled={isActionBusy || !canAddNote}
            onClick={onAddSupportNote}
            type="button"
          >
            <NotebookText size={16} />
            Add
          </button>
        </div>

        <div className="client360-note-form">
          <label className="form-field">
            <span>Note</span>
            <textarea
              disabled={isActionBusy}
              rows={4}
              value={supportNoteForm.text}
              onChange={(event) =>
                onSupportNoteFormChange({
                  ...supportNoteForm,
                  text: event.target.value
                })
              }
            />
          </label>

          <label className="form-field">
            <span>Created By</span>
            <input
              disabled={isActionBusy}
              value={supportNoteForm.createdBy}
              onChange={(event) =>
                onSupportNoteFormChange({
                  ...supportNoteForm,
                  createdBy: event.target.value
                })
              }
            />
          </label>
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<NotebookText size={17} />} title="Support Notes" eyebrow="History" />
        <div className="client360-note-list">
          {workspace.client.supportNotes.map((note, index) => (
            <div className="client360-note-row" key={`${note.createdAtUtc}-${index}`}>
              <span>{formatDateTime(note.createdAtUtc)}</span>
              <strong>{note.createdBy}</strong>
              <p>{note.text}</p>
            </div>
          ))}
          {workspace.client.supportNotes.length === 0 && (
            <div className="client360-empty">No support notes</div>
          )}
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<Building2 size={17} />} title="Contacts" eyebrow="People" />
        <div className="client360-contact-list">
          {workspace.client.contacts.map((contact) => (
            <div
              className="client360-contact-row actionable"
              key={contact.clientContactId}
            >
              <div className="client360-contact-copy">
                <strong>{contact.fullName}</strong>
                <span>{contact.role}{contact.isPrimary ? " / Primary" : ""}</span>
                <small>{contact.email ?? "No email"}</small>
                <small>{contact.phone ?? "No phone"}</small>
              </div>
              <button
                className="icon-button"
                disabled={isActionBusy || !hasEmail(contact.email)}
                onClick={() => onInvitePortalContact(contact.clientContactId)}
                title={hasEmail(contact.email) ? "Invite to portal" : "Contact needs an email"}
                type="button"
              >
                <Send size={14} />
                Invite
              </button>
            </div>
          ))}
          {workspace.client.contacts.length === 0 && (
            <div className="client360-empty">No contacts</div>
          )}
        </div>
      </section>

      <section className="client360-panel">
        <PanelHeader icon={<KeyRound size={17} />} title="Portal Invitations" eyebrow="Access" />
        <div className="client360-portal-list">
          {portalInvitations.map((invitation) => (
            <div className="client360-portal-row" key={invitation.invitationId}>
              <div className="client360-portal-copy">
                <strong>{invitation.fullName}</strong>
                <span>{invitation.email}</span>
                <small>
                  {invitation.status} / invited {formatDateTime(invitation.invitedAtUtc)}
                </small>
                <small>Expires {formatDateTime(invitation.expiresAtUtc)}</small>
              </div>
              <div className="client360-portal-actions">
                <button
                  className="icon-button"
                  disabled={isActionBusy || !canResendPortalInvitation(invitation)}
                  onClick={() => onResendPortalInvitation(invitation.invitationId)}
                  type="button"
                >
                  <RefreshCw size={14} />
                  Resend
                </button>
                <button
                  className="icon-button danger"
                  disabled={isActionBusy || !canRevokePortalInvitation(invitation)}
                  onClick={() => onRevokePortalInvitation(invitation.invitationId)}
                  type="button"
                >
                  <ShieldCheck size={14} />
                  Revoke
                </button>
              </div>
            </div>
          ))}
          {portalInvitations.length === 0 && (
            <div className="client360-empty">No portal invitations</div>
          )}
        </div>
      </section>
    </div>
  );
}

function hasEmail(value: string | null | undefined): boolean {
  return value !== null && value !== undefined && value.trim() !== "";
}

function canResendPortalInvitation(invitation: ClientPortalInvitation): boolean {
  const status = invitation.status.trim().toLowerCase();
  return status !== "revoked" && status !== "accepted";
}

function canRevokePortalInvitation(invitation: ClientPortalInvitation): boolean {
  const status = invitation.status.trim().toLowerCase();
  return status !== "revoked" && status !== "accepted" && status !== "expired";
}

function EmptyWorkspace({ isLoading }: { isLoading: boolean }) {
  return (
    <div className="client360-empty workspace">
      {isLoading ? "Loading client workspace" : "No client selected"}
    </div>
  );
}

function StatusCard({
  icon,
  label,
  value,
  detail,
  tone = "normal"
}: {
  icon: ReactNode;
  label: string;
  value: string;
  detail: string;
  tone?: "normal" | "warning" | "danger";
}) {
  return (
    <section className={`client360-status-card ${tone}`}>
      <div>
        {icon}
        <span>{label}</span>
      </div>
      <strong>{value}</strong>
      <small>{detail}</small>
    </section>
  );
}

function CloudSignal({
  icon,
  label,
  value,
  detail,
  tone
}: {
  icon: ReactNode;
  label: string;
  value: string;
  detail: string;
  tone: "ready" | "warning" | "danger";
}) {
  return (
    <div className={`client360-cloud-signal ${tone}`}>
      <div>
        {icon}
        <span>{label}</span>
      </div>
      <strong>{value}</strong>
      <small>{detail}</small>
    </div>
  );
}

function PanelHeader({
  icon,
  title,
  eyebrow
}: {
  icon: ReactNode;
  title: string;
  eyebrow: string;
}) {
  return (
    <div className="client360-panel-header">
      {icon}
      <div>
        <span>{eyebrow}</span>
        <h3>{title}</h3>
      </div>
    </div>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div className="client360-fact">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function ProgressCard({
  label,
  event,
  fallback,
  tone
}: {
  label: string;
  event: ClientProgressEvent | null;
  fallback: string;
  tone: "done" | "ready" | "warning";
}) {
  return (
    <div className={`client360-progress-card ${tone}`}>
      <span>{label}</span>
      <strong>{event?.label ?? fallback}</strong>
      <small>{event?.detail ?? "Waiting for activity"}</small>
      {event !== null && <time>{formatProgressTime(event.occurredAtUtc)}</time>}
    </div>
  );
}

function StatementRegisterContinuation({
  isBusy,
  register,
  statement,
  onLoadMore
}: {
  isBusy: boolean;
  register: ClientStatementRegister;
  statement: ClientStatement | null;
  onLoadMore: (register: ClientStatementRegister) => void;
}) {
  const page = statement?.registers[register];

  if (page?.hasMore !== true) {
    return null;
  }

  const loaded = statement?.[register].length ?? 0;

  return (
    <div className="client360-register-continuation">
      <span>{loaded} / {page.filteredCount}</span>
      <button
        className="icon-button"
        disabled={isBusy}
        onClick={() => onLoadMore(register)}
        type="button"
      >
        <ChevronDown size={15} />
        Load more
      </button>
    </div>
  );
}

function StatusPill({ status }: { status: string }) {
  return <span className={`status-pill ${status.toLowerCase()}`}>{status}</span>;
}

function VoucherMiniList({ vouchers }: { vouchers: VoucherRow[] }) {
  if (vouchers.length === 0) {
    return <div className="client360-empty compact">No vouchers</div>;
  }

  return (
    <div className="client360-mini-list">
      {vouchers.map((voucher) => (
        <div className="client360-mini-row" key={voucher.id}>
          <span>{voucher.type}</span>
          <strong>{voucher.reference}</strong>
          <small>{formatMoney(voucher.amount, voucher.currencyCode)}</small>
        </div>
      ))}
    </div>
  );
}

function InvoiceTable({ invoices }: { invoices: ClientStatementInvoice[] }) {
  if (invoices.length === 0) {
    return <div className="client360-empty">No invoices</div>;
  }

  return (
    <div className="client360-table-frame">
      <table className="client360-table">
        <thead>
          <tr>
            <th>Invoice</th>
            <th>Issue</th>
            <th>Due</th>
            <th>Status</th>
            <th className="numeric">Total</th>
            <th className="numeric">Balance</th>
          </tr>
        </thead>
        <tbody>
          {invoices.map((invoice) => (
            <tr key={invoice.invoiceId}>
              <td>{invoice.invoiceNumber}</td>
              <td>{formatDate(invoice.issueDate)}</td>
              <td>{formatDate(invoice.dueDate)}</td>
              <td>{invoice.status}</td>
              <td className="numeric">{formatMoney(invoice.totalAmount, invoice.currencyCode)}</td>
              <td className="numeric">{formatMoney(invoice.balanceDue, invoice.currencyCode)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function PaymentTable({ payments }: { payments: ClientStatementPayment[] }) {
  if (payments.length === 0) {
    return <div className="client360-empty">No receipts</div>;
  }

  return (
    <div className="client360-table-frame">
      <table className="client360-table">
        <thead>
          <tr>
            <th>Reference</th>
            <th>Date</th>
            <th>Method</th>
            <th>Status</th>
            <th className="numeric">Amount</th>
          </tr>
        </thead>
        <tbody>
          {payments.map((payment) => (
            <tr key={payment.paymentId}>
              <td>{payment.reference}</td>
              <td>{formatDate(payment.receivedOn)}</td>
              <td>{payment.method}</td>
              <td>{payment.status}</td>
              <td className="numeric">{formatMoney(payment.amount, payment.currencyCode)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function VoucherTable({ vouchers }: { vouchers: VoucherRow[] }) {
  if (vouchers.length === 0) {
    return <div className="client360-empty">No vouchers</div>;
  }

  return (
    <div className="client360-table-frame">
      <table className="client360-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Type</th>
            <th>Reference</th>
            <th>Source</th>
            <th>Status</th>
            <th className="numeric">Amount</th>
          </tr>
        </thead>
        <tbody>
          {vouchers.map((voucher) => (
            <tr key={voucher.id}>
              <td>{formatDate(voucher.date)}</td>
              <td>{voucher.type}</td>
              <td>{voucher.reference}</td>
              <td>{voucher.source}</td>
              <td>{voucher.status}</td>
              <td className="numeric">{formatMoney(voucher.amount, voucher.currencyCode)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ModuleList({ moduleCodes }: { moduleCodes: string[] }) {
  if (moduleCodes.length === 0) {
    return <div className="client360-empty">No modules</div>;
  }

  return (
    <div className="client360-module-list">
      {moduleCodes.map((moduleCode) => (
        <span key={moduleCode}>{moduleCode}</span>
      ))}
    </div>
  );
}

function OutboxTable({
  messages,
  hasMore,
  isLoadingOlder,
  onLoadOlder
}: {
  messages: CloudOutboxMessage[];
  hasMore: boolean;
  isLoadingOlder: boolean;
  onLoadOlder: () => void;
}) {
  if (messages.length === 0) {
    return <div className="client360-empty">No client cloud messages</div>;
  }

  return (
    <>
      <div className="client360-table-frame">
        <table className="client360-table">
        <thead>
          <tr>
            <th>Type</th>
            <th>Subject</th>
            <th>Status</th>
            <th>Occurred</th>
            <th>Attempts</th>
          </tr>
        </thead>
        <tbody>
          {messages.map((message) => (
            <tr key={message.cloudOutboxMessageId}>
              <td>{message.messageType}</td>
              <td>{message.subjectType}</td>
              <td>{message.status}</td>
              <td>{formatDateTime(message.occurredAtUtc)}</td>
              <td>{message.attemptCount}</td>
            </tr>
          ))}
        </tbody>
        </table>
      </div>
      {hasMore && (
        <div className="client-panel-actions">
          <button
            className="icon-button"
            disabled={isLoadingOlder}
            onClick={onLoadOlder}
            title="Load older cloud updates"
            type="button"
          >
            <ChevronDown size={16} />
            {isLoadingOlder ? "Loading" : "Load older"}
          </button>
        </div>
      )}
    </>
  );
}

function findActiveContract(contracts: ClientContract[]): ClientContract | null {
  return (
    contracts.find((contract) => contract.status.toLowerCase() === "active")
    ?? contracts[0]
    ?? null
  );
}

function findPrimaryDeployment(deployments: ClientDeployment[]): ClientDeployment | null {
  return deployments.find((deployment) => deployment.isPrimary) ?? deployments[0] ?? null;
}

function findOpenInvoice(statement: ClientStatement | null): ClientStatementInvoice | null {
  if (statement === null) {
    return null;
  }

  return (
    statement.invoices.find((invoice) =>
      invoice.balanceDue > 0
      && ["issued", "partiallypaid"].includes(invoice.status.toLowerCase())
    )
    ?? statement.invoices.find((invoice) => invoice.balanceDue > 0)
    ?? null
  );
}

function findDraftInvoice(statement: ClientStatement | null): ClientStatementInvoice | null {
  if (statement === null) {
    return null;
  }

  return statement.invoices.find((invoice) => invoice.status.toLowerCase() === "draft") ?? null;
}

function findPaidInvoices(statement: ClientStatement | null): ClientStatementInvoice[] {
  if (statement === null) {
    return [];
  }

  return statement.invoices
    .filter((invoice) => invoice.status.toLowerCase() === "paid")
    .sort((first, second) =>
      new Date(second.issueDate).getTime() - new Date(first.issueDate).getTime()
    );
}

function findLatestOutboxMessage(messages: CloudOutboxMessage[]): CloudOutboxMessage | null {
  return [...messages].sort(
    (first, second) =>
      new Date(second.occurredAtUtc).getTime() - new Date(first.occurredAtUtc).getTime()
  )[0] ?? null;
}

function createVoucherRows(statement: ClientStatement | null): VoucherRow[] {
  if (statement === null) {
    return [];
  }

  const invoiceRows: VoucherRow[] = statement.invoices.map((invoice) => ({
    id: `invoice-${invoice.invoiceId}`,
    date: invoice.issueDate,
    type: "Invoice",
    reference: invoice.invoiceNumber,
    status: invoice.status,
    amount: invoice.totalAmount,
    currencyCode: invoice.currencyCode,
    source: invoice.journalEntryId === null || invoice.journalEntryId === undefined ? "Commercial" : "Journal"
  }));

  const paymentRows: VoucherRow[] = statement.payments.map((payment) => ({
    id: `payment-${payment.paymentId}`,
    date: payment.receivedOn,
    type: "Receipt",
    reference: payment.reference,
    status: payment.status,
    amount: payment.amount,
    currencyCode: payment.currencyCode,
    source: payment.journalEntryId === null || payment.journalEntryId === undefined ? "Commercial" : "Journal"
  }));

  const adjustmentRows: VoucherRow[] = statement.lines
    .filter((line) => line.refundId !== null || line.creditApplicationId !== null)
    .map((line) => ({
      id: `line-${line.entryDate}-${line.reference}-${line.documentType}`,
      date: line.entryDate,
      type: line.documentType,
      reference: line.reference,
      status: "Posted",
      amount: Math.max(line.debit, line.credit),
      currencyCode: line.currencyCode,
      source: line.journalEntryId === null || line.journalEntryId === undefined ? "Commercial" : "Journal"
    }));

  return [...invoiceRows, ...paymentRows, ...adjustmentRows].sort(
    (first, second) => new Date(second.date).getTime() - new Date(first.date).getTime()
  );
}

function createSetupGaps(
  workspace: ClientWorkspace | null,
  activeContract: ClientContract | null,
  primaryDeployment: ClientDeployment | null
): string[] {
  if (workspace === null) {
    return [];
  }

  const gaps: string[] = [];

  if (workspace.client.status.toLowerCase() !== "active") {
    gaps.push("Activate client master record");
  }

  if (workspace.client.contacts.length === 0) {
    gaps.push("Add a billing or support contact");
  }

  if (workspace.accountingProfile === null) {
    gaps.push("Configure client accounting profile");
  }

  if (activeContract === null) {
    gaps.push("Create an active contract");
  }

  if (workspace.statement === null || workspace.statement.invoices.length === 0) {
    gaps.push("Create first invoice");
  }

  if (workspace.entitlement === null) {
    gaps.push("Issue entitlement after payment");
  }

  if (primaryDeployment === null) {
    gaps.push("Configure local server deployment");
  }

  if (workspace.cloudStatus === null) {
    gaps.push("Register cloud installation status");
  }

  return gaps;
}

function createNextRequiredAction({
  activeContract,
  invoiceDraft,
  messages,
  paidInvoices,
  primaryDeployment,
  receiptInvoice,
  workspace
}: {
  activeContract: ClientContract | null;
  invoiceDraft: InvoiceDraft | null;
  messages: CloudOutboxMessage[];
  paidInvoices: ClientStatementInvoice[];
  primaryDeployment: ClientDeployment | null;
  receiptInvoice: ClientStatementInvoice | null;
  workspace: ClientWorkspace | null;
}): NextRequiredAction {
  if (workspace === null) {
    return {
      label: "Select client",
      detail: "No client workspace is loaded.",
      tab: "overview",
      tone: "warning"
    };
  }

  if (workspace.client.status.toLowerCase() !== "active") {
    return {
      label: "Activate client",
      detail: `${workspace.client.displayName || workspace.client.legalName} is currently ${workspace.client.status}.`,
      tab: "setup",
      tone: "warning"
    };
  }

  if (workspace.client.contacts.length === 0) {
    return {
      label: "Add contact",
      detail: "Create a billing or support contact for this client.",
      tab: "setup",
      tone: "warning"
    };
  }

  if (workspace.accountingProfile === null) {
    return {
      label: "Save accounting profile",
      detail: "Set the receivable account and default currency before billing.",
      tab: "setup",
      tone: "warning"
    };
  }

  if (activeContract === null) {
    return {
      label: "Create contract",
      detail: "Create the commercial agreement for billing and access limits.",
      tab: "setup",
      tone: "warning"
    };
  }

  if (primaryDeployment === null) {
    return {
      label: "Save deployment",
      detail: "Create the local server installation profile for cloud communication.",
      tab: "setup",
      tone: "warning"
    };
  }

  const draftInvoice =
    invoiceDraft?.status.toLowerCase() === "draft"
      ? invoiceDraft
      : findDraftInvoice(workspace.statement);

  if (draftInvoice !== null) {
    return {
      label: "Issue invoice voucher",
      detail: `${draftInvoice.invoiceNumber} is drafted and ready to issue.`,
      tab: "billing",
      tone: "warning"
    };
  }

  if (workspace.statement === null || workspace.statement.invoices.length === 0) {
    return {
      label: "Draft invoice voucher",
      detail: "Create the first invoice voucher from the active contract.",
      tab: "billing",
      tone: "warning"
    };
  }

  if (receiptInvoice !== null) {
    return {
      label: "Record receipt",
      detail: `${receiptInvoice.invoiceNumber} has ${formatMoney(
        receiptInvoice.balanceDue,
        receiptInvoice.currencyCode
      )} due.`,
      tab: "payments",
      tone: "warning"
    };
  }

  if (paidInvoices.length > 0 && workspace.entitlement === null) {
    return {
      label: "Issue access renewal",
      detail: `${paidInvoices[0].invoiceNumber} is paid and can renew client access.`,
      tab: "access",
      tone: "warning"
    };
  }

  const pendingMessages = messages.filter((message) => message.status !== "Sent");
  const failedMessages = messages.filter((message) => message.status === "Failed");

  if (pendingMessages.length > 0) {
    return {
      label: "Send to Cloud",
      detail:
        failedMessages.length > 0
          ? `${failedMessages.length} client update${failedMessages.length === 1 ? "" : "s"} failed.`
          : `${pendingMessages.length} client update${pendingMessages.length === 1 ? "" : "s"} pending.`,
      tab: "cloud",
      tone: failedMessages.length > 0 ? "warning" : "ready"
    };
  }

  if (workspace.cloudStatus === null) {
    return {
      label: "Review cloud status",
      detail: "No cloud installation status is available for this client.",
      tab: "cloud",
      tone: "warning"
    };
  }

  if (workspace.cloudStatus.latestHeartbeat === null) {
    return {
      label: "Confirm local heartbeat",
      detail: "Cloud has no recent local server heartbeat for this installation.",
      tab: "cloud",
      tone: "warning"
    };
  }

  return {
    label: "Client current",
    detail: "Setup, billing, access, cloud updates, and local heartbeat are in place.",
    tab: "overview",
    tone: "done"
  };
}

function createClientProgressTrail({
  messages,
  vouchers,
  workspace
}: {
  messages: CloudOutboxMessage[];
  vouchers: VoucherRow[];
  workspace: ClientWorkspace | null;
}): ClientProgressTrail {
  if (workspace === null) {
    return {
      lastDone: null,
      recentEvents: []
    };
  }

  const events: ClientProgressEvent[] = [
    {
      key: `client-${workspace.client.clientId}`,
      label: "Client created",
      detail: workspace.client.displayName || workspace.client.legalName,
      occurredAtUtc: workspace.client.createdAtUtc
    }
  ];

  if (workspace.client.activatedAtUtc !== null && workspace.client.activatedAtUtc !== undefined) {
    events.push({
      key: `client-activated-${workspace.client.clientId}`,
      label: "Client activated",
      detail: workspace.client.displayName || workspace.client.legalName,
      occurredAtUtc: workspace.client.activatedAtUtc
    });
  }

  if (workspace.client.suspendedAtUtc !== null && workspace.client.suspendedAtUtc !== undefined) {
    events.push({
      key: `client-suspended-${workspace.client.clientId}`,
      label: "Client suspended",
      detail: workspace.client.displayName || workspace.client.legalName,
      occurredAtUtc: workspace.client.suspendedAtUtc
    });
  }

  workspace.client.contacts.forEach((contact) => {
    events.push({
      key: `contact-${contact.clientContactId}`,
      label: "Contact added",
      detail: `${contact.role} / ${contact.fullName}`,
      occurredAtUtc: contact.createdAtUtc
    });
  });

  workspace.client.supportNotes.forEach((note, index) => {
    events.push({
      key: `support-note-${note.createdAtUtc}-${index}`,
      label: "Support note added",
      detail: note.createdBy,
      occurredAtUtc: note.createdAtUtc
    });
  });

  workspace.portalInvitations.forEach((invitation) => {
    events.push({
      key: `portal-invitation-${invitation.invitationId}`,
      label: `Portal invite ${invitation.status}`,
      detail: `${invitation.fullName} / ${invitation.email}`,
      occurredAtUtc: invitation.invitedAtUtc
    });
  });

  if (workspace.accountingProfile !== null) {
    events.push({
      key: `profile-${workspace.accountingProfile.clientId}`,
      label: "Accounting profile saved",
      detail: workspace.accountingProfile.defaultCurrencyCode,
      occurredAtUtc: workspace.accountingProfile.updatedAtUtc
    });
  }

  workspace.contracts.forEach((contract) => {
    events.push({
      key: `contract-${contract.contractId}`,
      label: contract.status.toLowerCase() === "active" ? "Contract active" : "Contract created",
      detail: `${contract.contractNumber} / ${formatMoney(contract.recurringAmount, contract.currencyCode)}`,
      occurredAtUtc: contract.activatedAtUtc ?? contract.createdAtUtc
    });
  });

  workspace.deployments.forEach((deployment) => {
    events.push({
      key: `deployment-${deployment.clientDeploymentId}`,
      label: "Deployment saved",
      detail: `${deployment.displayName} / ${deployment.installationId}`,
      occurredAtUtc: deployment.updatedAtUtc
    });
  });

  vouchers.forEach((voucher) => {
    events.push({
      key: `voucher-${voucher.id}`,
      label: `${voucher.type} ${voucher.status}`,
      detail: `${voucher.reference} / ${formatMoney(voucher.amount, voucher.currencyCode)}`,
      occurredAtUtc: voucher.date
    });
  });

  if (workspace.entitlement !== null) {
    events.push({
      key: `entitlement-${workspace.entitlement.entitlementSnapshotId}`,
      label: "Access renewal issued",
      detail: `Paid until ${formatDate(workspace.entitlement.paidUntil)}`,
      occurredAtUtc: workspace.entitlement.issuedAtUtc
    });
  }

  messages
    .filter((message) => message.status === "Sent")
    .forEach((message) => {
      events.push({
        key: `cloud-${message.cloudOutboxMessageId}`,
        label: "Cloud update sent",
        detail: message.messageType,
        occurredAtUtc: message.sentAtUtc ?? message.occurredAtUtc
      });
    });

  if (workspace.cloudStatus?.latestHeartbeat !== null && workspace.cloudStatus?.latestHeartbeat !== undefined) {
    events.push({
      key: `heartbeat-${workspace.cloudStatus.latestHeartbeat.heartbeatId}`,
      label: "Local heartbeat received",
      detail: workspace.cloudStatus.latestHeartbeat.licenseStatus,
      occurredAtUtc: workspace.cloudStatus.latestHeartbeat.receivedAtUtc
    });
  }

  const recentEvents = events
    .filter((event) => event.occurredAtUtc !== null && event.occurredAtUtc.trim() !== "")
    .sort((first, second) => eventTime(second) - eventTime(first));

  return {
    lastDone: recentEvents[0] ?? null,
    recentEvents
  };
}

function createDefaultCreateClientForm(): CreateClientInput {
  return {
    code: "",
    legalName: "",
    displayName: ""
  };
}

function createDefaultUpdateClientForm(): UpdateClientInput {
  return {
    legalName: "",
    displayName: ""
  };
}

function toClientEditForm(client: ClientDetails): UpdateClientInput {
  return {
    legalName: client.legalName,
    displayName: client.displayName
  };
}

function mergeClientLookups(
  current: ClientLookup[],
  next: ClientLookup[]
): ClientLookup[] {
  const byClientId = new Map(current.map((client) => [client.clientId, client]));

  next.forEach((client) => byClientId.set(client.clientId, client));

  return [...byClientId.values()].sort((first, second) =>
    first.code.localeCompare(second.code, undefined, { numeric: true, sensitivity: "base" })
  );
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

function createDefaultContactForm(client?: ClientDetails | null): AddClientContactInput {
  return {
    role: "Billing",
    fullName: "",
    jobTitle: "",
    email: "",
    phone: "",
    isPrimary: client?.contacts.length === 0
  };
}

function createDefaultSupportNoteForm(createdBy = "SafarSuite Control Desk"): AddClientSupportNoteInput {
  return {
    text: "",
    createdBy
  };
}

function createDefaultAccountingProfileForm(
  client?: ClientDetails | null,
  contract?: ClientContract | null,
  receivableAccountId = ""
): ConfigureClientAccountingProfileInput {
  return {
    accountsReceivableAccountId: receivableAccountId,
    defaultCurrencyCode: contract?.currencyCode ?? "PKR",
    cloudCustomerId: client?.code ?? ""
  };
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

function isOptionalLimitValid(value: string): boolean {
  return value.trim() === "" || Number(value) >= 0;
}

function isUserLimitOrderValid(namedUsers: string, concurrentUsers: string): boolean {
  return namedUsers.trim() === ""
    || concurrentUsers.trim() === ""
    || Number(concurrentUsers) <= Number(namedUsers);
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
    allowedNamedUsers: "",
    allowedConcurrentUsers: "",
    approvalReason: "Commercial terms reviewed and approved in Control Desk.",
    moduleCodes: defaultContractModuleCodes(productModules),
    featureLimits: []
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
  const defaultModules =
    includedModules.length > 0
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

function createDefaultDeploymentForm(client?: ClientDetails | null): ConfigureClientDeploymentInput {
  const installationId = createDefaultInstallationId(client?.code);

  return {
    installationId,
    displayName: client === null || client === undefined ? "Main office" : `${client.displayName} main`,
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

function createDefaultInstallationId(clientCode: string | undefined): string {
  const suffix = clientCode?.trim().toLowerCase()
    .replace(/[^a-z0-9-]/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "");

  return `${suffix === "" || suffix === undefined ? "office" : suffix}-main`.slice(0, 160);
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

function createDefaultIssueInvoiceForm(
  date = toDateInputValue(new Date()),
  accountingProfile?: ClientAccountingProfile | null
): IssueInvoiceFormInput {
  return {
    postingDate: date,
    accountsReceivableAccountId: accountingProfile?.accountsReceivableAccountId ?? ""
  };
}

function createDefaultPaymentForm(
  input: {
    client?: ClientDetails | null;
    invoice?: ClientStatementInvoice | null;
    draft?: InvoiceDraft | null;
    accountingProfile?: ClientAccountingProfile | null;
    cashAccountId?: string;
  } = {}
): RecordInvoicePaymentInput {
  const today = new Date();
  const invoiceId = input.invoice?.invoiceId ?? input.draft?.invoiceId ?? "";
  const invoiceBalance = input.invoice?.balanceDue ?? input.draft?.balanceDue ?? 0;
  const invoiceCurrency =
    input.invoice?.currencyCode
    ?? input.draft?.currencyCode
    ?? input.accountingProfile?.defaultCurrencyCode
    ?? "PKR";

  return {
    invoiceId,
    method: "ManualCash",
    reference: defaultReceiptReference(input.client?.code, today),
    amount: invoiceBalance > 0 ? invoiceBalance.toFixed(2) : "0.00",
    currencyCode: invoiceCurrency,
    receivedOn: toDateInputValue(today),
    cashOrBankAccountId: input.cashAccountId ?? "",
    accountsReceivableAccountId: input.accountingProfile?.accountsReceivableAccountId ?? "",
    postingDate: toDateInputValue(today)
  };
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

async function optionalRequest<T>(request: () => Promise<T>, fallback: T): Promise<T>;
async function optionalRequest<T>(request: () => Promise<T>): Promise<T | null>;
async function optionalRequest<T>(request: () => Promise<T>, fallback: T | null = null) {
  try {
    return await request();
  } catch (caughtError) {
    if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
      return fallback;
    }

    throw caughtError;
  }
}

async function optionalNonCriticalRequest<T>(request: () => Promise<T>): Promise<T | null> {
  try {
    return await request();
  } catch {
    return null;
  }
}

function formatMoney(amount: number, currencyCode: string): string {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: currencyCode,
    currencyDisplay: "narrowSymbol"
  }).format(amount);
}

function createDefaultAccessEffectiveFrom(): string {
  const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000);
  tomorrow.setMinutes(0, 0, 0);

  return toLocalDateTimeInputValue(tomorrow);
}

function toLocalDateTimeInputValue(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, "0");

  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`;
}

function toUtcIsoString(value: string): string | null {
  if (value.trim() === "") {
    return null;
  }

  const parsed = new Date(value);

  return Number.isNaN(parsed.getTime()) || parsed.getTime() <= Date.now()
    ? null
    : parsed.toISOString();
}

function formatDate(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(date);
}

function formatDateTime(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(date);
}

function formatProgressTime(value: string | null): string {
  if (value === null || value.trim() === "") {
    return "-";
  }

  return value.includes("T") ? formatDateTime(value) : formatDate(value);
}

function eventTime(event: ClientProgressEvent): number {
  if (event.occurredAtUtc === null) {
    return 0;
  }

  const date = new Date(event.occurredAtUtc);

  return Number.isNaN(date.getTime()) ? 0 : date.getTime();
}

function formatError(caughtError: unknown, fallback: string): string {
  if (caughtError instanceof ApiError) {
    return caughtError.errors[0]?.message ?? caughtError.message;
  }

  if (caughtError instanceof Error) {
    return caughtError.message;
  }

  return fallback;
}
