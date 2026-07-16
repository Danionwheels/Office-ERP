# Client First Control Flow

Status: Superseded by `docs/architecture/product-charter-2026-07-11.md`. Keep as exploratory UI history only. Do not use this note to select or design new work.

Date added: 2026-07-10

Purpose: make SafarSuite Control Desk easy to understand, easy to operate per client, and aligned around one clear communication chain from Control Desk to Control Cloud to the installed Local Server.

This note is the design contract for the next UI and workflow pass. The product should feel like client management software first, not an accounting system with customer records attached.

## Recommendation

Yes: reduce the visible GL complexity.

Do not delete the accounting engine immediately. Keep the journal/posting model underneath for audit, invoices, receipts, reversals, credit notes, refunds, and statements. But the operator should mostly see simple, business-named vouchers and client activity.

The visible accounting shape should become:

```text
Client activity
  -> invoice voucher
  -> receipt voucher
  -> credit note voucher
  -> refund voucher
  -> adjustment voucher
  -> statement and audit proof
```

The operator should not need to think in terms of chart hierarchy, retained earnings, close artifacts, trial balance, or repair plans during normal client work. Those can stay in an admin/accounting setup area for later power users.

## Product North Star

Every daily action starts from a client.

```text
select client
  -> see current status
  -> complete setup gaps
  -> bill or receive payment
  -> publish to cloud
  -> renew or restrict access
  -> confirm local server state
```

The app should always answer five simple questions:

1. Who is the client?
2. What have we sold them?
3. What have we billed and received?
4. What access should they have now?
5. Is their installed system connected, healthy, and using the latest entitlement?

## Simplified Screen Model

| Screen | Job | What the operator sees |
| --- | --- | --- |
| Command Center | Daily work queue | clients needing setup, unpaid invoices, pending bank reviews, cloud/install warnings |
| Client 360 | One client home | profile, contacts, contract, balance, entitlement, deployment status, support notes |
| Commercial | Client money flow | plan/contract, charges, invoice, payment, credit, refund |
| Voucher Register | Simple proof of financial actions | invoice vouchers, receipt vouchers, credit/refund vouchers, adjustments |
| Cloud & Installation | Runtime support for the selected client | setup token, bootstrap package, handoff, heartbeat, diagnostics, commands |
| Setup | Reusable definitions | product modules, charge templates, default accounts, deployment defaults |
| Admin Accounting | Advanced accounting only | chart, periods, reports, reconciliation, repair/import tools |

The old Accounting Desk should not be a required stop in the normal client lifecycle. Normal users should live in Client 360, Commercial, Voucher Register, and Cloud & Installation.

## Client Lifecycle

### 1. Create Client

```text
operator creates client
  -> add contacts
  -> choose deployment profile
  -> choose product/modules
  -> set billing plan
```

Required client setup should be visible as a checklist:

| Setup item | Needed before |
| --- | --- |
| client profile | any client work |
| billing contact | portal invite and invoices |
| active contract/modules | invoice and entitlement |
| default receivable/accounting profile | invoice issue |
| deployment profile | setup token/bootstrap package |
| provider/cloud access ready | cloud provisioning |

### 2. Prepare Contract And Charges

```text
active contract
  -> module allowances
  -> charge rules
  -> draft invoice
```

The operator should not create ledger accounts during daily billing unless a required default is missing. Missing defaults should appear as setup gaps with one suggested fix.

### 3. Create Invoice

```text
generate draft invoice
  -> review lines
  -> issue invoice
  -> system creates invoice voucher
  -> system queues InvoiceIssued cloud message
```

The visible result should be an invoice voucher:

| Voucher field | Meaning |
| --- | --- |
| voucher number | human reference |
| client | billed client |
| source | invoice number |
| amount | invoice total |
| status | draft, issued, voided, paid, credited |
| cloud state | pending, sent, failed |
| proof | hidden journal lines available on demand |

### 4. Record Payment

```text
record receipt
  -> if bank transfer, mark pending review
  -> approve or reject
  -> system creates receipt voucher
  -> system queues PaymentRecorded and paid-status cloud messages
```

Payment review should be an operator queue, not an accounting screen.

### 5. Issue Entitlement

```text
paid invoice
  -> issue entitlement from contract defaults
  -> system creates entitlement snapshot
  -> system queues EntitlementSnapshotIssued cloud message
```

The operator should see:

| Field | Meaning |
| --- | --- |
| paid until | normal access date |
| warning starts | renewal warning date |
| grace until | still allowed but overdue |
| offline valid until | final offline validity boundary |
| modules | enabled/disabled module list |
| devices/branches | limits from contract |

### 6. Publish To Cloud

