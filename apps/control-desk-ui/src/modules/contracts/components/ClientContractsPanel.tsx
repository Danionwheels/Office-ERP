import { FilePlus2, FileText, PauseCircle, RefreshCw } from "lucide-react";
import { type FormEvent, useState } from "react";
import type {
  ClientContractsPanelProps,
  ContractWorkspaceView
} from "../types/contractWorkspaceTypes";
import {
  formatDate,
  formatDateTime,
  formatMoney,
  getActiveContract,
  getContractWorkspaceItems,
  isIncludedForAll,
  moduleCodesFromText,
  toggleModuleCode
} from "../utils/contractWorkspaceModel";
import { ProductAccessCatalogPanel } from "./ProductAccessCatalogPanel";
import {
  ContractHistoryList,
  ContractSetupForm,
  ContractWorkspaceHeader,
  ModulePlanList,
  ModulePlanReadinessPanel
} from "./shared/ContractWorkspace";

export function ClientContractsPanel({
  contracts,
  productModules,
  accessCatalog,
  accessCatalogRevisions,
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
  onPublishAccessCatalogRevision,
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
      <ContractWorkspaceHeader
        items={workspaceItems}
        activeView={activeView}
        activeLabel={activeWorkspaceItem.label}
        onViewChange={setActiveView}
      />

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
              <div className="contract-current-layout">
                <div className="contract-current-main">
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
                      <dt>Revision</dt>
                      <dd>#{activeContract.revisionNumber}</dd>
                    </div>
                    <div>
                      <dt>Product catalog</dt>
                      <dd>#{activeContract.productCatalogRevisionNumber}</dd>
                    </div>
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
                      <dt>Named users</dt>
                      <dd>{activeContract.allowedNamedUsers ?? "No cap"}</dd>
                    </div>
                    <div>
                      <dt>Concurrent users</dt>
                      <dd>{activeContract.allowedConcurrentUsers ?? "No cap"}</dd>
                    </div>
                    <div>
                      <dt>Activated</dt>
                      <dd>
                        {activeContract.activatedAtUtc === null || activeContract.activatedAtUtc === undefined
                          ? "-"
                          : formatDateTime(activeContract.activatedAtUtc)}
                      </dd>
                    </div>
                    <div>
                      <dt>Approved</dt>
                      <dd>{activeContract.approvedBy}</dd>
                    </div>
                  </dl>

                  <ModulePlanList
                    modules={activeContract.modules}
                    productModules={productModules}
                  />

                  {(activeContract.featureLimits ?? []).length > 0 && (
                    <div className="contract-feature-limit-summary">
                      <span>Feature limits</span>
                      <div>
                        {(activeContract.featureLimits ?? []).map((limit) => (
                          <strong key={`${limit.moduleCode}-${limit.featureCode}`}>
                            {limit.moduleCode}.{limit.featureCode}: {limit.limitValue} {limit.unit}
                          </strong>
                        ))}
                      </div>
                    </div>
                  )}
                </div>

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
              </div>

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
        <ContractSetupForm
          value={value}
          activeProductModules={activeProductModules}
          selectedModuleCodes={selectedModuleCodes}
          isBusy={isBusy}
          onSubmit={handleCreate}
          onChange={onChange}
          onReplaceActive={onReplaceActive}
          onModuleToggle={handleModuleToggle}
        />
      )}

      {activeView === "history" && (
        <ContractHistoryList
          contracts={contracts}
          productModules={productModules}
          isBusy={isBusy}
          onSuspend={onSuspend}
        />
      )}

      {activeView === "catalog" && (
        <ProductAccessCatalogPanel
          catalog={accessCatalog}
          revisions={accessCatalogRevisions}
          publishedCommand={publishedAccessCatalogCommand}
          value={accessCatalogPublishValue}
          isBusy={isBusy}
          onChange={onAccessCatalogPublishValueChange}
          onRefresh={onRefreshAccessCatalog}
          onSaveCatalog={onSaveAccessCatalog}
          onPublishRevision={onPublishAccessCatalogRevision}
          onPublish={onPublishAccessCatalog}
        />
      )}
    </div>
  );
}
