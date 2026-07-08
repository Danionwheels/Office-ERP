# Control Cloud Deployment Tracker

Date started: 2026-07-01

Use this tracker for the SafarSuite Control Cloud, Client Portal, and local SafarSuite deployment/setup work. Its purpose is to keep cloud, portal, licensing, and local Linux deployment decisions aligned.

## Core Decision

There is one production SafarSuite Control Cloud.

```text
SafarSuite Control Desk
  office ERP and commercial source of truth
    -> publishes approved client, invoice, payment, contract, and entitlement events

SafarSuite Control Cloud + Client Portal
  one runtime control plane and one portal
    -> portal state, activation, deployment setup, signed entitlements, command queue, heartbeat, audit

SafarSuite client local server
  client-owned Linux/local server
    -> pulls setup, config, licenses, updates, and commands from Control Cloud
```

The older SafarSuite workspace `CloudServer` remains reference/prototype material for activation, signing, product-kernel commands, owner dashboard ideas, and replay protection. It is not a second production cloud.

## Deployment Rule

The portal controls deployment, but the client Linux server initiates the connection outward.

Do not design V1 around direct cloud push into a client server.

Avoid by default:

- public SSH access into client servers
- storing client root/SSH credentials in the cloud
- requiring static public IP addresses
- requiring inbound firewall openings at the client office
- making remote push deployment the normal path

Preferred model:

```text
Portal creates installation
  -> portal issues one-time setup token and bootstrap package
technician copies/runs bundle on client Linux server
  -> installer verifies bundle
  -> installer starts SafarSuite local services
  -> local server connects outward to Control Cloud
  -> Control Cloud activates installation, sends config/license/commands
Portal shows setup status, version, heartbeat, and license state
```

## V1 Decision

V1 deployment uses Docker Compose.

Why:

- easier install on varied Linux servers
- simpler rollback and version pinning
- consistent support shape across clients
- good fit for local server API, database, worker, and portal/agent services
- avoids packaging too early while service boundaries are still moving

V1 should support two setup modes:

| Mode | Use Case | V1 Status |
| --- | --- | --- |
| `OnlineBootstrap` | Client Linux server has internet during install | Accepted |
| `OfflineAssistedBootstrap` | Portal bundle is downloaded elsewhere, copied to Linux server, then the server connects outward when internet is available | Accepted |

V1 does not require `.deb` packaging. A `.deb` installer can come later after the Docker Compose service layout, directories, ports, update behavior, and backup rules are stable.

Setup mode is not the same as the client's runtime deployment mode. Runtime deployment modes are `OfflineLocal`, `BranchToHqSync`, `CloudSyncMultiBranch`, and `HostedSaas`.

See:

```text
docs/architecture/client-deployment-and-data-sync-boundary.md
```

## V1 Deployment Shape

Portal/admin flow:

```text
create client
  -> create installation profile
  -> select deployment mode: online bootstrap or offline-assisted bootstrap
  -> generate one-time setup token
  -> download signed bootstrap bundle or copy install command
  -> monitor installation status
```

Client Linux server flow:

```text
receive install command or bootstrap bundle
  -> run installer locally with sudo
  -> verify signed manifest/checksums
  -> install Docker/Docker Compose prerequisites or report missing prerequisites
  -> create installation identity
  -> register outward with Control Cloud using one-time token
  -> pull deployment config
  -> start SafarSuite local services
  -> pull signed entitlement bundle
  -> send first heartbeat
  -> show active/pending/failed state in portal
```

## Bootstrap Bundle Contents

V1 signed bundle should contain:

```text
install.sh
docker-compose.yml
manifest.json
sha256 checksums
Control Cloud public verification key
bootstrap agent or registration script
environment template
```

Optional for offline-assisted mode:

```text
Docker image tar files
pre-downloaded package dependencies
initial signed entitlement or renewal file
```

The bundle must be signed and versioned. The installer must fail closed if the signature or checksum verification fails.

## Local Services

Expected Docker Compose services:

```text
safarsuite-local-api
safarsuite-local-worker
safarsuite-local-ui
safarsuite-local-db
safarsuite-local-agent
```

The local agent owns:

- installation registration
- outbound heartbeat
- command pull
- command acknowledgement
- entitlement bundle pull/import
- update check
- diagnostics export/upload

## Portal Responsibilities

The single Client Portal / Control Portal should expose role-based areas:

| Area | Users | Purpose |
| --- | --- | --- |
| Admin control | Provider owner/admin/support | Clients, contracts, invoices, payments, licenses, devices, deployments, commands |
| Client portal | Client owner/accounts/admin | Invoices, payment status, subscription, renewal files, setup bundle downloads |
| Installation status | Provider support and permitted client admins | Setup token, bundle version, last seen, installed version, command status, diagnostics |

