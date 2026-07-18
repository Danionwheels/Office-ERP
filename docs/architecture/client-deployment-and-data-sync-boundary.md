# Client Deployment And Data Sync Boundary

Date added: 2026-07-02

This note defines how SafarSuite deployment modes, local-server setup, branch/site identity, and future business-data sync should stay separated.

Physical placement and deployment acceptance are governed by `docs/architecture/final-system-requirements-and-deployment-contract.md`. The `local server` in this note is the client-premises SafarSuite runtime, not a separate provider-office Control Desk server.

The goal is to keep the current billing/license/control chain solid while leaving clean room for offline local, branch-to-HQ sync, cloud-sync multi-branch, and hosted SaaS deployments.

## Canonical Vocabulary

The shared constants live in:

```text
src/SafarSuite.ControlDesk.Contracts/ControlCloud/V1/LocalServerDeploymentTopologyContracts.cs
```

There are two different concepts that must not be mixed.

| Concept | Values | Meaning |
| --- | --- | --- |
| Bootstrap mode | `OnlineBootstrap`, `OfflineAssistedBootstrap` | How a local server is installed and registered |
| Client deployment mode | `OfflineLocal`, `BranchToHqSync`, `CloudSyncMultiBranch`, `HostedSaas` | How the client runs SafarSuite after setup |
| Data plane | `CommercialControl`, `OperationalBusinessDataSync` | Which channel a fact belongs to |

The current setup-token/bootstrap API already has a `DeploymentMode` field. Until we introduce explicit installation-profile fields, treat that field as the bootstrap mode. Do not put `OfflineLocal`, `BranchToHqSync`, `CloudSyncMultiBranch`, or `HostedSaas` into that field.

## System Responsibility

| System | Owns | Must not own |
| --- | --- | --- |
| SafarSuite Control Desk | Client, contract, pricing, invoices, payments, GL, allowed modules, allowed devices/users/branches, deployment-mode choice | Client portal credentials or client operational business records |
| SafarSuite Control Cloud | Accepted commercial projection, setup tokens, bootstrap packages, installation registry, signed entitlements, commands, heartbeat, diagnostics, audit, portal reads | Provider accounting truth or operational branch/HQ transaction sync |
| SafarSuite Client Portal | Client-facing invoices, payment status, license status, setup visibility, renewal-file downloads | Local feature enforcement or provider accounting decisions |
| SafarSuite local server/app | Local runtime, cached signed entitlement, module gates, branch/device/user limit enforcement, heartbeat, command pull/ack, diagnostics | Pricing, billing policy, Control Cloud identity, or provider GL |
| Future SafarSuite Data Sync | Operational branch/HQ/cloud data replication for modules such as Travel, Tour, Payroll, etc. | Billing/license authority |

## Deployment Modes

| Mode | Runtime shape | Control channel role | Data-sync role |
| --- | --- | --- | --- |
| `OfflineLocal` | One client local server/app, usually on LAN | Registration, signed entitlement, module/device/branch limits, heartbeat when internet exists, offline renewal file fallback | None by default |
| `BranchToHqSync` | Branch installations sync operational data to a client HQ installation | Each registered installation reports status; entitlement controls allowed branch count, modules, users/devices, and sync permission | Future branch-to-HQ data-plane module owned by SafarSuite |
| `CloudSyncMultiBranch` | Branch/HQ installations sync operational data through a cloud data plane | Entitlement controls cloud-sync module access, branch limits, installation identity, heartbeat, commands, diagnostics | Future cloud data-sync plane; not the Control Cloud billing/license channel |
| `HostedSaas` | Client runtime is hosted by provider/cloud | Entitlement controls hosted tenant access, modules, seats/devices where relevant, renewal/restriction state | Hosted SafarSuite tenant/data-plane owns operational data |

These are modes for the same SafarSuite product. They are not separate products.

## Identity Model

Current implemented identity:

| Field | Current meaning |
| --- | --- |
| `clientId` | Provider commercial client/customer |
| `installationId` | Registered local-server/runtime installation |

Future identity room:

| Field | Purpose |
| --- | --- |
| `clientDeploymentMode` | One of the canonical client deployment modes |
| `siteId` | Stable runtime site such as HQ, branch, warehouse, or hosted tenant |
| `siteRole` | `Hq`, `Branch`, `Standalone`, or `Hosted` |
| `parentSiteId` | HQ/parent for branch-to-HQ deployments |
| `branchCode` | Business branch code used by the SafarSuite app, not by provider accounting |
| `syncTopologyId` | Future data-plane grouping for branch/HQ/cloud sync |

