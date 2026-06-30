import { AlertCircle, CheckCircle2 } from "lucide-react";
import { useEffect, useState } from "react";
import { ApiError } from "../../../shared/api/apiError";
import {
  activateClient,
  addClientContact,
  addClientSupportNote,
  createClient,
  getClient,
  getClientAccountingProfile,
  listClients,
  suspendClient,
  updateClient
} from "../api/clientApi";
import { ClientCreateForm } from "../components/ClientCreateForm";
import { ClientDetailPanel } from "../components/ClientDetailPanel";
import { ClientListPanel } from "../components/ClientListPanel";
import type {
  AddClientContactInput,
  AddClientSupportNoteInput,
  ClientAccountingProfile,
  ClientDetails,
  ClientLookup,
  CreateClientInput,
  UpdateClientInput
} from "../types/clientTypes";

const emptyCreateForm: CreateClientInput = {
  code: "",
  legalName: "",
  displayName: ""
};

const emptyEditForm: UpdateClientInput = {
  legalName: "",
  displayName: ""
};

const emptyNoteForm: AddClientSupportNoteInput = {
  text: "",
  createdBy: "Control Desk"
};

const emptyContactForm: AddClientContactInput = {
  role: "Billing",
  fullName: "",
  jobTitle: "",
  email: "",
  phone: "",
  isPrimary: true
};

