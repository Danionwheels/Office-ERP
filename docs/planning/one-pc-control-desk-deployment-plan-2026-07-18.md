# SafarSuite Control Desk One-PC Deployment Plan

Date started: 2026-07-18

Status: Active implementation plan.

Authority: [Final System Requirements And Deployment Contract](../architecture/final-system-requirements-and-deployment-contract.md). This plan implements that contract and cannot change the accepted physical topology.

## Milestone Outcome

Prove that a normal office operator can install, start, use, update, back up, and recover SafarSuite Control Desk on one clean Windows office PC without operating Linux, Docker, PostgreSQL tools, Node.js, the .NET SDK, a public domain, inbound router forwarding, or SMTP.

The accepted runtime shape is:

```text
One dedicated Windows office PC
  desktop/Start-menu entry
      -> http://127.0.0.1:5188
  local Control Desk API Windows service
      -> built React UI on the same origin
      -> native local PostgreSQL Windows service
      -> outbound HTTPS to SafarSuite Control Cloud when available
```

This milestone is not complete when the application merely runs from source. It is complete only after the clean-PC, reboot, outage, backup/restore, and failed-update exercises pass with retained evidence.

## Repository Baseline

The business application is ready to enter packaging work, but the one-PC product lifecycle does not exist yet.

| Existing foundation | Evidence | Remaining deployment gap |
| --- | --- | --- |
| React UI already calls relative `/api` routes | [`httpClient.ts`](../../apps/control-desk-ui/src/shared/api/httpClient.ts) | The API does not yet host the built UI. |
| Development uses a loopback API target | [`vite.config.js`](../../apps/control-desk-ui/vite.config.js) and [`launchSettings.json`](../../src/SafarSuite.ControlDesk.Api/Properties/launchSettings.json) | There is no fixed production loopback binding or listener proof. |
| Real PostgreSQL persistence and EF migrations exist | [`ControlDeskServiceRegistration.cs`](../../src/SafarSuite.ControlDesk.Api/Composition/ControlDeskServiceRegistration.cs) and the Infrastructure migrations folder | Production defaults to `InMemory`; no Windows PostgreSQL lifecycle or installer migration runner exists. |
| Authentication and scoped authorization fail closed | [`ControlDeskAuthServiceCollectionExtensions.cs`](../../src/SafarSuite.ControlDesk.Api/Modules/Auth/ControlDeskAuthServiceCollectionExtensions.cs) and API authorization tests | Production operator bootstrap, credential custody, and recovery are not packaged. |
| Connected accounting/cloud/runtime proof exists | [`connected-acceptance-chain-proof-2026-07-17.md`](connected-acceptance-chain-proof-2026-07-17.md) | The proof does not exercise an installed Windows office package. |
| Linux CI builds and tests the source | [CI workflow](../../.github/workflows/ci.yml) | CI produces no Windows artifact and has no installed-package gate. |
| Tauri is intentionally deferred | [`desktop/tauri`](../../desktop/tauri) contains only a placeholder | There is no desktop shell or installer; Tauri must not become the first lifecycle experiment. |
| Full-stack Compose can prove integration | [`deploy/staging`](../../deploy/staging) | It is a Disposable Integration Lab, not an office deployment package. |

Additional operational gaps that this milestone must close:

- `/health` reports process liveness but does not prove PostgreSQL readiness or migration compatibility;
- API logging is console/debug only;
- cloud outbox publication requires an operator/API action instead of an automatic background worker;
- there is no `pg_dump`/`pg_restore`, schedule, retention, checksum, protected off-PC copy, or replacement-PC restore proof;
- there is no signed updater, pre-update backup, migration-failure recovery, application rollback, or uninstall/reinstall proof.

## First-Proof Engineering Decisions

These are the implementation defaults for the first physical proof. Changing one requires updating this plan and the canonical contract before code silently follows another topology.

