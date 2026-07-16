export type ClientContractModule = {
  moduleCode: string;
  isEnabled: boolean;
};

export type ProductModule = {
  moduleCode: string;
  displayName: string;
  description: string;
  commercialMode: "IncludedForAll" | "PaidAddOn" | string;
  isActive: boolean;
  billingDefaults?: ProductModuleBillingDefaults | null;
  compatibility: ProductModuleCompatibility;
  referencedBy: ProductModuleContractReference[];
};

export type ProductModuleContractReference = {
  contractId: string;
  contractNumber: string;
  contractRevisionNumber: number;
  clientId: string;
};

export type ClientContractFeatureLimit = {
  moduleCode: string;
  featureCode: string;
  limitValue: number;
  unit: string;
};

export type ClientContractFeatureLimitInput = {
  moduleCode: string;
  featureCode: string;
  limitValue: string;
  unit: string;
};

export type ProductModuleCompatibility = {
  minimumSafarSuiteVersion?: string | null;
  minimumLocalServerVersion?: string | null;
  supportedDeploymentModes: string[];
};

export type ProductModuleBillingDefaults = {
  chargeCode: string;
  chargeName: string;
  description: string;
  defaultUnitPriceAmount: number;
  currencyCode: string;
  billingCycle: string;
};

export type ProductAccessKind = "Public" | "CoreIncluded" | "PaidModule" | string;

export type ProductModuleGroup = {
  groupId: string;
  displayName: string;
  accessKind: ProductAccessKind;
  moduleCodes: string[];
};

export type ProductResource = {
  resourceId: string;
  displayName: string;
  accessKind: ProductAccessKind;
  requiredGroupIds: string[];
  requiredModuleCodes: string[];
  resolvedModuleCodes: string[];
};

export type ProductAccessCatalog = {
  state: "Draft" | "Published" | string;
  catalogRevisionId: string | null;
  revisionNumber: number | null;
  supersedesCatalogRevisionId: string | null;
  draftId: string | null;
  baseCatalogRevisionId: string | null;
  baseCatalogRevisionNumber: number | null;
  changeReason: string;
  changedBy: string;
  changedAtUtc: string | null;
  modules: ProductModule[];
  moduleGroups: ProductModuleGroup[];
  resources: ProductResource[];
};

export type PublishProductAccessCatalogCommandInput = {
  activationRequestId: string;
  expiresInHours: string;
  requestedBy: string;
  changeReason: string;
};

export type PublishedProductAccessCatalogCommand = {
  commandId: string;
  serverInstallationId: string;
  commandType: string;
  productKernelCommand: string;
  signature: string;
  signingKeyId: string;
  expiresAt: string;
  accessCatalog: ProductAccessCatalog;
};

export type ClientContract = {
  contractId: string;
  clientId: string;
  revisionNumber: number;
  supersedesContractId: string | null;
  productCatalogRevisionId: string;
  productCatalogRevisionNumber: number;
  contractNumber: string;
  startsOn: string;
  endsOn: string;
  recurringAmount: number;
  currencyCode: string;
  billingCycle: string;
  billingDayOfMonth: number;
  allowedDevices: number;
  allowedBranches: number;
  allowedNamedUsers: number | null;
  allowedConcurrentUsers: number | null;
  status: string;
  createdAtUtc: string;
  activatedAtUtc?: string | null;
  approvedBy: string;
  approvalReason: string;
  approvedAtUtc: string;
  modules: ClientContractModule[];
  featureLimits: ClientContractFeatureLimit[];
};

export type ClientContractFormInput = {
  contractNumber: string;
  startsOn: string;
  endsOn: string;
  recurringAmount: string;
  currencyCode: string;
  billingCycle: string;
  billingDayOfMonth: string;
  allowedDevices: string;
  allowedBranches: string;
  allowedNamedUsers: string;
  allowedConcurrentUsers: string;
  approvalReason: string;
  moduleCodes: string;
  featureLimits: ClientContractFeatureLimitInput[];
};

export type ReplaceActiveClientContractResult = {
  suspendedContract: ClientContract | null;
  activeContract: ClientContract;
};
