# Codex Handshake: SafarSuite Control Desk Phase

Use this file to start a separate Codex project/thread for the SafarSuite Control Desk phase.

## Opening Prompt For New Codex Thread

```text
We are starting the SafarSuite Control Desk project.

This is a separate project from the SafarSuite client product. The goal is to build our internal desktop app for managing SafarSuite clients, custom pricing, renewals, invoices, payments, device/module/branch limits, and entitlement/license control.

Please read CODEX_HANDSHAKE.md first, then read the docs under docs/architecture and docs/legacy before proposing or implementing anything.

The first phase is not to blindly code everything. First, help us turn the legacy Survey/FAS app into a tracked modernization plan, then scaffold the SafarSuite Control Desk project carefully.
```

## Project Folder

```text
C:/Users/Daniyal/Documents/Codex/safarsuite-control-desk
```

Current temporary workspace may still be named `provider-office-erp` until the folder is renamed.

This folder is intentionally separate from:

```text
C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2
```

The `hello-there-2` folder contains SafarSuite and the current prototype CloudServer. SafarSuite Control Desk should not be built directly inside the SafarSuite repo unless we intentionally copy/reuse code.

## Why This Project Exists

SafarSuite Control Desk is our internal office desktop software. It is not sold to clients.

It will manage:

- SafarSuite clients
- custom client pricing
- plans and renewals
- invoices and receipts
- Pakistani card/bank-transfer payment tracking
- portal publishing
- allowed modules
- allowed branches
- allowed devices/users
- activation and entitlement decisions
- office GL/accounting
- expenses and assets
- support/admin notes

The office app should become the source of truth for commercial decisions. The cloud/portal should mirror approved client-facing data and issue signed entitlements.

## Final Product Direction

Final product:

```text
SafarSuite Control Desk desktop app for office use
```

Development mode:

```text
React TypeScript UI can run in browser during development.
```

Recommended stack:

```text
.NET 10 backend/application layer
React + TypeScript frontend
Tauri desktop wrapper for production
PostgreSQL local database
SafarSuite Control Cloud API integration
```

Do not treat this as a normal public web app. The browser is only a development convenience.

## System Architecture

The clean model is:

```text
SafarSuite Control Desk
  internal desktop app
  source of truth for clients, pricing, billing, renewals, device/module limits

SafarSuite Control Cloud + SafarSuite Client Portal
  receives approved snapshots from SafarSuite Control Desk
  shows invoices/payment options to clients
  receives payment callbacks/reviews bank transfers
  issues signed entitlement snapshots and product-kernel commands

SafarSuite Client Systems
  offline local server
  branch-to-HQ sync
  cloud sync
  hosted SaaS
  consumes signed entitlements and obeys limits
```

Important distinction:

```text
SafarSuite Control Cloud
  billing, licensing, portal, payments, entitlements
  needed for provider operations

Client Data Sync Cloud
  actual SafarSuite business data sync from branches/HQ/cloud
  optional per client/plan
```

Do not mix these concepts.

## Legacy Survey/FAS Evidence

Legacy source files were provided at:

```text
E:/travel tour/survey/Actappl7.mdb
E:/travel tour/survey/Survey.sql
E:/travel tour/survey/ANI_backup_2025_11_19_120002_1863486.bak
```

Current extracted findings are copied here:

```text
docs/legacy/survey-software-sweep.md
```

Summary:

- Access app exported cleanly.
- 111 Access tables.
- 122 queries.
- 178 forms.
- 183 reports.
- 6 modules.
- 3 macros/scripts.
- `Survey.sql` has 96 SQL Server-style table definitions.
- The app is a Financial Accounting System integrated with Survey Management.
- It links to data files such as `ACTDATA7.mdb`, `VALDATA7.mdb`, `MYKDATA7.mdb`, and `SCRDATA7.mdb`.

Core rule:

```text
Clone business meaning, not old Access implementation.
```

Ambiguity rule:

```text
If a field, validation rule, lookup, action, or workflow status is unclear, inspect the legacy clone spec, exported Access form, Survey.sql, or Actappl7.mdb before choosing new behavior.
```

Some objects should be cloned closely. Some should be modernized. Some should be archived or ignored.

## Tracking Docs To Read

Read these first:

