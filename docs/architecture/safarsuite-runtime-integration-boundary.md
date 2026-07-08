# SafarSuite Runtime Integration Boundary

This note records how SafarSuite Control Cloud, the SafarSuite local server, and the deployed SafarSuite app/runtime should meet when we move from bootstrap scaffolding to a real client installation.

## Workspace Rule

The real deployed SafarSuite app is not in this workspace.

This workspace owns:

- SafarSuite Control Desk commercial/accounting source of truth
- SafarSuite Control Cloud setup, bootstrap, entitlement, heartbeat, command, diagnostics, and audit contracts
- SafarSuite local-server shared licensing, cache, diagnostics, and deployment templates
- deployment/data-sync vocabulary and boundary decisions in `docs/architecture/client-deployment-and-data-sync-boundary.md`

The SafarSuite app workspace must own:

- the real customer-facing app image
- module UI/runtime enforcement
- module-specific local business data and screens
- real service health endpoints
- any app-specific database migrations or workers

## Current Bootstrap Topology

Control Cloud now signs a bootstrap package that includes:

- `docker-compose.yml`
- `local-server.env.template`
- `runtime-services.manifest.json`

The Compose topology contains:

| Service | Starts by default | Role |
| --- | --- | --- |
| `local-db` | Yes | Local PostgreSQL store for local-server/runtime state |
| `local-api` | Yes | Local entitlement, diagnostics, and module-gateway API |
| `local-worker` | Yes | Background entitlement pull and heartbeat reporting |
| `local-agent` | Yes | Support command polling, diagnostics, and acknowledgement bridge |
| `safarsuite-app` | No, `app-runtime` profile | Customer-facing SafarSuite app runtime |

The `safarsuite-app` service is intentionally present but disabled by default. It becomes active only when the real app image exists and deployment can run:

```bash
docker compose --profile app-runtime --env-file local-server.env up -d
```

As of 2026-07-05, `tools/SafarSuite.LocalServer.ComposeBootstrapProof` can generate an install-shaped Compose directory and a stub Control Cloud. The proof has started the generated LocalServer-only stack, imported the signed bootstrap bundle through the local API, pulled entitlement version 1, posted an active heartbeat, and confirmed module-gateway access through the running container network. This proves the LocalServer runtime loop before the real Control Cloud seeded install and optional app profile proof.

The same tool now also supports `generate-real-cloud`. That mode seeds a disposable real Control Cloud through the signed Control Desk `EntitlementSnapshotIssued` envelope receiver, asks the real bootstrap-package endpoint for the installer artifacts, writes the returned Compose/env/runtime manifest/signed bundle files, and preserves local image/port overrides for repeatable proof runs. The first real-cloud proof passed with file-persistence Control Cloud, LocalServer split API/worker/agent containers, signed bootstrap import/registration, entitlement issue and pull, heartbeat, command polling, module-gateway access, and cloud status readback.

The optional `app-runtime` profile has also passed a container boot proof with proof-shaped production readiness values. On 2026-07-07 the generated app-runtime proof started the real app image beside LocalServer over the HTTPS-default Local API lane, verified LocalServer bootstrap/entitlement/heartbeat/module access, and verified app `/health` on the published app port.

The app bridge proof also passed again on 2026-07-07 through the real Control Cloud package flow. `ComposeBootstrapProof activate-app-runtime` read the running app's activation state, requested an activation import payload from Control Cloud, imported that token into the app, and confirmed the app's own `Accounting` module endpoint returned `Active`/allowed. The token is bound to the app's internal server installation id and public-key/fingerprint identity, while the provider LocalServer remains registered as the external installation. The latest proof used client `77777777-7777-7777-7777-777777777777`, provider installation `real-cloud-app-runtime-main`, activation issue `844c451b-00eb-4ad3-8adf-1017af1ef75a`, app server `6aecdcaa-1eec-47fd-9712-f7b8c8cb14e9`, and entitlement version `639190249093957034`.

