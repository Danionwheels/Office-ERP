# SafarSuite Control Desk Windows Office Package

This folder implements `OFFICE-P0-01` through the engineering portion of `OFFICE-P0-03` from the [one-PC deployment plan](../../docs/planning/one-pc-control-desk-deployment-plan-2026-07-18.md).

The current output is an engineering package, not yet the operator-facing office installer. It contains the React UI, self-contained .NET API, hash-pinned PostgreSQL 17.10 runtime, Microsoft-signed VC++ prerequisite, self-contained EF migration bundle, and native database lifecycle tools. Runtime operation needs neither Node.js, the .NET SDK, Docker, Linux, DNS, HTTPS, nor SMTP.

Acquire the two vendor files named and hash-pinned in `database/postgresql-distribution.json`, then build into a new or empty output directory:

```powershell
./deploy/office-windows/Build-OfficePackage.ps1 `
  -OutputDirectory ./.codex-run/office-windows/package `
  -PostgresDistributionArchivePath C:/verified-vendor-cache/postgresql-17.10-1-windows-x64-binaries.zip `
  -VisualCppRedistributablePath C:/verified-vendor-cache/vc_redist.x64.exe
```

Smoke the package:

```powershell
./deploy/office-windows/Test-OfficePackage.ps1 -PackageDirectory ./.codex-run/office-windows/package
```

The package smoke deliberately runs the API with Development in-memory persistence so it remains non-elevated and independent of the native database proof. The separate lifecycle gates test package integrity, state-machine behavior under Windows PowerShell 5.1, and a real PostgreSQL Windows service on a disposable GitHub runner. The hosted runner proves automatic start configuration, not a physical reboot.

On an elevated disposable/reference Windows PC, the packaged setup entry is:

```powershell
./Install-OfficeControlDesk.ps1 `
  -PackageDirectory . `
  -ProgramFilesRoot 'C:/Program Files' `
  -ProgramDataRoot 'C:/ProgramData'
```

It installs or verifies the owned PostgreSQL cluster, generates non-secret production settings that reference the protected application passfile, creates or loads the DPAPI machine-secret envelope, and invokes the packaged no-echo first-operator bootstrap. A rerun refuses to replace an existing operator or machine secret. It stops at the explicit `OperatorReady` checkpoint; API payload installation and service activation remain the next setup phase.

The lower-level database lifecycle entry points remain available for repair and diagnostics:

```powershell
./database/Install-OfficeDatabase.ps1
./database/Repair-OfficeDatabase.ps1
./database/Uninstall-OfficeDatabase.ps1
```

Default uninstall removes the owned PostgreSQL service and versioned runtime but preserves office data, generated database credentials, cluster identity, and lifecycle state for reinstall. There is deliberately no ordinary purge switch. See [the database lifecycle contract](database/README.md).

Production remains blocked on `OFFICE-P0-04` through `OFFICE-P0-08`: first-operator secret custody, the real API Windows service/operator entry, backup and replacement-PC restore, signed update/rollback, and physical clean-PC acceptance.

The packaged host now separates process liveness (`/health` or `/health/live`) from durable readiness (`/ready` or `/health/ready`). Readiness is successful only when PostgreSQL is reachable and its applied migration history exactly matches the packaged application. The authorized `GET /api/v1/diagnostics/summary` surface reports sanitized service, database, outbox, automation, and Control Cloud states without returning connection details, payloads, signatures, or failure text.

Production enables bounded rolling JSON logs under `%ProgramData%\SafarSuite\ControlDesk\Logs` by default. The installer/service work may set `ControlDesk__Logging__File__DirectoryPath` to an ACL-protected absolute directory. The package smoke overrides this path, verifies retained evidence survives an abrupt process stop, restarts the same package to `Ready`, and scans diagnostics/log output for the test credentials and bearer token. Diagnostics reports the package informational version with its source revision for support correlation.

Production also enables the automatic outbox worker. It processes one bounded batch at a time, shares an in-process publication gate with manual endpoints, and holds a PostgreSQL advisory lease so overlapping host processes cannot race the same rows. `MaximumAttemptCount=0` means transient retries do not expire during a long cloud outage; message-specific invalid payloads remain terminal. Global authentication failures remain retryable, while incomplete endpoint/signing configuration pauses publication before dequeueing.

Outside Development and Testing, the publisher requires an HTTPS endpoint plus non-development source environment, key id, and signing secret values. An option override cannot weaken that HTTPS boundary. Stable message IDs remain the cloud idempotency boundary when an acceptance must be replayed after a local acknowledgement loss.
