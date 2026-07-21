import { KeyRound, RefreshCw } from "lucide-react";
import { useState } from "react";
import type { EntitlementSnapshotPanelProps } from "../types/entitlementWorkspaceTypes";
import {
  canIssueEntitlementFromPaidInvoice,
  getEntitlementControlRows,
  getEntitlementInvoiceCue
} from "../utils/entitlementSnapshotModel";
import {
  EntitlementControlBoard,
  EntitlementFeatureLimitRegister,
  EntitlementModuleRegister,
  EntitlementSnapshotSummary
} from "./shared/EntitlementSnapshotWorkspace";

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
  const [approvalReason, setApprovalReason] = useState(
    "Paid invoice and active contract verified in Control Desk."
  );
  const displaySnapshot = issuedSnapshot ?? latestSnapshot;
  const canIssue = canIssueEntitlementFromPaidInvoice({
    invoiceDraft,
    isBusy,
    recordedPayment
  });
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

      <div className="entitlement-action-row">
        <button
          className="icon-button primary"
          type="button"
          disabled={!canIssue || approvalReason.trim() === ""}
          onClick={() => onIssueFromPaidInvoice(approvalReason.trim())}
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
          <RefreshCw size={16} />
          Refresh
        </button>
        <label className="entitlement-approval-reason">
          <span>Approval reason</span>
          <input
            type="text"
            value={approvalReason}
            maxLength={1000}
            disabled={isBusy}
            onChange={(event) => setApprovalReason(event.target.value)}
          />
        </label>
        <span className="billing-small-fact">
          {getEntitlementInvoiceCue(invoiceDraft)}
        </span>
      </div>

      <EntitlementControlBoard rows={controlRows} />

      {displaySnapshot === null ? (
        <div className="client-empty-state entitlement-empty">
          {latestSnapshotMissing ? "No entitlement snapshot" : "Latest snapshot not loaded"}
        </div>
      ) : (
        <div className="entitlement-snapshot-layout">
          <EntitlementSnapshotSummary
            snapshot={displaySnapshot}
            sourceInvoiceNumber={issuedSnapshot?.invoiceNumber ?? null}
          />
          <EntitlementModuleRegister
            modules={displaySnapshot.modules}
            productModules={productModules}
          />
          <EntitlementFeatureLimitRegister featureLimits={displaySnapshot.featureLimits ?? []} />
        </div>
      )}
    </section>
  );
}
