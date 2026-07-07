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

The legacy `X-SafarSuite-Provider-Key` header still works as a bootstrap fallback, but new tooling should prefer bearer sessions. Control Desk can send a bearer token through `ControlCloud:Status:ProviderAccessToken` and `ControlCloud:PortalInvitations:ProviderAccessToken`; when those are blank it falls back to the configured provider shared secret.

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
      "SessionSigningSecret": "change-before-production",
      "SessionMinutes": 60,
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

- `SessionSigningSecret` is required for bearer sessions.
- `SessionMinutes` is clamped between 5 and 1440 minutes.
- `OperatorStorePath` is used only by file persistence.
- `Users` is a seed list, not a long-term management surface. It is written only when the operator store is missing or empty.
- With PostgreSQL, operators are stored in `cloud.provider_access_operators`.
- With file persistence, operators are stored in the configured JSON file.

Apply the PostgreSQL schema:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src\SafarSuite.ControlCloud.Infrastructure\SafarSuite.ControlCloud.Infrastructure.csproj --startup-project src\SafarSuite.ControlCloud.Infrastructure\SafarSuite.ControlCloud.Infrastructure.csproj --context ControlCloudDbContext
```

The provider-operator migration is `20260707181435_AddProviderAccessOperators`.

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

## Mint Operator Sessions

Use operator credentials for normal sessions:

```powershell
$session = Invoke-RestMethod -Method Post -Uri "$base/api/v1/provider-access/operator-sessions" -ContentType "application/json" -Body (@{
  email = "ops.one@safarsuite.local"
  password = "ChangeThisProviderPassword123!"
  scopes = @("app-activation:read", "app-activation:write")
  expiresInMinutes = 15
} | ConvertTo-Json -Depth 5)
$headers = @{ Authorization = "Bearer $($session.accessToken)" }
```

If `scopes` is omitted, Control Cloud tries to issue all scopes assigned to the operator. Prefer explicit scopes for task-specific tooling.

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
Provider access smoke passed 8 checks:
```

The smoke proves file-backed seed persistence, scoped operator session issuance, over-scoped login rejection, unsupported-scope rejection, file-store save/reload, file-store validation, EF table mapping, and EF store validation before database access.

For live app-runtime activation proof, `tools\SafarSuite.LocalServer.ComposeBootstrapProof activate-app-runtime` defaults to `POST /api/v1/provider-access/operator-sessions` and then sends the returned bearer token to Control Cloud.

## Troubleshooting

| Code | Meaning | Operator action |
| --- | --- | --- |
| `ProviderAccessNotConfigured` | Missing shared secret or session signing secret. | Check `ClientPortal:ProviderAccess:SharedSecret` and `SessionSigningSecret`. |
| `ProviderAccessDenied` | Shared secret or bearer token is invalid. | Mint a fresh session and confirm the configured secret/token source. |
| `ProviderCredentialsInvalid` | Operator email, password, hash, or status did not authenticate. | Confirm the email, reset the password, and ensure status is `Active`. |
| `ProviderAccessScopeUnsupported` | The requested session scope is not in the supported scope catalog. | Fix the typo or add a new scope deliberately in code. |
| `ProviderAccessScopeDenied` | The session/operator lacks the required scope. | Mint a session with the needed assigned scope or update the operator scopes. |
| `ProviderAccessExpired` | Bearer session expired. | Mint a new session. |
| `ProviderOperatorScopesUnsupported` | A stored operator has invalid assigned scopes. | Repair the operator scopes in the store or database. |
| `ProviderOperatorAlreadyExists` | Create request reused an email already in the store. | Use the existing operator or choose a different email. |
| `ProviderOperatorNotFound` | Password/scope/status update targeted an unknown `userId`. | List operators and retry with the returned `userId`. |

## Production Guardrails

- Keep provider operators separate from client portal identities.
- Use named operators for routine work; keep shared-secret sessions for bootstrap or emergency recovery only.
- Assign the smallest scope set that covers the task.
- Rotate `SharedSecret` and `SessionSigningSecret` before production use.
- Do not keep development seed credentials in production config.
- Back up `cloud.provider_access_operators` with the rest of Control Cloud state.
- Treat a `SessionSigningSecret` rotation as invalidating all outstanding bearer sessions.
- Record provider operator changes through the existing audit lane before building richer manager UI.
