# Control Model Architecture Gap Map

Date reviewed: 2026-07-12

Canonical direction: `docs/architecture/product-charter-2026-07-11.md`

## Outcome

The connected control loop does not need to be rebuilt from zero. The repository already contains a strong delivery and enforcement foundation:

- a separate Control Desk API and PostgreSQL persistence layer
- transactional commercial/accounting actions
- a durable retryable outbox
- an idempotent Control Cloud receiver
- projected entitlement state
- installation-bound signed bundles
- LocalServer verification and module gates
- heartbeat entitlement-version reporting
- installation status and command acknowledgement

The authoritative office-side commercial and access chain now exists: an immutable published product catalog revision defines selectable modules, an immutable `ClientContract` revision binds to that exact catalog, an immutable scheduled `ClientAccessRevision` binds paid-invoice evidence and rich desired-access limits to that exact contract and catalog, and only then is an entitlement artifact issued. Control Cloud and SafarSuite Server now close the loop with desired, delivered, and observed value reconciliation. The remaining structural work is productionizing identity, high-volume reads, central database topology, and measured operational recovery.

## Capability Assessment

| Capability | Current State | Decision |
| --- | --- | --- |
| Office API and database separation | `SafarSuite.ControlDesk.Api` already owns application access to PostgreSQL; local Docker is only the current deployment | Keep; productionize as a centrally deployed Office Control API/database |
| Client contract | Immutable numbered revisions contain effective dates, price, device/branch/user allowances, module-scoped feature limits, modules, predecessor, approval evidence, and exact product catalog revision provenance | Keep; decide which allowance dimensions drive each commercial offer |
| Product catalog | Mutable drafts publish append-only numbered revisions containing commercial modules, compatibility, billing defaults, access groups, and resources | Keep; replace the temporary seed lineup only after the initial commercial catalog is decided |
| Desired access | Immutable `ClientAccessRevision` records the exact contract revision, modules, device/branch/user limits, module feature limits, paid/offline dates, paid-invoice evidence, approval, predecessor, and explicit UTC effective instant | Keep; define cancellation/replacement policy for a future revision before production |
| Office entitlement snapshot | One-to-one derived artifact of an approved access revision with an Office-owned version and exact desired-access values | Keep immutable; change limits only through a new source revision |
| Outbox | Transactional, retryable, indexed by publish status, and idempotently received | Keep; add client/aggregate/version metadata and paginated operator reads |
| Cloud commercial projection | Bounded scalar summary plus one indexed row per invoice, payment, credit note, refund, and credit application; portal history uses keyset pages | Keep; add measured volume tests and retention policy |
| Cloud entitlement signing | Installation-bound, signed, audited, protected against older versions, and retains the Office revision ID/version and exact access limits | Keep; replace development HMAC custody with production key management when topology is selected |
| Multiple installations | Cloud installations are bound independently to one client and receive installation-bound bundles | Keep; add an office read model that compares each installation with the same approved desired version |
| Observed state | Heartbeat stores the verified applied entitlement values, license state, dates, runtime version, and pairing summary; installation status compares desired, signed, and observed values with field-level differences | Keep; add retention and pagination policy for high-volume heartbeat history |
| Operator authentication | Current exploratory local session returns random tokens but does not persist or validate them; business endpoints are not protected by that session | Replace with real central Office Control authentication and authorization before production use |
| Large registers | Client, outbox, statement, contract, invoice, payment, journal, and several accounting reads return complete collections | Add server-side pagination and purpose-built summaries before high-volume use |
| Diagnostics and files | Rich diagnostics and package artifacts exist | Move large artifacts to file/object storage with retention metadata; keep relational indexes small |

## Evidence And Gaps

### Contract Is An Immutable Revision Boundary

`ClientContract` now has a per-client revision number, predecessor, approval metadata, and effective date window. Draft terms can be assembled before activation; every commercial mutator rejects changes after activation.

`ReplaceActiveClientContract` serializes writes per client, suspends the active predecessor, and creates the next immutable revision. Database constraints enforce one root, one use of each predecessor, unique client/revision numbers, and one active revision.

### Office Access And Entitlement Versioning Is Explicit

PostgreSQL allocates the Office-owned version. `ClientAccessRevision` records the approved business decision plus its exact contract and product-catalog revisions, and `EntitlementSnapshot` is constructed from that revision with the same access number and values. Event version 5 carries all three provenance boundaries, user caps, and module feature limits through Control Cloud bundle version 4, bundle-issue audit, installation status, and the verified SafarSuite Server cache.

Historical snapshots are backfilled as `LegacyEntitlementImport` revisions with null invoice evidence rather than guessed commercial history. New revisions can only be created from a real paid invoice and require approver, reason, modules, limits, and predecessor lineage.

### Desired, Delivered, And Observed Facts Exist But Are Not Joined

The necessary raw facts already exist:

- Office desired decision: `ClientAccessRevision`
- Office issued artifact: `EntitlementSnapshot`
- Office delivery state: `CloudOutboxMessage`
- Cloud signed delivery: `ControlCloudEntitlementBundleIssue`
- Server observation: `ControlCloudInstallationHeartbeat.EntitlementVersion`
- Cloud status response: latest issued bundle and latest heartbeat

The first comparison contract now joins desired, signed, and server-observed versions and reports `SigningPending`, `ApplyPending`, `Ahead`, `InSync`, or `Unknown`. It also exposes the signed access-revision ID. Office outbox delivery, explicit rejection, and richer per-installation difference summaries still need to join this read model.

### Cloud Commercial Projection Is Bounded

`cloud.client_commercial_projections` now stores only scalar totals, paid state, update time, and the latest entitlement. `cloud.commercial_documents` stores one current row per `(client, document type, document id)` with common indexed fields and bounded type-specific detail.

Each accepted event locks one client summary inside the existing receipt transaction, updates one affected document, and applies scalar deltas. Portal document history uses a required type, a page size from 1 through 100, and an opaque `(document date, document id)` keyset cursor. See `docs/architecture/cloud-commercial-read-model-scale-boundary.md`.

### Current Reads Are Mostly Unbounded

Examples include all-client listing, complete outbox listing, complete client statements, client document repositories, and broad accounting register reads. Existing indexes are useful, but the API contracts need page size, cursor or stable offset semantics, total/count behavior, and deterministic sort keys.

### Central Deployment Is An Evolution, Not A Rewrite

The React UI already calls an HTTP API and the API already owns PostgreSQL access. Moving authority away from one workstation therefore requires production topology, authentication, configuration, backup, and concurrency work rather than moving domain logic into a new service.

## Canonical Desired-Access Revision

The immutable office-owned revision foundation now contains:

```text
ClientAccessRevision
  revision id
  client id
  contract id and contract revision number
  entitlement version
  effective from (currently represented by approval/issue time; explicit policy remains pending)
  paid until / warning / grace / offline valid until
  allowed named users, nullable when no explicit cap exists
  allowed concurrent users, nullable when no explicit cap exists
  allowed devices
  allowed branches
  enabled module codes
  module-scoped feature-limit code, value, and unit rows
  approved decision state
  approved by / approved at / reason
  paid-invoice commercial proof reference
  supersedes revision id
```

The revision is the desired state. An entitlement snapshot is the issued control artifact derived from one approved revision. One revision can be delivered to multiple installations using installation-bound signed bundles without duplicating the commercial decision.

## Closed-Loop Status Read Model

For each client and installation, expose:

| Fact | Example |
| --- | --- |
| Desired version | 184 |
| Office outbox state | Sent |
| Cloud accepted version | 184 |
| Cloud signed version | 184 |
| Server reported version | 183 |
| Comparison state | ApplyPending |
| Last heartbeat | timestamp |
| License state | Active |
| Difference summary | Server has not reported version 184 yet |

Suggested comparison states:

- `InSync`: server reported the desired version
- `DeliveryPending`: Office outbox has not reached Control Cloud
- `SigningPending`: cloud accepted desired state but no bundle has been issued
- `ApplyPending`: bundle exists but server reports an older version
- `Rejected`: server or cloud explicitly rejected the desired version
- `Ahead`: server reports an unknown newer version and requires investigation
- `Unknown`: insufficient evidence

## First Implementation Slice

Implement explicit entitlement version ownership end to end before redesigning more UI:

1. Done: add an office-owned monotonic entitlement version to the entitlement model.
2. Done: persist a unique `(client_id, entitlement_version)` constraint and backfill existing snapshots.
3. Done: publish the explicit version in `EntitlementSnapshotIssued`.
4. Done: project that version in Control Cloud without allowing stale events to roll state backward.
5. Done: sign exactly that version instead of timestamp ticks.
6. Done: return desired, signed, and heartbeat-applied versions through one comparison contract.
7. Done: in-memory and PostgreSQL accounting smokes, LocalServer entitlement smoke, and the 28-check PostgreSQL Control Cloud proof pass, including stale/equal-version rejection, apply-pending status, signed provenance, heartbeat, command acknowledgement, and persisted boundaries.

This slice uses the strongest existing infrastructure and creates the semantic backbone for every later module, limit, dashboard, and desktop workflow.

Implemented: 2026-07-11.

## Second Implementation Slice

Implemented on 2026-07-11:

1. Added immutable `ClientAccessRevision`, module rows, predecessor lineage, approval metadata, and paid-invoice evidence.
2. Added per-client issuance serialization and uniqueness constraints for one linear revision history.
3. Made entitlement construction depend on one approved revision and persisted a one-to-one reference.
4. Added safe legacy provenance backfill migrations for Office snapshots and Cloud bundle audits.
5. Propagated `clientAccessRevisionId` through event version 2, cloud projection, signed bundle, issue audit, installation status, and server cache.
6. Added Control Desk approval provenance and access-revision display.
7. Extended accounting, PostgreSQL proof fixtures, compose proof fixtures, and LocalServer smoke assertions.

