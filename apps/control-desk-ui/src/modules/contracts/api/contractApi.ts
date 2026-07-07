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
};

type ListClientContractsResponse = {
  clientId: string;
  contracts: ClientContract[];
};

export async function listProductModules(): Promise<ProductModule[]> {
  const response = await apiRequest<ListProductModulesResponse>("/api/v1/contracts/product-modules");

  return response.modules;
}

export async function listProductAccessCatalog(): Promise<ProductAccessCatalog> {
  return apiRequest<ProductAccessCatalog>("/api/v1/contracts/product-access-catalog");
}

export async function saveProductAccessCatalog(
  catalog: ProductAccessCatalog,
  requestedBy: string
): Promise<ProductAccessCatalog> {
  return apiRequest<ProductAccessCatalog>("/api/v1/contracts/product-access-catalog", {
    method: "PUT",
    body: JSON.stringify({
      moduleGroups: catalog.moduleGroups,
      resources: catalog.resources.map((resource) => ({
        resourceId: resource.resourceId,
        displayName: resource.displayName,
        accessKind: resource.accessKind,
        requiredGroupIds: resource.requiredGroupIds,
        requiredModuleCodes: resource.requiredModuleCodes
      })),
      requestedBy: requestedBy.trim()
    })
  });
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
    modules: moduleCodesFromText(input.moduleCodes).map((moduleCode) => ({
      moduleCode,
      isEnabled: true
    }))
  };
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
