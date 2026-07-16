# Office Outbox Scale Boundary

Date accepted: 2026-07-12

## Decision

The Office Control outbox is an Office-owned delivery register. Each new message stores its owning `ClientId` separately from the event payload, and all interactive reads are bounded keyset pages. Payload JSON remains the immutable integration body; it is no longer the ownership index used by the desktop.

`CloudOutboxMessage.Create` requires a valid client identity. The persisted `client_id` column remains nullable only so an installation can retain malformed or orphaned historical rows during upgrade. Those rows remain visible in provider-wide diagnostics but cannot appear in a client-scoped page unless they are repaired deliberately.

The publisher boundary is unchanged. Ready-message selection remains a bounded oldest-first batch, message IDs remain the idempotency identity, and sent/failed/retry transitions keep their existing behavior.

## Read Contract

```text
GET /api/v1/control-cloud/outbox-messages
  ?clientId={optional client guid}
  &status={optional status}
  &messageType={optional exact type}
  &take=50
  &cursor={optional opaque continuation}
```

Rules:

- `take` defaults to 50 and must be from 1 through 100
- order is `occurred_at_utc DESC, cloud_outbox_message_id DESC`
- the cursor carries that immutable key pair and malformed cursors are rejected
- the response returns `pageSize`, `hasMore`, and `nextCursor`
- the response summary covers the complete filtered register, not only the returned page
- summary fields are total, pending, failed, sent, ready-for-publishing, and total attempts
- a client-scoped query filters on persisted `client_id`; it never parses `payload_json`

New messages arriving above the cursor do not duplicate or skip older rows. Status filters remain live operational views rather than snapshot-isolated exports, so status transitions between requests can change membership as expected.

The desktop uses the contract in three ways:

- Client Desk loads the latest 100 client rows and uses the full summary for controls and counts.
- Client 360 loads the latest 100 and follows `nextCursor` only when the operator asks for older updates.
- Command Center requests one row plus the client summary; it no longer downloads a provider-wide outbox or infers ownership from JSON.

## Persistence Boundary

The PostgreSQL indexes are:

```text
(occurred_at_utc DESC, cloud_outbox_message_id DESC)
(client_id, occurred_at_utc DESC, cloud_outbox_message_id DESC)
(status, message_type, occurred_at_utc DESC, cloud_outbox_message_id DESC)
(status, next_attempt_at_utc, attempt_count)
```

The repository exposes only capped page reads and capped publish-ready batches. The former unbounded list method has been removed from the application port and both providers.

No client foreign key is added in this migration. The outbox must retain delivery evidence even when a historical owner cannot be resolved, and a valid legacy payload may name a client row that no longer exists. New domain creation still requires a non-empty client ID.

## Migration And Compatibility

Migration `20260712122853_AddClientOwnedOutboxPaging`:

1. adds nullable `client_id`
2. backfills valid UUID values from either `clientId` or legacy `ClientId` JSON keys
3. leaves malformed ownership null instead of aborting the upgrade
4. replaces the old filtered index with the stable keyset form
5. adds general and client-scoped keyset indexes

Rollback removes only the derived owner column and new indexes. Event payloads and outbox rows are not rewritten or removed, so reapplying the migration derives the same ownership again.

## Executable Evidence

- in-memory accounting smoke produces nine real messages, proves every owner, traverses three non-overlapping pages, validates full summaries, and rejects a malformed cursor
- fresh PostgreSQL migration plus accounting smoke proves the same behavior through the EF repository and PostgreSQL UUID ordering
- a seeded legacy PostgreSQL proof recovers camel-case and Pascal-case owners, preserves a malformed row as unowned, verifies all four operational indexes, rolls back with all rows and payloads intact, and reapplies successfully
- Control Desk API Release build and the desktop production build pass

## Remaining Scale Work

- client-master paging and the server-side Command Center queue are complete; see `office-client-directory-work-queue-scale-boundary.md`
- paginate statements, journals, invoices, payments, and audit registers
- define outbox retention/archive rules and remediation for legacy-unowned rows
- use measured client/message volumes to decide whether exact summaries need transactionally maintained counters or partitions