```text
local transaction writes business result
  -> CloudOutboxMessage is created
  -> publisher signs envelope
  -> Control Cloud receives envelope
  -> Control Cloud stores receipt
  -> Control Cloud updates portal/client projection
```

This rule stays mandatory:

```text
no Control Cloud HTTP call inside invoice/payment/entitlement transaction
```

The UI should make outbox state obvious but not scary:

| State | Operator wording |
| --- | --- |
| Pending | waiting to send |
| Sent | cloud updated |
| Failed retryable | will retry |
| Failed final | needs support |

### 7. Cloud To Local Server

```text
Control Cloud has latest entitlement
  -> Local Server pulls signed bundle
  -> Local Server verifies signature and installation id
  -> Local Server caches entitlement
  -> SafarSuite app asks Local Server for module access
```

Heartbeat is not the license.

```text
heartbeat = communication visibility
signed entitlement = local access authority
```

The local app must keep working when internet is down if the cached signed entitlement is still valid.

## Communication Lanes

Keep these lanes mentally and technically separate.

| Lane | Direction | Purpose | Source of truth |
| --- | --- | --- | --- |
| Commercial outbox | Control Desk -> Control Cloud | invoices, payments, credits, refunds, entitlement snapshots | Control Desk |
| Portal projection | Control Cloud -> Client Portal | client-visible invoice/license/deployment summaries | Control Cloud projection |
| Entitlement pull | Local Server -> Control Cloud | latest signed installation-bound entitlement | Control Cloud |
| Heartbeat | Local Server -> Control Cloud | last seen, local license state, runtime version, pairing snapshot | Local Server report |
| Commands | Control Cloud -> Local Server via local pull | diagnostics, refresh entitlement, revoke app activation | Control Cloud command queue |
| Diagnostics | Local Server -> Control Cloud | support bundle and runtime facts | Local Server report |
| Operational data sync | future separate plane | branch/HQ/cloud business records | SafarSuite app/data-sync plane |

Do not put operational business data sync into the billing/license cloud channel.

## Client 360 Target Layout

The selected client page should have one top status strip:

| Tile | Shows |
| --- | --- |
| Client | active/suspended, contacts complete |
| Balance | due, credit, pending review |
| Contract | active plan, modules, renewal date |
| Access | entitlement state, paid/grace/offline dates |
| Installation | registered, heartbeat, diagnostics, pending commands |
| Cloud | pending outbox, failed sends |

Then the page should expose action tabs:

1. Overview
2. Contract
3. Billing
4. Payments
5. Vouchers
6. Access
7. Cloud Install
8. Support Notes
9. Audit

The operator should never have to leave the selected client context to complete a normal client task.

## Voucher View Contract

The voucher register is the replacement for the complicated day-to-day GL view.

Voucher types:

| Voucher type | Created by |
| --- | --- |
| Invoice Voucher | invoice issue |
| Receipt Voucher | approved payment |
| Credit Note Voucher | credit note issue |
| Refund Voucher | refund issue |
| Settlement Voucher | credit settlement, if proof is needed |
| Adjustment Voucher | manual correction, restricted |

Each voucher should show:

- voucher number/reference
- client
- date
- source document
- amount and currency
- status
- cloud publish state
- created by / approved by
- audit trail
- expandable posting lines for accountant/support only

This keeps accounting proof without making GL structure the main product experience.

## What To Hide From Normal Flow

Move these out of daily client management:

- chart of accounts hierarchy
- account-code ranges
- reconciliation repair plans
- retained earnings and period close details
- trial balance, profit and loss, balance sheet
- manual journal editor
- opening balance import

These should remain available under Admin Accounting, behind a clear advanced/admin boundary.

## Implementation Sequence

1. Document this client-first flow and treat it as the UI target.
2. Rename the visible accounting experience from Accounting Desk to Vouchers or Voucher Register for normal users.
3. Build Client 360 as the real home for one selected client.
4. Move billing, payment, entitlement, and cloud status into Client 360 tabs.
5. Add a voucher read model that groups invoice/payment/credit/refund proof by client.
6. Keep existing journal entries under the hood, but expose them only as expandable proof.
7. Add cloud communication status cards per client: outbox, last publish, latest cloud projection, local heartbeat, latest diagnostics, pending commands.
8. Move advanced GL tools into Admin Accounting.
9. Add operator wording everywhere: "Send to Cloud", "Refresh Local Access", "Request Diagnostics", "Issue Renewal", "Record Receipt".
10. After the UI is clean, harden authorization around local operator and provider/cloud actions.

## Implementation Notes

2026-07-11:

- Added a Client 360 workspace to the Control Desk shell.
- Added a read-only voucher register generated from invoices, payments, credits, and refunds in the client statement.
- Added first Client 360 write actions:
  - draft invoice voucher from the active contract
  - issue invoice voucher through the existing billing posting API
  - record receipt voucher against an open invoice through the existing payment API
  - issue access renewal from a paid invoice through the existing entitlement API
