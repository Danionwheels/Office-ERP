# Control Desk UI Overhaul Foundation

Status: Superseded by `docs/architecture/product-charter-2026-07-11.md`. The Command Center, Setup, Client 360, and Legacy Desk structure is exploratory and is not an accepted product architecture.

Date added: 2026-07-10

Purpose: redesign the Control Desk UI/UX in small, reversible slices while keeping the existing client desk usable.

Related flow contract:

```text
docs/planning/client-first-control-flow-2026-07-10.md
```

## Product Shape

The redesigned Control Desk should group work by operator intent instead of backend module names.

| Area | Job | Owns |
| --- | --- | --- |
| Login | Enter the local Control Desk workspace | Local operator session, session expiry, sign-out |
| Command Center | Know what needs attention | Work queue, setup gaps, client issues, payment review, cloud alerts |
| Setup | Define once and reuse | Clients, product catalog, accounting ranges, ledger controls, charge codes, deployment defaults, operators |
| Client 360 | Understand one customer | Profile, contacts, contracts, statement, entitlement, deployment health, support notes |
| Commercial Desk | Run customer money flow | Contracts, pricing, billing rules, invoice drafts, invoices, payments, credits, refunds, entitlement issue |
| Accounting Desk | Control the ledger | Chart of accounts, journals, opening balances, periods, close, reconciliation, financial reports |
| Deployment & Cloud | Install and support customer runtime | Setup tokens, bootstrap packages, handoff, heartbeat, diagnostics, app activation, pairing descriptor, support commands |
| Access & Security | Manage people and devices | Provider operators, scopes, MFA/recovery, local devices, pairing abuse events |
| Reports & Audit | Prove what happened | Client statements, journal source trail, cloud audit, diagnostics history, outbox messages |
| Legacy Desk | Preserve current capability during migration | Current `ClientDeskPage` until replacement screens are complete |

## Migration Rules

- Keep the current client desk reachable until every moved workflow has a replacement.
- Move one operator job at a time; do not split a working flow across old and new homes.
- Treat Setup as the source of reusable definitions; daily work should consume setup data, not define it inline by default.
- Treat each selected model as the owner of the screen; if Clients is open, only client work is interactive, and if COA Ranges is open, only range work is interactive.
- Keep Control Desk login local-first; Control Cloud access belongs inside Deployment & Cloud or Access & Security.
- Keep each slice buildable and shippable before starting the next one.
- Prefer reshaping the frontend shell first; defer backend authorization hardening to a focused security slice.

## First Slice

- Add a real login entry point using a local Control Desk operator session endpoint.
- Add an authenticated app shell with top-level sections.
- Add placeholder workspaces for the new hierarchy.
- Mount the current client desk as Legacy Desk.
- Do not migrate business workflows yet.

## Second Slice

- Promote Setup from a placeholder into a live setup snapshot.
- Group reusable definitions into Customer & Product, Accounting Foundation, and Deployment Defaults.
- Load clients, product modules, product access catalog, account ranges, ledger accounts, controls, opening balance profile, and voucher numbering in one place.
- Add client quick-add to Setup so new clients can be created where reusable definitions begin.
- Keep current setup editors in Legacy Desk until each editor is migrated cleanly.

## Third Slice

- Promote Command Center from a placeholder into the rough big-picture map.
- Show the operating flow from Setup through Client 360, Commercial, Accounting, Deployment, and Audit.
- List every major bucket with its current responsibility and migration state.
- Add the second-sweep sorting lens: shared definition, client-specific action, daily work, posting/proof, runtime support, and audit only.

## Fourth Slice

- Refocus Setup from a multi-lane snapshot into a single-model workbench.
- Add a compact model selector for Clients, Product Catalog, COA Ranges, Ledger Accounts, and Deployment Defaults.
- Let the active model take the full work surface, starting with dedicated Clients and COA Ranges views.
- Remove the old all-at-once lane grid from the active Setup experience.

## Fifth Slice

- Promote COA Ranges from a focused register into a real focused editor.
- Reuse the existing chart-of-accounts range panel for range facts, validation groups, editable fields, posting/active flags, and save behavior.
- Save range changes through the accounting setup endpoint, then refresh ranges, ledger accounts, and validation inside the Setup snapshot.
- Keep the focused model rule intact: when COA Ranges is active, client creation and other setup models are not visible in the work surface.

## Sixth Slice

- Promote Clients from quick-add/register into a focused client master editor.
- Keep Clients scoped to setup-owned master records: select, create, edit legal/display name, activate, and suspend.
- Leave contacts, portal invitations, notes, billing, deployment, and support for Client 360 or their own focused desks.
- Keep the focused model rule intact: when Clients is active, COA ranges and other setup models are not visible in the work surface.

## Seventh Slice

- Promote Product Catalog from summary lists into a focused catalog workbench.
- Keep product modules read-only in Setup while showing the selected module's commercial mode, billing defaults, access groups, and mapped resources.
- Move access catalog maintenance into Setup for define-once module groups and product resources, with refresh, save, edit, and remove actions.
- Leave product-kernel command publishing and runtime rollout for Deployment & Cloud.

## Eighth Slice

- Promote Ledger Accounts from a flat summary list into a focused chart-account workbench.
- Keep this slice read-oriented: account register, selected-account facts, range coverage, parent account, and child accounts.
- Show the dependency from COA Ranges into Ledger Accounts without mixing journals, reports, opening balances, or posting workflows into Setup.
- Defer ledger account create/update/toggle flows to a dedicated accounting setup/editor slice.

## Ninth Slice

- Promote Deployment Defaults from placeholder runtime rows into a focused setup-boundary workbench.
- Show reusable runtime defaults, their current readiness, their owner, and where each is consumed.
- Keep setup tokens, bootstrap packages, heartbeats, diagnostics, app activation/revocation, pairing descriptors, and support commands out of Setup.
- Finish the first Setup sweep; next work should begin the second-sweep separation and Client 360 shape.

## Next Slices

1. Use the client-first flow contract as the target shape for the second sweep.
2. Build Client 360 around selected-client context.
3. Combine contracts, billing, payments, entitlements, and statement into the selected-client flow.
4. Reframe normal accounting visibility as a Voucher Register with expandable proof.
5. Move advanced GL, reports, period close, reconciliation, and repair/import tools into Admin Accounting.
6. Reframe Deployment & Cloud around the selected client's installation lifecycle.
7. Split Access & Security from cloud operations.
8. Add Reports & Audit as proof/history surface.
