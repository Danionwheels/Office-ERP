import {
  AlertCircle,
  ArrowRight,
  Banknote,
  CheckCircle2,
  FileText,
  KeyRound,
  LayoutDashboard,
  ReceiptText,
  UserRound,
  Users,
  type LucideIcon
} from "lucide-react";
import { useEffect, useState } from "react";
import { ApiError } from "../../../shared/api/apiError";
import {
  createChargeCode,
  createClientChargeRule,
  createLedgerAccount,
  generateInvoiceDraft,
  issueInvoice,
  listChargeCodes
} from "../../billing/api/billingApi";
import { ClientBillingSetupPanel } from "../../billing/components/ClientBillingSetupPanel";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRule,
  ClientChargeRuleFormInput,
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccountFormInput
} from "../../billing/types/billingTypes";
import {
  createClientContract,
  listClientContracts,
  replaceActiveClientContract,
  suspendClientContract
} from "../../contracts/api/contractApi";
import { ClientContractsPanel } from "../../contracts/components/ClientContractsPanel";
import type {
  ClientContract,
  ClientContractFormInput
} from "../../contracts/types/contractTypes";
import {
  getLatestEntitlementSnapshot,
  issueEntitlementFromPaidInvoiceDefaults
} from "../../entitlements/api/entitlementApi";
import { EntitlementSnapshotPanel } from "../../entitlements/components/EntitlementSnapshotPanel";
import type {
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "../../entitlements/types/entitlementTypes";
import { recordInvoicePayment } from "../../payments/api/paymentApi";
import { PaymentReceiptPanel } from "../../payments/components/PaymentReceiptPanel";
import type {
  RecordedInvoicePayment,
  RecordInvoicePaymentInput
} from "../../payments/types/paymentTypes";
import {
  activateClient,
  addClientContact,
  addClientSupportNote,
  configureClientAccountingProfile,
  createClient,
  getClient,
  getClientAccountingProfile,
  listClients,
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
  ClientDetails,
  ClientLookup,
  ConfigureClientAccountingProfileInput,
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

type DashboardModule =
  | "dashboard"
  | "clients"
  | "profile"
  | "contracts"
  | "billing"
  | "payments"
  | "entitlements";

export function ClientDeskPage() {
  const [clients, setClients] = useState<ClientLookup[]>([]);
  const [selectedClientId, setSelectedClientId] = useState("");
  const [selectedClient, setSelectedClient] = useState<ClientDetails | null>(null);
  const [accountingProfile, setAccountingProfile] = useState<ClientAccountingProfile | null>(null);
  const [accountingProfileMissing, setAccountingProfileMissing] = useState(false);
  const [contracts, setContracts] = useState<ClientContract[]>([]);
  const [chargeCodes, setChargeCodes] = useState<ChargeCodeLookup[]>([]);
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
  const [invoiceDraft, setInvoiceDraft] = useState<InvoiceDraft | null>(null);
  const [issuedInvoice, setIssuedInvoice] = useState<IssuedInvoice | null>(null);
  const [paymentForm, setPaymentForm] = useState<RecordInvoicePaymentInput>(
    createDefaultPaymentForm()
  );
  const [recordedPayment, setRecordedPayment] = useState<RecordedInvoicePayment | null>(null);
  const [latestEntitlementSnapshot, setLatestEntitlementSnapshot] =
    useState<EntitlementSnapshot | null>(null);
  const [latestEntitlementSnapshotMissing, setLatestEntitlementSnapshotMissing] = useState(false);
  const [issuedEntitlementSnapshot, setIssuedEntitlementSnapshot] =
    useState<IssuedEntitlementSnapshot | null>(null);
  const [activeDashboardModule, setActiveDashboardModule] =
    useState<DashboardModule>("dashboard");
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    void refreshClients();
    void refreshChargeCodes();
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

  async function loadClientList(nextSelectedClientId = selectedClientId) {
    const clientList = await listClients();
    setClients(clientList);

    if (clientList.length === 0) {
      setSelectedClientId("");
      setSelectedClient(null);
      setAccountingProfile(null);
      setAccountingProfileMissing(false);
      setContracts([]);
      resetBillingForms();
      return;
    }

    const selectedExists = clientList.some((client) => client.clientId === nextSelectedClientId);
    setSelectedClientId(selectedExists ? nextSelectedClientId : clientList[0].clientId);
  }

  async function loadClient(clientId: string) {
    await runClientAction(async () => {
      const [client, clientContracts] = await Promise.all([
        getClient(clientId),
        listClientContracts(clientId)
      ]);

      applyLoadedClient(client);
      setContracts(clientContracts);
      setContractForm(createDefaultContractForm(client.code));
      applyBillingDefaults(client, getActiveContract(clientContracts));
      await loadAccountingProfile(clientId);
      await loadLatestEntitlementSnapshot(clientId);
    });
  }

  async function loadAccountingProfile(clientId: string) {
    try {
      const profile = await getClientAccountingProfile(clientId);
      setAccountingProfile(profile);
      setAccountingProfileMissing(false);
      setAccountingProfileForm(toAccountingProfileForm(profile));
      setPaymentForm((current) => ({
        ...current,
        accountsReceivableAccountId: profile.accountsReceivableAccountId
      }));
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setAccountingProfile(null);
        setAccountingProfileMissing(true);
        setAccountingProfileForm((current) => ({
          ...current,
          accountsReceivableAccountId: ""
        }));
        return;
      }

      throw caughtError;
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
      setContracts(nextContracts);
      setContractForm(createDefaultContractForm(selectedClient.code));
      applyBillingContractDefaults(selectedClient, getActiveContract(nextContracts));
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
      setContracts(refreshedContracts);
      setContractForm(createDefaultContractForm(selectedClient.code));
      applyBillingContractDefaults(selectedClient, getActiveContract(refreshedContracts));
      setMessage(result.suspendedContract === null ? "Contract activated." : "Active contract replaced.");
    });
  }

  async function handleSuspendContract(contractId: string) {
    await runClientAction(async () => {
      const contract = await suspendClientContract(contractId);
      const nextContracts = sortContracts(
        contracts.map((item) => (item.contractId === contract.contractId ? contract : item))
      );
      setContracts(nextContracts);
      applyBillingContractDefaults(selectedClient, getActiveContract(nextContracts));
      setMessage("Contract suspended.");
    });
  }

  async function handleCreateReceivableAccount() {
    await runClientAction(async () => {
      const account = await createLedgerAccount(receivableAccountForm);
      setAccountingProfileForm((current) => ({
        ...current,
        accountsReceivableAccountId: account.ledgerAccountId
      }));
      setMessage("AR account created.");
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
      setAccountingProfile(profile);
      setAccountingProfileMissing(false);
      setPaymentForm((current) => ({
        ...current,
        accountsReceivableAccountId: profile.accountsReceivableAccountId
      }));
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
      setMessage("Charge rule added.");
    });
  }

  async function handleGenerateInvoiceDraft() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const draft = await generateInvoiceDraft(selectedClient.clientId, invoiceDraftForm);
      setInvoiceDraft(draft);
      setIssuedInvoice(null);
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
      setRecordedPayment(null);
      setMessage("Invoice draft generated.");
    });
  }

  async function handleIssueInvoice() {
    if (invoiceDraft === null) {
      return;
    }

    await runClientAction(async () => {
      const issued = await issueInvoice(invoiceDraft.invoiceId, issueInvoiceForm);
      setIssuedInvoice(issued);
      setInvoiceDraft({
        ...invoiceDraft,
        status: issued.invoiceStatus
      });
      setPaymentForm(createDefaultPaymentForm(
        selectedClient,
        { ...invoiceDraft, status: issued.invoiceStatus },
        accountingProfile,
        issueInvoiceForm.accountsReceivableAccountId
      ));
      setRecordedPayment(null);
      setMessage("Invoice issued.");
    });
  }

  async function handleRecordInvoicePayment() {
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
      setMessage("Payment recorded.");
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
    setEditForm({
      legalName: client.legalName,
      displayName: client.displayName
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
    setLatestChargeRule(null);
    setInvoiceDraft(null);
    setIssuedInvoice(null);
    setRecordedPayment(null);
    setLatestEntitlementSnapshot(null);
    setLatestEntitlementSnapshotMissing(false);
    setIssuedEntitlementSnapshot(null);
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
    setLatestChargeRule(null);
    setInvoiceDraft(null);
    setIssuedInvoice(null);
    setRecordedPayment(null);
    setLatestEntitlementSnapshot(null);
    setLatestEntitlementSnapshotMissing(false);
    setIssuedEntitlementSnapshot(null);
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
    setInvoiceDraft(null);
    setIssuedInvoice(null);
    setRecordedPayment(null);
    setIssuedEntitlementSnapshot(null);
  }

  const activeContract = getActiveContract(contracts);
  const dashboardMetrics = getDashboardMetrics({
    activeContract,
    accountingProfile,
    invoiceDraft,
    recordedPayment,
    issuedEntitlementSnapshot,
    latestEntitlementSnapshot
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
                isBusy={isBusy}
                onEditChange={setEditForm}
                onContactChange={setContactForm}
                onNoteChange={setNoteForm}
                onSave={handleUpdateClient}
                onActivate={handleActivateClient}
                onSuspend={handleSuspendClient}
                onAddContact={handleAddContact}
                onAddNote={handleAddNote}
              />
            )}

            {activeDashboardModule === "contracts" && (
              <ClientContractsPanel
                contracts={contracts}
                value={contractForm}
                isBusy={isBusy || selectedClient === null}
                onChange={setContractForm}
                onCreate={handleCreateContract}
                onReplaceActive={handleReplaceActiveContract}
                onSuspend={handleSuspendContract}
              />
            )}

            {activeDashboardModule === "billing" && (
              <ClientBillingSetupPanel
                client={selectedClient}
                contracts={contracts}
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
              />
            )}

            {activeDashboardModule === "payments" && (
              <PaymentReceiptPanel
                invoiceDraft={invoiceDraft}
                issuedInvoice={issuedInvoice}
                accountingProfile={accountingProfile}
                cashAccountValue={cashAccountForm}
                paymentValue={paymentForm}
                recordedPayment={recordedPayment}
                isBusy={isBusy || selectedClient === null}
                onCashAccountChange={setCashAccountForm}
                onPaymentChange={setPaymentForm}
                onCreateCashAccount={handleCreateCashAccount}
                onRecordPayment={handleRecordInvoicePayment}
              />
            )}

            {activeDashboardModule === "entitlements" && (
              <EntitlementSnapshotPanel
                invoiceDraft={invoiceDraft}
                recordedPayment={recordedPayment}
                latestSnapshot={latestEntitlementSnapshot}
                latestSnapshotMissing={latestEntitlementSnapshotMissing}
                issuedSnapshot={issuedEntitlementSnapshot}
                isBusy={isBusy || selectedClient === null}
                onIssueFromPaidInvoice={handleIssueEntitlementSnapshot}
                onRefreshLatest={handleRefreshLatestEntitlementSnapshot}
              />
            )}
          </div>
        </section>
      </main>
    </div>
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

type DashboardMetricInput = {
  activeContract: ClientContract | null;
  accountingProfile: ClientAccountingProfile | null;
  invoiceDraft: InvoiceDraft | null;
  recordedPayment: RecordedInvoicePayment | null;
  issuedEntitlementSnapshot: IssuedEntitlementSnapshot | null;
  latestEntitlementSnapshot: EntitlementSnapshot | null;
};

function getDashboardMetrics({
  activeContract,
  accountingProfile,
  invoiceDraft,
  recordedPayment,
  issuedEntitlementSnapshot,
  latestEntitlementSnapshot
}: DashboardMetricInput): DashboardMetric[] {
  const entitlementSnapshot = issuedEntitlementSnapshot ?? latestEntitlementSnapshot;

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
      value: accountingProfile === null ? "Not linked" : accountingProfile.defaultCurrencyCode,
      summary: "Ledger profile and charge setup",
      tone: accountingProfile === null ? "warning" : "ready",
      Icon: Banknote,
      module: "billing"
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
      module: "billing",
      label: "Billing",
      summary: accountingMetric.tone === "warning" ? accountingMetric.value : invoiceMetric.value,
      description: "Accounting profile, charge rules, invoice drafts, and invoice issue.",
      tone: accountingMetric.tone === "warning" ? accountingMetric.tone : invoiceMetric.tone,
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
    code: defaultCode("AR", client?.code),
    name: client === undefined ? "Accounts receivable" : `${client.displayName} receivable`,
    type: "Asset",
    normalBalance: "Debit",
    parentAccountId: "",
    isPostingAccount: true
  };
}

function createDefaultRevenueAccountForm(client?: ClientDetails): LedgerAccountFormInput {
  return {
    code: defaultCode("REV", client?.code),
    name: client === undefined ? "Subscription revenue" : `${client.displayName} revenue`,
    type: "Revenue",
    normalBalance: "Credit",
    parentAccountId: "",
    isPostingAccount: true
  };
}

function createDefaultCashAccountForm(client?: ClientDetails): LedgerAccountFormInput {
  return {
    code: defaultCode("BANK", client?.code),
    name: client === undefined ? "Cash or bank" : `${client.displayName} cash/bank`,
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
    descriptionOverride: "",
    unitPriceAmount: contract?.recurringAmount.toFixed(2) ?? "0.00",
    currencyCode: contract?.currencyCode ?? "PKR",
    quantity: "1",
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

function toAccountingProfileForm(
  profile: ClientAccountingProfile
): ConfigureClientAccountingProfileInput {
  return {
    accountsReceivableAccountId: profile.accountsReceivableAccountId,
    defaultCurrencyCode: profile.defaultCurrencyCode,
    cloudCustomerId: profile.cloudCustomerId ?? ""
  };
}

function createDefaultContractForm(clientCode = ""): ClientContractFormInput {
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
    moduleCodes: "CONTROL_DESK"
  };
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

function defaultCode(prefix: string, clientCode: string | undefined): string {
  const suffix = clientCode?.trim() === "" || clientCode === undefined
    ? "NEW"
    : clientCode.trim().toUpperCase().replace(/[^A-Z0-9-]/g, "");

  return `${prefix}-${suffix}`.slice(0, 32);
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
