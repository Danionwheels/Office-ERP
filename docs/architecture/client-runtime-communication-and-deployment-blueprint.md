# Client Runtime Communication And Deployment Blueprint

Date added: 2026-07-05

Use this as the canonical blueprint for how SafarSuite Control Desk, SafarSuite Control Cloud, the SafarSuite local server, and the SafarSuite Windows app communicate during setup, normal LAN operation, renewal, support, and future module expansion.

This note does not replace the lower-level endpoint maps. It is the decision spine that keeps deployment, communication, local rights, module access, and future sync from becoming tangled.

## North Star

SafarSuite client deployment should feel like:

```text
install once
  -> pair once
  -> assign users, roles, devices, modules, and branch profile
  -> operate reliably on LAN
  -> receive signed cloud control updates when available
  -> keep working through the signed offline-valid period
```

The implementation rule is:

```text
Apps talk to gateways.
Gateways talk through signed or versioned contracts.
Contracts stay stable.
Everything behind them can evolve.
```

## Canonical Runtime Shape

```text
SafarSuite Control Desk
  provider office source of truth for client, contract, pricing, billing, modules, devices, branches, users, and entitlement decisions

SafarSuite Control Cloud
  one production control plane for setup, registration, signed entitlements, commands, heartbeat, diagnostics, portal state, and audit

Client LocalServer
  client-site LAN gateway, local runtime authority, local database owner, module gate, device/user/role authority, cloud control client

SafarSuite Windows App
  thin approved client that talks to LocalServer over LAN and never receives database credentials

Future Operational Data Sync
  separate data plane for branch/HQ/cloud business records when the client buys or enables that module
```

## Communication Rules

These rules are mandatory unless a later architecture note explicitly changes them.

| Path | Allowed? | Rule |
| --- | --- | --- |
| Windows App -> LocalServer | Yes | Primary LAN API path for normal client operation. |
| Windows App -> PostgreSQL | No | End devices never receive database credentials. |
| Windows App -> Control Cloud for runtime access | No | Runtime module/license access goes through LocalServer. |
| LocalServer -> local PostgreSQL | Yes | Only through LocalServer application/persistence layers. |
| LocalServer -> Control Cloud | Yes | Outbound HTTPS for registration, entitlement pull, heartbeat, command pull/ack, diagnostics, and renewal fallback. |
| Control Cloud -> client office inbound | No by default | Do not require public SSH, public IPs, inbound firewall openings, or stored root credentials. |
| Control Desk -> Control Cloud | Yes | Signed/durable commercial and control publishing, plus provider provisioning/status proxies. |
| Control Desk -> LocalServer direct | No by default | Provider support should use Control Cloud commands/status unless an explicit local support session is approved and audited. |
| Control Cloud -> Client Portal | Yes | Portal projections, invitations, sessions, setup visibility, renewal files, and client-scoped status. |
| Control Cloud -> operational business records | No for V1 control channel | Operational branch/HQ/cloud sync belongs to a separate future data plane. |

## One Controlled Gate

There are two controlled gates, each with a different job:

```text
Cloud gate:
  SafarSuite Control Cloud
  protects provider/cloud control, setup, entitlements, commands, portal, diagnostics, and audit

LAN gate:
  SafarSuite LocalServer
  protects local business workflows, database access, modules, devices, users, roles, and read/write policy
```

The Windows app must treat the LocalServer as authoritative for local access. The Control Cloud must treat signed Control Desk events and registered installation identity as authoritative for commercial/control state.

## Deployment Decision

Canonical V1 client deployment is:

```text
Control Cloud signed bootstrap package
  -> Docker Compose local runtime
  -> local-db, local-api, local-worker, local-agent
  -> optional safarsuite-app profile
```

The older SafarSuite app workspace notes about a self-contained Linux/systemd install remain useful reference material and may become an advanced/manual install path later. They are not the canonical V1 production path unless we deliberately reinstate them.

V1 must not depend on:

- direct SSH push into client servers
- stored client root passwords
- static public IP addresses
- inbound cloud access to client offices
- manual per-client database credentials on Windows PCs
- repeated re-setup after first successful pairing

## Setup Flow

Target setup flow:

```text
1. Provider creates or updates client, contract, modules, limits, and deployment profile in Control Desk.
2. Control Desk asks Control Cloud to create a setup token or bootstrap package.
3. Control Cloud issues a one-time installation-scoped setup token and signed bootstrap bundle.
4. Technician runs the install command or imports the bundle on the client server.
5. LocalServer starts in bootstrap mode.
6. LocalServer registers outward with Control Cloud using the setup token.
7. Control Cloud consumes the setup token and binds clientId + installationId + deployment profile.
8. LocalServer pulls the signed entitlement bundle.
9. First manager device is approved through signed setup authority.
10. First admin user is created locally.
11. Manager approves devices, creates users, assigns roles, and reviews module/license state.
```

After step 11, normal users should not need repeated technical setup. Future changes should arrive as signed entitlements, signed product-kernel commands, local role/device changes, or app/runtime updates.

## LAN Operation

Normal office operation:

```text
Windows App
  -> discovers or is configured with LocalServer URL
  -> pairs device with LocalServer
  -> receives signed device credential after manager approval
  -> signs in with local user
  -> calls LocalServer APIs

LocalServer
  -> validates device credential
  -> validates user session
  -> checks permission
  -> checks module/resource access
  -> checks subscription/write mode
  -> executes application use case
  -> writes local PostgreSQL transaction
  -> records audit/outbox where needed
```

The preferred client address should be a stable local name such as:

```text
safarsuite-branch01.lan
```

Pilot installs may use a LAN IP address, but production should prefer DHCP reservation plus branch-local DNS. The app should also verify the LocalServer installation identity/fingerprint rather than trusting only the address.

## Device And Manager Rights

Local manager UX controls business-local authority, not vendor authority.

Managers may control:

- pending, approved, revoked, and blocked Windows devices
- local users
- local roles/security levels
- local branch/company setup where permitted
- local sessions and audit views
- subscription/module status visibility
- backup/sync/diagnostics visibility where permitted

Managers must not control:

- installation identity
- activation signing keys
- vendor trust roots
- product-kernel entitlement writes
- module enablement outside signed entitlement/command paths
- license expiry/grace/offline-valid date math
- Control Cloud identity
- provider pricing, contracts, invoices, or payment truth

The policy chain for protected local actions is:

```text
request
  -> transport protection
  -> activated LocalServer
  -> approved device credential
  -> signed local user session
  -> role permission
  -> product access resource/module check
  -> subscription/write policy
  -> validator
  -> application use case
  -> transaction/audit/outbox
```

## Module And Resource Access

SafarSuite app features should be protected by stable resources, not scattered hardcoded module checks.

Preferred model:

```text
reports.execute
  -> Reporting group
  -> reporting-core module
  -> LocalServer module gateway
  -> signed entitlement state
```

The local module gateway remains the authority for:

- active, warning, grace, restricted, expired, disabled, missing, and installation-mismatch states
- paid-until, warning, grace, and offline-valid dates
- installation binding
- signature/replay checks
- module-enabled decisions

The Windows app may show warnings or hide disabled entries, but backend API entry points must enforce the same decision.

## Cloud Control Channel

The Control Cloud control channel carries:

- setup tokens and bootstrap packages
- installation registration and deployment profile
- signed entitlement bundles
- offline renewal files
- signed local-server commands
- heartbeat receipts
- diagnostics uploads
- command acknowledgements
- portal commercial/license/setup projections
- audit events

It must not carry:

- travel bookings
- vouchers or daily operational ledgers from the client runtime
- tickets, tours, payroll, inventory, or branch transaction streams
- branch/HQ conflict-resolution events
- direct writes into deployed SafarSuite business tables

Those belong to the future operational data sync plane.

## Reliability Rules

The control path must be designed for retries and intermittent internet.

Required patterns:

- Control Desk publishes commercial/control changes through a durable outbox.
- Control Cloud accepts messages idempotently.
- LocalServer pulls cloud state outbound instead of waiting for inbound push.
- Entitlement bundles are signed, versioned, installation-bound, locally cached, and older-version protected.
- Commands are signed, versioned, idempotent, expiring, pulled, and acknowledged.
- App activation revocations travel as signed commands into LocalServer, then the app consumes LocalServer's local revocation-status endpoint instead of polling Control Cloud.
- Heartbeat status is stored separately from license validity.
- Offline renewal files wrap the same signed entitlement chain used by direct cloud pulls.
- Diagnostics include runtime, Docker/Compose, bootstrap, service, entitlement, trust-state, import audit, and recent-error facts.