## Third Implementation Slice

Implemented on 2026-07-11:

1. Added immutable numbered `ClientContract` revisions with predecessor lineage, effective dates, and approval evidence.
2. Added per-client write serialization and database constraints for one root, one active revision, and one linear history.
3. Guarded every commercial mutator after activation so changes require replacement.
4. Backfilled existing contracts deterministically and linked existing access revisions to their contract revision number.
5. Bound every new access approval to the paid invoice's exact contract revision.
6. Propagated contract revision provenance through event version 3, signed bundle version 2, Control Cloud status APIs, and SafarSuite Server cache.
7. Updated Client 360 and both smoke chains to operate and prove the revision model.

## Fourth Implementation Slice

Implemented on 2026-07-11:

1. Replaced configuration-driven module interpretation and mutable access-catalog overwrite with one versioned product definition.
2. Added mutable draft editing and append-only published revisions with global numbering, predecessor lineage, publication evidence, and a database mutation trigger.
3. Added module lifecycle, compatibility, optional billing defaults, runtime groups, and resources to each revision payload while preserving their distinct code spaces.
4. Bound contracts and approved access revisions through composite catalog ID/number foreign keys and backfilled existing histories without dropping legacy module codes.
5. Propagated catalog provenance through event version 4, Control Cloud bundle version 3, bundle-issue audit, installation status, and SafarSuite Server cache.
6. Added Setup and contract/access visibility for draft state, published history, exact bound revision, and separate server delivery.
7. Proved fresh migration, existing-data upgrade, append-only rejection, in-memory/PostgreSQL commercial flow, catalog publication, Cloud signing, and local verification.

## Fifth Implementation Slice

Implemented on 2026-07-12:

1. Added nullable named-user and concurrent-user allowances to immutable contract, access-revision, and entitlement-snapshot boundaries without inventing limits for legacy rows.
2. Added normalized module-scoped feature-limit value objects and child tables with unique parent/module/feature identities, nonnegative values, and enabled-module validation.
3. Propagated the exact values through event version 5, the Control Cloud projection, signed bundle version 4, bundle-issue audit, installation status, and the verified LocalServer cache.
4. Added `GET /api/v1/local-server/limits` as the structured runtime gateway for user allowances and module feature quantities.
5. Added contract editing and history, Client 360, entitlement, cloud-status, and readiness visibility for the new values.
6. Preserved compatibility with older Office events and bundles by treating absent user caps as unspecified and absent feature-limit collections as empty.
7. Proved model-snapshot parity, full idempotent migration SQL, fresh PostgreSQL migration, in-memory/PostgreSQL Office flow, Cloud signing/audit, and LocalServer verification and lookup.

## Sixth Implementation Slice

Implemented on 2026-07-12:

1. Added `EffectiveFromUtc` to immutable Office access revisions and entitlement snapshots with truthful historical backfill from approval and issue timestamps.
2. Propagated the exact instant through event version 6 and bundle version 5 while retaining fallback behavior for earlier event and bundle formats.
3. Projected future desired state immediately but held installation-bound signing until the effective instant, leaving the current signed and observed version enforceable.
4. Extended LocalServer heartbeat evidence with the verified entitlement values actually cached at the installation.
5. Added a per-installation reconciliation read model that compares canonical desired, delivered, and observed values and explains every mismatch.
6. Added schedule controls and reconciliation tables to Client 360 and the Cloud operator workspace.
7. Proved model parity, clean full PostgreSQL migration chains, Office smokes, LocalServer verification/heartbeat evidence, value-level `InSync`, and a future-version `Scheduled` signing hold across all 29 Cloud proof boundaries.

## Seventh Implementation Slice

Implemented on 2026-07-12:

1. Replaced the per-client ever-growing commercial JSON projection with a bounded scalar summary and normalized current document rows.
2. Added common indexed fields plus bounded detail for invoices, payments, credit notes, refunds, and credit applications.
3. Changed signed-envelope projection to lock one client and mutate only the affected document and summary contributions.
4. Kept the summary response shape compatible while moving history to an authenticated keyset-paginated endpoint with a maximum page size of 100.
5. Updated the Client Portal to load eight recent invoices and continue through the opaque cursor on demand.
6. Added a forward migration that backfills every legacy collection before dropping the old JSON and a reverse migration that reconstructs it.
7. Proved scalar and five-type backfill, reverse reconstruction, both indexes, bounded summary behavior, three non-overlapping pages, cursor rejection, and normalized PostgreSQL rows.

## Subsequent Scale Work

After the versioned closed loop passes:

1. Add pagination to high-growth Office reads and remaining Cloud operational registers.
2. Replace exploratory local authentication with durable central operator identity and authorization.
3. Define cancellation and replacement rules for approved future access revisions.
4. Define heartbeat and signed-artifact retention boundaries.
5. Define backup/restore, artifact retention, and measured load tests.
