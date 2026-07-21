# SafarSuite Control Desk

This is the separate workspace for the provider-owned office software.

It is not the SafarSuite client product. It is the internal desktop system we use to manage SafarSuite customers, pricing, renewals, portal invoices, payment status, allowed devices, allowed branches, and module entitlements.

## Current Direction

The canonical product direction is `docs/architecture/product-charter-2026-07-11.md`. The canonical physical topology and deployment acceptance rules are in `docs/architecture/final-system-requirements-and-deployment-contract.md`.

SafarSuite Control Desk is the desktop operating experience for a connected provider-control system. For V1, one dedicated office PC hosts the desktop UI, local Office Control API process, and authoritative local PostgreSQL database; no separate office server or Linux Control Desk host is required. Approved state moves outbound through SafarSuite Control Cloud to SafarSuite Server, which enforces signed entitlements and returns observed state.

Active priorities are tracked in `docs/planning/active-roadmap-2026-07-11.md`. Existing implementation history remains in `docs/planning/project-tracker.md` but does not set current priority.

Canonical project names:

- Product / desktop app: SafarSuite Control Desk
- Project folder: `safarsuite-control-desk`
- Solution: `SafarSuite.ControlDesk.sln`
- Cloud service: SafarSuite Control Cloud
- Client portal: SafarSuite Client Portal

## Product Boundary

```text
SafarSuite Office Control System
  one dedicated office PC: Control Desk desktop UI + local Office Control API + local PostgreSQL
  source of truth for client contracts, pricing, billing, and commercial controls

SafarSuite Control Cloud + SafarSuite Client Portal
  receives approved client/pricing/license changes from SafarSuite Control Desk
  exposes invoices and payments to clients
  issues signed entitlements and activation/product-kernel commands

SafarSuite Client Systems
  offline local server, HQ sync, cloud sync, or hosted SaaS
  consume entitlements from the control cloud
```

## Initial Docs

- `docs/architecture/product-charter-2026-07-11.md`
- `docs/architecture/final-system-requirements-and-deployment-contract.md`
- `docs/architecture/control-model-gap-map-2026-07-11.md`
- `docs/planning/active-roadmap-2026-07-11.md`
- `docs/planning/existing-work-disposition-2026-07-11.md`
- `docs/architecture/product-direction.md`
- `docs/architecture/product-naming.md`
- `docs/architecture/layered-architecture.md`
- `docs/architecture/client-runtime-communication-and-deployment-blueprint.md`
- `docs/architecture/cloud-local-communication-map.md`
- `docs/architecture/product-module-catalog-boundary.md`
- `docs/architecture/why-and-cloud-portal-link.md`
- `docs/architecture/current-cloud-server-reuse-assessment.md`
- `docs/legacy/source-reference.md`
- `docs/legacy/survey-software-sweep.md`
- `docs/planning/survey-clone-and-modernization-tracker.md`
- `docs/planning/survey-object-clone-register.md`
- `docs/planning/control-desk-new-requirements.md`
- `docs/planning/offline-entitlement-control-rules.md`
- `docs/planning/control-cloud-deployment-tracker.md`
- `docs/planning/project-tracker.md`
- `docs/planning/milestone-01-control-spine.md`
- `docs/planning/control-spine-domain-model.md`
- `docs/planning/survey-valuation-domain-model.md`
- `docs/planning/legacy-form-clone-plan.md`
- `docs/planning/legacy-form-name-map.md`
- `docs/planning/form-specs/survey-job-entry-clone-spec.md`

## Legacy Reference

Use `E:/travel tour/survey` only as legacy research evidence. Preserve a behavior only when a current SafarSuite provider workflow independently requires it; legacy parity is not a product objective.

## Development Database

Development uses PostgreSQL through Docker Compose. It represents the authoritative Office Control database during local development. V1 production keeps PostgreSQL on the same dedicated office PC as Control Desk, binds it locally, manages its lifecycle through the installer/service boundary, and protects it with automated backup plus clean replacement-PC restore evidence.

```powershell
dotnet tool restore
docker compose up -d safarsuite-control-desk-postgres
$env:SAFARSUITE_ALLOW_DEVELOPMENT_DB_FALLBACK = "true"
dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlDesk.Infrastructure --startup-project src/SafarSuite.ControlDesk.Api --context ControlDeskDbContext
Remove-Item Env:SAFARSUITE_ALLOW_DEVELOPMENT_DB_FALLBACK
dotnet run --project src/SafarSuite.ControlDesk.Api
```

Connection string used by `appsettings.Development.json`:

```text
Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password
```

The current control-spine persistence slices store clients, contacts, support notes, client accounting profiles, client contracts, contract module allowances, ledger accounts, journal entries, journal lines, charge codes, client charge rules, invoices, invoice lines, payments, immutable approved client-access revisions, derived entitlement snapshots, module rows, and cloud outbox messages in PostgreSQL.

Office outbox reads are client-aware keyset pages rather than unbounded payload scans. For example:

```text
GET /api/v1/control-cloud/outbox-messages?clientId={clientId}&take=50&cursor={opaqueCursor}
```

The response includes the bounded rows, continuation metadata, and complete filtered pending/failed/sent/ready/attempt counts. See `docs/architecture/office-outbox-scale-boundary.md`.

Client discovery and daily operator work are bounded too:

```text
GET /api/v1/clients?search={text}&sort=code&take=50&cursor={opaqueCursor}
GET /api/v1/command-center/client-work?lane=setup&sort=priority&take=25&cursor={opaqueCursor}
```

The client response includes filtered matches plus whole-register status totals. The Command Center response reads a transactionally refreshed per-client projection and includes exact search-scoped lane totals, replacing the former browser-side all-client load and per-client request fan-out. See `docs/architecture/office-client-directory-work-queue-scale-boundary.md`.

Office financial history is composed from an exact summary and independent register pages:

```text
GET /api/v1/clients/{clientId}/financial-summary
GET /api/v1/clients/{clientId}/invoices?take=25&cursor={opaqueCursor}
GET /api/v1/clients/{clientId}/payments?take=25&cursor={opaqueCursor}
GET /api/v1/clients/{clientId}/financial-activity?take=25&cursor={opaqueCursor}
GET /api/v1/clients/{clientId}/journal-postings?take=20&cursor={opaqueCursor}
```

Commercial journals store their client and immutable source document ID. The company journal register is paged too, and journal lines load only when one entry is opened. See `docs/architecture/office-financial-read-spine-scale-boundary.md`.

For local development, pending cloud outbox messages can be marked sent through:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5188/api/v1/control-cloud/outbox-messages/publish-local?batchSize=20"
```

## Local Verification

Run the fast local verification pass from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-Local.ps1
```

The script builds the three API hosts into `.codex-run/verify` so existing Visual Studio or running API processes do not lock the normal `bin` output. It also runs the Control Desk UI production build, the in-memory accounting smoke, and the LocalServer entitlement smoke.
