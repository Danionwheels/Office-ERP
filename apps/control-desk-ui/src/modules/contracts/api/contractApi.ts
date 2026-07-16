import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ClientContract,
  ClientContractFormInput,
  ProductAccessCatalog,
  PublishedProductAccessCatalogCommand,
  PublishProductAccessCatalogCommandInput,
  ProductModule,
  ReplaceActiveClientContractResult
} from "../types/contractTypes";

type ListProductModulesResponse = {
  modules: ProductModule[];
  catalogRevisionId?: string | null;
  catalogRevisionNumber?: number | null;
};

type ListProductCatalogRevisionsResponse = {
  revisions: ProductAccessCatalog[];
};

type ListClientContractsResponse = {
  clientId: string;
  contracts: ClientContract[];
};

export async function listProductModules(): Promise<ProductModule[]> {
  const response = await apiRequest<ListProductModulesResponse>("/api/v1/contracts/product-modules");

  return response.modules.map(normalizeProductModule);
}

export async function listProductAccessCatalog(): Promise<ProductAccessCatalog> {
  const catalog = await apiRequest<ProductAccessCatalog>("/api/v1/contracts/product-access-catalog");
  return normalizeCatalog(catalog);
}

export async function listProductCatalogRevisions(): Promise<ProductAccessCatalog[]> {
  const response = await apiRequest<ListProductCatalogRevisionsResponse>(
    "/api/v1/contracts/product-access-catalog/revisions"
  );

  return response.revisions.map(normalizeCatalog);
}

export async function saveProductAccessCatalog(
  catalog: ProductAccessCatalog,
  requestedBy: string
): Promise<ProductAccessCatalog> {
  const saved = await apiRequest<ProductAccessCatalog>("/api/v1/contracts/product-access-catalog", {
    method: "PUT",
    body: JSON.stringify({
      modules: catalog.modules.map((module) => ({
        moduleCode: module.moduleCode,
        displayName: module.displayName,
        description: module.description,
        commercialMode: module.commercialMode,
        isActive: module.isActive,
        billingDefaults: module.billingDefaults,
        compatibility: module.compatibility
      })),
      moduleGroups: catalog.moduleGroups,
      resources: catalog.resources.map((resource) => ({
        resourceId: resource.resourceId,
        displayName: resource.displayName,
        accessKind: resource.accessKind,
        requiredGroupIds: resource.requiredGroupIds,
        requiredModuleCodes: resource.requiredModuleCodes
      })),
      requestedBy: requestedBy.trim(),
      changeReason: catalog.changeReason.trim()
    })
  });

  return normalizeCatalog(saved);
}

export async function publishProductCatalogRevision(
  requestedBy: string
): Promise<ProductAccessCatalog> {
  const published = await apiRequest<ProductAccessCatalog>(
    "/api/v1/contracts/product-access-catalog/publish-revision",
    {
      method: "POST",
      body: JSON.stringify({ requestedBy: requestedBy.trim() })
    }
  );

  return normalizeCatalog(published);
}

export async function publishProductAccessCatalogCommand(
  input: PublishProductAccessCatalogCommandInput
): Promise<PublishedProductAccessCatalogCommand> {
  const expiresInHours = input.expiresInHours.trim();

  return apiRequest<PublishedProductAccessCatalogCommand>(
    "/api/v1/contracts/product-access-catalog/product-kernel-command",
    {
      method: "POST",
      body: JSON.stringify({
        activationRequestId: input.activationRequestId.trim(),
        expiresInHours: expiresInHours === "" ? undefined : Number(expiresInHours),
        requestedBy: input.requestedBy.trim()
      })
    }
  );
}

export async function listClientContracts(clientId: string): Promise<ClientContract[]> {
  const response = await apiRequest<ListClientContractsResponse>(
    `/api/v1/contracts/clients/${clientId}/client-contracts`
  );

  return response.contracts;
}

export async function createClientContract(
  clientId: string,
  input: ClientContractFormInput
): Promise<ClientContract> {
  return apiRequest<ClientContract>("/api/v1/contracts/client-contracts", {
    method: "POST",
    body: JSON.stringify(toContractRequest(clientId, input))
  });
}

export async function replaceActiveClientContract(
  clientId: string,
  input: ClientContractFormInput
): Promise<ReplaceActiveClientContractResult> {
  return apiRequest<ReplaceActiveClientContractResult>(
    "/api/v1/contracts/client-contracts/replace-active",
    {
      method: "POST",
      body: JSON.stringify(toContractRequest(clientId, input))
    }
  );
}

export async function suspendClientContract(contractId: string): Promise<ClientContract> {
  return apiRequest<ClientContract>(`/api/v1/contracts/client-contracts/${contractId}/suspend`, {
    method: "POST",
    body: JSON.stringify({})
  });
}

function toContractRequest(clientId: string, input: ClientContractFormInput) {
  return {
    clientId,
    contractNumber: input.contractNumber,
    startsOn: input.startsOn,
    endsOn: input.endsOn,
    recurringAmount: Number(input.recurringAmount),
    currencyCode: input.currencyCode,
    billingCycle: input.billingCycle,
    billingDayOfMonth: Number(input.billingDayOfMonth),
    allowedDevices: Number(input.allowedDevices),
    allowedBranches: Number(input.allowedBranches),
    allowedNamedUsers: optionalNumber(input.allowedNamedUsers),
    allowedConcurrentUsers: optionalNumber(input.allowedConcurrentUsers),
    approvalReason: input.approvalReason.trim(),
    modules: moduleCodesFromText(input.moduleCodes).map((moduleCode) => ({
      moduleCode,
      isEnabled: true
    })),
    featureLimits: input.featureLimits.map((limit) => ({
      moduleCode: limit.moduleCode.trim().toUpperCase(),
      featureCode: limit.featureCode.trim().toUpperCase(),
      limitValue: Number(limit.limitValue),
      unit: limit.unit.trim().toUpperCase()
    }))
  };
}

function optionalNumber(value: string): number | null {
  const normalized = value.trim();
  return normalized === "" ? null : Number(normalized);
}

function moduleCodesFromText(value: string): string[] {
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

function normalizeCatalog(catalog: ProductAccessCatalog): ProductAccessCatalog {
  return {
    ...catalog,
    modules: (catalog.modules ?? []).map(normalizeProductModule),
    moduleGroups: catalog.moduleGroups ?? [],
    resources: catalog.resources ?? [],
    state: catalog.state ?? "Published",
    catalogRevisionId: catalog.catalogRevisionId ?? null,
    revisionNumber: catalog.revisionNumber ?? null,
    supersedesCatalogRevisionId: catalog.supersedesCatalogRevisionId ?? null,
    draftId: catalog.draftId ?? null,
    baseCatalogRevisionId: catalog.baseCatalogRevisionId ?? null,
    baseCatalogRevisionNumber: catalog.baseCatalogRevisionNumber ?? null,
    changeReason: catalog.changeReason ?? "",
    changedBy: catalog.changedBy ?? "",
    changedAtUtc: catalog.changedAtUtc ?? null
  };
}

function normalizeProductModule(module: ProductModule): ProductModule {
  return {
    ...module,
    description: module.description ?? "",
    compatibility: {
      minimumSafarSuiteVersion: module.compatibility?.minimumSafarSuiteVersion ?? null,
      minimumLocalServerVersion: module.compatibility?.minimumLocalServerVersion ?? null,
      supportedDeploymentModes: module.compatibility?.supportedDeploymentModes ?? []
    },
    referencedBy: module.referencedBy ?? []
  };
}