One backend can serve these areas. Separate permissions decide what each user can see or do.

## Cloud Modules Needed

| Module | Responsibility | Status |
| --- | --- | --- |
| Identity and roles | Provider admins, support users, client users | Basic client contact invitation/user/session foundation exists with provider-key invite management, file/SMTP invitation delivery, and invite/session audit records; real provider/admin users, MFA, password reset, and production mail retry handling pending |
| Client portal projection | Client-facing invoice/payment/subscription state | Basic backend projection exists |
| Activation/installations | Installation profiles, setup tokens, registered machines | Basic entitlement installation registry, one-time setup-token registration, Control Desk setup-token/bootstrap controls, signed bootstrap bundle/download generation, Docker Compose template artifacts, and bootstrap package command generation exist; installer/runtime integration pending |
| Deployment packages | Versioned bundles, manifests, checksums, release channels | Basic backend register done: bootstrap package generation now stamps non-secret package metadata onto setup-token records, file/PostgreSQL persistence can list package summaries by client installation, Control Cloud/Control Desk expose provider-gated package register reads, and setup-token/bootstrap-package creation plus bundle download are behind `deployment-packages:write`; release channel/version promotion remains pending |
| Bootstrap downloads | Online command and offline-assisted bundle download | Basic Control Desk provisioning action, copyable online command package, signed JSON bootstrap bundle download, served `install.sh`, Docker Compose template, environment template, and runtime service manifest exist; real SafarSuite image bundle download pending |
| Entitlements | Signed license bundles, direct pull, offline renewal files | Basic signing boundary, setup-token-bound installation registration, local-server direct pull/verification/cache/gating, offline renewal file export/import, local import audit persistence, and local clock/replay trust state exist |
| Command queue | Renew, revoke, change limits, diagnostics, update commands | Basic local execution loop exists for diagnostics and entitlement refresh |
| Heartbeat/status view | Last seen, version, license state, entitlement issue, command acknowledgement | Basic heartbeat endpoint, persistence, shared status endpoint, Control Desk status panel, and minimal Client Portal status preview exist |
| Diagnostics | Local-server support bundle export/upload | Basic typed diagnostics bundle, local exporter, upload client, Control Cloud receive endpoint, latest-report endpoint, PostgreSQL/file persistence, local import-audit section, runtime service manifest boundary, runtime/bootstrap/service/error diagnostic slots, and Control Desk latest diagnostics review/download exist; deployed runtime log tail collection pending |
| Audit | Every setup, token, entitlement, command, renewal, support action | Partial; invite/session, entitlement issue, setup-token creation, signed bootstrap package generation/download, local-server registration accept/reject, diagnostics upload, offline renewal file generation, local import audit persistence, local replay/clock-warning state, command, acknowledgement, heartbeat trails, basic audit-events read endpoint, and Control Desk installation history visibility exist; support override audit still pending |

## V1 Todo

