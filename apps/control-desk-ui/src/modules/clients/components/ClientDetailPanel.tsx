import { CheckCircle2, MessageSquarePlus, PauseCircle, Save, UserPlus } from "lucide-react";
import type { FormEvent } from "react";
import type {
  AddClientContactInput,
  AddClientSupportNoteInput,
  ClientAccountingProfile,
  ClientDetails,
  UpdateClientInput
} from "../types/clientTypes";

type ClientDetailPanelProps = {
  client: ClientDetails | null;
  accountingProfile: ClientAccountingProfile | null;
  accountingProfileMissing: boolean;
  editValue: UpdateClientInput;
  contactValue: AddClientContactInput;
  noteValue: AddClientSupportNoteInput;
  isBusy: boolean;
  onEditChange: (value: UpdateClientInput) => void;
  onContactChange: (value: AddClientContactInput) => void;
  onNoteChange: (value: AddClientSupportNoteInput) => void;
  onSave: () => Promise<void>;
  onActivate: () => Promise<void>;
  onSuspend: () => Promise<void>;
  onAddContact: () => Promise<void>;
  onAddNote: () => Promise<void>;
};

export function ClientDetailPanel({
  client,
  accountingProfile,
  accountingProfileMissing,
  editValue,
  contactValue,
  noteValue,
  isBusy,
  onEditChange,
  onContactChange,
  onNoteChange,
  onSave,
  onActivate,
  onSuspend,
  onAddContact,
  onAddNote
}: ClientDetailPanelProps) {
  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSave();
  }

  async function handleAddContact(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onAddContact();
  }

  async function handleAddNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onAddNote();
  }

  if (client === null) {
    return (
      <section className="client-panel client-detail-panel">
        <div className="client-empty-detail">Select a client</div>
      </section>
    );
  }

  return (
    <section className="client-detail-panel">
      <div className="client-detail-header">
        <div>
          <span>{client.code}</span>
          <h1>{client.displayName}</h1>
        </div>
        <span className={`status-pill large ${client.status.toLowerCase()}`}>{client.status}</span>
      </div>

      <div className="client-detail-grid">
        <form className="client-panel client-edit-form" onSubmit={handleSave}>
          <div className="client-panel-heading">
            <div>
              <span>Profile</span>
              <strong>Client</strong>
            </div>
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Save client">
              <Save size={16} />
              Save
            </button>
          </div>

          <label className="form-field">
            <span>Legal name</span>
            <input
              value={editValue.legalName}
              onChange={(event) => onEditChange({ ...editValue, legalName: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Display name</span>
            <input
              value={editValue.displayName}
              onChange={(event) => onEditChange({ ...editValue, displayName: event.target.value })}
              disabled={isBusy}
            />
          </label>

          <div className="client-action-row">
            <button className="icon-button" type="button" onClick={onActivate} disabled={isBusy} title="Activate client">
              <CheckCircle2 size={16} />
              Activate
            </button>
            <button className="icon-button" type="button" onClick={onSuspend} disabled={isBusy} title="Suspend client">
              <PauseCircle size={16} />
              Suspend
            </button>
          </div>
        </form>

        <div className="client-panel accounting-profile-panel">
          <div className="client-panel-heading">
            <div>
              <span>Accounting</span>
              <strong>{accountingProfileMissing ? "Not linked" : "Linked"}</strong>
            </div>
          </div>
          <dl className="client-facts">
            <div>
              <dt>Currency</dt>
              <dd>{accountingProfile?.defaultCurrencyCode ?? "-"}</dd>
            </div>
            <div>
              <dt>Cloud customer</dt>
              <dd>{accountingProfile?.cloudCustomerId ?? "-"}</dd>
            </div>
            <div>
              <dt>AR account</dt>
              <dd>{accountingProfile?.accountsReceivableAccountId ?? "-"}</dd>
            </div>
          </dl>
        </div>
      </div>

      <div className="client-contacts-zone">
        <form className="client-panel client-contact-form" onSubmit={handleAddContact}>
          <div className="client-panel-heading">
            <div>
              <span>Contacts</span>
              <strong>New contact</strong>
            </div>
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Add contact">
              <UserPlus size={16} />
              Add
            </button>
          </div>

          <div className="contact-form-grid">
            <label className="form-field">
              <span>Role</span>
              <select
                value={contactValue.role}
                onChange={(event) => onContactChange({ ...contactValue, role: event.target.value })}
                disabled={isBusy}
              >
                <option value="Owner">Owner</option>
                <option value="Billing">Billing</option>
                <option value="Support">Support</option>
                <option value="Technical">Technical</option>
                <option value="Accounts">Accounts</option>
                <option value="Other">Other</option>
              </select>
            </label>
            <label className="form-field">
              <span>Full name</span>
              <input
                value={contactValue.fullName}
                onChange={(event) => onContactChange({ ...contactValue, fullName: event.target.value })}
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Title</span>
              <input
                value={contactValue.jobTitle}
                onChange={(event) => onContactChange({ ...contactValue, jobTitle: event.target.value })}
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Email</span>
              <input
                value={contactValue.email}
                onChange={(event) => onContactChange({ ...contactValue, email: event.target.value })}
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Phone</span>
              <input
                value={contactValue.phone}
                onChange={(event) => onContactChange({ ...contactValue, phone: event.target.value })}
                disabled={isBusy}
              />
            </label>
            <label className="checkbox-field contact-primary-field">
              <input
                type="checkbox"
                checked={contactValue.isPrimary}
                onChange={(event) => onContactChange({ ...contactValue, isPrimary: event.target.checked })}
                disabled={isBusy}
              />
              Primary
            </label>
          </div>
        </form>

        <div className="client-panel client-contacts-list">
          <div className="client-panel-heading">
            <div>
              <span>Contacts</span>
              <strong>{client.contacts.length}</strong>
            </div>
          </div>
          <div className="contact-list">
            {client.contacts.length === 0 && <div className="client-empty-state">No contacts</div>}
            {client.contacts.map((contact) => (
              <article className="contact-item" key={contact.clientContactId}>
                <header>
                  <strong>{contact.fullName}</strong>
                  <span>{contact.isPrimary ? `${contact.role} primary` : contact.role}</span>
                </header>
                <dl>
                  <div>
                    <dt>Title</dt>
                    <dd>{contact.jobTitle ?? "-"}</dd>
                  </div>
                  <div>
                    <dt>Email</dt>
                    <dd>{contact.email ?? "-"}</dd>
                  </div>
                  <div>
                    <dt>Phone</dt>
                    <dd>{contact.phone ?? "-"}</dd>
                  </div>
                </dl>
              </article>
            ))}
          </div>
        </div>
      </div>

      <div className="client-notes-zone">
        <form className="client-panel client-note-form" onSubmit={handleAddNote}>
          <div className="client-panel-heading">
            <div>
              <span>History</span>
              <strong>New note</strong>
            </div>
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Add note">
              <MessageSquarePlus size={16} />
              Add
            </button>
          </div>
          <label className="form-field">
            <span>Text</span>
            <textarea
              rows={4}
              value={noteValue.text}
              onChange={(event) => onNoteChange({ ...noteValue, text: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Author</span>
            <input
              value={noteValue.createdBy}
              onChange={(event) => onNoteChange({ ...noteValue, createdBy: event.target.value })}
              disabled={isBusy}
            />
          </label>
        </form>

        <div className="client-panel client-notes-list">
          <div className="client-panel-heading">
            <div>
              <span>History</span>
              <strong>{client.supportNotes.length}</strong>
            </div>
          </div>
          <div className="note-list">
            {client.supportNotes.length === 0 && <div className="note-empty">No notes</div>}
            {client.supportNotes.map((note) => (
              <article className="note-item" key={`${note.createdAtUtc}-${note.createdBy}-${note.text}`}>
                <p>{note.text}</p>
                <footer>
                  <span>{note.createdBy}</span>
                  <time dateTime={note.createdAtUtc}>{formatDateTime(note.createdAtUtc)}</time>
                </footer>
              </article>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}
