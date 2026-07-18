# SafarSuite Control Desk Windows Office Package

This folder implements `OFFICE-P0-01` and carries the Windows package proof for `OFFICE-P0-02` from the [one-PC deployment plan](../../docs/planning/one-pc-control-desk-deployment-plan-2026-07-18.md).

The current output is an engineering pilot package, not an office installer. It proves that the React UI and self-contained .NET API can run together from one folder on Windows, on one loopback origin, without Node.js or the .NET SDK at runtime.

Build into a new or empty output directory:

```powershell
./deploy/office-windows/Build-OfficePackage.ps1 -OutputDirectory ./.codex-run/office-windows/package
```

Smoke the package:

```powershell
./deploy/office-windows/Test-OfficePackage.ps1 -PackageDirectory ./.codex-run/office-windows/package
```

The smoke deliberately runs with Development in-memory persistence. Production configuration fails closed unless PostgreSQL is selected and provisioned. Native PostgreSQL installation, Windows service registration, operator bootstrap, backup/restore, update/rollback, and the operator-facing installer belong to later work packages and must pass before real office data is used.

The packaged host now separates process liveness (`/health` or `/health/live`) from durable readiness (`/ready` or `/health/ready`). Readiness is successful only when PostgreSQL is reachable and its applied migration history exactly matches the packaged application. The authorized `GET /api/v1/diagnostics/summary` surface reports sanitized service, database, outbox, automation, and Control Cloud states without returning connection details, payloads, signatures, or failure text.

Production enables bounded rolling JSON logs under `%ProgramData%\SafarSuite\ControlDesk\Logs` by default. The installer/service work may set `ControlDesk__Logging__File__DirectoryPath` to an ACL-protected absolute directory. The package smoke overrides this path, verifies retained evidence survives an abrupt process stop, restarts the same package to `Ready`, and scans diagnostics/log output for the test credentials and bearer token. Diagnostics reports the package informational version with its source revision for support correlation.

Production also enables the automatic outbox worker. It processes one bounded batch at a time, shares an in-process publication gate with manual endpoints, and holds a PostgreSQL advisory lease so overlapping host processes cannot race the same rows. `MaximumAttemptCount=0` means transient retries do not expire during a long cloud outage; message-specific invalid payloads remain terminal. Global authentication failures remain retryable, while incomplete endpoint/signing configuration pauses publication before dequeueing.

Outside Development and Testing, the publisher requires an HTTPS endpoint plus non-development source environment, key id, and signing secret values. An option override cannot weaken that HTTPS boundary. Stable message IDs remain the cloud idempotency boundary when an acceptance must be replayed after a local acknowledgement loss.