| Step | Goal | Status |
| --- | --- | --- |
| 1 | Document one-cloud rule and deployment rule | Done in this tracker |
| 2 | Define installation profile model: client, installation ID, mode, token, status, expiry | Basic done |
| 3 | Add one-time setup token generation and expiry rules | Basic done |
| 4 | Add deployment package manifest model: version, channel, checksum, signature, required services | Basic backend register done: generated bootstrap packages persist package id, local/app versions, bundle file name, bundle checksum, generated time, setup-token status, expiry, and consumption metadata without storing setup-token plaintext; full release-channel/package promotion model remains pending |
| 5 | Add portal/API endpoint to download bootstrap bundle or copy install command | Basic done: API can generate a bootstrap package with setup token, cloud endpoints, install command, signed bundle metadata, artifact manifest, and a signed JSON bundle download |
| 6 | Create first Docker Compose local-server template | Basic done: first `docker-compose.yml`, `local-server.env`, and `runtime-services.manifest.json` template artifacts cover local API, worker, agent, database, and optional `safarsuite-app` profile slots |
| 7 | Create `install.sh` bootstrap script with signature/checksum verification | Basic done: Control Cloud serves the first `install.sh` template; it can verify a supplied bundle checksum, write bootstrap/compose/env files, register online, and only starts Compose when explicitly requested |
| 8 | Add outbound registration endpoint for local Linux server | Basic done |
| 9 | Add heartbeat endpoint and portal status view | Basic done: heartbeat endpoint/reporting, shared status endpoint, Control Desk status panel, and `/client-portal/index.html` preview are in place |
| 10 | Connect first signed entitlement pull after registration | Basic done: entitlement issue now requires a registered installation |
| 11 | Add command pull/acknowledgement to the local agent | Basic done: local-server application/infrastructure layers can pull signed pending commands, verify HMAC signatures, execute `request_diagnostics`, `refresh_entitlement`, and Cloud-owned `revoke_app_activation`, persist app activation revocations, expose local revocation status for the app, and post Applied/Failed/Rejected acknowledgements |
| 12 | Add diagnostics export/upload for failed installs | Basic done: local-server libraries can export cached entitlement/trust-state diagnostics plus local import audit and runtime/bootstrap/service/error facts, then upload them to Control Cloud; runtime manifest boundary exists, deployed log collection pending |
| 13 | Add support/admin audit for every token, download, install, heartbeat, command, and entitlement | Partial: setup token, bootstrap package, registration accept/reject, invite/session, command, acknowledgement, heartbeat, entitlement issue, offline renewal generation, and local import audit records are recorded; basic audit-events API is available |
| 14 | Add persisted client user invitations, password credentials, role assignment, and session audit | Basic Control Desk contact invites, provider-key invite management, password credentials, list/resend/revoke, file/SMTP delivery boundary, and invite/session audit done; real provider/admin authorization, MFA, password reset, and production mail retry handling pending |
| 15 | Add explicit installation/deployment profile fields for bootstrap mode, client deployment mode, site/branch identity, parent site, and sync topology metadata | Basic done: setup tokens and registered installations persist the profile, and bootstrap/status/registration/heartbeat/diagnostics surfaces can expose it |
| 16 | Add provider-facing Control Desk setup-token/bootstrap package actions | Basic done: the client desk Cloud tab saves the selected deployment profile, proxies setup-token/bootstrap-package creation to Control Cloud, and displays the generated setup token or install command |
| 17 | Add provider-facing installation audit history to Control Desk | Basic done: the client desk Cloud tab can refresh setup/bootstrap/registration/diagnostics/renewal audit events for the selected installation through the Control Desk API |
| 18 | Add provider-facing latest diagnostics review to Control Desk | Basic done: the client desk Cloud tab can refresh and download the latest diagnostics report for the selected installation through the Control Desk API |
| 19 | Add provider-facing low-risk support command actions | Basic done: the client desk Cloud tab can queue whitelisted `request_diagnostics` and `refresh_entitlement` commands through Control Desk into Control Cloud's signed installation command queue; Cloud-owned app activation revoke queues `revoke_app_activation` directly after revoke, and the local-server command processor executes, persists, exposes local revocation status, and acknowledges all three command types |

## Later Todo

| Step | Goal | Why Later |
| --- | --- | --- |
| 1 | `.deb` installer packaging | Wait until Docker Compose directories, services, ports, and update behavior are stable |
| 2 | Fully offline bundle with Docker images and initial license included | Larger bundle and stricter signing/update requirements |
| 3 | Remote update rollout waves | Needs stable version channels and rollback behavior |
| 4 | Client-side backup/restore automation from portal | Needs data retention and security policy |
| 5 | Optional VPN/agent-assisted remote support | Needs strict consent, audit, and access controls |
| 6 | Separate Control Cloud repo | Split only when deployment complexity demands it |

## Non-Goals For V1

- Do not build a direct SSH push deployment system.
- Do not store client root credentials in Control Cloud.
- Do not make inbound access to client offices mandatory.
- Do not make `.deb` packaging the first deployment target.
- Do not mix Control Cloud deployment/licensing with client business-data sync.
- Do not create a second production cloud in the SafarSuite client product workspace.

## Security Rules

- Setup tokens are one-time, short-lived, and installation-scoped.
- Setup tokens are stored only as hashes and consumed on successful registration.
- Bootstrap packages expose one-time setup tokens only once; the current JSON bootstrap bundle is signed and downloadable with template artifact checksums, and later full Docker image bundles must include full manifest checksums.
- Local server registration binds the installation to a generated local identity.
- Entitlement bundles are signed, versioned, installation-bound, locally verified, cached, and older-version protected.
- Every install, token use, heartbeat, command, entitlement issue/import, renewal file, and support action is audited.
- The local server connects outward over HTTPS.
- Any emergency support path must require consent, expiry, reason, actor, and audit.

## Link To Licensing Rules

Deployment gets the local server installed and registered. Licensing decides whether the installed server can keep operating.

Canonical licensing note:

```text
docs/planning/offline-entitlement-control-rules.md
```
