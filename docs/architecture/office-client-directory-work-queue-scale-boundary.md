# Office Client Directory and Work Queue Scale Boundary

Status: Implemented on 2026-07-12.

## Decision

The Office client master is no longer exposed as an unbounded aggregate list. Interactive desktop surfaces use a server-side client directory page, and Command Center uses one provider work-queue read model instead of loading every client and issuing five requests per client.

The Office database remains the source of truth. These APIs are read projections only; Client 360 and the existing command endpoints still own changes to clients, billing, access, deployment, and Cloud delivery.

## Client Directory Contract

```text
GET /api/v1/clients?search={text}&status={status}&sort={sort}&direction={asc|desc}&take={1..100}&cursor={opaqueCursor}
```

- default page size is 50 and the hard maximum is 100
- sort keys are `code`, `displayName`, `legalName`, and `status`
- ordering always ends with client code and client ID for deterministic continuation
- search covers code, display name, legal name, and status
- the response separates filtered matches from whole-register status totals
- cursors are bound to search, status, sort, and direction; reusing one for another query is rejected

The client aggregate repository no longer has a `ListAsync` operation. PostgreSQL and in-memory persistence both implement the same bounded directory port.

## Command Center Contract

```text
GET /api/v1/command-center/client-work?lane={lane}&search={text}&sort={sort}&take={1..100}&cursor={opaqueCursor}
```

- default page size is 25 and the hard maximum is 100
- lanes are `all`, `setup`, `billing`, `payments`, `access`, `cloud`, and `overview`
- sorts are `priority`, `client`, and `action`
- the response returns exact lane counts after search and before the selected lane filter
- one database command returns an index-driven page and an exact summary as two result sets
- queue cursors are bound to search, lane, and sort

Command Center now makes one page request. It no longer downloads all clients, caps them at 20 in the browser, or fans out to client details, deployments, statements, entitlements, and outbox summaries.

## Priority Contract

The projection refresh function assigns the first matching action:

| Priority | Condition | Action | Lane |
|---:|---|---|---|
| 0 | client is not active | Activate client | Setup |
| 1 | active client has no contact | Add contact | Setup |
| 2 | active client has no deployment | Save deployment | Setup |
| 3 | active client has no invoice | Draft invoice | Billing |
| 4 | issued or partially paid invoice remains open | Record receipt | Payments |
| 5 | paid invoice exists but no entitlement exists | Issue access | Access |
| 6 | pending or failed Office outbox rows exist | Send to Cloud | Cloud |
| 7 | no earlier action applies | Review next action | Overview |

PostgreSQL stores one narrow queue row per client. `EfUnitOfWork` captures affected client IDs, saves the authoritative change, and invokes `control.refresh_client_work_queue(client_id)` before the transaction commits. Reads therefore avoid cross-module joins while queue state remains transactionally aligned with normal EF writes. In-memory persistence derives the same contract directly from its repositories.

## PostgreSQL Support

Migration `20260712145300_AddClientDirectoryAndWorkQueueReadModels` adds:

- generated lower-case `clients.search_text`
- `pg_trgm` and a GIN trigram index for contains search
- client display/legal/status ordering indexes
- a client-contact ownership index
- a client/status outbox index

Existing client-prefixed deployment, invoice, entitlement, and outbox indexes serve the correlated queue signals.

Migration `20260712161029_MaterializeClientWorkQueue` adds:

- `control.client_work_queue_items`, one row per client
- generated client/action sort keys and a combined search document
- priority, lane, client-sort, action-sort, and GIN search indexes
- `control.refresh_client_work_queue(uuid)` for one-client rebuilds
- a forward backfill for every existing client

The queue table is a disposable projection. The Office client, contact, deployment, invoice, entitlement, and outbox tables remain authoritative and can rebuild it.

## Desktop Behavior

- Command Center loads and continues server pages with authoritative lane totals.
- Client 360 searches and continues the client selector; a launched client can be pinned even when outside the current page.
- Setup reports whole-register client totals while its register remains paged and searchable.
- Legacy Client Desk delegates search and all four sort modes to the server and continues by cursor.

No desktop surface calls an all-client endpoint.

## Verification

- Control Desk API and desktop production builds pass.
- The migration applies to the development database and a fresh disposable PostgreSQL database.
- Eight focused synthetic clients prove non-overlapping client and queue pages, exact status/lane totals, Billing-lane classification, and terminal continuation behavior.
- Reusing a client cursor with another search returns `400`.
- A 25,008-client PostgreSQL run returns the directory in about 42 ms and warm queue pages in about 9-11 ms on the local development machine.
- Adding a contact through the API moves the same client from `Add contact` to `Save deployment` before the write operation returns.
- In-memory and clean PostgreSQL accounting smokes pass across billing, payment, entitlement, and outbox writes with queue refresh enabled.
- Browser verification covers Command Center, Client 360, and Setup against the migrated API.

## Next Scale Work

- paginate client statements without rebuilding complete invoice/payment/journal collections
- add bounded invoice, payment, journal, and audit registers with purpose-built summaries
- add production telemetry for queue refresh failures, read latency, and projection age
- decide whether exact lane totals need maintained counters only after measured production-like volume
- define outbox retention/archive rules and remediation for legacy-unowned rows
