const elements = {
  clientIdInput: document.querySelector("#clientIdInput"),
  installationIdInput: document.querySelector("#installationIdInput"),
  emailInput: document.querySelector("#emailInput"),
  passwordInput: document.querySelector("#passwordInput"),
  inviteTokenInput: document.querySelector("#inviteTokenInput"),
  inviteFullNameInput: document.querySelector("#inviteFullNameInput"),
  invitePasswordInput: document.querySelector("#invitePasswordInput"),
  acceptInviteButton: document.querySelector("#acceptInviteButton"),
  loadButton: document.querySelector("#loadButton"),
  connectionStatus: document.querySelector("#connectionStatus"),
  accountState: document.querySelector("#accountState"),
  balanceDue: document.querySelector("#balanceDue"),
  availableCredit: document.querySelector("#availableCredit"),
  commercialUpdated: document.querySelector("#commercialUpdated"),
  licenseState: document.querySelector("#licenseState"),
  paidUntil: document.querySelector("#paidUntil"),
  graceUntil: document.querySelector("#graceUntil"),
  offlineValidUntil: document.querySelector("#offlineValidUntil"),
  installationState: document.querySelector("#installationState"),
  lastHeartbeat: document.querySelector("#lastHeartbeat"),
  localServerVersion: document.querySelector("#localServerVersion"),
  commandState: document.querySelector("#commandState"),
  pairingState: document.querySelector("#pairingState"),
  pairingMode: document.querySelector("#pairingMode"),
  firstManagerState: document.querySelector("#firstManagerState"),
  pairingDevices: document.querySelector("#pairingDevices"),
  pairingLastUpdate: document.querySelector("#pairingLastUpdate"),
  invoiceCount: document.querySelector("#invoiceCount"),
  invoiceRows: document.querySelector("#invoiceRows"),
  moduleCount: document.querySelector("#moduleCount"),
  moduleList: document.querySelector("#moduleList")
};

const params = new URLSearchParams(window.location.search);
elements.clientIdInput.value = params.get("clientId") ?? "";
elements.installationIdInput.value = params.get("installationId") ?? "office-main";
elements.emailInput.value = params.get("email") ?? "";
elements.inviteTokenInput.value = params.get("invite") ?? "";
elements.loadButton.addEventListener("click", () => {
  void loadPortalState();
});
elements.acceptInviteButton.addEventListener("click", () => {
  void acceptInvitation();
});

if (elements.clientIdInput.value.trim() !== "" && elements.emailInput.value.trim() !== "") {
  void loadPortalState();
}

async function loadPortalState() {
  const clientId = elements.clientIdInput.value.trim();
  const installationId = elements.installationIdInput.value.trim();
  const email = elements.emailInput.value.trim();
  const password = elements.passwordInput.value;

  if (clientId === "") {
    setStatus("Client id is required.", "error");
    return;
  }

  if (email === "" || password === "") {
    setStatus("Email and password are required.", "error");
    return;
  }

  setBusy(true);
  setStatus("Creating session", "");

  try {
    const session = await createSession(clientId, email, password);
    setStatus("Loading", "");

    const [commercialSummary, installationStatus] = await Promise.all([
      getJson(
        `/api/v1/client-portal/clients/${encodeURIComponent(clientId)}/commercial-summary`,
        session.accessToken
      ),
      installationId === ""
        ? Promise.resolve(null)
        : getJson(
            `/api/v1/client-portal/clients/${encodeURIComponent(
              clientId
            )}/installations/${encodeURIComponent(installationId)}/status`,
            session.accessToken
          )
    ]);

    renderCommercialSummary(commercialSummary);
    renderInstallationStatus(installationStatus);
    setStatus("Loaded", "ready");
  } catch (error) {
    setStatus(formatError(error), "error");
  } finally {
    setBusy(false);
  }
}