Important rule: do not overload `installationId` to mean branch, site, tenant, or client. It is a runtime installation identity only.

## Control Channel

The commercial/control channel may carry:

- client and installation identifiers
- bootstrap mode and later client deployment mode
- allowed modules
- allowed users, devices, and branches
- paid-until, warning, grace, offline-valid-until, and trust policy dates
- signed entitlement bundles and offline renewal files
- signed commands such as renew, suspend, change limit, request diagnostics, or change module state
- heartbeat and reported license state
- diagnostics, runtime version, service health, bootstrap checksum, and local import audit
- portal-visible invoice/payment/license summaries projected from accepted Control Desk events

The commercial/control channel must not carry:

- travel bookings, tours, payroll records, tickets, vouchers, daily operational activity, customer business records, or branch operational ledgers
- branch/HQ conflict-resolution events
- cloud data-sync event streams
- owner dashboard operational analytics sourced from client business records
- direct writes into the deployed SafarSuite app database

## Business Data Sync Channel

Business-data sync is a future data-plane module. It can be entitled and billed by Control Desk, but the operational records belong outside the Control Cloud billing/license channel.

Examples:

```text
Control Desk
  decides that Cloud Sync is paid and enabled

Control Cloud
  signs entitlement that allows Cloud Sync and N branches

SafarSuite app/local server
  enables the Cloud Sync module

Future Data Sync plane
  moves operational SafarSuite business records between branch/HQ/cloud
```

Payment failure can restrict future access through signed entitlements, but it must not corrupt, partially delete, or mingle with the client's operational sync data.

## Module And Plan Rule

Deployment modes are controlled through modules and limits:

| Capability | Suggested entitlement representation |
| --- | --- |
| Offline local runtime | Baseline local runtime/module entitlement |
| Branch-to-HQ sync | Paid add-on module plus allowed branch count |
| Cloud sync | Paid add-on module plus allowed branch count and cloud-sync permission |
| Hosted SaaS | Hosted deployment mode plus hosted tenant entitlement |
| Travel, Tour, Payroll, etc. | Independent modules, included or paid add-ons depending on catalog |

The final module list is intentionally not fixed. The product module catalog remains the flexible source for included-for-all and paid add-on modules.

## Transaction And Sync Rules

- Control Desk accounting actions still use local transactions and the existing Control Cloud outbox.
- Control Desk must not call Control Cloud directly inside invoice/payment/GL transactions.
- Control Cloud projection writes should remain transactional inside the cloud database for accepted control messages.
- Future operational business-data sync must own its own outbox/event log and conflict rules.
- Do not put future operational sync events into `CloudOutboxMessage` unless the message is only a commercial/control event about paid access to sync.
- Do not require business-data sync for offline local licensing to work.

## What Is Implemented Now

- Setup/bootstrap supports a bootstrap mode, currently `OnlineBootstrap` or `OfflineAssistedBootstrap`.
- Setup tokens and registered installations now carry an explicit deployment profile with bootstrap mode, client deployment mode, site id, site role, optional parent site id, optional branch code, and optional sync topology id.
- Bootstrap package responses, signed bootstrap payloads, registration responses, installation status responses, heartbeat responses, diagnostics bundles, install commands, and environment templates can surface the deployment profile.
- Local-server setup tokens, bootstrap packages, signed runtime plan, diagnostics, heartbeat, command queue, signed entitlement pull/import, offline renewal files, local import audit, and module-gateway access checks exist in basic form.
- Contracts and entitlement snapshots already carry allowed branch counts and module allowances.
- The real SafarSuite app workspace is not in this repository, so real app-side branch/site enforcement and operational sync changes must happen there later.

## Next Implementation Room

The explicit installation/deployment profile now exists:

```text
clientId
installationId
bootstrapMode
clientDeploymentMode
siteId
siteRole
parentSiteId
branchCode
syncTopologyId
registeredAtUtc
status
```

Last-seen state remains heartbeat/status data, not part of the static profile.

Next, add Control Desk/client-desk setup controls that let provider staff choose the client deployment mode and site/branch values when creating a setup token or bootstrap package. Keep this as control metadata only; do not implement operational data sync in this step.

After that, the SafarSuite app workspace can consume the profile and enforce branch/site/module behavior locally.
