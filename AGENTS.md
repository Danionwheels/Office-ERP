# AGENTS.md — SafarSuite Control Desk

You are working on SafarSuite Control Desk, an internal desktop app used by a provider office to manage SafarSuite customers, billing, licensing, and entitlements.

Read this entire file before making any changes. Every rule here is non-negotiable.

## Canonical Deployment Boundary

Read `docs/architecture/final-system-requirements-and-deployment-contract.md` before any packaging, hosting, infrastructure, or deployment work.

- SafarSuite Control Desk V1 runs completely on one dedicated office PC: desktop UI, local Office Control API process, and local PostgreSQL.
- Linux/cloud infrastructure hosts SafarSuite Control Cloud and the SafarSuite Client Portal, or an explicitly labelled disposable integration lab. It is not the final Control Desk host.
- Public DNS, HTTPS, reverse proxying, and SMTP are Control Cloud/Portal concerns, not Control Desk runtime prerequisites.
- A test Compose bundle or deployment proof must never silently redefine the accepted physical topology.

---

## Product Names (never invent new ones)

| Area | Canonical Name |
|------|---------------|
| Desktop app / this repo | SafarSuite Control Desk |
| Solution file | `SafarSuite.ControlDesk.sln` |
| Cloud service | SafarSuite Control Cloud |
| Client-facing portal | SafarSuite Client Portal |
| Client runtime | SafarSuite (local server / app) |

---

## Architecture: Modular Monolith with Clean Architecture

This is NOT microservices. It is a modular monolith with strict layer boundaries.

### Projects and Dependency Direction

```
SafarSuite.ControlDesk.Domain         ← innermost, depends on NOTHING external
SafarSuite.ControlDesk.Application    ← depends on Domain + Contracts
SafarSuite.ControlDesk.Infrastructure ← depends on Domain + Application + Contracts
SafarSuite.ControlDesk.Api            ← depends on Application + Contracts (thin HTTP layer)
SafarSuite.ControlDesk.Contracts      ← stable API request/response records only
apps/control-desk-ui/                 ← React + TypeScript frontend
```

**THE GOLDEN RULE: Dependencies point INWARD toward Domain.**

- Domain NEVER imports from Application, Infrastructure, Api, or any external package (no EF Core, no HTTP clients, no React, no Tauri, no payment SDKs, no cloud SDKs).
- Application NEVER imports from Infrastructure or Api.
- Infrastructure implements interfaces defined in Application.
- Api is thin: receive HTTP, call Application use case, map result, return response.

If you violate this direction, the code will be rejected.

---

## File Organization

### Backend

```
src/SafarSuite.ControlDesk.Domain/Modules/{ModuleName}/
src/SafarSuite.ControlDesk.Application/Modules/{ModuleName}/{UseCaseName}/
src/SafarSuite.ControlDesk.Infrastructure/Persistence/Configurations/{ModuleName}/
src/SafarSuite.ControlDesk.Infrastructure/Persistence/Repositories/
src/SafarSuite.ControlDesk.Api/Modules/{ModuleName}/
src/SafarSuite.ControlDesk.Contracts/ControlDeskApi/V1/{ModuleName}/
```

### Frontend

```
apps/control-desk-ui/src/
  app/          — shell, routing, providers, layout
  modules/      — feature-owned pages, components, hooks, API calls, types
  shared/       — reusable UI primitives, API client base, formatting
  assets/       — static files
```

Frontend dependency rule: `app -> modules -> shared`. Never import private files across modules. Use each module's `index.ts` for cross-module imports.

---

## Module Ownership

| Module | Owns |
|--------|------|
| Clients | client/company records, contacts, status, support notes |
| Contracts | contract terms, custom pricing, module/device/branch allowances |
| Billing | invoices, invoice lines, receivables, charge codes, charge rules |
| Payments | payment records, bank-transfer proofs, reconciliation, receipts |
| Accounting | GL, ledger accounts, journal entries, journal lines, COA |
| Entitlements | snapshots, module entitlements, offline validity, cloud outbox |
| ControlCloud | publishing, cloud receiver, portal projection, setup tokens, bootstrap |
| Audit | user actions, pricing changes, payment decisions |

New code goes in the module that owns it. Do not create cross-module "shared services" unless real duplication exists in 2+ modules.

---

## Critical Rules for Code Generation

1. **One file, one responsibility.** One aggregate per file. One command/query per folder. One handler per use case. One EF configuration per entity.

2. **No dumping-ground folders.** No `Helpers.cs`, no `Utils.cs`, no `Common/Everything.cs`.

3. **API contracts are mapped deliberately.** Never expose database entities to the frontend. Always map through DTOs/API models.

4. **Frontend pages do NOT contain business rules.** Business logic belongs in backend Domain or Application layers.

5. **Transaction boundary rule:** Any action writing to multiple aggregates/tables MUST use `IUnitOfWork.ExecuteInTransactionAsync`. Especially for accounting workflows (invoice issue + journal entry, payment + allocation, reversals).

6. **Do not call Control Cloud inside accounting transactions.** Enqueue durable outbox messages and publish them separately.

7. **Clone business meaning, not Access implementation.** This project modernizes a legacy Access system. Replicate what the business needs, not how Access did it.

8. **No premature microservices.** Keep it a modular monolith.

9. **Survey/FAS work is excluded.** Do not add SurveyValuation screens or Survey/FAS clone work unless explicitly told to.

---

## System Boundaries (what goes where)

```
Control Desk (this repo)
  → Source of truth for clients, contracts, pricing, billing, payments, accounting
  → Publishes approved events to Control Cloud via signed outbox

Control Cloud
  → Online control plane: receives events, signs entitlements, manages portal identity
  → Does NOT originate commercial decisions

Client Portal
  → Client-facing view only (invoices, payments, license status, self-service)
  → Does NOT become source of truth for anything

SafarSuite local server/app
  → Client business runtime
  → Consumes signed entitlements, enforces module gates locally
  → Does NOT make pricing/billing decisions
```

---

## Before Writing Any Code, Ask Yourself

1. Which module owns this feature?
2. Which layer should this logic live in?
3. Does the dependency direction stay inward?
4. Am I exposing database entities to the API/frontend? (Don't.)
5. Does this support the active milestone, or is it scope creep?

If unclear, ask before proceeding.

---

## Tech Stack

- **Backend:** C# / .NET, PostgreSQL, EF Core, minimal API endpoints
- **Frontend:** React + TypeScript + Vite
- **Database:** PostgreSQL via Docker Compose (local dev)
- **Desktop:** Tauri (later, don't add Tauri logic now)
- **Cloud:** Separate SafarSuite.ControlCloud.Api project in this repo

---

## Change Workflow

When implementing a feature, follow this order:

1. Domain entities/rules
2. Application command/query + handler
3. Infrastructure persistence/integration
4. API endpoint + contract
5. Frontend API/types/mappers
6. Frontend hook + page/component
7. Tests around domain rules and use cases

Do not skip steps or start from the UI and work backward.
