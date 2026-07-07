import {
  Banknote,
  CheckCircle2,
  Cloud,
  FileText,
  KeyRound,
  LayoutDashboard,
  ListTree,
  ReceiptText,
  ScrollText,
  UserRound,
  Users
} from "lucide-react";
import type {
  InvoiceDraft,
  IssuedInvoice,
  ClientChargeRule
} from "../../billing/types/billingTypes";
import type { ClientContract, ProductModule } from "../../contracts/types/contractTypes";
import { findProductModule } from "../../contracts/utils/productModuleDisplay";
import type {
  ControlCloudInstallationStatus,
  LocalServerDeploymentProfile
} from "../../control-cloud/types/controlCloudTypes";
import type {
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "../../entitlements/types/entitlementTypes";
import type { RecordedInvoicePayment } from "../../payments/types/paymentTypes";
import type { ClientStatement } from "../../statements/types/statementTypes";
import type {
  ClientAccountingProfile,
  ClientDetails,
  ClientPortalInvitation
} from "../types/clientTypes";
import type {
  DashboardMetric,
  DashboardNavigationItem,
  DashboardModule,
  DashboardWorkQueueItem,
  DashboardWorkQueuePriority
} from "../types/clientDashboardTypes";

type DashboardMetricInput = {
  activeContract: ClientContract | null;
  accountCodeRangeCount: number;
  invoiceDraft: InvoiceDraft | null;
  recordedPayment: RecordedInvoicePayment | null;
  issuedEntitlementSnapshot: IssuedEntitlementSnapshot | null;
  latestEntitlementSnapshot: EntitlementSnapshot | null;
  cloudInstallationStatus: ControlCloudInstallationStatus | null;
  clientStatement: ClientStatement | null;
};

type DashboardWorkQueueInput = {
  clientCount: number;
  selectedClient: ClientDetails | null;
  activeContract: ClientContract | null;
  accountingProfile: ClientAccountingProfile | null;
  productModules: ProductModule[];
  chargeRules: ClientChargeRule[];
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
  recordedPayment: RecordedInvoicePayment | null;
  issuedEntitlementSnapshot: IssuedEntitlementSnapshot | null;
  latestEntitlementSnapshot: EntitlementSnapshot | null;
  latestEntitlementSnapshotMissing: boolean;
  cloudInstallationStatus: ControlCloudInstallationStatus | null;
  latestPortalInvitation: ClientPortalInvitation | null;
  portalInvitations: ClientPortalInvitation[];
  clientStatement: ClientStatement | null;
};

export function getDashboardWorkQueueItems({
  clientCount,
  selectedClient,
  activeContract,
  accountingProfile,
  productModules,
  chargeRules,
  invoiceDraft,
  issuedInvoice,
  recordedPayment,
  issuedEntitlementSnapshot,
  latestEntitlementSnapshot,
  latestEntitlementSnapshotMissing,
  cloudInstallationStatus,
  latestPortalInvitation,
  portalInvitations,
  clientStatement
}: DashboardWorkQueueInput): DashboardWorkQueueItem[] {
  if (selectedClient === null) {
    return [
      {
        key: "select-client",
        priority: "high",
        area: "Clients",
        label: "Select a client",
        detail: "Open the register before reviewing contracts, billing, payments, or cloud status.",
        status: `${clientCount} clients`,
        actionLabel: "Open register",
        module: "clients",
        Icon: Users
      }
    ];
  }

  const items: DashboardWorkQueueItem[] = [];
  const invoiceStatus = invoiceDraft?.status.toLowerCase() ?? "";
  const hasEntitlementSnapshot = issuedEntitlementSnapshot !== null || latestEntitlementSnapshot !== null;
  const statementBalance = getPrimaryStatementBalance(clientStatement);

  if (selectedClient.status.toLowerCase() !== "active") {
    items.push({
      key: "client-status",
      priority: "medium",
      area: "Profile",
      label: "Review client status",
      detail: "The client is not active, so lifecycle and access should be checked before billing work.",
      status: selectedClient.status,
      actionLabel: "Open profile",
      module: "profile",
      Icon: UserRound
    });
  }

  if (activeContract === null) {
    items.push({
      key: "contract-missing",
      priority: "high",
      area: "Contracts",
      label: "Create active contract",
      detail: "Billing, entitlements, and device limits need an active agreement.",
      status: "Missing",
      actionLabel: "Open contracts",
      module: "contracts",
      Icon: FileText
    });
  } else if (activeContract.status.toLowerCase() !== "active") {
    items.push({
      key: "contract-review",
      priority: "high",
      area: "Contracts",
      label: "Review contract status",
      detail: "The current agreement is not active, so downstream billing and access may be blocked.",
      status: activeContract.status,
      actionLabel: "Open contracts",
      module: "contracts",
      Icon: FileText
    });
  }

  if (accountingProfile === null) {
    items.push({
      key: "accounting-profile",
      priority: "high",
      area: "Billing",
      label: "Link accounting profile",
      detail: "Invoices need receivable account, currency, and cloud customer identity.",
      status: "Not linked",
      actionLabel: "Open billing",
      module: "billing",
      Icon: ReceiptText
    });
  }

  if (activeContract !== null && accountingProfile !== null) {
    const missingRuleCount = getMissingPaidAddOnRuleCount(activeContract, productModules, chargeRules);

    if (missingRuleCount > 0) {
      items.push({
        key: "billing-rules",
        priority: "medium",
        area: "Billing",
        label: "Complete paid add-on rules",
        detail: "Paid modules should have charge rules before invoice drafting.",
        status: `${missingRuleCount} missing`,
        actionLabel: "Open billing",
        module: "billing",
        Icon: ReceiptText
      });
    }

    if (invoiceDraft === null) {
      items.push({
        key: "invoice-draft",
        priority: "medium",
        area: "Billing",
        label: "Prepare invoice draft",
        detail: "The client has setup context but no current invoice draft loaded.",
        status: "No draft",
        actionLabel: "Open billing",
        module: "billing",
        Icon: ReceiptText
      });
    }
  }

  if (invoiceDraft !== null && invoiceStatus === "draft" && issuedInvoice === null) {
    items.push({
      key: "invoice-issue",
      priority: "high",
      area: "Billing",
      label: "Issue invoice",
      detail: "Draft is ready for posting once the invoice issue fields are complete.",
      status: invoiceDraft.invoiceNumber,
      actionLabel: "Open billing",
      module: "billing",
      Icon: ReceiptText
    });
  }

  if (recordedPayment?.paymentStatus === "PendingReview") {
    items.push({
      key: "payment-review",
      priority: "high",
      area: "Payments",
      label: "Review pending payment",
      detail: "Bank transfer receipts should be approved or rejected from payments.",
      status: recordedPayment.invoiceNumber,
      actionLabel: "Open payments",
      module: "payments",
      Icon: Banknote
    });
  } else if (
    invoiceDraft !== null
    && ["issued", "partiallypaid"].includes(invoiceStatus)
    && invoiceDraft.balanceDue > 0
  ) {
    items.push({
      key: "payment-due",
      priority: "medium",
      area: "Payments",
      label: "Record invoice payment",
      detail: "The issued invoice still has receivable balance outstanding.",
      status: `${invoiceDraft.balanceDue.toFixed(2)} ${invoiceDraft.currencyCode}`,
      actionLabel: "Open payments",
      module: "payments",
      Icon: Banknote
    });
  }

  if (invoiceStatus === "paid" && recordedPayment !== null && !hasEntitlementSnapshot) {
    items.push({
      key: "entitlement-issue",
      priority: "high",
      area: "Entitlements",
      label: "Issue entitlement snapshot",
      detail: "Paid invoice is available; cloud access should be synchronized.",
      status: "Not issued",
      actionLabel: "Open entitlements",
      module: "entitlements",
      Icon: KeyRound
    });
  } else if (latestEntitlementSnapshotMissing) {
    items.push({
      key: "entitlement-missing",
      priority: "medium",
      area: "Entitlements",
      label: "Refresh entitlement snapshot",
      detail: "The latest entitlement snapshot could not be found for this client.",
      status: "Missing",
      actionLabel: "Open entitlements",
      module: "entitlements",
      Icon: KeyRound
    });
  }

  if (cloudInstallationStatus === null) {
    items.push({
      key: "cloud-status",
      priority: "low",
      area: "Cloud",
      label: "Load cloud installation status",
      detail: "Heartbeat, license state, and command status have not been refreshed.",
      status: "Not loaded",
      actionLabel: "Open cloud",
      module: "cloud",
      Icon: Cloud
    });
  } else if (!isDashboardCloudStatusReady(cloudInstallationStatus)) {
    items.push({
      key: "cloud-review",
      priority: "medium",
      area: "Cloud",
      label: "Review cloud status",
      detail: "The latest installation state is not active, healthy, or registered.",
      status: cloudInstallationStatus.installationStatus,
      actionLabel: "Open cloud",
      module: "cloud",
      Icon: Cloud
    });
  }

  if (latestPortalInvitation === null && portalInvitations.length === 0) {
    items.push({
      key: "portal-invite",
      priority: "low",
      area: "Profile",
      label: "Invite portal contact",
      detail: "No client portal invitation is loaded for this client.",
      status: "No invite",
      actionLabel: "Open profile",
      module: "profile",
      Icon: UserRound
    });
  }

  if (statementBalance !== null && statementBalance.balanceDue > 0) {
    items.push({
      key: "statement-balance",
      priority: "low",
      area: "Statement",
      label: "Review receivable balance",
      detail: "Statement shows an outstanding client balance.",
      status: `${statementBalance.balanceDue.toFixed(2)} ${statementBalance.currencyCode}`,
      actionLabel: "Open statement",
      module: "statement",
      Icon: ScrollText
    });
  }

  if (items.length === 0) {
    return [
      {
        key: "client-clear",
        priority: "done",
        area: "Client",
        label: "No open control items",
        detail: "The selected client has no dashboard-level work requiring attention.",
        status: selectedClient.code,
        actionLabel: "View statement",
        module: "statement",
        Icon: CheckCircle2
      }
    ];
  }

  return items.sort(compareDashboardWorkQueueItems);
}

export function getDashboardMetrics({
  activeContract,
  accountCodeRangeCount,
  invoiceDraft,
  recordedPayment,
  issuedEntitlementSnapshot,
  latestEntitlementSnapshot,
  cloudInstallationStatus,
  clientStatement
}: DashboardMetricInput): DashboardMetric[] {
  const entitlementSnapshot = issuedEntitlementSnapshot ?? latestEntitlementSnapshot;
  const cloudHeartbeat = cloudInstallationStatus?.latestHeartbeat ?? null;
  const deploymentProfile = getCloudDeploymentProfile(cloudInstallationStatus);
  const deploymentSummary = formatCloudDeploymentSummary(deploymentProfile);
  const cloudStatus = cloudHeartbeat?.licenseStatus
    ?? cloudInstallationStatus?.installationStatus
    ?? "Not loaded";
  const normalizedCloudStatus = cloudStatus.toLowerCase();
  const primaryStatementSummary = clientStatement?.currencySummaries[0] ?? null;

  return [
    {
      label: "Contract",
      value: activeContract === null ? "Missing" : activeContract.status,
      summary: "Agreement, pricing, and allowances",
      tone: activeContract?.status.toLowerCase() === "active" ? "ready" : "warning",
      Icon: FileText,
      module: "contracts"
    },
    {
      label: "Accounting",
      value: accountCodeRangeCount === 0 ? "Not loaded" : `${accountCodeRangeCount} ranges`,
      summary: "COA setup and ledger register",
      tone: accountCodeRangeCount === 0 ? "warning" : "ready",
      Icon: Banknote,
      module: "accounting"
    },
    {
      label: "Invoice",
      value: invoiceDraft === null
        ? "No draft"
        : `${invoiceDraft.status} ${invoiceDraft.balanceDue.toFixed(2)} ${invoiceDraft.currencyCode}`,
      summary: "Draft, issue, and receivable state",
      tone: invoiceDraft?.status.toLowerCase() === "paid" ? "ready" : "neutral",
      Icon: ReceiptText,
      module: "billing"
    },
    {
      label: "Payment",
      value: recordedPayment === null ? "Pending" : recordedPayment.paymentStatus,
      summary: "Receipt posting and balance",
      tone: recordedPayment?.paymentStatus.toLowerCase() === "approved" ? "ready" : "neutral",
      Icon: CheckCircle2,
      module: "payments"
    },
    {
      label: "Entitlement",
      value: entitlementSnapshot === null ? "Not issued" : entitlementSnapshot.status,
      summary: "Cloud access snapshot",
      tone: entitlementSnapshot?.status.toLowerCase() === "active" ? "ready" : "neutral",
      Icon: KeyRound,
      module: "entitlements"
    },
    {
      label: "Cloud",
      value: cloudStatus,
      summary: cloudHeartbeat === null
        ? deploymentSummary
        : `${deploymentSummary} / ${formatDashboardDateTime(cloudHeartbeat.receivedAtUtc)}`,
      tone:
        normalizedCloudStatus === "active"
          || normalizedCloudStatus === "healthy"
          || normalizedCloudStatus === "registered"
          ? "ready"
          : cloudInstallationStatus === null
            ? "neutral"
            : "warning",
      Icon: Cloud,
      module: "cloud"
    },
    {
      label: "Statement",
      value: primaryStatementSummary === null
        ? "No balance"
        : `${primaryStatementSummary.balanceDue.toFixed(2)} ${primaryStatementSummary.currencyCode}`,
      summary: "Invoices, receipts, and GL trail",
      tone: primaryStatementSummary !== null && primaryStatementSummary.balanceDue === 0 ? "ready" : "neutral",
      Icon: ScrollText,
      module: "statement"
    }
  ];
}

export function getDashboardNavigation(
  metrics: DashboardMetric[],
  clientCount: number,
  selectedClient: ClientDetails | null
): DashboardNavigationItem[] {
  const contractMetric = findDashboardMetric(metrics, "Contract");
  const accountingMetric = findDashboardMetric(metrics, "Accounting");
  const invoiceMetric = findDashboardMetric(metrics, "Invoice");
  const paymentMetric = findDashboardMetric(metrics, "Payment");
  const entitlementMetric = findDashboardMetric(metrics, "Entitlement");
  const cloudMetric = findDashboardMetric(metrics, "Cloud");
  const statementMetric = findDashboardMetric(metrics, "Statement");
  const selectedClientStatus = selectedClient?.status ?? "No client";

  return [
    {
      module: "dashboard",
      label: "Dashboard",
      summary: "Current stats",
      description: "Current operational status for the selected client.",
      tone: "neutral",
      Icon: LayoutDashboard
    },
    {
      module: "clients",
      label: "Clients",
      summary: `${clientCount} total`,
      description: "Select, refresh, and quick add clients.",
      tone: selectedClient === null ? "warning" : "neutral",
      Icon: Users
    },
    {
      module: "profile",
      label: "Profile",
      summary: selectedClientStatus,
      description: "Client profile, contacts, support notes, and lifecycle actions.",
      tone: selectedClient?.status.toLowerCase() === "active" ? "ready" : "neutral",
      Icon: UserRound
    },
    {
      module: "contracts",
      label: "Contracts",
      summary: contractMetric.value,
      description: "Agreement terms, allowed modules, devices, branches, and contract replacement.",
      tone: contractMetric.tone,
      Icon: FileText
    },
    {
      module: "accounting",
      label: "Accounting",
      summary: accountingMetric.value,
      description: "General ledger, journals, reports, and reconciliation.",
      tone: accountingMetric.tone,
      Icon: ListTree
    },
    {
      module: "billing",
      label: "Billing",
      summary: invoiceMetric.value,
      description: "Accounting profile, charge rules, invoice drafts, and invoice issue.",
      tone: invoiceMetric.tone,
      Icon: ReceiptText
    },
    {
      module: "payments",
      label: "Payments",
      summary: paymentMetric.value,
      description: "Cash or bank account setup and invoice payment receipt.",
      tone: paymentMetric.tone,
      Icon: Banknote
    },
    {
      module: "entitlements",
      label: "Entitlements",
      summary: entitlementMetric.value,
      description: "Issue and refresh the latest cloud entitlement snapshot.",
      tone: entitlementMetric.tone,
      Icon: KeyRound
    },
    {
      module: "cloud",
      label: "Cloud",
      summary: cloudMetric.value,
      description: "Control Cloud heartbeat, license, entitlement, and command status.",
      tone: cloudMetric.tone,
      Icon: Cloud
    },
    {
      module: "statement",
      label: "Statement",
      summary: statementMetric.value,
      description: "Client invoices, payments, receivable balance, and journal postings.",
      tone: statementMetric.tone,
      Icon: ScrollText
    }
  ];
}

export function getDashboardNavigationItem(
  items: DashboardNavigationItem[],
  module: DashboardModule
): DashboardNavigationItem {
  return items.find((item) => item.module === module) ?? items[0] ?? {
    module: "dashboard",
    label: "Dashboard",
    summary: "Current stats",
    description: "Current operational status for the selected client.",
    tone: "neutral",
    Icon: LayoutDashboard
  };
}

export function formatDashboardQueuePriority(priority: DashboardWorkQueuePriority): string {
  switch (priority) {
    case "high":
      return "High";
    case "medium":
      return "Medium";
    case "low":
      return "Low";
    case "done":
      return "Clear";
    default:
      return "Review";
  }
}

export function getCloudDeploymentProfile(
  status: ControlCloudInstallationStatus | null
): LocalServerDeploymentProfile | null {
  return status?.deploymentProfile ?? status?.latestHeartbeat?.deploymentProfile ?? null;
}

function findDashboardMetric(metrics: DashboardMetric[], label: string): DashboardMetric {
  return metrics.find((metric) => metric.label === label) ?? {
    label,
    value: "Unknown",
    summary: "No signal",
    tone: "neutral",
    Icon: LayoutDashboard,
    module: "dashboard"
  };
}

function compareDashboardWorkQueueItems(
  left: DashboardWorkQueueItem,
  right: DashboardWorkQueueItem
): number {
  const priorityOrder: Record<DashboardWorkQueuePriority, number> = {
    high: 0,
    medium: 1,
    low: 2,
    done: 3
  };
  const priorityDifference = priorityOrder[left.priority] - priorityOrder[right.priority];

  return priorityDifference !== 0
    ? priorityDifference
    : left.area.localeCompare(right.area);
}

function getMissingPaidAddOnRuleCount(
  contract: ClientContract,
  productModules: ProductModule[],
  chargeRules: ClientChargeRule[]
): number {
  const enabledModuleCodes = getDashboardEnabledModuleCodes(contract.modules);
  const paidAddOnCodes = enabledModuleCodes.filter((moduleCode) =>
    findProductModule(productModules, moduleCode)?.commercialMode === "PaidAddOn"
  );
  const billedModuleCodes = new Set(
    chargeRules
      .filter((rule) => rule.status.toLowerCase() === "active")
      .filter((rule) => rule.contractId === undefined
        || rule.contractId === null
        || rule.contractId === contract.contractId)
      .map((rule) => normalizeDashboardModuleCode(rule.productModuleCode ?? ""))
      .filter((moduleCode) => moduleCode !== "")
  );

  return paidAddOnCodes.filter((moduleCode) => !billedModuleCodes.has(moduleCode)).length;
}

function getDashboardEnabledModuleCodes(
  modules: Array<{ moduleCode: string; isEnabled: boolean }>
): string[] {
  const seen = new Set<string>();

  return modules
    .filter((module) => module.isEnabled)
    .map((module) => normalizeDashboardModuleCode(module.moduleCode))
    .filter((moduleCode) => {
      if (moduleCode === "" || seen.has(moduleCode)) {
        return false;
      }

      seen.add(moduleCode);
      return true;
    });
}

function normalizeDashboardModuleCode(value: string): string {
  return value.trim().toUpperCase();
}

function isDashboardCloudStatusReady(status: ControlCloudInstallationStatus): boolean {
  const latestStatus = (
    status.latestHeartbeat?.licenseStatus
    ?? status.installationStatus
  ).toLowerCase();

  return latestStatus === "active"
    || latestStatus === "healthy"
    || latestStatus === "registered";
}

function getPrimaryStatementBalance(
  statement: ClientStatement | null
): ClientStatement["currencySummaries"][number] | null {
  return statement?.currencySummaries[0] ?? null;
}

function formatDashboardDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function formatCloudDeploymentSummary(profile: LocalServerDeploymentProfile | null): string {
  if (profile === null) {
    return "Install status";
  }

  const role = profile.siteRole.trim();
  const site = profile.branchCode?.trim() || profile.siteId.trim();
  const mode = profile.clientDeploymentMode.trim();

  if (role !== "" && site !== "") {
    return `${role} ${site}`;
  }

  return mode === "" ? "Install status" : mode;
}