Important product rule:

```text
heartbeat status != license validity
```

A paid offline-capable client keeps working through the signed paid/offline-valid period even if heartbeat is temporarily unavailable.

## Security Rules

Minimum security posture:

- TLS/HTTPS for cloud communication.
- Local LAN HTTPS should be the production target; pilot HTTP must remain a documented exception. The current deployment contract keeps the single LocalServer API gate protocol-configurable through `SAFARSUITE_LOCAL_API_ASPNETCORE_URLS`, server certificate path/password settings, generated local-CA automation, and an app-side trusted CA path.
- Setup tokens are one-time, short-lived, installation-scoped, stored as hashes, and consumed on registration.
- Entitlement bundles and commands are signed and installation-bound.
- Local app activation status checks must be bound to the bootstrapped client/installation and fail closed when a revoked issue is queried with a mismatched app identity.
- LocalServer stores signing/trust secrets in protected server-side configuration, never in the Windows app.
- Windows clients store only device identity/credential material through protected client storage.
- LocalServer never receives Control Cloud private signing keys.
- Control Cloud never stores client root/server passwords for normal deployment.
- Every setup, token use, entitlement issue/import, command, acknowledgement, heartbeat, diagnostics upload, renewal file, support override, and device/user rights change is audited.

## Source Of Truth

| Fact | Source of truth |
| --- | --- |
| Provider client, pricing, invoices, payments, contract modules, allowed devices/branches/users | Control Desk |
| Accepted cloud/portal commercial projection | Control Cloud |
| Client Portal credentials and sessions | Control Cloud |
| Setup token and installation registry | Control Cloud |
| Signed entitlement bundle and offline renewal file | Control Cloud |
| Runtime module/access decision | LocalServer from cached signed entitlement |
| Local users, roles, device approvals, local sessions | LocalServer |
| Local business records and local database | LocalServer/app modules |
| Future branch/HQ/cloud operational sync records | Future operational data sync plane |

## Current Implementation Status

Basic pieces already exist:

- Control Desk durable outbox and Control Cloud receiver.
- Control Cloud setup tokens, bootstrap packages, registration, audit, entitlement bundle issue, command queue, heartbeat, status, diagnostics, and offline renewal routes.
- LocalServer registration, entitlement pull/import/cache/gating, local import audit, clock/replay trust state, heartbeat, command processing, diagnostics, and module gateway.
- LocalServer Docker wrapper with `safarsuite-local-api`, `safarsuite-local-worker`, and `safarsuite-local-agent` commands; generated Compose/env templates now share state and mount the runtime services manifest into LocalServer and app containers.
- Install-shaped LocalServer Compose proof through `tools/SafarSuite.LocalServer.ComposeBootstrapProof`: generated package, stub Control Cloud, split API/worker/agent runtime, signed bootstrap import, entitlement pull, heartbeat, and module-gateway access. The `run-compose` mode now owns the one-command stub-cloud + Docker Compose + CA-pinned Local API verification path, while `verify-running-runtime` can re-check an already-running generated stack.
- Real Control Cloud LocalServer Compose proof through `tools/SafarSuite.LocalServer.ComposeBootstrapProof generate-real-cloud`: signed Control Desk entitlement seed, real Control Cloud bootstrap package generation, generated package import, installation registration, entitlement issue/pull, heartbeat, module-gateway access, command polling, and cloud status readback.
- App-runtime profile boot proof through the same generated Compose package: `safarsuite-app` starts healthy beside LocalServer when proof-shaped production readiness values are supplied, receives the external LocalServer URL, module-gateway URL, client id, installation id, and generated Local API CA path, and has passed over the HTTPS-default Local API lane.
- Control Cloud-owned app activation bridge proof through `tools/SafarSuite.LocalServer.ComposeBootstrapProof activate-app-runtime`: the proof reads the app activation state, mints a scoped provider bearer session from configured provider-operator credentials by default, asks Control Cloud to validate the registered installation and issue the app activation import payload from the latest signed entitlement issue, imports it into the app, verifies the app's own `Accounting` endpoint returns `Active`/allowed, and confirms the activation issue register while LocalServer remains registered and entitled through Control Cloud.
- Control Desk-managed app activation issuance: the Control Cloud issuer now accepts scoped provider bearer sessions with `app-activation:read`/`app-activation:write`, keeps the old provider key as a compatibility fallback, Control Desk can send a configured bearer token or legacy key through the cloud provisioning client, and the Cloud tab can issue and download the signed SafarSuite app activation import for a selected installation.
- SafarSuite app pre-login activation handoff: the Windows client can export app activation request JSON with server installation id, fingerprint, public key, and optional activation request id, then import the signed activation JSON downloaded from Control Desk through the LocalServer activation-token endpoint.
- SafarSuite app pre-login first-manager handoff: the same pre-login surface can open the shared Device Manager for first-device bootstrap, first-admin creation, manager login, and device approval, then transition a successful manager/admin session into the authenticated workspace.
- SafarSuite app setup-token authority diagnostics: Device Manager can import signed first-manager setup-token JSON, decode the token claims locally, and display whether the token is bound to the current LocalServer installation id and pending device id before bootstrap.
- Control Desk app/provider identity mapping UX: the Cloud tab can import app activation request JSON, auto-fill app server identity fields, show the provider LocalServer installation id beside the app LocalServer id/fingerprint before issuance, and show the issued provider-installation -> app-server mapping in the register/result.
- Control Cloud app activation mapping register: activation issuance writes structured provider-installation -> app-server issue metadata, Control Cloud exposes a provider-gated searchable issue list, Control Desk proxies it, and the Cloud tab shows the activation register beside import/issue/download.
- App activation mapping revoke flow: Control Cloud can revoke an activation issue with actor/reason/time and audit, Control Desk proxies the provider-gated command, and the Cloud tab exposes per-issue revoke from the activation register.
- App activation replacement rotation flow: new issuance can explicitly reference the revoked issue it replaces; Control Cloud verifies the replaced issue belongs to the same client/provider installation and is already revoked, persists the lineage, exposes it in the register/result, and Control Desk can prepare a replacement from a revoked row while requiring a fresh app activation request/public key import before issuance.
- App activation revocation command sync: Cloud-side revoke now signs and queues a `revoke_app_activation` installation command; LocalServer verifies it through the existing command-signature lane, records the revoked activation issue/app-server identity in a durable local ledger, acknowledges the command, and exposes `POST /api/v1/local-server/app-activations/revocation-status` with `Revoked`, `RevokedIdentityMismatch`, and `NotRevoked` states for app enforcement.
- SafarSuite app runtime revocation enforcement: the app LocalServer preserves imported activation issue/client/provider metadata, asks the provider LocalServer revocation-status endpoint through the LAN authority URL using `X-SafarSuite-Local-Api-Key`, blocks login/writes for revoked app activation issues, and fails closed with `CheckUnavailable` when the local authority cannot confirm status.
- Local API access-key gate: generated bootstrap artifacts now carry `SAFARSUITE_LOCAL_API_ACCESS_KEY`, the installer auto-generates it when not supplied, provider LocalServer requires it on the app activation revocation-status route, and the SafarSuite app sends it from its runtime configuration.
- HTTPS-default LAN transport contract: generated bootstrap artifacts now default to `SAFARSUITE_LOCAL_API_TLS_MODE=GeneratedLocalCa`, `SAFARSUITE_LOCAL_API_BASE_URL=https://local-api:8080`, `SAFARSUITE_MODULE_GATEWAY_URL=https://local-api:8080`, and `SAFARSUITE_LOCAL_API_ASPNETCORE_URLS=https://0.0.0.0:8080`; `HttpOnly` remains an explicit compatibility override. Compose mounts `certs/local-api` only into the LocalServer API and `certs/trust` only into the app runtime; the LocalServer API shim maps the configured certificate into Kestrel; and the SafarSuite app revocation client validates the provider LocalServer through the configured CA instead of bypassing certificate checks.
- Local API generated certificate automation: by default the installer creates a local CA, LocalServer API server certificate/PFX, trusted CA PEM, configurable DNS/IP SANs, and a durable private PFX password file. Installer reruns reuse that password and host-side local API health/import checks use the generated CA with curl.
- SafarSuite app shared Local API CA trust: app revocation checks now use a shared CA-pinned Local API HTTP client factory. The app backend currently hosts module-gateway access locally, so there is no duplicate outbound module-gateway client to secure.
- SafarSuite app workspace module-gateway enforcement for menu/window access, backend writes, reporting execution/audit, product access catalog, runtime health/profile, and container proof.
- Control Desk product access catalog persistence and publish path into the real app product kernel.
- SafarSuite app to LocalServer pairing authority slice: shared discovery/hello/device pairing/profile/first-manager setup-token/device-credential/manager-session contracts now exist, Control Cloud/Control Desk can issue and download signed first-manager setup tokens, LocalServer exposes non-secret `/.well-known/safarsuite-local-server` discovery, `POST /api/v1/local-server/pairing/hello`, pending device request create/status, signed first-manager token import, signed device credential verification, local manager-session minting, and manager-protected device list/approve/suspend/revoke endpoints backed by a local file ledger. LocalServer heartbeat now reports a non-secret pairing snapshot into Control Cloud status, and Control Desk plus the Client Portal preview surface pairing mode, device counts, and first-manager approval. The smoke proof validates signed credential issue/verification, the PostgreSQL proof validates pairing status persistence/readback through Control Cloud and Client Portal status routes, and the Docker Compose proof validates cloud issuance/download, discovery/hello identity, first-manager import/replay rejection, anonymous manager-route denial, manager-session approval flow, and device pairing lifecycle after bootstrap import.

