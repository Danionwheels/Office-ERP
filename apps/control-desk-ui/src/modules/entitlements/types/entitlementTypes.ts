export type EntitlementModule = {
  moduleCode: string;
  isEnabled: boolean;
};

export type EntitlementFeatureLimit = {
  moduleCode: string;
  featureCode: string;
  limitValue: number;
  unit: string;
};

export type EntitlementSnapshot = {
  entitlementSnapshotId: string;
  clientId: string;
  contractId: string;
  contractRevisionNumber: number;
  productCatalogRevisionId: string;
  productCatalogRevisionNumber: number;
  clientAccessRevisionId: string;
  entitlementVersion: number;
  status: string;
  paidUntil: string;
  graceUntil: string;
  offlineValidUntil: string;
  allowedDevices: number;
  allowedBranches: number;
  allowedNamedUsers: number | null;
  allowedConcurrentUsers: number | null;
  issuedAtUtc: string;
  effectiveFromUtc: string;
  supersedesClientAccessRevisionId: string | null;
  approvedBy: string;
  approvalReason: string;
  approvedAtUtc: string;
  modules: EntitlementModule[];
  featureLimits: EntitlementFeatureLimit[];
};

export type IssuedEntitlementSnapshot = EntitlementSnapshot & {
  invoiceId: string;
  invoiceNumber: string;
};
