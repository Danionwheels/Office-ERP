# SafarSuite Staging Deployment Runbook

Date added: 2026-07-10

Use this runbook for the first non-production deployment of SafarSuite Control Cloud, SafarSuite Control Desk, and a clean-machine SafarSuite local server install.

This is a staging proof, not a customer production launch. Use demo clients only until production auth, secret custody, backup/restore, monitoring, and operational recovery are proven.

## Topology Warning

The final deployment topology is governed by `docs/architecture/final-system-requirements-and-deployment-contract.md`.

The full-stack Linux shape in this runbook is a **Disposable Integration Lab**, not the SafarSuite Control Desk production topology. Control Desk V1 belongs entirely on one dedicated office PC with its local API and local PostgreSQL. Linux/cloud infrastructure is used for Control Cloud/Client Portal staging or client-runtime proof. A hosted `desk-staging` instance may be used only for disposable demo-data integration testing and must never be treated as the office deployment target.

## Goal

Prove the real deployment loop outside the local developer machine:

```text
Control Desk staging
  -> publishes signed commercial/control events
Control Cloud staging + Client Portal preview
  -> stores projection, setup packages, entitlements, commands, heartbeat, diagnostics, audit
Clean Linux local server
  -> installs from generated package, registers outward, pulls entitlement, reports heartbeat, processes support commands
```

## Deployment Rules

- Keep one staging Control Cloud; do not create a second cloud in the client app workspace.
- Keep the local server outbound-only. Do not require inbound access to the client/local-server VM.
- Use PostgreSQL, not in-memory or file persistence, for the staging services.
- Use HTTPS for every browser/cloud endpoint.
- Keep generated setup tokens and bootstrap packages demo-only during this pass.
- Store all staging secrets in the host secret store or mounted secret files. Do not commit them.
- Do not reuse local development secrets from `appsettings*.json`.

## Staging Topology

| Piece | First staging shape | Notes |
| --- | --- | --- |
| Control Cloud API + Client Portal preview | Public HTTPS service | This is the cloud endpoint local servers call outward. |
| Control Cloud database | PostgreSQL database | Prefer a dedicated staging database such as `safarsuite_control_cloud_staging`. |
| Control Desk API/UI | Canonically the dedicated office PC; optional hosted copy only in the Disposable Integration Lab | The hosted lab copy is not a production option. |
| Control Desk database | PostgreSQL on the dedicated office PC; optional isolated lab database | A Linux lab database may contain demo data only and is not office authority. |
| Clean local server | Disposable Linux VM with Docker Compose | Used to prove customer setup without real customer data. |

The first VPS-style starter bundle lives in:

```text
deploy/staging/
```

It contains source-build Dockerfiles, a Docker Compose stack, Caddy HTTPS routing, an Nginx-served Control Desk UI, `.env.example`, and a server-side secrets folder template.

For the current tester domain, use:

```text
cloud-staging.forgeaxis.tech
desk-staging.forgeaxis.tech
```

Current Linux staging server LAN IP:

```text
192.168.10.14
```

Because this is a private LAN address, public DNS should point to the router/public WAN IP with ports `80` and `443` forwarded to `192.168.10.14`. Pointing public DNS directly at `192.168.10.14` is only useful for internal/LAN resolution and will not satisfy normal public HTTPS certificate challenges.

## Phase 0: Local Gate

Run the local verification pass before deploying:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-Local.ps1
```

Expected result:

- solution build passes
- Control Desk UI production build passes, allowing the known large-chunk Vite warning
- in-memory accounting smoke passes
- LocalServer entitlement smoke passes
- no new unexpected `git diff --check` failures

## Phase 1: Staging Config Inventory

Create staging values for these settings before starting the services.

### Control Cloud

| Setting | Staging value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `Persistence__Provider` | `Postgres` |
| `ConnectionStrings__ControlCloud` | PostgreSQL connection string for the Control Cloud staging DB |
| `ControlCloud__BootstrapPackages__CloudBaseUrl` | Public HTTPS Control Cloud base URL |
| `ControlCloud__BootstrapPackages__InstallScriptUrl` | Public install script URL if the default served artifact URL is not enough |
| `ControlCloud__Receiver__SigningKeys__0__KeyId` | Staging Control Desk publisher key id |
| `ControlCloud__Receiver__SigningKeys__0__SecretFile` or `Secret` | Staging Control Desk publisher HMAC secret |
| `ControlCloud__EntitlementSigning__ActiveKeyId` | Staging entitlement signing key id |
| `ControlCloud__EntitlementSigning__SigningKeys__0__KeyId` | Same active entitlement key id |
| `ControlCloud__EntitlementSigning__SigningKeys__0__SecretFile` or `Secret` | Staging entitlement HMAC secret |
| `ControlCloud__AppActivationSigning__ActiveKeyId` | Staging app activation key id |
| `ControlCloud__AppActivationSigning__PublicKeyPemFile` | Staging ECDSA public key file |
| `ControlCloud__AppActivationSigning__PrivateKeyPemFile` | Staging ECDSA private key file |
| `ClientPortal__ProviderAccess__SharedSecretFile` or `SharedSecret` | Transitional provider access shared secret used by Control Desk compatibility calls |
| `ClientPortal__ProviderAccess__SessionSigningSecretFile` or `SessionSigningSecret` | Provider session signing secret |
| `ClientPortal__ProviderAccess__TotpProtectionSecretFile` or `TotpProtectionSecret` | Provider TOTP protection secret |
| `ClientPortal__Access__SessionSigningSecret` | Client portal session signing secret |
| `CLIENT_PORTAL_INVITATION_DELIVERY_PROVIDER` | Must be `Smtp` outside Development |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASS`, `FROM_ADDRESS` | Production SMTP connection and sender settings; user/password may both be empty only for an anonymous relay |
| `AllowedHosts` | Staging host name, or controlled wildcard only behind a trusted proxy |

