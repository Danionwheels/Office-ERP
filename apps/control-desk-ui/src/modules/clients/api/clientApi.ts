import { apiRequest } from "../../../shared/api/httpClient";
import type {
  AddClientContactInput,
  AddClientSupportNoteInput,
  ClientAccountingProfile,
  ClientContact,
  ClientDeployment,
  ClientDetails,
  ClientDirectoryPage,
  ClientDirectoryQuery,
  ClientLookup,
  ClientPortalInvitation,
  ClientSupportNote,
  ConfigureClientAccountingProfileInput,
  ConfigureClientDeploymentInput,
  CreateClientInput,
  UpdateClientInput
} from "../types/clientTypes";

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

type InviteClientPortalContactResponse = ClientPortalInvitation;

type ListClientPortalInvitationsResponse = {
  clientId: string;
  invitations: ClientPortalInvitation[];
};

type ListClientDeploymentsResponse = {
  clientId: string;
  deployments: ClientDeployment[];
};

export async function listClientPage(
  input: ClientDirectoryQuery = {}
): Promise<ClientDirectoryPage> {
  const search = new URLSearchParams();

  if (input.search?.trim()) {
    search.set("search", input.search.trim());
  }

  if (input.status?.trim()) {
    search.set("status", input.status.trim());
  }

  if (input.sort !== undefined) {
    search.set("sort", input.sort);
  }

  if (input.direction !== undefined) {
    search.set("direction", input.direction);
  }

  if (input.take !== undefined) {
    search.set("take", String(input.take));
  }

  if (input.cursor?.trim()) {
    search.set("cursor", input.cursor.trim());
  }

  const query = search.toString();
  const page = await apiRequest<ClientDirectoryPage>(
    `/api/v1/clients${query === "" ? "" : `?${query}`}`
  );

  if (page.summary === undefined || page.pageSize === undefined || page.hasMore === undefined) {
    throw new Error("Office Control API must be upgraded before client pages can be read.");
  }

  return page;
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

export async function inviteClientPortalContact(
  clientId: string,
  clientContactId: string
): Promise<ClientPortalInvitation> {
  return apiRequest<InviteClientPortalContactResponse>(
    `/api/v1/clients/${clientId}/contacts/${clientContactId}/portal-invitation`,
    {
      method: "POST",
      body: JSON.stringify({
        expiresInDays: 7,
        createdBy: "SafarSuite Control Desk"
      })
    }
  );
}

export async function listClientPortalInvitations(
  clientId: string
): Promise<ClientPortalInvitation[]> {
  const response = await apiRequest<ListClientPortalInvitationsResponse>(
    `/api/v1/clients/${clientId}/portal-invitations`
  );

  return response.invitations;
}

export async function resendClientPortalInvitation(
  clientId: string,
  invitationId: string
): Promise<ClientPortalInvitation> {
  return apiRequest<ClientPortalInvitation>(
    `/api/v1/clients/${clientId}/portal-invitations/${invitationId}/resend`,
    {
      method: "POST",
      body: JSON.stringify({
        expiresInDays: 7,
        createdBy: "SafarSuite Control Desk"
      })
    }
  );
}

export async function revokeClientPortalInvitation(
  clientId: string,
  invitationId: string
): Promise<ClientPortalInvitation> {
  return apiRequest<ClientPortalInvitation>(
    `/api/v1/clients/${clientId}/portal-invitations/${invitationId}/revoke`,
    {
      method: "POST",
      body: JSON.stringify({
        revokedBy: "SafarSuite Control Desk"
      })
    }
  );
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

export async function configureClientAccountingProfile(
  clientId: string,
  input: ConfigureClientAccountingProfileInput
): Promise<ClientAccountingProfile> {
  return apiRequest<ClientAccountingProfile>(`/api/v1/clients/${clientId}/accounting-profile`, {
    method: "PUT",
    body: JSON.stringify({
      accountsReceivableAccountId: input.accountsReceivableAccountId,
      defaultCurrencyCode: input.defaultCurrencyCode,
      cloudCustomerId: optionalText(input.cloudCustomerId)
    })
  });
}

export async function listClientDeployments(clientId: string): Promise<ClientDeployment[]> {
  const response = await apiRequest<ListClientDeploymentsResponse>(
    `/api/v1/clients/${clientId}/deployments`
  );

  return response.deployments;
}

export async function configureClientDeployment(
  clientId: string,
  input: ConfigureClientDeploymentInput
): Promise<ClientDeployment> {
  return apiRequest<ClientDeployment>(
    `/api/v1/clients/${clientId}/deployments/${encodeURIComponent(input.installationId)}`,
    {
      method: "PUT",
      body: JSON.stringify({
        displayName: input.displayName,
        bootstrapMode: input.bootstrapMode,
        clientDeploymentMode: input.clientDeploymentMode,
        siteId: input.siteId,
        siteRole: input.siteRole,
        parentSiteId: optionalText(input.parentSiteId),
        branchCode: optionalText(input.branchCode),
        syncTopologyId: optionalText(input.syncTopologyId),
        localServerVersion: input.localServerVersion,
        safarSuiteAppVersion: optionalText(input.safarSuiteAppVersion),
        isPrimary: input.isPrimary
      })
    }
  );
}

function optionalText(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === "" ? undefined : trimmed;
}
