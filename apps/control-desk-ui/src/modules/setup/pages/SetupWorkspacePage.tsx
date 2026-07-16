import {
  AlertCircle,
  ArrowRight,
  BookOpen,
  Building2,
  CheckCircle2,
  CircleDot,
  Edit3,
  GitBranch,
  Landmark,
  ListTree,
  Package,
  PauseCircle,
  Plus,
  RefreshCw,
  Save,
  Search,
  ServerCog,
  SlidersHorizontal,
  Trash2,
  Upload,
  Users
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { ApiError } from "../../../shared/api/apiError";
import {
  configureAccountCodeRange,
  getAccountingControlSettings,
  getAccountCodeRangeValidation,
  getOpeningBalanceProfile,
  listAccountCodeRanges,
  listLedgerAccounts,
  listVoucherNumberingRules
} from "../../accounting/api/accountingApi";
import { ChartOfAccountsRangePanel } from "../../accounting/components/shared/ChartOfAccountsRangePanel";
import { accountingCompanyCode } from "../../accounting/constants/accountingConstants";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  AccountCodeRangeValidation,
  AccountingControlSettings,
  LedgerAccountSummary,
  OpeningBalanceProfile,
  VoucherNumberingRule
} from "../../accounting/types/accountingTypes";
import {
  emptyAccountCodeRangeForm,
  sortAccountCodeRanges,
  toAccountCodeRangeForm
} from "../../accounting/utils/accountingForms";
import { getLegacyAccountLevel } from "../../accounting/utils/chartOfAccountsModel";
import {
  buildRangeUsageByRole,
  buildRangeValidationIssueMap,
  compareLedgerAccounts,
  getRangeSetupFacts,
  getRangeValidationIssueGroups,
  getSelectedRangeFacts
} from "../../accounting/utils/chartOfAccountsWorkspaceModel";
import {
  activateClient,
  createClient,
  listClientPage,
  suspendClient,
  updateClient
} from "../../clients/api/clientApi";
import type {
  ClientDirectoryPage,
  ClientDirectorySummary,
  ClientLookup,
  CreateClientInput,
  UpdateClientInput
} from "../../clients/types/clientTypes";
import {
  ProductModuleCatalogAdminPanel,
  listProductAccessCatalog,
  listProductCatalogRevisions,
  listProductModules,
  publishProductCatalogRevision,
  saveProductAccessCatalog,
  type ProductAccessCatalog,
  type ProductAccessKind,
  type ProductModule,
  type ProductModuleGroup,
  type ProductResource
} from "../../contracts";

type SetupWorkspacePageProps = {
  onOpenLegacyDesk: () => void;
};

type SetupSnapshot = {
  clients: ClientLookup[];
  clientSummary: ClientDirectorySummary;
  productModules: ProductModule[];
  productAccessCatalog: ProductAccessCatalog | null;
  productCatalogRevisions: ProductAccessCatalog[];
  ledgerAccounts: LedgerAccountSummary[];
  accountRanges: AccountCodeRange[];
  rangeValidation: AccountCodeRangeValidation | null;
  accountingControls: AccountingControlSettings | null;
  openingBalanceProfile: OpeningBalanceProfile | null;
  voucherRules: VoucherNumberingRule[];
};

const emptyClientSummary: ClientDirectorySummary = {
  totalCount: 0,
  draftCount: 0,
  activeCount: 0,
  suspendedCount: 0,
  archivedCount: 0
};

const emptyClientPage: ClientDirectoryPage = {
  clients: [],
  pageSize: 50,
  hasMore: false,
  nextCursor: null,
  filteredCount: 0,
  summary: emptyClientSummary
};

type SetupLoadIssue = {
  label: string;
  message: string;
};

type SetupMetric = {
  label: string;
  value: string;
  detail: string;
  tone: "ready" | "attention" | "quiet";
};

type SetupModelKey =
  | "clients"
  | "product-catalog"
  | "account-ranges"
  | "ledger-accounts"
  | "deployment-defaults";

type SetupModelDefinition = {
  key: SetupModelKey;
  label: string;
  eyebrow: string;
  description: string;
  Icon: LucideIcon;
};

type ProductModuleGroupFormInput = {
  groupId: string;
  displayName: string;
  accessKind: ProductAccessKind;
  moduleCodes: string;
};

type ProductResourceFormInput = {
  resourceId: string;
  displayName: string;
  accessKind: ProductAccessKind;
  requiredGroupIds: string;
  requiredModuleCodes: string;
};

type DeploymentDefaultStatus = "Ready" | "Partial" | "Planned";

type DeploymentDefaultItem = {
  key: string;
  label: string;
  value: string;
  detail: string;
  owner: string;
  consumedBy: string;
  source: string;
  status: DeploymentDefaultStatus;
  Icon: LucideIcon;
};

const emptySnapshot: SetupSnapshot = {
  clients: [],
  clientSummary: emptyClientSummary,
  productModules: [],
  productAccessCatalog: null,
  productCatalogRevisions: [],
  ledgerAccounts: [],
  accountRanges: [],
  rangeValidation: null,
  accountingControls: null,
  openingBalanceProfile: null,
  voucherRules: []
};

const emptyClientForm: CreateClientInput = {
  code: "",
  legalName: "",
  displayName: ""
};

const emptyClientEditForm: UpdateClientInput = {
  legalName: "",
  displayName: ""
};

const emptyProductGroupForm: ProductModuleGroupFormInput = {
  groupId: "",
  displayName: "",
  accessKind: "PaidModule",
  moduleCodes: ""
};

const emptyProductResourceForm: ProductResourceFormInput = {
  resourceId: "",
  displayName: "",
  accessKind: "PaidModule",
  requiredGroupIds: "",
  requiredModuleCodes: ""
};

const setupModelDefinitions: SetupModelDefinition[] = [
  {
    key: "clients",
    label: "Clients",
    eyebrow: "Customer setup",
    description: "Create and maintain the client records that the rest of the desk consumes.",
    Icon: Building2
  },
  {
    key: "product-catalog",
    label: "Product Catalog",
    eyebrow: "Commercial setup",
    description: "Review modules, billing defaults, access groups, and product resources.",
    Icon: Package
  },
  {
    key: "account-ranges",
    label: "COA Ranges",
    eyebrow: "Accounting setup",
    description: "Control account code ranges before ledger accounts and posting work use them.",
    Icon: Landmark
  },
  {
    key: "ledger-accounts",
    label: "Ledger Accounts",
    eyebrow: "Accounting setup",
    description: "Review the chart accounts that posting, reports, and controls depend on.",
    Icon: BookOpen
  },
  {
    key: "deployment-defaults",
    label: "Deployment Defaults",
    eyebrow: "Runtime setup",
    description: "Keep setup defaults that feed cloud deployment, access, and support flows.",
    Icon: ServerCog
  }
];