| ID | First-proof decision | Reason |
| --- | --- | --- |
| `OFFICE-DEC-001` | Use a 64-bit Windows 11 reference PC. | It is the current supported office desktop target and provides the service, security, and recovery surface needed for the proof. |
| `OFFICE-DEC-002` | Use native PostgreSQL 17 as a Windows service. Do not require Docker Desktop, WSL, or a Linux VM. | The repository already targets PostgreSQL 17 in development, while Docker/WSL would reintroduce an unnecessary Linux runtime dependency. |
| `OFFICE-DEC-003` | Publish the .NET 10 API self-contained for `win-x64` and run it as a Windows service under a restricted local identity. | A clean office PC must not require the .NET SDK or an operator-managed console process. |
| `OFFICE-DEC-004` | Bind the API to `127.0.0.1:5188` and PostgreSQL to loopback only. | The office backend is local software, not a LAN or public service. |
| `OFFICE-DEC-005` | Build the React UI into the API package and serve UI and `/api` from the same loopback origin. | Existing relative API calls work without CORS, Nginx, or a second web server. |
| `OFFICE-DEC-006` | The first desktop entry is a Start-menu/desktop launcher for the local UI. Tauri remains deferred until the installed lifecycle passes. | This proves the service and recovery boundary before adding a native shell. |
| `OFFICE-DEC-007` | Apply schema changes through a versioned, one-shot migration artifact during install/update, never through ad-hoc SDK commands or normal API startup. | Migration failure must stop promotion and leave recoverable evidence. |
| `OFFICE-DEC-008` | Store mutable data, configuration, logs, backups, and recovery material under a restricted `%ProgramData%\SafarSuite\ControlDesk` hierarchy, not inside application binaries. | Updates and reinstalls must not destroy office data. |
| `OFFICE-DEC-009` | Generate independent local secrets during installation and protect them with Windows ACLs plus Windows-protected storage where applicable. | Development placeholders and ordinary text configuration are not acceptable for office use. |

Initial engineering validation target, not yet a marketed minimum:

- 64-bit Windows 11;
- 4 CPU cores;
- 16 GB RAM;
- SSD storage with at least 100 GB free before installation;
- one configured off-PC backup destination.

The final minimum is accepted only after measuring the installed product and restore exercise on the actual reference PC.

## Work Packages And Gates

Work packages are sequential unless explicitly marked parallel. Every pull request must name the requirement and work-package IDs it satisfies.

### `OFFICE-P0-01` — Local Combined Host

Requirements: `TOP-001` through `TOP-005`, `CD-101`, `CD-103`, `CD-108`, `OPEN-001`, `OPEN-007`.

Deliverables:

- fail closed outside Development unless `Persistence:Provider=Postgres` and a non-development connection is supplied;
- build the React UI reproducibly and place its output in the release API package;
- serve the UI at `/`, keep API routes under `/api`, and add SPA fallback without intercepting API or health routes;
- publish a self-contained `win-x64` single-folder artifact;
- add a Windows CI job that builds and launches the published artifact without Node.js or the .NET SDK at runtime;
- record measured artifact size and startup time.

Gate evidence:

- `http://127.0.0.1:5188/` opens Control Desk from the published folder;
- authenticated UI calls reach the same-origin API;
- the process refuses `InMemory` and development credentials outside Development;
- no listener uses `0.0.0.0`, the LAN address, or a public interface.

### `OFFICE-P0-02` — Readiness, Diagnostics, And Automatic Outbox

Requirements: `CD-104`, `CD-105`, `CD-106`, `CD-109`.

Deliverables:

- separate liveness from readiness;
- readiness verifies PostgreSQL connectivity and exact migration compatibility without leaking connection details;
- add retained rolling logs suitable for an unattended Windows service;
- add an idempotent background outbox publisher with bounded retries, cancellation, and safe cloud-outage behavior;
- expose an authorized diagnostics summary for service, database, outbox, and cloud reachability.