async function acceptInvitation() {
  const invitationToken = elements.inviteTokenInput.value.trim();
  const password = elements.invitePasswordInput.value;
  const fullName = elements.inviteFullNameInput.value.trim();

  if (invitationToken === "") {
    setStatus("Invitation token is required.", "error");
    return;
  }

  if (password.length < 8) {
    setStatus("New password must be at least 8 characters.", "error");
    return;
  }

  setBusy(true);
  setStatus("Accepting invitation", "");

  try {
    const accepted = await postJson("/api/v1/client-portal/invitations/accept", {
      invitationToken,
      password,
      fullName
    });
    elements.clientIdInput.value = accepted.clientId;
    elements.emailInput.value = accepted.email;
    elements.passwordInput.value = password;
    renderAcceptedSession(accepted);
    setStatus("Invitation accepted", "ready");
  } catch (error) {
    setStatus(formatError(error), "error");
  } finally {
    setBusy(false);
  }
}

async function createSession(clientId, email, password) {
  return postJson("/api/v1/client-portal/sessions", {
    clientId,
    email,
    password
  });
}

async function postJson(path, body) {
  const response = await fetch(path, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    throw new Error(await readError(response));
  }

  return response.json();
}

async function getJson(path, accessToken) {
  const response = await fetch(path, {
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`
    }
  });

  if (!response.ok) {
    throw new Error(await readError(response));
  }

  return response.json();
}

function renderCommercialSummary(summary) {
  const entitlement = summary.latestEntitlement ?? null;
  elements.accountState.textContent = summary.isPaid ? "Paid" : "Balance due";
  elements.balanceDue.textContent = formatMoney(summary.balanceDue, summary.currencyCode);
  elements.availableCredit.textContent = formatMoney(summary.availableCredit, summary.currencyCode);
  elements.commercialUpdated.textContent = formatDateTime(summary.lastUpdatedAtUtc);
  elements.licenseState.textContent = entitlement?.status ?? "Not issued";
  elements.paidUntil.textContent = formatDate(entitlement?.paidUntil);
  elements.graceUntil.textContent = formatDate(entitlement?.graceUntil);
  elements.offlineValidUntil.textContent = formatDate(entitlement?.offlineValidUntil);

  renderInvoices(summary.invoices ?? [], summary.currencyCode);
  renderModules(entitlement?.modules ?? []);
}

function renderInstallationStatus(status) {
  if (status === null) {
    elements.installationState.textContent = "Not selected";
    elements.lastHeartbeat.textContent = "-";
    elements.localServerVersion.textContent = "-";
    elements.commandState.textContent = "-";
    renderPairingStatus(null);
    return;
  }

  const heartbeat = status.latestHeartbeat ?? null;
  const commandStatus = status.commandStatus ?? null;
  elements.installationState.textContent = status.installationStatus;
  elements.lastHeartbeat.textContent = formatDateTime(heartbeat?.receivedAtUtc);
  elements.localServerVersion.textContent = heartbeat?.localServerVersion ?? "Not reported";
  elements.commandState.textContent =
    `${commandStatus?.pendingCommandCount ?? 0} pending`
    + ` / ${commandStatus?.latestCommandStatus ?? "no command"}`;
  renderPairingStatus(heartbeat?.pairingStatus ?? null);

  if (heartbeat?.licenseStatus !== undefined && heartbeat.licenseStatus !== null) {
    elements.licenseState.textContent = heartbeat.licenseStatus;
  }
}

function renderPairingStatus(pairingStatus) {
  if (pairingStatus === null) {
    elements.pairingState.textContent = "Not reported";
    elements.pairingMode.textContent = "-";
    elements.firstManagerState.textContent = "-";
    elements.pairingDevices.textContent = "-";
    elements.pairingLastUpdate.textContent = "-";
    return;
  }

  elements.pairingState.textContent = pairingStatus.firstManagerDeviceApproved
    ? "Ready"
    : "Manager needed";
  elements.pairingMode.textContent = pairingStatus.pairingMode ?? "Not reported";
  elements.firstManagerState.textContent = pairingStatus.firstManagerDeviceApproved
    ? "Approved"
    : "Needed";
  elements.pairingDevices.textContent = formatPairingDevices(pairingStatus);
  elements.pairingLastUpdate.textContent = formatDateTime(pairingStatus.lastDeviceUpdatedAtUtc);
}

function renderAcceptedSession(accepted) {
  elements.accountState.textContent = "Session ready";
  elements.licenseState.textContent = accepted.role;
  elements.installationState.textContent = "Log in ready";
  elements.lastHeartbeat.textContent = "-";
  elements.localServerVersion.textContent = "-";
  elements.commandState.textContent = "-";
  renderPairingStatus(null);
}

function renderInvoices(invoices, currencyCode) {
  elements.invoiceCount.textContent = invoices.length.toString();

  if (invoices.length === 0) {
    elements.invoiceRows.innerHTML = "<tr><td colspan=\"4\">No invoices found</td></tr>";
    return;
  }

  elements.invoiceRows.replaceChildren(
    ...invoices.slice(-8).reverse().map((invoice) => {
      const row = document.createElement("tr");
      row.append(
        cell(invoice.invoiceNumber),
        cell(invoice.invoiceStatus),
        cell(formatDate(invoice.dueDate)),
        cell(formatMoney(invoice.balanceDue, invoice.currencyCode ?? currencyCode))
      );
      return row;
    })
  );
}

function renderModules(modules) {
  elements.moduleCount.textContent = modules.length.toString();

  if (modules.length === 0) {
    elements.moduleList.replaceChildren(modulePill("No modules loaded", ""));
    return;
  }

  elements.moduleList.replaceChildren(
    ...modules.map((module) =>
      modulePill(module.moduleCode, module.isEnabled ? "enabled" : "disabled")
    )
  );
}

function cell(value) {
  const item = document.createElement("td");
  item.textContent = value ?? "-";

  return item;
}

function modulePill(text, className) {
  const item = document.createElement("span");
  item.textContent = text;
  item.className = className;

  return item;
}

function formatPairingDevices(pairingStatus) {
  const approvedCount = pairingStatus.approvedDeviceCount ?? 0;
  const totalCount = pairingStatus.totalDeviceCount ?? 0;
  const pendingCount = pairingStatus.pendingDeviceCount ?? 0;
  const revokedCount = pairingStatus.revokedDeviceCount ?? 0;
  const details = [];

  if (pendingCount > 0) {
    details.push(`${pendingCount} pending`);
  }

  if (revokedCount > 0) {
    details.push(`${revokedCount} revoked`);
  }

  return details.length === 0
    ? `${approvedCount}/${totalCount} approved`
    : `${approvedCount}/${totalCount} approved, ${details.join(", ")}`;
}

function setBusy(isBusy) {
  elements.loadButton.disabled = isBusy;
  elements.acceptInviteButton.disabled = isBusy;
  elements.clientIdInput.disabled = isBusy;
  elements.installationIdInput.disabled = isBusy;
  elements.emailInput.disabled = isBusy;
  elements.passwordInput.disabled = isBusy;
  elements.inviteTokenInput.disabled = isBusy;
  elements.inviteFullNameInput.disabled = isBusy;
  elements.invitePasswordInput.disabled = isBusy;
}

function setStatus(message, className) {
  elements.connectionStatus.textContent = message;
  elements.connectionStatus.className = `portal-status ${className}`.trim();
}

async function readError(response) {
  try {
    const body = await response.json();
    return body.detail ?? body.title ?? body.code ?? response.statusText;
  } catch {
    return response.statusText;
  }
}

function formatError(error) {
  return error instanceof Error ? error.message : "Unexpected error.";
}

function formatMoney(amount, currencyCode) {
  const safeAmount = Number.isFinite(amount) ? amount : 0;
  const normalizedCurrency = typeof currencyCode === "string" && currencyCode.trim() !== ""
    ? currencyCode.trim().toUpperCase()
    : "PKR";

  return `${safeAmount.toFixed(2)} ${normalizedCurrency}`;
}

function formatDate(value) {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

function formatDateTime(value) {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}
