# Layered Architecture

Date decided: 2026-06-30

This is the architecture contract for SafarSuite Control Desk.

The goal is to keep the project slow, clean, modular, DRY, and update friendly. We should be able to add or change a module without hunting business rules across unrelated files.

## Architecture Style

Use a modular monolith with clean architecture boundaries.

Do not start with microservices. SafarSuite Control Desk is an internal desktop app with a local PostgreSQL database and a controlled integration with SafarSuite Control Cloud. A modular monolith gives us strong boundaries without deployment complexity.

## Non-Negotiable Rules

1. Each file has one clear responsibility.
2. Each module owns its domain rules, use cases, persistence mapping, API endpoints, and UI screens.
3. Dependencies point inward toward domain logic.
4. Domain code does not depend on database, HTTP, React, Tauri, payment providers, or cloud clients.
5. Application code coordinates use cases but does not know UI details.
6. Infrastructure implements external details such as PostgreSQL, files, payment adapters, cloud API clients, signing clients, and clock/id providers.
7. Frontend screens do not contain business rules that belong in backend domain/application code.
8. Shared code must be small, stable, and genuinely reused. No dumping-ground folders.
9. API contracts and frontend DTOs are mapped deliberately; do not leak database entities into the UI.
10. New work starts in the relevant module. Cross-module abstractions are added only after real duplication appears.

## Solution Shape

Recommended repository structure:

```text
safarsuite-control-desk/
  SafarSuite.ControlDesk.sln
  src/
    SafarSuite.ControlDesk.Api/
    SafarSuite.ControlDesk.Application/
    SafarSuite.ControlDesk.Domain/
    SafarSuite.ControlDesk.Infrastructure/
    SafarSuite.ControlDesk.Contracts/
  apps/
    control-desk-ui/
  desktop/
    tauri/
  database/
    migrations/
    seeds/
  docs/
```

`desktop/tauri` comes after the first browser-hosted React UI works. Tauri should package the app, not become a second business-logic layer.

## Backend Layers

### Domain

Project:

```text
src/SafarSuite.ControlDesk.Domain
```

Responsibility:

- entities
- aggregates
- value objects
- domain events
- domain services
- business invariants
- module-owned rule objects

Allowed dependencies:

- .NET base libraries
- shared domain primitives inside the same project

Not allowed:

- EF Core
- HTTP clients
- controllers/endpoints
- UI DTOs
- database connection strings
- payment gateway SDKs
- cloud API clients

Example module layout:

```text
SafarSuite.ControlDesk.Domain/
  SharedKernel/
    Entity.cs
    ValueObject.cs
    Money.cs
    DateRange.cs
  Modules/
    Clients/
      Client.cs
      ClientCode.cs
      ClientStatus.cs
      ClientCreated.cs
    Contracts/
      ClientContract.cs
      ContractTerm.cs
      ModuleAllowance.cs
      DeviceAllowance.cs
      BranchAllowance.cs
    Billing/
      Invoice.cs
      InvoiceLine.cs
      InvoiceStatus.cs
      PaymentAllocation.cs
    Entitlements/
      EntitlementSnapshot.cs
      EntitlementStatus.cs
      EntitlementPolicy.cs
```

### Application

Project:

```text
src/SafarSuite.ControlDesk.Application
```

Responsibility:

- commands
- queries
- use-case handlers
- validation
- transaction orchestration
- authorization checks at use-case level
- ports/interfaces for persistence and integrations
- application DTOs when useful

Allowed dependencies:

- Domain
- Contracts when a use case needs shared request/response models

Not allowed:

- EF Core DbContext implementation
- direct SQL
- React/UI concerns
- direct payment provider SDK usage
- direct cloud SDK usage

### Transaction Boundary Rule

Application use cases own transaction orchestration.

Single-aggregate actions may call `IUnitOfWork.SaveChangesAsync` once at the end of the handler.

Any action that writes more than one aggregate/table must run through `IUnitOfWork.ExecuteInTransactionAsync`. This is mandatory for accounting-sensitive workflows such as:

- issuing/finalizing an invoice and creating journal entries
- recording a payment and allocating it to invoices
- reversing payments or voiding posted accounting documents
- importing opening balances across accounts, customers, and invoices

The Infrastructure implementation must map `ExecuteInTransactionAsync` to a real database transaction, such as an EF Core `BeginTransactionAsync` / commit / rollback block over one `DbContext`. If the operation fails after any write, no partial accounting state should remain.

