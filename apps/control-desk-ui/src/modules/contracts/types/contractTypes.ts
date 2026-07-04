export type ClientContractModule = {
  moduleCode: string;
  isEnabled: boolean;
};

export type ProductModule = {
  moduleCode: string;
  displayName: string;
  commercialMode: "IncludedForAll" | "PaidAddOn" | string;
  isActive: boolean;
  billingDefaults?: ProductModuleBillingDefaults | null;
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
  moduleGroups: ProductModuleGroup[];
  resources: ProductResource[];
};

export type PublishProductAccessCatalogCommandInput = {
  activationRequestId: string;
  expiresInHours: string;
  requestedBy: string;
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
  contractNumber: string;
  startsOn: string;
  endsOn: string;
  recurringAmount: number;
  currencyCode: string;
  billingCycle: string;
  billingDayOfMonth: number;
  allowedDevices: number;
  allowedBranches: number;
  status: string;
  createdAtUtc: string;
  activatedAtUtc?: string | null;
  modules: ClientContractModule[];
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
  moduleCodes: string;
};

export type ReplaceActiveClientContractResult = {
  suspendedContract: ClientContract | null;
  activeContract: ClientContract;
};
