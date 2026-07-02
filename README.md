# SafarSuite Control Desk

This is the separate workspace for the provider-owned office software.

It is not the SafarSuite client product. It is the internal desktop system we use to manage SafarSuite customers, pricing, renewals, portal invoices, payment status, allowed devices, allowed branches, and module entitlements.

Canonical project names:

- Product / desktop app: SafarSuite Control Desk
- Project folder: `safarsuite-control-desk`
- Solution: `SafarSuite.ControlDesk.sln`
- Cloud service: SafarSuite Control Cloud
- Client portal: SafarSuite Client Portal

## Product Boundary

```text
SafarSuite Control Desk
  internal desktop app used by our office
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

- `docs/architecture/product-naming.md`
- `docs/architecture/layered-architecture.md`
- `docs/architecture/cloud-local-communication-map.md`
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

Use `E:/travel tour/survey` as the canonical reference folder for cloning core legacy behavior. The rule is to clone business meaning and important workflows, not the old Access implementation directly.

## Local Database

Development uses PostgreSQL through Docker Compose.

```powershell
dotnet tool restore
docker compose up -d safarsuite-control-desk-postgres
dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlDesk.Infrastructure --startup-project src/SafarSuite.ControlDesk.Api --context ControlDeskDbContext
dotnet run --project src/SafarSuite.ControlDesk.Api
```

Connection string used by `appsettings.Development.json`:

```text
Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password
```

The current control-spine persistence slices store clients, contacts, support notes, client accounting profiles, client contracts, contract module allowances, ledger accounts, journal entries, journal lines, charge codes, client charge rules, invoices, invoice lines, payments, entitlement snapshots, entitlement modules, and cloud outbox messages in PostgreSQL.

For local development, pending cloud outbox messages can be marked sent through:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5188/api/v1/control-cloud/outbox-messages/publish-local?batchSize=20"
```