Gate evidence:

- liveness remains responsive while a simulated database outage makes readiness fail;
- database recovery restores readiness without data loss;
- queued cloud messages publish automatically after reconnection and do not duplicate accepted events;
- controlled stop and abrupt process termination leave actionable evidence and recover safely.

### `OFFICE-P0-03` — Native PostgreSQL Lifecycle

Requirements: `TOP-002`, `TOP-003`, `TOP-007`, `CD-101`, `CD-102`, `CD-109`, `OPEN-002`.

Deliverables:

- installer-managed native PostgreSQL 17 service with a restricted service identity and data-directory ACLs;
- loopback-only `listen_addresses`, restricted `pg_hba.conf`, a random application role password, and no shared cloud/client database;
- idempotent database/role creation and required `pg_trgm` extension setup;
- versioned EF migration bundle with target verification, lock, preflight, audit output, and failure stop;
- repair flow that distinguishes missing service, stopped service, corrupt configuration, unavailable database, and migration mismatch;
- API service dependency/retry behavior for PostgreSQL startup.

Gate evidence:

- installation and rerun are idempotent;
- both services start automatically after reboot;
- `Get-NetTCPConnection` proves database and API listeners are loopback-only;
- all 32 current Control Desk migrations and `pg_trgm` are verified;
- a deliberately failed migration does not start the new application version.

### `OFFICE-P0-04` — Operator Bootstrap And Secret Custody

Requirements: `CD-201`, `CD-202`, `CD-203`, `CD-208`.

Deliverables:

- no built-in development operator or placeholder secret outside Development;
- interactive, no-echo first-operator provisioning using the existing PBKDF2 format;
- protected storage and ACL rules for session, database, publisher, and provider-access material;
- explicit backup/recovery treatment for protected configuration in addition to PostgreSQL;
- operator/password recovery ceremony with actor and reason evidence;
- redaction tests for logs and diagnostics.

Gate evidence:

- anonymous access returns `401`, wrong scope returns `403`, and the provisioned operator succeeds;
- a normal Windows account cannot read protected service secrets;
- reinstall preserves authorized operators through the documented recovery path;
- log and diagnostics scans contain no secret values.

### `OFFICE-P0-05` — Windows Service And Operator Entry

Requirements: `CD-101` through `CD-104`, `CD-109`, `OPEN-001`.

Deliverables:

- install the API as an automatic delayed-start Windows service under a restricted identity;
- configure service dependency/retry/recovery and controlled shutdown;
- create a Start-menu entry and optional desktop shortcut that checks readiness and opens the local UI;
- provide authorized health/repair guidance without asking the operator to use PostgreSQL or service tools;
- implement uninstall that removes binaries/services/shortcuts but preserves office data unless a separately confirmed purge is requested.

Gate evidence:

- a normal operator signs in after a cold reboot using only the installed entry point;
- no terminal, Docker, Node.js, .NET SDK, DNS, SMTP, or Linux machine is required;
- service termination triggers the accepted recovery behavior;
- uninstall/reinstall preserves and reopens the same office database.

### `OFFICE-P0-06` — Backup And Replacement-PC Restore

Requirements: `CD-204`, `CD-205`, `CD-208`, `CD-209`, `OPEN-003`, `OPEN-008`.

Recommended first acceptance target:

- recovery-point objective: no more than four hours of committed office work;
- recovery-time objective: restore a replacement PC to verified operation within four hours;
- full-format PostgreSQL backup every four hours while the PC is available, plus a mandatory pre-update backup;
- protected off-PC copy for every successful scheduled backup;
- retention recommendation: fourteen rolling local backups and thirty days off-PC, adjusted after measured storage growth.

Deliverables:

- scheduled `pg_dump` custom-format backup with checksum, timestamp, version manifest, and protected configuration/recovery bundle;
- atomic copy to the configured off-PC destination and visible failure state;
- retention pruning that never deletes the last verified good backup;
- restore tooling that targets an empty replacement database, verifies migration/schema/version state, and requires explicit destructive confirmation for a non-empty target;
- automated restore validation against a disposable database;
- physical clean replacement-PC rehearsal with retained timings and IDs.

Gate evidence:

- a backup can be restored without the original PC;
- restored client, contract, invoice, payment, accounting, entitlement, outbox, operator, and audit records reconcile;
- RPO/RTO measurements meet the accepted targets;
- losing the off-PC destination raises an operator-visible failure without stopping normal office work.

### `OFFICE-P0-07` — Signed Update And Rollback

Requirements: `CD-206`, `CD-209`, `OPEN-004`.

Deliverables:

- versioned and signed Windows artifact plus checksum/manifest verification;
- compatibility preflight, disk-space check, service drain, mandatory backup, and migration preview;
- atomic application-binary replacement;
- post-update readiness and primary-workflow smoke;
- automatic binary rollback when startup/readiness fails;
- database recovery path from the pre-update backup when schema change cannot safely roll back;
- retained update and rollback evidence.

Gate evidence:

- tampered or unsigned artifacts fail closed;
- simulated migration and startup failures return the PC to the last verified version/data state;
- successful update preserves data, operators, configuration, and queued outbox messages.

### `OFFICE-P0-08` — Physical Clean-PC Acceptance

Requirements: complete Control Desk Office Gate and Connected Platform Gate in the canonical contract.

The test PC must begin without this repository, Node.js, the .NET SDK, Docker Desktop, WSL, PostgreSQL, or retained SafarSuite Control Desk configuration.

Acceptance run:

1. Install through the operator-facing package.
2. Provision the first authorized operator without exposing the password.
3. Reboot and confirm PostgreSQL/API automatic startup.
4. Audit listeners and firewall exposure.
5. Complete login and the primary client-to-entitlement workflow.
6. Power off or disconnect the Linux/cloud test host and continue core office reads/writes.
7. Queue cloud-bound work, reconnect, and prove automatic exactly-once acceptance/reconciliation.
8. Force-stop API and database processes separately and prove recovery/evidence.
9. Run scheduled and manual backups.
10. Restore onto a second clean/replacement Windows environment and reconcile retained IDs.
11. Exercise a good update, tampered update, failed migration, and rollback.
12. Uninstall/reinstall without purging data and reopen the restored office state.

The retained evidence package must contain versions, timestamps, service states, redacted configuration fingerprints, listener output, migration ledger, backup checksums, restore timings, connected-chain IDs, reconciliation result, and failure-drill outcomes. It must never contain passwords, tokens, keys, or raw connection strings.

## Parallel Drift-Prevention Work

### `CLOUD-P1-01` — Cloud-Only Staging Default

This work may proceed in parallel after `OFFICE-P0-01` is stable. It does not block the local combined-host proof.

- make `deploy/staging/docker-compose.yml` contain only Caddy, Control Cloud API, and Control Cloud PostgreSQL by default;
- publish only the Control Cloud hostname in the default Caddy file;
- move Control Desk API/UI/PostgreSQL into an explicitly named `docker-compose.disposable-integration-lab.yml` overlay and isolated Compose project;
- give cloud and lab preflight separate explicit targets, environment examples, secrets, volumes, and commands;
- make CI validate cloud-only staging by default and the disposable lab separately;
- never copy real office data into the lab.

## Required Implementation Order

```text
Requirements baseline
  -> OFFICE-P0-01 local combined host
  -> OFFICE-P0-02 readiness and automatic outbox
  -> OFFICE-P0-03 native PostgreSQL lifecycle
  -> OFFICE-P0-04 operator/secrets bootstrap
  -> OFFICE-P0-05 Windows service and operator entry
  -> OFFICE-P0-06 backup and replacement-PC restore
  -> OFFICE-P0-07 signed update and rollback
  -> OFFICE-P0-08 physical clean-PC acceptance
  -> optional Tauri shell after the service boundary is proven
```