The first Control Cloud-owned issuer slice is now wired. `GET /api/v1/control-cloud/app-activation/signing-key` exposes the active app verifier key, and `POST /api/v1/control-cloud/clients/{clientId}/installations/{installationId}/app-activation-token` validates the registered installation, reads the latest signed entitlement bundle issue, maps enabled product modules into app module entitlements, signs the app activation token with the Control Cloud app-activation ECDSA key, returns the app import payload, and records `AppActivationTokenIssued` audit. The current Control Desk/Cloud flow also records structured app activation issue metadata, exposes a searchable provider-gated mapping register, supports provider-gated issue revocation with actor/reason/time plus `AppActivationTokenRevoked` audit, links replacement issuance to the revoked issue it replaces while requiring a fresh app activation request/public key, and signs a `revoke_app_activation` installation command that LocalServer verifies, records in a durable app-activation revocation ledger, acknowledges, and exposes through a local revocation-status endpoint for app enforcement. Provider-gated Control Cloud actions now prefer scoped provider bearer sessions from durable provider operators through `POST /api/v1/provider-access/operator-sessions`; `provider-operators:manage` protects list/create/password-reset/scope/status administration under `/api/v1/provider-access/operators`, and the transitional shared-secret session endpoint plus old `X-SafarSuite-Provider-Key` path remain as compatibility bootstrap lanes. App activation list requires `app-activation:read`, issue/revoke requires `app-activation:write`, and Client Portal invitation management requires `client-portal:manage`. Provider operators now have file and PostgreSQL-backed stores, a no-live-cloud smoke proof in `tools/SafarSuite.ControlCloud.ProviderAccessSmoke`, a live PostgreSQL Control Cloud proof in `tools/SafarSuite.ControlCloud.PostgresProof`, a Control Desk manager surface for list/create/scope/status/password-reset work, and an operator handoff in `docs/planning/control-cloud-provider-access-runbook-2026-07-07.md`. The SafarSuite app now consumes the revocation-status endpoint, sends `X-SafarSuite-Local-Api-Key`, validates configured local CA certificates for HTTPS provider LocalServer calls through a shared Local API client factory, and blocks revoked imported tokens. The app backend hosts module-gateway access locally today; there is no separate outbound module-gateway HTTP client to secure. Generated deployment artifacts now default the Local API lane to HTTPS with generated local CA material and carry the active app activation verifier key inside the signed bootstrap package, env artifact, and copyable install command. Remaining production work is real provider-user MFA/password reset UX, production key storage/rotation, and full clean-machine app deployment proof with real deployment secrets.

## App Runtime Contract

The SafarSuite app image should accept these environment variables:

| Variable | Meaning |
| --- | --- |
| `SAFARSUITE_APP_VERSION` | App image/runtime version selected by Control Cloud bootstrap |
| `SAFARSUITE_APP_IMAGE` | Full container image reference |
| `SAFARSUITE_APP_HTTP_PORT` | Host port for app UI/API exposure |
| `SAFARSUITE_LOCAL_API_BASE_URL` | Internal local-server API URL, default `https://local-api:8080` |
| `SAFARSUITE_LOCAL_API_ACCESS_KEY` | Shared local API access key sent as `X-SafarSuite-Local-Api-Key` for protected app-to-provider LocalServer checks |
| `SAFARSUITE_LOCAL_API_CA_CERTIFICATE_PATH` | Optional trusted CA certificate path for HTTPS provider LocalServer checks; when present, the app validates the provider certificate chain and hostname |
| `SAFARSUITE_MODULE_GATEWAY_URL` | Internal module entitlement/gateway URL, default `https://local-api:8080` |
| `SAFARSUITE_RUNTIME_MANIFEST_PATH` | Path to the signed/deployed runtime service manifest |
| `SAFARSUITE_CLIENT_DEPLOYMENT_MODE` | Client runtime mode such as `OfflineLocal`, `BranchToHqSync`, `CloudSyncMultiBranch`, or `HostedSaas` |
| `SAFARSUITE_SITE_ID` | Stable site/runtime identity such as standalone site, HQ, branch, or hosted tenant |
| `SAFARSUITE_SITE_ROLE` | Site role such as `Standalone`, `Hq`, `Branch`, or `Hosted` |

The app image should expose:

- `GET /health`
- module-gated UI/API startup that reads the local entitlement state through `SAFARSUITE_MODULE_GATEWAY_URL`
- a clear restricted mode when the local server denies a module
- later branch/site behavior from an installation/deployment profile, without putting operational business-data sync into the Control Cloud billing/license channel

## Module Gateway Contract

The shared contract lives in:

```text
src/SafarSuite.ControlDesk.Contracts/ControlCloud/V1/LocalServerModuleGatewayContracts.cs
```

Current app-facing request shape:

```text
LocalServerModuleAccessRequest
  installationId
  moduleCode
  asOfDate
  requestedBy
```

Current app-facing response shape:

```text
LocalServerModuleAccessResponse
  formatVersion = safarsuite-local-module-gateway-v1
  installationId
  moduleCode
  isAllowed
  accessState
  reason
  entitlementVersion
  paidUntil
  warningStartsAt
  graceUntil
  offlineValidUntil
  checkedAtUtc
```

The local-server application handler exists at:

```text
src/SafarSuite.LocalServer.Application/ModuleGateway/EvaluateModuleAccess
```

The local API host now exposes this handler through local-server endpoints for the app runtime:

```text
POST /api/v1/local-server/modules/access
GET  /api/v1/local-server/modules/{moduleCode}/access
```

The SafarSuite app workspace should point module-gated screens and APIs at `SAFARSUITE_MODULE_GATEWAY_URL`, call one of these endpoints, and treat `isAllowed = false` as authoritative. The app should show a restricted/renewal/module-disabled state based on `accessState` instead of reimplementing entitlement rules.

As of 2026-07-04, the real app workspace at `C:\Users\Daniyal\Documents\Codex\2026-06-09\hello-there-2` verifies this contract through app local-server GET/POST routes, Windows client menu/window policy, backend `LocalWritePolicy` enforcement, reporting execution/audit enforcement, `tests\ProductKernelGuardSmoke`, `tests\ReadOnlyEnforcementSmoke`, and `tests\ReportExecutionSmoke`. It also has the first runtime wrapper in `Dockerfile.localserver`, `.dockerignore`, `docker-compose.runtime.yml`, and `docker/localserver.env.template`; local publish, Compose config, Docker image build, Compose runtime startup, container `/health`, local migrations, and v1 module-gateway probes now pass.

