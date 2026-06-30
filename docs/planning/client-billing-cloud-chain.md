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
| Client accounting profile | Basic done | Client can be linked to AR ledger account, default currency, and cloud customer identity |
| Ledger accounts | Partial | Create endpoint and activity view exist |
| Charge codes | Partial | Create/list endpoints exist and link to revenue/tax accounts |
| Client charge rules | Partial | Client-specific dynamic charge rules exist |
| Invoice draft generation | Done for basic charges | Generates draft invoice from active client charge rules |
| Invoice issue posting | Done for basic revenue | Issues invoice and posts balanced AR/revenue journal in one transaction; AR can resolve from client accounting profile |
| Cloud invoice outbox | Basic done | Invoice issue enqueues a pending `InvoiceIssued` message and exposes an outbox read endpoint |
| Payment posting | Done for approved payment methods | Records payment, updates invoice, posts balanced cash/AR journal |
| Accounting visibility | Partial | Journal list and ledger activity endpoints exist |

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
1. Done: add `ClientAccountingProfile` domain/application/API/in-memory persistence.
2. Done: update client setup flow so a client can be linked to AR/default currency/cloud identity.
3. Done: use the profile during invoice issue so AR account does not have to be manually provided every time.
4. Done: add `CloudOutboxMessage` domain/application/API read model.
5. Done: enqueue `InvoiceIssued` cloud message inside invoice issue transaction.
6. Add a fake/local cloud publisher that marks messages as sent for development.
7. Add `PaymentRecorded` / `ClientPaidStatusChanged` outbox messages after receipt posting.
8. Add entitlement snapshot issue/update once payment clears.

## Guardrails

- No more Survey/FAS clone work unless it becomes a paid/current requirement again.
- Client maintenance is the product center; supporting modules must tie back to client control.
- No cloud HTTP call inside accounting transactions.
- Every multi-write accounting action uses `ExecuteInTransactionAsync`.
- Keep manual screens minimal until the core chain is proven.
- PostgreSQL persistence comes before production-like testing.