`CLOUD-P1-01` is a separate cloud-lane correction and must not delay or redefine the office critical path.

## Implementation Checkpoint — `OFFICE-P0-01` Local Proof

Local implementation passed on 2026-07-18:

- the API serves the built React UI at `/` only when a packaged `wwwroot/index.html` is present;
- UI and API share `http://127.0.0.1:5188`, while unmatched `/api` requests remain JSON/API `404` boundaries rather than falling into the SPA;
- Production configuration selects PostgreSQL and a fixed loopback Kestrel endpoint;
- host validation rejects Production `InMemory`, missing PostgreSQL connections, the checked-in development password, and placeholder connection markers;
- a repeatable PowerShell builder emits a self-contained `win-x64` package plus a redacted source/size manifest;
- a package smoke rejects prohibited Production configurations, launches the actual packaged executable, loads the UI, proves anonymous `401`, performs operator login plus bearer-authenticated business API access, proves unknown API `404`, audits loopback listeners, and writes redacted evidence;
- CI now contains a Windows package job that builds, smokes, and uploads the pilot artifact.

Local evidence:

- self-contained payload: `128,122,969` bytes;
- measured health startup: `1,293 ms`;
- package checks: Production `InMemory` rejected, development PostgreSQL connection rejected, UI `200`, anonymous business API `401`, authenticated business API `200`, unknown API `404`, listener `127.0.0.1` only;
- solution tests: `84/84` passed (`17` domain, `30` cloud identity, `22` staging preflight, `15` Control Desk API);
- UI production build passed; the existing approximately `988 KB` minified JavaScript bundle warning remains a later optimization;
- accounting smoke, LocalServer entitlement/security smoke, Control Desk migration parity, PowerShell syntax, and `git diff --check` passed;
- independent review found two configuration/evidence gaps; both were fixed and the re-review reported no residual blocker.

`OFFICE-P0-01` remains open until the new `office-windows-package-gate` passes from the exact clean pushed commit. The ignored local package is engineering evidence, not an office installer.

## Status Tracker

| Work package | Status | Completion evidence |
| --- | --- | --- |
| Requirements baseline | Complete | Commit `0d38151`; canonical contract and active-doc alignment. |
| Repository packaging audit | Complete | API/UI/PostgreSQL/staging audit summarized in this plan. |
| `OFFICE-P0-01` Local Combined Host | Local proof passed; remote CI pending | Self-contained package, startup/HTTP/auth/listener evidence, and Windows CI job implemented. |
| `OFFICE-P0-02` Readiness/Automatic Outbox | Pending | Pending. |
| `OFFICE-P0-03` Native PostgreSQL Lifecycle | Pending | Pending. |
| `OFFICE-P0-04` Operator/Secret Custody | Pending | Pending. |
| `OFFICE-P0-05` Windows Service/Entry | Pending | Pending. |
| `OFFICE-P0-06` Backup/Restore | Pending | Pending. |
| `OFFICE-P0-07` Update/Rollback | Pending | Pending. |
| `OFFICE-P0-08` Physical Acceptance | Pending | Pending. |
| `CLOUD-P1-01` Cloud-Only Staging | Pending, parallel | Pending. |
| Tauri shell | Deferred | Begins only after `OFFICE-P0-08`. |

## Scope Guard

Until `OFFICE-P0-08` passes:

- do not deploy real office data;
- do not resume Linux-hosted Control Desk work;
- do not treat a developer browser session or Docker Compose run as deployment acceptance;
- do not add Tauri business logic;
- do not configure DNS, HTTPS, SMTP, or Brevo as part of the office package;
- do not close a work package without executable or physical evidence.

The immediate release action is to push the clean implementation commit and confirm `office-windows-package-gate`. After that gate is green, mark `OFFICE-P0-01` complete and begin `OFFICE-P0-02`.