Example module layout:

```text
SafarSuite.ControlDesk.Application/
  Common/
    Abstractions/
      IUnitOfWork.cs
      ICurrentUser.cs
      IClock.cs
      IIdGenerator.cs
  Modules/
    Clients/
      CreateClient/
        CreateClientCommand.cs
        CreateClientHandler.cs
        CreateClientValidator.cs
        CreateClientResult.cs
      GetClient/
        GetClientQuery.cs
        GetClientHandler.cs
      Ports/
        IClientRepository.cs
    Billing/
      GenerateInvoice/
      RecordPayment/
      Ports/
        IInvoiceRepository.cs
    Entitlements/
      IssueEntitlement/
      Ports/
        IEntitlementSigner.cs
        IControlCloudPublisher.cs
```

### Infrastructure

Project:

```text
src/SafarSuite.ControlDesk.Infrastructure
```

Responsibility:

- PostgreSQL persistence
- EF Core configurations
- migrations if using EF migrations
- repository implementations
- external file storage
- SafarSuite Control Cloud client
- payment gateway adapters
- bank-transfer proof storage
- email/SMS adapters if added later
- local signing implementation only if explicitly approved

Allowed dependencies:

- Domain
- Application
- Contracts
- infrastructure packages

Example layout:

```text
SafarSuite.ControlDesk.Infrastructure/
  Persistence/
    ControlDeskDbContext.cs
    Configurations/
      Clients/
      Contracts/
      Billing/
      Entitlements/
    Repositories/
      ClientRepository.cs
      InvoiceRepository.cs
  Integrations/
    ControlCloud/
      ControlCloudClient.cs
      ControlCloudOptions.cs
    Payments/
      PaymentGatewayClient.cs
      ManualBankTransferReviewService.cs
  Files/
    LocalDocumentStore.cs
  System/
    SystemClock.cs
    GuidIdGenerator.cs
```

### API

Project:

```text
src/SafarSuite.ControlDesk.Api
```

Responsibility:

- HTTP endpoints/controllers
- authentication middleware
- authorization policies
- request/response mapping
- dependency injection composition root
- health checks
- OpenAPI setup

The API layer should be thin. It receives HTTP, calls application use cases, maps results, and returns responses.

Example layout:

```text
SafarSuite.ControlDesk.Api/
  Program.cs
  Composition/
    ApplicationServices.cs
    InfrastructureServices.cs
    ModuleEndpoints.cs
  Modules/
    Clients/
      ClientEndpoints.cs
      ClientApiModels.cs
    Billing/
      BillingEndpoints.cs
      BillingApiModels.cs
    Entitlements/
      EntitlementEndpoints.cs
      EntitlementApiModels.cs
```

### Contracts

Project:

```text
src/SafarSuite.ControlDesk.Contracts
```

Responsibility:

- stable API request/response records
- shared enum names that are intentionally part of the API
- versioned integration contracts for SafarSuite Control Cloud

This project should stay small. Do not put domain entities here.

Example layout:

```text
SafarSuite.ControlDesk.Contracts/
  ControlDeskApi/
    V1/
      Clients/
      Billing/
      Entitlements/
  ControlCloud/
    V1/
      PublishClientSnapshotRequest.cs
      PublishInvoiceRequest.cs
      EntitlementIssuedResponse.cs
```

## Frontend Layers

Frontend app:

```text
apps/control-desk-ui
```

Use React + TypeScript. Keep the frontend modular by business feature, not by technical type only.

### Frontend Layer Responsibilities

```text
app
  app shell, routing, providers, layout, bootstrapping

modules
  feature-owned pages, components, hooks, API calls, state, validation, and types

shared
  reusable UI primitives, API client base, formatting, dates, permissions, layout helpers

assets
  images, icons, fonts, static files
```

Recommended layout:

```text
apps/control-desk-ui/
  src/
    app/
      App.tsx
      routes.tsx
      providers/
      layout/
    modules/
      clients/
        api/
          clientsApi.ts
          clientsMappers.ts
        components/
          ClientForm.tsx
          ClientStatusBadge.tsx
        hooks/
          useClient.ts
          useClientSearch.ts
        pages/
          ClientListPage.tsx
          ClientDetailPage.tsx
        state/
          clientsStore.ts
        types/
          clientTypes.ts
        validation/
          clientSchema.ts
        index.ts
      contracts/
      billing/
      payments/
      entitlements/
      control-cloud/
      audit/
    shared/
      api/
        httpClient.ts
        apiError.ts
      components/
        Button.tsx
        DataTable.tsx
        Dialog.tsx
        FormField.tsx
      hooks/
      layout/
      permissions/
      utils/
    assets/
```

