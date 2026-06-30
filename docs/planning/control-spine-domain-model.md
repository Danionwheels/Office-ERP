# Control Spine Domain Model

Date started: 2026-06-30

This is the first domain model for SafarSuite Control Desk. It supports the initial vertical slice only:

```text
client -> client accounting profile -> contract/pricing -> invoice -> cloud publish -> payment -> entitlement -> active/paid status
```

It is intentionally not the full Survey/FAS clone model. SurveyValuation work is paused because it does not support the current product goal. A minimal accounting foundation now exists because dynamic client billing needs ledger-ready invoice and receipt behavior.

The active API surface is now client-centered. SurveyValuation code remains in the repository as historical reference only, but its routes and dependency registrations are not mapped into the running API.

## Current Modules

| Module | Current responsibility | Key types |
| --- | --- | --- |
| Clients | SafarSuite customer/company record and lifecycle plus accounting defaults | `Client`, `ClientId`, `ClientCode`, `ClientStatus`, `SupportNote`, `ClientAccountingProfile` |
| Contracts | commercial terms, pricing, modules, devices, branches | `ClientContract`, `ContractPricing`, `ModuleAllowance`, `DeviceAllowance`, `BranchAllowance` |
| Accounting | chart of accounts and balanced journal postings | `LedgerAccount`, `LedgerAccountCode`, `JournalEntry`, `JournalLine` |
| Billing | invoice lifecycle, invoice balances, charge catalog, dynamic charge rules | `Invoice`, `InvoiceLine`, `InvoiceNumber`, `InvoiceStatus`, `ChargeCode`, `ClientChargeRule` |
| Payments | payment capture/review/reversal decisions | `Payment`, `PaymentReference`, `PaymentMethod`, `PaymentStatus` |
| Entitlements | signed-access snapshot shape before cloud signing | `EntitlementSnapshot`, `EntitlementModule`, `EntitlementStatus` |
| ControlCloud | reliable publication of invoices, client snapshots, payment state, and entitlement commands | `CloudOutboxMessage`, `CloudOutboxMessageStatus` |
| Audit | durable business action history | `AuditEvent` |

## Shared Kernel

| Type | Purpose |
| --- | --- |
| `Entity<TId>` | base identity holder for aggregate/entity roots |
| `ValueObject` | equality base for immutable value objects |
| `Money` | amount plus currency, with same-currency add/subtract |
| `DateRange` | inclusive start/end date range |

## Aggregate Boundaries

Initial aggregate roots:

- `Client`
- `ClientAccountingProfile`
- `ClientContract`
- `LedgerAccount`
- `JournalEntry`
- `Invoice`
- `ChargeCode`
- `ClientChargeRule`
- `Payment`
- `EntitlementSnapshot`
- `CloudOutboxMessage`
- `AuditEvent`

Planned next roots/value objects:

- payment/client status outbox payloads

These are separate because they have different lifecycles:

- a client can exist before a contract
- a client accounting profile has its own setup lifecycle and points to reusable ledger accounts
- contracts can be revised or replaced
- ledger accounts must have their own lifecycle and can be reused by many posting profiles
- journal entries are durable accounting records and must balance before posting
- invoices and payments need separate accounting/reconciliation history
- charge codes are reusable billable items
- client charge rules are client-specific commercial rules that can change over time
- entitlement snapshots are issued records, not editable contract state
- cloud outbox messages are durable publish work items created by local business transactions
- audit events are append-only history

## Important Rules Captured

Clients:

- client code is required, normalized uppercase, and between 3 and 32 characters
- client legal name is required
- client legal/display names can be maintained after creation
- support notes require text and an author
- draft clients can be activated or suspended

Contracts:

- contract number is required and normalized uppercase
- billing day is restricted to 1-28 to avoid month-end edge cases
- device and branch allowances cannot be negative
- at least one module allowance is required before activation
- module allowance updates replace the previous allowance for the same module code

Accounting:

- ledger account code is required, normalized uppercase, and between 2 and 32 characters
- ledger account name is required
- journal line amount must be positive
- journal line currency must match the journal entry currency
- journal entries must have at least two lines before posting
- journal entries must include debit and credit amounts before posting
- journal entry total debit must equal total credit before posting
- only posted journal entries can be voided

Billing:

- due date cannot be before issue date
- invoice line amounts cannot be negative
- invoice lines may optionally reference a charge code
- charge code key is required, normalized uppercase, and between 2 and 32 characters
- charge code default unit price cannot be negative
- client charge unit price cannot be negative
- client charge quantity must be positive
- client charge billing day is restricted to 1-28
- active client charge rules must be effective on the billing date before invoice generation
- only draft invoices can be edited or issued
- invoice issue can resolve the receivable account from the client accounting profile
- issued invoices can become partially paid or paid as payments are applied
- paid invoices cannot be cancelled

Payments:

- payment amount must be positive
- bank transfer payments start as `PendingReview`
- non-bank-transfer payments start as `Approved` for now
- the first invoice-payment posting use case only posts approved payment methods
- payment amount cannot exceed the invoice balance in the first control-spine flow
- rejected and reversed payments require decision notes
- only approved payments can be reversed

