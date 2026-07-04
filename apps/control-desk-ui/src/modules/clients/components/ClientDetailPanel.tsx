import {
  CheckCircle2,
  MessageSquarePlus,
  PauseCircle,
  RefreshCw,
  Save,
  Send,
  UserPlus,
  XCircle
} from "lucide-react";
import type { FormEvent } from "react";
import type {
  AddClientContactInput,
  AddClientSupportNoteInput,
  ClientAccountingProfile,
  ClientDetails,
  ClientPortalInvitation,
  UpdateClientInput
} from "../types/clientTypes";

type ClientDetailPanelProps = {
  client: ClientDetails | null;
  accountingProfile: ClientAccountingProfile | null;
  accountingProfileMissing: boolean;
  editValue: UpdateClientInput;
  contactValue: AddClientContactInput;
  noteValue: AddClientSupportNoteInput;
  latestPortalInvitation: ClientPortalInvitation | null;
  portalInvitations: ClientPortalInvitation[];
  isBusy: boolean;
  onEditChange: (value: UpdateClientInput) => void;
  onContactChange: (value: AddClientContactInput) => void;
  onNoteChange: (value: AddClientSupportNoteInput) => void;
  onSave: () => Promise<void>;
  onActivate: () => Promise<void>;
  onSuspend: () => Promise<void>;
  onAddContact: () => Promise<void>;
  onInvitePortalContact: (clientContactId: string) => Promise<void>;
  onRefreshPortalInvitations: () => Promise<void>;
  onResendPortalInvitation: (invitationId: string) => Promise<void>;
  onRevokePortalInvitation: (invitationId: string) => Promise<void>;
  onAddNote: () => Promise<void>;
};

