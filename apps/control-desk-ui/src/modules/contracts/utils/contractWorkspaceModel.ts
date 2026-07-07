import type { ClientChargeRule } from "../../billing/types/billingTypes";
import type { EntitlementSnapshot } from "../../entitlements/types/entitlementTypes";
import type { ClientContract, ProductAccessCatalog, ProductModule } from "../types/contractTypes";
import type {
  ContractReadinessModel,
  ContractWorkspaceItem,
  EntitlementReadiness,
  PaidAddOnReadiness
} from "../types/contractWorkspaceTypes";
import {
  findProductModule,
  formatProductModuleBillingDefaults,
  getProductModuleDisplayName,
  getProductModuleMeta,
  normalizeProductModuleCode
} from "./productModuleDisplay";

export function getActiveContract(contracts: ClientContract[]): ClientContract | null {
  return contracts.find((contract) => contract.status.toLowerCase() === "active")
    ?? contracts[0]
    ?? null;
}

export function getContractWorkspaceItems(
  contracts: ClientContract[],
  activeContract: ClientContract | null,
  accessCatalog: ProductAccessCatalog | null
): ContractWorkspaceItem[] {
  return [
    {
      view: "current",
      label: "Current contract",
      summary: activeContract === null ? "Missing" : activeContract.status,
      tone: activeContract?.status.toLowerCase() === "active" ? "ready" : "warning"
    },
    {
      view: "setup",
      label: "Create / Replace",
      summary: activeContract === null ? "Create first" : "Ready",
      tone: activeContract === null ? "warning" : "neutral"
    },
    {
      view: "history",
      label: "History",
      summary: `${contracts.length} contracts`,
      tone: contracts.length === 0 ? "neutral" : "ready"
    },
    {
      view: "catalog",
      label: "Access catalog",
      summary: accessCatalog === null ? "Not loaded" : `${accessCatalog.moduleGroups.length} groups`,
      tone: accessCatalog === null ? "neutral" : "ready"
    }
  ];
}

export function getContractReadinessModel(
  contract: ClientContract,
  productModules: ProductModule[],
  chargeRules: ClientChargeRule[],
  latestSnapshot: EntitlementSnapshot | null,
  latestSnapshotMissing: boolean
): ContractReadinessModel {
  const enabledModuleCodes = getEnabledModuleCodes(contract.modules);
  const includedModuleCount = enabledModuleCodes.filter((moduleCode) =>
    findProductModule(productModules, moduleCode)?.commercialMode === "IncludedForAll"
  ).length;
  const paidAddOns = getPaidAddOnReadiness(contract, productModules, chargeRules);
  const unmatchedPaidAddOns = paidAddOns.filter((module) => !module.hasChargeRule);
  const entitlementReadiness = getEntitlementReadiness(
    contract,
    latestSnapshot,
    latestSnapshotMissing
  );
  const hasEntitlementDetails =
    entitlementReadiness.hasContractMismatch
    || entitlementReadiness.hasLimitMismatch
    || entitlementReadiness.missingModuleCodes.length > 0
    || entitlementReadiness.extraModuleCodes.length > 0;

  return {
    readinessItems: [
      {
        label: "Contract modules",
        summary: enabledModuleCodes.length === 0
          ? "No modules enabled"
          : `${enabledModuleCodes.length} enabled, ${includedModuleCount} included`,
        tone: enabledModuleCodes.length === 0 ? "warning" : "ready",
        icon: "modules"
      },
      {
        label: "Billing rules",
        summary: paidAddOns.length === 0
          ? "No add-ons enabled"
          : unmatchedPaidAddOns.length === 0
            ? `${paidAddOns.length} add-ons covered`
            : `${unmatchedPaidAddOns.length} add-ons missing`,
        tone: paidAddOns.length === 0
          ? "neutral"
          : unmatchedPaidAddOns.length === 0
            ? "ready"
            : "warning",
        icon: "billing"
      },
      {
        label: entitlementReadiness.label,
        summary: entitlementReadiness.summary,
        tone: entitlementReadiness.tone,
        icon: "entitlement"
      }
    ],
    paidAddOns,
    entitlementReadiness,
    hasEntitlementDetails,
    needsEntitlementAction: entitlementReadiness.tone === "warning"
  };
}

export function enabledModules(contract: ClientContract, productModules: ProductModule[]): string {
  const modules = contract.modules
    .filter((module) => module.isEnabled)
    .map((module) => getProductModuleDisplayName(productModules, module.moduleCode));

  return modules.length === 0 ? "-" : modules.join(", ");
}

export function getEnabledModuleCodes(
  modules: Array<{ moduleCode: string; isEnabled: boolean }>
): string[] {
  const seen = new Set<string>();

  return modules
    .filter((module) => module.isEnabled)
    .map((module) => normalizeProductModuleCode(module.moduleCode))
    .filter((moduleCode) => {
      if (moduleCode === "" || seen.has(moduleCode)) {
        return false;
      }

      seen.add(moduleCode);
      return true;
    });
}