Known remaining gaps:

- Production hardening for the app revocation hook and broader control lane: real provider-user MFA/password reset UX, production secret custody/rotation, customer-facing clean setup UX, and live deployment proof with production-grade secrets. Provider-operator file/PostgreSQL persistence, the provider-access smoke proof, and the first Control Desk manager surface now exist; see `docs/planning/control-cloud-provider-access-runbook-2026-07-07.md`.
- App-to-LocalServer pairing implementation beyond the local authority lifecycle slice: signed local pairing key custody, Windows app discovery UI, protected pairing-profile storage, and no-repeat client reconnect behavior; design captured in `docs/planning/safarsuite-app-local-server-pairing-flow.md`.
- Production install proof for the generated bootstrap package with real secret custody/rotation, not proof-local values.
- PostgreSQL-backed Control Cloud proof now exists through `tools/SafarSuite.ControlCloud.PostgresProof`; remaining deployment proof should focus on real secret custody/rotation and clean-machine packaging.
- Successful build/push of the LocalServer image to the chosen registry; the first Dockerfile exists, has a pre-published local build target, and has passed a local container `/health` proof using a cached ASP.NET-derived base override, but the production-default base still depends on .NET runtime image access from MCR.
- Production secret provisioning and rotation scripts.
- Clean customer-style one-time setup UX polish.
- Real provider/admin identity replacing the remaining development provider-key bootstrap shortcut behind the scoped provider-session gate.
- Operational data sync plane design and implementation.
- Final decision on whether the old systemd/self-contained install path remains as an advanced alternate.

## Drift Check

Before adding deployment, communication, rights, module, or sync work, answer:

1. Is this provider commercial control, cloud control-plane, local runtime, app UX, or operational business-data sync?
2. Which system owns the truth?
3. Does it use the LAN gate, cloud gate, signed outbox, signed entitlement, signed command, local role permission, or future data-sync plane?
4. Does it require inbound access to a client office? If yes, stop and justify it explicitly.
5. Would a future module be able to plug into this through catalog/resource/module contracts?
6. Can it retry safely without duplicate records or broken local state?
7. Is the action audited with actor, reason, client, installation, timestamp, and result?

If the answer is unclear, update this blueprint or the relevant boundary document before implementing.

## Related Documents

- `docs/architecture/product-direction.md`
- `docs/architecture/cloud-local-communication-map.md`
- `docs/architecture/client-deployment-and-data-sync-boundary.md`
- `docs/architecture/safarsuite-runtime-integration-boundary.md`
- `docs/architecture/product-module-catalog-boundary.md`
- `docs/planning/control-cloud-deployment-tracker.md`
- `docs/planning/offline-entitlement-control-rules.md`
- `docs/planning/safarsuite-app-integration-handoff.md`
- `docs/planning/safarsuite-app-local-server-pairing-flow.md`
