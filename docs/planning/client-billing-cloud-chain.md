# Client Billing Cloud Chain

Date started: 2026-06-30

This is the corrected product path for SafarSuite Control Desk.

SurveyValuation is paused. The working goal is the client-control chain:

```text
client setup
  -> accounting profile
  -> charges/contract
  -> invoice draft
  -> invoice issue and GL posting
  -> cloud invoice publish
  -> payment receipt and GL posting
  -> entitlement/client status publish
  -> SafarSuite access renewed or restricted
```

## What Is Already Usable

| Capability | Status | Notes |
| --- | --- | --- |
| Client master maintenance | Basic done | API and active frontend support create, list, detail, edit, activate, suspend, contacts, and support notes |
| PostgreSQL client persistence | Done | Docker Compose and EF Core migration persist clients, contacts, and support notes |
| PostgreSQL contract persistence | Done | Active client contracts and module allowances are persisted in PostgreSQL |
| Client contract maintenance | Basic done | API and active frontend can create, read/list, suspend, and replace active client contracts |
| PostgreSQL accounting persistence | Done | Ledger accounts, journal entries, and journal lines are persisted in PostgreSQL |
| PostgreSQL billing/profile/outbox persistence | Done | Client accounting profiles, charge codes, client charge rules, invoices, invoice lines, and cloud outbox messages are persisted in PostgreSQL |
| PostgreSQL payment persistence | Done | Approved invoice payment records are persisted with invoice balance/status update and receipt journal posting in one transaction |
| Client accounting profile | Basic done | Client can be linked to AR ledger account, default currency, and cloud customer identity |
| Ledger accounts | Partial | Create endpoint and activity view exist; accounts are persisted in PostgreSQL |
| Charge codes | Partial | Create/list endpoints exist and link to revenue/tax accounts |
| Client charge rules | Partial | Client-specific dynamic charge rules exist |
| Invoice draft generation | Done for basic charges | Generates draft invoice from active client charge rules |
| Invoice issue posting | Done for basic revenue | Issues invoice and posts balanced AR/revenue journal in one transaction; AR can resolve from client accounting profile |
| Cloud invoice outbox | Basic done | Invoice issue enqueues a pending persisted `InvoiceIssued` message, exposes an outbox read endpoint, and can be processed through signed publishing |
| Payment outbox events | Basic done | Approved receipt posting enqueues pending persisted `PaymentRecorded` and `ClientPaidStatusChanged` messages |
| Local outbox publisher | Basic done | Manual dev endpoint builds signed envelopes and marks ready outbox messages `Sent` or `Failed` without calling the real cloud |
| Control Cloud publish envelope | Basic done | Invoice, payment, client paid-status, and entitlement payloads are wrapped in a signed v1 envelope with an idempotency key |
| HTTP Control Cloud publisher adapter | Contract ready | Config can switch publishing from local validation to HTTP delivery once the real cloud endpoint exists |
| Retry-safe outbox attempts | Basic done | Outbox rows track attempt count, last attempt time, next attempt time, sent/failed state, and retry eligibility |
| Local entitlement snapshots | Basic done | Paid invoices can issue persisted entitlement snapshots with local limits/modules and enqueue `EntitlementSnapshotIssued` |
| Contract-driven entitlement defaults | Basic done | Paid-invoice entitlement issue can derive paid-until, grace, offline validity, device/branch limits, and modules from the invoice contract |
| Payment posting | Done for approved payment methods | Records persisted payment, updates invoice, posts balanced cash/AR journal |
| Client billing setup UI | Basic done | Client desk includes accounting profile, charge rules, invoice draft, and invoice issue workflow |
| Client payment and entitlement UI | Basic done | Client desk includes approved payment receipt plus local entitlement issue/refresh workflow |
| Accounting visibility | Partial | Journal list, ledger activity, and client statement endpoints exist and read PostgreSQL-backed invoices, payments, and journal entries |
| Client statement/receivables view | Basic done | Client desk can reconcile invoices, approved payments, running balance, and related journal postings for the selected client |

## Correct Accounting Shape

Invoices should be generated from billing rules, charge codes, contracts, and one-off charges.

The ledger should not generate invoices. The ledger records what happened after invoice issue and payment posting.

The bridge now exists in basic form as a client accounting profile:

| Concept | Purpose |
| --- | --- |
| `ClientAccountingProfile` | Accounting identity and posting defaults for a client |
| `AccountsReceivableAccountId` | AR control/receivable account used when issuing invoices |
| `DefaultCurrencyCode` | Default invoice and payment currency |
| `CloudCustomerId` | External identity used by SafarSuite Control Cloud / Client Portal |
| `PostingProfile` later | Revenue, tax, discount, receivable, and cash/bank defaults |

## Cloud Publishing Rule

Do not call SafarSuite Control Cloud inside invoice/payment transactions.

Use an outbox:

```text
issue invoice transaction
  -> update invoice
  -> create journal entry
  -> enqueue CloudOutboxMessage: InvoiceIssued

publisher process
  -> sends invoice to SafarSuite Control Cloud
  -> marks outbox sent or failed
```

Same rule for payment and entitlement events.

## Next Implementation Slices

0. Done: add client detail, edit, activate, and suspend actions; remove Survey/FAS routes from active API mapping.
0.1. Done: add internal support notes/history to client maintenance.
0.2. Done: add structured client contacts with role and primary-contact handling.
1. Done: add `ClientAccountingProfile` domain/application/API/PostgreSQL persistence.
2. Done: update client setup flow so a client can be linked to AR/default currency/cloud identity.
3. Done: use the profile during invoice issue so AR account does not have to be manually provided every time.
4. Done: add `CloudOutboxMessage` domain/application/API read model.
5. Done: enqueue `InvoiceIssued` cloud message inside invoice issue transaction.
5.1. Done: persist charge codes, client charge rules, invoices, invoice lines, client accounting profiles, and cloud outbox messages in PostgreSQL.
5.2. Done: persist approved invoice payment records in PostgreSQL with invoice balance/status and receipt journal in one transaction.
6. Done: add `PaymentRecorded` / `ClientPaidStatusChanged` outbox messages after receipt posting.
7. Done: add a fake/local cloud publisher that marks messages as sent for development.
8. Done: add local entitlement snapshot issue from paid invoice.
9. Done: add contract-driven entitlement defaults so devices, branches, modules, paid-until, and grace rules no longer need manual request values.
10. Done for backend: add contract maintenance API for list/read/suspend/replace active contract.
11. Done: connect contract setup and maintenance into the client UI.
12. Done: add minimal client billing setup UI for accounting profile, charge rules, invoice draft, and invoice issue.
13. Done: add a client statement/receivables view that reconciles invoices, payments, balance due, and journal postings from the client desk.
14. Done: add cloud-readiness contracts for invoice/payment/entitlement publishing: signed payload envelope, publisher interface, retry-safe status handling, and environment configuration.
15. Before touching real cloud service code, confirm the cloud boundary; next cloud-side slice is a SafarSuite Control Cloud receiver skeleton with signature validation, idempotency handling, and persisted receipt status.

## Guardrails

- No more Survey/FAS clone work unless it becomes a paid/current requirement again.
- Client maintenance is the product center; supporting modules must tie back to client control.
- No cloud HTTP call inside accounting transactions.
- Every multi-write accounting action uses `ExecuteInTransactionAsync`.
- Keep manual screens minimal until the core chain is proven.
- PostgreSQL persistence comes before production-like testing.
