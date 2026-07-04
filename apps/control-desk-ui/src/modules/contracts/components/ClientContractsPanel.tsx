import {
  AlertTriangle,
  CheckCircle2,
  FilePlus2,
  FileText,
  History,
  KeyRound,
  ListChecks,
  PauseCircle,
  RefreshCw,
  WalletCards,
  type LucideIcon
} from "lucide-react";
import { type FormEvent, useState } from "react";
import type { ClientChargeRule } from "../../billing/types/billingTypes";
import type { EntitlementSnapshot } from "../../entitlements/types/entitlementTypes";
import type {
  ClientContract,
  ClientContractModule,
  ClientContractFormInput,
  ProductAccessCatalog,
  PublishedProductAccessCatalogCommand,
  PublishProductAccessCatalogCommandInput,
  ProductModule
} from "../types/contractTypes";
import {
  findProductModule,
  formatProductModuleBillingDefaults,
  formatProductModuleCommercialMode,
  getProductModuleDisplayName,
  getProductModuleMeta,
  normalizeProductModuleCode
} from "../utils/productModuleDisplay";
import { ProductAccessCatalogPanel } from "./ProductAccessCatalogPanel";

type ClientContractsPanelProps = {
  contracts: ClientContract[];
  productModules: ProductModule[];
  accessCatalog: ProductAccessCatalog | null;
  publishedAccessCatalogCommand: PublishedProductAccessCatalogCommand | null;
  accessCatalogPublishValue: PublishProductAccessCatalogCommandInput;
  chargeRules: ClientChargeRule[];
  latestSnapshot: EntitlementSnapshot | null;
  latestSnapshotMissing: boolean;
  canIssueEntitlementSnapshot: boolean;
  value: ClientContractFormInput;
  isBusy: boolean;
  onChange: (value: ClientContractFormInput) => void;
  onAccessCatalogPublishValueChange: (value: PublishProductAccessCatalogCommandInput) => void;
  onRefreshAccessCatalog: () => Promise<void>;
  onSaveAccessCatalog: (catalog: ProductAccessCatalog, requestedBy: string) => Promise<void>;
  onPublishAccessCatalog: () => Promise<void>;
  onCreate: () => Promise<void>;
  onReplaceActive: () => Promise<void>;
  onSuspend: (contractId: string) => Promise<void>;
  onPrepareModuleBilling: (moduleCode: string) => void;
  onResolveEntitlementReadiness: () => Promise<void>;
};

type ContractWorkspaceView = "current" | "setup" | "history" | "catalog";

type ContractWorkspaceItem = {
  view: ContractWorkspaceView;
  label: string;
  summary: string;
  tone: "neutral" | "ready" | "warning";
  Icon: LucideIcon;
};