```text
docs/architecture/product-naming.md
docs/architecture/layered-architecture.md
README.md
docs/architecture/why-and-cloud-portal-link.md
docs/architecture/current-cloud-server-reuse-assessment.md
docs/legacy/source-reference.md
docs/legacy/survey-software-sweep.md
docs/planning/survey-clone-and-modernization-tracker.md
docs/planning/survey-object-clone-register.md
docs/planning/control-desk-new-requirements.md
docs/planning/offline-entitlement-control-rules.md
docs/planning/control-cloud-deployment-tracker.md
docs/planning/project-tracker.md
docs/planning/milestone-01-control-spine.md
docs/planning/control-spine-domain-model.md
docs/planning/survey-valuation-domain-model.md
docs/planning/legacy-form-clone-plan.md
docs/planning/legacy-form-name-map.md
docs/planning/form-specs/survey-job-entry-clone-spec.md
```

The original tracker copies came from the SafarSuite repo:

```text
C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2/docs/provider-office-erp/survey-clone-and-modernization-tracker.md
C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2/docs/provider-office-erp/survey-object-clone-register.md
C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2/docs/provider-office-erp/provider-office-new-requirements.md
```

Local copies are now available under `docs/planning`.

## Existing CloudServer To Reuse

Reference project:

```text
C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2/src/CloudServer
```

Useful existing concepts:

- activation requests
- signed activation tokens
- signed first-manager setup tokens
- signed product-kernel commands
- module entitlement snapshots
- owner dashboard auth
- owner action audit
- local server activation enforcement
- command replay protection

Current limitations:

- activation registry is in-memory
- subscription state endpoint is stubbed
- device revocation endpoint is stubbed
- no billing model yet
- no portal payment model yet
- no production signing-key lifecycle yet

Decision:

Use the current CloudServer as the seed/reference for SafarSuite Control Cloud. Do not make SafarSuite Control Desk depend on the prototype in-memory implementation.

## First Real Phase

Do not begin by rebuilding all 178 forms.

Start with this flow:

```text
create SafarSuite client
set custom pricing
set allowed modules/devices/branches
generate invoice
publish invoice to portal/control cloud
record payment
issue signed entitlement or product-kernel command
SafarSuite applies renewed access
SafarSuite Control Desk shows active/paid status
```

This proves the new business value before pulling in every legacy Survey/FAS screen.

## Early Work Items

1. Copy the SafarSuite Control Desk tracking docs into this project if missing.
2. Decide project scaffold:
   - .NET solution
   - backend API/application project
   - React TypeScript frontend
   - Tauri wrapper later
   - PostgreSQL migration folder
3. Create domain model draft for:
   - client
   - contract
   - plan
   - module entitlement
   - device allowance
   - branch allowance
   - invoice
   - payment
   - entitlement snapshot
4. Create first migration draft.
5. Build first vertical slice after schema is reviewed.

## Important Product Decisions Already Made

- SafarSuite Control Desk is separate from the SafarSuite client product.
- SafarSuite Control Desk is office-use only.
- Final app should be desktop.
- Development UI can run in browser.
- Cloud sync for client business data is optional.
- Control Cloud for billing/licensing/portal is still needed.
- Dynamic pricing is per client and owned by SafarSuite Control Desk.
- SafarSuite should consume signed entitlements, not hard-code pricing.
- Heartbeat status and license validity are separate; a paid offline-capable client must keep working through the paid period even if heartbeat is unavailable.
- Offline renewal files are required for local servers that cannot reach the internet near expiry.
- Revocation for offline paid clients is enforced on next heartbeat, next renewal-file import, or license boundary unless a shorter high-risk lease is assigned.
- Every entitlement, heartbeat, product command, renewal file, emergency unlock, and support override must be audited.
- There is one production SafarSuite Control Cloud; the older SafarSuite workspace CloudServer is reference/prototype material, not a second production cloud.
- Client Linux deployment is portal-controlled but local-server-pulled: V1 uses signed bootstrap bundles and Docker Compose, with `.deb` packaging deferred until the service layout is stable.

## Do Not Do Yet

- Do not blindly recreate every Access form.
- Do not copy old Access passwords/security logic.
- Do not merge SafarSuite Control Desk into the SafarSuite client product.
- Do not make client business-data cloud sync mandatory.
- Do not hard-code one payment provider into core billing rules.
- Do not rely on in-memory activation storage for production.

## Useful Next Question

Before scaffolding code, ask:

```text
Should the new project start as a single .NET solution with API + React + Tauri folders, or should backend/control-cloud/desktop UI be split into separate repos from day one?
```

Recommended answer unless the user says otherwise:

```text
Start as one repo/solution with clear folders. Split later only if deployment complexity demands it.
```
