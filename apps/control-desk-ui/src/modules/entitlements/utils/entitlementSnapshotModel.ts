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
import type {
  EntitlementControlRow,
  EntitlementFact,
  EntitlementModuleRow
} from "../types/entitlementWorkspaceTypes";

export function canIssueEntitlementFromPaidInvoice({
  invoiceDraft,
  isBusy,
  recordedPayment
}: {
  invoiceDraft: InvoiceDraft | null;
  isBusy: boolean;
  recordedPayment: RecordedInvoicePayment | null;
}): boolean {
  return invoiceDraft !== null
    && invoiceDraft.status.toLowerCase() === "paid"
    && recordedPayment !== null
    && !isBusy;
}

export function getEntitlementControlRows({
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
      key: "invoice",
      label: "Invoice gate",
      status: invoiceDraft === null ? "No invoice" : invoiceDraft.status,
      detail: invoiceDraft?.status.toLowerCase() === "paid"
        ? "Paid invoice can issue a fresh entitlement snapshot"
        : "Payment must be complete before issue",
      tone: invoiceDraft?.status.toLowerCase() === "paid" ? "ready" : "warning"
    },
    {
      key: "receipt",
      label: "Receipt trail",
      status: recordedPayment === null ? "No receipt" : recordedPayment.paymentStatus,
      detail: recordedPayment === null
        ? "Recorded payment is required before issue"
        : "Payment record is available for entitlement issue",
      tone: recordedPayment === null ? "warning" : "ready"
    },
    {
      key: "snapshot",
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
      key: "limits",
      label: "Access limits",
      status: displaySnapshot === null
        ? "Pending"
        : `${displaySnapshot.allowedDevices} devices / ${displaySnapshot.allowedBranches} branches`,
      detail: displaySnapshot === null
        ? "Limits appear after a snapshot is loaded"
        : `${displaySnapshot.modules.filter((module) => module.isEnabled).length} modules / ${(displaySnapshot.featureLimits ?? []).length} feature limits`,
      tone: displaySnapshot === null ? "neutral" : "ready"
    },
    {
      key: "source",
      label: "Source",
      status: issuedSnapshot === null ? "Latest snapshot" : issuedSnapshot.invoiceNumber,
      detail: issuedSnapshot === null
        ? "Showing the latest loaded entitlement snapshot"
        : "Showing the snapshot just issued from the invoice",
      tone: "neutral"
    }
  ];
}

export function getEntitlementFacts(snapshot: EntitlementSnapshot): EntitlementFact[] {
  return [
    {
      label: "Access revision",
      value: `#${snapshot.entitlementVersion}`
    },
    {
      label: "Contract revision",
      value: `#${snapshot.contractRevisionNumber}`
    },
    {
      label: "Product catalog",
      value: `#${snapshot.productCatalogRevisionNumber}`
    },
    {
      label: "Approved by",
      value: snapshot.approvedBy
    },
    {
      label: "Paid until",
      value: formatDate(snapshot.paidUntil)
    },
    {
      label: "Grace until",
      value: formatDate(snapshot.graceUntil)
    },
    {
      label: "Offline valid",
      value: formatDate(snapshot.offlineValidUntil)
    },
    {
      label: "Devices",
      value: String(snapshot.allowedDevices)
    },
    {
      label: "Branches",
      value: String(snapshot.allowedBranches)
    },
    {
      label: "Named users",
      value: snapshot.allowedNamedUsers?.toString() ?? "No cap"
    },
    {
      label: "Concurrent users",
      value: snapshot.allowedConcurrentUsers?.toString() ?? "No cap"
    },
    {
      label: "Feature limits",
      value: String((snapshot.featureLimits ?? []).length)
    },
    {
      label: "Issued",
      value: formatDateTime(snapshot.issuedAtUtc)
    }
  ];
}

export function getEntitlementModuleRows(
  modules: EntitlementModule[],
  productModules: ProductModule[]
): EntitlementModuleRow[] {
  return modules.map((module) => {
    const productModule = findProductModule(productModules, module.moduleCode);

    return {
      moduleCode: module.moduleCode,
      displayName: getProductModuleDisplayName(productModules, module.moduleCode),
      meta: getProductModuleMeta(productModules, module.moduleCode),
      billingText: formatProductModuleBillingDefaults(productModule) ?? "Included or no billing rule",
      isEnabled: module.isEnabled
    };
  });
}

export function getEntitlementInvoiceCue(invoiceDraft: InvoiceDraft | null): string {
  if (invoiceDraft === null) {
    return "No invoice";
  }

  return invoiceDraft.status.toLowerCase() === "paid"
    ? "Paid invoice ready"
    : "Payment required";
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}