If the host supports mounted secrets, prefer `*File` settings where available.

Before startup, run the redacted repository preflight and the quiet Compose validation. Do not render the resolved Compose configuration into logs because it contains secret-bearing environment values.

```powershell
dotnet run --project tools/SafarSuite.StagingPreflight --configuration Release -- --staging-directory deploy/staging
docker compose -f deploy/staging/docker-compose.yml config --quiet
```

### Control Desk

| Setting | Staging value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `Persistence__Provider` | `Postgres` |
| `ConnectionStrings__ControlDesk` | PostgreSQL connection string for the Control Desk staging DB |
| `ControlDesk__OperatorAccess__SessionSigningSecret` | Independent non-placeholder Control Desk bearer-session signing secret with at least 32 characters |
| Control Desk operators | Persisted in PostgreSQL and provisioned through the approved bootstrap path; configuration-based users are rejected in Staging |
| `ControlCloud__Publisher__Mode` | `Http` |
| `ControlCloud__Publisher__Environment` | `Staging` |
| `ControlCloud__Publisher__SigningKeyId` | Must match the Control Cloud receiver key id |
| `ControlCloud__Publisher__SigningSecret` | Must match the Control Cloud receiver secret |
| `ControlCloud__Publisher__EndpointUrl` | `https://<control-cloud-host>/api/v1/control-desk/messages` |
| `ControlCloud__Status__BaseUrl` | Public HTTPS Control Cloud base URL |
| `ControlCloud__Status__ProviderAccessSecret` | Same transitional provider access shared secret configured in Control Cloud |
| `ControlCloud__PortalInvitations__BaseUrl` | Public HTTPS Control Cloud base URL |
| `ControlCloud__PortalInvitations__ProviderAccessSecret` | Same transitional provider access shared secret configured in Control Cloud |

When provider bearer-token handoff is fully operational for the office flow, prefer `ProviderAccessToken` over the transitional shared secret.

## Phase 2: Database Migration

The staging Compose bundle binds PostgreSQL to loopback only: Control Cloud on `127.0.0.1:55432` and Control Desk on `127.0.0.1:55433` by default. Run migrations on the staging host, adjust the ports if `.env` overrides them, and set each `SAFARSUITE_*_CONNECTION_STRING` explicitly. The design-time factories otherwise fail closed. `SAFARSUITE_ALLOW_DEVELOPMENT_DB_FALLBACK=true` is a local-tooling escape hatch and must never be enabled on staging.

Apply the Control Cloud migrations to the staging Control Cloud database:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --startup-project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --context ControlCloudDbContext
```

Apply the Control Desk migrations to the staging Control Desk database:

```powershell
dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlDesk.Infrastructure --startup-project src/SafarSuite.ControlDesk.Api --context ControlDeskDbContext
```

Run migrations with the staging connection strings in the environment of the command.

Before applying either migration set, record `current_database()` and `current_user`. After applying, retain the migration-ledger count and latest migration ID. Expected at this checkpoint:

- Control Cloud: 23 rows through `20260713221145_AddClientPortalPaymentBoundary` in `cloud.__ef_migrations_history`.
- Control Desk: 32 rows through `20260713220254_AddPortalPaymentBoundary` in `control.__ef_migrations_history`.
- Control Desk: `pg_trgm` is present in `pg_extension`.

## Phase 3: Service Smoke

After the services are running:

- open the Control Cloud health/root endpoint or API index available for the chosen host
- open the Client Portal preview page
- open the Control Desk UI/API
- confirm Control Desk can reach Control Cloud status endpoints
- create or verify the first staging provider operator
- keep logs visible for both services while running the first end-to-end test

## Phase 4: Demo End-To-End Proof

Use one demo client and one demo installation.

Checklist:

- create the demo client in Control Desk
- configure the deployment profile
- issue or publish a demo entitlement snapshot
- publish pending Control Desk outbox messages to Control Cloud
- create a setup token/bootstrap package from the Control Desk Cloud tab
- download or copy the generated install command/package
- run the package on a clean Linux VM with Docker Compose
- verify local server registration becomes active
- verify entitlement pull succeeds
- verify heartbeat is received
- queue `request_diagnostics`
- verify the local server acknowledges the command
- verify latest diagnostics is visible/downloadable in Control Desk
- issue first-manager setup token
- prove manager/device pairing on the local runtime
- issue app activation import if the app runtime slot is included
- verify module access reports `Accounting` as allowed/active

Record the client id, installation id, package id, entitlement version, command id, diagnostics report id, and any operator used for the proof in `docs/planning/project-tracker.md`.

## Phase 5: Production Readiness Blockers

Do not put real customers on the system until these are explicitly handled:

- production provider/admin auth, including MFA and password reset/recovery
- production key storage and rotation for HMAC and ECDSA keys
- PostgreSQL backup/restore rehearsal
- service monitoring, log retention, and alerting
- SMTP credential provisioning plus delivery-failure monitoring and alerting
- migration rollback/recovery plan
- clear process for replacing leaked staging secrets
- local-server abuse controls for noisy pairing/discovery endpoints
- at least one clean-machine customer setup proof from a freshly provisioned Linux VM

## Rollback Rule

If a staging proof fails, do not patch directly on the staging machine first. Capture logs, reproduce locally or in a disposable proof environment, fix in the repo, run the local gate, then redeploy staging.
