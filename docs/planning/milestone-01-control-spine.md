# Milestone 1: Control Spine

Date started: 2026-06-30

## Goal

Create the first clean product spine for SafarSuite Control Desk without drifting into legacy Survey/FAS cloning.

This milestone proves that the project structure, naming, dependency direction, and the client-accounting-billing-cloud chain are stable enough to build on.

## Scope

Included:

- `SafarSuite.ControlDesk.sln`
- backend layer projects:
  - `SafarSuite.ControlDesk.Api`
  - `SafarSuite.ControlDesk.Application`
  - `SafarSuite.ControlDesk.Domain`
  - `SafarSuite.ControlDesk.Infrastructure`
  - `SafarSuite.ControlDesk.Contracts`
- placeholder frontend folder:
  - `apps/control-desk-ui`
- placeholder desktop folder:
  - `desktop/tauri`
- database migration/seed folders
- first domain model draft for:
  - clients
  - contracts
  - custom pricing
  - module allowances
  - device allowances
  - branch allowances
  - invoices
  - payments
  - entitlement snapshots
  - audit events
- client accounting profile and ledger identity planning
- reliable cloud publishing/outbox planning

Excluded:

- full Access form cloning
- full accounting engine
- SurveyValuation screens and Survey/FAS clone work
- payment gateway implementation
- Tauri packaging
- client portal UI
- cloud deployment

## First Vertical Slice

```text
create SafarSuite client
create/link client accounting profile
set contract terms
set custom pricing
select allowed modules
set allowed devices and branches
generate invoice
issue invoice and post receivable GL
publish invoice to cloud/portal through outbox
record payment
post receipt GL
issue entitlement snapshot
publish entitlement/client status to cloud
show active/paid status
```

## Tracking Rule

New ideas are allowed, but they must land in one of three places:

- current milestone work item
- parking lot
- later milestone

If a task does not support the first vertical slice, it should be written down and deferred.

## Acceptance Criteria

- solution and project names match the naming contract
- backend projects compile: done, `dotnet build SafarSuite.ControlDesk.sln` passes with 0 warnings and 0 errors
- project references enforce clean dependency direction: done for initial scaffold
- frontend/desktop/database folders exist but do not contain premature logic: done
- first domain model draft exists before UI implementation begins: done, see `docs/planning/control-spine-domain-model.md`
- Survey/FAS clone work is explicitly deferred so it cannot pull Milestone 1 off the client billing/control path
