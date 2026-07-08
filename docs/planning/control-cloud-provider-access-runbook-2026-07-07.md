# Control Cloud Provider Access Runbook

Date added: 2026-07-07

Purpose: give operators and implementers a single handoff for enabling, seeding, validating, and troubleshooting Control Cloud provider access.

## Boundary

Provider access is the cloud administration lane for support and setup actions that should not run as a client portal user.

Preferred flow:

```text
provider operator credentials
  -> POST /api/v1/provider-access/operator-sessions
  -> scoped bearer session
  -> provider-gated Control Cloud action
```

Compatibility flow:

```text
provider shared secret
  -> POST /api/v1/provider-access/sessions
  -> scoped bearer session
```

The legacy `X-SafarSuite-Provider-Key` header still works as a bootstrap fallback, but new tooling should prefer bearer sessions. Control Desk can send a configured bearer token through `ControlCloud:Status:ProviderAccessToken` and `ControlCloud:PortalInvitations:ProviderAccessToken`; when those are blank it falls back to the configured provider shared secret. The Control Desk UI can also mint a named provider-operator bearer session and forward it to Control Desk proxy routes through the `X-SafarSuite-Provider-Access-Token` override header; that session takes precedence over configured fallback credentials for the current browser request.

## Control Desk Manager Surface

Control Desk now exposes provider operator management in the Client Desk Cloud workspace. The surface uses the Control Desk proxy routes under `/api/v1/control-cloud/provider-access/operators`, so managers do not need to call the Control Cloud admin endpoints directly for routine work.

From the provider access panel, a manager can:

- refresh the provider operator register
- create a named operator with a temporary password and explicit scopes
- update an operator's assigned scopes
- suspend or reactivate an operator
- reset an operator's temporary password
- reset TOTP enrollment
- reset one-time recovery codes for recovery-code MFA
- change the signed-in operator's own password

The panel can sign in a named provider operator, store the short-lived bearer session in the browser until expiry, and use that session for provider-gated Control Desk proxy calls. The Control Desk server can still use a configured provider bearer token or shared-secret fallback for bootstrap and automation. Keep routine changes tied to named operators and use the shared secret only for bootstrap or emergency recovery.

## Supported Scopes

| Scope | Use |
| --- | --- |
| `app-activation:read` | Read app activation issue/register state. |
| `app-activation:write` | Issue, revoke, or replace app activation mappings. |
| `client-portal:manage` | Create, list, resend, or revoke client portal invitations. |
| `provider-operators:manage` | List, create, reset password, update scopes, and suspend/reactivate provider operators. |
| `*` | Break-glass all-scope value. Do not use for routine operators. |

## Configure

Development uses PostgreSQL by default:

```json
{
  "Persistence": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "ControlCloud": "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password"
  }
}
```

Provider access settings live under `ClientPortal:ProviderAccess`:

```json
{
  "ClientPortal": {
    "ProviderAccess": {
      "SharedSecret": "change-before-production",
      "SharedSecretFile": "",
      "SessionSigningSecret": "change-before-production",
      "SessionSigningSecretFile": "",
      "TotpProtectionSecret": "change-before-production",
      "TotpProtectionSecretFile": "",
      "ActiveSessionSigningKeyId": "",
      "SessionSigningKeys": [],
      "SessionMinutes": 60,
      "FailedLoginLockoutThreshold": 5,
      "FailedLoginLockoutMinutes": 15,
      "DefaultScopes": [
        "app-activation:read",
        "app-activation:write",
        "client-portal:manage",
        "provider-operators:manage"
      ],
      "OperatorStorePath": "App_Data/provider-access-operators.json",
      "Users": []
    }
  }
}
```

Notes:

