# SafarSuite Control Platform Active Roadmap

Date started: 2026-07-11

Status: Active. This is the short execution roadmap. `project-tracker.md` remains the historical implementation record.

Canonical direction: `docs/architecture/product-charter-2026-07-11.md`

Canonical deployment contract: `docs/architecture/final-system-requirements-and-deployment-contract.md`

## Current Objective

Package and prove the scale-ready Office Control System on one dedicated Windows office PC while preserving the completed desired-state-to-observed-state chain across Control Desk, Control Cloud, and SafarSuite Server.

## Now: One-PC Control Desk Deployment Proof

Execution contract: `docs/planning/one-pc-control-desk-deployment-plan-2026-07-18.md`

- [x] Lock the canonical one-PC requirements and physical topology.
- [x] Audit the current API, UI, PostgreSQL, desktop, CI, and staging assets against the office gate.
- [ ] `OFFICE-P0-01`: produce one loopback, same-origin, self-contained Windows artifact.
- [ ] `OFFICE-P0-02`: add database readiness, retained diagnostics, and automatic outbox recovery.
- [ ] `OFFICE-P0-03`: implement native Windows PostgreSQL installation, migration, repair, and reboot lifecycle.
- [ ] `OFFICE-P0-04`: package first-operator provisioning, secret custody, and recovery.
- [ ] `OFFICE-P0-05`: install the API service and normal operator desktop/Start-menu entry.
- [ ] `OFFICE-P0-06`: implement scheduled backup and prove clean replacement-PC restore.
- [ ] `OFFICE-P0-07`: implement signed update, failed-update recovery, and rollback.
- [ ] `OFFICE-P0-08`: pass the complete physical clean-PC acceptance run with retained evidence.
- [ ] `CLOUD-P1-01`: make cloud-only staging the default and isolate full-stack colocation as a Disposable Integration Lab.
- [ ] Add the Tauri shell only after the installed service/recovery boundary passes.

## Now: Direction And Architecture Reset

- [x] Establish the canonical product charter.
- [x] Separate the desktop operating experience from the durable Office Control API/database authority.
- [x] Define Office Control, Control Cloud, SafarSuite Server, and client-data ownership boundaries.
- [x] Retire legacy parity as a product objective.
- [x] Define desired state, delivery state, and observed state.
- [x] Audit the existing schema and APIs against the charter. See `docs/architecture/control-model-gap-map-2026-07-11.md`.
- [x] Decide the deployable Office Control API topology for V1: one dedicated office PC hosts the desktop UI, local API process, and local PostgreSQL; no separate office server or Linux Control Desk host is required. See the canonical deployment contract.
- [ ] Define backup, restore, concurrency, and operator-session acceptance criteria for the authoritative office database.

## Next: Canonical Control Model

- [x] Move entitlement-version ownership into the Office Control System: database sequence, immutable snapshot version, outbox propagation, cloud projection/signing, server-observed comparison, and status UI.
- [x] Add the immutable approved `ClientAccessRevision` foundation and derive every new entitlement snapshot from it.
- [x] Define the versioned product and module catalog model.
- [x] Define immutable client contract revisions, predecessor lineage, approval evidence, and effective dates.
- [x] Extend the canonical desired-access model beyond modules, devices, branches, and paid/offline dates to named/concurrent users and module-scoped feature limits.
- [x] Define immutable entitlement versions derived from approved desired state.
- [x] Define desired-versus-observed client/installation status read models.
- [x] Normalize the Cloud commercial read model and prove its keyset pagination indexes and contract.
- [x] Make the Office delivery outbox client-owned and keyset-paged from PostgreSQL through the desktop.
- [ ] Confirm remaining Office/Cloud register indexes, append-only histories, and artifact storage boundaries.

### Implemented Checkpoint: Office-Owned Entitlement Versions

The first redirection slice now proves:

- PostgreSQL allocates durable entitlement versions through `control.entitlement_version_sequence`.
- Existing snapshots are backfilled and `(client_id, entitlement_version)` is unique.
- Control Desk publishes the explicit version in `EntitlementSnapshotIssued`.
- Control Cloud ignores stale lower-version projections and signs the Office-issued version.
- Installation status distinguishes desired, signed, and server-observed versions.
- The existing Control Desk status surface shows `ApplyPending`, `InSync`, `Ahead`, `SigningPending`, or `Unknown` with version detail.
- In-memory accounting smoke proves monotonic issue and outbox behavior.
- Live PostgreSQL proof passes stale-projection, equal-version conflict, apply-pending, heartbeat, command, and persisted-boundary assertions.

