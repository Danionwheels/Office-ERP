# SafarSuite Control Desk V1 Office Installation Manifest

Status: accepted for `SVC05-01` / `OPEN-001`  
Topology: one dedicated Windows office PC

## Installer decision

V1 uses a signed, self-contained office package launched by an elevated Windows bootstrapper. The bootstrapper invokes the reviewed PowerShell 5.1 lifecycle scripts and is idempotent. A GUI installer is not a V1 prerequisite.

The bootstrapper owns preflight, Visual C++ prerequisite verification, PostgreSQL runtime/service installation, database initialization and migrations, machine-secret preparation, first-operator provisioning, API service registration, readiness verification, and launcher registration. It must stop on any failed prerequisite and must not silently adopt foreign services, clusters, paths, or secrets.

## Fixed local manifest

| Component | V1 location/identity | Boundary |
| --- | --- | --- |
| Desktop launcher | `%ProgramFiles%\SafarSuite\ControlDesk\Launcher` | Starts the loopback API/UI only |
| Control Desk API | `%ProgramFiles%\SafarSuite\ControlDesk\Api` | Windows service `SafarSuiteControlDeskApi`; loopback listener only |
| PostgreSQL runtime | `%ProgramFiles%\SafarSuite\ControlDesk\Database` | Windows service `SafarSuiteControlDeskPostgreSQL`; pinned PostgreSQL 17 runtime |
| PostgreSQL data | `%ProgramData%\SafarSuite\ControlDesk\Database\PostgreSQL17\Data` | Durable office data; preserved by default uninstall |
| Lifecycle receipts | `%ProgramData%\SafarSuite\ControlDesk\Database` | Owned state, manifests, migration and cluster identity receipts |
| Machine secrets | `%ProgramData%\SafarSuite\ControlDesk\Secrets\Machine\control-desk-machine-secrets.v1.json` | SYSTEM-owned protected envelope; no configuration fallback |
| Logs | `%ProgramData%\SafarSuite\ControlDesk\Logs` | ACL-protected bounded JSON logs; secret-free |
| API endpoint | `http://127.0.0.1:5188` | No public bind, DNS, HTTPS, SMTP, Linux, Docker, or cloud prerequisite |

## Installation order

1. Validate Windows/hardware/free-space and elevation.
2. Verify signed package and pinned vendor inputs.
3. Stage owned runtime paths and converge ACLs.
4. Install/configure PostgreSQL and verify loopback-only listener.
5. Apply migrations and verify exact migration history.
6. Prepare machine-secret ACLs and envelope.
7. Run the no-echo first-operator bootstrap.
8. Register the API service with the PostgreSQL dependency, then verify `/health` and `/ready`.
9. Register the launcher and emit secret-free installation evidence.

## Preservation rules

Normal uninstall removes owned services and versioned binaries but preserves the PostgreSQL cluster, generated credentials, machine secrets, operator records, receipts, and logs. Reinstall must verify ownership and identity before reusing preserved state. Destructive purge is a separate, explicitly authorized operation and is not part of the normal installer path.

This manifest does not authorize Control Cloud hosting or public ingress; those remain separate system boundaries.