export function formatModuleNames(moduleCodes: string[], productModules: ProductModule[]): string {
  return moduleCodes
    .map((moduleCode) => getProductModuleDisplayName(productModules, moduleCode))
    .join(", ");
}

export function moduleCodesFromText(value: string): string[] {
  const seen = new Set<string>();

  return value
    .split(/[\n,]/)
    .map((item) => item.trim().toUpperCase())
    .filter((item) => {
      if (item === "" || seen.has(item)) {
        return false;
      }

      seen.add(item);
      return true;
    });
}

export function toggleModuleCode(value: string, moduleCode: string, isSelected: boolean): string {
  const normalizedModuleCode = moduleCode.trim().toUpperCase();
  const moduleCodes = moduleCodesFromText(value);
  const nextModuleCodes = isSelected
    ? [...moduleCodes, normalizedModuleCode]
    : moduleCodes.filter((item) => item !== normalizedModuleCode);

  return [...new Set(nextModuleCodes)].join(", ");
}

export function isIncludedForAll(moduleCode: string, productModules: ProductModule[]): boolean {
  const normalizedModuleCode = moduleCode.trim().toUpperCase();

  return productModules.some(
    (module) =>
      module.moduleCode === normalizedModuleCode && module.commercialMode === "IncludedForAll"
  );
}

export function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function getPaidAddOnReadiness(
  contract: ClientContract,
  productModules: ProductModule[],
  chargeRules: ClientChargeRule[]
): PaidAddOnReadiness[] {
  const billedModuleCodes = getBilledModuleCodes(chargeRules, contract);

  return getEnabledModuleCodes(contract.modules)
    .filter((moduleCode) =>
      findProductModule(productModules, moduleCode)?.commercialMode === "PaidAddOn"
    )
    .map((moduleCode) => {
      const productModule = findProductModule(productModules, moduleCode);

      return {
        moduleCode,
        displayName: getProductModuleDisplayName(productModules, moduleCode),
        meta: getProductModuleMeta(productModules, moduleCode),
        billingDefaults: formatProductModuleBillingDefaults(productModule),
        hasChargeRule: billedModuleCodes.has(moduleCode)
      };
    });
}

function getBilledModuleCodes(
  chargeRules: ClientChargeRule[],
  contract: ClientContract
): Set<string> {
  return new Set(
    chargeRules
      .filter((rule) => rule.status.toLowerCase() === "active")
      .filter((rule) => rule.contractId === undefined
        || rule.contractId === null
        || rule.contractId === contract.contractId)
      .map((rule) => normalizeOptionalModuleCode(rule.productModuleCode))
      .filter((moduleCode): moduleCode is string => moduleCode !== null)
  );
}

function getEntitlementReadiness(
  contract: ClientContract,
  latestSnapshot: EntitlementSnapshot | null,
  latestSnapshotMissing: boolean
): EntitlementReadiness {
  if (latestSnapshot === null) {
    return {
      label: "Entitlement",
      summary: latestSnapshotMissing ? "No snapshot issued" : "Not loaded",
      tone: "warning",
      missingModuleCodes: [],
      extraModuleCodes: [],
      hasLimitMismatch: false,
      hasContractMismatch: false
    };
  }

  const contractModuleCodes = getEnabledModuleCodes(contract.modules);
  const snapshotModuleCodes = getEnabledModuleCodes(latestSnapshot.modules);
  const missingModuleCodes = contractModuleCodes.filter(
    (moduleCode) => !snapshotModuleCodes.includes(moduleCode)
  );
  const extraModuleCodes = snapshotModuleCodes.filter(
    (moduleCode) => !contractModuleCodes.includes(moduleCode)
  );
  const hasContractMismatch = latestSnapshot.contractId !== contract.contractId;
  const hasLimitMismatch =
    latestSnapshot.allowedDevices !== contract.allowedDevices
    || latestSnapshot.allowedBranches !== contract.allowedBranches;

  if (
    !hasContractMismatch
    && !hasLimitMismatch
    && missingModuleCodes.length === 0
    && extraModuleCodes.length === 0
  ) {
    return {
      label: "Entitlement",
      summary: `${snapshotModuleCodes.length} modules aligned`,
      tone: "ready",
      missingModuleCodes,
      extraModuleCodes,
      hasLimitMismatch,
      hasContractMismatch
    };
  }

  const differences = [
    hasContractMismatch ? "contract changed" : null,
    hasLimitMismatch ? "limits differ" : null,
    missingModuleCodes.length > 0 ? `${missingModuleCodes.length} missing` : null,
    extraModuleCodes.length > 0 ? `${extraModuleCodes.length} extra` : null
  ].filter((item): item is string => item !== null);

  return {
    label: "Entitlement",
    summary: differences.join(", "),
    tone: "warning",
    missingModuleCodes,
    extraModuleCodes,
    hasLimitMismatch,
    hasContractMismatch
  };
}

function normalizeOptionalModuleCode(value: string | null | undefined): string | null {
  if (value === null || value === undefined) {
    return null;
  }

  const normalizedModuleCode = normalizeProductModuleCode(value);

  return normalizedModuleCode === "" ? null : normalizedModuleCode;
}
