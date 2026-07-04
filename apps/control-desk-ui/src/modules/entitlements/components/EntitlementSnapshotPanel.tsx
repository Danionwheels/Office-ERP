import { BadgeCheck, KeyRound } from "lucide-react";
import type { InvoiceDraft } from "../../billing/types/billingTypes";
import type { ProductModule } from "../../contracts/types/contractTypes";
import {
  findProductModule,
  formatProductModuleBillingDefaults,
  getProductModuleDisplayName,
  getProductModuleMeta
} from "../../contracts/utils/productModuleDisplay";
import type { RecordedInvoicePayment } from "../../payments/types/paymentTypes";
import type {
  EntitlementModule,
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "../types/entitlementTypes";

type EntitlementSnapshotPanelProps = {
  invoiceDraft: InvoiceDraft | null;
  recordedPayment: RecordedInvoicePayment | null;
  productModules: ProductModule[];
  latestSnapshot: EntitlementSnapshot | null;
  latestSnapshotMissing: boolean;
  issuedSnapshot: IssuedEntitlementSnapshot | null;
  isBusy: boolean;
  onIssueFromPaidInvoice: () => Promise<void>;
  onRefreshLatest: () => Promise<void>;
};

type EntitlementControlRow = {
  label: string;
  status: string;
  detail: string;
  tone: "neutral" | "ready" | "warning";
};

export function EntitlementSnapshotPanel({
  invoiceDraft,
  recordedPayment,
  productModules,
  latestSnapshot,
  latestSnapshotMissing,
  issuedSnapshot,
  isBusy,
  onIssueFromPaidInvoice,
  onRefreshLatest
}: EntitlementSnapshotPanelProps) {
  const displaySnapshot = issuedSnapshot ?? latestSnapshot;
  const canIssue =
    invoiceDraft !== null
    && invoiceDraft.status.toLowerCase() === "paid"
    && recordedPayment !== null
    && !isBusy;
  const controlRows = getEntitlementControlRows({
    displaySnapshot,
    invoiceDraft,
    issuedSnapshot,
    latestSnapshotMissing,
    recordedPayment
  });

  return (
    <section className="client-panel entitlement-panel">
      <div className="client-panel-heading">
        <div>
          <span>Entitlements</span>
          <strong>{displaySnapshot === null ? "Snapshot" : displaySnapshot.status}</strong>
        </div>
        {displaySnapshot !== null && (
          <span className={`status-pill ${displaySnapshot.status.toLowerCase()}`}>
            {displaySnapshot.status}
          </span>
        )}
      </div>

      <EntitlementControlRegister rows={controlRows} />

      <div className="entitlement-action-row">
        <button
          className="icon-button primary"
          type="button"
          disabled={!canIssue}
          onClick={onIssueFromPaidInvoice}
          title="Issue entitlement from paid invoice"
        >
          <KeyRound size={16} />
          Issue
        </button>
        <button
          className="icon-button"
          type="button"
          disabled={isBusy}
          onClick={onRefreshLatest}
          title="Refresh latest entitlement"
        >
          <BadgeCheck size={16} />
          Refresh
        </button>
        <span className="billing-small-fact">
          {invoiceDraft === null
            ? "No invoice"
            : invoiceDraft.status.toLowerCase() === "paid"
              ? "Paid invoice ready"
              : "Payment required"}
        </span>
      </div>

      {displaySnapshot === null ? (
        <div className="client-empty-state entitlement-empty">
          {latestSnapshotMissing ? "No entitlement snapshot" : "Latest snapshot not loaded"}
        </div>
      ) : (
        <>
          <dl className="entitlement-facts">
            <div>
              <dt>Paid until</dt>
              <dd>{formatDate(displaySnapshot.paidUntil)}</dd>
            </div>
            <div>
              <dt>Grace until</dt>
              <dd>{formatDate(displaySnapshot.graceUntil)}</dd>
            </div>
            <div>
              <dt>Offline valid</dt>
              <dd>{formatDate(displaySnapshot.offlineValidUntil)}</dd>
            </div>
            <div>
              <dt>Devices</dt>
              <dd>{displaySnapshot.allowedDevices}</dd>
            </div>
            <div>
              <dt>Branches</dt>
              <dd>{displaySnapshot.allowedBranches}</dd>
            </div>
            <div>
              <dt>Issued</dt>
              <dd>{formatDateTime(displaySnapshot.issuedAtUtc)}</dd>
            </div>
          </dl>

          <EntitlementModuleList
            modules={displaySnapshot.modules}
            productModules={productModules}
          />

          {issuedSnapshot !== null && (
            <div className="billing-small-fact">
              Source invoice {issuedSnapshot.invoiceNumber}
            </div>
          )}
        </>
      )}
    </section>
  );
}

function EntitlementControlRegister({ rows }: { rows: EntitlementControlRow[] }) {
  return (
    <div className="entitlement-control-register">
      <table className="entitlement-control-table" aria-label="Entitlement controls">
        <thead>
          <tr>
            <th scope="col">Control</th>
            <th scope="col">Status</th>
            <th scope="col">Operator cue</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr className={row.tone} key={row.label}>
              <td>
                <strong>{row.label}</strong>
              </td>
              <td>
                <span className={`entitlement-control-status ${row.tone}`}>
                  {row.status}
                </span>
              </td>
              <td>{row.detail}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function EntitlementModuleList({
  modules,
  productModules
}: {
  modules: EntitlementModule[];
  productModules: ProductModule[];
}) {
  if (modules.length === 0) {
    return (
      <div className="entitlement-module-register">
        <span className="entitlement-module-register-empty">No modules</span>
      </div>
    );
  }

  return (
    <div className="entitlement-module-register">
      <table className="entitlement-module-table" aria-label="Entitlement modules">
        <thead>
          <tr>
            <th scope="col">Module</th>
            <th scope="col">Access</th>
            <th scope="col">Commercial rule</th>
            <th scope="col">Catalog</th>
          </tr>
        </thead>
        <tbody>
          {modules.map((module) => {
            const productModule = findProductModule(productModules, module.moduleCode);
            const billingDefaults = formatProductModuleBillingDefaults(productModule);

            return (
              <tr className={module.isEnabled ? "enabled" : "disabled"} key={module.moduleCode}>
                <td>
                  <strong>{getProductModuleDisplayName(productModules, module.moduleCode)}</strong>
                  <small>{module.moduleCode}</small>
                </td>
                <td>
                  <span className={`entitlement-module-status${
                    module.isEnabled ? " enabled" : " disabled"
                  }`}
                  >
                    {module.isEnabled ? "Enabled" : "Disabled"}
                  </span>
                </td>
                <td>{billingDefaults ?? "Included or no billing rule"}</td>
                <td>{getProductModuleMeta(productModules, module.moduleCode)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function getEntitlementControlRows({
  displaySnapshot,
  invoiceDraft,
  issuedSnapshot,
  latestSnapshotMissing,
  recordedPayment
}: {
  displaySnapshot: EntitlementSnapshot | null;
  invoiceDraft: InvoiceDraft | null;
  issuedSnapshot: IssuedEntitlementSnapshot | null;
  latestSnapshotMissing: boolean;
  recordedPayment: RecordedInvoicePayment | null;
}): EntitlementControlRow[] {
  return [
    {
      label: "Invoice gate",
      status: invoiceDraft === null ? "No invoice" : invoiceDraft.status,
      detail: invoiceDraft?.status.toLowerCase() === "paid"
        ? "Paid invoice can issue a fresh entitlement snapshot"
        : "Payment must be complete before issue",
      tone: invoiceDraft?.status.toLowerCase() === "paid" ? "ready" : "warning"
    },
    {
      label: "Receipt trail",
      status: recordedPayment === null ? "No receipt" : recordedPayment.paymentStatus,
      detail: recordedPayment === null
        ? "Recorded payment is required before issue"
        : "Payment record is available for entitlement issue",
      tone: recordedPayment === null ? "warning" : "ready"
    },
    {
      label: "Snapshot",
      status: displaySnapshot === null
        ? latestSnapshotMissing ? "Missing" : "Not loaded"
        : displaySnapshot.status,
      detail: displaySnapshot === null
        ? "Refresh latest snapshot or issue from a paid invoice"
        : `Valid offline until ${formatDate(displaySnapshot.offlineValidUntil)}`,
      tone: displaySnapshot === null ? "warning" : "ready"
    },
    {
      label: "Access limits",
      status: displaySnapshot === null
        ? "Pending"
        : `${displaySnapshot.allowedDevices} devices / ${displaySnapshot.allowedBranches} branches`,
      detail: displaySnapshot === null
        ? "Limits appear after a snapshot is loaded"
        : `${displaySnapshot.modules.filter((module) => module.isEnabled).length} enabled modules in snapshot`,
      tone: displaySnapshot === null ? "neutral" : "ready"
    },
    {
      label: "Source",
      status: issuedSnapshot === null ? "Latest snapshot" : issuedSnapshot.invoiceNumber,
      detail: issuedSnapshot === null
        ? "Showing the latest loaded entitlement snapshot"
        : "Showing the snapshot just issued from the invoice",
      tone: "neutral"
    }
  ];
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