- Prefer `SharedSecretFile`, `SessionSigningSecretFile`, `TotpProtectionSecretFile`, and per-key `SecretFile` for production so app configuration points at mounted secret files instead of carrying secret text.
- `SessionSigningSecret` is the legacy single-secret signing setting and is still supported for bearer sessions.
- `SessionSigningKeys` is the preferred rotation shape. When populated, Control Cloud signs new sessions with `ActiveSessionSigningKeyId` and validates bearer sessions against every configured key in the array. Keep the previous key in the array for one maximum session lifetime, then remove it.
- `TotpProtectionSecret` protects stored provider-operator TOTP secrets. Keep it stable across deploys; if it is lost or changed, reset each operator's TOTP enrollment.
- `SessionMinutes` is clamped between 5 and 1440 minutes.
- `FailedLoginLockoutThreshold` is clamped between 2 and 20 failed credential/MFA attempts.
- `FailedLoginLockoutMinutes` is clamped between 1 and 1440 minutes.
- `OperatorStorePath` is used only by file persistence.
- `Users` is a seed list, not a long-term management surface. It is written only when the operator store is missing or empty.
- With PostgreSQL, operators are stored in `cloud.provider_access_operators`.
- With file persistence, operators are stored in the configured JSON file.

Apply the PostgreSQL schema:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src\SafarSuite.ControlCloud.Infrastructure\SafarSuite.ControlCloud.Infrastructure.csproj --startup-project src\SafarSuite.ControlCloud.Infrastructure\SafarSuite.ControlCloud.Infrastructure.csproj --context ControlCloudDbContext
```

The provider-operator migrations are `20260707181435_AddProviderAccessOperators`, `20260708123000_AddProviderAccessOperatorRecoveryCodes`, `20260708133000_AddProviderAccessOperatorTotp`, and `20260708144500_AddProviderAccessOperatorLoginAbuseControls`.

## Bootstrap First Operator

Start Control Cloud:

```powershell
dotnet run --project src\SafarSuite.ControlCloud.Api\SafarSuite.ControlCloud.Api.csproj --urls http://localhost:5127
```

Mint a temporary provider admin session from the shared secret:

```powershell
$base = "http://localhost:5127"
$session = Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/sessions" -ContentType "application/json" -Body (@{
  sharedSecret = "local-development-provider-access-secret-change-before-cloud"
  actor = "provider-bootstrap"
  scopes = @("provider-operators:manage")
  expiresInMinutes = 15
} | ConvertTo-Json -Depth 5)
$headers = @{ Authorization = "Bearer $($session.accessToken)" }
```

Create the first named operator:

```powershell
$operator = Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operators" -Headers $headers -ContentType "application/json" -Body (@{
  email = "ops.one@safarsuite.local"
  fullName = "Ops One"
  password = "ChangeThisProviderPassword123!"
  scopes = @("app-activation:read", "app-activation:write", "client-portal:manage")
  createdBy = "provider-bootstrap"
} | ConvertTo-Json -Depth 5)
```

After the first named operator works, rotate the shared secret and avoid using it for routine actions.

## File-Backed Production Secrets

Use file-backed provider-access secrets when deploying Control Cloud with Docker, Kubernetes, systemd credentials, or another host secret manager. The file path is configuration; the file content is the secret.

```json
{
  "ClientPortal": {
    "ProviderAccess": {
      "SharedSecretFile": "/run/secrets/safarsuite-provider-access-shared-secret",
      "TotpProtectionSecretFile": "/run/secrets/safarsuite-provider-totp-protection-secret",
      "ActiveSessionSigningKeyId": "provider-session-2026-07",
      "SessionSigningKeys": [
        {
          "KeyId": "provider-session-2026-07",
          "SecretFile": "/run/secrets/safarsuite-provider-session-2026-07"
        },
        {
          "KeyId": "provider-session-2026-06",
          "SecretFile": "/run/secrets/safarsuite-provider-session-2026-06"
        }
      ]
    }
  }
}
```

If a configured secret file is missing or empty, Control Cloud fails startup instead of accepting a weakened provider access boundary. Relative file paths resolve from the Control Cloud content root.

## Mint Operator Sessions

Use operator credentials for normal sessions:

```powershell
$session = Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operator-sessions" -ContentType "application/json" -Body (@{
  email = "ops.one@safarsuite.local"
  password = "ChangeThisProviderPassword123!"
  totpCode = "123456"
  scopes = @("app-activation:read", "app-activation:write")
  expiresInMinutes = 15
} | ConvertTo-Json -Depth 5)
$headers = @{ Authorization = "Bearer $($session.accessToken)" }
```

If `scopes` is omitted, Control Cloud tries to issue all scopes assigned to the operator. Prefer explicit scopes for task-specific tooling.

If TOTP is enabled for an operator, `totpCode` is required unless an unused `recoveryCode` is supplied as backup. A valid recovery code is consumed on successful session creation. If only recovery-code MFA is enabled and all codes are consumed, a manager with `provider-operators:manage` must reset codes before that operator can mint another session.

The local development seed operator is `provider.admin@safarsuite.local`; the proof tooling uses `provider-dev-password-change-before-cloud`. Treat that account and password as development-only.

## Manage Operators

List operators:

```powershell
Invoke-RestMethod -Method Get -Uri "$base/api/v1/provider-access/operators" -Headers $headers
```

Reset an operator password:

```powershell
Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operators/$($operator.userId)/password" -Headers $headers -ContentType "application/json" -Body (@{
  password = "AnotherStrongProviderPassword123!"
  updatedBy = "ops.one@safarsuite.local"
} | ConvertTo-Json -Depth 5)
```

Reset operator recovery codes:

```powershell
$recoveryCodes = Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operators/$($operator.userId)/recovery-codes" -Headers $headers -ContentType "application/json" -Body (@{
  count = 10
  updatedBy = "ops.one@safarsuite.local"
} | ConvertTo-Json -Depth 5)
$recoveryCodes.recoveryCodes
```

Recovery codes are returned only in this reset response. Control Cloud stores only hashes, consumes a valid code on provider-operator session creation, and blocks password-only login once recovery codes have been configured.

Reset operator TOTP enrollment:

```powershell
$totp = Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operators/$($operator.userId)/totp" -Headers $headers -ContentType "application/json" -Body (@{
  updatedBy = "ops.one@safarsuite.local"
} | ConvertTo-Json -Depth 5)
$totp.secret
$totp.otpAuthUri
```

The TOTP secret and `otpauth://` URI are returned only in this reset response. Control Cloud stores a protected TOTP payload keyed by `TotpProtectionSecret`; any legacy plaintext TOTP material is rewritten to the protected format after a successful TOTP login.