export function ClientContractsPanel({
  contracts,
  productModules,
  accessCatalog,
  publishedAccessCatalogCommand,
  accessCatalogPublishValue,
  chargeRules,
  latestSnapshot,
  latestSnapshotMissing,
  canIssueEntitlementSnapshot,
  value,
  isBusy,
  onChange,
  onAccessCatalogPublishValueChange,
  onRefreshAccessCatalog,
  onSaveAccessCatalog,
  onPublishAccessCatalog,
  onCreate,
  onReplaceActive,
  onSuspend,
  onPrepareModuleBilling,
  onResolveEntitlementReadiness
}: ClientContractsPanelProps) {
  const [activeView, setActiveView] = useState<ContractWorkspaceView>("current");
  const activeContract = getActiveContract(contracts);
  const workspaceItems = getContractWorkspaceItems(contracts, activeContract, accessCatalog);
  const activeWorkspaceItem =
    workspaceItems.find((item) => item.view === activeView) ?? workspaceItems[0];
  const activeProductModules = productModules.filter((module) => module.isActive);
  const selectedModuleCodes = moduleCodesFromText(value.moduleCodes);

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreate();
  }

  async function handleReplaceActive() {
    await onReplaceActive();
  }

  function handleModuleToggle(moduleCode: string, isSelected: boolean) {
    if (isIncludedForAll(moduleCode, activeProductModules)) {
      return;
    }

    onChange({
      ...value,
      moduleCodes: toggleModuleCode(value.moduleCodes, moduleCode, isSelected)
    });
  }

  return (
    <div className="client-contracts-zone contract-workspace">
      <header className="contract-workspace-header">
        <div>
          <span>Contracts</span>
          <h2>{activeWorkspaceItem.label}</h2>
        </div>
        <div className="contract-summary-grid">
          {workspaceItems.map((item) => (
            <button
              className={`contract-summary-button ${item.tone}${
                activeView === item.view ? " active" : ""
              }`}
              key={item.view}
              type="button"
              onClick={() => setActiveView(item.view)}
            >
              <item.Icon size={16} />
              <span>
                <strong>{item.label}</strong>
                <small>{item.summary}</small>
              </span>
            </button>
          ))}
        </div>
      </header>

      {activeView === "current" && (
        <section className="client-panel contract-current-panel">
          <div className="client-panel-heading">
            <div>
              <span>Contracts</span>
              <strong>Current contract</strong>
            </div>
            {activeContract !== null && (
              <span className={`status-pill ${activeContract.status.toLowerCase()}`}>
                {activeContract.status}
              </span>
            )}
          </div>

          {activeContract === null ? (
            <div className="contract-empty-state">
              <FileText size={18} />
              <span>No contract</span>
              <button className="mini-button" type="button" onClick={() => setActiveView("setup")}>
                <FilePlus2 size={14} />
                Create
              </button>
            </div>
          ) : (
            <>
              <div className="contract-current-hero">
                <div>
                  <span>{activeContract.contractNumber}</span>
                  <h3>{formatMoney(activeContract.recurringAmount, activeContract.currencyCode)}</h3>
                </div>
                <strong>
                  {activeContract.billingCycle}, day {activeContract.billingDayOfMonth}
                </strong>
              </div>

              <dl className="contract-fact-grid">
                <div>
                  <dt>Term</dt>
                  <dd>
                    {formatDate(activeContract.startsOn)} to {formatDate(activeContract.endsOn)}
                  </dd>
                </div>
                <div>
                  <dt>Devices</dt>
                  <dd>{activeContract.allowedDevices}</dd>
                </div>
                <div>
                  <dt>Branches</dt>
                  <dd>{activeContract.allowedBranches}</dd>
                </div>
                <div>
                  <dt>Activated</dt>
                  <dd>
                    {activeContract.activatedAtUtc === null || activeContract.activatedAtUtc === undefined
                      ? "-"
                      : formatDateTime(activeContract.activatedAtUtc)}
                  </dd>
                </div>
              </dl>

              <ModulePlanList
                modules={activeContract.modules}
                productModules={productModules}
              />

              <ModulePlanReadinessPanel
                contract={activeContract}
                productModules={productModules}
                chargeRules={chargeRules}
                latestSnapshot={latestSnapshot}
                latestSnapshotMissing={latestSnapshotMissing}
                canIssueEntitlementSnapshot={canIssueEntitlementSnapshot}
                isBusy={isBusy}
                onPrepareModuleBilling={onPrepareModuleBilling}
                onResolveEntitlementReadiness={onResolveEntitlementReadiness}
              />

              <div className="contract-current-actions">
                <button
                  className="mini-button"
                  type="button"
                  onClick={() => setActiveView("setup")}
                  disabled={isBusy}
                >
                  <RefreshCw size={14} />
                  Replace
                </button>
                <button
                  className="mini-button"
                  type="button"
                  onClick={() => onSuspend(activeContract.contractId)}
                  disabled={isBusy || activeContract.status.toLowerCase() !== "active"}
                  title="Suspend contract"
                >
                  <PauseCircle size={14} />
                  Suspend
                </button>
              </div>
            </>
          )}
        </section>
      )}

      {activeView === "setup" && (
        <form className="client-panel client-contract-form contract-setup-panel" onSubmit={handleCreate}>
        <div className="client-panel-heading">
          <div>
            <span>Contracts</span>
            <strong>New contract</strong>
          </div>
          <div className="client-panel-actions">
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Create contract">
              <FilePlus2 size={16} />
              Create
            </button>
            <button
              className="icon-button"
              type="button"
              disabled={isBusy}
              onClick={handleReplaceActive}
              title="Replace active contract"
            >
              <RefreshCw size={16} />
              Replace
            </button>
          </div>
        </div>

        <div className="contract-form-grid">
          <label className="form-field">
            <span>Contract #</span>
            <input
              value={value.contractNumber}
              onChange={(event) => onChange({ ...value, contractNumber: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Start</span>
            <input
              type="date"
              value={value.startsOn}
              onChange={(event) => onChange({ ...value, startsOn: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>End</span>
            <input
              type="date"
              value={value.endsOn}
              onChange={(event) => onChange({ ...value, endsOn: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Amount</span>
            <input
              type="number"
              min="0"
              step="0.01"
              value={value.recurringAmount}
              onChange={(event) => onChange({ ...value, recurringAmount: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Currency</span>
            <input
              value={value.currencyCode}
              onChange={(event) => onChange({ ...value, currencyCode: event.target.value.toUpperCase() })}
              disabled={isBusy}
              maxLength={3}
            />
          </label>
          <label className="form-field">
            <span>Cycle</span>
            <select
              value={value.billingCycle}
              onChange={(event) => onChange({ ...value, billingCycle: event.target.value })}
              disabled={isBusy}
            >
              <option value="Monthly">Monthly</option>
              <option value="Quarterly">Quarterly</option>
              <option value="SemiAnnual">SemiAnnual</option>
              <option value="Annual">Annual</option>
            </select>
          </label>
          <label className="form-field">
            <span>Bill day</span>
            <input
              type="number"
              min="1"
              max="28"
              value={value.billingDayOfMonth}
              onChange={(event) => onChange({ ...value, billingDayOfMonth: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Devices</span>
            <input
              type="number"
              min="0"
              value={value.allowedDevices}
              onChange={(event) => onChange({ ...value, allowedDevices: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Branches</span>
            <input
              type="number"
              min="0"
              value={value.allowedBranches}
              onChange={(event) => onChange({ ...value, allowedBranches: event.target.value })}
              disabled={isBusy}
            />
          </label>
          {activeProductModules.length === 0 ? (
            <label className="form-field contract-modules-field">
              <span>Modules</span>
              <textarea
                rows={2}
                value={value.moduleCodes}
                onChange={(event) => onChange({ ...value, moduleCodes: event.target.value })}
                disabled={isBusy}
              />
            </label>
          ) : (
            <fieldset className="contract-module-catalog" disabled={isBusy}>
              <legend>Modules</legend>
              {activeProductModules.map((module) => {
                const isIncluded = module.commercialMode === "IncludedForAll";
                const billingDefaults = formatProductModuleBillingDefaults(module);

                return (
                  <label className="contract-module-option" key={module.moduleCode}>
                    <input
                      type="checkbox"
                      checked={isIncluded || selectedModuleCodes.includes(module.moduleCode)}
                      disabled={isIncluded}
                      onChange={(event) => handleModuleToggle(module.moduleCode, event.target.checked)}
                    />
                    <span>
                      <strong>{module.displayName}</strong>
                      <small>
                        {module.moduleCode} - {formatProductModuleCommercialMode(module.commercialMode)}
                      </small>
                      {billingDefaults !== null && <small>{billingDefaults}</small>}
                    </span>
                  </label>
                );
              })}
            </fieldset>
          )}
        </div>
      </form>
      )}

      {activeView === "history" && (
        <div className="client-panel client-contract-list-panel contract-history-panel">
        <div className="client-panel-heading">
          <div>
            <span>Contracts</span>
            <strong>{contracts.length}</strong>
          </div>
        </div>
        <div className="contract-list">
          {contracts.length === 0 && <div className="client-empty-state">No contracts</div>}
          {contracts.map((contract) => (
            <article className="contract-item" key={contract.contractId}>
              <header>
                <div>
                  <strong>{contract.contractNumber}</strong>
                  <span>
                    {formatDate(contract.startsOn)} to {formatDate(contract.endsOn)}
                  </span>
                </div>
                <span className={`status-pill ${contract.status.toLowerCase()}`}>{contract.status}</span>
              </header>
              <dl>
                <div>
                  <dt>Amount</dt>
                  <dd>
                    {contract.recurringAmount.toFixed(2)} {contract.currencyCode}
                  </dd>
                </div>
                <div>
                  <dt>Cycle</dt>
                  <dd>
                    {contract.billingCycle}, day {contract.billingDayOfMonth}
                  </dd>
                </div>
                <div>
                  <dt>Limits</dt>
                  <dd>
                    {contract.allowedDevices} devices, {contract.allowedBranches} branches
                  </dd>
                </div>
                <div>
                  <dt>Modules</dt>
                  <dd>{enabledModules(contract, productModules)}</dd>
                </div>
              </dl>
              <div className="contract-actions">
                <button
                  className="mini-button"
                  type="button"
                  onClick={() => onSuspend(contract.contractId)}
                  disabled={isBusy || contract.status.toLowerCase() !== "active"}
                  title="Suspend contract"
                >
                  <PauseCircle size={14} />
                  Suspend
                </button>
              </div>
            </article>
          ))}
        </div>
      </div>
      )}

      {activeView === "catalog" && (
        <ProductAccessCatalogPanel
          catalog={accessCatalog}
          publishedCommand={publishedAccessCatalogCommand}
          value={accessCatalogPublishValue}
          isBusy={isBusy}
          onChange={onAccessCatalogPublishValueChange}
          onRefresh={onRefreshAccessCatalog}
          onSaveCatalog={onSaveAccessCatalog}
          onPublish={onPublishAccessCatalog}
        />
      )}
    </div>
  );
}

type ReadinessTone = "neutral" | "ready" | "warning";

type ModulePlanReadinessPanelProps = {
  contract: ClientContract;
  productModules: ProductModule[];
  chargeRules: ClientChargeRule[];
  latestSnapshot: EntitlementSnapshot | null;
  latestSnapshotMissing: boolean;
  canIssueEntitlementSnapshot: boolean;
  isBusy: boolean;
  onPrepareModuleBilling: (moduleCode: string) => void;
  onResolveEntitlementReadiness: () => Promise<void>;
};

type ModuleReadinessItem = {
  label: string;
  summary: string;
  tone: ReadinessTone;
  Icon: LucideIcon;
};

type PaidAddOnReadiness = {
  moduleCode: string;
  displayName: string;
  meta: string;
  billingDefaults: string | null;
  hasChargeRule: boolean;
};

type EntitlementReadiness = {
  label: string;
  summary: string;
  tone: ReadinessTone;
  missingModuleCodes: string[];
  extraModuleCodes: string[];
  hasLimitMismatch: boolean;
  hasContractMismatch: boolean;
};

function ModulePlanReadinessPanel({
  contract,
  productModules,
  chargeRules,
  latestSnapshot,
  latestSnapshotMissing,
  canIssueEntitlementSnapshot,
  isBusy,
  onPrepareModuleBilling,
  onResolveEntitlementReadiness
}: ModulePlanReadinessPanelProps) {
  const enabledModuleCodes = getEnabledModuleCodes(contract.modules);
  const includedModuleCount = enabledModuleCodes.filter((moduleCode) =>
    findProductModule(productModules, moduleCode)?.commercialMode === "IncludedForAll"
  ).length;
  const paidAddOns = getPaidAddOnReadiness(contract, productModules, chargeRules);
  const unmatchedPaidAddOns = paidAddOns.filter((module) => !module.hasChargeRule);
  const entitlementReadiness = getEntitlementReadiness(
    contract,
    productModules,
    latestSnapshot,
    latestSnapshotMissing
  );
  const readinessItems: ModuleReadinessItem[] = [
    {
      label: "Contract modules",
      summary: enabledModuleCodes.length === 0
        ? "No modules enabled"
        : `${enabledModuleCodes.length} enabled, ${includedModuleCount} included`,
      tone: enabledModuleCodes.length === 0 ? "warning" : "ready",
      Icon: ListChecks
    },
    {
      label: "Billing rules",
      summary: paidAddOns.length === 0
        ? "No add-ons enabled"
        : unmatchedPaidAddOns.length === 0
          ? `${paidAddOns.length} add-ons covered`
          : `${unmatchedPaidAddOns.length} add-ons missing`,
      tone: paidAddOns.length === 0
        ? "neutral"
        : unmatchedPaidAddOns.length === 0
          ? "ready"
          : "warning",
      Icon: WalletCards
    },
    {
      label: entitlementReadiness.label,
      summary: entitlementReadiness.summary,
      tone: entitlementReadiness.tone,
      Icon: entitlementReadiness.tone === "ready" ? CheckCircle2 : AlertTriangle
    }
  ];
  const hasEntitlementDetails =
    entitlementReadiness.hasContractMismatch
    || entitlementReadiness.hasLimitMismatch
    || entitlementReadiness.missingModuleCodes.length > 0
    || entitlementReadiness.extraModuleCodes.length > 0;
  const needsEntitlementAction = entitlementReadiness.tone === "warning";

  return (
    <section className="module-readiness-panel">
      <div className="module-readiness-header">
        <span>Module control</span>
        <strong>Plan readiness</strong>
      </div>

      <div className="module-readiness-grid">
        {readinessItems.map((item) => (
          <article className={`module-readiness-card ${item.tone}`} key={item.label}>
            <item.Icon size={17} />
            <span>
              <strong>{item.label}</strong>
              <small>{item.summary}</small>
            </span>
          </article>
        ))}
      </div>

      {paidAddOns.length > 0 && (
        <div className="module-readiness-module-list">
          {paidAddOns.map((module) => (
            <article
              className={`module-readiness-module ${module.hasChargeRule ? "ready" : "warning"}`}
              key={module.moduleCode}
            >
              <span>
                <strong>{module.displayName}</strong>
                <small>{module.meta}</small>
                {module.billingDefaults !== null && <small>{module.billingDefaults}</small>}
              </span>
              <div className="module-readiness-module-actions">
                <em>{module.hasChargeRule ? "Billed" : "Missing"}</em>
                {!module.hasChargeRule && (
                  <button
                    className="mini-button module-readiness-action"
                    type="button"
                    disabled={isBusy}
                    onClick={() => onPrepareModuleBilling(module.moduleCode)}
                    title="Prepare billing rule"
                  >
                    <FilePlus2 size={13} />
                    Set up
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>
      )}

      {(needsEntitlementAction || hasEntitlementDetails) && (
        <div className="module-readiness-details">
          {needsEntitlementAction && (
            <div className="module-readiness-detail-action">
              <span>
                <strong>Entitlement snapshot</strong>
                <small>{canIssueEntitlementSnapshot ? "Paid invoice ready" : "Paid invoice required"}</small>
              </span>
              <button
                className="mini-button module-readiness-action"
                type="button"
                disabled={isBusy}
                onClick={onResolveEntitlementReadiness}
                title={canIssueEntitlementSnapshot
                  ? "Issue entitlement snapshot"
                  : "Open entitlement workflow"}
              >
                <KeyRound size={13} />
                {canIssueEntitlementSnapshot ? "Issue" : "Open"}
              </button>
            </div>
          )}
          {entitlementReadiness.hasContractMismatch && <small>Contract changed since latest snapshot</small>}
          {entitlementReadiness.hasLimitMismatch && <small>Device or branch limits differ</small>}
          {entitlementReadiness.missingModuleCodes.length > 0 && (
            <small>
              Missing in snapshot: {formatModuleNames(
                entitlementReadiness.missingModuleCodes,
                productModules
              )}
            </small>
          )}
          {entitlementReadiness.extraModuleCodes.length > 0 && (
            <small>
              Extra in snapshot: {formatModuleNames(
                entitlementReadiness.extraModuleCodes,
                productModules
              )}
            </small>
          )}
        </div>
      )}
    </section>
  );
}

function ModulePlanList({
  modules,
  productModules
}: {
  modules: ClientContractModule[];
  productModules: ProductModule[];
}) {
  if (modules.length === 0) {
    return (
      <div className="module-control-list">
        <span className="contract-module-pill disabled">No modules</span>
      </div>
    );
  }

  return (
    <div className="module-control-list">
      {modules.map((module) => {
        const productModule = findProductModule(productModules, module.moduleCode);
        const billingDefaults = formatProductModuleBillingDefaults(productModule);

        return (
          <article
            className={`module-control-item${module.isEnabled ? "" : " disabled"}`}
            key={module.moduleCode}
          >
            <header>
              <span>
                <strong>{getProductModuleDisplayName(productModules, module.moduleCode)}</strong>
                <small>{getProductModuleMeta(productModules, module.moduleCode)}</small>
              </span>
              <em>{module.isEnabled ? "Enabled" : "Disabled"}</em>
            </header>
            {billingDefaults !== null && <p>{billingDefaults}</p>}
          </article>
        );
      })}
    </div>
  );
}

function getPaidAddOnReadiness(
  contract: ClientContract,
  productModules: ProductModule[],
  chargeRules: ClientChargeRule[]
): PaidAddOnReadiness[] {
  const billedModuleCodes = getBilledModuleCodes(chargeRules, contract);

  return getEnabledModuleCodes(contract.modules)
    .filter((moduleCode) =>
      findProductModule(productModules, moduleCode)?.commercialMode === "PaidAddOn"
    )
    .map((moduleCode) => {
      const productModule = findProductModule(productModules, moduleCode);

      return {
        moduleCode,
        displayName: getProductModuleDisplayName(productModules, moduleCode),
        meta: getProductModuleMeta(productModules, moduleCode),
        billingDefaults: formatProductModuleBillingDefaults(productModule),
        hasChargeRule: billedModuleCodes.has(moduleCode)
      };
    });
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

function getEntitlementReadiness(
  contract: ClientContract,
  productModules: ProductModule[],
  latestSnapshot: EntitlementSnapshot | null,
  latestSnapshotMissing: boolean
): EntitlementReadiness {
  if (latestSnapshot === null) {
    return {
      label: "Entitlement",
      summary: latestSnapshotMissing ? "No snapshot issued" : "Not loaded",
      tone: "warning",
      missingModuleCodes: [],
      extraModuleCodes: [],
      hasLimitMismatch: false,
      hasContractMismatch: false
    };
  }

  const contractModuleCodes = getEnabledModuleCodes(contract.modules);
  const snapshotModuleCodes = getEnabledModuleCodes(latestSnapshot.modules);
  const missingModuleCodes = contractModuleCodes.filter(
    (moduleCode) => !snapshotModuleCodes.includes(moduleCode)
  );
  const extraModuleCodes = snapshotModuleCodes.filter(
    (moduleCode) => !contractModuleCodes.includes(moduleCode)
  );
  const hasContractMismatch = latestSnapshot.contractId !== contract.contractId;
  const hasLimitMismatch =
    latestSnapshot.allowedDevices !== contract.allowedDevices
    || latestSnapshot.allowedBranches !== contract.allowedBranches;

  if (
    !hasContractMismatch
    && !hasLimitMismatch
    && missingModuleCodes.length === 0
    && extraModuleCodes.length === 0
  ) {
    return {
      label: "Entitlement",
      summary: `${snapshotModuleCodes.length} modules aligned`,
      tone: "ready",
      missingModuleCodes,
      extraModuleCodes,
      hasLimitMismatch,
      hasContractMismatch
    };
  }

  const differences = [
    hasContractMismatch ? "contract changed" : null,
    hasLimitMismatch ? "limits differ" : null,
    missingModuleCodes.length > 0 ? `${missingModuleCodes.length} missing` : null,
    extraModuleCodes.length > 0 ? `${extraModuleCodes.length} extra` : null
  ].filter((item): item is string => item !== null);

  return {
    label: "Entitlement",
    summary: differences.join(", "),
    tone: "warning",
    missingModuleCodes,
    extraModuleCodes,
    hasLimitMismatch,
    hasContractMismatch
  };
}

function getActiveContract(contracts: ClientContract[]): ClientContract | null {
  return contracts.find((contract) => contract.status.toLowerCase() === "active")
    ?? contracts[0]
    ?? null;
}

function getContractWorkspaceItems(
  contracts: ClientContract[],
  activeContract: ClientContract | null,
  accessCatalog: ProductAccessCatalog | null
): ContractWorkspaceItem[] {
  return [
    {
      view: "current",
      label: "Current contract",
      summary: activeContract === null ? "Missing" : activeContract.status,
      tone: activeContract?.status.toLowerCase() === "active" ? "ready" : "warning",
      Icon: FileText
    },
    {
      view: "setup",
      label: "Create / Replace",
      summary: activeContract === null ? "Create first" : "Ready",
      tone: activeContract === null ? "warning" : "neutral",
      Icon: FilePlus2
    },
    {
      view: "history",
      label: "History",
      summary: `${contracts.length} contracts`,
      tone: contracts.length === 0 ? "neutral" : "ready",
      Icon: History
    },
    {
      view: "catalog",
      label: "Access catalog",
      summary: accessCatalog === null ? "Not loaded" : `${accessCatalog.moduleGroups.length} groups`,
      tone: accessCatalog === null ? "neutral" : "ready",
      Icon: ListChecks
    }
  ];
}

function enabledModules(contract: ClientContract, productModules: ProductModule[]): string {
  const modules = contract.modules
    .filter((module) => module.isEnabled)
    .map((module) => getProductModuleDisplayName(productModules, module.moduleCode));

  return modules.length === 0 ? "-" : modules.join(", ");
}

function getEnabledModuleCodes(modules: Array<{ moduleCode: string; isEnabled: boolean }>): string[] {
  const seen = new Set<string>();

  return modules
    .filter((module) => module.isEnabled)
    .map((module) => normalizeProductModuleCode(module.moduleCode))
    .filter((moduleCode) => {
      if (moduleCode === "" || seen.has(moduleCode)) {
        return false;
      }

      seen.add(moduleCode);
      return true;
    });
}

function formatModuleNames(moduleCodes: string[], productModules: ProductModule[]): string {
  return moduleCodes
    .map((moduleCode) => getProductModuleDisplayName(productModules, moduleCode))
    .join(", ");
}

function moduleCodesFromText(value: string): string[] {
  const seen = new Set<string>();

  return value
    .split(/[\n,]/)
    .map((item) => item.trim().toUpperCase())
    .filter((item) => {
      if (item === "" || seen.has(item)) {
        return false;
      }

      seen.add(item);
      return true;
    });
}

function toggleModuleCode(value: string, moduleCode: string, isSelected: boolean): string {
  const normalizedModuleCode = moduleCode.trim().toUpperCase();
  const moduleCodes = moduleCodesFromText(value);
  const nextModuleCodes = isSelected
    ? [...moduleCodes, normalizedModuleCode]
    : moduleCodes.filter((item) => item !== normalizedModuleCode);

  return [...new Set(nextModuleCodes)].join(", ");
}

function isIncludedForAll(moduleCode: string, productModules: ProductModule[]): boolean {
  const normalizedModuleCode = moduleCode.trim().toUpperCase();

  return productModules.some(
    (module) =>
      module.moduleCode === normalizedModuleCode && module.commercialMode === "IncludedForAll"
  );
}

function normalizeOptionalModuleCode(value: string | null | undefined): string | null {
  if (value === null || value === undefined) {
    return null;
  }

  const normalizedModuleCode = normalizeProductModuleCode(value);

  return normalizedModuleCode === "" ? null : normalizedModuleCode;
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}
