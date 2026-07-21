# Cloud Commercial Read Model Scale Boundary

Date accepted: 2026-07-12

## Decision

Control Desk remains the commercial source of truth. Control Cloud keeps a bounded client summary and one current read row per accepted commercial document; it does not copy a client's complete history into one JSON value.

The production PostgreSQL model is:

```text
cloud.client_commercial_projections
  one row per client
  scalar financial totals
  current paid state
  latest entitlement JSON only

cloud.commercial_documents
  one row per (client, document type, document id)
  common filter/sort fields
  related document id
  last source message and timestamps
  bounded document-specific JSON detail
```

The five document types are `Invoice`, `Payment`, `CreditNote`, `Refund`, and `CreditApplication`.

## Write Boundary

One accepted Control Desk envelope runs in the same database transaction as its receipt. PostgreSQL creates or locks the client's summary row with `FOR UPDATE`, updates only the affected document row, and applies scalar contribution deltas to the summary.

This gives per-client ordering without serializing unrelated clients. Status changes can update one existing document row; they never deserialize or rewrite the client's other documents. Older or equal entitlement versions remain ignored by the existing monotonic entitlement rule.

Control Cloud is a projection, not a second accounting authority. The Office outbox and accounting records remain the recovery source if a Cloud projection must be rebuilt.

## Read Boundary

The bounded account endpoint remains:

```text
GET /api/v1/client-portal/clients/{clientId}/commercial-summary
```

Its scalar and entitlement fields are unchanged. Its legacy document collections are returned empty so existing JSON deserializers remain compatible without carrying unbounded history.

Commercial history uses:

```text
GET /api/v1/client-portal/clients/{clientId}/commercial-documents
  ?documentType=Invoice
  &take=25
  &cursor={opaque continuation}
```

Rules:

- a client-scoped portal bearer session is required
- `documentType` is required and limited to the five known types
- `take` must be from 1 through 100
- order is `document_date DESC, document_id DESC`
- continuation is keyset based; no offset scan or total-count query is required
- malformed cursors return `CommercialDocumentCursorInvalid`

The supporting index is `(client_id, document_type, document_date DESC, document_id DESC)`. A second related-document index supports invoice-linked payment, credit-note, and credit-application lookup.

## Migration And Compatibility

Migration `20260712012439_NormalizeCommercialProjectionDocuments`:

1. adds bounded summary columns and the normalized document table
2. backfills scalar totals and all five GUID-keyed JSON collections
3. retains the latest entitlement as one bounded JSON value
4. creates the keyset and related-document indexes
5. removes the old `projection_json` column only after backfill

The reverse migration reconstructs the former JSON shape before removing normalized rows. Legacy rows have no source message per document, so their `last_message_id` is the zero GUID and their summary update time is retained as the best available timestamp.

The file provider remains a development fallback and pages its in-memory projection. PostgreSQL is the scale path.

## Remaining Scale Work

- paginate high-growth Office clients, outbox, statements, journals, invoices, and payment registers
- define retention for receipts, heartbeats, diagnostics, commands, and signed artifacts
- decide which large payloads move to object storage
- add measured concurrency and volume tests using accepted production targets
