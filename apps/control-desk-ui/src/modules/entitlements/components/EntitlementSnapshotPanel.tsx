import { BadgeCheck, KeyRound } from "lucide-react";
import type { InvoiceDraft } from "../../billing/types/billingTypes";
import type { RecordedInvoicePayment } from "../../payments/types/paymentTypes";
import type {
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "../types/entitlementTypes";

type EntitlementSnapshotPanelProps = {
  invoiceDraft: InvoiceDraft | null;
  recordedPayment: RecordedInvoicePayment | null;
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

          <div className="entitlement-module-list">
            {displaySnapshot.modules.map((module) => (
              <span
                className={`entitlement-module ${module.isEnabled ? "enabled" : "disabled"}`}
                key={module.moduleCode}
              >
                {module.moduleCode}
              </span>
            ))}
          </div>

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
