# SafarSuite Control Desk Project Tracker

Date started: 2026-06-30

Use this tracker to keep the project slow, deliberate, and on track.

Status values:

- Proposed
- Accepted
- In Progress
- Done
- Deferred

## Current Milestone

| Milestone | Goal | Status |
| --- | --- | --- |
| Milestone 1: Client Maintenance Spine | Maintain SafarSuite clients and only the accounting/billing/cloud support needed for client control | In Progress |

## Scope Rule

SafarSuite Control Desk is a client maintenance system first. Accounting, billing, payments, and cloud publishing stay active only when they support maintaining a client, charging that client, or controlling that client's access. Survey/FAS work is not part of the active product surface.

## Active Work

| Work item | Module | Status | Notes |
| --- | --- | --- | --- |
| Layered architecture contract | Architecture | Done | See `docs/architecture/layered-architecture.md` |
| Canonical product naming | Architecture | Done | See `docs/architecture/product-naming.md` |
| Legacy source reference | Legacy | Done | See `docs/legacy/source-reference.md` |
| Backend solution scaffold | Platform | Done | Created solution and projects for Api, Application, Domain, Infrastructure, Contracts |
| Frontend scaffold | Platform | Done | Vite React TypeScript app created under `apps/control-desk-ui` |
| Client maintenance UI | Clients/Frontend | Done | Active frontend now opens the client desk with list, create, detail, edit, lifecycle, support notes, and accounting-profile status |
| First domain model draft | Clients/Contracts/Billing/Payments/Entitlements/Audit | Done | Core aggregate roots and repository ports created |
| Client maintenance basics | Clients | Done | Client detail, edit, activate, and suspend API actions are wired |
| Client support notes | Clients | Done | Add/list internal client notes; client detail includes note history |
| Accounting/GL correction sweep | Accounting/Billing | Done | Invoice creation split clarified in `accounting-gl-foundation-sweep.md` |
| Accounting/Billing foundation | Accounting/Billing | In Progress | Dynamic charge setup, invoice draft generation, profile-assisted invoice issue GL posting, approved invoice payment posting, and basic journal/ledger read models are wired through API; review, reversals, tax, reports, and persistence still pending |
| Client accounting profile | Clients/Accounting | Done | Client can be linked to AR/default currency/cloud identity; invoice issue can resolve AR from the profile |
| Cloud invoice outbox | Billing/ControlCloud | In Progress | Invoice issue now creates a pending `InvoiceIssued` outbox message and API read model; publisher and PostgreSQL durability still pending |
| Client entitlement chain | Entitlements/ControlCloud | Proposed | After payment, update paid status and publish entitlement/client snapshot |

## Parking Lot

| Item | Reason Parked | Status |
| --- | --- | --- |
| Full Survey/FAS form cloning | Too broad for Milestone 1 | Deferred |
| SurveyValuation module | No longer part of the product goal for SafarSuite Control Desk; code/docs remain as reference but API routes and DI registrations are not active | Deferred |
| Legacy form clone plan | Historical reference only after pivot away from Survey/FAS clone | Deferred |
| Legacy form name map | Historical reference only after pivot away from Survey/FAS clone | Deferred |
| Survey Job Entry implementation | Work exists but is paused; do not extend unless requirements change | Deferred |
| Tauri desktop packaging | Wait until browser-hosted React UI proves the first flow | Deferred |
| Payment gateway selection | Needs business decision | Proposed |
| SQL backup restore | Useful later for sample data and parity checks | Proposed |
| PostgreSQL persistence for Survey Job Entry | Not needed while SurveyValuation remains parked | Deferred |
| Broader Survey reference lookups | Not needed while SurveyValuation remains parked | Deferred |
