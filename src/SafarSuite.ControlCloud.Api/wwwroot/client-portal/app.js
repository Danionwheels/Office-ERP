const SESSION_STORAGE_KEY = "safarsuite-client-portal-session";
const PORTAL_API_BASE = "/portal/api/v1";
const MAX_PROOF_SIZE_BYTES = 5 * 1024 * 1024;
const MAX_TRANSFER_REFERENCE_LENGTH = 80;
const ALLOWED_PROOF_TYPES = new Set(["image/jpeg", "image/png", "application/pdf"]);

const AuthView = Object.freeze({
  Login: "login",
  Invitation: "invitation",
  Mfa: "mfa",
  ForgotPassword: "forgot-password",
  ResetPassword: "reset-password"
});

const PortalView = Object.freeze({
  Overview: "overview",
  Billing: "billing",
  InvoiceDetail: "invoice-detail",
  Payment: "payment",
  Claims: "claims",
  ClaimDetail: "claim-detail"
});

const elements = {
  authZone: document.querySelector("#authZone"),
  portalContent: document.querySelector("#portalContent"),
  loginView: document.querySelector("#loginView"),
  invitationView: document.querySelector("#invitationView"),
  mfaView: document.querySelector("#mfaView"),
  forgotPasswordView: document.querySelector("#forgotPasswordView"),
  resetPasswordView: document.querySelector("#resetPasswordView"),
  clientIdInput: document.querySelector("#clientIdInput"),
  installationIdInput: document.querySelector("#installationIdInput"),
  emailInput: document.querySelector("#emailInput"),
  passwordInput: document.querySelector("#passwordInput"),
  loadButton: document.querySelector("#loadButton"),
  forgotPasswordButton: document.querySelector("#forgotPasswordButton"),
  showInvitationButton: document.querySelector("#showInvitationButton"),
  inviteTokenInput: document.querySelector("#inviteTokenInput"),
  inviteFullNameInput: document.querySelector("#inviteFullNameInput"),
  invitePasswordInput: document.querySelector("#invitePasswordInput"),
  invitationHint: document.querySelector("#invitationHint"),
  acceptInviteButton: document.querySelector("#acceptInviteButton"),
  useTotpButton: document.querySelector("#useTotpButton"),
  useRecoveryButton: document.querySelector("#useRecoveryButton"),
  totpChallengeField: document.querySelector("#totpChallengeField"),
  recoveryChallengeField: document.querySelector("#recoveryChallengeField"),
  totpCodeInput: document.querySelector("#totpCodeInput"),
  recoveryCodeInput: document.querySelector("#recoveryCodeInput"),
  cancelMfaButton: document.querySelector("#cancelMfaButton"),
  resetClientIdInput: document.querySelector("#resetClientIdInput"),
  resetEmailInput: document.querySelector("#resetEmailInput"),
  newPasswordInput: document.querySelector("#newPasswordInput"),
  confirmPasswordInput: document.querySelector("#confirmPasswordInput"),
  sessionSummary: document.querySelector("#sessionSummary"),
  sessionIdentity: document.querySelector("#sessionIdentity"),
  logoutButton: document.querySelector("#logoutButton"),
  logoutAllButton: document.querySelector("#logoutAllButton"),
  connectionStatus: document.querySelector("#connectionStatus"),
  enrollmentStart: document.querySelector("#enrollmentStart"),
  enrollmentPasswordInput: document.querySelector("#enrollmentPasswordInput"),
  startEnrollmentButton: document.querySelector("#startEnrollmentButton"),
  enrollmentPanel: document.querySelector("#enrollmentPanel"),
  totpQrCode: document.querySelector("#totpQrCode"),
  totpSecret: document.querySelector("#totpSecret"),
  otpAuthUriLink: document.querySelector("#otpAuthUriLink"),
  totpConfirmForm: document.querySelector("#totpConfirmForm"),
  totpEnrollmentCodeInput: document.querySelector("#totpEnrollmentCodeInput"),
  cancelEnrollmentButton: document.querySelector("#cancelEnrollmentButton"),
  recoveryCodesPanel: document.querySelector("#recoveryCodesPanel"),
  recoveryCodeList: document.querySelector("#recoveryCodeList"),
  copyRecoveryCodesButton: document.querySelector("#copyRecoveryCodesButton"),
  finishRecoveryCodesButton: document.querySelector("#finishRecoveryCodesButton"),
  mfaState: document.querySelector("#mfaState"),
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
  invoiceMoreButton: document.querySelector("#invoiceMoreButton"),
  moduleCount: document.querySelector("#moduleCount"),
  moduleList: document.querySelector("#moduleList"),
  portalNavButtons: [...document.querySelectorAll("[data-portal-view]")],
  topNavButtons: [...document.querySelectorAll(".portal-nav-button")],
  billingNotice: document.querySelector("#billingNotice"),
  overviewView: document.querySelector("#overviewView"),
  billingView: document.querySelector("#billingView"),
  invoiceDetailView: document.querySelector("#invoiceDetailView"),
  paymentView: document.querySelector("#paymentView"),
  claimsView: document.querySelector("#claimsView"),
  claimDetailView: document.querySelector("#claimDetailView"),
  refreshBillingButton: document.querySelector("#refreshBillingButton"),
  billingSummaryState: document.querySelector("#billingSummaryState"),
  billingTotalOutstanding: document.querySelector("#billingTotalOutstanding"),
  billingUnpaidCount: document.querySelector("#billingUnpaidCount"),
  billingLastPayment: document.querySelector("#billingLastPayment"),
  billingInvoiceCount: document.querySelector("#billingInvoiceCount"),
  invoiceStatusFilter: document.querySelector("#invoiceStatusFilter"),
  billingInvoiceRows: document.querySelector("#billingInvoiceRows"),
  billingInvoicesEmpty: document.querySelector("#billingInvoicesEmpty"),
  invoicePayButton: document.querySelector("#invoicePayButton"),
  detailInvoiceNumber: document.querySelector("#detailInvoiceNumber"),
  detailInvoiceStatus: document.querySelector("#detailInvoiceStatus"),
  detailIssueDate: document.querySelector("#detailIssueDate"),
  detailDueDate: document.querySelector("#detailDueDate"),
  detailClientName: document.querySelector("#detailClientName"),
  detailClientContact: document.querySelector("#detailClientContact"),
  invoiceLineRows: document.querySelector("#invoiceLineRows"),
  detailInvoiceTotal: document.querySelector("#detailInvoiceTotal"),
  detailPaymentsApplied: document.querySelector("#detailPaymentsApplied"),
  detailBalanceRemaining: document.querySelector("#detailBalanceRemaining"),
  detailPaymentCount: document.querySelector("#detailPaymentCount"),
  invoicePaymentRows: document.querySelector("#invoicePaymentRows"),
  invoicePaymentsEmpty: document.querySelector("#invoicePaymentsEmpty"),
  detailClaimCount: document.querySelector("#detailClaimCount"),
  invoiceClaimList: document.querySelector("#invoiceClaimList"),
  invoiceClaimsEmpty: document.querySelector("#invoiceClaimsEmpty"),
  paymentBackButton: document.querySelector("#paymentBackButton"),
  paymentEntryStep: document.querySelector("#paymentEntryStep"),
  paymentConfirmStep: document.querySelector("#paymentConfirmStep"),
  paymentSuccessStep: document.querySelector("#paymentSuccessStep"),
  bankDetailsState: document.querySelector("#bankDetailsState"),
  bankName: document.querySelector("#bankName"),
  bankAccountTitle: document.querySelector("#bankAccountTitle"),
  bankAccountNumber: document.querySelector("#bankAccountNumber"),
  bankIban: document.querySelector("#bankIban"),
  bankBranchInfo: document.querySelector("#bankBranchInfo"),
  paymentForm: document.querySelector("#paymentForm"),
  paymentInvoiceNumber: document.querySelector("#paymentInvoiceNumber"),
  paymentAmountInput: document.querySelector("#paymentAmountInput"),
  paymentBalanceHint: document.querySelector("#paymentBalanceHint"),
  paymentReferenceInput: document.querySelector("#paymentReferenceInput"),
  paymentProofInput: document.querySelector("#paymentProofInput"),
  confirmInvoiceNumber: document.querySelector("#confirmInvoiceNumber"),
  confirmPaymentAmount: document.querySelector("#confirmPaymentAmount"),
  confirmTransferReference: document.querySelector("#confirmTransferReference"),
  confirmProofName: document.querySelector("#confirmProofName"),
  editPaymentButton: document.querySelector("#editPaymentButton"),
  submitPaymentButton: document.querySelector("#submitPaymentButton"),
  successClaimReference: document.querySelector("#successClaimReference"),
  successSubmittedAt: document.querySelector("#successSubmittedAt"),
  successAmount: document.querySelector("#successAmount"),
  successTransferReference: document.querySelector("#successTransferReference"),
  printSuccessButton: document.querySelector("#printSuccessButton"),
  viewClaimsButton: document.querySelector("#viewClaimsButton"),
  refreshClaimsButton: document.querySelector("#refreshClaimsButton"),
  claimHistoryCount: document.querySelector("#claimHistoryCount"),
  claimHistoryList: document.querySelector("#claimHistoryList"),
  claimsEmpty: document.querySelector("#claimsEmpty"),
  printClaimButton: document.querySelector("#printClaimButton"),
  claimDetailReference: document.querySelector("#claimDetailReference"),
  claimDetailStatus: document.querySelector("#claimDetailStatus"),
  claimDetailNotice: document.querySelector("#claimDetailNotice"),
  claimDetailInvoice: document.querySelector("#claimDetailInvoice"),
  claimDetailAmount: document.querySelector("#claimDetailAmount"),
  claimDetailTransferReference: document.querySelector("#claimDetailTransferReference"),
  claimDetailSubmitted: document.querySelector("#claimDetailSubmitted"),
  claimDetailReviewed: document.querySelector("#claimDetailReviewed"),
  claimDetailProof: document.querySelector("#claimDetailProof"),
  claimRejectionPanel: document.querySelector("#claimRejectionPanel"),
  claimRejectionReason: document.querySelector("#claimRejectionReason")
};

