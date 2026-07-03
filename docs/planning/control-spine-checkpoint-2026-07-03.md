# Control Spine Checkpoint - 2026-07-03

Purpose: freeze the current project status after the support-command sweep so the next move is deliberate.

## Verification

| Check | Result | Notes |
| --- | --- | --- |
| `dotnet build --no-restore SafarSuite.ControlDesk.sln` | Passed | 0 warnings, 0 errors. The first parallel run hit a `VBCSCompiler` file lock; after `dotnet build-server shutdown`, the sequential rerun passed. |
| `dotnet run --no-restore --project tools/SafarSuite.ControlDesk.AccountingSmoke/SafarSuite.ControlDesk.AccountingSmoke.csproj` | Passed | Accounting smoke passed in memory with run id `20260702211240143`. |
| `dotnet run --no-restore --project tools/SafarSuite.LocalServer.EntitlementSmoke/SafarSuite.LocalServer.EntitlementSmoke.csproj` | Passed | Smoke result was `Passed`; cached entitlement version `102`; command processing applied `2`; command acknowledgements `2`. |
| `npm run build` in `apps/control-desk-ui` | Passed | Vite production build completed. |
| Live PostgreSQL accounting smoke with Control Cloud receiver | Passed | Published `7` cloud messages through the local Control Cloud receiver for client `8eaa8769-fb2a-406b-b6c8-e7713736e83c`. |
| Live Control Desk -> Control Cloud -> Local Server proof | Passed | Registered installation `codex-demo-20260702212826`, pulled an active entitlement, reported heartbeat, applied `request_diagnostics` and `refresh_entitlement`, uploaded diagnostics, and confirmed active module access. |

## Strong Now

- Client maintenance, contacts, support notes, lifecycle actions, and accounting-profile setup are usable in the active Control Desk surface.
- Contracts, module allowances, charge rules, invoice draft/issue, tax, void, credit note, refund, settlement, payment review/reversal, statements, and GL visibility have a working local loop.
- The local outbox and Control Cloud receiver path exists for invoice, payment, credit, refund, settlement, paid-status, and entitlement messages.
- Client Portal foundations exist for cloud-owned commercial/license views plus invitation, password-backed user, and session boundaries.
- Installation setup now has one-time setup tokens, bootstrap package generation, runtime manifest templates, deployment profile metadata, audit visibility, status, heartbeat, diagnostics upload/review, and signed entitlement bundles.
- Local-server entitlement verification is in place: pull/import, HMAC verification, replay/version rejection, trust state, local import audit, module access evaluation, heartbeat reporting, and diagnostics export.
- The low-risk support command lane is now end to end for the first two command types. Control Desk can queue `request_diagnostics` and `refresh_entitlement`; Control Cloud signs/stores them; the local-server processor verifies, executes, uploads or refreshes, and acknowledges them.
- Signed support commands now survive PostgreSQL `jsonb` payload reordering and microsecond timestamp precision round-trips because signing and verification canonicalize JSON payloads and normalize command timestamps before hashing/signing.
- Command-triggered diagnostics now collect the signed bootstrap runtime manifest automatically and probe Docker/Compose when available, so Local Server uploads include runtime availability, expected service state, recent warning/error log-tail lines, and the optional `safarsuite-app` Compose profile slot.

## Current Standing

We are no longer in the "can this control spine exist?" phase. The first spine exists and is smoke-proven across the desktop/API/cloud/local-server/UI pieces, including a live PostgreSQL run through Control Cloud and Local Server.

The project is still not at the final destination because the deployed SafarSuite runtime is not consuming this control plane yet. The next risk is not another small Control Desk screen; it is proving the app/runtime package can use the module-gateway contract and report useful runtime status/logs.

## Remaining Gaps

- Real SafarSuite image publication and version/channel handling.
- Actual SafarSuite app workspace integration with the local module-gateway contract.
- Portal payment UI and the first Pakistani payment provider adapter.
- Real provider/admin authorization, roles, MFA, password reset, and production mail retry handling.
- Product module catalog management screens beyond the current seeded/catalog boundary.
- Persistence-backed accounting/reporting screens that go beyond the current statement/journal visibility.
- Desktop packaging, backup/restore, rollout, and operational runbook hardening.

## Open Decisions

- Which payment gateway should be first?
- Is branch allowance billed separately?
- Is device count or named-user count the primary commercial limit?
- Do we allow any overuse during grace?
- Can Control Desk sign emergency offline files, or must Control Cloud remain the signer with an audited emergency fallback?

## Recommended Next Move

1. Use `docs/planning/safarsuite-app-integration-handoff.md` as the workspace-switch contract.
2. Move into the real SafarSuite app/runtime integration: publish a real app image or placeholder image with the correct runtime contract and wire the local module-gateway check into the app workspace.
3. Keep the runbook at `docs/planning/control-spine-demo-runbook.md` as the repeatable proof path before and after each runtime integration slice.

The recommended order is now real app/module-gateway integration first, then payment/auth hardening later. The control spine is strong enough to carry the next layer.
