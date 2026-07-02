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
  -> payment receipt/review and GL posting
  -> entitlement/client status publish
  -> signed entitlement bundle issued or renewed
  -> SafarSuite access renewed, warned, restricted, or revoked by policy
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
| PostgreSQL payment persistence | Done | Invoice payment records are persisted for pending review, approval, rejection, and reversal; approved/reversed GL effects stay transactional |
| Client accounting profile | Basic done | Client can be linked to AR ledger account, default currency, and cloud customer identity |
| Ledger accounts | Partial | Create endpoint and activity view exist; accounts are persisted in PostgreSQL |
| Charge codes | Partial | Create/list endpoints exist and validate revenue/tax posting account mappings |
| Client charge rules | Partial | Client-specific dynamic charge rules exist with optional tax percent |
| Invoice draft generation | Done for basic charges/tax | Generates draft invoice from active client charge rules and materializes tax lines when configured |
| Invoice issue posting | Done for basic revenue/tax | Issues invoice and posts balanced AR/revenue/tax-payable journal in one transaction; AR can resolve from client accounting profile |
| Unpaid invoice voiding | Basic done | Unpaid issued invoices can be voided with a reversing journal and pending `InvoiceVoided` outbox event |
| Full credit notes | Basic done | Paid or partially paid invoices can receive one full credit note with reversing sale journal and pending `CreditNoteIssued` outbox event |
| Client refunds | Basic done | Client credit balances created by credit notes can be refunded with balanced AR/cash-bank GL posting and pending `ClientRefundIssued` outbox event |
| Client credit settlement | Basic done | Unapplied client credit can be allocated to issued/partially paid invoices without a new GL journal and with pending `ClientCreditApplied` outbox event |
| Cloud invoice outbox | Basic done | Invoice issue enqueues a pending persisted `InvoiceIssued` message, exposes an outbox read endpoint, and can be processed through signed publishing |
| Payment outbox events | Basic done | Approved receipt posting enqueues pending persisted `PaymentRecorded` and `ClientPaidStatusChanged`; reversal enqueues `PaymentReversed` and paid-status updates when needed |
| Local outbox publisher | Basic done | Manual dev endpoint builds signed envelopes and marks ready outbox messages `Sent` or `Failed` without calling the real cloud |
| Control Cloud publish envelope | Basic done | Invoice, payment, client paid-status, and entitlement payloads are wrapped in a signed v1 envelope with an idempotency key |
| HTTP Control Cloud publisher adapter | Contract ready | Config can switch publishing from local validation to HTTP delivery once the real cloud endpoint exists |
| Retry-safe outbox attempts | Basic done | Outbox rows track attempt count, last attempt time, next attempt time, sent/failed state, and retry eligibility |
| Local entitlement snapshots | Basic done | Paid invoices can issue persisted entitlement snapshots with local limits/modules and enqueue `EntitlementSnapshotIssued` |
| Contract-driven entitlement defaults | Basic done | Paid-invoice entitlement issue can derive paid-until, warning window, grace, offline validity, device/branch limits, and modules from the invoice contract |
| Control Cloud commercial projection | Basic done | Accepted Control Desk invoice/payment/credit/refund/settlement envelopes project into a PostgreSQL-backed cloud-owned client commercial summary for the Client Portal |
| Client Portal identity/session boundary | Basic contact invite foundation done | Control Desk can request portal invites for client contacts, Control Cloud protects invitation creation with a provider key, and Control Cloud owns portal invitations, password-backed users, and signed client-scoped sessions through `POST /api/v1/client-portal/invitations`, `/invitations/accept`, and `/sessions`; real provider users, MFA, email delivery, resend/revoke/list, and audit remain pending |
| Control Cloud entitlement signing | Basic done | Latest projected entitlement snapshot can be returned to the Client Portal as an installation-bound signed bundle with payload hash, key id, HMAC signature, bundle issue id, paid/grace/offline dates, warning start, module states, and limits; issue audit and installation registry state persist in PostgreSQL |
| Control Cloud installation commands | Basic done | Control Cloud can queue signed monotonic commands for registered installations; local servers can pull pending commands and acknowledge Applied/Failed/Rejected outcomes with persisted audit |
| Control Cloud/local-server heartbeat | Basic done | Local servers can report heartbeat to Control Cloud with heartbeat status stored separately from reported license state, entitlement version, paid/grace/offline dates, and local-server version |
| Control Cloud installation status view | Basic portal preview done | Shared status endpoint returns installation identity, latest heartbeat/license state, latest entitlement bundle issue, pending command count, and latest command acknowledgement summary; the Control Desk client page has a minimal manual refresh panel, and Control Cloud serves a minimal Client Portal preview at `/client-portal/index.html` |
| Local server entitlement verification | Basic done | Local-server layers can pull the latest signed bundle from Control Cloud, import HMAC-signed entitlement bundles, reject bad signatures and older versions, cache the latest accepted bundle, gate module access through active, warning, grace, restricted, expired, and module-disabled states, and report the current license state during heartbeat |
| Payment posting | Done for local review loop | Records persisted payment, supports pending bank-transfer review, posts balanced cash/AR journal on approval, and posts balanced reversal journals |
| Client billing setup UI | Basic done | Client desk includes accounting profile, charge/tax rules, invoice draft, invoice issue, unpaid invoice void, and full credit-note workflow |
| Client payment and entitlement UI | Basic done | Client desk includes payment receipt, bank-transfer approval/rejection, approved-payment reversal, credit settlement, client refund, and local entitlement issue/refresh workflow |
| Accounting visibility | Partial | Journal list, ledger activity, and client statement endpoints exist and read PostgreSQL-backed invoices, payments, and journal entries |
| Client statement/receivables view | Basic done | Client desk can reconcile invoices, voided invoices, credit notes, applied credit, client refunds, approved/reversed payments, available credit, running balance, and related journal postings for the selected client |

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