export function ClientDetailPanel({
  client,
  accountingProfile,
  accountingProfileMissing,
  editValue,
  contactValue,
  noteValue,
  latestPortalInvitation,
  portalInvitations,
  isBusy,
  onEditChange,
  onContactChange,
  onNoteChange,
  onSave,
  onActivate,
  onSuspend,
  onAddContact,
  onInvitePortalContact,
  onRefreshPortalInvitations,
  onResendPortalInvitation,
  onRevokePortalInvitation,
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
    <section className="client-detail-panel profile-workspace">
      <div className="client-detail-header profile-hero profile-record-header">
        <div>
          <span>{client.code}</span>
          <h1>{client.displayName}</h1>
        </div>
        <div className="profile-hero-actions">
          <span className={`status-pill large ${client.status.toLowerCase()}`}>{client.status}</span>
          <button className="icon-button" type="button" onClick={onActivate} disabled={isBusy} title="Activate client">
            <CheckCircle2 size={16} />
            Activate
          </button>
          <button className="icon-button" type="button" onClick={onSuspend} disabled={isBusy} title="Suspend client">
            <PauseCircle size={16} />
            Suspend
          </button>
        </div>
        <dl className="profile-record-strip">
          <div>
            <dt>Legal name</dt>
            <dd>{client.legalName}</dd>
          </div>
          <div>
            <dt>Contacts</dt>
            <dd>{client.contacts.length}</dd>
          </div>
          <div>
            <dt>Portal invites</dt>
            <dd>{portalInvitations.length}</dd>
          </div>
          <div>
            <dt>Currency</dt>
            <dd>{accountingProfile?.defaultCurrencyCode ?? "-"}</dd>
          </div>
        </dl>
      </div>

      <div className="client-detail-grid profile-top-grid">
        <form className="client-panel client-edit-form profile-panel" onSubmit={handleSave}>
          <div className="client-panel-heading">
            <div>
              <span>Profile</span>
              <strong>Master record</strong>
            </div>
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Save client">
              <Save size={16} />
              Save
            </button>
          </div>

          <div className="profile-form-grid">
            <label className="form-field">
              <span>Client code</span>
              <input value={client.code} readOnly />
            </label>
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
          </div>
        </form>

        <div className="client-panel accounting-profile-panel profile-panel">
          <div className="client-panel-heading">
            <div>
              <span>Accounting</span>
              <strong>{accountingProfileMissing ? "Not linked" : "Linked"}</strong>
            </div>
          </div>
          <dl className="client-facts profile-facts">
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

      <div className="profile-secondary-grid">
        <section className="client-panel profile-panel profile-section">
          <div className="client-panel-heading">
            <div>
              <span>Contacts</span>
              <strong>{client.contacts.length}</strong>
            </div>
            <button
              className="icon-button"
              type="button"
              onClick={onRefreshPortalInvitations}
              disabled={isBusy}
              title="Refresh portal invitations"
            >
              <RefreshCw size={16} />
              Refresh
            </button>
          </div>

          {latestPortalInvitation !== null && (
            <div className="portal-invite-result">
              <div>
                <span>Portal invite</span>
                <strong>{latestPortalInvitation.email}</strong>
              </div>
              <input
                readOnly
                value={latestPortalInvitation.invitationUrl ?? latestPortalInvitation.invitationToken ?? ""}
              />
            </div>
          )}

          <div className="profile-register-frame portal-invitation-register">
            <table className="profile-register-table profile-portal-table">
              <thead>
                <tr>
                  <th scope="col">Email</th>
                  <th scope="col">Role</th>
                  <th scope="col">Status</th>
                  <th scope="col">Invited</th>
                  <th scope="col">Expires</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {portalInvitations.length === 0 && (
                  <tr>
                    <td colSpan={6}>
                      <div className="client-empty-state">No portal invitations loaded</div>
                    </td>
                  </tr>
                )}
                {portalInvitations.map((invitation) => {
                  const status = invitation.status.toLowerCase();
                  const canChange = status !== "accepted" && status !== "revoked";

                  return (
                    <tr key={invitation.invitationId}>
                      <td title={invitation.email}>{invitation.email}</td>
                      <td>{invitation.role}</td>
                      <td>
                        <span className={`status-pill ${status}`}>{invitation.status}</span>
                      </td>
                      <td>{formatDateTime(invitation.invitedAtUtc)}</td>
                      <td>{formatDateTime(invitation.expiresAtUtc)}</td>
                      <td>
                        <div className="profile-register-actions">
                          <button
                            className="mini-button"
                            type="button"
                            onClick={() => onResendPortalInvitation(invitation.invitationId)}
                            disabled={isBusy || !canChange}
                            title="Resend invitation"
                          >
                            <Send size={13} />
                            Resend
                          </button>
                          <button
                            className="mini-button"
                            type="button"
                            onClick={() => onRevokePortalInvitation(invitation.invitationId)}
                            disabled={isBusy || !canChange}
                            title="Revoke invitation"
                          >
                            <XCircle size={13} />
                            Revoke
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <form className="client-contact-form profile-inline-form" onSubmit={handleAddContact}>
            <div className="contact-form-grid profile-contact-form-grid">
              <label className="form-field contact-role-field">
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
              <label className="form-field contact-name-field">
                <span>Full name</span>
                <input
                  value={contactValue.fullName}
                  onChange={(event) => onContactChange({ ...contactValue, fullName: event.target.value })}
                  disabled={isBusy}
                />
              </label>
              <label className="form-field contact-title-field">
                <span>Title</span>
                <input
                  value={contactValue.jobTitle}
                  onChange={(event) => onContactChange({ ...contactValue, jobTitle: event.target.value })}
                  disabled={isBusy}
                />
              </label>
              <label className="form-field contact-email-field">
                <span>Email</span>
                <input
                  value={contactValue.email}
                  onChange={(event) => onContactChange({ ...contactValue, email: event.target.value })}
                  disabled={isBusy}
                />
              </label>
              <label className="form-field contact-phone-field">
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
            <div className="client-action-row">
              <button className="icon-button primary" type="submit" disabled={isBusy} title="Add contact">
                <UserPlus size={16} />
                Add
              </button>
            </div>
          </form>

          <div className="profile-register-frame contact-register">
            <table className="profile-register-table profile-contact-table">
              <thead>
                <tr>
                  <th scope="col">Contact</th>
                  <th scope="col">Role</th>
                  <th scope="col">Title</th>
                  <th scope="col">Email</th>
                  <th scope="col">Phone</th>
                  <th scope="col">Portal</th>
                </tr>
              </thead>
              <tbody>
                {client.contacts.length === 0 && (
                  <tr>
                    <td colSpan={6}>
                      <div className="client-empty-state">No contacts</div>
                    </td>
                  </tr>
                )}
                {client.contacts.map((contact) => (
                  <tr key={contact.clientContactId}>
                    <td>
                      <strong title={contact.fullName}>{contact.fullName}</strong>
                    </td>
                    <td>
                      <span className="profile-role-chip">
                        {contact.role}
                        {contact.isPrimary && <em>Primary</em>}
                      </span>
                    </td>
                    <td title={contact.jobTitle ?? ""}>{contact.jobTitle ?? "-"}</td>
                    <td title={contact.email ?? ""}>{contact.email ?? "-"}</td>
                    <td title={contact.phone ?? ""}>{contact.phone ?? "-"}</td>
                    <td>
                      <button
                        className="mini-button"
                        type="button"
                        onClick={() => onInvitePortalContact(contact.clientContactId)}
                        disabled={isBusy || contact.email === null || contact.email === undefined || contact.email.trim() === ""}
                        title="Invite to portal"
                      >
                        <Send size={13} />
                        Invite
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="client-panel profile-panel profile-section">
          <div className="client-panel-heading">
            <div>
              <span>History</span>
              <strong>{client.supportNotes.length}</strong>
            </div>
          </div>

          <form className="client-note-form profile-inline-form" onSubmit={handleAddNote}>
            <label className="form-field">
              <span>Text</span>
              <textarea
                rows={3}
                value={noteValue.text}
                onChange={(event) => onNoteChange({ ...noteValue, text: event.target.value })}
                disabled={isBusy}
              />
            </label>
            <div className="profile-note-row">
              <label className="form-field">
                <span>Author</span>
                <input
                  value={noteValue.createdBy}
                  onChange={(event) => onNoteChange({ ...noteValue, createdBy: event.target.value })}
                  disabled={isBusy}
                />
              </label>
              <button className="icon-button primary" type="submit" disabled={isBusy} title="Add note">
                <MessageSquarePlus size={16} />
                Add
              </button>
            </div>
          </form>

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
        </section>
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