Update scopes:

```powershell
Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operators/$($operator.userId)/scopes" -Headers $headers -ContentType "application/json" -Body (@{
  scopes = @("app-activation:read")
  updatedBy = "ops.one@safarsuite.local"
} | ConvertTo-Json -Depth 5)
```

Suspend or reactivate:

```powershell
Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operators/$($operator.userId)/status" -Headers $headers -ContentType "application/json" -Body (@{
  status = "Suspended"
  updatedBy = "ops.one@safarsuite.local"
} | ConvertTo-Json -Depth 5)
```

Supported statuses are `Active` and `Suspended`. There is intentionally no delete endpoint yet; suspend operators when access should stop.

## Verify

Run the no-live-cloud provider access smoke:

```powershell
dotnet run --project tools\SafarSuite.ControlCloud.ProviderAccessSmoke\SafarSuite.ControlCloud.ProviderAccessSmoke.csproj --no-restore
```

Expected result:

```text
Provider access smoke passed 16 checks:
```

The smoke proves file-backed seed persistence, scoped operator session issuance, recovery-code MFA enforcement/consumption/exhaustion, TOTP MFA enforcement/replay protection/protected custody, legacy plaintext TOTP migration, failed-login counter reset after success, temporary provider login lockout after repeated MFA failures, session-signing key rotation acceptance/removal, file-backed secret loading, over-scoped login rejection, unsupported-scope rejection, file-store save/reload, file-store validation, EF table mapping, and EF store validation before database access.

Run the live Control Desk proxy proof:

```powershell
dotnet run --project tools\SafarSuite.ControlDesk.ProviderAccessProxyProof\SafarSuite.ControlDesk.ProviderAccessProxyProof.csproj --no-restore
```

Expected result:

```text
Control Desk provider-access proxy proof passed 19 checks:
```