## Offline Entitlement Rule

The active product rule is:

```text
heartbeat status != license validity
```

A paid offline-capable monthly client should not be disturbed just because the local server missed heartbeat while the signed entitlement is still inside the paid period.

See the canonical rule note:

```text
docs/planning/offline-entitlement-control-rules.md
```

The implementation must support:

- signed entitlement bundles verified locally
- paid-until, warning-start, grace-until, and product/module limits
- heartbeat when internet is available
- command queue and acknowledgement for renew/revoke/change-limit actions
- offline renewal file import when the local server cannot connect near expiry
- trust-based lease lengths for normal vs high-risk clients
- clock/replay protection
- full audit trail for every entitlement and command action

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
15. Done: add payment review and reversal foundation for bank transfers, approval/rejection, reversal GL posting, and statement visibility.
16. Done: add billing tax foundation so taxable charge rules create invoice tax lines and balanced tax-payable journal credits.
17. Done: add unpaid issued-invoice voiding with reversing GL journal, statement visibility, and `InvoiceVoided` outbox event.
18. Done: add full credit-note foundation for paid/partially paid invoice correction.
19. Done: add client refund controls to clear credit balances created by credit notes.
20. Done: add settlement controls to apply client credit balances to future invoices.
21. Done: harden the local accounting chain. The repeatable smoke runner now covers invoice issue, payment, credit note, refund, second invoice, and credit settlement through application handlers; both in-memory and PostgreSQL provider modes have passed, and the client desk now has basic disabled-state and confirmation guardrails around accounting-impacting actions.
22. Done: scaffold the SafarSuite Control Cloud receiver skeleton. It accepts signed Control Desk envelopes at `POST /api/v1/control-desk/messages`, validates payload hash/HMAC, persists accepted/rejected/duplicate receipt status, and returns stable cloud message references.
23. Done: wire Control Desk HTTP publisher mode to the local Control Cloud receiver. Development config points to `http://localhost:5127/api/v1/control-desk/messages`, and the accounting smoke runner can publish seven real outbox rows through the receiver.
24. Done: create cloud-side projections for accepted invoice/payment/credit/refund/entitlement messages so the Client Portal can read cloud-owned state. The local receiver now updates a portal-readable commercial summary endpoint from accepted envelopes.
25. Done: replace local Control Cloud projection/receipt files with PostgreSQL persistence. Development Control Cloud now stores receipts and client commercial projections in the `cloud` schema, and accepted envelope projection plus receipt insert happen in one EF transaction.
26. Done: start the cloud entitlement signing boundary. The Client Portal can request a session-protected signed bundle at `GET /api/v1/client-portal/clients/{clientId}/entitlement-bundle`, and local servers pull the machine-facing bundle through `GET /api/v1/local-server/installations/{installationId}/entitlement-bundle?clientId={clientId}`.
27. Done: harden signed offline entitlement bundle issue with persisted issue audit, installation registry/binding, required installation id, monotonic entitlement version rejection, and signed bundle issue ids.
28. Done: add cloud command queue and local-server acknowledgement contracts. Control Cloud can queue signed monotonic commands through `POST /api/v1/control-cloud/clients/{clientId}/installations/{installationId}/commands`, local servers can pull pending commands through `GET /api/v1/local-server/installations/{installationId}/commands/pending`, and acknowledgements are persisted through `POST /api/v1/local-server/installations/{installationId}/commands/{commandId}/acknowledgement`.
29. Done: add local-server entitlement import, signature verification, cache, and feature-gating rules. `SafarSuite.LocalServer` now has Domain/Application/Infrastructure layers plus `tools/SafarSuite.LocalServer.EntitlementSmoke` covering valid import, bad-signature rejection, older-version rejection, and active/warning/grace/restricted/expired/module-disabled gate decisions.
30. Done: add direct Control Cloud entitlement pull over HTTP using the local-server signed bundle endpoint and the local import verifier/cache.
31. Done: add heartbeat endpoint and local-server heartbeat state reporting. Control Cloud accepts `POST /api/v1/local-server/installations/{installationId}/heartbeat`, persists heartbeat records, and the local server reports cached entitlement/license state separately from heartbeat receipt status.
32. Done for Control Desk and portal visibility: add shared Control Cloud installation status for installation heartbeat, reported license state, pending commands, latest entitlement, and latest command acknowledgement summary; Control Desk can refresh it from the client page, and the Client Portal preview can read it through the portal namespace.
33. Done: add the first Client Portal identity/session boundary. Control Cloud now stores client portal invitations and users, accepts one-time invitation tokens, hashes passwords, and mints signed client-scoped sessions.
34. Done: wire basic provider-key authorization and Control Desk contact-level invite action so invitations are created from the client maintenance workflow.
35. Next: add email delivery plus invitation resend/revoke/list and invite/session audit.
36. Next after identity hardening: add offline renewal file import/export as a fallback for sites that cannot connect near expiry.

