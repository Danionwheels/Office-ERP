import { apiRequest } from "../../../shared/api/httpClient";
import type {
  EntitlementSnapshot,
  IssuedEntitlementSnapshot
} from "../types/entitlementTypes";

export async function getLatestEntitlementSnapshot(
  clientId: string
): Promise<EntitlementSnapshot> {
  return apiRequest<EntitlementSnapshot>(
    `/api/v1/entitlements/clients/${clientId}/latest-snapshot`
  );
}

export async function issueEntitlementFromPaidInvoiceDefaults(
  invoiceId: string
): Promise<IssuedEntitlementSnapshot> {
  return apiRequest<IssuedEntitlementSnapshot>(
    "/api/v1/entitlements/snapshots/from-paid-invoice/defaults",
    {
      method: "POST",
      body: JSON.stringify({
        invoiceId
      })
    }
  );
}
