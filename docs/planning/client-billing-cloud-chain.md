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
| Client accounting profile | Basic done | Client can be linked to AR ledger account, default currency, and cloud customer identity; normal setup auto-generates the AR ledger account code and links the created account to the profile |
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
| Paid product module composition | Basic catalog and billing defaults done | Client contracts and entitlement snapshots must include at least one enabled module; a flexible product module catalog can separate IncludedForAll modules from PaidAddOn modules without fixing the final lineup yet, active paid add-ons can prefill billing charge setup, saved charge rules and invoice draft lines preserve the originating module code, and runtime access is enforced through signed entitlements |
| Control Cloud commercial projection | Basic done | Accepted Control Desk invoice/payment/credit/refund/settlement envelopes project into a PostgreSQL-backed cloud-owned client commercial summary for the Client Portal |
| Client Portal identity/session boundary | Basic invite management foundation done | Control Desk can request, list, resend, and revoke portal invites for client contacts; Control Cloud protects invitation management with a provider key, supports file or SMTP invitation delivery through a pluggable adapter, records invitation/session audit events, and owns portal invitations, password-backed users, and signed client-scoped sessions through `POST /api/v1/client-portal/invitations`, `/invitations/accept`, and `/sessions`; real provider users, MFA, password reset, and production mail retry handling remain pending |
| Control Cloud entitlement signing | Basic done | Latest projected entitlement snapshot can be returned to the Client Portal/local server as an installation-bound signed bundle with payload hash, key id, HMAC signature, bundle issue id, paid/grace/offline dates, warning start, module states, and limits; issue audit and installation registry state persist in PostgreSQL; offline renewal files wrap the same signed bundle |
| Control Cloud installation commands | Basic done | Control Cloud can queue signed monotonic commands for registered installations; local servers can pull pending commands and acknowledge Applied/Failed/Rejected outcomes with persisted audit |
| Control Cloud/local-server heartbeat | Basic done | Local servers can report heartbeat to Control Cloud with heartbeat status stored separately from reported license state, entitlement version, paid/grace/offline dates, and local-server version |
| Control Cloud installation status view | Basic portal preview done | Shared status endpoint returns installation identity, latest heartbeat/license state, latest entitlement bundle issue, pending command count, and latest command acknowledgement summary; the Control Desk client page has a minimal manual refresh panel, and Control Cloud serves a minimal Client Portal preview at `/client-portal/index.html` |
| Local server setup-token registration | Basic done | Control Cloud can create one-time hashed setup tokens for client installations, local servers can register outward with the token, and entitlement bundle issue now requires the installation to be registered |
| Control Cloud bootstrap package generation | Basic done | Control Cloud can generate a setup-token-backed local-server bootstrap package with cloud endpoints, a copyable install command, signed JSON bootstrap bundle download, artifact checksums, first served `install.sh`, Docker Compose template, environment template, runtime services manifest, deployment profile metadata, and an optional `safarsuite-app` Compose profile slot; bootstrap mode vocabulary is now constrained to online or offline-assisted setup, and `docs/planning/safarsuite-app-integration-handoff.md` records the real app workspace switch contract |
| Control Cloud setup/bootstrap audit visibility | Basic done | Control Cloud records setup token creation, bootstrap package generation, local-server registration acceptance/rejection, and exposes a filtered audit-events endpoint for support review |
| Local server diagnostics export/upload | Basic done | Local-server libraries can export diagnostics from cached entitlement, trust-state, local import audit, runtime, Docker/Compose, bootstrap config, local service status, and recent-error data, upload it to Control Cloud, and Control Cloud persists/latest-reads the report for support |
| Local server entitlement verification | Basic done | Local-server layers can pull the latest signed bundle from Control Cloud, import HMAC-signed entitlement bundles directly or from offline renewal files, reject bad signatures, older versions, and same-file replay, persist trust state for last accepted bundle/last trusted cloud time/local clock rollback, persist local import audit records for accepted/rejected imports, cache the latest accepted bundle, gate module access through active, warning, grace, restricted, expired, and module-disabled states, and report the current license state during heartbeat |
| Client deployment/data-sync boundary | Basic done | `docs/architecture/client-deployment-and-data-sync-boundary.md` defines bootstrap mode versus client deployment mode, the four supported runtime modes, branch/site identity room, and the separation between commercial control and future operational business-data sync |
| Installation/deployment profile | Basic done | Setup tokens and registered installations persist bootstrap mode, client deployment mode, site id, site role, optional parent site, branch code, and sync topology id; bootstrap/status/registration/heartbeat/diagnostics contracts can surface the profile without implementing operational business-data sync |
| Payment posting | Done for local review loop | Records persisted payment, supports pending bank-transfer review, posts balanced cash/AR journal on approval, and posts balanced reversal journals |
| Client billing setup UI | Basic done | Client desk includes accounting profile, charge/tax rules, invoice draft, invoice issue, unpaid invoice void, and full credit-note workflow; GL account setup shows enforced type/balance/posting defaults and uses `GL_Working.xlsx`-aligned controlled numeric account-code suggestions instead of manual ledger codes |
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
- paid module composition, where clients can combine configurable IncludedForAll and PaidAddOn modules without hard-coding the final module list into the contract flow
- heartbeat when internet is available
- command queue and acknowledgement for renew/revoke/change-limit actions
- offline renewal file import when the local server cannot connect near expiry
- trust-based lease lengths for normal vs high-risk clients
- clock/replay protection
- full audit trail for every entitlement issue, local import, and command action

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
35. Done: add invitation list/resend/revoke management and local invitation delivery outbox records.
36. Done: add a pluggable Client Portal invitation delivery boundary with local file delivery and SMTP delivery, plus invite/session audit records.
37. Done: add offline renewal file export/import as a fallback for sites that cannot connect near expiry.
38. Done: add local-server clock/replay trust state for accepted bundle version/issue tracking, last trusted cloud time, same-file replay rejection, replay warnings, and clock rollback warnings.
39. Done: add setup-token registration so Control Cloud creates one-time setup tokens and local servers register outward before entitlement pulls.
40. Done: add first bootstrap package generation so Control Cloud can return a setup token, cloud endpoints, copyable local-server install command, signed bootstrap bundle metadata/download, template artifact checksums, and the first served install script template.
41. Done: add setup/bootstrap/registration audit visibility with filtered Control Cloud audit-events API.
42. Done: add diagnostics export/upload foundation with local-server bundle generation, Control Cloud receive/latest endpoints, file/PostgreSQL persistence, richer runtime/service/bootstrap/error fields, and smoke coverage.
43. Done: tighten module guardrails so contracts and entitlement snapshots require at least one enabled module.
44. Done: preserve product-module billing intent from module-backed charge rules through generated invoice draft lines.
45. Done: add signed bootstrap bundle/download generation and the first `install.sh` template served by Control Cloud.
46. Done: add first Docker Compose local-server service template and environment template artifacts to the signed bootstrap bundle path.
47. Done: add richer deployed-runtime diagnostics slots for runtime version/build/channel, Docker/Compose availability, bootstrap checksums, service state, and recent error summaries.
48. Done: add local import audit persistence so accepted/rejected Control Cloud pulls, direct bundle imports, and offline renewal-file imports are stored locally and surfaced in diagnostics.
49. Done: add the first SafarSuite runtime integration boundary. Bootstrap packages now carry a signed runtime plan, a runtime services manifest artifact, a diagnostics endpoint, app image/version environment variables, and an optional `safarsuite-app` Compose profile slot.
50. Done: add the shared local module-gateway contract and application handler so the future SafarSuite app/local API can evaluate module access through `LocalServerModuleAccessResponse`.
51. Done: define the client deployment topology and data-sync boundary for offline local, branch-to-HQ sync, cloud-sync multi-branch, and hosted SaaS modes. The boundary note separates bootstrap mode from client deployment mode, reserves branch/site identity fields, and keeps operational business-data sync out of the billing/license channel.
52. Done: add an explicit installation/deployment profile model that carries `bootstrapMode`, `clientDeploymentMode`, `siteId`, `siteRole`, `parentSiteId`, `branchCode`, and `syncTopologyId` through Control Cloud setup, registration, status, heartbeat, diagnostics, and bootstrap responses without requiring operational data sync.
53. Done: add minimal Control Desk/client-desk controls for creating setup tokens/bootstrap packages with deployment profile values, plus status visibility for the stored profile. Control Desk now proxies setup-token and bootstrap-package creation to Control Cloud through layered application/infrastructure clients, and the client desk Cloud tab can save the deployment profile before provisioning.
54. Done: add provider-facing installation history visibility on the client desk. Control Desk now proxies recent Control Cloud audit events, filters installation setup/bootstrap/registration/diagnostics/renewal events for the selected installation, and shows them in the Cloud tab next to setup-token/bootstrap controls.
55. Done: add the first read-only diagnostics review/download lane to the client desk. Control Desk now proxies the latest Control Cloud diagnostics report, and the Cloud tab can show runtime, Docker, service, check, recent-error, and license facts plus download the diagnostics JSON.
56. Done: add low-risk command queue support actions from the client desk. Control Desk now exposes a support-command proxy that only allows `request_diagnostics` and `refresh_entitlement`, signs them through Control Cloud's registered-installation command queue, and the Cloud tab can queue a command with reason/actor/expiry while refreshing pending-command status.
57. Done: wire local-agent handling for the low-risk commands so `request_diagnostics` verifies the signed command, exports/uploads diagnostics, and acknowledges the command, while `refresh_entitlement` verifies the signed command, pulls/imports the latest entitlement bundle, and acknowledges the command. The local-server API also exposes a manual command-processing endpoint, and the disabled-by-default runtime worker can poll commands on an interval.
58. Done: prepare the SafarSuite app integration handoff. `docs/planning/safarsuite-app-integration-handoff.md` records the local module-gateway contract, app-side behavior rules, module-code proof examples, workspace boundary, and switch gate.
58.1. Next: move into the real SafarSuite app workspace for actual module-gateway enforcement inside deployed app menus/routes/API boundaries.
59. Done: replace the temporary ledger-code suggestion bridge with persisted Accounting Setup ranges. Control Desk now stores active company-scoped account-code ranges under `control.account_code_ranges`, seeds legacy-aligned defaults for `MAIN`, drives suggestions from those ranges, and rejects ledger account creation outside configured ranges or with wrong type/balance/posting flags.
60. Done: build the first visible Accounting Setup / COA Setup UI. The Control Desk Accounting module now shows the ledger register, range filters, and editable company account-code range rules that feed controlled account suggestions and ledger-account validation.
61. Done: add the first controlled COA open/create workflow. The Accounting module can prepare the next account code from the selected range, create/edit/reactivate/deactivate ledger accounts, and show parent-code context; backend creation auto-links to an existing configured parent account by code.
62. Done: add the first legacy hierarchy read-model polish. The Accounting module now shows derived `H/T/M/D/C/S` badges, clickable level filters, Default/All/Header+Total views, and a read-only posting flag in account maintenance so posting behavior stays controlled by setup ranges.
63. Done: persist explicit COA hierarchy levels. Ledger accounts now store a backend `Level`, API/UI responses surface it, creation resolves Control/Master/Detail/Subsidiary from Accounting Setup context, and Header/Total/Master/Control levels cannot be posting accounts.
64. Done: add explicit Header/Total account creation and stricter parent/account-level rules. Accounting Setup now backfills Header/Total ranges for major account classes, the COA account editor can pass an explicit level, Subsidiary accounts require an existing Control parent, and Detail parents must be Master accounts when supplied.
65. Done: add old loose-account reconciliation/admin review. Control Desk now has a read-only COA reconciliation endpoint and panel that flags setup-range drift, level/posting mismatches, orphan subsidiaries, wrong parent levels, and inactive/outside-range accounts without mutating data.
66. Done: add guided COA repair dry-run. Control Desk now has a repair-plan endpoint and reconciliation-panel repair guidance that maps findings to current value, suggested value, repair mode, safety notes, and future automatable/manual repair classification without mutating accounts.
67. Done: add Accounting workspace report surfaces. Trial balance, profit and loss, balance sheet, ledger activity, and report refresh now sit inside the extracted Accounting workspace, with filters and drill-through back into account activity.
68. Done: add MAIN default GL controls plus period-close handoff polish. Control Desk can create/reuse retained earnings, income summary, and rounding accounts from Accounting Setup ranges, configure controls from one action, close/reopen periods with refreshed reports, suppress current earnings after close, and open generated close journals from the close artifact.
69. Done: capture the accounting/GL MVP checkpoint in `docs/planning/accounting-gl-mvp-checkpoint-2026-07-04.md`, including verified behavior, legacy alignment, acceptance checklist, gaps, and next controlled moves.
70. Next accounting dependency: add broader import dry-run checks and deliberate guided repair actions for reconciliation findings, then opening balances.

