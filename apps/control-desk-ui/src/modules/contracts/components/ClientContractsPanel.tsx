import {
  FilePlus2,
  FileText,
  History,
  PauseCircle,
  RefreshCw,
  type LucideIcon
} from "lucide-react";
import { type FormEvent, useState } from "react";
import type {
  ClientContract,
  ClientContractFormInput
} from "../types/contractTypes";

type ClientContractsPanelProps = {
  contracts: ClientContract[];
  value: ClientContractFormInput;
  isBusy: boolean;
  onChange: (value: ClientContractFormInput) => void;
  onCreate: () => Promise<void>;
  onReplaceActive: () => Promise<void>;
  onSuspend: (contractId: string) => Promise<void>;
};

type ContractWorkspaceView = "current" | "setup" | "history";

type ContractWorkspaceItem = {
  view: ContractWorkspaceView;
  label: string;
  summary: string;
  tone: "neutral" | "ready" | "warning";
  Icon: LucideIcon;
};

export function ClientContractsPanel({
  contracts,
  value,
  isBusy,
  onChange,
  onCreate,
  onReplaceActive,
  onSuspend
}: ClientContractsPanelProps) {
  const [activeView, setActiveView] = useState<ContractWorkspaceView>("current");
  const activeContract = getActiveContract(contracts);
  const workspaceItems = getContractWorkspaceItems(contracts, activeContract);
  const activeWorkspaceItem =
    workspaceItems.find((item) => item.view === activeView) ?? workspaceItems[0];

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreate();
  }

  async function handleReplaceActive() {
    await onReplaceActive();
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

              <div className="contract-module-list">
                {activeContract.modules.length === 0 && (
                  <span className="contract-module-pill disabled">No modules</span>
                )}
                {activeContract.modules.map((module) => (
                  <span
                    className={`contract-module-pill${module.isEnabled ? "" : " disabled"}`}
                    key={module.moduleCode}
                  >
                    {module.moduleCode}
                  </span>
                ))}
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
          <label className="form-field contract-modules-field">
            <span>Modules</span>
            <textarea
              rows={2}
              value={value.moduleCodes}
              onChange={(event) => onChange({ ...value, moduleCodes: event.target.value })}
              disabled={isBusy}
            />
          </label>
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
                  <dd>{enabledModules(contract)}</dd>
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
    </div>
  );
}

function getActiveContract(contracts: ClientContract[]): ClientContract | null {
  return contracts.find((contract) => contract.status.toLowerCase() === "active")
    ?? contracts[0]
    ?? null;
}

function getContractWorkspaceItems(
  contracts: ClientContract[],
  activeContract: ClientContract | null
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
    }
  ];
}

function enabledModules(contract: ClientContract): string {
  const modules = contract.modules
    .filter((module) => module.isEnabled)
    .map((module) => module.moduleCode);

  return modules.length === 0 ? "-" : modules.join(", ");
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