### Implemented Checkpoint: Approved Client Access Revisions

The second redirection slice now proves:

- Every new entitlement issue first records an immutable Office-owned `ClientAccessRevision`.
- A revision records its client, contract, paid-invoice evidence, revision number, predecessor, limits, modules, approver, reason, and approval timestamp.
- Per-client locking plus chain and database constraints prevent silent competing revision branches.
- Entitlement snapshots can only be constructed from an approved revision and have a one-to-one revision reference.
- `EntitlementSnapshotIssued` event version 2, the cloud projection, signed bundle, bundle-issue audit, installation status, and SafarSuite Server cache retain the same revision ID.
- Historical snapshots are migrated into explicit `LegacyEntitlementImport` revisions without inventing invoice evidence.
- Control Desk shows the approval provenance, access revision, delivery version, and server-observed comparison.
- Client 360 treats cloud-backed invitation/status reads as noncritical, so Office client work remains available during a Control Cloud outage.
- In-memory and PostgreSQL accounting smokes plus LocalServer entitlement smoke pass; both EF model snapshots and generated migration SQL pass validation.

### Implemented Checkpoint: Immutable Contract Revisions

The third redirection slice now proves:

- Every client contract is a numbered Office-owned commercial revision with one predecessor, approver, reason, approval timestamp, and effective date window.
- Root, revision-number, predecessor, and active-revision constraints plus per-client locking prevent competing contract histories.
- Activated commercial terms and module selections cannot be edited; a changed agreement must create the next replacement revision.
- Existing contract history is deterministically backfilled into linear revision chains without rewriting commercial values.
- Each approved `ClientAccessRevision` records the exact contract revision number referenced by its paid invoice.
- `EntitlementSnapshotIssued` event version 3, Control Cloud projection and bundle audit, signed bundle version 2, installation status, and SafarSuite Server cache retain that contract revision.
- Client 360 creates or replaces a revision with an approval reason and shows effective dates, predecessor identity, approval provenance, and the access-to-contract link.
- Accounting smoke proves immutability, revision-2 lineage, event-v3 provenance, and access binding; LocalServer smoke proves the signed revision survives verification and caching.
- Both EF models have no pending changes, complete idempotent migration SQL generation succeeds, and a fresh PostgreSQL database passes the Office migration/accounting smoke and all 28 Control Cloud proof checks with event-v3 contract provenance.

### Implemented Checkpoint: Versioned Product Catalog

The fourth redirection slice now proves:

- Product modules, billing defaults, compatibility metadata, runtime access groups, and protected resources publish together as one Office-owned catalog revision.
- Catalog editing uses a mutable singleton draft; publication creates the next immutable numbered revision with predecessor, actor, reason, and timestamp.
- PostgreSQL composite foreign keys bind every contract and approved access revision to the exact catalog revision ID and number.
- Existing configuration is bootstrap input only. After revision 1 exists, changing `appsettings` cannot reinterpret published contracts.
- Legacy contracts and access revisions backfill to revision 1, and legacy contract module codes are retained in that published definition.
- `EntitlementSnapshotIssued` event version 4, Control Cloud bundle version 3, bundle-issue audit, installation status, and SafarSuite Server cache retain catalog provenance.
- Setup and contract/access views distinguish draft, published history, server delivery, and the revision bound to each client decision.
- In-memory and PostgreSQL accounting smokes, catalog draft/publish/history smoke, LocalServer entitlement smoke, and all 28 Control Cloud PostgreSQL checks pass.

### Implemented Checkpoint: Rich Desired-Access Limits

The fifth redirection slice now proves:

- Immutable contract revisions, approved access revisions, and entitlement snapshots retain nullable named-user and concurrent-user caps plus normalized module-scoped feature limits.
- `null` means no explicit cap or legacy-unspecified state; zero remains an explicit limit, and a concurrent-user cap cannot exceed a named-user cap when both exist.
- Every feature limit has one normalized `(module code, feature code)` identity, a nonnegative integer value, a unit, and an enabled-module reference.
- PostgreSQL stores feature limits as indexed child rows rather than embedding an unbounded JSON document, while legacy rows remain valid with null user caps and no feature limits.
- `EntitlementSnapshotIssued` event version 5, Control Cloud signed bundle version 4, bundle-issue audit, installation status, and the SafarSuite Server verified cache preserve the exact values.
- SafarSuite Server exposes its verified limits through `GET /api/v1/local-server/limits` for module-owned runtime enforcement.
- Contract setup, Client 360, entitlement review, cloud installation status, and readiness comparisons display the same desired values.
- Both EF models have no pending changes, full idempotent SQL generation succeeds, fresh PostgreSQL migration and Office accounting smoke pass, the LocalServer entitlement smoke passes, and the 27-boundary Control Cloud PostgreSQL proof retains `40` named users, `12` concurrent users, and one feature limit.

### Implemented Checkpoint: Effective Access And Reconciliation

The sixth redirection slice now proves:

- Every approved access revision and derived entitlement snapshot records an explicit UTC effective instant; omitted or past input becomes immediate at approval, and a schedule cannot begin after paid access ends.
- `EntitlementSnapshotIssued` event version 6 projects future desired state into Control Cloud immediately without making it enforceable early.
- Control Cloud reports future desired state as `Scheduled` and returns stable `EntitlementScheduled` conflict evidence instead of signing before the effective instant.
- Signed bundle version 5 preserves the exact effective instant and remains compatible with older bundles whose date-only `ValidFrom` is the available evidence.
- SafarSuite Server heartbeats return the verified values actually held in its entitlement cache, including user/device/branch allowances, modules, and feature limits.
- Installation status joins desired, delivered, and observed canonical values and classifies `Scheduled`, `DeliveryPending`, `ApplyPending`, `DeliveryDrift`, `ObservedDrift`, `Ahead`, or `InSync` with field-level differences.
- Client 360 can schedule a paid-invoice access change and both Client 360 and the detailed Cloud workspace render the same reconciliation evidence.
- Both EF models match their migration snapshots, clean full PostgreSQL migration chains pass, in-memory/PostgreSQL Office smokes pass, LocalServer smoke passes, and all 29 Cloud PostgreSQL proof boundaries pass including scheduled signing hold and value-level `InSync` evidence.

### Implemented Checkpoint: Bounded Cloud Commercial Reads

The seventh redirection slice now proves:

- The Cloud client summary is a bounded scalar row with only the latest entitlement retained as JSON.
- Invoices, payments, credit notes, refunds, and credit applications are separate indexed rows with bounded detail.
- One accepted Office event locks one client and mutates only its affected document and summary contribution inside the receipt transaction.
- Client Portal history uses authenticated keyset pages ordered by document date and ID, limited to 100 rows per request.
- The portal loads eight recent invoices and follows the returned continuation cursor on demand.
- Existing JSON projections migrate forward without losing any of the five collections, and rollback reconstructs the former shape.
- PostgreSQL proof covers bounded totals, normalized rows, stable non-overlapping pages, final-page termination, and malformed-cursor rejection.

### Implemented Checkpoint: Client-Owned Office Outbox Pages

The eighth redirection slice now proves:

- Every newly created Office outbox message persists its owning client separately from payload JSON.
- Legacy camel-case and Pascal-case payload owners backfill safely; malformed ownership remains explicit and does not block installation.
- The Office API returns at most 100 rows ordered by immutable timestamp/ID keys, with an opaque continuation cursor and full filtered delivery counts.
- General, client-scoped, status/type, and publish-ready PostgreSQL indexes serve separate read and delivery paths.
- Client Desk uses authoritative client totals, Client 360 loads older updates on demand, and Command Center no longer downloads or parses the global payload stream.
- The repository no longer exposes an unbounded list operation; interactive reads and publisher reads are capped independently.
- In-memory and fresh PostgreSQL accounting smokes traverse three non-overlapping pages across all nine real event paths, while a forward/reverse/forward migration proof preserves legacy rows and payloads.

### Implemented Checkpoint: Office Client Directory and Work Queue

The ninth redirection slice now proves:

- `/api/v1/clients` is a searchable, status-filtered keyset directory capped at 100 rows, with deterministic server sort and authoritative whole-register status totals.
- Client cursors are bound to search, status, sort, and direction; a cursor reused against another query is rejected.
- The client aggregate repository no longer exposes an unbounded list operation.
- Command Center uses one transactionally refreshed server-side work-queue projection and no longer downloads every client, truncates at 20, or sends five requests per candidate.
- Queue pages include exact search-scoped lane counts, deterministic continuation, and the same priority/action semantics used by Client 360.
- Command Center, Client 360, Setup, and Legacy Client Desk all search or continue through server pages.
- PostgreSQL integration proof covers non-overlapping directory and queue pages, exact status/lane totals, Billing-lane classification, stale-cursor rejection, transaction write-through, and 25,008-client timing.

See `docs/architecture/office-client-directory-work-queue-scale-boundary.md`.

### Implemented Checkpoint: Office Financial Read Spine

The tenth redirection slice now proves:

- The monolithic client statement endpoint and its complete cross-module collections are removed.
- Exact per-currency balances are separate from independently continued invoice, payment, activity, and client-journal registers, each capped at 100 rows.
- Commercial journals persist client ownership and immutable source document IDs; migration backfill leaves no known legacy commercial journal unowned.
- Invoice corrections, payment reversals, document audit reads, refunds, and credit applications no longer depend on company-wide journal scans or full client-history collections.
- The company journal register is a bounded keyset page; journal lines load only when one journal is opened.
- Client Desk, Client 360, and Accounting expose authoritative totals and continuation instead of presenting the first page as complete history.
- In-memory and clean PostgreSQL accounting smokes prove non-overlapping pages and query-bound cursor rejection.
- A repeatable PostgreSQL fixture proves 20,002 invoices, 6,667 payments, 26,671 client journals, 53,345 journal lines, and 26,672 activity rows under one client.

See `docs/architecture/office-financial-read-spine-scale-boundary.md`.

The remaining accounting scale work is report-specific: trial balance, balance sheet, profit and loss, period close, and ledger-account activity still need SQL reporting read models. It does not block the connected product acceptance chain below.

## Then: Connected Acceptance Chain

- [x] Create or select a client.
- [x] Configure a contract, custom pricing, modules, and limits.
- [x] Issue an invoice with provider accounting proof.
- [x] Record or approve payment.
- [x] Approve and issue the next entitlement version.
- [x] Publish through the Office Control outbox.
- [x] Accept, sign, and distribute through Control Cloud.
- [x] Apply and enforce through SafarSuite Server.
- [x] Return acknowledgement and observed state.
- [x] Display the complete state and audit chain in Control Desk.

Completed on 2026-07-17 with one clean PostgreSQL-backed run, 135 executable assertions, retained accounting/outbox/receipt/signature/audit IDs, deterministic local allow and deny decisions, and final exact-value reconciliation at `InSync` with zero differences. See `docs/planning/connected-acceptance-chain-proof-2026-07-17.md`.

## After The Chain: Product Operation

- [ ] Replace the exploratory UI shell with workflow designs grounded in the accepted chain.
- [ ] Add provider roles and permissions around real office actions.
- [ ] Complete payment review and correction workflows.
- [ ] Add backup/restore and data-retention operations.
- [ ] Package and update the Control Desk desktop application.
- [ ] Add production key custody and rotation.
- [ ] Add operational dashboards only from demonstrated operator needs.
- [ ] Load and performance test measured client/control-plane volumes.

## Decision Gates

Do not begin a later gate until the previous gate has an executable acceptance test.

| Gate | Evidence Required |
| --- | --- |
| Direction | Charter, ownership, data boundary, and V1 acceptance agreed |
| Control model | One versioned desired-access model can represent a client's contract |
| Commercial proof | Invoice and approved payment produce explainable accounting state |
| Cloud delivery | Outbox delivery is accepted idempotently and produces a signed version |
| Server enforcement | SafarSuite Server applies or rejects the exact version deterministically |
| Closed loop | Control Desk shows desired, delivered, and observed versions distinctly |
| Operational readiness | Authorized-operator use on the designated office PC, backup/restore, secrets, and failure recovery pass |

## Product Decisions Still Needed

- First production payment method and gateway strategy.
- Whether device count, named users, concurrent users, or a combination drives each product limit.
- Commercial handling of branches and additional installations.
- Initial stable product/module catalog.
- One-PC installer, local service lifecycle, local PostgreSQL lifecycle, update, rollback, and recovery mechanism for the first real office environment.
- Retention policy for heartbeat, diagnostics, commands, and audit history.

These decisions should not be hidden inside UI or infrastructure implementation.
