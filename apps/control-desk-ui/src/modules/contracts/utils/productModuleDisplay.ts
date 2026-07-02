import type { ProductModule } from "../types/contractTypes";

export function findProductModule(
  productModules: ProductModule[],
  moduleCode: string
): ProductModule | null {
  const normalizedModuleCode = normalizeProductModuleCode(moduleCode);

  return productModules.find((module) => module.moduleCode === normalizedModuleCode) ?? null;
}

export function getProductModuleDisplayName(
  productModules: ProductModule[],
  moduleCode: string
): string {
  return findProductModule(productModules, moduleCode)?.displayName ?? normalizeProductModuleCode(moduleCode);
}

export function getProductModuleMeta(
  productModules: ProductModule[],
  moduleCode: string
): string {
  const normalizedModuleCode = normalizeProductModuleCode(moduleCode);
  const productModule = findProductModule(productModules, normalizedModuleCode);

  if (productModule === null) {
    return `${normalizedModuleCode} - not in catalog`;
  }

  const catalogStatus = productModule.isActive ? "" : " - inactive";

  return `${normalizedModuleCode} - ${formatProductModuleCommercialMode(productModule.commercialMode)}${catalogStatus}`;
}

export function formatProductModuleCommercialMode(value: string): string {
  if (value === "IncludedForAll") {
    return "Included";
  }

  if (value === "PaidAddOn") {
    return "Add-on";
  }

  return value;
}

export function formatProductModuleBillingDefaults(
  productModule: ProductModule | null | undefined
): string | null {
  const billingDefaults = productModule?.billingDefaults;

  if (billingDefaults === null || billingDefaults === undefined) {
    return null;
  }

  return `${formatAmount(billingDefaults.defaultUnitPriceAmount)} ${billingDefaults.currencyCode} / ${formatBillingCycle(billingDefaults.billingCycle)}`;
}

export function normalizeProductModuleCode(value: string): string {
  return value.trim().toUpperCase();
}

function formatAmount(value: number): string {
  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0
  }).format(value);
}

function formatBillingCycle(value: string): string {
  if (value === "SemiAnnual") {
    return "semi-annual";
  }

  return value.toLowerCase();
}
