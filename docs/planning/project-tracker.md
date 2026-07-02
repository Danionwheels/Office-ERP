# SafarSuite Control Desk Project Tracker

Date started: 2026-06-30

Use this tracker to keep the project slow, deliberate, and on track.

Status values:

- Proposed
- Accepted
- In Progress
- Done
- Deferred

## Current Milestone

| Milestone | Goal | Status |
| --- | --- | --- |
| Milestone 1: Client Maintenance Spine | Maintain SafarSuite clients and only the accounting/billing/cloud support needed for client control | In Progress |

## Scope Rule

SafarSuite Control Desk is a client maintenance system first. Accounting, billing, payments, and cloud publishing stay active only when they support maintaining a client, charging that client, or controlling that client's access. Survey/FAS work is not part of the active product surface.

## Active Work

| Work item | Module | Status | Notes |
| --- | --- | --- | --- |
| Layered architecture contract | Architecture | Done | See `docs/architecture/layered-architecture.md` |
| Canonical product naming | Architecture | Done | See `docs/architecture/product-naming.md` |
| Cloud/local communication map | Architecture/ControlCloud/LocalServer | Done | See `docs/architecture/cloud-local-communication-map.md`; records Control Desk, Control Cloud, Client Portal, and SafarSuite local-server communication paths for license, heartbeat, command, and portal alignment |
| Legacy source reference | Legacy | Done | See `docs/legacy/source-reference.md` |
| Backend solution scaffold | Platform | Done | Created solution and projects for Api, Application, Domain, Infrastructure, Contracts |
| Frontend scaffold | Platform | Done | Vite React TypeScript app created under `apps/control-desk-ui` |
| Client maintenance UI | Clients/Frontend | Done | Active frontend now opens the client desk with list, create, detail, edit, lifecycle, support notes, and accounting-profile status |
| First domain model draft | Clients/Contracts/Billing/Payments/Entitlements/Audit | Done | Core aggregate roots and repository ports created |
| Client maintenance basics | Clients | Done | Client detail, edit, activate, and suspend API actions are wired |
| Client contacts | Clients | Done | Add/list structured contacts with roles and primary flag; active UI shows contacts |
| Client support notes | Clients | Done | Add/list internal client notes; client detail includes note history |
| PostgreSQL local database foundation | Persistence | Done | Docker Compose, local EF tool, EF DbContext, and initial migration are in place |
| Client maintenance persistence | Clients/Persistence | Done | PostgreSQL slice persists clients, contacts, and support notes |
| Client contract persistence | Contracts/Persistence | Done | PostgreSQL slice persists active client contracts and module allowances |
| Client contract maintenance | Contracts/Frontend | Basic done | Client desk can list contracts, create a contract, replace the active contract, and suspend active contracts |
| Accounting persistence | Accounting/Persistence | Done | PostgreSQL slice persists ledger accounts, journal entries, and journal lines |
| Billing/Profile/Outbox persistence | Billing/Clients/ControlCloud/Persistence | Done | PostgreSQL slice persists client accounting profiles, charge codes, client charge rules, invoices, invoice lines, and cloud outbox messages |
| Payment persistence | Payments/Persistence | Done | PostgreSQL slice persists invoice payments, including pending-review bank transfers, approved receipts, invoice balance/status updates, and receipt journals |
| Client billing setup UI | Billing/Frontend | Basic done | Client desk can maintain accounting profile, charge/tax setup, invoice draft, and invoice issue flow |
| Client payment and entitlement UI | Payments/Entitlements/Frontend | Basic done | Client desk can record invoice payments, review bank transfers, reverse approved payments, apply/refund client credit balances, and issue/refresh local entitlement snapshots |
| Client statement/receivables UI | Clients/Billing/Accounting/Frontend | Basic done | Client desk can show invoices, payments, credit notes, applied credit, client refunds, available credit, running receivable balance, and related journal postings |
| Accounting/GL correction sweep | Accounting/Billing | Done | Invoice creation split clarified in `accounting-gl-foundation-sweep.md` |
| Accounting/Billing foundation | Accounting/Billing | In Progress | Dynamic charge/tax setup, invoice draft generation, profile-assisted invoice issue GL posting, unpaid invoice voiding, full credit notes, client refunds, credit settlement, payment review/reversal, signed local outbox publishing, basic journal/ledger read models, client statement visibility, full-chain accounting smoke coverage, basic UI guardrails, the first Control Cloud receiver boundary, PostgreSQL-backed cloud commercial projection, installation-bound cloud entitlement bundle issue audit, signed installation command acknowledgements, local-server direct entitlement pull/verification/cache/feature gates, basic heartbeat reporting, a shared installation status endpoint, Control Desk cloud status panel, and minimal Client Portal status preview are wired; portal identity/payment UI, offline renewal fallback, and reports are still pending |
| Client accounting profile | Clients/Accounting | Done | Client can be linked to AR/default currency/cloud identity; invoice issue can resolve AR from the profile |
| Cloud invoice outbox | Billing/ControlCloud | Done for local loop | Invoice issue creates persisted `InvoiceIssued` messages, and the local publisher can mark signed pending outbox messages sent |
| Payment outbox events | Payments/ControlCloud | Done | Approved receipt posting enqueues persisted `PaymentRecorded` and `ClientPaidStatusChanged`; reversals enqueue `PaymentReversed` and reopen paid-status when needed |
| Local outbox publisher | ControlCloud | Done | Manual dev endpoint builds signed envelopes and marks ready outbox messages sent/failed without calling SafarSuite Control Cloud |
| Control Cloud publish readiness | ControlCloud | Basic done | Signed payload envelope, local/HTTP publisher adapter, retry attempt metadata, and config-driven publish mode are wired |
| Control Desk to Control Cloud local publish | ControlCloud/Integration | Basic done | Control Desk Development config points HTTP publishing to the local Control Cloud receiver, and the accounting smoke can publish seven real outbox rows through the receiver |
| Client entitlement chain | Entitlements/ControlCloud/LocalServer | In Progress | Paid invoices can now issue persisted local entitlement snapshots from active contract defaults and enqueue signed `EntitlementSnapshotIssued` publish messages; cloud-side commercial projection accepts entitlement snapshots, issues installation-bound audited HMAC-signed portal entitlement bundles, queues signed installation commands with acknowledgement audit, and local-server libraries can pull, import, cache, gate, and report heartbeat/license state |
| Offline entitlement control rules | Entitlements/ControlCloud/Client Portal | Accepted | `docs/planning/offline-entitlement-control-rules.md` defines paid-period offline validity, heartbeat separation, warning/grace/restriction states, offline renewal files, trust-based lease lengths, command acknowledgements, replay protection, and audit requirements |
| Control Cloud deployment tracker | ControlCloud/Client Portal/Local Server | Accepted | `docs/planning/control-cloud-deployment-tracker.md` defines the one-cloud rule, portal-controlled/local-server-pulled deployment, V1 Docker Compose bootstrap bundle, and later `.deb` packaging path |
| Control Cloud receiver skeleton | ControlCloud/API/Application/Infrastructure | Basic done | `SafarSuite.ControlCloud.Api` receives signed Control Desk envelopes, validates payload hash/HMAC, records accepted/rejected/duplicate receipts in PostgreSQL, projects accepted commercial messages into a portal-readable read model, and returns stable cloud message references |
| Control Cloud PostgreSQL persistence | ControlCloud/Persistence | Done | Cloud receiver receipts and client commercial projections are persisted in PostgreSQL under the `cloud` schema with a filtered unique accepted-idempotency index |
| Control Cloud portal commercial projection | ControlCloud/ClientPortal | Basic done | Accepted invoice, payment, credit note, refund, credit-application, paid-status, and entitlement-snapshot envelopes update PostgreSQL-backed `GET /api/v1/client-portal/clients/{clientId}/commercial-summary` |
| Client Portal identity/session boundary | ControlDesk/ControlCloud/ClientPortal | Basic contact invite foundation done | `docs/architecture/client-portal-identity-boundary.md` records that Control Cloud owns portal credentials; Control Desk can request a portal invite for a client contact, Control Cloud protects invitation creation with a provider key, and invite acceptance creates password-backed client-scoped sessions; real provider users, MFA, email delivery, resend/revoke/list, and session audit remain pending |
| Control Cloud entitlement signing boundary | ControlCloud/ClientPortal/LocalServer/Entitlements | Basic done | Latest projected entitlement snapshots can be returned as installation-bound HMAC-signed bundles through the session-protected Client Portal route and the machine-facing `GET /api/v1/local-server/installations/{installationId}/entitlement-bundle?clientId={clientId}` route; bundle issues and installation registry state are persisted in PostgreSQL |
| Control Cloud installation command queue | ControlCloud/LocalServer/Entitlements | Basic done | Control Cloud can queue signed monotonic commands for registered installations, local servers can pull pending commands and acknowledge Applied/Failed/Rejected results, and PostgreSQL persists command plus acknowledgement audit rows |
| Control Cloud installation heartbeat | ControlCloud/LocalServer | Basic done | Local servers can report heartbeat to `POST /api/v1/local-server/installations/{installationId}/heartbeat`; Control Cloud stores heartbeat status separately from reported license state in PostgreSQL/file persistence |
| Control Cloud installation status view | ControlCloud/ClientPortal/ControlDesk | Basic portal preview done | Shared status is available through Control Cloud and Client Portal routes; the Control Desk client page can refresh by installation id, and `/client-portal/index.html` can show commercial, license, heartbeat, entitlement, and command status for a client/installation |
| Local server entitlement verification | LocalServer/Entitlements | Basic done | New `SafarSuite.LocalServer` layers can pull the latest signed bundle from Control Cloud, verify HMAC-signed entitlement bundles, reject bad signatures and older versions, cache the latest accepted bundle, gate module access through active/warning/grace/restricted/expired states, and report heartbeat/license status |
| Payment review and reversal foundation | Payments/Accounting/Frontend | Basic done | Bank transfers enter pending review without GL posting; approval posts receipt journal and payment events; rejection closes pending review; reversal posts a dedicated reversal journal and updates the client statement |
| Billing tax foundation | Billing/Accounting/Frontend/Persistence | Basic done | Client charge rules support tax percent, invoice drafts materialize tax lines, invoice issue credits tax payable, and EF migration adds tax/line-type persistence |
| Unpaid invoice void foundation | Billing/Accounting/ControlCloud/Frontend | Basic done | Unpaid issued invoices can be voided with a reversing GL journal, `InvoiceVoided` outbox event, and statement visibility; paid invoice correction remains a later credit/refund workflow |
| Full credit note foundation | Billing/Accounting/ControlCloud/Frontend/Persistence | Basic done | Paid and partially paid invoices can receive one full credit note with reversing sale journal, `CreditNoteIssued` outbox event, persistence, and statement credit visibility |
| Client refund foundation | Payments/Accounting/ControlCloud/Frontend/Persistence | Basic done | Client credit balances can be refunded with a balanced AR/cash-bank journal, `ClientRefundIssued` outbox event, persistence, and statement visibility |
| Client credit settlement foundation | Payments/Billing/ControlCloud/Frontend/Persistence | Basic done | Unapplied client credit can be allocated to issued/partially paid invoices without a new GL journal, with `ClientCreditApplied` outbox event, persistence, invoice balance updates, and statement visibility |
| Accounting action UI guardrails | Billing/Payments/Frontend | Basic done | Client desk disables incomplete invoice/payment/credit/refund actions and asks for confirmation before GL-impacting issue, void, credit note, receipt, approval, reversal, settlement, and refund actions |
| Accounting chain smoke runner | Tools/Accounting | Basic done | `tools/SafarSuite.ControlDesk.AccountingSmoke` exercises the core local chain through application handlers and verifies balanced journals, statement credit behavior, and outbox events; both in-memory and PostgreSQL modes have passed |

## Parking Lot

| Item | Reason Parked | Status |
| --- | --- | --- |
| Full Survey/FAS form cloning | Too broad for Milestone 1 | Deferred |
| SurveyValuation module | No longer part of the product goal for SafarSuite Control Desk; code/docs remain as reference but API routes and DI registrations are not active | Deferred |
| Legacy form clone plan | Historical reference only after pivot away from Survey/FAS clone | Deferred |
| Legacy form name map | Historical reference only after pivot away from Survey/FAS clone | Deferred |
| Survey Job Entry implementation | Work exists but is paused; do not extend unless requirements change | Deferred |
| Tauri desktop packaging | Wait until browser-hosted React UI proves the first flow | Deferred |
| Payment gateway selection | Needs business decision | Proposed |
| SQL backup restore | Useful later for sample data and parity checks | Proposed |
| PostgreSQL persistence for Survey Job Entry | Not needed while SurveyValuation remains parked | Deferred |
| Broader Survey reference lookups | Not needed while SurveyValuation remains parked | Deferred |
