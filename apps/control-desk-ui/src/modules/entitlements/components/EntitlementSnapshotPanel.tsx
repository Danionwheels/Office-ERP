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

function EntitlementModuleList({
  modules,
  productModules
}: {
  modules: EntitlementModule[];
  productModules: ProductModule[];
}) {
  if (modules.length === 0) {
    return (
      <div className="module-control-list">
        <span className="entitlement-module disabled">No modules</span>
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
            className={`module-control-item entitlement-module-item${
              module.isEnabled ? "" : " disabled"
            }`}
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
