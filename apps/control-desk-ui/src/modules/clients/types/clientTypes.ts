export type ClientLookup = {
  clientId: string;
  code: string;
  legalName: string;
  displayName: string;
  status: string;
};

export type ClientSupportNote = {
  text: string;
  createdBy: string;
  createdAtUtc: string;
};

export type ClientContact = {
  clientContactId: string;
  role: string;
  fullName: string;
  jobTitle?: string | null;
  email?: string | null;
  phone?: string | null;
  isPrimary: boolean;
  createdAtUtc: string;
};

export type ClientDetails = ClientLookup & {
  createdAtUtc: string;
  activatedAtUtc?: string | null;
  suspendedAtUtc?: string | null;
  contacts: ClientContact[];
  supportNotes: ClientSupportNote[];
};

export type ClientAccountingProfile = {
  clientId: string;
  accountsReceivableAccountId: string;
  defaultCurrencyCode: string;
  cloudCustomerId?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type ConfigureClientAccountingProfileInput = {
  accountsReceivableAccountId: string;
  defaultCurrencyCode: string;
  cloudCustomerId: string;
};

export type CreateClientInput = {
  code: string;
  legalName: string;
  displayName: string;
};

export type UpdateClientInput = {
  legalName: string;
  displayName: string;
};

export type AddClientSupportNoteInput = {
  text: string;
  createdBy: string;
};

export type AddClientContactInput = {
  role: string;
  fullName: string;
  jobTitle: string;
  email: string;
  phone: string;
  isPrimary: boolean;
};