### Frontend Dependency Rule

Allowed:

```text
app -> modules -> shared
```

Avoid:

```text
shared -> modules
module A importing private files from module B
page component calling fetch directly
form component containing invoice/payment business policy
```

If one module needs another module, import only from that module's public `index.ts`.

Example:

```text
modules/billing/pages/InvoiceDetailPage.tsx
  may import from modules/clients
  only through modules/clients/index.ts
```

## Module Boundaries

Start with these modules:

| Module | Owns |
| --- | --- |
| Clients | SafarSuite client/company records, contacts, status, support notes |
| Contracts | custom pricing, contract terms, modules, devices, branches |
| Billing | invoices, invoice lines, receivables, invoice status |
| Payments | card payments, bank-transfer proofs, manual reconciliation, receipt records |
| Entitlements | paid-until, grace, snapshots, product-kernel commands, license status |
| ControlCloud | publishing approved snapshots, cloud callbacks, portal mirrors |
| Audit | user actions, pricing changes, payment decisions, entitlement decisions |

Later modules:

| Module | Owns |
| --- | --- |
| Accounting | GL, vouchers, COA, postings, trial balance, P&L, balance sheet |
| Assets | office assets and asset accounting movements |
| SurveyValuation | dockets, valuation jobs, valuators, valuation invoices, survey reports |
| Reporting | report catalog, filters, exports, print layouts |
| IdentityAccess | users, roles, permissions, sessions |

## First Vertical Slice

The first build should prove this flow:

```text
create SafarSuite client
set contract terms
set custom pricing
select allowed modules
set allowed devices and branches
generate invoice
record payment
issue entitlement snapshot
show active/paid status
```

Do not begin by recreating every Access form. Legacy Survey/FAS objects should feed this architecture through the tracker, not pull us away from it.

## DRY Rules

DRY means remove meaningful duplication, not hide clear code behind premature abstractions.

Good reuse:

- shared `Money`, `DateRange`, `Result`, and `PagedResult` primitives
- one HTTP client base
- one form field wrapper
- common validation helpers
- module-owned repositories with consistent interfaces
- shared table, dialog, and toast components

Bad reuse:

- one generic service that handles every module
- one generic repository for all business rules
- shared folders full of unrelated helpers
- frontend hooks that know too much about multiple modules
- copying database entities into API responses
- putting all screens into one `components` folder

Add shared abstractions only when:

1. at least two modules need the same behavior,
2. the behavior is stable,
3. the abstraction reduces code and improves readability,
4. the module boundaries stay clear.

## File Responsibility Rules

Backend:

- one aggregate root per file
- one command/query per folder
- one handler per use case
- one validator per command when validation is non-trivial
- one repository interface per aggregate or query model
- one EF configuration per entity
- endpoint files grouped by module
- no large `Helpers.cs` or `Utils.cs` files

Frontend:

- pages compose workflows
- components render UI
- hooks coordinate screen state and API calls
- API files call HTTP and return typed results
- mappers convert API DTOs to frontend view models
- validation files contain form schemas/rules
- state files contain module state only
- no page should directly know low-level HTTP details

## Change Workflow

When adding a feature:

1. Update or create the planning note for the module.
2. Add or adjust domain entities/rules.
3. Add the application command/query/use case.
4. Add or adjust infrastructure persistence/integration.
5. Add the API endpoint and contract.
6. Add frontend API/types/mappers.
7. Add frontend hook and page/component.
8. Add focused tests around domain rules and use cases.
9. Update the tracker status.

This workflow keeps backend and frontend moving together without mixing responsibilities.

## Testing Shape

Backend:

- domain tests for business rules
- application tests for use cases
- infrastructure tests for persistence mappings and adapters
- API tests for endpoint behavior and auth boundaries

Frontend:

- component tests for important forms and tables
- hook tests for module logic where helpful
- API mock tests for screen workflows
- end-to-end tests only for critical flows after the first slice exists

## Architecture Guardrail

Before building any feature, ask:

```text
Which module owns this?
Which layer should this logic live in?
Is this shared because it is truly reused, or just because it feels tidy?
Does this support the first Control Desk vertical slice?
```

If the answer is unclear, write the decision down before coding.
