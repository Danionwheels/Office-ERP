export type EntitlementModule = {
  moduleCode: string;
  isEnabled: boolean;
};

export type EntitlementSnapshot = {
  entitlementSnapshotId: string;
  clientId: string;
  contractId: string;
  status: string;
  paidUntil: string;
  graceUntil: string;
  offlineValidUntil: string;
  allowedDevices: number;
  allowedBranches: number;
  issuedAtUtc: string;
  modules: EntitlementModule[];
};

export type IssuedEntitlementSnapshot = EntitlementSnapshot & {
  invoiceId: string;
  invoiceNumber: string;
};
