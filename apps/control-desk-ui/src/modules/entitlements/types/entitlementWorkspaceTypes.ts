import type { InvoiceDraft } from "../../billing/types/billingTypes";
import type { ProductModule } from "../../contracts/types/contractTypes";
import type { RecordedInvoicePayment } from "../../payments/types/paymentTypes";
import type {
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "./entitlementTypes";

export type EntitlementSnapshotPanelProps = {
  invoiceDraft: InvoiceDraft | null;
  recordedPayment: RecordedInvoicePayment | null;
  productModules: ProductModule[];
  latestSnapshot: EntitlementSnapshot | null;
  latestSnapshotMissing: boolean;
  issuedSnapshot: IssuedEntitlementSnapshot | null;
  isBusy: boolean;
  onIssueFromPaidInvoice: (approvalReason: string) => Promise<void>;
  onRefreshLatest: () => Promise<void>;
};

export type EntitlementTone = "neutral" | "ready" | "warning";

export type EntitlementControlKey = "invoice" | "receipt" | "snapshot" | "limits" | "source";

export type EntitlementControlRow = {
  key: EntitlementControlKey;
  label: string;
  status: string;
  detail: string;
  tone: EntitlementTone;
};

export type EntitlementFact = {
  label: string;
  value: string;
};

export type EntitlementModuleRow = {
  moduleCode: string;
  displayName: string;
  meta: string;
  billingText: string;
  isEnabled: boolean;
};
