# SafarSuite Control Desk API Service Contract V1

Status: accepted for `SVC05-02`  
Service: `SafarSuiteControlDeskApi`

## Identity and command

- Service account: `NT SERVICE\SafarSuiteControlDeskApi` (virtual service account; no stored password).
- Executable: `%ProgramFiles%\SafarSuite\ControlDesk\Api\SafarSuite.ControlDesk.Api.exe`.
- Configuration root: `%ProgramData%\SafarSuite\ControlDesk\Config`.
- Log root: `%ProgramData%\SafarSuite\ControlDesk\Logs`.
- Machine-secret root: `%ProgramData%\SafarSuite\ControlDesk\Secrets\Machine`.
- Listener: `http://127.0.0.1:5188` only.
- Dependency: `SafarSuiteControlDeskPostgreSQL`.
- Startup: automatic after the database dependency; recovery actions restart the service after transient failure and leave it stopped on failed readiness.

The service receives no password-bearing command-line arguments. PostgreSQL credentials and the session signing key are loaded only from protected local stores.

## Ownership and ACL contract

The installer writes an ownership receipt containing the product, service name, executable path, package hash, service SID, dependency, and creation revision. Every repair or reinstall must verify the receipt and normalized paths before mutating anything.

- Program Files binaries: SYSTEM and Administrators full control; service SID read/execute.
- ProgramData configuration and logs: SYSTEM and Administrators full control; service SID read/write only where runtime operation requires it.
- Machine-secret envelope: exact machine-secret ACL contract; service SID read only.
- Ordinary users receive no access to private configuration, logs, database credentials, or machine-secret material.

## Collision and preservation rules

Installation fails closed when the named service exists with a foreign executable, account, dependency, startup policy, or ownership receipt. It also refuses reparse paths, path collisions, foreign receipts, and package-hash mismatches. It never adopts or overwrites foreign state.

An owned stopped service may be repaired in place. Reinstall may reuse only an owned receipt and matching durable state. Normal uninstall removes the owned API service and versioned binaries but preserves the database, operators, machine secrets, receipts, and logs.
