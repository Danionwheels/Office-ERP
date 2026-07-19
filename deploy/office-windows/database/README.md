# SafarSuite Control Desk Native Database Lifecycle

This directory owns the `OFFICE-P0-03` engineering implementation for PostgreSQL on the same dedicated Windows office PC as SafarSuite Control Desk. It does not install or contact a Linux machine, cloud host, domain, DNS provider, SMTP provider, or Docker daemon.

## Managed topology

| Resource | Product-owned value |
| --- | --- |
| PostgreSQL | 17.10 x64, trimmed from the reviewed EnterpriseDB Windows binary archive |
| Windows service | `SafarSuiteControlDeskPostgreSQL` |
| Service identity | `NT SERVICE\SafarSuiteControlDeskPostgreSQL` virtual account |
| Listener | `127.0.0.1:54329` only |
| Database | `safarsuite_control_desk` |
| Roles | separate admin, migrator, and least-privilege application logins |
| Authentication | SCRAM-SHA-256 with an explicit reject-by-default HBA |
| Migration target | exact ordered 32-migration ledger ending at `20260713220254_AddPortalPaymentBoundary` |
| Required extension | `pg_trgm`, created by the reviewed EF migration |

The package also carries the reviewed Microsoft Visual C++ v14 x64 redistributable because the PostgreSQL binaries import `VCRUNTIME140.dll`. Both vendor inputs are checked against pinned SHA-256 values; the VC++ installer must additionally have a valid Microsoft Authenticode signature.

## Installed paths

Application/runtime files are versioned below `%ProgramFiles%\SafarSuite\ControlDesk\Database`. Durable state remains below `%ProgramData%\SafarSuite\ControlDesk`:

- `Database\PostgreSQL17\Data` — database cluster;
- `Logs\PostgreSQL` — PostgreSQL logs;
- `Logs\DatabaseLifecycle` — redacted JSONL lifecycle evidence;
- `Secrets\Database` — generated role passfiles;
- `State\Database` — ownership marker and activation receipt.

The PostgreSQL virtual service SID receives read/execute on the runtime and modify access on database data/logs. Database passfiles and lifecycle state are restricted to LocalSystem and local Administrators in this work package; the application-service grant belongs to `OFFICE-P0-04/05` and must never broaden access to ordinary Windows users.

## Safety and convergence rules

Runtime extraction and `initdb` never write an unowned partial installation directly into the final paths. The database manifest binds the normalized relative path and SHA-256 of every file in the trimmed PostgreSQL runtime, not only the launch executables. Installed verification rejects missing, altered, case-colliding, reparse, or unexpected runtime entries; the only generated addition allowed in that tree is the protected root lifecycle receipt. Any ordinary runtime drift forces repair to restage the reviewed archive. Nonce-bound runtime and cluster receipts bind the exact staging/final paths, package hash, and live PostgreSQL system identifier. A rerun discards only receipt-owned partial staging or completes a verified promotion. Passfiles, state, and activation receipts use same-directory atomic replacement.

Install accepts an absent database, an explicitly resumable initialization, a stopped owned service, a missing owned service with a valid preserved cluster, or an exact known migration prefix. A rerun retains the cluster identifier, generated credentials, and records. The lifecycle lock is acquired before package state, credentials, or host state are read.

Repair reports and treats these states separately:

- `MissingPrerequisite` — install/verify the pinned VC++ runtime;
- `MissingService` — restage the reviewed runtime and re-register the existing marked cluster;
- `StoppedService` — start and verify;
- `CorruptConfiguration` — preserve the prior managed files, restore the exact templates, restart, and verify;
- `UnavailableDatabase` — bounded restart and readiness check;
- `MigrationMismatch` — advance only an exact ordered prefix through the reviewed bundle.

Additional repair classifications cover exact filesystem-permission drift, Windows-service identity/recovery-policy drift, interrupted initialization, PostgreSQL role/ownership/grant drift, and a missing required extension.

The lifecycle fails closed without adoption, downgrade, reinitialization, service mutation, or deletion for foreign/unmarked clusters, live cluster/receipt mismatches, foreign services, unsafe/reparse paths, port collisions, missing credentials, unsupported cluster versions, and unknown/reordered/ahead migration histories.

The migration bundle receives its credential through an ACL-protected PostgreSQL passfile referenced by a process-only environment connection string. Passwords are never command-line arguments or audit fields. A global Windows mutex serializes local lifecycle calls, while Entity Framework supplies the database migration lock. Automatic service activation and the package activation receipt occur only after exact migration, extension, live configuration, service, ACL, role, ownership, grant, listener, and cluster-identity verification.

The Windows proof injects interruptions after runtime extraction, cluster initialization, cluster promotion, service startup, and service activation. It also mutation-tests live cluster identity, service recovery settings, filesystem permissions, PostgreSQL security drift, exact-prefix repair, divergent-history refusal, unavailable authentication, concurrent lifecycle calls, and a deliberately conflicting real EF migration. The hermetic proof additionally tampers with a non-required runtime dependency and adds an unexpected runtime file to prove the complete inventory fails closed. The uploaded release candidate is an immutable ZIP with a retained SHA-256; smoke evidence is written outside that archive.

## Uninstall and remaining proof

Ordinary uninstall removes only the verified product-owned PostgreSQL service and versioned runtime. It preserves `%ProgramData%` so reinstall opens the same cluster. Purge is intentionally unavailable from the operator lifecycle; disposable CI cleanup has a separate GitHub-only, GUID-rooted, marker-checked script.

The API executable is Windows-Service-aware and the package declares the exact API-to-PostgreSQL dependency, but `OFFICE-P0-05` still owns creation of the real restricted API service. Consequently, a GitHub runner cannot close the physical reboot criterion. `OFFICE-P0-03` remains in progress until the persistent clean reference PC proves a newer boot, both real services starting automatically, API readiness, exact migrations, and loopback-only listeners.
