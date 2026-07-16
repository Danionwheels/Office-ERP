import {
  AlertTriangle,
  CheckCircle2,
  FilePlus2,
  FileText,
  History,
  KeyRound,
  ListChecks,
  PauseCircle,
  Plus,
  RefreshCw,
  Trash2,
  WalletCards,
  type LucideIcon
} from "lucide-react";
import type { FormEventHandler } from "react";
import type { ClientChargeRule } from "../../../billing/types/billingTypes";
import type { EntitlementSnapshot } from "../../../entitlements/types/entitlementTypes";
import type {
  ClientContract,
  ClientContractFormInput,
  ClientContractModule,
  ProductModule
} from "../../types/contractTypes";
import type {
  ContractWorkspaceItem,
  ContractWorkspaceView,
  ModuleReadinessIcon
} from "../../types/contractWorkspaceTypes";
import {
  enabledModules,
  formatDate,
  formatModuleNames,
  getContractReadinessModel
} from "../../utils/contractWorkspaceModel";
import {
  findProductModule,
  formatProductModuleBillingDefaults,
  formatProductModuleCommercialMode,
  getProductModuleDisplayName,
  getProductModuleMeta
} from "../../utils/productModuleDisplay";

const workspaceIcons: Record<ContractWorkspaceView, LucideIcon> = {
  current: FileText,
  setup: FilePlus2,
  history: History,
  catalog: ListChecks
};

const readinessIcons: Record<ModuleReadinessIcon, LucideIcon> = {
  modules: ListChecks,
  billing: WalletCards,
  entitlement: CheckCircle2
};

type ContractWorkspaceHeaderProps = {
  items: ContractWorkspaceItem[];
  activeView: ContractWorkspaceView;
  activeLabel: string;
  onViewChange: (view: ContractWorkspaceView) => void;
};

export function ContractWorkspaceHeader({
  items,
  activeView,
  activeLabel,
  onViewChange
}: ContractWorkspaceHeaderProps) {
  return (
    <header className="contract-workspace-header">
      <div className="contract-workspace-title">
        <span>Contracts</span>
        <h2>{activeLabel}</h2>
      </div>
      <div className="contract-summary-grid">
        {items.map((item) => {
          const Icon = workspaceIcons[item.view];

          return (
            <button
              className={`contract-summary-button ${item.tone}${
                activeView === item.view ? " active" : ""
              }`}
              key={item.view}
              type="button"
              onClick={() => onViewChange(item.view)}
            >
              <Icon size={16} />
              <span>
                <strong>{item.label}</strong>
                <small>{item.summary}</small>
              </span>
            </button>
          );
        })}
      </div>
    </header>
  );
}

type ContractSetupFormProps = {
  value: ClientContractFormInput;
  activeProductModules: ProductModule[];
  selectedModuleCodes: string[];
  isBusy: boolean;
  onSubmit: FormEventHandler<HTMLFormElement>;
  onChange: (value: ClientContractFormInput) => void;
  onReplaceActive: () => Promise<void>;
  onModuleToggle: (moduleCode: string, isSelected: boolean) => void;
};

