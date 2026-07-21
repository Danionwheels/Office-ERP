# Source Stabilization Map - 2026-07-07

This map is for turning the current large worktree into reviewable checkpoints. It intentionally ignores generated proof/build output under `artifacts/codex`, nested `artifacts/codex`, `.codex-run`, `dist`, `bin`, and `obj`.

## Current Source Lanes

| Lane | Suggested checkpoint | Main paths | Notes |
| --- | --- | --- | --- |
| Repo hygiene and verification | `stabilize local verification and generated outputs` | `.gitignore`, `.dockerignore`, `README.md`, `tools/Verify-Local.ps1`, removal of `apps/control-desk-ui/tsconfig.tsbuildinfo` | Keep this first so every later slice has a repeatable verification command and less status noise. |
| Accounting backend | `complete GL setup, opening balances, and accounting close foundation` | `src/SafarSuite.ControlDesk.Application/Modules/Accounting`, `src/SafarSuite.ControlDesk.Domain/Modules/Accounting`, `src/SafarSuite.ControlDesk.Infrastructure/Persistence/*OpeningBalance*`, `src/SafarSuite.ControlDesk.Contracts/ControlDeskApi/V1/Accounting/AccountingContracts.cs`, `src/SafarSuite.ControlDesk.Api/Modules/Accounting/AccountingEndpoints.cs`, `tools/SafarSuite.ControlDesk.AccountingSmoke` | Includes COA bootstrap/import, account-code validation, opening balance profiles, stronger ledger hierarchy policy, voucher/source document metadata, and smoke coverage. |
| Accounting frontend workspace | `reshape accounting operator workspace` | `apps/control-desk-ui/src/modules/accounting`, accounting-related sections in `apps/control-desk-ui/src/styles.css` | Large UI split into work-window/shared components for COA, journals, periods, controls, reports, reconciliation, and opening balances. |
| Billing, payments, statements, and client desk UI | `reshape client commercial operator workspaces` | `apps/control-desk-ui/src/modules/billing`, `clients`, `contracts`, `entitlements`, `payments`, `statements`, related CSS | New workflow models and shared workspaces for the client commercial desk. Keep separate from accounting UI if possible because review surface is already large. |
| Control Cloud provider access and app activation | `wire provider access and app activation register controls` | `src/SafarSuite.ControlCloud.Api/Modules/ProviderAccess`, `src/SafarSuite.ControlCloud.*/*AppActivation*`, `src/SafarSuite.ControlDesk.Application/Modules/ControlCloud/*AppActivation*`, `src/SafarSuite.ControlDesk.Contracts/ControlCloud/V1/*Activation*`, `src/SafarSuite.ControlDesk.Infrastructure/ControlCloud`, `src/SafarSuite.ControlDesk.Api/Modules/ControlCloud/ControlCloudEndpoints.cs` | Covers provider-scoped activation issue issuance/list/revoke/replacement surfaces and Control Desk proxies. |
| LocalServer runtime access and revocation | `harden LocalServer runtime access and revocation sync` | `src/SafarSuite.LocalServer.*`, `src/SafarSuite.LocalServer.Api/Dockerfile`, `src/SafarSuite.LocalServer.Api/docker`, `tools/SafarSuite.LocalServer.ComposeBootstrapProof`, `tools/SafarSuite.LocalServer.EntitlementSmoke` | Covers Local API access key, HTTPS-ready runtime defaults, docker command shims, command polling, revocation ledger/status, and proof tooling. |
| Control Cloud bootstrap/deployment package | `ship HTTPS-default bootstrap package contract` | `src/SafarSuite.ControlCloud.Api/wwwroot/install/safarsuite-local-server`, `src/SafarSuite.ControlCloud.Application/Modules/LocalServer/CreateLocalServerBootstrapPackage`, related appsettings | Keep with LocalServer runtime lane if reviewing together, or split when the diff gets too dense. |
| Portal invitation/provider access adjustments | `tighten provider-gated portal invitation controls` | `src/SafarSuite.ControlCloud.Api/Modules/ClientPortal`, `src/SafarSuite.ControlCloud.Application/Modules/ClientPortal`, `src/SafarSuite.ControlDesk.Application/Modules/Clients`, `src/SafarSuite.ControlDesk.Infrastructure/ControlCloud/HttpClientPortalInvitationClient.cs` | Smaller but security-sensitive; good candidate for an isolated review after provider access lands. |
| Planning and architecture record | `record runtime/accounting stabilization evidence` | `docs/architecture`, `docs/planning`, `README.md` | Some docs belong with their code checkpoints; keep broad trackers in a docs-only checkpoint after code is stable. |

## Recommended Order

1. Repo hygiene and verification.
2. Accounting backend.
3. Accounting frontend workspace.
4. Control Cloud provider access and app activation.
5. LocalServer runtime access, revocation, and compose proof tooling.
6. Control Cloud bootstrap/deployment package contract.
7. Client commercial workspace UI.
8. Portal invitation/provider access adjustments.
9. Planning and architecture record.

## Verification Gate

Use this command after each checkpoint:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-Local.ps1
```

Expected current result: API builds pass, Control Desk UI production build passes with the known large chunk warning, accounting smoke passes, and LocalServer entitlement smoke passes.

## Open Cleanup

- The frontend bundle still warns because the main JS chunk is over 500 kB.
- PostgreSQL and Docker/Compose proof runs are not part of `tools/Verify-Local.ps1` yet.
- Existing line-ending warnings are still present; they are not whitespace errors, but a future `.gitattributes` pass would make diffs calmer.