## Guardrails

- No more Survey/FAS clone work unless it becomes a paid/current requirement again.
- Client maintenance is the product center; supporting modules must tie back to client control.
- Product access is module-composed: a client may receive baseline IncludedForAll modules plus selected PaidAddOn modules in any allowed combination, and the local app must only enable modules present as enabled entries in the signed entitlement bundle.
- SafarSuite is not an offline-only product. The client-side runtime/control path must support offline local, HQ-sync, cloud-sync multi-branch, and hosted SaaS deployments; operational branch/HQ/cloud business-data sync stays separate from billing, license, portal, heartbeat, diagnostics, and command control.
- Bootstrap mode and client deployment mode are separate concepts. `OnlineBootstrap` and `OfflineAssistedBootstrap` describe setup; `OfflineLocal`, `BranchToHqSync`, `CloudSyncMultiBranch`, and `HostedSaas` describe how the client runs SafarSuite.
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
- Portal invitation creation is protected by a local provider key for now; production use must replace that with real provider/admin users, production mail delivery/retry handling, expiry/audit review screens, and role management.
- Heartbeat status and license validity remain separate; missed heartbeat alone must not disturb a paid offline client.
- Warnings start near `paid_until`; grace and restriction begin only after configured dates.
- Offline renewal files, local entitlement imports, and emergency unlocks must be signed or signature-verified where applicable, installation-bound, versioned, expiring, and audited.
- Revocation can be immediate only for installations that can receive the command; otherwise it takes effect on next heartbeat, next renewal file import, or the next license boundary.
