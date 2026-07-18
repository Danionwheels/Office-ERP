# SafarSuite Control Desk Windows Office Package

This folder implements `OFFICE-P0-01` from the [one-PC deployment plan](../../docs/planning/one-pc-control-desk-deployment-plan-2026-07-18.md).

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