export function SetupWorkspacePage({ onOpenLegacyDesk }: SetupWorkspacePageProps) {
  const [snapshot, setSnapshot] = useState<SetupSnapshot>(emptySnapshot);
  const [issues, setIssues] = useState<SetupLoadIssue[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [activeModel, setActiveModel] = useState<SetupModelKey>("clients");
  const [clientForm, setClientForm] = useState<CreateClientInput>(emptyClientForm);
  const [isCreatingClient, setIsCreatingClient] = useState(false);
  const [clientCreateMessage, setClientCreateMessage] = useState("");
  const [clientCreateError, setClientCreateError] = useState("");
  const [selectedClientId, setSelectedClientId] = useState("");
  const [clientSearch, setClientSearch] = useState("");
  const [clientFilteredCount, setClientFilteredCount] = useState(0);
  const [clientNextCursor, setClientNextCursor] = useState<string | null>(null);
  const [isLoadingMoreClients, setIsLoadingMoreClients] = useState(false);
  const [isLoadingClientDirectory, setIsLoadingClientDirectory] = useState(false);
  const [clientEditForm, setClientEditForm] =
    useState<UpdateClientInput>(emptyClientEditForm);
  const [isSavingClient, setIsSavingClient] = useState(false);
  const [clientEditMessage, setClientEditMessage] = useState("");
  const [clientEditError, setClientEditError] = useState("");
  const [isSavingProductCatalog, setIsSavingProductCatalog] = useState(false);
  const [productCatalogMessage, setProductCatalogMessage] = useState("");
  const [productCatalogError, setProductCatalogError] = useState("");
  const [selectedRangeRole, setSelectedRangeRole] = useState("");
  const [rangeForm, setRangeForm] =
    useState<AccountCodeRangeFormInput>(emptyAccountCodeRangeForm);
  const [isSavingRange, setIsSavingRange] = useState(false);
  const [rangeSaveMessage, setRangeSaveMessage] = useState("");
  const [rangeSaveError, setRangeSaveError] = useState("");

  const refresh = useCallback(async () => {
    setIsLoading(true);

    const [
      clientsResult,
      modulesResult,
      catalogResult,
      catalogRevisionsResult,
      accountsResult,
      rangesResult,
      validationResult,
      controlsResult,
      openingProfileResult,
      voucherRulesResult
    ] = await Promise.allSettled([
      listClientPage({ sort: "code", direction: "asc", take: 50 }),
      listProductModules(),
      listProductAccessCatalog(),
      listProductCatalogRevisions(),
      listLedgerAccounts({
        companyCode: accountingCompanyCode,
        search: "",
        type: "",
        status: "",
        posting: "",
        role: "",
        viewMode: "",
        level: ""
      }),
      listAccountCodeRanges(accountingCompanyCode),
      getAccountCodeRangeValidation(accountingCompanyCode),
      getAccountingControlSettings(accountingCompanyCode),
      getOpeningBalanceProfile(accountingCompanyCode),
      listVoucherNumberingRules(accountingCompanyCode)
    ]);

    const nextRanges = sortAccountCodeRanges(valueOrDefault(rangesResult, []));

    const clientPage = valueOrDefault(clientsResult, emptyClientPage);

    setSnapshot({
      clients: clientPage.clients,
      clientSummary: clientPage.summary,
      productModules: valueOrDefault(modulesResult, []),
      productAccessCatalog: valueOrDefault(catalogResult, null),
      productCatalogRevisions: valueOrDefault(catalogRevisionsResult, []),
      ledgerAccounts: valueOrDefault(accountsResult, []),
      accountRanges: nextRanges,
      rangeValidation: valueOrDefault(validationResult, null),
      accountingControls: valueOrDefault(controlsResult, null),
      openingBalanceProfile: valueOrDefault(openingProfileResult, null),
      voucherRules: valueOrDefault(voucherRulesResult, [])
    });
    setClientFilteredCount(clientPage.filteredCount);
    setClientNextCursor(clientPage.nextCursor ?? null);

    setIssues([
      issueOrNull("Clients", clientsResult),
      issueOrNull("Product modules", modulesResult),
      issueOrNull("Product access catalog", catalogResult),
      issueOrNull("Product catalog history", catalogRevisionsResult),
      issueOrNull("Ledger accounts", accountsResult),
      issueOrNull("Account ranges", rangesResult),
      issueOrNull("Range validation", validationResult),
      issueOrNull("Accounting controls", controlsResult),
      issueOrNull("Opening balance profile", openingProfileResult),
      issueOrNull("Voucher numbering", voucherRulesResult)
    ].filter((issue): issue is SetupLoadIssue => issue !== null));

    setIsLoading(false);
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (snapshot.clients.length === 0) {
      if (selectedClientId !== "") {
        setSelectedClientId("");
        setClientEditForm(emptyClientEditForm);
      }
      return;
    }

    const selectedClient = snapshot.clients.find((client) => client.clientId === selectedClientId);

    if (selectedClient === undefined) {
      const firstClient = snapshot.clients[0];
      setSelectedClientId(firstClient.clientId);
      setClientEditForm(toClientEditForm(firstClient));
      return;
    }

    setClientEditForm(toClientEditForm(selectedClient));
  }, [selectedClientId, snapshot.clients]);

  useEffect(() => {
    if (snapshot.accountRanges.length === 0) {
      if (selectedRangeRole !== "") {
        setSelectedRangeRole("");
        setRangeForm(emptyAccountCodeRangeForm);
      }
      return;
    }

    const selectedRange = snapshot.accountRanges.find((range) => range.role === selectedRangeRole);

    if (selectedRange === undefined) {
      const firstRange = snapshot.accountRanges[0];
      setSelectedRangeRole(firstRange.role);
      setRangeForm(toAccountCodeRangeForm(firstRange));
    }
  }, [selectedRangeRole, snapshot.accountRanges]);

  const model = useMemo(() => buildSetupModel(snapshot), [snapshot]);
  const activeModelDefinition = getSetupModelDefinition(activeModel);

  async function loadSetupClients(
    search: string,
    append = false,
    preferredClient?: ClientLookup
  ) {
    if (append) {
      setIsLoadingMoreClients(true);
    } else {
      setIsLoadingClientDirectory(true);
    }

    setClientEditError("");

    try {
      const page = await listClientPage({
        search,
        sort: "code",
        direction: "asc",
        take: 50,
        cursor: append ? clientNextCursor ?? undefined : undefined
      });
      let nextClients = append
        ? page.clients.reduce(upsertClient, snapshot.clients)
        : page.clients;
      const pinnedClient = preferredClient
        ?? snapshot.clients.find((client) => client.clientId === selectedClientId);

      if (pinnedClient !== undefined) {
        nextClients = upsertClient(nextClients, pinnedClient);
      }

      setSnapshot((current) => ({
        ...current,
        clients: sortClients(nextClients),
        clientSummary: page.summary
      }));
      setClientFilteredCount(page.filteredCount);
      setClientNextCursor(page.nextCursor ?? null);

      if (preferredClient !== undefined) {
        setSelectedClientId(preferredClient.clientId);
      }
    } catch (caughtError) {
      setClientEditError(formatError(caughtError));
    } finally {
      setIsLoadingClientDirectory(false);
      setIsLoadingMoreClients(false);
    }
  }

  async function handleSearchClients(search: string) {
    const normalizedSearch = search.trim();
    setClientSearch(normalizedSearch);
    await loadSetupClients(normalizedSearch);
  }

  async function handleCreateClient(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsCreatingClient(true);
    setClientCreateError("");
    setClientCreateMessage("");

    try {
      const createdClient = await createClient(clientForm);
      setClientSearch("");
      await loadSetupClients("", false, createdClient);
      setSelectedClientId(createdClient.clientId);
      setClientEditForm(toClientEditForm(createdClient));
      setClientForm(emptyClientForm);
      setClientCreateMessage(`Client ${createdClient.code} created.`);
    } catch (caughtError) {
      setClientCreateError(formatError(caughtError));
    } finally {
      setIsCreatingClient(false);
    }
  }

  function handleSelectClient(client: ClientLookup) {
    setSelectedClientId(client.clientId);
    setClientEditForm(toClientEditForm(client));
    setClientEditError("");
    setClientEditMessage("");
  }

  async function handleSaveClient() {
    if (selectedClientId === "") {
      return;
    }

    setIsSavingClient(true);
    setClientEditError("");
    setClientEditMessage("");

    try {
      const updatedClient = await updateClient(selectedClientId, clientEditForm);
      setSnapshot((current) => ({
        ...current,
        clients: sortClients(upsertClient(current.clients, updatedClient))
      }));
      setSelectedClientId(updatedClient.clientId);
      setClientEditForm(toClientEditForm(updatedClient));
      setClientEditMessage(`${updatedClient.code} saved.`);
    } catch (caughtError) {
      setClientEditError(formatError(caughtError));
    } finally {
      setIsSavingClient(false);
    }
  }

  async function handleSetClientStatus(nextStatus: "active" | "suspended") {
    if (selectedClientId === "") {
      return;
    }

    setIsSavingClient(true);
    setClientEditError("");
    setClientEditMessage("");

    try {
      const previousStatus = snapshot.clients.find(
        (client) => client.clientId === selectedClientId
      )?.status;
      const updatedClient = nextStatus === "active"
        ? await activateClient(selectedClientId)
        : await suspendClient(selectedClientId);
      setSnapshot((current) => ({
        ...current,
        clients: sortClients(upsertClient(current.clients, updatedClient)),
        clientSummary: updateClientStatusSummary(
          current.clientSummary,
          previousStatus,
          updatedClient.status
        )
      }));
      setSelectedClientId(updatedClient.clientId);
      setClientEditForm(toClientEditForm(updatedClient));
      setClientEditMessage(`${updatedClient.code} ${updatedClient.status.toLowerCase()}.`);
    } catch (caughtError) {
      setClientEditError(formatError(caughtError));
    } finally {
      setIsSavingClient(false);
    }
  }

  async function handleRefreshProductCatalog() {
    setIsSavingProductCatalog(true);
    setProductCatalogError("");
    setProductCatalogMessage("");

    try {
      const [productModules, productAccessCatalog, productCatalogRevisions] = await Promise.all([
        listProductModules(),
        listProductAccessCatalog(),
        listProductCatalogRevisions()
      ]);
      setSnapshot((current) => ({
        ...current,
        productModules,
        productAccessCatalog,
        productCatalogRevisions
      }));
      setProductCatalogMessage("Product catalog refreshed.");
    } catch (caughtError) {
      setProductCatalogError(formatError(caughtError));
    } finally {
      setIsSavingProductCatalog(false);
    }
  }

  async function handleSaveProductCatalog(
    catalog: ProductAccessCatalog,
    requestedBy: string
  ) {
    setIsSavingProductCatalog(true);
    setProductCatalogError("");
    setProductCatalogMessage("");

    try {
      const savedCatalog = await saveProductAccessCatalog(catalog, requestedBy);
      setSnapshot((current) => ({
        ...current,
        productAccessCatalog: savedCatalog
      }));
      setProductCatalogMessage("Product catalog draft saved. Published behavior is unchanged.");
      return true;
    } catch (caughtError) {
      setProductCatalogError(formatError(caughtError));
      return false;
    } finally {
      setIsSavingProductCatalog(false);
    }
  }

  async function handlePublishProductCatalog(requestedBy: string) {
    setIsSavingProductCatalog(true);
    setProductCatalogError("");
    setProductCatalogMessage("");

    try {
      const published = await publishProductCatalogRevision(requestedBy);
      const [productModules, productCatalogRevisions] = await Promise.all([
        listProductModules(),
        listProductCatalogRevisions()
      ]);
      setSnapshot((current) => ({
        ...current,
        productModules,
        productAccessCatalog: published,
        productCatalogRevisions
      }));
      setProductCatalogMessage(`Product catalog revision #${published.revisionNumber ?? "-"} published.`);
    } catch (caughtError) {
      setProductCatalogError(formatError(caughtError));
    } finally {
      setIsSavingProductCatalog(false);
    }
  }

  function handleSelectRange(range: AccountCodeRange) {
    setSelectedRangeRole(range.role);
    setRangeForm(toAccountCodeRangeForm(range));
    setRangeSaveError("");
    setRangeSaveMessage("");
  }

  async function handleSaveRange() {
    if (selectedRangeRole === "") {
      return;
    }

    setIsSavingRange(true);
    setRangeSaveError("");
    setRangeSaveMessage("");

    try {
      const savedRange = await configureAccountCodeRange(
        accountingCompanyCode,
        selectedRangeRole,
        rangeForm
      );
      const [accounts, ranges, validation] = await Promise.all([
        listLedgerAccounts({
          companyCode: accountingCompanyCode,
          search: "",
          type: "",
          status: "",
          posting: "",
          role: "",
          viewMode: "",
          level: ""
        }),
        listAccountCodeRanges(accountingCompanyCode),
        getAccountCodeRangeValidation(accountingCompanyCode)
      ]);
      const sortedRanges = sortAccountCodeRanges(ranges);
      const selectedRange =
        sortedRanges.find((range) => range.role === savedRange.role) ?? savedRange;

      setSnapshot((current) => ({
        ...current,
        ledgerAccounts: accounts,
        accountRanges: sortedRanges,
        rangeValidation: validation
      }));
      setSelectedRangeRole(selectedRange.role);
      setRangeForm(toAccountCodeRangeForm(selectedRange));
      setRangeSaveMessage(`${selectedRange.displayName} saved.`);
    } catch (caughtError) {
      setRangeSaveError(formatError(caughtError));
    } finally {
      setIsSavingRange(false);
    }
  }

  return (
    <section className="setup-workspace">
      <div className="setup-command-strip">
        <div>
          <span>Setup foundation</span>
          <h2>{activeModelDefinition.label}</h2>
          <p>{activeModelDefinition.description}</p>
        </div>
        <div className="setup-command-actions">
          <span className="setup-company-pill">{accountingCompanyCode}</span>
          <button
            className="icon-button"
            disabled={isLoading}
            onClick={refresh}
            title="Refresh setup snapshot"
            type="button"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
          <button
            className="icon-button primary"
            onClick={onOpenLegacyDesk}
            type="button"
          >
            <SlidersHorizontal size={16} />
            Open current editors
          </button>
        </div>
      </div>

      {issues.length > 0 && (
        <div className="setup-load-alert" role="alert">
          <AlertCircle size={17} />
          <div>
            <strong>Partial setup snapshot</strong>
            <span>{issues.map((issue) => issue.label).join(", ")}</span>
          </div>
        </div>
      )}

      <div className="setup-focus-shell">
        <nav className="setup-model-rail" aria-label="Setup models">
          {setupModelDefinitions.map((definition) => (
            <button
              className={definition.key === activeModel ? "active" : ""}
              key={definition.key}
              onClick={() => setActiveModel(definition.key)}
              title={definition.label}
              type="button"
            >
              <definition.Icon size={18} />
              <span>
                <strong>{definition.label}</strong>
                <small>{getSetupModelStatus(definition.key, snapshot, model)}</small>
              </span>
            </button>
          ))}
        </nav>

        <div className="setup-focus-surface">
          {activeModel === "clients" && (
            <ClientsSetupModel
              activeClients={model.activeClients}
              clientCreateError={clientCreateError}
              clientCreateMessage={clientCreateMessage}
              clientEditError={clientEditError}
              clientEditMessage={clientEditMessage}
              clientEditValue={clientEditForm}
              clientForm={clientForm}
              clients={snapshot.clients}
              filteredCount={clientFilteredCount}
              hasMoreClients={clientNextCursor !== null}
              isCreatingClient={isCreatingClient}
              isLoadingClients={isLoadingClientDirectory}
              isLoadingMoreClients={isLoadingMoreClients}
              isSavingClient={isSavingClient}
              onActivateClient={() => handleSetClientStatus("active")}
              onClientFormChange={setClientForm}
              onCreateClient={handleCreateClient}
              onLoadMoreClients={() => loadSetupClients(clientSearch, true)}
              onSaveClient={handleSaveClient}
              onSearchClients={handleSearchClients}
              onSelectClient={handleSelectClient}
              onSuspendClient={() => handleSetClientStatus("suspended")}
              onClientEditChange={setClientEditForm}
              selectedClientId={selectedClientId}
              totalClients={snapshot.clientSummary.totalCount}
            />
          )}

          {activeModel === "product-catalog" && (
            <ProductCatalogSetupModel
              catalog={snapshot.productAccessCatalog}
              catalogRevisions={snapshot.productCatalogRevisions}
              catalogError={productCatalogError}
              catalogMessage={productCatalogMessage}
              isSavingCatalog={isSavingProductCatalog || isLoading}
              onRefreshCatalog={handleRefreshProductCatalog}
              onSaveCatalog={handleSaveProductCatalog}
              onPublishCatalog={handlePublishProductCatalog}
              productModules={snapshot.productModules}
            />
          )}

          {activeModel === "account-ranges" && (
            <AccountRangesSetupModel
              accountRanges={snapshot.accountRanges}
              isSavingRange={isSavingRange || isLoading}
              ledgerAccounts={snapshot.ledgerAccounts}
              onRangeChange={setRangeForm}
              onRangeSelect={handleSelectRange}
              onSaveRange={handleSaveRange}
              rangeSaveError={rangeSaveError}
              rangeSaveMessage={rangeSaveMessage}
              rangeValue={rangeForm}
              rangeValidation={snapshot.rangeValidation}
              selectedRangeRole={selectedRangeRole}
            />
          )}

          {activeModel === "ledger-accounts" && (
            <LedgerAccountsSetupModel
              accountRanges={snapshot.accountRanges}
              ledgerAccounts={snapshot.ledgerAccounts}
            />
          )}

          {activeModel === "deployment-defaults" && (
            <DeploymentDefaultsSetupModel
              flow={model.flow}
              runtimeItems={model.runtimeItems}
            />
          )}
        </div>
      </div>
    </section>
  );
}

function ClientsSetupModel({
  activeClients,
  clientCreateError,
  clientCreateMessage,
  clientEditError,
  clientEditMessage,
  clientEditValue,
  clientForm,
  clients,
  filteredCount,
  hasMoreClients,
  isCreatingClient,
  isLoadingClients,
  isLoadingMoreClients,
  isSavingClient,
  onActivateClient,
  onClientEditChange,
  onClientFormChange,
  onCreateClient,
  onLoadMoreClients,
  onSaveClient,
  onSearchClients,
  onSelectClient,
  onSuspendClient,
  selectedClientId,
  totalClients
}: {
  activeClients: number;
  clientCreateError: string;
  clientCreateMessage: string;
  clientEditError: string;
  clientEditMessage: string;
  clientEditValue: UpdateClientInput;
  clientForm: CreateClientInput;
  clients: ClientLookup[];
  filteredCount: number;
  hasMoreClients: boolean;
  isCreatingClient: boolean;
  isLoadingClients: boolean;
  isLoadingMoreClients: boolean;
  isSavingClient: boolean;
  onActivateClient: () => Promise<void>;
  onClientEditChange: (input: UpdateClientInput) => void;
  onClientFormChange: (input: CreateClientInput) => void;
  onCreateClient: (event: FormEvent<HTMLFormElement>) => void;
  onLoadMoreClients: () => Promise<void>;
  onSaveClient: () => Promise<void>;
  onSearchClients: (search: string) => Promise<void>;
  onSelectClient: (client: ClientLookup) => void;
  onSuspendClient: () => Promise<void>;
  selectedClientId: string;
  totalClients: number;
}) {
  const [searchText, setSearchText] = useState("");
  const selectedClient = clients.find((client) => client.clientId === selectedClientId) ?? null;
  const inactiveClients = totalClients - activeClients;
  const selectedIsActive =
    selectedClient !== null && equalsIgnoreCase(selectedClient.status, "Active");
  const selectedIsSuspended =
    selectedClient !== null && equalsIgnoreCase(selectedClient.status, "Suspended");

  async function handleSaveClient(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSaveClient();
  }

  return (
    <section className="setup-focus-pane">
      <SetupFocusHeading
        Icon={Building2}
        eyebrow="Customer setup"
        title="Clients"
        description="One place for the client records reused by contracts, billing, deployment, and support."
      />

      <div className="setup-focus-stats">
        <SetupFact label="Total clients" value={totalClients.toString()} />
        <SetupFact label="Active" value={activeClients.toString()} />
        <SetupFact label="Inactive" value={inactiveClients.toString()} />
        <SetupFact
          label="Selected"
          value={selectedClient?.code ?? "-"}
        />
      </div>

      <div className="setup-client-master-workbench">
        <section className="setup-focus-panel setup-client-register-panel">
          <div className="setup-panel-heading">
            <span>Client register</span>
            <strong>{filteredCount} matches / {clients.length} available</strong>
          </div>

          <div className="setup-client-register-search">
            <label className="client-search">
              <Search size={16} />
              <input
                maxLength={128}
                placeholder="Code, name, or status"
                value={searchText}
                onChange={(event) => setSearchText(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    void onSearchClients(searchText);
                  }
                }}
              />
            </label>
            <button
              className="icon-button"
              disabled={isLoadingClients}
              onClick={() => onSearchClients(searchText)}
              title="Search clients"
              type="button"
            >
              <Search size={16} />
            </button>
          </div>

          {clients.length === 0 ? (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>No clients yet</span>
            </div>
          ) : (
            <div className="setup-client-register" role="list">
              {clients.map((client) => (
                <button
                  className={client.clientId === selectedClientId ? "active" : ""}
                  key={client.clientId}
                  onClick={() => onSelectClient(client)}
                  type="button"
                >
                  <span>
                    <strong>{client.displayName || client.legalName}</strong>
                    <small>{client.code}</small>
                  </span>
                  <em className={`status-pill ${client.status.toLowerCase()}`}>
                    {client.status}
                  </em>
                </button>
              ))}
            </div>
          )}

          {hasMoreClients && (
            <button
              className="icon-button setup-client-load-more"
              disabled={isLoadingMoreClients}
              onClick={onLoadMoreClients}
              type="button"
            >
              <Users size={16} />
              {isLoadingMoreClients ? "Loading" : `Load more (${clients.length} of ${filteredCount})`}
            </button>
          )}
        </section>

        <form
          className="setup-focus-panel setup-client-editor-panel"
          onSubmit={handleSaveClient}
        >
          <div className="setup-panel-heading">
            <span>Selected client</span>
            <strong>{selectedClient?.code ?? "No selection"}</strong>
          </div>

          {clientEditError !== "" && (
            <div className="setup-inline-error" role="alert">
              <AlertCircle size={14} />
              <span>{clientEditError}</span>
            </div>
          )}
          {clientEditMessage !== "" && (
            <div className="setup-inline-success" role="status">
              <CheckCircle2 size={14} />
              <span>{clientEditMessage}</span>
            </div>
          )}

          {selectedClient === null ? (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>Select or create a client</span>
            </div>
          ) : (
            <>
              <div className="setup-client-record-strip">
                <span className={`status-pill large ${selectedClient.status.toLowerCase()}`}>
                  {selectedClient.status}
                </span>
                <strong>{selectedClient.displayName || selectedClient.legalName}</strong>
                <small>{selectedClient.legalName}</small>
              </div>

              <div className="setup-client-edit-grid">
                <label className="form-field">
                  <span>Client code</span>
                  <input value={selectedClient.code} readOnly />
                </label>
                <label className="form-field">
                  <span>Legal name</span>
                  <input
                    disabled={isSavingClient}
                    maxLength={200}
                    value={clientEditValue.legalName}
                    onChange={(event) =>
                      onClientEditChange({
                        ...clientEditValue,
                        legalName: event.target.value
                      })
                    }
                  />
                </label>
                <label className="form-field">
                  <span>Display name</span>
                  <input
                    disabled={isSavingClient}
                    maxLength={200}
                    value={clientEditValue.displayName}
                    onChange={(event) =>
                      onClientEditChange({
                        ...clientEditValue,
                        displayName: event.target.value
                      })
                    }
                  />
                </label>
              </div>

              <div className="setup-client-editor-actions">
                <button
                  className="icon-button primary"
                  disabled={isSavingClient || clientEditValue.legalName.trim() === ""}
                  title="Save client"
                  type="submit"
                >
                  <Save size={16} />
                  Save client
                </button>
                <button
                  className="icon-button"
                  disabled={isSavingClient || selectedIsActive}
                  onClick={onActivateClient}
                  title="Activate client"
                  type="button"
                >
                  <CheckCircle2 size={16} />
                  Activate
                </button>
                <button
                  className="icon-button"
                  disabled={isSavingClient || selectedIsSuspended}
                  onClick={onSuspendClient}
                  title="Suspend client"
                  type="button"
                >
                  <PauseCircle size={16} />
                  Suspend
                </button>
              </div>
            </>
          )}
        </form>

        <form
          className="setup-client-create-form setup-client-create-panel setup-focus-form"
          onSubmit={onCreateClient}
        >
          <div className="setup-form-heading">
            <span>New client</span>
            <strong>Create client</strong>
          </div>
          {clientCreateError !== "" && (
            <div className="setup-inline-error" role="alert">
              <AlertCircle size={14} />
              <span>{clientCreateError}</span>
            </div>
          )}
          {clientCreateMessage !== "" && (
            <div className="setup-inline-success" role="status">
              <CheckCircle2 size={14} />
              <span>{clientCreateMessage}</span>
            </div>
          )}
          <label className="form-field">
            <span>Code</span>
            <input
              disabled={isCreatingClient}
              maxLength={32}
              value={clientForm.code}
              onChange={(event) =>
                onClientFormChange({ ...clientForm, code: event.target.value })
              }
            />
          </label>
          <label className="form-field">
            <span>Legal name</span>
            <input
              disabled={isCreatingClient}
              maxLength={200}
              value={clientForm.legalName}
              onChange={(event) =>
                onClientFormChange({ ...clientForm, legalName: event.target.value })
              }
            />
          </label>
          <label className="form-field">
            <span>Display name</span>
            <input
              disabled={isCreatingClient}
              maxLength={200}
              value={clientForm.displayName}
              onChange={(event) =>
                onClientFormChange({ ...clientForm, displayName: event.target.value })
              }
            />
          </label>
          <button
            className="icon-button primary"
            disabled={
              isCreatingClient
              || clientForm.code.trim() === ""
              || clientForm.legalName.trim() === ""
            }
            type="submit"
          >
            <Plus size={16} />
            Create client
          </button>
        </form>
      </div>
    </section>
  );
}

