import type { ClientChargeRule } from "../../billing/types/billingTypes";
import type { EntitlementSnapshot } from "../../entitlements/types/entitlementTypes";
import type {
  ClientContract,
  ClientContractFormInput,
  ProductAccessCatalog,
  PublishedProductAccessCatalogCommand,
  PublishProductAccessCatalogCommandInput,
  ProductModule
} from "./contractTypes";

export type ClientContractsPanelProps = {
  contracts: ClientContract[];
  productModules: ProductModule[];
  accessCatalog: ProductAccessCatalog | null;
  accessCatalogRevisions: ProductAccessCatalog[];
  publishedAccessCatalogCommand: PublishedProductAccessCatalogCommand | null;
  accessCatalogPublishValue: PublishProductAccessCatalogCommandInput;
  chargeRules: ClientChargeRule[];
  latestSnapshot: EntitlementSnapshot | null;
  latestSnapshotMissing: boolean;
  canIssueEntitlementSnapshot: boolean;
  value: ClientContractFormInput;
  isBusy: boolean;
  onChange: (value: ClientContractFormInput) => void;
  onAccessCatalogPublishValueChange: (value: PublishProductAccessCatalogCommandInput) => void;
  onRefreshAccessCatalog: () => Promise<void>;
  onSaveAccessCatalog: (catalog: ProductAccessCatalog, requestedBy: string) => Promise<void>;
  onPublishAccessCatalogRevision: () => Promise<void>;
  onPublishAccessCatalog: () => Promise<void>;
  onCreate: () => Promise<void>;
  onReplaceActive: () => Promise<void>;
  onSuspend: (contractId: string) => Promise<void>;
  onPrepareModuleBilling: (moduleCode: string) => void;
  onResolveEntitlementReadiness: () => Promise<void>;
};

export type ContractWorkspaceView = "current" | "setup" | "history" | "catalog";

export type ReadinessTone = "neutral" | "ready" | "warning";

export type ContractWorkspaceItem = {
  view: ContractWorkspaceView;
  label: string;
  summary: string;
  tone: ReadinessTone;
};

export type ModuleReadinessIcon = "modules" | "billing" | "entitlement";

export type ModuleReadinessItem = {
  label: string;
  summary: string;
  tone: ReadinessTone;
  icon: ModuleReadinessIcon;
};

export type PaidAddOnReadiness = {
  moduleCode: string;
  displayName: string;
  meta: string;
  billingDefaults: string | null;
  hasChargeRule: boolean;
};

export type EntitlementReadiness = {
  label: string;
  summary: string;
  tone: ReadinessTone;
  missingModuleCodes: string[];
  extraModuleCodes: string[];
  hasLimitMismatch: boolean;
  hasContractMismatch: boolean;
};

export type ContractReadinessModel = {
  readinessItems: ModuleReadinessItem[];
  paidAddOns: PaidAddOnReadiness[];
  entitlementReadiness: EntitlementReadiness;
  hasEntitlementDetails: boolean;
  needsEntitlementAction: boolean;
};
