import { apiRequest } from "../../../shared/api/httpClient";
import type {
  AddClientContactInput,
  AddClientSupportNoteInput,
  ClientAccountingProfile,
  ClientContact,
  ClientDetails,
  ClientLookup,
  ClientSupportNote,
  CreateClientInput,
  UpdateClientInput
} from "../types/clientTypes";

type ListClientsResponse = {
  clients: ClientLookup[];
};

type AddClientSupportNoteResponse = {
  clientId: string;
  supportNote: ClientSupportNote;
};

type AddClientContactResponse = {
  clientId: string;
  contact: ClientContact;
};

type ListClientContactsResponse = {
  clientId: string;
  contacts: ClientContact[];
};

type ListClientSupportNotesResponse = {
  clientId: string;
  supportNotes: ClientSupportNote[];
};

export async function listClients(): Promise<ClientLookup[]> {
  const response = await apiRequest<ListClientsResponse>("/api/v1/clients");

  return response.clients;
}

export async function getClient(clientId: string): Promise<ClientDetails> {
  return apiRequest<ClientDetails>(`/api/v1/clients/${clientId}`);
}

export async function createClient(input: CreateClientInput): Promise<ClientLookup> {
  return apiRequest<ClientLookup>("/api/v1/clients", {
    method: "POST",
    body: JSON.stringify({
      code: input.code,
      legalName: input.legalName,
      displayName: optionalText(input.displayName)
    })
  });
}

export async function updateClient(
  clientId: string,
  input: UpdateClientInput
): Promise<ClientDetails> {
  return apiRequest<ClientDetails>(`/api/v1/clients/${clientId}`, {
    method: "PUT",
    body: JSON.stringify({
      legalName: input.legalName,
      displayName: optionalText(input.displayName)
    })
  });
}

export async function activateClient(clientId: string): Promise<ClientDetails> {
  return apiRequest<ClientDetails>(`/api/v1/clients/${clientId}/activate`, {
    method: "POST",
    body: JSON.stringify({})
  });
}

export async function suspendClient(clientId: string): Promise<ClientDetails> {
  return apiRequest<ClientDetails>(`/api/v1/clients/${clientId}/suspend`, {
    method: "POST",
    body: JSON.stringify({})
  });
}

export async function addClientContact(
  clientId: string,
  input: AddClientContactInput
): Promise<ClientContact> {
  const response = await apiRequest<AddClientContactResponse>(
    `/api/v1/clients/${clientId}/contacts`,
    {
      method: "POST",
      body: JSON.stringify({
        role: input.role,
        fullName: input.fullName,
        jobTitle: optionalText(input.jobTitle),
        email: optionalText(input.email),
        phone: optionalText(input.phone),
        isPrimary: input.isPrimary
      })
    }
  );

  return response.contact;
}

export async function listClientContacts(clientId: string): Promise<ClientContact[]> {
  const response = await apiRequest<ListClientContactsResponse>(
    `/api/v1/clients/${clientId}/contacts`
  );

  return response.contacts;
}

export async function addClientSupportNote(
  clientId: string,
  input: AddClientSupportNoteInput
): Promise<ClientSupportNote> {
  const response = await apiRequest<AddClientSupportNoteResponse>(
    `/api/v1/clients/${clientId}/support-notes`,
    {
      method: "POST",
      body: JSON.stringify(input)
    }
  );

  return response.supportNote;
}

export async function listClientSupportNotes(clientId: string): Promise<ClientSupportNote[]> {
  const response = await apiRequest<ListClientSupportNotesResponse>(
    `/api/v1/clients/${clientId}/support-notes`
  );

  return response.supportNotes;
}

export async function getClientAccountingProfile(
  clientId: string
): Promise<ClientAccountingProfile> {
  return apiRequest<ClientAccountingProfile>(`/api/v1/clients/${clientId}/accounting-profile`);
}

function optionalText(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === "" ? undefined : trimmed;
}
