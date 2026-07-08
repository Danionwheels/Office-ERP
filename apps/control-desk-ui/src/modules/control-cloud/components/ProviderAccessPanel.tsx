import { useEffect, useMemo, useState } from "react";
import {
  KeyRound,
  LogIn,
  LogOut,
  Plus,
  RefreshCw,
  Save,
  ShieldCheck,
  ShieldOff,
  UserPlus
} from "lucide-react";
import { ApiError } from "../../../shared/api/apiError";
import {
  clearProviderAccessSession,
  getProviderAccessSession,
  saveProviderAccessSession,
  type StoredProviderAccessSession
} from "../../../shared/api/providerAccessSession";
import {
  changeProviderAccessOperatorPassword,
  createProviderAccessOperatorSession,
  createProviderAccessOperator,
  listProviderAccessOperators,
  resetProviderAccessOperatorPassword,
  resetProviderAccessOperatorRecoveryCodes,
  resetProviderAccessOperatorTotp,
  updateProviderAccessOperatorScopes,
  updateProviderAccessOperatorStatus
} from "../api/controlCloudApi";
import type {
  ProviderAccessOperator,
  ProviderAccessOperatorCreateInput,
  ProviderAccessSessionCreateInput
} from "../types/controlCloudTypes";
import { formatNullableDateTime, shortIdentifier } from "../utils/cloudWorkspaceModel";

type ProviderScopeOption = {
  scope: string;
  label: string;
  detail: string;
  warning?: boolean;
};

const providerScopeOptions: ProviderScopeOption[] = [
  {
    scope: "app-activation:read",
    label: "Activation read",
    detail: "View app activation mappings."
  },
  {
    scope: "app-activation:write",
    label: "Activation write",
    detail: "Issue and revoke app activation mappings."
  },
  {
    scope: "client-portal:manage",
    label: "Client portal",
    detail: "Create, resend, and revoke client portal invitations."
  },
  {
    scope: "provider-operators:manage",
    label: "Operator admin",
    detail: "Create operators and change rights."
  },
  {
    scope: "*",
    label: "All scopes",
    detail: "Break-glass access to every provider action.",
    warning: true
  }
];

const emptyCreateOperatorForm: ProviderAccessOperatorCreateInput = {
  email: "",
  fullName: "",
  password: "",
  scopes: ["app-activation:read"],
  createdBy: "SafarSuite Control Desk"
};

const emptySessionForm: ProviderAccessSessionCreateInput = {
  email: "",
  password: "",
  recoveryCode: "",
  totpCode: "",
  scopes: ["provider-operators:manage"],
  expiresInMinutes: 60
};

const emptyPasswordChangeForm = {
  email: "",
  currentPassword: "",
  newPassword: "",
  confirmPassword: ""
};

