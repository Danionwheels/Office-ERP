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