Entitlements:

- grace date cannot be before paid-until date
- offline-valid-until cannot be before paid-until date
- device and branch limits cannot be negative
- at least one module is required
- active entitlement allows use until paid-until
- grace entitlement allows use until grace-until

Audit:

- module, action, subject, actor, and summary are required
- audit events are modeled as immutable records

## Application Ports

Repository ports now exist in the Application layer:

- `IClientRepository`
- `IClientAccountingProfileRepository`
- `ILedgerAccountRepository`
- `IJournalEntryRepository`
- `IChargeCodeRepository`
- `IClientChargeRuleRepository`
- `IContractRepository`
- `IInvoiceRepository`
- `IPaymentRepository`
- `IEntitlementSnapshotRepository`
- `ICloudOutboxMessageRepository`
- `IAuditEventRepository`

The current Infrastructure layer uses temporary in-memory implementations for active local slices. PostgreSQL/EF implementations belong later in Infrastructure.

## Revised Product Chain

The core product chain is:

```text
create client
  -> create/link client accounting profile
       receivable account / AR control setup
       default currency
       cloud customer identity
  -> configure charges and contract allowances
  -> generate invoice draft from charges
  -> issue invoice in one transaction
       update invoice
       create posted journal entry
       enqueue cloud invoice publish message
  -> cloud publisher sends invoice/client balance to SafarSuite Control Cloud
  -> client portal shows invoice
  -> record/receive payment
       post receipt journal
       update invoice balance/status
       enqueue paid-status/entitlement publish message
  -> cloud issues signed entitlement/product command
  -> SafarSuite client becomes active/renewed
```

Invoices are generated from billing rules and charge setup, then posted into the ledger. The ledger is the accounting record and client balance source; it should not be the invoice generator itself.

## Transaction Boundary

`IUnitOfWork` now has two persistence boundaries:

| Method | Use |
| --- | --- |
| `SaveChangesAsync` | One action writes one aggregate/table |
| `ExecuteInTransactionAsync` | One action writes multiple aggregates/tables and must be atomic |

Invoice issue with GL posting uses `ExecuteInTransactionAsync` because it updates the invoice and creates a journal entry in the same business action. Invoice payment posting follows the same rule because it records the payment, updates the invoice balance, and creates a journal entry together.

Cloud publishing must not happen directly inside the accounting transaction. Invoice issue/payment approval should create a durable outbox message in the same transaction. A separate publisher sends it to SafarSuite Control Cloud and records sent/failed status.

## Current API Surface

The backend now exposes these control-spine create endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/clients` | Create a SafarSuite client |
| `GET` | `/api/v1/clients` | List SafarSuite clients |
| `GET` | `/api/v1/clients/{clientId}` | Read client details |
| `PUT` | `/api/v1/clients/{clientId}` | Update client legal/display name |
| `POST` | `/api/v1/clients/{clientId}/activate` | Activate a client |
| `POST` | `/api/v1/clients/{clientId}/suspend` | Suspend a client |
| `POST` | `/api/v1/clients/{clientId}/support-notes` | Add an internal client support note |
| `GET` | `/api/v1/clients/{clientId}/support-notes` | List internal client support notes newest-first |
| `PUT` | `/api/v1/clients/{clientId}/accounting-profile` | Link a client to AR ledger account, default currency, and cloud customer identity |
| `GET` | `/api/v1/clients/{clientId}/accounting-profile` | Read a client's accounting profile |
| `POST` | `/api/v1/accounting/ledger-accounts` | Create a ledger account |
| `POST` | `/api/v1/billing/charge-codes` | Create a charge code tied to posting accounts |
| `POST` | `/api/v1/billing/client-charge-rules` | Create a client-specific dynamic charge rule |
| `POST` | `/api/v1/billing/invoice-drafts` | Generate a draft invoice from active client charge rules |
| `POST` | `/api/v1/billing/invoices/{invoiceId}/issue` | Issue a draft invoice and post a balanced GL journal entry, resolving AR from the client profile when omitted |
| `POST` | `/api/v1/payments/invoice-payments` | Record an approved invoice payment and post a balanced receipt journal entry |
| `GET` | `/api/v1/accounting/journal-entries` | List journal entries with optional date/source filters |
| `GET` | `/api/v1/accounting/ledger-accounts/{ledgerAccountId}/activity` | Show ledger account activity with running balance |
| `GET` | `/api/v1/control-cloud/outbox-messages` | List cloud outbox messages with optional status/message type filters |

## Explicitly Deferred

- accounting persistence and migrations
- tax payable posting during invoice issue
- bank-transfer review and approval flow
- payment reversal journal posting use case
- persistence-backed accounting reports
- payment settlement batches
- product-kernel command signing
- cloud publisher implementation
- PostgreSQL-backed outbox durability
- client portal mirror models
- SurveyValuation docket model and screens
- legacy report parity

These remain important, but they should not enter the first slice until the control spine is stable.
