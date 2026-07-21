# SafarSuite Control Desk Native Setup Acceptance v1

Status: prepared for `SVC05-11B`

This rehearsal is disposable Windows-host proof for the one-PC topology. It is not a production deployment and must not use a customer database, existing Control Desk installation, or non-disposable machine.

## Preconditions

- A clean, elevated Windows host or disposable GitHub Actions Windows runner.
- A sealed `office-windows-native-postgresql-v2` package and its SHA-256 checksum.
- The pinned PostgreSQL and VC++ inputs already validated by the package gate.
- A GUID-owned test root under the runner temporary directory; no manually chosen broad path.
- No Linux, Docker, DNS, HTTPS, SMTP, or public listener is involved.

## Acceptance sequence

1. Record host identity, boot ID, Windows version, package revision, package SHA-256, and the GUID test root.
2. Invoke `Install-OfficeControlDesk.ps1` elevated with the package, Program Files, and ProgramData roots.
3. Retain each setup checkpoint: `DatabaseReady`, `OperatorReady`, `ApiPayloadInstalled`, `ApiRegistered`, `ServicesConfigured`, `ShortcutsInstalled`, and `Ready`.
4. Verify both owned services, their service SIDs, dependency, delayed-start/recovery policy, and stopped/running state.
5. Verify exact migrations, PostgreSQL cluster identity, and the protected application passfile and machine-secret ACLs.
6. Verify `GET http://127.0.0.1:5188/ready` returns database `Ready`; verify no listener exists outside loopback.
7. Verify the owned Start-menu shortcut launches only `Start-OfficeControlDesk.ps1` and opens only `http://127.0.0.1:5188`.
8. Capture redacted lifecycle logs, setup receipts, service receipts, readiness response, listener inventory, and shortcut target.

## Interruption and recovery matrix

Repeat the setup with a controlled interruption after each checkpoint. The recovery run must resume or repair only owned state, stop an API service that cannot reach readiness, and preserve the PostgreSQL cluster, operator rows, machine-secret envelope, passfiles, receipts, and logs. Foreign service or receipt state must fail closed.

## Uninstall and reinstall

Run the normal uninstall. Confirm the API service, API binaries, launcher, and owned shortcuts are removed while the PostgreSQL service data, operator identity, machine-secret envelope, passfiles, receipts, and logs remain. Run setup again and prove the same cluster system identifier and operator ID reopen without password or secret replacement.

## Evidence and cleanup

The retained evidence must include the package hash, source revision, host/boot identifiers, every checkpoint, service configuration, readiness response, listener inventory, ACL summaries, interruption outcomes, and reinstall identity comparison. Cleanup may remove only the GUID-owned disposable root and services whose receipts point inside it. A missing receipt, path mismatch, foreign service, or ambiguous cleanup target is a hard failure.