- Made the Client 360 Cloud tab action-oriented:
  - Send to Cloud from the selected client context
  - show pending and failed client updates
  - show last send result
  - show latest local heartbeat, local access version, and pending support commands
- Added a Client 360 Setup tab for common setup gaps:
  - add the first billing/support contact
  - save client accounting profile
  - create the client contract
  - save the local server deployment profile
- Added a guided Next Required Action on Client 360 Overview:
  - routes setup blockers to Setup
  - routes draft/issue invoice work to Billing
  - routes open invoice collection to Payments
  - routes paid invoice access renewal to Access
  - routes pending cloud updates and local heartbeat checks to Cloud
- Added a lightweight Overview progress trail:
  - shows the last completed client event beside the next pending action
  - derives recent proof from setup records, vouchers, access renewals, sent cloud updates, and local heartbeat
- Added support note creation inside Client 360 Notes and included support notes in the Overview progress trail.
- Added portal invitation handling inside Client 360 Notes:
  - invite a client contact to the portal
  - resend or revoke portal invitations
  - include portal invite events in the Overview progress trail
- Added client master actions inside Client 360:
  - create a new client from the Client 360 command area
  - edit the selected client's legal and display name in Setup
  - activate or suspend the client from the same setup flow
  - treat inactive client status as a Next Required Action blocker
- Removed Legacy Desk escape hatches from the Client 360 daily path:
  - Billing and Payments now route proof review to the Voucher Register
  - the global fallback is labeled Admin Desk instead of Legacy Desk
- Connected Command Center to Client 360:
  - Command Center now loads a client work queue
  - obvious blockers open the selected client directly in the Setup tab
  - normal review rows open the selected client in Overview so the Next Required Action can guide the operator
- Expanded the Command Center client queue signals:
  - missing local server deployment routes to Setup
  - no invoice routes to Billing
  - open invoice balance routes to Payments
  - paid invoice without access renewal routes to Access
  - pending or failed cloud updates route to Cloud
- Added Command Center queue filters with live counters:
  - All, Setup, Billing, Payments, Access, Cloud, and Review lanes
  - the selected lane filters the client work list without changing the Client 360 handoff
- Added Command Center queue search and sort:
  - search matches client code, name, status, action, detail, and target tab
  - sort can keep priority order or switch to client/action order
- Added Command Center queue freshness states:
  - shows the current lane and visible count
  - shows the last successful refresh time
  - lane counters show a loading state while refresh is running
- Added a Command Center queue summary band:
  - total clients loaded
  - setup blockers
  - money work
  - cloud work
  - review-ready/access work
- Trimmed the old static Command Center map:
  - removed planning bucket cards and sorting-lens copy
  - kept the compact operating path strip below the live client queue
- Renamed the remaining Command Center implementation classes from map language to command-center language.
- Extracted the Command Center queue model:
  - client classification, lane filters, sorting, summaries, and refresh labels now live outside the page component
  - the page keeps API loading and rendering while the flow rules stay easier to read and test
- Aligned the main sidebar to the client-first operating chain:
  - Command Center, Client 360, Client Money, Voucher Register, Cloud & Installation, Setup, Security, Audit, and Admin
  - old accounting-first wording now appears as voucher proof or advanced/admin wording instead of the daily path
- Kept GL/journal posting behind the billing and payment APIs instead of exposing it as the normal client workflow.

## Naming Guidance

Use business names in the UI:

| Technical term | UI term |
| --- | --- |
| CloudOutboxMessage | Cloud update |
| EntitlementSnapshot | Access renewal |
| JournalEntry | Voucher proof |
| LedgerAccount | Posting account |
| InstallationCommand | Support command |
| InstallationHeartbeat | Last seen |
| Signed entitlement bundle | Local access file |
| Client commercial projection | Cloud client summary |

## Non-Negotiable Rules

- One selected client is the center of daily work.
- Cloud publishing is asynchronous through the outbox.
- Cloud receives signed, idempotent messages.
- Client Portal reads Control Cloud projections, not Control Desk tables.
- Local Server pulls from Cloud; Cloud does not require inbound LAN access.
- Local license decisions come from cached signed entitlement, not heartbeat.
- Operational business data sync stays separate from billing/license/control.
- The visible UI should explain state in operator language, not database language.

## Success Test

A new operator should be able to say this after one pass through the app:

```text
I open a client.
I see what is missing.
I create or update their contract.
I generate and issue an invoice.
I record payment.
I issue access renewal.
I send pending updates to cloud.
I confirm the local server pulled the new access and is healthy.
```

If the operator has to understand full GL structure to complete that path, the design is still too complicated.
