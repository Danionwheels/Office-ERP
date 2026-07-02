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
  -> portal issues one-time setup token and signed bootstrap bundle
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
| Online bootstrap | Client Linux server has internet during install | Accepted |
| Offline-assisted bootstrap | Portal bundle is downloaded elsewhere, copied to Linux server, then the server connects outward when internet is available | Accepted |

V1 does not require `.deb` packaging. A `.deb` installer can come later after the Docker Compose service layout, directories, ports, update behavior, and backup rules are stable.

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
| Identity and roles | Provider admins, support users, client users | Basic client contact invitation/user/session foundation exists with provider-key invite protection; real provider/admin users, MFA, email delivery, and audit pending |
| Client portal projection | Client-facing invoice/payment/subscription state | Basic backend projection exists |
| Activation/installations | Installation profiles, setup tokens, registered machines | Basic entitlement installation registry exists; setup tokens pending |
| Deployment packages | Versioned bundles, manifests, checksums, release channels | Proposed |
| Bootstrap downloads | Online command and offline-assisted bundle download | Proposed |
| Entitlements | Signed license bundles, direct pull, offline renewal files | Basic signing boundary plus local-server direct pull/verification/cache/gating exist; offline renewal files remain a fallback pending item |
| Command queue | Renew, revoke, change limits, diagnostics, update commands | Basic command queue exists |
| Heartbeat/status view | Last seen, version, license state, entitlement issue, command acknowledgement | Basic heartbeat endpoint, persistence, shared status endpoint, Control Desk status panel, and minimal Client Portal status preview exist |
| Audit | Every setup, token, entitlement, command, renewal, support action | Partial; must be expanded |

## V1 Todo

| Step | Goal | Status |
| --- | --- | --- |
| 1 | Document one-cloud rule and deployment rule | Done in this tracker |
| 2 | Define installation profile model: client, installation ID, mode, token, status, expiry | Proposed |
| 3 | Add one-time setup token generation and expiry rules | Proposed |
| 4 | Add deployment package manifest model: version, channel, checksum, signature, required services | Proposed |
| 5 | Add portal/API endpoint to download bootstrap bundle or copy install command | Proposed |
| 6 | Create first Docker Compose local-server template | Proposed |
| 7 | Create `install.sh` bootstrap script with signature/checksum verification | Proposed |
| 8 | Add outbound registration endpoint for local Linux server | Proposed |
| 9 | Add heartbeat endpoint and portal status view | Basic done: heartbeat endpoint/reporting, shared status endpoint, Control Desk status panel, and `/client-portal/index.html` preview are in place |
| 10 | Connect first signed entitlement pull after registration | Direct HTTP pull adapter exists; registration-bound pull wiring pending |
| 11 | Add command pull/acknowledgement to the local agent | Proposed |
| 12 | Add diagnostics export/upload for failed installs | Proposed |
| 13 | Add support/admin audit for every token, download, install, heartbeat, command, and entitlement | Proposed |
| 14 | Add persisted client user invitations, password credentials, role assignment, and session audit | Basic Control Desk contact invites, provider-key invite protection, and password credentials done; real provider/admin authorization, email delivery, resend/revoke/list, and session audit pending |

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
- Bootstrap bundles are signed and checksum verified.
- Local server registration binds the installation to a generated local identity.
- Entitlement bundles are signed, versioned, installation-bound, locally verified, cached, and older-version protected.
- Every install, token use, heartbeat, command, entitlement, renewal file, and support action is audited.
- The local server connects outward over HTTPS.
- Any emergency support path must require consent, expiry, reason, actor, and audit.

## Link To Licensing Rules

Deployment gets the local server installed and registered. Licensing decides whether the installed server can keep operating.

Canonical licensing note:

```text
docs/planning/offline-entitlement-control-rules.md
```