export function ClientDeskPage() {
  const [clients, setClients] = useState<ClientLookup[]>([]);
  const [selectedClientId, setSelectedClientId] = useState("");
  const [selectedClient, setSelectedClient] = useState<ClientDetails | null>(null);
  const [accountingProfile, setAccountingProfile] = useState<ClientAccountingProfile | null>(null);
  const [accountingProfileMissing, setAccountingProfileMissing] = useState(false);
  const [createForm, setCreateForm] = useState<CreateClientInput>(emptyCreateForm);
  const [editForm, setEditForm] = useState<UpdateClientInput>(emptyEditForm);
  const [contactForm, setContactForm] = useState<AddClientContactInput>(emptyContactForm);
  const [noteForm, setNoteForm] = useState<AddClientSupportNoteInput>(emptyNoteForm);
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    void refreshClients();
  }, []);

  useEffect(() => {
    if (selectedClientId !== "") {
      void loadClient(selectedClientId);
    }
  }, [selectedClientId]);

  async function refreshClients(nextSelectedClientId = selectedClientId) {
    await runClientAction(() => loadClientList(nextSelectedClientId));
  }

  async function loadClientList(nextSelectedClientId = selectedClientId) {
    const clientList = await listClients();
    setClients(clientList);

    if (clientList.length === 0) {
      setSelectedClientId("");
      setSelectedClient(null);
      setAccountingProfile(null);
      setAccountingProfileMissing(false);
      return;
    }

    const selectedExists = clientList.some((client) => client.clientId === nextSelectedClientId);
    setSelectedClientId(selectedExists ? nextSelectedClientId : clientList[0].clientId);
  }

  async function loadClient(clientId: string) {
    await runClientAction(async () => {
      const client = await getClient(clientId);
      setSelectedClient(client);
      setEditForm({
        legalName: client.legalName,
        displayName: client.displayName
      });
      await loadAccountingProfile(clientId);
    });
  }

  async function loadAccountingProfile(clientId: string) {
    try {
      const profile = await getClientAccountingProfile(clientId);
      setAccountingProfile(profile);
      setAccountingProfileMissing(false);
    } catch (caughtError) {
      if (caughtError instanceof ApiError && caughtError.statusCode === 404) {
        setAccountingProfile(null);
        setAccountingProfileMissing(true);
        return;
      }

      throw caughtError;
    }
  }

  async function handleCreateClient() {
    await runClientAction(async () => {
      const createdClient = await createClient(createForm);
      setCreateForm(emptyCreateForm);
      await loadClientList(createdClient.clientId);
      setMessage("Client created.");
    });
  }

  async function handleUpdateClient() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const updatedClient = await updateClient(selectedClient.clientId, editForm);
      applyLoadedClient(updatedClient);
      setClients((current) =>
        current.map((client) =>
          client.clientId === updatedClient.clientId ? toClientLookup(updatedClient) : client
        )
      );
      setMessage("Client saved.");
    });
  }

  async function handleActivateClient() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const updatedClient = await activateClient(selectedClient.clientId);
      applyLoadedClient(updatedClient);
      updateClientListRow(updatedClient);
      setMessage("Client activated.");
    });
  }

  async function handleSuspendClient() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const updatedClient = await suspendClient(selectedClient.clientId);
      applyLoadedClient(updatedClient);
      updateClientListRow(updatedClient);
      setMessage("Client suspended.");
    });
  }

  async function handleAddContact() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      await addClientContact(selectedClient.clientId, contactForm);
      const refreshedClient = await getClient(selectedClient.clientId);
      applyLoadedClient(refreshedClient);
      setContactForm({
        ...emptyContactForm,
        role: contactForm.role
      });
      setMessage("Contact added.");
    });
  }

  async function handleAddNote() {
    if (selectedClient === null) {
      return;
    }

    await runClientAction(async () => {
      const note = await addClientSupportNote(selectedClient.clientId, noteForm);
      setSelectedClient({
        ...selectedClient,
        supportNotes: [note, ...selectedClient.supportNotes]
      });
      setNoteForm({
        ...emptyNoteForm,
        createdBy: noteForm.createdBy
      });
      setMessage("Note added.");
    });
  }

  async function runClientAction(action: () => Promise<void>) {
    setIsBusy(true);
    setError("");
    setMessage("");

    try {
      await action();
    } catch (caughtError) {
      setError(formatError(caughtError));
    } finally {
      setIsBusy(false);
    }
  }

  function applyLoadedClient(client: ClientDetails) {
    setSelectedClient(client);
    setEditForm({
      legalName: client.legalName,
      displayName: client.displayName
    });
  }

  function updateClientListRow(client: ClientDetails) {
    setClients((current) =>
      current.map((item) => (item.clientId === client.clientId ? toClientLookup(client) : item))
    );
  }

  return (
    <div className="client-desk">
      <header className="client-desk-header">
        <div>
          <span>SafarSuite Control Desk</span>
          <h1>Clients</h1>
        </div>
        <ClientCreateForm
          value={createForm}
          isBusy={isBusy}
          onChange={setCreateForm}
          onSubmit={handleCreateClient}
        />
      </header>

      <div className="status-line" aria-live="polite">
        {error !== "" && (
          <span className="status-error">
            <AlertCircle size={16} />
            {error}
          </span>
        )}
        {message !== "" && (
          <span className="status-success">
            <CheckCircle2 size={16} />
            {message}
          </span>
        )}
      </div>

      <div className="client-desk-body">
        <ClientListPanel
          clients={clients}
          selectedClientId={selectedClientId}
          isBusy={isBusy}
          onSelect={setSelectedClientId}
          onRefresh={() => refreshClients()}
        />
        <ClientDetailPanel
          client={selectedClient}
          accountingProfile={accountingProfile}
          accountingProfileMissing={accountingProfileMissing}
          editValue={editForm}
          contactValue={contactForm}
          noteValue={noteForm}
          isBusy={isBusy}
          onEditChange={setEditForm}
          onContactChange={setContactForm}
          onNoteChange={setNoteForm}
          onSave={handleUpdateClient}
          onActivate={handleActivateClient}
          onSuspend={handleSuspendClient}
          onAddContact={handleAddContact}
          onAddNote={handleAddNote}
        />
      </div>
    </div>
  );
}

function toClientLookup(client: ClientDetails): ClientLookup {
  return {
    clientId: client.clientId,
    code: client.code,
    legalName: client.legalName,
    displayName: client.displayName,
    status: client.status
  };
}

function formatError(caughtError: unknown): string {
  if (caughtError instanceof ApiError) {
    const details = caughtError.errors.map((error) => error.message).join(" ");
    return details === "" ? caughtError.message : details;
  }

  if (caughtError instanceof Error) {
    return caughtError.message;
  }

  return "Unexpected error.";
}
