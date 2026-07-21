import { apiRequest } from "../../../shared/api/httpClient";
import type {
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "../types/entitlementTypes";

const defaultApprovalReason = "Paid invoice and active contract verified in Control Desk.";

export async function getLatestEntitlementSnapshot(
  clientId: string
): Promise<EntitlementSnapshot> {
  return apiRequest<EntitlementSnapshot>(
    `/api/v1/entitlements/clients/${clientId}/latest-snapshot`
  );
}

export async function issueEntitlementFromPaidInvoiceDefaults(
  invoiceId: string,
  approvalReason = defaultApprovalReason,
  effectiveFromUtc: string | null = null
): Promise<IssuedEntitlementSnapshot> {
  return apiRequest<IssuedEntitlementSnapshot>(
    "/api/v1/entitlements/snapshots/from-paid-invoice/defaults",
    {
      method: "POST",
      body: JSON.stringify({
        invoiceId,
        approvalReason,
        effectiveFromUtc
      })
    }
  );
}