This workspace also includes a placeholder app-side probe at:

```text
tools/SafarSuite.AppRuntimeProbe
```

It reads `SAFARSUITE_MODULE_GATEWAY_URL`, `SAFARSUITE_INSTALLATION_ID`, and module-code environment variables, calls the same local module-gateway v1 contract, and has a `--self-test` mode for allowed and module-disabled responses. It is a contract probe, not the real SafarSuite app image.

## Local Server Contract

The local server image wrapper in this workspace now provides the Compose command surface:

- `safarsuite-local-api`
- `safarsuite-local-worker`
- `safarsuite-local-agent`

`safarsuite-local-api` keeps background automation disabled, `safarsuite-local-worker` handles entitlement pull and heartbeat reporting, and `safarsuite-local-agent` handles command polling, diagnostics commands, and acknowledgements. All three map installer `SAFARSUITE_*` values into the `LocalServer:*` .NET configuration shape and share `/var/lib/safarsuite/local-server` state.

The local server image should also keep providing:

- `GET /health` on the local API
- a module-gateway endpoint for the real app to ask whether a module is enabled
- an app-activation revocation-status endpoint protected by `X-SafarSuite-Local-Api-Key` for the real app to fail closed on revoked or mismatched activation issues
- bootstrap import/status endpoints for the downloaded signed bootstrap bundle
- manual and background hooks for entitlement pull and heartbeat reporting
- diagnostics that include runtime services, bootstrap checksums, import audit, and recent errors

Current local API endpoints in this workspace:

```text
GET  /health
GET  /api/v1/local-server/bootstrap
POST /api/v1/local-server/bootstrap-package/import
POST /api/v1/local-server/entitlement/pull
POST /api/v1/local-server/heartbeat
POST /api/v1/local-server/app-activations/revocation-status
POST /api/v1/local-server/modules/access
GET  /api/v1/local-server/modules/{moduleCode}/access
```

Current local proof tools in this workspace:

```text
tools/SafarSuite.LocalServer.EntitlementSmoke
tools/SafarSuite.LocalServer.ComposeBootstrapProof
```

`EntitlementSmoke` guards the generated contract artifacts. `ComposeBootstrapProof` exercises the generated LocalServer runtime package against a stub cloud for fast checks, and its `run-compose` mode now generates the HTTPS-default proof directory, starts the embedded stub cloud, runs Docker Compose, waits on the CA-pinned Local API health endpoint, imports the signed bootstrap bundle, pulls entitlement, posts heartbeat, processes commands, checks bootstrap/cache status, and verifies module-gateway access. The LocalServer-only `run-compose` proof passed on 2026-07-07 with generated local CA HTTPS, active registration/heartbeat, cached entitlement, and allowed `Accounting` access. The app-runtime `run-compose` proof also boots the real app image and now emits explicit app health proof output. Its `verify-running-runtime` mode runs the same LocalServer checks against an already-running generated stack. The `generate-real-cloud` mode exercises the same runtime path against the real Control Cloud signed envelope, bootstrap, registration, entitlement, heartbeat, command, and status endpoints for file-backed/runtime-artifact checks. `SafarSuite.ControlCloud.PostgresProof` now covers the live PostgreSQL variant by applying EF migrations, starting Control Cloud with `Persistence:Provider=Postgres`, driving the provider operator, envelope, bootstrap, registration, entitlement, heartbeat, command, acknowledgement, and status endpoints, and verifying the EF rows afterward. Its `activate-app-runtime` mode now mints a scoped provider bearer session from configured provider-operator credentials by default before requesting Control Cloud app activation-token import and proving the app-side module gate after the generated runtime is running. Production secret provisioning, app activation key storage/rotation, and first-manager/setup-token UX remain separate deployment requirements.

## Handoff Checklist

Detailed app-workspace implementation handoff lives at `docs/planning/safarsuite-app-integration-handoff.md`.

Before we enable `safarsuite-app` by default:

- keep the SafarSuite app integration handoff current
- keep `tools/SafarSuite.AppRuntimeProbe --self-test` passing as the Control Desk contract probe
- keep the app workspace `ProductKernelGuardSmoke`, `ReadOnlyEnforcementSmoke`, and `ReportExecutionSmoke` passing
- keep the app workspace LocalServer publish, Compose config validation, and internal healthcheck passing
- confirm the real app image name and registry
- confirm app container port and health route
- add protected provider/admin authorization for app activation issuance
- move app activation signing private keys out of local proof config into production key storage and rotation
- keep the SafarSuite app revocation-status client and local API access-key contract passing in app workspace smokes
- confirm whether the app owns its own database or uses local-server APIs only
- consume the explicit installation/deployment profile before implementing branch/HQ/cloud data-sync behavior
- add runtime diagnostics for the real app container state