## Guardrails

- No more Survey/FAS clone work unless it becomes a paid/current requirement again.
- Client maintenance is the product center; supporting modules must tie back to client control.
- No cloud HTTP call inside accounting transactions.
- Every multi-write accounting action uses `ExecuteInTransactionAsync`.
- Bank transfers stay pending until reviewed; approval and reversal each own their GL transaction.
- Taxable charge rules require charge codes with tax payable accounts; invoice issue must stay balanced across AR, revenue, and tax payable.
- Only unpaid issued invoices can be voided directly; paid or partially paid invoice corrections use credit notes, then refund or settlement.
- Keep manual screens minimal until the core chain is proven.
- PostgreSQL persistence comes before production-like testing.
- Control Cloud persistence owns its own `cloud` schema even while local development uses the same PostgreSQL container/database.
- Client Portal reads cloud-owned projections, not the Control Desk operational database.
- Client Portal commercial/license/deployment reads require a client-scoped portal session.
- Portal invitation creation is protected by a local provider key for now; production use must replace that with real provider/admin users, email delivery, expiry/audit, and role management.
- Heartbeat status and license validity remain separate; missed heartbeat alone must not disturb a paid offline client.
- Warnings start near `paid_until`; grace and restriction begin only after configured dates.
- Offline renewal files and emergency unlocks must be signed, installation-bound, versioned, expiring, and audited.
- Revocation can be immediate only for installations that can receive the command; otherwise it takes effect on next heartbeat, next renewal file import, or the next license boundary.