const authState = {
  view: AuthView.Login,
  session: null,
  installationId: "office-main",
  invitationToken: "",
  resetToken: "",
  pendingCredentials: null,
  mfaMethod: "totp",
  enrollment: null
};

let refreshPromise = null;
let invoicePageState = emptyInvoicePageState();
let portalView = PortalView.Overview;
let billingState = emptyBillingState();

class ApiError extends Error {
  constructor(status, code, message) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

void initialize();

async function initialize() {
  const params = new URLSearchParams(window.location.search);
  const fragmentParams = new URLSearchParams(window.location.hash.replace(/^#/, ""));
  authState.invitationToken = params.get("invite") ?? fragmentParams.get("invite") ?? "";
  authState.resetToken = params.get("reset") ?? fragmentParams.get("reset") ?? "";
  elements.clientIdInput.value = params.get("clientId") ?? "";
  elements.installationIdInput.value = params.get("installationId") ?? "office-main";
  elements.emailInput.value = params.get("email") ?? "";
  stripSensitiveUrlTokens();
  bindEvents();

  if (authState.resetToken !== "") {
    clearStoredSession();
    authState.view = AuthView.Login;
    renderAuthState();
    setStatus("Validating password reset link", "");

    try {
      const validation = await requestJson("/api/v1/client-portal/password-resets/validate", {
        method: "POST",
        body: { resetToken: authState.resetToken }
      });

      if (validation?.isValid === true) {
        authState.view = AuthView.ResetPassword;
        setStatus("Choose a new password", "ready");
      } else {
        authState.resetToken = "";
        setStatus("This password reset link is invalid, expired, or already used.", "error");
      }
    } catch (error) {
      authState.resetToken = "";
      setStatus(formatError(error), "error");
    }
  } else if (authState.invitationToken !== "") {
    clearStoredSession();
    authState.view = AuthView.Invitation;
    elements.invitationHint.textContent = "Invitation link loaded. Choose your password to continue.";
    elements.inviteTokenInput.placeholder = "Invitation link loaded";
  } else {
    restoreSession();
  }

  renderAuthState();

  if (authState.session !== null) {
    void loadPortalState();
  }
}

function bindEvents() {
  elements.loginView.addEventListener("submit", (event) => void submitLogin(event));
  elements.invitationView.addEventListener("submit", (event) => void acceptInvitation(event));
  elements.mfaView.addEventListener("submit", (event) => void submitMfa(event));
  elements.forgotPasswordView.addEventListener("submit", (event) => void requestPasswordReset(event));
  elements.resetPasswordView.addEventListener("submit", (event) => void resetPassword(event));
  elements.forgotPasswordButton.addEventListener("click", showForgotPassword);
  elements.showInvitationButton.addEventListener("click", () => setAuthView(AuthView.Invitation));
  document.querySelectorAll(".back-to-login").forEach((button) => {
    button.addEventListener("click", returnToLogin);
  });
  elements.useTotpButton.addEventListener("click", () => setMfaMethod("totp"));
  elements.useRecoveryButton.addEventListener("click", () => setMfaMethod("recovery"));
  elements.cancelMfaButton.addEventListener("click", returnToLogin);
  elements.logoutButton.addEventListener("click", () => void logout(false));
  elements.logoutAllButton.addEventListener("click", () => void logout(true));
  elements.startEnrollmentButton.addEventListener("click", () => void startTotpEnrollment());
  elements.totpConfirmForm.addEventListener("submit", (event) => void confirmTotpEnrollment(event));
  elements.cancelEnrollmentButton.addEventListener("click", cancelTotpEnrollment);
  elements.copyRecoveryCodesButton.addEventListener("click", () => void copyRecoveryCodes());
  elements.finishRecoveryCodesButton.addEventListener("click", finishRecoveryCodes);
  elements.invoiceMoreButton.addEventListener("click", () => void loadMoreInvoices());
  elements.portalNavButtons.forEach((button) => {
    button.addEventListener("click", () => void navigatePortal(button.dataset.portalView));
  });
  elements.refreshBillingButton.addEventListener("click", () => void loadBilling(true));
  elements.invoiceStatusFilter.addEventListener("change", renderBillingInvoices);
  elements.invoicePayButton.addEventListener("click", startPaymentSubmission);
  elements.paymentBackButton.addEventListener("click", () => void navigatePortal(PortalView.InvoiceDetail));
  elements.paymentForm.addEventListener("submit", reviewPaymentSubmission);
  elements.paymentProofInput.addEventListener("change", validateSelectedProof);
  elements.editPaymentButton.addEventListener("click", editPaymentSubmission);
  elements.submitPaymentButton.addEventListener("click", () => void submitPaymentClaim());
  elements.printSuccessButton.addEventListener("click", () => printPortalView(elements.paymentView));
  elements.viewClaimsButton.addEventListener("click", () => void navigatePortal(PortalView.Claims));
  elements.refreshClaimsButton.addEventListener("click", () => void loadClaims(true));
  elements.printClaimButton.addEventListener("click", () => printPortalView(elements.claimDetailView));
}

async function submitLogin(event) {
  event.preventDefault();
  const credentials = {
    clientId: elements.clientIdInput.value.trim(),
    email: elements.emailInput.value.trim(),
    password: elements.passwordInput.value
  };

  if (credentials.clientId === "" || credentials.email === "" || credentials.password === "") {
    setStatus("Client ID, email, and password are required.", "error");
    return;
  }

  authState.pendingCredentials = credentials;
  authState.installationId = elements.installationIdInput.value.trim();
  setFormBusy(elements.loginView, true);
  setStatus("Signing in", "");

  try {
    const session = await createSession(credentials);
    await completeAuthentication(session);
  } catch (error) {
    if (isMfaError(error, "ClientPortalMfaRequired")) {
      elements.passwordInput.value = "";
      setAuthView(AuthView.Mfa);
      setMfaMethod("totp");
      setStatus("Enter your verification code.", "");
    } else {
      authState.pendingCredentials = null;
      setStatus(formatError(error), "error");
    }
  } finally {
    setFormBusy(elements.loginView, false);
  }
}

async function submitMfa(event) {
  event.preventDefault();
  if (authState.pendingCredentials === null) {
    returnToLogin();
    setStatus("Sign in again to continue.", "error");
    return;
  }

  const challenge = authState.mfaMethod === "recovery"
    ? elements.recoveryCodeInput.value.trim()
    : elements.totpCodeInput.value.trim();

  if (challenge === "") {
    setStatus("Enter a verification code.", "error");
    return;
  }

  if (authState.mfaMethod === "totp" && !/^\d{6}$/.test(challenge)) {
    setStatus("Authenticator code must contain 6 digits.", "error");
    return;
  }

  const mfa = authState.mfaMethod === "recovery"
    ? { recoveryCode: challenge }
    : { totpCode: challenge };
  setFormBusy(elements.mfaView, true);
  setStatus("Verifying", "");

  try {
    const session = await createSession(authState.pendingCredentials, mfa);
    await completeAuthentication(session);
  } catch (error) {
    elements.totpCodeInput.value = "";
    elements.recoveryCodeInput.value = "";
    if (isMfaError(error, "ClientPortalMfaInvalid")) {
      setStatus("That verification code is not valid. Try again.", "error");
    } else if (isMfaError(error, "ClientPortalMfaUnavailable")) {
      setStatus("Verification is temporarily unavailable. Try again shortly.", "error");
    } else {
      setStatus(formatError(error), "error");
    }
  } finally {
    setFormBusy(elements.mfaView, false);
  }
}

async function acceptInvitation(event) {
  event.preventDefault();
  const invitationToken = authState.invitationToken || elements.inviteTokenInput.value.trim();
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

  setFormBusy(elements.invitationView, true);
  setStatus("Accepting invitation", "");

  try {
    const accepted = await requestJson("/api/v1/client-portal/invitations/accept", {
      method: "POST",
      body: { invitationToken, password, fullName }
    });
    authState.invitationToken = "";
    elements.inviteTokenInput.value = "";
    elements.invitePasswordInput.value = "";
    elements.clientIdInput.value = accepted.clientId ?? "";
    elements.emailInput.value = accepted.email ?? "";

    if (accepted.accessToken) {
      await completeAuthentication(accepted);
    } else {
      setAuthView(AuthView.Login);
      setStatus("Invitation accepted. Sign in to continue.", "ready");
    }
  } catch (error) {
    setStatus(formatError(error), "error");
  } finally {
    setFormBusy(elements.invitationView, false);
  }
}

async function requestPasswordReset(event) {
  event.preventDefault();
  const clientId = elements.resetClientIdInput.value.trim();
  const email = elements.resetEmailInput.value.trim();

  if (clientId === "" || email === "") {
    setStatus("Client ID and email are required.", "error");
    return;
  }

  setFormBusy(elements.forgotPasswordView, true);
  setStatus("Requesting reset instructions", "");

  try {
    await requestJson("/api/v1/client-portal/password-reset-requests", {
      method: "POST",
      body: { clientId, email }
    });
    elements.clientIdInput.value = clientId;
    elements.emailInput.value = email;
    setAuthView(AuthView.Login);
    setStatus("If the account exists, reset instructions have been sent.", "ready");
  } catch (error) {
    setStatus(formatError(error), "error");
  } finally {
    setFormBusy(elements.forgotPasswordView, false);
  }
}

async function resetPassword(event) {
  event.preventDefault();
  const newPassword = elements.newPasswordInput.value;
  const confirmation = elements.confirmPasswordInput.value;

  if (authState.resetToken === "") {
    setStatus("This password reset link is invalid or has already been used.", "error");
    return;
  }

  if (newPassword.length < 8) {
    setStatus("New password must be at least 8 characters.", "error");
    return;
  }

  if (newPassword !== confirmation) {
    setStatus("The passwords do not match.", "error");
    return;
  }

  setFormBusy(elements.resetPasswordView, true);
  setStatus("Resetting password", "");

  try {
    await requestJson("/api/v1/client-portal/password-resets", {
      method: "POST",
      body: { resetToken: authState.resetToken, newPassword }
    });
    authState.resetToken = "";
    elements.newPasswordInput.value = "";
    elements.confirmPasswordInput.value = "";
    setAuthView(AuthView.Login);
    setStatus("Password reset. Sign in with your new password.", "ready");
  } catch (error) {
    setStatus(formatError(error), "error");
  } finally {
    setFormBusy(elements.resetPasswordView, false);
  }
}

async function completeAuthentication(session) {
  if (!session || typeof session.accessToken !== "string" || session.accessToken === "") {
    throw new ApiError(500, "ClientPortalSessionInvalid", "The server returned an invalid session.");
  }

  setSession(session);
  authState.pendingCredentials = null;
  elements.passwordInput.value = "";
  elements.totpCodeInput.value = "";
  elements.recoveryCodeInput.value = "";
  setStatus("Loading portal", "");
  await loadPortalState();
  await navigatePortal(PortalView.Overview);
}

async function createSession(credentials, mfa = {}) {
  return requestJson("/api/v1/client-portal/sessions", {
    method: "POST",
    body: { ...credentials, ...mfa }
  });
}

async function loadPortalState() {
  if (authState.session === null) {
    return;
  }

  const clientId = authState.session.clientId;
  const installationId = authState.installationId;
  setStatus("Loading portal", "");

  try {
    const [commercialSummary, invoicePage, installationStatus] = await Promise.all([
      getJson(`/api/v1/client-portal/clients/${encodeURIComponent(clientId)}/commercial-summary`),
      getJson(commercialDocumentsPath(clientId)),
      installationId === ""
        ? Promise.resolve(null)
        : getJson(`/api/v1/client-portal/clients/${encodeURIComponent(clientId)}`
          + `/installations/${encodeURIComponent(installationId)}/status`)
    ]);

    renderCommercialSummary(commercialSummary);
    invoicePageState = {
      clientId,
      currencyCode: commercialSummary.currencyCode,
      items: invoicePage.items ?? [],
      nextCursor: invoicePage.nextCursor ?? null
    };
    renderInvoices(invoicePageState.items, invoicePageState.currencyCode, invoicePage.hasMore === true);
    renderInstallationStatus(installationStatus);
    setStatus("Loaded", "ready");
  } catch (error) {
    setStatus(formatError(error), "error");
  }
}

async function navigatePortal(view) {
  if (authState.session === null) {
    return;
  }

  const views = {
    [PortalView.Overview]: elements.overviewView,
    [PortalView.Billing]: elements.billingView,
    [PortalView.InvoiceDetail]: elements.invoiceDetailView,
    [PortalView.Payment]: elements.paymentView,
    [PortalView.Claims]: elements.claimsView,
    [PortalView.ClaimDetail]: elements.claimDetailView
  };
  if (!views[view]) {
    view = PortalView.Overview;
  }

  portalView = view;
  Object.entries(views).forEach(([name, element]) => {
    element.hidden = name !== view;
  });

  const activeNavView = view === PortalView.Claims || view === PortalView.ClaimDetail
    ? PortalView.Claims
    : view === PortalView.Billing || view === PortalView.InvoiceDetail || view === PortalView.Payment
      ? PortalView.Billing
      : PortalView.Overview;
  elements.topNavButtons.forEach((button) => {
    const active = button.dataset.portalView === activeNavView;
    button.classList.toggle("active", active);
    if (active) {
      button.setAttribute("aria-current", "page");
    } else {
      button.removeAttribute("aria-current");
    }
  });
  clearBillingNotice();

  if (view === PortalView.Billing) {
    await loadBilling(false);
  } else if (view === PortalView.Claims) {
    await loadClaims(false);
  }

  window.scrollTo({ top: 0, behavior: "smooth" });
}

async function loadBilling(forceReload) {
  if (billingState.billingLoaded && !forceReload) {
    renderBillingSummary();
    renderBillingInvoices();
    return;
  }

  elements.refreshBillingButton.disabled = true;
  elements.billingSummaryState.textContent = "Loading";
  clearBillingNotice();

  try {
    const [summary, invoiceResponse] = await Promise.all([
      getJson(`${PORTAL_API_BASE}/billing-summary`),
      getJson(`${PORTAL_API_BASE}/invoices`)
    ]);
    billingState.summary = summary ?? {};
    billingState.invoices = responseItems(invoiceResponse, "invoices");
    billingState.billingLoaded = true;
    renderBillingSummary();
    renderBillingInvoices();
    setStatus("Billing loaded", "ready");

    try {
      const claimResponse = await getJson(`${PORTAL_API_BASE}/payment-claims`);
      billingState.claims = responseItems(claimResponse, "claims");
      billingState.claimsLoaded = true;
      renderInvoiceDetailIfSelected();
      renderClaimHistory();
    } catch (error) {
      billingState.claimsLoaded = false;
      showBillingWarning(userFacingLoadError(error, "payment claim history"));
    }
  } catch (error) {
    elements.billingSummaryState.textContent = "Unavailable";
    showBillingError(userFacingLoadError(error, "billing information"));
  } finally {
    elements.refreshBillingButton.disabled = false;
  }
}

async function loadClaims(forceReload) {
  if (billingState.claimsLoaded && !forceReload) {
    renderClaimHistory();
    return;
  }

  elements.refreshClaimsButton.disabled = true;
  clearBillingNotice();
  try {
    const response = await getJson(`${PORTAL_API_BASE}/payment-claims`);
    billingState.claims = responseItems(response, "claims");
    billingState.claimsLoaded = true;
    renderClaimHistory();
    setStatus("Payment claims loaded", "ready");
  } catch (error) {
    showBillingError(userFacingLoadError(error, "payment claims"));
  } finally {
    elements.refreshClaimsButton.disabled = false;
  }
}

async function openInvoiceDetail(invoiceId) {
  if (!invoiceId) {
    return;
  }

  clearBillingNotice();
  setStatus("Loading invoice", "");
  try {
    const invoice = await getJson(`${PORTAL_API_BASE}/invoices/${encodeURIComponent(invoiceId)}`);
    billingState.selectedInvoice = invoice;
    renderInvoiceDetail(invoice);
    await navigatePortal(PortalView.InvoiceDetail);
    setStatus("Invoice loaded", "ready");

    if (!billingState.claimsLoaded) {
      try {
        const claimResponse = await getJson(`${PORTAL_API_BASE}/payment-claims`);
        billingState.claims = responseItems(claimResponse, "claims");
        billingState.claimsLoaded = true;
        renderInvoiceDetail(invoice);
      } catch (error) {
        showBillingWarning(userFacingLoadError(error, "payment claim history"));
      }
    }
  } catch (error) {
    showBillingError(userFacingLoadError(error, "this invoice"));
  }
}

function renderBillingSummary() {
  const summary = billingState.summary ?? {};
  const currencyCode = summary.currencyCode ?? billingCurrencyCode();
  elements.billingSummaryState.textContent = numberValue(summary.totalOutstanding) > 0
    ? "Payment due"
    : "Up to date";
  elements.billingTotalOutstanding.textContent = formatMoney(
    numberValue(summary.totalOutstanding),
    currencyCode
  );
  elements.billingUnpaidCount.textContent = Math.max(0, integerValue(summary.unpaidInvoiceCount)).toString();
  elements.billingLastPayment.textContent = summary.lastPaymentDate
    ? formatDate(summary.lastPaymentDate)
    : "No payments yet";
}

function renderBillingInvoices() {
  const filter = elements.invoiceStatusFilter.value;
  const invoices = [...billingState.invoices]
    .sort(compareInvoicesByDueDate)
    .filter((invoice) => filter === "all" || invoiceDisplayStatus(invoice) === filter);

  elements.billingInvoiceCount.textContent = invoices.length.toString();
  elements.billingInvoiceRows.replaceChildren();
  elements.billingInvoicesEmpty.hidden = invoices.length !== 0;
  elements.billingInvoiceRows.closest(".table-shell").hidden = invoices.length === 0;

  invoices.forEach((invoice) => {
    const row = document.createElement("tr");
    if (isInvoiceOverdue(invoice)) {
      row.classList.add("overdue-row");
    }

    const invoiceNumber = invoice.invoiceNumber ?? "Invoice";
    const currencyCode = invoice.currencyCode ?? billingCurrencyCode();
    row.append(
      cell(invoiceNumber),
      cell(formatDate(invoice.issueDate)),
      cell(formatDate(invoice.dueDate)),
      cell(formatMoney(numberValue(invoice.totalAmount), currencyCode)),
      cell(formatMoney(numberValue(invoice.amountPaid), currencyCode)),
      cell(formatMoney(numberValue(invoice.balanceRemaining), currencyCode)),
      statusCell(invoiceDisplayStatus(invoice))
    );
    const actionCell = document.createElement("td");
    const action = document.createElement("button");
    action.type = "button";
    action.className = "row-action";
    action.textContent = "View";
    action.setAttribute("aria-label", `View invoice ${invoiceNumber}`);
    action.addEventListener("click", () => void openInvoiceDetail(invoice.invoiceId));
    actionCell.append(action);
    row.append(actionCell);
    elements.billingInvoiceRows.append(row);
  });
}

function renderInvoiceDetail(invoice) {
  const currencyCode = invoice.currencyCode ?? billingCurrencyCode();
  const payments = Array.isArray(invoice.payments) ? invoice.payments : [];
  const lines = Array.isArray(invoice.lines) ? invoice.lines : [];
  const claims = claimsForInvoice(invoice.invoiceId);
  const total = numberValue(invoice.totalAmount);
  const paymentsApplied = invoice.amountPaid === undefined
    ? payments.reduce((sum, payment) => sum + numberValue(payment.amount), 0)
    : numberValue(invoice.amountPaid);
  const remaining = invoice.balanceRemaining === undefined
    ? Math.max(0, total - paymentsApplied)
    : Math.max(0, numberValue(invoice.balanceRemaining));
  const availableForClaim = claimAvailableBalance({ ...invoice, balanceRemaining: remaining });
  const status = invoiceDisplayStatus({ ...invoice, amountPaid: paymentsApplied, balanceRemaining: remaining });
  const client = invoice.client ?? {};

  elements.detailInvoiceNumber.textContent = invoice.invoiceNumber ?? "-";
  setStatusBadge(elements.detailInvoiceStatus, status);
  elements.detailIssueDate.textContent = formatDate(invoice.issueDate);
  elements.detailDueDate.textContent = formatDate(invoice.dueDate);
  elements.detailClientName.textContent = client.name ?? "-";
  elements.detailClientContact.textContent = [client.contactName, client.email, client.phone]
    .filter((value) => typeof value === "string" && value.trim() !== "")
    .join(" · ") || "-";

  elements.invoiceLineRows.replaceChildren();
  if (lines.length === 0) {
    elements.invoiceLineRows.append(emptyTableRow(4, "No line items are available for this invoice."));
  } else {
    lines.forEach((line) => {
      const row = document.createElement("tr");
      row.append(
        cell(line.description ?? "-"),
        cell(formatQuantity(line.quantity)),
        cell(formatMoney(numberValue(line.unitPrice), line.currencyCode ?? currencyCode)),
        cell(formatMoney(numberValue(line.lineTotal), line.currencyCode ?? currencyCode))
      );
      elements.invoiceLineRows.append(row);
    });
  }
  elements.detailInvoiceTotal.textContent = formatMoney(total, currencyCode);
  elements.detailPaymentsApplied.textContent = formatMoney(paymentsApplied, currencyCode);
  elements.detailBalanceRemaining.textContent = formatMoney(remaining, currencyCode);
  renderInvoicePayments(payments, currencyCode);
  renderInvoiceClaims(claims);
  elements.invoicePayButton.hidden = !canSubmitPayments() || availableForClaim <= 0;
  elements.invoicePayButton.disabled = availableForClaim <= 0;
}

function renderInvoicePayments(payments, currencyCode) {
  elements.detailPaymentCount.textContent = payments.length.toString();
  elements.invoicePaymentRows.replaceChildren();
  elements.invoicePaymentsEmpty.hidden = payments.length !== 0;
  elements.invoicePaymentRows.closest(".table-shell").hidden = payments.length === 0;
  payments.forEach((payment) => {
    const row = document.createElement("tr");
    row.append(
      cell(formatDate(payment.receivedOn)),
      cell(payment.reference ?? "-"),
      cell(humanizeStatus(payment.method ?? "Bank transfer")),
      cell(formatMoney(numberValue(payment.amount), payment.currencyCode ?? currencyCode))
    );
    elements.invoicePaymentRows.append(row);
  });
}

function renderInvoiceClaims(claims) {
  elements.detailClaimCount.textContent = claims.length.toString();
  elements.invoiceClaimList.replaceChildren();
  elements.invoiceClaimsEmpty.hidden = claims.length !== 0;
  claims.forEach((claim) => elements.invoiceClaimList.append(claimListItem(claim, false)));
}

function renderClaimHistory() {
  const claims = [...billingState.claims].sort((left, right) =>
    dateTimeValue(right.submittedAtUtc) - dateTimeValue(left.submittedAtUtc));
  elements.claimHistoryCount.textContent = claims.length.toString();
  elements.claimHistoryList.replaceChildren();
  elements.claimsEmpty.hidden = claims.length !== 0;
  claims.forEach((claim) => elements.claimHistoryList.append(claimListItem(claim, true)));
}

function claimListItem(claim, showInvoice) {
  const item = document.createElement("div");
  item.className = "claim-list-item";

  const identity = document.createElement("div");
  const reference = document.createElement("strong");
  reference.textContent = showInvoice
    ? claim.invoiceNumber ?? "Invoice"
    : `Claim ${shortReference(claim.claimId)}`;
  const submitted = document.createElement("small");
  submitted.textContent = `Submitted ${formatDateTime(claim.submittedAtUtc)}`;
  identity.append(reference, submitted);

  const amount = document.createElement("strong");
  amount.textContent = formatMoney(numberValue(claim.amount), claim.currencyCode ?? billingCurrencyCode());
  const status = statusBadge(claim.status ?? "pending_verification");

  const action = document.createElement("button");
  action.type = "button";
  action.className = "row-action";
  action.textContent = "View";
  action.setAttribute("aria-label", `View payment claim ${shortReference(claim.claimId)}`);
  action.addEventListener("click", () => void openClaimDetail(claim.claimId));
  item.append(identity, amount, status, action);

  if (canonicalStatus(claim.status) === "rejected" && claim.rejectionReason) {
    const reason = document.createElement("p");
    reason.className = "claim-rejection-summary";
    reason.textContent = `Rejected: ${claim.rejectionReason}`;
    item.append(reason);
  }
  return item;
}

async function openClaimDetail(claimId) {
  if (!claimId) {
    return;
  }
  clearBillingNotice();
  setStatus("Loading payment claim", "");
  try {
    const claim = await getJson(`${PORTAL_API_BASE}/payment-claims/${encodeURIComponent(claimId)}`);
    billingState.selectedClaim = claim;
    renderClaimDetail(claim);
    await navigatePortal(PortalView.ClaimDetail);
    setStatus("Payment claim loaded", "ready");
  } catch (error) {
    showBillingError(userFacingLoadError(error, "this payment claim"));
  }
}

function renderClaimDetail(claim) {
  const status = claim.status ?? "pending_verification";
  elements.claimDetailReference.textContent = fullReference(claim.claimId);
  setStatusBadge(elements.claimDetailStatus, status);
  elements.claimDetailNotice.textContent = claimStatusMessage(status);
  elements.claimDetailInvoice.textContent = claim.invoiceNumber ?? shortReference(claim.invoiceId);
  elements.claimDetailAmount.textContent = formatMoney(
    numberValue(claim.amount),
    claim.currencyCode ?? billingCurrencyCode()
  );
  elements.claimDetailTransferReference.textContent = claim.transferReferenceNumber ?? "-";
  elements.claimDetailSubmitted.textContent = formatDateTime(claim.submittedAtUtc);
  elements.claimDetailReviewed.textContent = formatDateTime(claim.reviewedAtUtc);
  elements.claimDetailProof.textContent = claim.proofAttachment?.fileName
    ?? (claim.proofAttachmentId ? "Proof attached" : "No attachment");
  const rejected = canonicalStatus(status) === "rejected";
  elements.claimRejectionPanel.hidden = !rejected;
  elements.claimRejectionReason.textContent = rejected
    ? claim.rejectionReason || "No reason was provided."
    : "";
}

async function startPaymentSubmission() {
  const invoice = billingState.selectedInvoice;
  if (!invoice || !canSubmitPayments()) {
    showBillingError("Your portal role cannot submit payment claims.");
    return;
  }
  const availableForClaim = claimAvailableBalance(invoice);
  if (availableForClaim <= 0) {
    showBillingError("This invoice has no balance available after pending payment claims.");
    return;
  }

  billingState.paymentDraft = null;
  elements.paymentEntryStep.hidden = false;
  elements.paymentConfirmStep.hidden = true;
  elements.paymentSuccessStep.hidden = true;
  elements.paymentInvoiceNumber.textContent = invoice.invoiceNumber ?? "-";
  elements.paymentAmountInput.value = availableForClaim.toFixed(2);
  elements.paymentAmountInput.max = availableForClaim.toFixed(2);
  elements.paymentBalanceHint.textContent = `Available after pending claims: ${formatMoney(availableForClaim, invoice.currencyCode)}`;
  elements.paymentReferenceInput.value = "";
  elements.paymentProofInput.value = "";
  elements.paymentProofInput.setCustomValidity("");
  await navigatePortal(PortalView.Payment);
  await loadBankDetails();
}

async function loadBankDetails() {
  elements.bankDetailsState.textContent = "Loading";
  setFormBusy(elements.paymentForm, true);
  try {
    const bankDetails = await getJson(`${PORTAL_API_BASE}/config/bank-details`);
    billingState.bankDetails = bankDetails;
    renderBankDetails(bankDetails);
    if (bankDetails?.isConfigured !== true) {
      showBillingError("Bank transfer details are not configured yet. Contact the provider before submitting payment.");
      setFormBusy(elements.paymentForm, true);
      return;
    }
    setFormBusy(elements.paymentForm, false);
  } catch (error) {
    elements.bankDetailsState.textContent = "Unavailable";
    showBillingError(userFacingLoadError(error, "bank transfer details"));
    setFormBusy(elements.paymentForm, true);
  }
}

function renderBankDetails(bankDetails) {
  const configured = bankDetails?.isConfigured === true;
  elements.bankDetailsState.textContent = configured ? "Ready" : "Not configured";
  elements.bankName.textContent = bankDetails?.bankName || "-";
  elements.bankAccountTitle.textContent = bankDetails?.accountTitle || "-";
  elements.bankAccountNumber.textContent = bankDetails?.accountNumber || "-";
  elements.bankIban.textContent = bankDetails?.iban || "-";
  elements.bankBranchInfo.textContent = bankDetails?.branchOrRoutingInfo || "-";
}

function validateSelectedProof() {
  const file = elements.paymentProofInput.files?.[0] ?? null;
  const error = validateProofFile(file);
  elements.paymentProofInput.setCustomValidity(error ?? "");
  if (error) {
    elements.paymentProofInput.reportValidity();
    showBillingError(error);
  } else {
    clearBillingNotice();
  }
}

function reviewPaymentSubmission(event) {
  event.preventDefault();
  const invoice = billingState.selectedInvoice;
  if (!invoice || !canSubmitPayments()) {
    showBillingError("Your portal role cannot submit payment claims.");
    return;
  }

  const amount = Number.parseFloat(elements.paymentAmountInput.value);
  const availableForClaim = claimAvailableBalance(invoice);
  const transferReferenceNumber = elements.paymentReferenceInput.value.trim();
  const proof = elements.paymentProofInput.files?.[0] ?? null;
  const proofError = validateProofFile(proof);
  if (!Number.isFinite(amount) || amount <= 0) {
    showBillingError("Enter a payment amount greater than zero.");
    elements.paymentAmountInput.focus();
    return;
  }
  if (amount > availableForClaim) {
    showBillingError("The payment amount cannot exceed the balance available after pending claims.");
    elements.paymentAmountInput.focus();
    return;
  }
  if (transferReferenceNumber === "") {
    showBillingError("Enter the transfer reference number shown by your bank.");
    elements.paymentReferenceInput.focus();
    return;
  }
  if (transferReferenceNumber.length > MAX_TRANSFER_REFERENCE_LENGTH) {
    showBillingError(`Transfer reference number cannot exceed ${MAX_TRANSFER_REFERENCE_LENGTH} characters.`);
    elements.paymentReferenceInput.focus();
    return;
  }
  if (proofError) {
    showBillingError(proofError);
    elements.paymentProofInput.focus();
    return;
  }

  billingState.paymentDraft = { amount, transferReferenceNumber, proof };
  elements.confirmInvoiceNumber.textContent = invoice.invoiceNumber ?? "-";
  elements.confirmPaymentAmount.textContent = formatMoney(amount, invoice.currencyCode);
  elements.confirmTransferReference.textContent = transferReferenceNumber;
  elements.confirmProofName.textContent = proof?.name ?? "No attachment";
  elements.paymentEntryStep.hidden = true;
  elements.paymentConfirmStep.hidden = false;
  clearBillingNotice();
}

function editPaymentSubmission() {
  elements.paymentConfirmStep.hidden = true;
  elements.paymentEntryStep.hidden = false;
  clearBillingNotice();
  elements.paymentAmountInput.focus();
}

async function submitPaymentClaim() {
  const invoice = billingState.selectedInvoice;
  const draft = billingState.paymentDraft;
  if (!invoice || !draft || !canSubmitPayments()) {
    showBillingError("The payment details are incomplete. Review the payment again.");
    return;
  }

  elements.submitPaymentButton.disabled = true;
  elements.editPaymentButton.disabled = true;
  setStatus("Submitting payment claim", "");
  clearBillingNotice();
  try {
    let proofAttachmentId = null;
    if (draft.proof !== null) {
      const attachment = await uploadProofAttachment(draft.proof);
      proofAttachmentId = attachment?.attachmentId ?? null;
      if (!proofAttachmentId) {
        throw new ApiError(500, "PortalAttachmentInvalid", "The proof upload did not complete. Please try again.");
      }
    }

    const claim = await requestJson(`${PORTAL_API_BASE}/payment-claims`, {
      method: "POST",
      auth: true,
      body: {
        invoiceId: invoice.invoiceId,
        amount: draft.amount,
        transferReferenceNumber: draft.transferReferenceNumber,
        proofAttachmentId
      }
    });
    billingState.claims = [claim, ...billingState.claims.filter((item) => item.claimId !== claim.claimId)];
    billingState.claimsLoaded = true;
    billingState.billingLoaded = false;
    renderPaymentSuccess(claim);
    renderClaimHistory();
    elements.paymentConfirmStep.hidden = true;
    elements.paymentSuccessStep.hidden = false;
    setStatus("Payment submitted for verification", "ready");
  } catch (error) {
    showBillingError(paymentSubmissionError(error));
  } finally {
    elements.submitPaymentButton.disabled = false;
    elements.editPaymentButton.disabled = false;
  }
}

async function uploadProofAttachment(file) {
  const formData = new FormData();
  formData.append("file", file, file.name);
  return requestMultipart(`${PORTAL_API_BASE}/attachments`, formData, { auth: true });
}

function renderPaymentSuccess(claim) {
  const draft = billingState.paymentDraft;
  elements.successClaimReference.textContent = fullReference(claim.claimId);
  elements.successSubmittedAt.textContent = formatDateTime(claim.submittedAtUtc);
  elements.successAmount.textContent = formatMoney(
    numberValue(claim.amount ?? draft?.amount),
    claim.currencyCode ?? billingState.selectedInvoice?.currencyCode
  );
  elements.successTransferReference.textContent = claim.transferReferenceNumber
    ?? draft?.transferReferenceNumber
    ?? "-";
}

function printPortalView(view) {
  view.classList.add("print-target");
  const cleanup = () => view.classList.remove("print-target");
  window.addEventListener("afterprint", cleanup, { once: true });
  window.print();
  window.setTimeout(cleanup, 1000);
}

function claimsForInvoice(invoiceId) {
  return billingState.claims
    .filter((claim) => String(claim.invoiceId).toLowerCase() === String(invoiceId).toLowerCase())
    .sort((left, right) => dateTimeValue(right.submittedAtUtc) - dateTimeValue(left.submittedAtUtc));
}

function claimAvailableBalance(invoice) {
  if (!billingState.claimsLoaded) {
    return 0;
  }
  const pendingAmount = claimsForInvoice(invoice.invoiceId)
    .filter((claim) => canonicalStatus(claim.status) === "pending-verification")
    .reduce((total, claim) => total + numberValue(claim.amount), 0);
  return Math.max(0, numberValue(invoice.balanceRemaining) - pendingAmount);
}

function renderInvoiceDetailIfSelected() {
  if (billingState.selectedInvoice) {
    renderInvoiceDetail(billingState.selectedInvoice);
  }
}

function compareInvoicesByDueDate(left, right) {
  const overdueDifference = Number(isInvoiceOverdue(right)) - Number(isInvoiceOverdue(left));
  if (overdueDifference !== 0) {
    return overdueDifference;
  }
  return dateValue(left.dueDate) - dateValue(right.dueDate);
}

function isInvoiceOverdue(invoice) {
  if (numberValue(invoice.balanceRemaining) <= 0) {
    return false;
  }
  if (canonicalStatus(invoice.status) === "overdue") {
    return true;
  }
  const dueDate = dateValue(invoice.dueDate);
  return Number.isFinite(dueDate) && dueDate < startOfToday();
}

function invoiceDisplayStatus(invoice) {
  const sourceStatus = canonicalStatus(invoice.status);
  if (numberValue(invoice.balanceRemaining) <= 0 || canonicalStatus(invoice.status) === "paid") {
    return "paid";
  }
  if (isInvoiceOverdue(invoice)) {
    return "overdue";
  }
  if (numberValue(invoice.amountPaid) > 0
      || sourceStatus === "partial"
      || sourceStatus === "partially-paid"
      || sourceStatus === "partiallypaid") {
    return "partial";
  }
  return "pending";
}

function canSubmitPayments() {
  const role = String(authState.session?.role ?? "").toLowerCase();
  return role === "clientowner" || role === "clientbilling";
}

function validateProofFile(file) {
  if (file === null) {
    return null;
  }
  if (!ALLOWED_PROOF_TYPES.has(file.type.toLowerCase())) {
    return "Proof must be a JPEG, PNG, or PDF file.";
  }
  if (file.size > MAX_PROOF_SIZE_BYTES) {
    return "Proof must be 5 MB or smaller.";
  }
  return null;
}

function showBillingError(message) {
  elements.billingNotice.textContent = message;
  elements.billingNotice.hidden = false;
  setStatus("Action needed", "error");
}

function showBillingWarning(message) {
  elements.billingNotice.textContent = `${message} Invoice information remains available.`;
  elements.billingNotice.hidden = false;
  setStatus("Billing partially loaded", "ready");
}

function clearBillingNotice() {
  elements.billingNotice.textContent = "";
  elements.billingNotice.hidden = true;
}

async function logout(allSessions) {
  const button = allSessions ? elements.logoutAllButton : elements.logoutButton;
  button.disabled = true;
  setStatus(allSessions ? "Logging out everywhere" : "Logging out", "");

  try {
    await requestJson(
      allSessions ? "/api/v1/client-portal/sessions/all" : "/api/v1/client-portal/sessions/current",
      { method: "DELETE", auth: true }
    );
  } catch (error) {
    if (!(error instanceof ApiError && error.status === 401)) {
      setStatus(formatError(error), "error");
    }
  } finally {
    clearSession();
    button.disabled = false;
    setStatus(allSessions ? "Logged out on all devices" : "Logged out", "ready");
  }
}

async function startTotpEnrollment() {
  const password = elements.enrollmentPasswordInput.value;
  if (password === "") {
    setStatus("Enter your current password before changing MFA.", "error");
    return;
  }
  elements.startEnrollmentButton.disabled = true;
  setStatus("Preparing authenticator setup", "");

  try {
    const enrollment = await requestJson("/api/v1/client-portal/mfa/totp/enrollment", {
      method: "POST",
      body: { password },
      auth: true
    });
    elements.enrollmentPasswordInput.value = "";
    authState.enrollment = {
      ...enrollment,
      recoveryCodes: Array.isArray(enrollment.recoveryCodes) ? enrollment.recoveryCodes : []
    };
    renderEnrollment(enrollment);
    elements.enrollmentStart.hidden = true;
    elements.enrollmentPanel.hidden = false;
    elements.recoveryCodesPanel.hidden = true;
    setStatus("Scan the code and verify setup.", "");
    elements.totpEnrollmentCodeInput.focus();
  } catch (error) {
    setStatus(formatError(error), "error");
  } finally {
    elements.startEnrollmentButton.disabled = false;
  }
}

async function confirmTotpEnrollment(event) {
  event.preventDefault();
  const code = elements.totpEnrollmentCodeInput.value.trim();

  if (!/^\d{6}$/.test(code)) {
    setStatus("Verification code must contain 6 digits.", "error");
    return;
  }

  setFormBusy(elements.totpConfirmForm, true);
  setStatus("Confirming authenticator", "");

  try {
    const replacementSession = await requestJson("/api/v1/client-portal/mfa/totp/confirm", {
      method: "POST",
      body: { code },
      auth: true
    });
    setSession(replacementSession);
    elements.totpEnrollmentCodeInput.value = "";
    elements.enrollmentPanel.hidden = true;
    elements.recoveryCodesPanel.hidden = false;
    elements.mfaState.textContent = "Enabled";
    renderRecoveryCodes(authState.enrollment?.recoveryCodes ?? []);
    setStatus("Authenticator enabled. Save your recovery codes.", "ready");
  } catch (error) {
    elements.totpEnrollmentCodeInput.value = "";
    setStatus(formatError(error), "error");
  } finally {
    setFormBusy(elements.totpConfirmForm, false);
  }
}

function cancelTotpEnrollment() {
  clearEnrollmentState();
  elements.enrollmentStart.hidden = false;
  setStatus("Authenticator setup cancelled.", "");
}

async function copyRecoveryCodes() {
  const codes = authState.enrollment?.recoveryCodes ?? [];
  if (codes.length === 0) {
    setStatus("No recovery codes are available to copy.", "error");
    return;
  }

  try {
    if (!navigator.clipboard || typeof navigator.clipboard.writeText !== "function") {
      throw new Error("Clipboard access is unavailable in this browser.");
    }
    await navigator.clipboard.writeText(codes.join("\n"));
    setStatus("Recovery codes copied.", "ready");
  } catch (error) {
    setStatus(formatError(error), "error");
  }
}

function finishRecoveryCodes() {
  clearEnrollmentState();
  elements.enrollmentStart.hidden = true;
  elements.mfaState.textContent = "Enabled";
  setStatus("Authenticator setup complete.", "ready");
}

function renderEnrollment(enrollment) {
  elements.totpSecret.textContent = enrollment.secret ?? "";
  const qrCode = typeof enrollment.qrCodeDataUri === "string" ? enrollment.qrCodeDataUri : "";
  const safeQrCode = /^data:image\/(?:png|jpeg|webp|svg\+xml)(?:;charset=[^;,]+)?;base64,/i.test(qrCode);
  elements.totpQrCode.hidden = !safeQrCode;
  if (safeQrCode) {
    elements.totpQrCode.src = qrCode;
  } else {
    elements.totpQrCode.removeAttribute("src");
  }

  const otpAuthUri = typeof enrollment.otpAuthUri === "string" ? enrollment.otpAuthUri : "";
  const safeOtpAuthUri = otpAuthUri.toLowerCase().startsWith("otpauth://");
  elements.otpAuthUriLink.hidden = !safeOtpAuthUri;
  elements.otpAuthUriLink.href = safeOtpAuthUri ? otpAuthUri : "#";
}

function renderRecoveryCodes(codes) {
  if (codes.length === 0) {
    const item = document.createElement("li");
    item.textContent = "No recovery codes returned";
    elements.recoveryCodeList.replaceChildren(item);
    elements.copyRecoveryCodesButton.disabled = true;
    return;
  }

  elements.copyRecoveryCodesButton.disabled = false;
  elements.recoveryCodeList.replaceChildren(...codes.map((code) => {
    const item = document.createElement("li");
    item.textContent = code;
    return item;
  }));
}

function clearEnrollmentState() {
  authState.enrollment = null;
  elements.enrollmentPanel.hidden = true;
  elements.recoveryCodesPanel.hidden = true;
  elements.totpEnrollmentCodeInput.value = "";
  elements.totpSecret.textContent = "";
  elements.totpQrCode.removeAttribute("src");
  elements.totpQrCode.hidden = true;
  elements.otpAuthUriLink.href = "#";
  elements.otpAuthUriLink.hidden = true;
  elements.recoveryCodeList.replaceChildren();
}

function showForgotPassword() {
  elements.resetClientIdInput.value = elements.clientIdInput.value.trim();
  elements.resetEmailInput.value = elements.emailInput.value.trim();
  setAuthView(AuthView.ForgotPassword);
}

function returnToLogin() {
  authState.pendingCredentials = null;
  authState.invitationToken = "";
  elements.inviteTokenInput.value = "";
  elements.invitePasswordInput.value = "";
  elements.totpCodeInput.value = "";
  elements.recoveryCodeInput.value = "";
  setAuthView(AuthView.Login);
  setStatus("Ready", "");
}

function setMfaMethod(method) {
  authState.mfaMethod = method;
  const useRecoveryCode = method === "recovery";
  if (useRecoveryCode) {
    elements.totpCodeInput.value = "";
  } else {
    elements.recoveryCodeInput.value = "";
  }
  elements.totpChallengeField.hidden = useRecoveryCode;
  elements.recoveryChallengeField.hidden = !useRecoveryCode;
  elements.useTotpButton.className = useRecoveryCode ? "link-button" : "secondary-button";
  elements.useRecoveryButton.className = useRecoveryCode ? "secondary-button" : "link-button";
  (useRecoveryCode ? elements.recoveryCodeInput : elements.totpCodeInput).focus();
}

function setAuthView(view) {
  authState.view = view;
  renderAuthState();
}

function renderAuthState() {
  const authenticated = authState.session !== null;
  elements.authZone.hidden = authenticated;
  elements.portalContent.hidden = !authenticated;
  elements.sessionSummary.hidden = !authenticated;
  elements.loginView.hidden = authenticated || authState.view !== AuthView.Login;
  elements.invitationView.hidden = authenticated || authState.view !== AuthView.Invitation;
  elements.mfaView.hidden = authenticated || authState.view !== AuthView.Mfa;
  elements.forgotPasswordView.hidden = authenticated || authState.view !== AuthView.ForgotPassword;
  elements.resetPasswordView.hidden = authenticated || authState.view !== AuthView.ResetPassword;

  if (authenticated) {
    const role = authState.session.role ? ` · ${authState.session.role}` : "";
    elements.sessionIdentity.textContent = `${authState.session.clientId}${role}`;
  } else {
    elements.sessionIdentity.textContent = "";
  }
}

function setSession(session) {
  const previous = authState.session ?? {};
  authState.session = {
    ...previous,
    ...session,
    refreshToken: session.refreshToken ?? previous.refreshToken ?? ""
  };
  authState.installationId = elements.installationIdInput.value.trim() || authState.installationId;
  persistSession();
  renderAuthState();
}

function clearSession() {
  authState.session = null;
  authState.pendingCredentials = null;
  clearStoredSession();
  resetPortalState();
  authState.view = AuthView.Login;
  renderAuthState();
}

function persistSession() {
  try {
    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({
      session: authState.session,
      installationId: authState.installationId
    }));
  } catch {
    // A usable in-memory session is preferable to failing sign-in when storage is unavailable.
  }
}

function restoreSession() {
  try {
    const stored = JSON.parse(sessionStorage.getItem(SESSION_STORAGE_KEY) ?? "null");
    const session = stored?.session ?? null;
    if (!session?.accessToken || !session?.clientId || isIdleExpired(session)) {
      clearStoredSession();
      return;
    }

    authState.session = session;
    authState.installationId = stored.installationId ?? elements.installationIdInput.value.trim();
    elements.clientIdInput.value = session.clientId;
    elements.installationIdInput.value = authState.installationId;
  } catch {
    clearStoredSession();
  }
}

function isIdleExpired(session) {
  if (!session.idleExpiresAtUtc) {
    return false;
  }
  const expiresAt = Date.parse(session.idleExpiresAtUtc);
  return Number.isFinite(expiresAt) && expiresAt <= Date.now();
}

function clearStoredSession() {
  try {
    sessionStorage.removeItem(SESSION_STORAGE_KEY);
  } catch {
    // Nothing else is required when browser storage is disabled.
  }
}

async function requestJson(path, options = {}) {
  const method = options.method ?? "GET";
  const auth = options.auth === true;
  const retryRefresh = options.retryRefresh !== false;
  const headers = { Accept: "application/json" };

  if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (auth) {
    if (authState.session === null) {
      throw new ApiError(401, "ClientPortalSessionRequired", "Sign in to continue.");
    }
    headers.Authorization = `Bearer ${authState.session.accessToken}`;
  }

  const response = await fetch(path, {
    method,
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body)
  });

  if (response.status === 401 && auth && retryRefresh && authState.session?.refreshToken) {
    try {
      await refreshSession();
      return requestJson(path, { ...options, retryRefresh: false });
    } catch {
      clearSession();
      throw new ApiError(401, "ClientPortalSessionExpired", "Your session expired. Sign in again.");
    }
  }

  if (!response.ok) {
    const error = await readApiError(response);
    if (response.status === 401 && auth) {
      clearSession();
    }
    throw error;
  }

  if (response.status === 204) {
    return null;
  }

  const text = await response.text();
  return text === "" ? null : JSON.parse(text);
}

async function requestMultipart(path, formData, options = {}) {
  const auth = options.auth === true;
  const retryRefresh = options.retryRefresh !== false;
  const headers = { Accept: "application/json" };
  if (auth) {
    if (authState.session === null) {
      throw new ApiError(401, "ClientPortalSessionRequired", "Sign in to continue.");
    }
    headers.Authorization = `Bearer ${authState.session.accessToken}`;
  }

  const response = await fetch(path, {
    method: "POST",
    headers,
    body: formData
  });
  if (response.status === 401 && auth && retryRefresh && authState.session?.refreshToken) {
    try {
      await refreshSession();
      return requestMultipart(path, formData, { ...options, retryRefresh: false });
    } catch {
      clearSession();
      throw new ApiError(401, "ClientPortalSessionExpired", "Your session expired. Sign in again.");
    }
  }
  if (!response.ok) {
    const error = await readApiError(response);
    if (response.status === 401 && auth) {
      clearSession();
    }
    throw error;
  }
  if (response.status === 204) {
    return null;
  }
  const text = await response.text();
  return text === "" ? null : JSON.parse(text);
}

async function refreshSession() {
  if (refreshPromise !== null) {
    return refreshPromise;
  }

  const refreshToken = authState.session?.refreshToken;
  if (!refreshToken) {
    throw new ApiError(401, "ClientPortalRefreshRequired", "Sign in again to continue.");
  }

  refreshPromise = (async () => {
    const refreshed = await requestJson("/api/v1/client-portal/sessions/refresh", {
      method: "POST",
      body: { refreshToken }
    });
    setSession(refreshed);
    return refreshed;
  })();

  try {
    return await refreshPromise;
  } finally {
    refreshPromise = null;
  }
}

function getJson(path) {
  return requestJson(path, { auth: true });
}

async function readApiError(response) {
  let body = null;
  try {
    body = await response.json();
  } catch {
    // Non-JSON failures still retain their HTTP status below.
  }

  const code = body?.code ?? body?.errorCode ?? body?.title ?? `Http${response.status}`;
  const message = body?.detail ?? body?.message ?? body?.title ?? response.statusText
    ?? "The request failed.";
  return new ApiError(response.status, code, message);
}

function isMfaError(error, code) {
  return error instanceof ApiError && error.code === code;
}

function stripSensitiveUrlTokens() {
  const url = new URL(window.location.href);
  const fragmentHasToken = url.hash.startsWith("#invite=") || url.hash.startsWith("#reset=");
  if (!url.searchParams.has("invite") && !url.searchParams.has("reset") && !fragmentHasToken) {
    return;
  }
  url.searchParams.delete("invite");
  url.searchParams.delete("reset");
  url.hash = "";
  window.history.replaceState({}, document.title, `${url.pathname}${url.search}`);
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
  renderModules(entitlement?.modules ?? []);
}

async function loadMoreInvoices() {
  if (invoicePageState.nextCursor === null || authState.session === null) {
    return;
  }

  elements.invoiceMoreButton.disabled = true;
  try {
    const page = await getJson(commercialDocumentsPath(invoicePageState.clientId, invoicePageState.nextCursor));
    const merged = new Map([...invoicePageState.items, ...(page.items ?? [])]
      .map((invoice) => [invoice.documentId, invoice]));
    invoicePageState.items = [...merged.values()];
    invoicePageState.nextCursor = page.nextCursor ?? null;
    renderInvoices(invoicePageState.items, invoicePageState.currencyCode, page.hasMore === true);
  } catch (error) {
    setStatus(formatError(error), "error");
  } finally {
    elements.invoiceMoreButton.disabled = false;
  }
}

function commercialDocumentsPath(clientId, cursor = null) {
  const query = new URLSearchParams({ documentType: "Invoice", take: "8" });
  if (cursor !== null) {
    query.set("cursor", cursor);
  }
  return `/api/v1/client-portal/clients/${encodeURIComponent(clientId)}/commercial-documents?${query}`;
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
  elements.commandState.textContent = `${commandStatus?.pendingCommandCount ?? 0} pending`
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

  elements.pairingState.textContent = pairingStatus.firstManagerDeviceApproved ? "Ready" : "Manager needed";
  elements.pairingMode.textContent = pairingStatus.pairingMode ?? "Not reported";
  elements.firstManagerState.textContent = pairingStatus.firstManagerDeviceApproved ? "Approved" : "Needed";
  elements.pairingDevices.textContent = formatPairingDevices(pairingStatus);
  elements.pairingLastUpdate.textContent = formatDateTime(pairingStatus.lastDeviceUpdatedAtUtc);
}

function renderInvoices(invoices, currencyCode, hasMore) {
  elements.invoiceCount.textContent = hasMore ? `${invoices.length}+` : invoices.length.toString();
  elements.invoiceMoreButton.hidden = !hasMore;

  if (invoices.length === 0) {
    elements.invoiceRows.replaceChildren(emptyTableRow(4, "No invoices found"));
    return;
  }

  elements.invoiceRows.replaceChildren(...invoices.map((invoice) => {
    const row = document.createElement("tr");
    row.append(
      cell(invoice.reference),
      cell(invoice.status),
      cell(formatDate(invoice.documentDate)),
      cell(formatMoney(invoice.balanceAmount, invoice.currencyCode ?? currencyCode))
    );
    return row;
  }));
}

function renderModules(modules) {
  elements.moduleCount.textContent = modules.length.toString();
  if (modules.length === 0) {
    elements.moduleList.replaceChildren(modulePill("No modules loaded", ""));
    return;
  }
  elements.moduleList.replaceChildren(...modules.map((module) =>
    modulePill(module.moduleCode, module.isEnabled ? "enabled" : "disabled")));
}

function resetPortalState() {
  invoicePageState = emptyInvoicePageState();
  billingState = emptyBillingState();
  portalView = PortalView.Overview;
  elements.accountState.textContent = "Not loaded";
  elements.balanceDue.textContent = "-";
  elements.availableCredit.textContent = "-";
  elements.commercialUpdated.textContent = "-";
  elements.licenseState.textContent = "Not loaded";
  elements.paidUntil.textContent = "-";
  elements.graceUntil.textContent = "-";
  elements.offlineValidUntil.textContent = "-";
  elements.installationState.textContent = "Not loaded";
  elements.lastHeartbeat.textContent = "-";
  elements.localServerVersion.textContent = "-";
  elements.commandState.textContent = "-";
  renderPairingStatus(null);
  elements.invoiceCount.textContent = "0";
  elements.invoiceRows.replaceChildren(emptyTableRow(4, "No invoices loaded"));
  elements.invoiceMoreButton.hidden = true;
  elements.moduleCount.textContent = "0";
  elements.moduleList.replaceChildren(modulePill("No modules loaded", ""));
  elements.mfaState.textContent = "Authenticator";
  elements.enrollmentStart.hidden = false;
  elements.billingTotalOutstanding.textContent = "-";
  elements.billingUnpaidCount.textContent = "-";
  elements.billingLastPayment.textContent = "-";
  elements.billingInvoiceRows.replaceChildren();
  elements.billingInvoiceCount.textContent = "0";
  elements.claimHistoryList.replaceChildren();
  elements.claimHistoryCount.textContent = "0";
  elements.paymentForm.reset();
  elements.overviewView.hidden = false;
  elements.billingView.hidden = true;
  elements.invoiceDetailView.hidden = true;
  elements.paymentView.hidden = true;
  elements.claimsView.hidden = true;
  elements.claimDetailView.hidden = true;
  elements.topNavButtons.forEach((button) => {
    const active = button.dataset.portalView === PortalView.Overview;
    button.classList.toggle("active", active);
    if (active) {
      button.setAttribute("aria-current", "page");
    } else {
      button.removeAttribute("aria-current");
    }
  });
  clearBillingNotice();
  clearEnrollmentState();
}

function emptyInvoicePageState() {
  return { clientId: "", currencyCode: "PKR", items: [], nextCursor: null };
}

function emptyBillingState() {
  return {
    summary: null,
    invoices: [],
    claims: [],
    selectedInvoice: null,
    selectedClaim: null,
    paymentDraft: null,
    bankDetails: null,
    billingLoaded: false,
    claimsLoaded: false
  };
}

function responseItems(response, propertyName) {
  if (Array.isArray(response)) {
    return response;
  }
  return Array.isArray(response?.[propertyName]) ? response[propertyName] : [];
}

function cell(value) {
  const item = document.createElement("td");
  item.textContent = value ?? "-";
  return item;
}

function statusCell(status) {
  const item = document.createElement("td");
  item.append(statusBadge(status));
  return item;
}

function statusBadge(status) {
  const badge = document.createElement("span");
  badge.className = "status-badge";
  setStatusBadge(badge, status);
  return badge;
}

function setStatusBadge(badge, status) {
  const canonical = canonicalStatus(status);
  badge.className = `status-badge ${canonical}`;
  badge.textContent = humanizeStatus(canonical);
}

function emptyTableRow(columnCount, message) {
  const row = document.createElement("tr");
  const item = document.createElement("td");
  item.colSpan = columnCount;
  item.textContent = message;
  row.append(item);
  return row;
}

function modulePill(text, className) {
  const item = document.createElement("span");
  item.textContent = text;
  item.className = className;
  return item;
}

function numberValue(value) {
  const parsed = typeof value === "number" ? value : Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function integerValue(value) {
  const parsed = typeof value === "number" ? value : Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? Math.trunc(parsed) : 0;
}

function formatQuantity(value) {
  const quantity = numberValue(value);
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 4 }).format(quantity);
}

