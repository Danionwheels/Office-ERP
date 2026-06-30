import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ChargeCodeLookupOption,
  ClientLookupOption,
  EditableSurveyJobInvoiceLine,
  SurveyBillingDraftFields,
  SurveyBillingDraftResult,
  SurveyDocumentChecklistItem,
  SurveyJobEntry,
  SurveyJobEntryFields,
  SurveyJobStatus
} from "../types/surveyJobEntryTypes";

type CreateSurveyJobResponse = {
  surveyJobId: string;
  surveyJobNumber: string;
  status: SurveyJobStatus;
};

type ListClientsResponse = {
  clients: ClientLookupOption[];
};

type ListChargeCodesResponse = {
  chargeCodes: ChargeCodeLookupOption[];
};

type SurveyJobEntryFieldsRequest = Omit<
  Partial<SurveyJobEntryFields>,
  | "deliveredDate"
  | "reInspectionDate"
  | "invoiceDate"
  | "voucherDate"
  | "discountDate"
  | "purchaseOrderDate"
> & {
  intimationDate: string;
  deliveredDate?: string;
  reInspectionDate?: string;
  invoiceDate?: string;
  voucherDate?: string;
  discountDate?: string;
  purchaseOrderDate?: string;
};

export async function createSurveyJob(
  surveyJobNumber: string,
  fields: SurveyJobEntryFields
): Promise<CreateSurveyJobResponse> {
  return apiRequest<CreateSurveyJobResponse>("/api/v1/survey-valuation/jobs", {
    method: "POST",
    body: JSON.stringify({
      surveyJobNumber,
      fields: toRequestFields(fields)
    })
  });
}

export async function getSurveyJobEntryById(surveyJobId: string): Promise<SurveyJobEntry> {
  return apiRequest<SurveyJobEntry>(`/api/v1/survey-valuation/jobs/${surveyJobId}`);
}

export async function getSurveyJobEntryByNumber(
  surveyJobNumber: string
): Promise<SurveyJobEntry> {
  return apiRequest<SurveyJobEntry>(
    `/api/v1/survey-valuation/jobs/by-number/${encodeURIComponent(surveyJobNumber)}`
  );
}

export async function updateSurveyJob(
  surveyJobId: string,
  status: SurveyJobStatus,
  fields: SurveyJobEntryFields
): Promise<SurveyJobEntry> {
  return apiRequest<SurveyJobEntry>(`/api/v1/survey-valuation/jobs/${surveyJobId}`, {
    method: "PUT",
    body: JSON.stringify({
      status,
      fields: toRequestFields(fields)
    })
  });
}

export async function updateSurveyJobDocuments(
  surveyJobId: string,
  documents: SurveyDocumentChecklistItem[]
): Promise<SurveyJobEntry> {
  return apiRequest<SurveyJobEntry>(
    `/api/v1/survey-valuation/jobs/${surveyJobId}/documents`,
    {
      method: "PUT",
      body: JSON.stringify({
        documents
      })
    }
  );
}

export async function listClientOptions(): Promise<ClientLookupOption[]> {
  const response = await apiRequest<ListClientsResponse>("/api/v1/clients");

  return response.clients;
}

export async function listChargeCodeOptions(): Promise<ChargeCodeLookupOption[]> {
  const response = await apiRequest<ListChargeCodesResponse>("/api/v1/billing/charge-codes");

  return response.chargeCodes;
}

export async function updateSurveyJobInvoiceLines(
  surveyJobId: string,
  invoiceLines: EditableSurveyJobInvoiceLine[]
): Promise<SurveyJobEntry> {
  return apiRequest<SurveyJobEntry>(
    `/api/v1/survey-valuation/jobs/${surveyJobId}/invoice-lines`,
    {
      method: "PUT",
      body: JSON.stringify({
        invoiceLines: invoiceLines.map((line) => ({
          sequenceNumber: line.sequenceNumber,
          descriptionType: line.descriptionType,
          description: line.description,
          amount: Number(line.amount),
          currencyCode: line.currencyCode,
          billingHeadCode: optionalText(line.billingHeadCode),
          taxCode: optionalText(line.taxCode),
          categoryCode: optionalText(line.categoryCode)
        }))
      })
    }
  );
}

export async function createSurveyJobBillingDraft(
  surveyJobId: string,
  fields: SurveyBillingDraftFields
): Promise<SurveyBillingDraftResult> {
  return apiRequest<SurveyBillingDraftResult>(
    `/api/v1/survey-valuation/jobs/${surveyJobId}/billing-draft`,
    {
      method: "POST",
      body: JSON.stringify({
        clientId: fields.clientId,
        contractId: fields.contractId,
        invoiceNumber: fields.invoiceNumber,
        issueDate: fields.issueDate,
        dueDate: fields.dueDate,
        currencyCode: fields.currencyCode
      })
    }
  );
}

function toRequestFields(fields: SurveyJobEntryFields): SurveyJobEntryFieldsRequest {
  return {
    ...fields,
    deliveredDate: optionalDate(fields.deliveredDate),
    reInspectionDate: optionalDate(fields.reInspectionDate),
    invoiceDate: optionalDate(fields.invoiceDate),
    voucherDate: optionalDate(fields.voucherDate),
    discountDate: optionalDate(fields.discountDate),
    purchaseOrderDate: optionalDate(fields.purchaseOrderDate)
  };
}

function optionalDate(value: string): string | undefined {
  return value.trim() === "" ? undefined : value;
}

function optionalText(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === "" ? undefined : trimmed;
}