export function ContractSetupForm({
  value,
  activeProductModules,
  selectedModuleCodes,
  isBusy,
  onSubmit,
  onChange,
  onReplaceActive,
  onModuleToggle
}: ContractSetupFormProps) {
  function addFeatureLimit() {
    onChange({
      ...value,
      featureLimits: [
        ...value.featureLimits,
        {
          moduleCode: selectedModuleCodes[0] ?? activeProductModules[0]?.moduleCode ?? "",
          featureCode: "",
          limitValue: "0",
          unit: "COUNT"
        }
      ]
    });
  }

  function updateFeatureLimit(
    index: number,
    patch: Partial<ClientContractFormInput["featureLimits"][number]>
  ) {
    onChange({
      ...value,
      featureLimits: value.featureLimits.map((limit, itemIndex) =>
        itemIndex === index ? { ...limit, ...patch } : limit)
    });
  }

  function removeFeatureLimit(index: number) {
    onChange({
      ...value,
      featureLimits: value.featureLimits.filter((_, itemIndex) => itemIndex !== index)
    });
  }

  return (
    <form className="client-panel client-contract-form contract-setup-panel" onSubmit={onSubmit}>
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
            onClick={onReplaceActive}
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
        <label className="form-field">
          <span>Named users</span>
          <input
            type="number"
            min="0"
            value={value.allowedNamedUsers}
            onChange={(event) => onChange({ ...value, allowedNamedUsers: event.target.value })}
            disabled={isBusy}
            placeholder="No cap"
          />
        </label>
        <label className="form-field">
          <span>Concurrent users</span>
          <input
            type="number"
            min="0"
            value={value.allowedConcurrentUsers}
            onChange={(event) => onChange({ ...value, allowedConcurrentUsers: event.target.value })}
            disabled={isBusy}
            placeholder="No cap"
          />
        </label>
        <label className="form-field contract-modules-field">
          <span>Approval reason</span>
          <input
            maxLength={1000}
            value={value.approvalReason}
            onChange={(event) => onChange({ ...value, approvalReason: event.target.value })}
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
                    onChange={(event) => onModuleToggle(module.moduleCode, event.target.checked)}
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

        <fieldset className="contract-feature-limits" disabled={isBusy}>
          <legend>Feature limits</legend>
          <div className="contract-feature-limit-heading">
            <span>{value.featureLimits.length} configured</span>
            <button className="mini-button" type="button" onClick={addFeatureLimit}>
              <Plus size={14} />
              Add
            </button>
          </div>
          {value.featureLimits.length === 0 ? (
            <div className="client-empty-state compact">No feature-specific limits</div>
          ) : (
            <div className="contract-feature-limit-list">
              {value.featureLimits.map((limit, index) => (
                <div
                  className="contract-feature-limit-row"
                  key={`${index}-${limit.moduleCode}-${limit.featureCode}`}
                >
                  <label className="form-field">
                    <span>Module</span>
                    <select
                      value={limit.moduleCode}
                      onChange={(event) => updateFeatureLimit(index, { moduleCode: event.target.value })}
                    >
                      {activeProductModules.map((module) => (
                        <option key={module.moduleCode} value={module.moduleCode}>
                          {module.displayName}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="form-field">
                    <span>Feature code</span>
                    <input
                      maxLength={64}
                      value={limit.featureCode}
                      onChange={(event) => updateFeatureLimit(index, {
                        featureCode: event.target.value.toUpperCase()
                      })}
                    />
                  </label>
                  <label className="form-field">
                    <span>Limit</span>
                    <input
                      min="0"
                      type="number"
                      value={limit.limitValue}
                      onChange={(event) => updateFeatureLimit(index, { limitValue: event.target.value })}
                    />
                  </label>
                  <label className="form-field">
                    <span>Unit</span>
                    <input
                      maxLength={32}
                      value={limit.unit}
                      onChange={(event) => updateFeatureLimit(index, {
                        unit: event.target.value.toUpperCase()
                      })}
                    />
                  </label>
                  <button
                    className="mini-button danger"
                    type="button"
                    onClick={() => removeFeatureLimit(index)}
                    title="Remove feature limit"
                  >
                    <Trash2 size={14} />
                    Remove
                  </button>
                </div>
              ))}
            </div>
          )}
        </fieldset>
      </div>
    </form>
  );
}

type ContractHistoryListProps = {
  contracts: ClientContract[];
  productModules: ProductModule[];
  isBusy: boolean;
  onSuspend: (contractId: string) => Promise<void>;
};

export function ContractHistoryList({
  contracts,
  productModules,
  isBusy,
  onSuspend
}: ContractHistoryListProps) {
  return (
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
                  Revision {contract.revisionNumber} / {formatDate(contract.startsOn)} to {formatDate(contract.endsOn)}
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
                  {contract.allowedDevices} devices, {contract.allowedBranches} branches, {contract.allowedNamedUsers ?? "no cap"} named, {contract.allowedConcurrentUsers ?? "no cap"} concurrent
                </dd>
              </div>
              <div>
                <dt>Feature limits</dt>
                <dd>
                  {(contract.featureLimits ?? []).length === 0
                    ? "None"
                    : (contract.featureLimits ?? [])
                      .map((limit) => `${limit.moduleCode}.${limit.featureCode} ${limit.limitValue} ${limit.unit}`)
                      .join(", ")}
                </dd>
              </div>
              <div>
                <dt>Modules</dt>
                <dd>{enabledModules(contract, productModules)}</dd>
              </div>
              <div>
                <dt>Product catalog</dt>
                <dd>Revision #{contract.productCatalogRevisionNumber}</dd>
              </div>
              <div>
                <dt>Approved</dt>
                <dd>{contract.approvedBy}</dd>
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
  );
}

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

export function ModulePlanReadinessPanel({
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
  const {
    readinessItems,
    paidAddOns,
    entitlementReadiness,
    hasEntitlementDetails,
    needsEntitlementAction
  } = getContractReadinessModel(
    contract,
    productModules,
    chargeRules,
    latestSnapshot,
    latestSnapshotMissing
  );

  return (
    <section className="module-readiness-panel">
      <div className="module-readiness-header">
        <span>Module control</span>
        <strong>Plan readiness</strong>
      </div>

      <div className="module-readiness-grid">
        {readinessItems.map((item) => {
          const Icon = item.icon === "entitlement" && item.tone === "warning"
            ? AlertTriangle
            : readinessIcons[item.icon];

          return (
            <article className={`module-readiness-card ${item.tone}`} key={item.label}>
              <Icon size={17} />
              <span>
                <strong>{item.label}</strong>
                <small>{item.summary}</small>
              </span>
            </article>
          );
        })}
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

export function ModulePlanList({
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