The proxy proof applies Control Cloud PostgreSQL migrations, starts Control Cloud and Control Desk on temporary local ports, points Control Desk at the Control Cloud provider access gate, performs list/create/scope/status/password-reset/recovery-code-reset/TOTP-reset operations through `/api/v1/control-cloud/provider-access/operators`, mints provider bearer sessions through `/api/v1/control-cloud/provider-access/operator-sessions`, proves an under-scoped override session is enforced ahead of the configured shared-secret fallback, proves a manager-scoped override session can list operators, changes a provider operator password through `/api/v1/control-cloud/provider-access/operator-password`, signs in with the self-changed password and a TOTP code, and verifies the final provider operator row plus protected TOTP payload in PostgreSQL.

For live app-runtime activation proof, `tools\SafarSuite.LocalServer.ComposeBootstrapProof activate-app-runtime` defaults to `POST /api/v1/provider-access/operator-sessions` and then sends the returned bearer token to Control Cloud.

## Troubleshooting

| Code | Meaning | Operator action |
| --- | --- | --- |
| `ProviderAccessNotConfigured` | Missing shared secret or usable session signing key. | Check `ClientPortal:ProviderAccess:SharedSecretFile`, `SharedSecret`, `SessionSigningSecretFile`, `SessionSigningSecret`, or the `ActiveSessionSigningKeyId`/`SessionSigningKeys` pair. |
| `ProviderAccessDenied` | Shared secret or bearer token is invalid. | Mint a fresh session and confirm the configured secret/token source. |
| `ProviderCredentialsInvalid` | Operator email, password, hash, or status did not authenticate. | Confirm the email, reset the password, and ensure status is `Active`. |
| `ProviderAccessScopeUnsupported` | The requested session scope is not in the supported scope catalog. | Fix the typo or add a new scope deliberately in code. |
| `ProviderAccessScopeDenied` | The session/operator lacks the required scope. | Mint a session with the needed assigned scope or update the operator scopes. |
| `ProviderAccessExpired` | Bearer session expired. | Mint a new session. |
| `ProviderLoginLocked` | The operator exceeded the failed credential/MFA attempt threshold. | Wait until the reported lockout expiry, then retry with valid credentials and MFA. |
| `ProviderMfaRequired` | The operator has MFA enabled and did not provide a TOTP code or recovery code. | Provide the current TOTP code or one unused recovery code. |
| `ProviderMfaInvalid` | The supplied TOTP code or recovery code is invalid or already used. | Retry with a fresh TOTP code, use an unused recovery code, or reset MFA material. |
| `ProviderMfaUnavailable` | Recovery-code MFA is enabled without TOTP and no unused recovery codes remain. | Reset recovery codes through a manager-scoped provider session. |
| `ProviderOperatorScopesUnsupported` | A stored operator has invalid assigned scopes. | Repair the operator scopes in the store or database. |
| `ProviderOperatorAlreadyExists` | Create request reused an email already in the store. | Use the existing operator or choose a different email. |
| `ProviderOperatorNotFound` | Password/scope/status update targeted an unknown `userId`. | List operators and retry with the returned `userId`. |
| `ProviderOperatorPasswordUnchanged` | Self-service password change reused the current password. | Choose a different new password. |
| `ProviderOperatorRecoveryCodeCountInvalid` | Recovery code reset requested an unsupported count. | Use a count between 1 and 20. |

## Production Guardrails

- Keep provider operators separate from client portal identities.
- Use named operators for routine work; keep shared-secret sessions for bootstrap or emergency recovery only.
- Assign the smallest scope set that covers the task.
- Enable TOTP MFA for provider operators that can perform production changes, keep recovery codes as backup, and rotate recovery codes after break-glass use.
- Keep production provider-access secrets in a host secret store or mounted secret files, not inline appsettings.
- Rotate `SharedSecret` before production use.
- Rotate session signing through `SessionSigningKeys`: mount the new key file, add the new key, set `ActiveSessionSigningKeyId` to it, keep the previous key configured until the longest possible provider session expires, then remove the previous key and its mounted secret.
- Do not keep development seed credentials in production config.
- Back up `cloud.provider_access_operators` with the rest of Control Cloud state.
- Treat removal of an old signing key as invalidating any outstanding bearer sessions signed by that key.
- Keep provider operator changes flowing through the existing audit lane as the manager UI grows.