function canonicalStatus(value) {
  const status = String(value ?? "pending").trim().toLowerCase()
    .replace(/[_\s]+/g, "-");
  if (status === "pendingverification") {
    return "pending-verification";
  }
  return status || "pending";
}

function humanizeStatus(value) {
  const canonical = canonicalStatus(value);
  const labels = {
    "pending": "Pending",
    "pending-verification": "Pending verification",
    "partial": "Partial",
    "overdue": "Overdue",
    "paid": "Paid",
    "verified": "Verified",
    "approved": "Verified",
    "rejected": "Rejected"
  };
  return labels[canonical] ?? canonical.split("-")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function billingCurrencyCode() {
  return billingState.summary?.currencyCode
    ?? billingState.selectedInvoice?.currencyCode
    ?? invoicePageState.currencyCode
    ?? "PKR";
}

function shortReference(value) {
  const reference = String(value ?? "").trim();
  if (reference === "") {
    return "-";
  }
  return reference.length > 12 ? reference.slice(0, 12).toUpperCase() : reference.toUpperCase();
}

function fullReference(value) {
  const reference = String(value ?? "").trim();
  return reference === "" ? "-" : reference.toUpperCase();
}

function claimStatusMessage(status) {
  const canonical = canonicalStatus(status);
  if (canonical === "verified" || canonical === "approved") {
    return "This transfer was verified and the payment was applied to the invoice.";
  }
  if (canonical === "rejected") {
    return "This transfer was not verified. Review the provider's reason below before submitting again.";
  }
  return "This claim is pending provider verification. The invoice is not marked paid yet.";
}

function dateValue(value) {
  if (value === null || value === undefined || value === "") {
    return Number.POSITIVE_INFINITY;
  }
  const text = String(value);
  const parsed = Date.parse(text.includes("T") ? text : `${text}T00:00:00`);
  return Number.isFinite(parsed) ? parsed : Number.POSITIVE_INFINITY;
}

function dateTimeValue(value) {
  const parsed = Date.parse(value ?? "");
  return Number.isFinite(parsed) ? parsed : 0;
}

function startOfToday() {
  const today = new Date();
  return new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime();
}

function userFacingLoadError(error, subject) {
  if (error instanceof ApiError && error.status === 404) {
    return `We could not find ${subject}. Refresh the page or contact your provider.`;
  }
  if (error instanceof ApiError && error.status === 403) {
    return `Your portal account does not have access to ${subject}.`;
  }
  if (error instanceof TypeError) {
    return `We could not connect to load ${subject}. Check your connection and try again.`;
  }
  return `We could not load ${subject}. ${formatError(error)}`;
}

function paymentSubmissionError(error) {
  if (error instanceof ApiError && error.status === 409) {
    return "That transfer reference has already been submitted. Check your payment claims before trying again.";
  }
  if (error instanceof ApiError && error.status === 422) {
    return error.message || "The payment could not be submitted because the invoice balance changed. Refresh and try again.";
  }
  if (error instanceof ApiError && error.status === 413) {
    return "The proof file is too large. Choose a file that is 5 MB or smaller.";
  }
  if (error instanceof TypeError) {
    return "We could not submit the payment because of a network problem. Check your connection and try again.";
  }
  return `We could not submit the payment claim. ${formatError(error)}`;
}

function formatPairingDevices(pairingStatus) {
  const approvedCount = pairingStatus.approvedDeviceCount ?? 0;
  const totalCount = pairingStatus.totalDeviceCount ?? 0;
  const pendingCount = pairingStatus.pendingDeviceCount ?? 0;
  const revokedCount = pairingStatus.revokedDeviceCount ?? 0;
  const details = [];
  if (pendingCount > 0) details.push(`${pendingCount} pending`);
  if (revokedCount > 0) details.push(`${revokedCount} revoked`);
  return details.length === 0
    ? `${approvedCount}/${totalCount} approved`
    : `${approvedCount}/${totalCount} approved, ${details.join(", ")}`;
}

function setFormBusy(form, isBusy) {
  form.querySelectorAll("button, input").forEach((control) => {
    control.disabled = isBusy;
  });
}

function setStatus(message, className) {
  elements.connectionStatus.textContent = message;
  elements.connectionStatus.className = `portal-status ${className}`.trim();
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
  if (value === null || value === undefined || value === "") return "-";
  const text = String(value);
  const parsed = new Date(text.includes("T") ? text : `${text}T00:00:00`);
  return Number.isFinite(parsed.getTime())
    ? new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(parsed)
    : "-";
}

function formatDateTime(value) {
  if (value === null || value === undefined || value === "") return "-";
  const parsed = new Date(value);
  return Number.isFinite(parsed.getTime())
    ? new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(parsed)
    : "-";
}