function ProductCatalogSetupModel({
  catalog,
  catalogRevisions,
  catalogError,
  catalogMessage,
  isSavingCatalog,
  onRefreshCatalog,
  onSaveCatalog,
  onPublishCatalog,
  productModules
}: {
  catalog: ProductAccessCatalog | null;
  catalogRevisions: ProductAccessCatalog[];
  catalogError: string;
  catalogMessage: string;
  isSavingCatalog: boolean;
  onRefreshCatalog: () => Promise<void>;
  onSaveCatalog: (catalog: ProductAccessCatalog, requestedBy: string) => Promise<boolean>;
  onPublishCatalog: (requestedBy: string) => Promise<void>;
  productModules: ProductModule[];
}) {
  const [groupForm, setGroupForm] =
    useState<ProductModuleGroupFormInput>(emptyProductGroupForm);
  const [resourceForm, setResourceForm] =
    useState<ProductResourceFormInput>(emptyProductResourceForm);
  const [requestedBy, setRequestedBy] = useState("Control Desk");
  const [changeReason, setChangeReason] = useState(
    "Product definition reviewed for the next published revision."
  );
  const editableCatalog: ProductAccessCatalog = catalog ?? createEmptyProductCatalog(productModules);
  const catalogModules = editableCatalog.modules.length > 0
    ? editableCatalog.modules
    : productModules;
  const catalogGroups = editableCatalog.moduleGroups;
  const catalogResources = editableCatalog.resources;
  const activeModules = catalogModules.filter((module) => module.isActive).length;
  const includedModules = catalogModules.filter(
    (module) => module.commercialMode === "IncludedForAll"
  ).length;
  const paidModules = catalogModules.filter(
    (module) => module.commercialMode === "PaidAddOn"
  ).length;
  const draftBillingDefaultCount = catalogModules.filter(
    (module) => module.billingDefaults !== null && module.billingDefaults !== undefined
  ).length;

  async function handleSaveGroup(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const group: ProductModuleGroup = {
      groupId: groupForm.groupId.trim(),
      displayName: groupForm.displayName.trim(),
      accessKind: groupForm.accessKind,
      moduleCodes: splitCatalogValues(groupForm.moduleCodes)
    };

    if (group.groupId === "" || group.displayName === "") {
      return;
    }

    const wasSaved = await onSaveCatalog(
      {
        ...catalogWithReason(editableCatalog, changeReason),
        moduleGroups: sortProductGroups(upsertCatalogItem(
          catalogGroups,
          group,
          (item) => item.groupId,
          group.groupId
        ))
      },
      requestedByOrDefault(requestedBy)
    );
    if (wasSaved) {
      setGroupForm(emptyProductGroupForm);
    }
  }

  async function handleSaveResource(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const resource: ProductResource = {
      resourceId: resourceForm.resourceId.trim(),
      displayName: resourceForm.displayName.trim(),
      accessKind: resourceForm.accessKind,
      requiredGroupIds: splitCatalogValues(resourceForm.requiredGroupIds),
      requiredModuleCodes: splitCatalogValues(resourceForm.requiredModuleCodes),
      resolvedModuleCodes: []
    };

    if (resource.resourceId === "" || resource.displayName === "") {
      return;
    }

    const wasSaved = await onSaveCatalog(
      {
        ...catalogWithReason(editableCatalog, changeReason),
        resources: sortProductResources(upsertCatalogItem(
          catalogResources,
          resource,
          (item) => item.resourceId,
          resource.resourceId
        ))
      },
      requestedByOrDefault(requestedBy)
    );
    if (wasSaved) {
      setResourceForm(emptyProductResourceForm);
    }
  }

  async function handleRemoveGroup(groupId: string) {
    await onSaveCatalog(
      {
        ...catalogWithReason(editableCatalog, changeReason),
        moduleGroups: catalogGroups.filter((group) => !catalogIdEquals(group.groupId, groupId)),
        resources: catalogResources.map((resource) => ({
          ...resource,
          requiredGroupIds: resource.requiredGroupIds.filter((item) => !catalogIdEquals(item, groupId))
        }))
      },
      requestedByOrDefault(requestedBy)
    );
  }

  async function handleRemoveResource(resourceId: string) {
    await onSaveCatalog(
      {
        ...catalogWithReason(editableCatalog, changeReason),
        resources: catalogResources.filter((resource) => !catalogIdEquals(resource.resourceId, resourceId))
      },
      requestedByOrDefault(requestedBy)
    );
  }

  return (
    <section className="setup-focus-pane">
      <SetupFocusHeading
        Icon={Package}
        eyebrow="Commercial setup"
        title="Product Catalog"
        description="Product definitions only: modules, billing defaults, access groups, and resources."
      />

      <div className="setup-focus-stats">
        <SetupFact label="Modules" value={catalogModules.length.toString()} />
        <SetupFact label="Active" value={activeModules.toString()} />
        <SetupFact label="Included" value={includedModules.toString()} />
        <SetupFact label="Paid add-ons" value={paidModules.toString()} />
        <SetupFact label="Billing defaults" value={draftBillingDefaultCount.toString()} />
        <SetupFact label="Resources" value={catalogResources.length.toString()} />
      </div>

      <div className="setup-product-catalog-workbench">
        <ProductModuleCatalogAdminPanel
          catalog={editableCatalog}
          changeReason={changeReason}
          error={catalogError}
          isBusy={isSavingCatalog}
          message={catalogMessage}
          publishedModules={productModules}
          requestedBy={requestedBy}
          onSaveCatalog={onSaveCatalog}
        />

        <section className="setup-focus-panel setup-product-access-panel">
          <div className="setup-panel-heading">
            <span>Access catalog</span>
            <strong>{catalogGroups.length} groups / {catalogResources.length} resources</strong>
          </div>

          {catalogError !== "" && (
            <div className="setup-inline-error" role="alert">
              <AlertCircle size={14} />
              <span>{catalogError}</span>
            </div>
          )}
          {catalogMessage !== "" && (
            <div className="setup-inline-success" role="status">
              <CheckCircle2 size={14} />
              <span>{catalogMessage}</span>
            </div>
          )}

          <div className="setup-product-catalog-actions">
            <div className="setup-product-catalog-revision-state">
              <span className={`status-pill ${editableCatalog.state.toLowerCase()}`}>
                {editableCatalog.state}
              </span>
              <strong>
                {editableCatalog.state.toLowerCase() === "draft"
                  ? `Draft from #${editableCatalog.baseCatalogRevisionNumber ?? "-"}`
                  : `Revision #${editableCatalog.revisionNumber ?? "-"}`}
              </strong>
            </div>
            <label className="form-field">
              <span>Requested by</span>
              <input
                disabled={isSavingCatalog}
                maxLength={160}
                value={requestedBy}
                onChange={(event) => setRequestedBy(event.target.value)}
              />
            </label>
            <label className="form-field setup-product-catalog-reason">
              <span>Change reason</span>
              <input
                disabled={isSavingCatalog}
                maxLength={1000}
                value={changeReason}
                onChange={(event) => setChangeReason(event.target.value)}
              />
            </label>
            <button
              className="icon-button primary"
              disabled={isSavingCatalog || editableCatalog.state.toLowerCase() !== "draft"}
              onClick={() => onPublishCatalog(requestedByOrDefault(requestedBy))}
              title="Publish immutable product catalog revision"
              type="button"
            >
              <Upload size={16} />
              Publish revision
            </button>
            <button
              className="icon-button"
              disabled={isSavingCatalog}
              onClick={onRefreshCatalog}
              title="Refresh product catalog"
              type="button"
            >
              <RefreshCw size={16} />
              Refresh
            </button>
          </div>

          {catalog === null && (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>Catalog not loaded</span>
            </div>
          )}

          <div className="setup-product-access-editor-grid">
            <form className="setup-product-access-form" onSubmit={handleSaveGroup}>
              <div className="setup-form-heading">
                <span>Module group</span>
                <strong>{groupForm.groupId.trim() === "" ? "New" : "Edit"}</strong>
              </div>
              <label className="form-field">
                <span>Group id</span>
                <input
                  disabled={isSavingCatalog}
                  value={groupForm.groupId}
                  onChange={(event) => setGroupForm({ ...groupForm, groupId: event.target.value })}
                />
              </label>
              <label className="form-field">
                <span>Name</span>
                <input
                  disabled={isSavingCatalog}
                  value={groupForm.displayName}
                  onChange={(event) => setGroupForm({ ...groupForm, displayName: event.target.value })}
                />
              </label>
              <label className="form-field">
                <span>Access</span>
                <select
                  disabled={isSavingCatalog}
                  value={groupForm.accessKind}
                  onChange={(event) => setGroupForm({
                    ...groupForm,
                    accessKind: event.target.value
                  })}
                >
                  <option value="Public">Public</option>
                  <option value="CoreIncluded">CoreIncluded</option>
                  <option value="PaidModule">PaidModule</option>
                </select>
              </label>
              <label className="form-field">
                <span>Modules</span>
                <textarea
                  disabled={isSavingCatalog}
                  rows={3}
                  value={groupForm.moduleCodes}
                  onChange={(event) => setGroupForm({ ...groupForm, moduleCodes: event.target.value })}
                />
              </label>
              <div className="setup-product-access-actions">
                <button
                  className="mini-button"
                  disabled={isSavingCatalog}
                  onClick={() => setGroupForm(emptyProductGroupForm)}
                  type="button"
                >
                  <Plus size={13} />
                  New
                </button>
                <button
                  className="mini-button"
                  disabled={
                    isSavingCatalog
                    || groupForm.groupId.trim() === ""
                    || groupForm.displayName.trim() === ""
                  }
                  type="submit"
                >
                  <Save size={13} />
                  Save
                </button>
              </div>
            </form>

            <form className="setup-product-access-form" onSubmit={handleSaveResource}>
              <div className="setup-form-heading">
                <span>Resource</span>
                <strong>{resourceForm.resourceId.trim() === "" ? "New" : "Edit"}</strong>
              </div>
              <label className="form-field">
                <span>Resource id</span>
                <input
                  disabled={isSavingCatalog}
                  value={resourceForm.resourceId}
                  onChange={(event) => setResourceForm({ ...resourceForm, resourceId: event.target.value })}
                />
              </label>
              <label className="form-field">
                <span>Name</span>
                <input
                  disabled={isSavingCatalog}
                  value={resourceForm.displayName}
                  onChange={(event) => setResourceForm({ ...resourceForm, displayName: event.target.value })}
                />
              </label>
              <label className="form-field">
                <span>Access</span>
                <select
                  disabled={isSavingCatalog}
                  value={resourceForm.accessKind}
                  onChange={(event) => setResourceForm({
                    ...resourceForm,
                    accessKind: event.target.value
                  })}
                >
                  <option value="Public">Public</option>
                  <option value="CoreIncluded">CoreIncluded</option>
                  <option value="PaidModule">PaidModule</option>
                </select>
              </label>
              <label className="form-field">
                <span>Groups</span>
                <textarea
                  disabled={isSavingCatalog}
                  rows={2}
                  value={resourceForm.requiredGroupIds}
                  onChange={(event) => setResourceForm({
                    ...resourceForm,
                    requiredGroupIds: event.target.value
                  })}
                />
              </label>
              <label className="form-field">
                <span>Direct modules</span>
                <textarea
                  disabled={isSavingCatalog}
                  rows={2}
                  value={resourceForm.requiredModuleCodes}
                  onChange={(event) => setResourceForm({
                    ...resourceForm,
                    requiredModuleCodes: event.target.value
                  })}
                />
              </label>
              <div className="setup-product-access-actions">
                <button
                  className="mini-button"
                  disabled={isSavingCatalog}
                  onClick={() => setResourceForm(emptyProductResourceForm)}
                  type="button"
                >
                  <Plus size={13} />
                  New
                </button>
                <button
                  className="mini-button"
                  disabled={
                    isSavingCatalog
                    || resourceForm.resourceId.trim() === ""
                    || resourceForm.displayName.trim() === ""
                  }
                  type="submit"
                >
                  <Save size={13} />
                  Save
                </button>
              </div>
            </form>
          </div>

          <div className="setup-product-access-lists">
            <section className="setup-product-access-list-panel">
              <div className="setup-form-heading">
                <span>Module groups</span>
                <strong>{catalogGroups.length}</strong>
              </div>
              {catalogGroups.length === 0 ? (
                <div className="setup-empty-row">
                  <ListTree size={14} />
                  <span>No module groups</span>
                </div>
              ) : (
                <div className="setup-product-access-list">
                  {catalogGroups.map((group) => (
                    <article className="setup-product-access-item" key={group.groupId}>
                      <header>
                        <span>
                          <strong>{group.displayName}</strong>
                          <small>{group.groupId}</small>
                        </span>
                        <em className={`setup-product-access-kind ${accessKindClass(group.accessKind)}`}>
                          {formatAccessKind(group.accessKind)}
                        </em>
                      </header>
                      <p>{joinCatalogValues(group.moduleCodes)}</p>
                      <div className="setup-product-access-actions">
                        <button
                          className="mini-button"
                          disabled={isSavingCatalog}
                          onClick={() => setGroupForm(toProductGroupForm(group))}
                          type="button"
                        >
                          <Edit3 size={13} />
                          Edit
                        </button>
                        <button
                          className="mini-button"
                          disabled={isSavingCatalog}
                          onClick={() => handleRemoveGroup(group.groupId)}
                          type="button"
                        >
                          <Trash2 size={13} />
                          Remove
                        </button>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>

            <section className="setup-product-access-list-panel">
              <div className="setup-form-heading">
                <span>Resources</span>
                <strong>{catalogResources.length}</strong>
              </div>
              {catalogResources.length === 0 ? (
                <div className="setup-empty-row">
                  <ListTree size={14} />
                  <span>No resources</span>
                </div>
              ) : (
                <div className="setup-product-access-list">
                  {catalogResources.map((resource) => (
                    <article className="setup-product-access-item" key={resource.resourceId}>
                      <header>
                        <span>
                          <strong>{resource.displayName}</strong>
                          <small>{resource.resourceId}</small>
                        </span>
                        <em className={`setup-product-access-kind ${accessKindClass(resource.accessKind)}`}>
                          {formatAccessKind(resource.accessKind)}
                        </em>
                      </header>
                      <dl className="setup-product-resource-facts">
                        <div>
                          <dt>Groups</dt>
                          <dd>{joinCatalogValues(resource.requiredGroupIds)}</dd>
                        </div>
                        <div>
                          <dt>Direct modules</dt>
                          <dd>{joinCatalogValues(resource.requiredModuleCodes)}</dd>
                        </div>
                        <div>
                          <dt>Resolved modules</dt>
                          <dd>{joinCatalogValues(resource.resolvedModuleCodes)}</dd>
                        </div>
                      </dl>
                      <div className="setup-product-access-actions">
                        <button
                          className="mini-button"
                          disabled={isSavingCatalog}
                          onClick={() => setResourceForm(toProductResourceForm(resource))}
                          type="button"
                        >
                          <Edit3 size={13} />
                          Edit
                        </button>
                        <button
                          className="mini-button"
                          disabled={isSavingCatalog}
                          onClick={() => handleRemoveResource(resource.resourceId)}
                          type="button"
                        >
                          <Trash2 size={13} />
                          Remove
                        </button>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>
          </div>

          <section className="setup-product-catalog-history">
            <div className="setup-form-heading">
              <span>Published revisions</span>
              <strong>{catalogRevisions.length}</strong>
            </div>
            <div className="setup-product-catalog-history-list">
              {catalogRevisions.map((revision) => (
                <article key={revision.catalogRevisionId ?? `revision-${revision.revisionNumber ?? "unknown"}`}>
                  <GitBranch size={14} />
                  <span>
                    <strong>Revision #{revision.revisionNumber ?? "-"}</strong>
                    <small>{revision.changeReason || "No change reason recorded"}</small>
                  </span>
                  <em>{revision.changedBy || "-"}</em>
                </article>
              ))}
            </div>
          </section>
        </section>
      </div>
    </section>
  );
}

function AccountRangesSetupModel({
  accountRanges,
  isSavingRange,
  ledgerAccounts,
  onRangeChange,
  onRangeSelect,
  onSaveRange,
  rangeSaveError,
  rangeSaveMessage,
  rangeValue,
  rangeValidation,
  selectedRangeRole
}: {
  accountRanges: AccountCodeRange[];
  isSavingRange: boolean;
  ledgerAccounts: LedgerAccountSummary[];
  onRangeChange: (value: AccountCodeRangeFormInput) => void;
  onRangeSelect: (range: AccountCodeRange) => void;
  onSaveRange: () => Promise<void>;
  rangeSaveError: string;
  rangeSaveMessage: string;
  rangeValue: AccountCodeRangeFormInput;
  rangeValidation: AccountCodeRangeValidation | null;
  selectedRangeRole: string;
}) {
  const activeRanges = accountRanges.filter((range) => range.isActive);
  const selectedRange = accountRanges.find((range) => range.role === selectedRangeRole) ?? null;
  const rangeValidationIssues = rangeValidation?.issues ?? [];
  const rangeValidationIssuesByRole = buildRangeValidationIssueMap(rangeValidationIssues);
  const selectedRangeIssues = selectedRange === null
    ? []
    : rangeValidationIssuesByRole.get(selectedRange.role) ?? [];
  const displayedRangeValidationIssues =
    selectedRangeIssues.length > 0 ? selectedRangeIssues : rangeValidationIssues;
  const rangeUsageByRole = buildRangeUsageByRole(ledgerAccounts, accountRanges);
  const selectedRangeUsage = selectedRange === null
    ? 0
    : rangeUsageByRole.get(selectedRange.role) ?? 0;
  const rangeSetupFacts = getRangeSetupFacts(
    accountRanges,
    rangeValidation,
    selectedRange,
    selectedRangeIssues,
    selectedRangeUsage
  );
  const selectedRangeFacts = selectedRange === null
    ? []
    : getSelectedRangeFacts(selectedRange, selectedRangeIssues, selectedRangeUsage);
  const displayedRangeIssueGroups = getRangeValidationIssueGroups(displayedRangeValidationIssues);
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

  return (
    <section className="setup-focus-pane">
      <SetupFocusHeading
        Icon={Landmark}
        eyebrow="Accounting setup"
        title="COA Ranges"
        description="Only account code ranges are visible here, because this model feeds ledger creation and posting."
      />

      {rangeSaveError !== "" && (
        <div className="setup-inline-error" role="alert">
          <AlertCircle size={14} />
          <span>{rangeSaveError}</span>
        </div>
      )}
      {rangeSaveMessage !== "" && (
        <div className="setup-inline-success" role="status">
          <CheckCircle2 size={14} />
          <span>{rangeSaveMessage}</span>
        </div>
      )}

      <div className="setup-focus-range-editor">
        <ChartOfAccountsRangePanel
          activeRangeCount={activeRanges.length}
          canSaveRange={canSaveRange}
          displayedRangeIssueGroups={displayedRangeIssueGroups}
          displayedRangeValidationIssues={displayedRangeValidationIssues}
          filtersRole=""
          isBusy={isSavingRange}
          onRangeChange={onRangeChange}
          onRangeSelect={onRangeSelect}
          onSaveRange={onSaveRange}
          rangeSetupFacts={rangeSetupFacts}
          rangeUsageByRole={rangeUsageByRole}
          rangeValidation={rangeValidation}
          rangeValidationIssuesByRole={rangeValidationIssuesByRole}
          rangeValidationText={rangeValidationText}
          rangeValue={rangeValue}
          ranges={accountRanges}
          selectedRange={selectedRange}
          selectedRangeFacts={selectedRangeFacts}
          selectedRangeIssues={selectedRangeIssues}
          selectedRangeRole={selectedRangeRole}
        />
      </div>
    </section>
  );
}

function LedgerAccountsSetupModel({
  accountRanges,
  ledgerAccounts
}: {
  accountRanges: AccountCodeRange[];
  ledgerAccounts: LedgerAccountSummary[];
}) {
  const sortedAccounts = useMemo(
    () => [...ledgerAccounts].sort(compareLedgerAccounts),
    [ledgerAccounts]
  );
  const [selectedAccountId, setSelectedAccountId] = useState("");
  const selectedAccount =
    sortedAccounts.find((account) => account.ledgerAccountId === selectedAccountId) ?? null;
  const accountsById = useMemo(
    () => new Map(sortedAccounts.map((account) => [account.ledgerAccountId, account])),
    [sortedAccounts]
  );
  const selectedRange = selectedAccount === null
    ? null
    : getLedgerAccountRange(selectedAccount, accountRanges);
  const selectedLevel = selectedAccount === null
    ? null
    : getLegacyAccountLevel(selectedAccount, accountRanges);
  const parentAccount = selectedAccount?.parentAccountId === null
    || selectedAccount?.parentAccountId === undefined
    ? null
    : accountsById.get(selectedAccount.parentAccountId) ?? null;
  const childAccounts = selectedAccount === null
    ? []
    : sortedAccounts.filter((account) => account.parentAccountId === selectedAccount.ledgerAccountId);
  const activeAccounts = ledgerAccounts.filter((account) =>
    equalsIgnoreCase(account.status, "Active")
  ).length;
  const postingAccounts = countPostingAccounts(ledgerAccounts);
  const rangeSummaries = getLedgerRangeSummaries(accountRanges, sortedAccounts);

  useEffect(() => {
    if (sortedAccounts.length === 0) {
      if (selectedAccountId !== "") {
        setSelectedAccountId("");
      }
      return;
    }

    if (!sortedAccounts.some((account) => account.ledgerAccountId === selectedAccountId)) {
      setSelectedAccountId(sortedAccounts[0].ledgerAccountId);
    }
  }, [selectedAccountId, sortedAccounts]);

  return (
    <section className="setup-focus-pane">
      <SetupFocusHeading
        Icon={BookOpen}
        eyebrow="Accounting setup"
        title="Ledger Accounts"
        description="Chart accounts only, grouped as the model that journals and reports depend on."
      />

      <div className="setup-focus-stats">
        <SetupFact label="Accounts" value={ledgerAccounts.length.toString()} />
        <SetupFact label="Active" value={activeAccounts.toString()} />
        <SetupFact
          label="Posting"
          value={postingAccounts.toString()}
        />
        <SetupFact
          label="Headers"
          value={(ledgerAccounts.length - postingAccounts).toString()}
        />
        <SetupFact label="Ranges" value={rangeSummaries.length.toString()} />
        <SetupFact label="Selected" value={selectedAccount?.displayCode ?? "-"} />
      </div>

      <div className="setup-ledger-workbench">
        <section className="setup-focus-panel setup-ledger-register-panel">
          <div className="setup-panel-heading">
            <span>Chart register</span>
            <strong>{ledgerAccounts.length} accounts</strong>
          </div>
          {sortedAccounts.length === 0 ? (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>No ledger accounts yet</span>
            </div>
          ) : (
            <div className="setup-ledger-register" role="list">
              {sortedAccounts.map((account) => (
                <button
                  className={account.ledgerAccountId === selectedAccountId ? "active" : ""}
                  key={account.ledgerAccountId}
                  onClick={() => setSelectedAccountId(account.ledgerAccountId)}
                  type="button"
                >
                  <span>
                    <strong>{account.displayCode}</strong>
                    <small>{account.name}</small>
                  </span>
                  <em className={`status-pill ${account.status.toLowerCase()}`}>
                    {account.status}
                  </em>
                </button>
              ))}
            </div>
          )}
        </section>

        <section className="setup-focus-panel setup-ledger-range-panel">
          <div className="setup-panel-heading">
            <span>Range coverage</span>
            <strong>{rangeSummaries.length} ranges</strong>
          </div>
          {rangeSummaries.length === 0 ? (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>No range usage yet</span>
            </div>
          ) : (
            <div className="setup-ledger-range-list">
              {rangeSummaries.map((summary) => (
                <article
                  className={
                    selectedRange?.role === summary.range.role ? "active" : ""
                  }
                  key={summary.range.role}
                >
                  <span>
                    <strong>{summary.range.displayName}</strong>
                    <small>
                      {summary.range.rangeStart} to {summary.range.rangeEnd}
                    </small>
                  </span>
                  <em>{summary.accountCount}</em>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="setup-focus-panel setup-ledger-detail-panel">
          <div className="setup-panel-heading">
            <span>Selected account</span>
            <strong>{selectedAccount?.displayCode ?? "No selection"}</strong>
          </div>

          {selectedAccount === null || selectedLevel === null ? (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>Select a ledger account</span>
            </div>
          ) : (
            <>
              <div className="setup-ledger-record-strip">
                <span className={`status-pill large ${selectedAccount.status.toLowerCase()}`}>
                  {selectedAccount.status}
                </span>
                <strong>{selectedAccount.name}</strong>
                <small>{selectedAccount.displayCode}</small>
              </div>

              <div className="setup-ledger-detail-grid">
                <SetupFact label="Type" value={selectedAccount.type} />
                <SetupFact label="Normal balance" value={selectedAccount.normalBalance} />
                <SetupFact label="Level" value={selectedLevel.label} />
                <SetupFact
                  label="Posting"
                  value={selectedAccount.isPostingAccount ? "Yes" : "No"}
                />
                <SetupFact
                  label="Range"
                  value={selectedRange?.displayName ?? selectedAccount.rangeDisplayName ?? "-"}
                />
                <SetupFact label="Children" value={childAccounts.length.toString()} />
              </div>

              <div className="setup-ledger-context-grid">
                <section className="setup-ledger-context-card">
                  <div className="setup-form-heading">
                    <span>Parent</span>
                    <strong>{parentAccount?.displayCode ?? "Root account"}</strong>
                  </div>
                  {parentAccount === null ? (
                    <div className="setup-empty-row">
                      <CircleDot size={14} />
                      <span>No parent account</span>
                    </div>
                  ) : (
                    <div className="setup-ledger-mini-account">
                      <strong>{parentAccount.name}</strong>
                      <small>{parentAccount.type} / {parentAccount.status}</small>
                    </div>
                  )}
                </section>

                <section className="setup-ledger-context-card">
                  <div className="setup-form-heading">
                    <span>Children</span>
                    <strong>{childAccounts.length}</strong>
                  </div>
                  {childAccounts.length === 0 ? (
                    <div className="setup-empty-row">
                      <CircleDot size={14} />
                      <span>No child accounts</span>
                    </div>
                  ) : (
                    <div className="setup-ledger-child-list">
                      {childAccounts.slice(0, 6).map((account) => (
                        <div className="setup-ledger-mini-account" key={account.ledgerAccountId}>
                          <strong>{account.displayCode} {account.name}</strong>
                          <small>{account.type} / {account.status}</small>
                        </div>
                      ))}
                    </div>
                  )}
                </section>
              </div>
            </>
          )}
        </section>
      </div>
    </section>
  );
}

function DeploymentDefaultsSetupModel({
  flow,
  runtimeItems
}: {
  flow: Array<{ source: string; target: string }>;
  runtimeItems: DeploymentDefaultItem[];
}) {
  const [selectedDefaultKey, setSelectedDefaultKey] = useState(runtimeItems[0]?.key ?? "");
  const selectedDefault =
    runtimeItems.find((item) => item.key === selectedDefaultKey) ?? null;
  const readyDefaults = runtimeItems.filter((item) => item.status === "Ready").length;
  const partialDefaults = runtimeItems.filter((item) => item.status === "Partial").length;
  const plannedDefaults = runtimeItems.filter((item) => item.status === "Planned").length;
  const outOfSetupItems = [
    "Setup tokens and bootstrap packages",
    "Cloud heartbeat, diagnostics, and handoff status",
    "Customer app activation or revocation",
    "Pairing descriptors and support commands"
  ];

  useEffect(() => {
    if (runtimeItems.length === 0) {
      if (selectedDefaultKey !== "") {
        setSelectedDefaultKey("");
      }
      return;
    }

    if (!runtimeItems.some((item) => item.key === selectedDefaultKey)) {
      setSelectedDefaultKey(runtimeItems[0].key);
    }
  }, [runtimeItems, selectedDefaultKey]);

  return (
    <section className="setup-focus-pane">
      <SetupFocusHeading
        Icon={ServerCog}
        eyebrow="Runtime setup"
        title="Deployment Defaults"
        description="Runtime setup only: the defaults that Deployment & Cloud will consume later."
      />

      <div className="setup-focus-stats">
        <SetupFact label="Defaults" value={runtimeItems.length.toString()} />
        <SetupFact label="Ready" value={readyDefaults.toString()} />
        <SetupFact label="Partial" value={partialDefaults.toString()} />
        <SetupFact label="Planned" value={plannedDefaults.toString()} />
        <SetupFact label="Flows" value={flow.length.toString()} />
        <SetupFact label="Selected" value={selectedDefault?.status ?? "-"} />
      </div>

      <div className="setup-deployment-workbench">
        <section className="setup-focus-panel setup-deployment-register-panel">
          <div className="setup-panel-heading">
            <span>Default register</span>
            <strong>{runtimeItems.length} items</strong>
          </div>

          {runtimeItems.length === 0 ? (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>No deployment defaults yet</span>
            </div>
          ) : (
            <div className="setup-deployment-register" role="list">
              {runtimeItems.map((item) => (
                <button
                  className={item.key === selectedDefaultKey ? "active" : ""}
                  key={item.key}
                  onClick={() => setSelectedDefaultKey(item.key)}
                  type="button"
                >
                  <item.Icon size={17} />
                  <span>
                    <strong>{item.label}</strong>
                    <small>{item.value}</small>
                  </span>
                  <em className={`status-pill ${deploymentDefaultStatusClass(item.status)}`}>
                    {item.status}
                  </em>
                </button>
              ))}
            </div>
          )}
        </section>

        <section className="setup-focus-panel setup-deployment-detail-panel">
          <div className="setup-panel-heading">
            <span>Selected default</span>
            <strong>{selectedDefault?.label ?? "No selection"}</strong>
          </div>

          {selectedDefault === null ? (
            <div className="setup-empty-row">
              <CircleDot size={14} />
              <span>Select a deployment default</span>
            </div>
          ) : (
            <>
              <div className="setup-deployment-record-strip">
                <selectedDefault.Icon size={18} />
                <span className={`status-pill large ${deploymentDefaultStatusClass(selectedDefault.status)}`}>
                  {selectedDefault.status}
                </span>
                <strong>{selectedDefault.label}</strong>
                <small>{selectedDefault.detail}</small>
              </div>

              <div className="setup-deployment-detail-grid">
                <SetupFact label="Current state" value={selectedDefault.value} />
                <SetupFact label="Owner" value={selectedDefault.owner} />
                <SetupFact label="Consumed by" value={selectedDefault.consumedBy} />
                <SetupFact label="Source" value={selectedDefault.source} />
              </div>

              <section className="setup-deployment-boundary">
                <div className="setup-form-heading">
                  <span>Not part of Setup</span>
                  <strong>Deployment & Cloud owns these later</strong>
                </div>
                <div className="setup-deployment-boundary-list">
                  {outOfSetupItems.map((item) => (
                    <div key={item}>
                      <PauseCircle size={15} />
                      <span>{item}</span>
                    </div>
                  ))}
                </div>
              </section>
            </>
          )}
        </section>

        <section className="setup-focus-panel setup-deployment-flow-panel">
          <div className="setup-panel-heading">
            <span>Reuse flow</span>
            <strong>{flow.length} paths</strong>
          </div>
          <div className="setup-deployment-flow-map">
            {flow.map((item) => (
              <div className="setup-deployment-flow-row" key={item.source}>
                <strong>{item.source}</strong>
                <ArrowRight size={15} />
                <span>{item.target}</span>
              </div>
            ))}
          </div>
        </section>
      </div>
    </section>
  );
}

function SetupFocusHeading({
  Icon,
  description,
  eyebrow,
  title
}: {
  Icon: LucideIcon;
  description: string;
  eyebrow: string;
  title: string;
}) {
  return (
    <header className="setup-focus-heading">
      <Icon size={22} />
      <div>
        <span>{eyebrow}</span>
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </header>
  );
}

function SetupMetricCard({ metric }: { metric: SetupMetric }) {
  return (
    <section className={`setup-metric-card ${metric.tone}`}>
      <span>{metric.label}</span>
      <strong>{metric.value}</strong>
      <small>{metric.detail}</small>
    </section>
  );
}

function SetupLaneHeading({
  Icon,
  eyebrow,
  title
}: {
  Icon: LucideIcon;
  eyebrow: string;
  title: string;
}) {
  return (
    <div className="setup-lane-heading">
      <Icon size={19} />
      <div>
        <span>{eyebrow}</span>
        <strong>{title}</strong>
      </div>
    </div>
  );
}

function SetupFact({ label, value }: { label: string; value: string }) {
  return (
    <div className="setup-fact">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function SetupList({
  emptyLabel,
  items
}: {
  emptyLabel: string;
  items: Array<{ key: string; title: string; meta: string }>;
}) {
  if (items.length === 0) {
    return (
      <div className="setup-empty-row">
        <CircleDot size={14} />
        <span>{emptyLabel}</span>
      </div>
    );
  }

  return (
    <ul className="setup-mini-list">
      {items.map((item) => (
        <li key={item.key}>
          <span>{item.title}</span>
          <small>{item.meta}</small>
        </li>
      ))}
    </ul>
  );
}

function buildSetupModel(snapshot: SetupSnapshot) {
  const totalClients = snapshot.clientSummary.totalCount;
  const activeClients = snapshot.clientSummary.activeCount;
  const activeModules = snapshot.productModules.filter((module) => module.isActive).length;
  const billingDefaultCount = snapshot.productModules.filter((module) =>
    module.billingDefaults !== null && module.billingDefaults !== undefined
  ).length;
  const activeRanges = snapshot.accountRanges.filter((range) => range.isActive).length;
  const configuredVoucherRules = snapshot.voucherRules.filter((rule) => rule.isConfigured).length;
  const catalogGroups = snapshot.productAccessCatalog?.moduleGroups.length ?? 0;
  const catalogResources = snapshot.productAccessCatalog?.resources.length ?? 0;
  const accountingReady = Boolean(
    snapshot.rangeValidation?.isValid
    && snapshot.accountingControls?.isConfigured
    && snapshot.openingBalanceProfile?.isConfigured
  );
  const accountingStatus = accountingReady
    ? "Ranges, controls, and opening profile are configured"
    : "Accounting foundation needs attention";
  const accountingTone: "ready" | "attention" = accountingReady ? "ready" : "attention";

  const overview: SetupMetric[] = [
    {
      label: "Clients",
      value: totalClients.toString(),
      detail: `${activeClients} active`,
      tone: totalClients > 0 ? "ready" : "attention"
    },
    {
      label: "Product Catalog",
      value: activeModules.toString(),
      detail: `${catalogGroups} groups / ${catalogResources} resources`,
      tone: activeModules > 0 ? "ready" : "attention"
    },
    {
      label: "Account Ranges",
      value: activeRanges.toString(),
      detail: snapshot.rangeValidation?.isValid ? "valid" : "review",
      tone: snapshot.rangeValidation?.isValid ? "ready" : "attention"
    },
    {
      label: "Ledger Accounts",
      value: snapshot.ledgerAccounts.length.toString(),
      detail: `${countPostingAccounts(snapshot.ledgerAccounts)} posting`,
      tone: snapshot.ledgerAccounts.length > 0 ? "ready" : "quiet"
    },
    {
      label: "Controls",
      value: snapshot.accountingControls?.isConfigured ? "Set" : "Open",
      detail: snapshot.accountingControls?.baseCurrencyCode ?? "currency pending",
      tone: snapshot.accountingControls?.isConfigured ? "ready" : "attention"
    },
    {
      label: "Opening Profile",
      value: snapshot.openingBalanceProfile?.isConfigured ? "Set" : "Open",
      detail: snapshot.openingBalanceProfile?.isConfigured
        ? snapshot.openingBalanceProfile.status
        : "profile pending",
      tone: snapshot.openingBalanceProfile?.isConfigured ? "ready" : "attention"
    }
  ];

  const clientDeploymentStatus: DeploymentDefaultStatus =
    totalClients === 0
      ? "Planned"
      : activeClients === totalClients
        ? "Ready"
        : "Partial";
  const productAccessStatus: DeploymentDefaultStatus =
    catalogGroups > 0 && catalogResources > 0
      ? "Ready"
      : catalogGroups > 0 || catalogResources > 0
        ? "Partial"
        : "Planned";
  const voucherNumberingStatus: DeploymentDefaultStatus =
    snapshot.voucherRules.length === 0
      ? "Planned"
      : configuredVoucherRules === snapshot.voucherRules.length
        ? "Ready"
        : configuredVoucherRules > 0
          ? "Partial"
          : "Planned";
  const runtimeItems: DeploymentDefaultItem[] = [
    {
      key: "client-deployment-profiles",
      label: "Client deployment profiles",
      value: `${activeClients}/${totalClients} active clients`,
      detail:
        "Client records are the identity seed for deployments. Per-customer install health still belongs in Deployment & Cloud.",
      owner: "Setup > Clients",
      consumedBy: "Deployment & Cloud",
      source: "Client master records",
      status: clientDeploymentStatus,
      Icon: Building2
    },
    {
      key: "provider-operator-access",
      label: "Provider operator access",
      value: "Local session boundary",
      detail:
        "Control Desk sign-in stays local-first. Operator scopes, devices, MFA, and recovery belong in Access & Security.",
      owner: "Local Control Desk",
      consumedBy: "Access & Security",
      source: "Local operator session",
      status: "Partial",
      Icon: Users
    },
    {
      key: "product-access-catalog",
      label: "Product access catalog",
      value: `${catalogResources} resources mapped`,
      detail:
        "Module groups and resources define access once so commercial and runtime desks can reuse the same catalog.",
      owner: "Setup > Product Catalog",
      consumedBy: "Commercial Desk",
      source: `${catalogGroups} groups / ${catalogResources} resources`,
      status: productAccessStatus,
      Icon: Package
    },
    {
      key: "voucher-numbering",
      label: "Voucher numbering",
      value: `${configuredVoucherRules}/${snapshot.voucherRules.length} rules configured`,
      detail:
        "Voucher number rules are reusable accounting defaults. Voucher entry and posting remain Accounting Desk work.",
      owner: "Accounting setup",
      consumedBy: "Accounting Desk",
      source: "Voucher number rules",
      status: voucherNumberingStatus,
      Icon: BookOpen
    },
    {
      key: "runtime-handoff-boundary",
      label: "Runtime handoff boundary",
      value: "Deployment desk only",
      detail:
        "Setup records the reusable inputs. Tokens, packages, handoff, heartbeat, and diagnostics stay with deployment operations.",
      owner: "Deployment & Cloud",
      consumedBy: "Client runtime installs",
      source: "Cloud/runtime lifecycle",
      status: "Planned",
      Icon: ServerCog
    }
  ];
  const deploymentDefaultReadyCount =
    runtimeItems.filter((item) => item.status === "Ready").length;

  const flow = [
    { source: "Clients", target: "Client 360" },
    { source: "Product modules", target: "Commercial Desk" },
    { source: "Account ranges", target: "Accounting Desk" },
    { source: "Deployment defaults", target: "Deployment & Cloud" },
    { source: "Access scopes", target: "Access & Security" }
  ];

  return {
    activeClients,
    activeModules,
    billingDefaultCount,
    activeRanges,
    configuredVoucherRules,
    accountingReady,
    accountingStatus,
    accountingTone,
    deploymentDefaultReadyCount,
    overview,
    runtimeItems,
    flow
  };
}

function getSetupModelDefinition(key: SetupModelKey): SetupModelDefinition {
  return setupModelDefinitions.find((definition) => definition.key === key)
    ?? setupModelDefinitions[0];
}

function getSetupModelStatus(
  key: SetupModelKey,
  snapshot: SetupSnapshot,
  model: ReturnType<typeof buildSetupModel>
): string {
  if (key === "clients") {
    return `${snapshot.clientSummary.totalCount} total / ${model.activeClients} active`;
  }

  if (key === "product-catalog") {
    return `${snapshot.productModules.length} modules / ${model.billingDefaultCount} defaults`;
  }

  if (key === "account-ranges") {
    return snapshot.rangeValidation?.isValid
      ? `${model.activeRanges} active / valid`
      : `${model.activeRanges} active / review`;
  }

  if (key === "ledger-accounts") {
    return `${snapshot.ledgerAccounts.length} accounts`;
  }

  return `${model.deploymentDefaultReadyCount}/${model.runtimeItems.length} ready`;
}

function valueOrDefault<T>(
  result: PromiseSettledResult<T>,
  defaultValue: T
): T {
  return result.status === "fulfilled" ? result.value : defaultValue;
}

function issueOrNull<T>(
  label: string,
  result: PromiseSettledResult<T>
): SetupLoadIssue | null {
  if (result.status === "fulfilled") {
    return null;
  }

  return {
    label,
    message: formatError(result.reason)
  };
}

function getLedgerRangeSummaries(
  accountRanges: AccountCodeRange[],
  ledgerAccounts: LedgerAccountSummary[]
): Array<{ range: AccountCodeRange; accountCount: number }> {
  return accountRanges
    .map((range) => ({
      range,
      accountCount: ledgerAccounts.filter((account) =>
        getLedgerAccountRange(account, accountRanges)?.role === range.role
      ).length
    }))
    .filter((summary) => summary.accountCount > 0);
}

function getLedgerAccountRange(
  account: LedgerAccountSummary,
  accountRanges: AccountCodeRange[]
): AccountCodeRange | null {
  const byRole = accountRanges.find((range) => range.role === account.rangeRole);

  if (byRole !== undefined) {
    return byRole;
  }

  return accountRanges.find((range) =>
    account.code.length === range.codeLength
    && account.code.startsWith(range.searchPrefix)
    && account.code >= range.rangeStart
    && account.code <= range.rangeEnd
  ) ?? null;
}

function countPostingAccounts(accounts: LedgerAccountSummary[]): number {
  return accounts.filter((account) => account.isPostingAccount).length;
}

function upsertClient(clients: ClientLookup[], client: ClientLookup): ClientLookup[] {
  const existingIndex = clients.findIndex((item) => item.clientId === client.clientId);

  if (existingIndex === -1) {
    return [...clients, client];
  }

  return clients.map((item) => item.clientId === client.clientId ? client : item);
}

function updateClientStatusSummary(
  summary: ClientDirectorySummary,
  previousStatus: string | undefined,
  nextStatus: string
): ClientDirectorySummary {
  if (previousStatus === undefined || equalsIgnoreCase(previousStatus, nextStatus)) {
    return summary;
  }

  const statusKey = (status: string): keyof ClientDirectorySummary | null => {
    switch (status.trim().toLowerCase()) {
      case "draft": return "draftCount";
      case "active": return "activeCount";
      case "suspended": return "suspendedCount";
      case "archived": return "archivedCount";
      default: return null;
    }
  };
  const previousKey = statusKey(previousStatus);
  const nextKey = statusKey(nextStatus);

  if (previousKey === null || nextKey === null) {
    return summary;
  }

  return {
    ...summary,
    [previousKey]: Math.max(summary[previousKey] - 1, 0),
    [nextKey]: summary[nextKey] + 1
  };
}

function sortClients(clients: ClientLookup[]): ClientLookup[] {
  return [...clients].sort((left, right) => left.code.localeCompare(right.code));
}

function toClientEditForm(client: ClientLookup): UpdateClientInput {
  return {
    legalName: client.legalName,
    displayName: client.displayName
  };
}

function toProductGroupForm(group: ProductModuleGroup): ProductModuleGroupFormInput {
  return {
    groupId: group.groupId,
    displayName: group.displayName,
    accessKind: group.accessKind,
    moduleCodes: group.moduleCodes.join(", ")
  };
}

function toProductResourceForm(resource: ProductResource): ProductResourceFormInput {
  return {
    resourceId: resource.resourceId,
    displayName: resource.displayName,
    accessKind: resource.accessKind,
    requiredGroupIds: resource.requiredGroupIds.join(", "),
    requiredModuleCodes: resource.requiredModuleCodes.join(", ")
  };
}

function upsertCatalogItem<T>(
  items: T[],
  nextItem: T,
  getId: (item: T) => string,
  nextId: string
): T[] {
  const exists = items.some((item) => catalogIdEquals(getId(item), nextId));

  if (!exists) {
    return [...items, nextItem];
  }

  return items.map((item) => catalogIdEquals(getId(item), nextId) ? nextItem : item);
}

function sortProductGroups(groups: ProductModuleGroup[]): ProductModuleGroup[] {
  return [...groups].sort((left, right) => left.groupId.localeCompare(right.groupId));
}

function sortProductResources(resources: ProductResource[]): ProductResource[] {
  return [...resources].sort((left, right) => left.resourceId.localeCompare(right.resourceId));
}

function splitCatalogValues(value: string): string[] {
  const seen = new Set<string>();

  return value
    .split(/[\n,]/)
    .map((item) => item.trim())
    .filter((item) => {
      const key = item.toLowerCase();

      if (item === "" || seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
}

function catalogIdEquals(left: string, right: string): boolean {
  return left.trim().localeCompare(right.trim(), undefined, { sensitivity: "accent" }) === 0;
}

function requestedByOrDefault(value: string): string {
  const normalized = value.trim();

  return normalized === "" ? "Control Desk" : normalized;
}

function catalogWithReason(
  catalog: ProductAccessCatalog,
  changeReason: string
): ProductAccessCatalog {
  return {
    ...catalog,
    changeReason: changeReason.trim()
  };
}

function createEmptyProductCatalog(productModules: ProductModule[]): ProductAccessCatalog {
  return {
    state: "Published",
    catalogRevisionId: null,
    revisionNumber: null,
    supersedesCatalogRevisionId: null,
    draftId: null,
    baseCatalogRevisionId: null,
    baseCatalogRevisionNumber: null,
    changeReason: "",
    changedBy: "",
    changedAtUtc: null,
    modules: productModules,
    moduleGroups: [],
    resources: []
  };
}

function joinCatalogValues(values: string[]): string {
  return values.length === 0 ? "-" : values.join(", ");
}

function accessKindClass(accessKind: ProductAccessKind): string {
  return accessKind.replace(/[^a-z0-9]/gi, "").toLowerCase();
}

function deploymentDefaultStatusClass(status: DeploymentDefaultStatus): string {
  if (status === "Ready") {
    return "active";
  }

  if (status === "Partial") {
    return "pending";
  }

  return "draft";
}

function formatAccessKind(accessKind: ProductAccessKind): string {
  return accessKind.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/_/g, " ");
}

function equalsIgnoreCase(left: string, right: string): boolean {
  return left.localeCompare(right, undefined, { sensitivity: "accent" }) === 0;
}

function formatError(caughtError: unknown): string {
  if (caughtError instanceof ApiError) {
    return caughtError.errors[0]?.message ?? caughtError.message;
  }

  if (caughtError instanceof Error) {
    return caughtError.message;
  }

  return "Unable to load setup data.";
}