export function ProviderAccessPanel() {
  const [operators, setOperators] = useState<ProviderAccessOperator[]>([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [session, setSession] = useState<StoredProviderAccessSession | null>(
    () => getProviderAccessSession()
  );
  const [sessionForm, setSessionForm] =
    useState<ProviderAccessSessionCreateInput>(emptySessionForm);
  const [passwordChangeForm, setPasswordChangeForm] = useState({
    ...emptyPasswordChangeForm,
    email: getProviderAccessSession()?.email ?? ""
  });
  const [createForm, setCreateForm] =
    useState<ProviderAccessOperatorCreateInput>(emptyCreateOperatorForm);
  const [scopeDraft, setScopeDraft] = useState<string[]>([]);
  const [statusDraft, setStatusDraft] = useState("Active");
  const [passwordDraft, setPasswordDraft] = useState("");
  const [recoveryCodeCount, setRecoveryCodeCount] = useState(10);
  const [generatedRecoveryCodes, setGeneratedRecoveryCodes] = useState<string[]>([]);
  const [generatedTotpEnrollment, setGeneratedTotpEnrollment] = useState<{
    secret: string;
    otpAuthUri: string;
  } | null>(null);
  const [updatedBy, setUpdatedBy] = useState("SafarSuite Control Desk");
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const selectedOperator = useMemo(
    () => operators.find((operator) => operator.userId === selectedUserId) ?? null,
    [operators, selectedUserId]
  );
  const activeCount = operators.filter((operator) => isActive(operator.status)).length;
  const suspendedCount = operators.filter((operator) => !isActive(operator.status)).length;
  const scopesChanged = selectedOperator !== null
    && !sameScopes(selectedOperator.scopes, scopeDraft);
  const statusChanged = selectedOperator !== null
    && selectedOperator.status !== statusDraft;

  useEffect(() => {
    void loadOperators();
  }, []);

  useEffect(() => {
    if (session === null) {
      return;
    }

    setUpdatedBy(session.actor);
    setPasswordChangeForm((current) => ({
      ...current,
      email: session.email ?? current.email
    }));
    setCreateForm((current) => ({
      ...current,
      createdBy: session.actor
    }));
  }, [session]);

  useEffect(() => {
    if (selectedOperator === null) {
      setScopeDraft([]);
      setStatusDraft("Active");
      setPasswordDraft("");
      return;
    }

    setScopeDraft(selectedOperator.scopes);
    setStatusDraft(selectedOperator.status);
    setPasswordDraft("");
  }, [selectedOperator]);

  useEffect(() => {
    setGeneratedRecoveryCodes([]);
    setGeneratedTotpEnrollment(null);
  }, [selectedUserId]);

  async function handleCreateSession() {
    await runPanelAction(async () => {
      const createdSession = await createProviderAccessOperatorSession({
        ...sessionForm,
        scopes: normalizeScopes(sessionForm.scopes)
      });
      const storedSession = {
        ...createdSession,
        email: sessionForm.email.trim()
      };
      saveProviderAccessSession(storedSession);
      setSession(storedSession);
      setPasswordChangeForm({
        ...emptyPasswordChangeForm,
        email: sessionForm.email.trim()
      });
      setSessionForm({
        ...sessionForm,
        password: "",
        recoveryCode: "",
        totpCode: ""
      });
      setMessage(`Signed in provider operator ${createdSession.actor}.`);

      const nextOperators = sortOperators(await listProviderAccessOperators());
      setOperators(nextOperators);
      setSelectedUserId((current) =>
        nextOperators.some((operator) => operator.userId === current)
          ? current
          : nextOperators[0]?.userId ?? "");
    });
  }

  async function handleChangeOwnPassword() {
    if (passwordChangeForm.newPassword !== passwordChangeForm.confirmPassword) {
      setError("New provider operator password confirmation does not match.");
      return;
    }

    await runPanelAction(async () => {
      await changeProviderAccessOperatorPassword({
        email: passwordChangeForm.email,
        currentPassword: passwordChangeForm.currentPassword,
        newPassword: passwordChangeForm.newPassword
      });
      clearProviderAccessSession();
      setSession(null);
      setSessionForm({
        ...emptySessionForm,
        email: passwordChangeForm.email
      });
      setPasswordChangeForm({
        ...emptyPasswordChangeForm,
        email: passwordChangeForm.email
      });
      setMessage("Provider operator password changed. Sign in again with the new password.");
    });
  }

  function handleClearSession() {
    clearProviderAccessSession();
    setSession(null);
    setPasswordChangeForm(emptyPasswordChangeForm);
    setMessage("Provider operator session cleared.");
  }

  async function loadOperators() {
    await runPanelAction(async () => {
      const nextOperators = sortOperators(await listProviderAccessOperators());
      setOperators(nextOperators);
      setSelectedUserId((current) =>
        nextOperators.some((operator) => operator.userId === current)
          ? current
          : nextOperators[0]?.userId ?? "");
      setMessage(`Loaded ${nextOperators.length} provider operator(s).`);
    });
  }

  async function handleCreateOperator() {
    await runPanelAction(async () => {
      const created = await createProviderAccessOperator({
        ...createForm,
        scopes: normalizeScopes(createForm.scopes)
      });
      const nextOperators = upsertOperator(operators, created);
      setOperators(nextOperators);
      setSelectedUserId(created.userId);
      setCreateForm({
        ...emptyCreateOperatorForm,
        createdBy: createForm.createdBy
      });
      setMessage(`Created provider operator ${created.email}.`);
    });
  }

  async function handleSaveScopes() {
    if (selectedOperator === null) {
      return;
    }

    await runPanelAction(async () => {
      const updated = await updateProviderAccessOperatorScopes(selectedOperator.userId, {
        scopes: normalizeScopes(scopeDraft),
        updatedBy
      });
      setOperators(upsertOperator(operators, updated));
      setSelectedUserId(updated.userId);
      setMessage(`Updated rights for ${updated.email}.`);
    });
  }

  async function handleSaveStatus() {
    if (selectedOperator === null) {
      return;
    }

    await runPanelAction(async () => {
      const updated = await updateProviderAccessOperatorStatus(selectedOperator.userId, {
        status: statusDraft,
        updatedBy
      });
      setOperators(upsertOperator(operators, updated));
      setSelectedUserId(updated.userId);
      setMessage(`${updated.email} is now ${updated.status}.`);
    });
  }

  async function handleResetPassword() {
    if (selectedOperator === null) {
      return;
    }

    await runPanelAction(async () => {
      const updated = await resetProviderAccessOperatorPassword(selectedOperator.userId, {
        password: passwordDraft,
        updatedBy
      });
      setOperators(upsertOperator(operators, updated));
      setSelectedUserId(updated.userId);
      setPasswordDraft("");
      setMessage(`Password reset for ${updated.email}.`);
    });
  }

  async function handleResetRecoveryCodes() {
    if (selectedOperator === null) {
      return;
    }

    await runPanelAction(async () => {
      const result = await resetProviderAccessOperatorRecoveryCodes(selectedOperator.userId, {
        count: recoveryCodeCount,
        updatedBy
      });
      setOperators(upsertOperator(operators, result.operator));
      setSelectedUserId(result.operator.userId);
      setGeneratedRecoveryCodes(result.recoveryCodes);
      setGeneratedTotpEnrollment(null);
      setMessage(`Recovery codes reset for ${result.operator.email}.`);
    });
  }

  async function handleResetTotp() {
    if (selectedOperator === null) {
      return;
    }

    await runPanelAction(async () => {
      const result = await resetProviderAccessOperatorTotp(selectedOperator.userId, {
        updatedBy
      });
      setOperators(upsertOperator(operators, result.operator));
      setSelectedUserId(result.operator.userId);
      setGeneratedRecoveryCodes([]);
      setGeneratedTotpEnrollment({
        secret: result.secret,
        otpAuthUri: result.otpAuthUri
      });
      setMessage(`TOTP reset for ${result.operator.email}.`);
    });
  }

  async function runPanelAction(action: () => Promise<void>) {
    setIsBusy(true);
    setError("");

    try {
      await action();
    } catch (caughtError) {
      setError(toPanelError(caughtError));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <section className="client-panel provider-access-panel">
      <div className="client-panel-heading">
        <div>
          <span>Provider access</span>
          <strong>Operator rights</strong>
          <em>{activeCount} active / {suspendedCount} suspended</em>
        </div>
        <div className="client-panel-actions">
          <span className={`status-pill ${error === "" ? "active" : "suspended"}`}>
            {error === "" ? "Ready" : "Needs review"}
          </span>
          <button
            className="icon-button"
            type="button"
            disabled={isBusy}
            onClick={loadOperators}
            title="Refresh provider operators"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>
      </div>

      <div className="provider-access-session">
        <div className="provider-access-subheading">
          <LogIn size={16} />
          <strong>Operator session</strong>
        </div>

        {session === null ? (
          <div className="provider-access-session-form">
            <label>
              <span>Email</span>
              <input
                type="email"
                value={sessionForm.email}
                disabled={isBusy}
                maxLength={320}
                onChange={(event) => setSessionForm({
                  ...sessionForm,
                  email: event.target.value
                })}
              />
            </label>
            <label>
              <span>Password</span>
              <input
                type="password"
                value={sessionForm.password}
                disabled={isBusy}
                onChange={(event) => setSessionForm({
                  ...sessionForm,
                  password: event.target.value
                })}
              />
            </label>
            <label>
              <span>Recovery code</span>
              <input
                type="text"
                value={sessionForm.recoveryCode ?? ""}
                disabled={isBusy}
                maxLength={32}
                onChange={(event) => setSessionForm({
                  ...sessionForm,
                  recoveryCode: event.target.value
                })}
              />
            </label>
            <label>
              <span>TOTP code</span>
              <input
                type="text"
                inputMode="numeric"
                value={sessionForm.totpCode ?? ""}
                disabled={isBusy}
                maxLength={12}
                onChange={(event) => setSessionForm({
                  ...sessionForm,
                  totpCode: event.target.value
                })}
              />
            </label>
            <label>
              <span>Minutes</span>
              <input
                type="number"
                min={5}
                max={1440}
                value={sessionForm.expiresInMinutes}
                disabled={isBusy}
                onChange={(event) => setSessionForm({
                  ...sessionForm,
                  expiresInMinutes: Number(event.target.value)
                })}
              />
            </label>
            <ScopePicker
              value={sessionForm.scopes}
              disabled={isBusy}
              onChange={(scopes) => setSessionForm({
                ...sessionForm,
                scopes
              })}
            />
            <button
              className="icon-button primary"
              type="button"
              disabled={
                isBusy
                || sessionForm.email.trim() === ""
                || sessionForm.password.trim() === ""
                || normalizeScopes(sessionForm.scopes).length === 0
                || sessionForm.expiresInMinutes < 5
                || sessionForm.expiresInMinutes > 1440
              }
              onClick={handleCreateSession}
              title="Sign in provider operator"
            >
              <LogIn size={16} />
              Sign in
            </button>
          </div>
        ) : (
          <div className="provider-access-session-current">
            <dl className="provider-access-facts">
              <div>
                <dt>Actor</dt>
                <dd>{session.actor}</dd>
              </div>
              <div>
                <dt>Expires</dt>
                <dd>{formatNullableDateTime(session.expiresAtUtc)}</dd>
              </div>
              <div>
                <dt>Scopes</dt>
                <dd>{formatScopes(session.scopes)}</dd>
              </div>
            </dl>
            <button
              className="icon-button"
              type="button"
              disabled={isBusy}
              onClick={handleClearSession}
              title="Clear provider operator session"
            >
              <LogOut size={16} />
              Sign out
            </button>
            <div className="provider-access-password-change">
              <label>
                <span>Email</span>
                <input
                  type="email"
                  value={passwordChangeForm.email}
                  disabled={isBusy}
                  maxLength={320}
                  onChange={(event) => setPasswordChangeForm({
                    ...passwordChangeForm,
                    email: event.target.value
                  })}
                />
              </label>
              <label>
                <span>Current password</span>
                <input
                  type="password"
                  value={passwordChangeForm.currentPassword}
                  disabled={isBusy}
                  onChange={(event) => setPasswordChangeForm({
                    ...passwordChangeForm,
                    currentPassword: event.target.value
                  })}
                />
              </label>
              <label>
                <span>New password</span>
                <input
                  type="password"
                  value={passwordChangeForm.newPassword}
                  disabled={isBusy}
                  minLength={12}
                  onChange={(event) => setPasswordChangeForm({
                    ...passwordChangeForm,
                    newPassword: event.target.value
                  })}
                />
              </label>
              <label>
                <span>Confirm</span>
                <input
                  type="password"
                  value={passwordChangeForm.confirmPassword}
                  disabled={isBusy}
                  minLength={12}
                  onChange={(event) => setPasswordChangeForm({
                    ...passwordChangeForm,
                    confirmPassword: event.target.value
                  })}
                />
              </label>
              <button
                className="icon-button"
                type="button"
                disabled={
                  isBusy
                  || passwordChangeForm.email.trim() === ""
                  || passwordChangeForm.currentPassword.trim() === ""
                  || passwordChangeForm.newPassword.length < 12
                  || passwordChangeForm.confirmPassword.length < 12
                }
                onClick={handleChangeOwnPassword}
                title="Change provider operator password"
              >
                <KeyRound size={16} />
                Change
              </button>
            </div>
          </div>
        )}
      </div>

      {message !== "" && <div className="provider-access-message">{message}</div>}
      {error !== "" && <div className="provider-access-error">{error}</div>}

      <div className="provider-access-grid">
        <div className="provider-access-create">
          <div className="provider-access-subheading">
            <UserPlus size={16} />
            <strong>Create operator</strong>
          </div>

          <div className="provider-access-form-grid">
            <label>
              <span>Email</span>
              <input
                type="email"
                value={createForm.email}
                disabled={isBusy}
                maxLength={320}
                onChange={(event) => setCreateForm({
                  ...createForm,
                  email: event.target.value
                })}
              />
            </label>
            <label>
              <span>Full name</span>
              <input
                type="text"
                value={createForm.fullName}
                disabled={isBusy}
                maxLength={180}
                onChange={(event) => setCreateForm({
                  ...createForm,
                  fullName: event.target.value
                })}
              />
            </label>
            <label>
              <span>Temporary password</span>
              <input
                type="password"
                value={createForm.password}
                disabled={isBusy}
                minLength={12}
                onChange={(event) => setCreateForm({
                  ...createForm,
                  password: event.target.value
                })}
              />
            </label>
            <label>
              <span>Created by</span>
              <input
                type="text"
                value={createForm.createdBy}
                disabled={isBusy}
                maxLength={120}
                onChange={(event) => setCreateForm({
                  ...createForm,
                  createdBy: event.target.value
                })}
              />
            </label>
          </div>

          <ScopePicker
            value={createForm.scopes}
            disabled={isBusy}
            onChange={(scopes) => setCreateForm({
              ...createForm,
              scopes
            })}
          />

          <button
            className="icon-button primary"
            type="button"
            disabled={
              isBusy
              || createForm.email.trim() === ""
              || createForm.fullName.trim() === ""
              || createForm.password.length < 12
              || normalizeScopes(createForm.scopes).length === 0
            }
            onClick={handleCreateOperator}
            title="Create provider operator"
          >
            <Plus size={16} />
            Create
          </button>
        </div>

        <div className="provider-access-register">
          <div className="provider-access-subheading">
            <ShieldCheck size={16} />
            <strong>Operators</strong>
          </div>
          <div className="provider-access-operator-list">
            {operators.length === 0 ? (
              <div className="client-empty-state">
                No provider operators loaded.
              </div>
            ) : operators.map((operator) => (
              <button
                className={`provider-access-operator ${operator.userId === selectedUserId ? "selected" : ""}`}
                type="button"
                key={operator.userId}
                disabled={isBusy}
                onClick={() => setSelectedUserId(operator.userId)}
              >
                <span className={`status-pill ${operator.status.toLowerCase()}`}>
                  {operator.status}
                </span>
                <span>
                  <strong>{operator.fullName}</strong>
                  <small>{operator.email}</small>
                </span>
                <em>{shortIdentifier(operator.userId)}</em>
              </button>
            ))}
          </div>
        </div>

        <div className="provider-access-editor">
          <div className="provider-access-subheading">
            <KeyRound size={16} />
            <strong>Selected operator</strong>
          </div>

          {selectedOperator === null ? (
            <div className="client-empty-state">
              Select an operator to manage rights.
            </div>
          ) : (
            <>
              <dl className="provider-access-facts">
                <div>
                  <dt>Email</dt>
                  <dd>{selectedOperator.email}</dd>
                </div>
                <div>
                  <dt>Last login</dt>
                  <dd>{formatNullableDateTime(selectedOperator.lastLoginAtUtc)}</dd>
                </div>
                <div>
                  <dt>Updated</dt>
                  <dd>{formatNullableDateTime(selectedOperator.updatedAtUtc)}</dd>
                </div>
                <div>
                  <dt>MFA</dt>
                  <dd>{formatMfaSummary(selectedOperator)}</dd>
                </div>
                <div>
                  <dt>Login guard</dt>
                  <dd>{formatLoginGuard(selectedOperator)}</dd>
                </div>
              </dl>

              <label className="provider-access-actor-field">
                <span>Updated by</span>
                <input
                  type="text"
                  value={updatedBy}
                  disabled={isBusy}
                  maxLength={120}
                  onChange={(event) => setUpdatedBy(event.target.value)}
                />
              </label>

              <ScopePicker
                value={scopeDraft}
                disabled={isBusy}
                onChange={setScopeDraft}
              />

              <div className="provider-access-action-row">
                <button
                  className="icon-button primary"
                  type="button"
                  disabled={
                    isBusy
                    || !scopesChanged
                    || normalizeScopes(scopeDraft).length === 0
                    || updatedBy.trim() === ""
                  }
                  onClick={handleSaveScopes}
                  title="Save provider operator rights"
                >
                  <Save size={16} />
                  Save rights
                </button>
              </div>

              <div className="provider-access-status-row">
                <label>
                  <span>Status</span>
                  <select
                    value={statusDraft}
                    disabled={isBusy}
                    onChange={(event) => setStatusDraft(event.target.value)}
                  >
                    <option value="Active">Active</option>
                    <option value="Suspended">Suspended</option>
                  </select>
                </label>
                <button
                  className={`icon-button ${statusDraft === "Suspended" ? "danger" : "primary"}`}
                  type="button"
                  disabled={isBusy || !statusChanged || updatedBy.trim() === ""}
                  onClick={handleSaveStatus}
                  title="Update provider operator status"
                >
                  {statusDraft === "Suspended" ? <ShieldOff size={16} /> : <ShieldCheck size={16} />}
                  Update
                </button>
              </div>

              <div className="provider-access-status-row">
                <label>
                  <span>New password</span>
                  <input
                    type="password"
                    value={passwordDraft}
                    disabled={isBusy}
                    minLength={12}
                    onChange={(event) => setPasswordDraft(event.target.value)}
                  />
                </label>
                <button
                  className="icon-button"
                  type="button"
                  disabled={isBusy || passwordDraft.length < 12 || updatedBy.trim() === ""}
                  onClick={handleResetPassword}
                  title="Reset provider operator password"
                >
                  <KeyRound size={16} />
                  Reset
                </button>
              </div>

              <div className="provider-access-status-row">
                <label>
                  <span>Recovery codes</span>
                  <input
                    type="number"
                    min={1}
                    max={20}
                    value={recoveryCodeCount}
                    disabled={isBusy}
                    onChange={(event) => setRecoveryCodeCount(Number(event.target.value))}
                  />
                </label>
                <button
                  className="icon-button"
                  type="button"
                  disabled={
                    isBusy
                    || recoveryCodeCount < 1
                    || recoveryCodeCount > 20
                    || updatedBy.trim() === ""
                  }
                  onClick={handleResetRecoveryCodes}
                  title="Reset provider operator recovery codes"
                >
                  <ShieldCheck size={16} />
                  Reset codes
                </button>
              </div>

              {generatedRecoveryCodes.length > 0 && (
                <div className="provider-access-recovery-codes">
                  {generatedRecoveryCodes.map((code) => (
                    <code key={code}>{code}</code>
                  ))}
                </div>
              )}

              <div className="provider-access-status-row">
                <label className="provider-access-checkbox-state">
                  <span>TOTP</span>
                  <input type="checkbox" checked={selectedOperator.totpEnabled === true} readOnly />
                </label>
                <button
                  className="icon-button"
                  type="button"
                  disabled={isBusy || updatedBy.trim() === ""}
                  onClick={handleResetTotp}
                  title="Reset provider operator TOTP"
                >
                  <ShieldCheck size={16} />
                  Reset TOTP
                </button>
              </div>

              {generatedTotpEnrollment !== null && (
                <div className="provider-access-totp-enrollment">
                  <code>{generatedTotpEnrollment.secret}</code>
                  <code>{generatedTotpEnrollment.otpAuthUri}</code>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </section>
  );
}

function ScopePicker({
  value,
  disabled,
  onChange
}: {
  value: string[];
  disabled: boolean;
  onChange: (value: string[]) => void;
}) {
  return (
    <div className="provider-scope-picker">
      {providerScopeOptions.map((option) => {
        const checked = hasScope(value, option.scope);

        return (
          <label
            className={option.warning ? "warning" : ""}
            key={option.scope}
          >
            <input
              type="checkbox"
              checked={checked}
              disabled={disabled}
              onChange={(event) => onChange(toggleScope(value, option.scope, event.target.checked))}
            />
            <span>
              <strong>{option.label}</strong>
              <small>{option.detail}</small>
            </span>
          </label>
        );
      })}
    </div>
  );
}

function toggleScope(scopes: string[], scope: string, checked: boolean): string[] {
  if (!checked) {
    return normalizeScopes(scopes.filter((candidate) =>
      candidate.toLowerCase() !== scope.toLowerCase()));
  }

  if (scope === "*") {
    return ["*"];
  }

  return normalizeScopes([
    ...scopes.filter((candidate) => candidate !== "*"),
    scope
  ]);
}

function hasScope(scopes: string[], scope: string): boolean {
  return scopes.some((candidate) => candidate.toLowerCase() === scope.toLowerCase());
}

function normalizeScopes(scopes: string[]): string[] {
  return scopes
    .map((scope) => scope.trim())
    .filter((scope) => scope !== "")
    .filter((scope, index, all) =>
      all.findIndex((candidate) => candidate.toLowerCase() === scope.toLowerCase()) === index);
}

function sameScopes(left: string[], right: string[]): boolean {
  const normalizedLeft = normalizeScopes(left).map((scope) => scope.toLowerCase()).sort();
  const normalizedRight = normalizeScopes(right).map((scope) => scope.toLowerCase()).sort();

  return normalizedLeft.length === normalizedRight.length
    && normalizedLeft.every((scope, index) => scope === normalizedRight[index]);
}

function upsertOperator(
  operators: ProviderAccessOperator[],
  providerOperator: ProviderAccessOperator
): ProviderAccessOperator[] {
  return sortOperators([
    ...operators.filter((operator) => operator.userId !== providerOperator.userId),
    providerOperator
  ]);
}

function sortOperators(operators: ProviderAccessOperator[]): ProviderAccessOperator[] {
  return [...operators].sort((left, right) =>
    left.email.localeCompare(right.email, undefined, { sensitivity: "base" }));
}

function isActive(status: string): boolean {
  return status.trim().toLowerCase() === "active";
}

function formatScopes(scopes: string[]): string {
  return scopes.some((scope) => scope === "*")
    ? "All scopes"
    : scopes.join(", ");
}

function formatMfaSummary(providerOperator: ProviderAccessOperator): string {
  const parts: string[] = [];

  if (providerOperator.totpEnabled === true) {
    parts.push("TOTP");
  }

  if ((providerOperator.recoveryCodeCount ?? 0) > 0) {
    parts.push(`${providerOperator.recoveryCodeCount} codes`);
  }

  return parts.length > 0 ? parts.join(" + ") : "Not enabled";
}

function formatLoginGuard(providerOperator: ProviderAccessOperator): string {
  if (providerOperator.lockoutEndsAtUtc !== null && providerOperator.lockoutEndsAtUtc !== undefined) {
    return `Locked until ${formatNullableDateTime(providerOperator.lockoutEndsAtUtc)}`;
  }

  const failedCount = providerOperator.failedLoginAttemptCount ?? 0;

  return failedCount > 0 ? `${failedCount} failed` : "Clear";
}

function toPanelError(caughtError: unknown): string {
  if (caughtError instanceof ApiError) {
    return caughtError.errors[0]?.message ?? caughtError.message;
  }

  if (caughtError instanceof Error) {
    return caughtError.message;
  }

  return "Provider access request failed.";
}
