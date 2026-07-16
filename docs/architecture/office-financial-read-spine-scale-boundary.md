# Office Financial Read Spine Scale Boundary

Date: 2026-07-12

## Decision

The Office API no longer returns a client's complete financial lifetime as one statement payload.

The former `GET /api/v1/clients/{clientId}/statement` path loaded every invoice, payment, credit note, refund, and credit application, scanned six company-wide journal streams, loaded the complete chart of accounts, and assembled all running balances in application memory. Its response and query work grew without a contract limit.

The replacement has one exact scalar summary and four independent keyset registers:

| Read | Route | Default | Maximum | Continuation key |
| --- | --- | ---: | ---: | --- |
| Currency position | `GET /api/v1/clients/{clientId}/financial-summary` | scalar | scalar | none |
| Invoices | `GET /api/v1/clients/{clientId}/invoices` | 25 | 100 | issue date, created UTC, invoice ID |
| Payments | `GET /api/v1/clients/{clientId}/payments` | 25 | 100 | received date, recorded UTC, payment ID |
| Financial activity | `GET /api/v1/clients/{clientId}/financial-activity` | 25 | 100 | entry date, type order, reference, document ID |
| Journal evidence | `GET /api/v1/clients/{clientId}/journal-postings` | 20 | 100 | entry date, created UTC, journal ID |

The company GL register at `GET /api/v1/accounting/journal-entries` is also a keyset page, defaults to 50, and is capped at 100. Register rows contain `LineCount`; debit and credit lines are loaded only through the existing single-journal detail route.

## Ownership And Evidence

Commercial journal entries now persist two nullable facts:

- `client_id`: the client that owns the commercial event
- `source_document_id`: the immutable invoice, payment, credit-note, or refund ID

They remain nullable because manual, opening-balance, adjustment, and period-close journals are company-level evidence.

Invoice issue/void, credit-note, payment receipt/reversal, and client-refund writes assign both facts when the journal is created. Document reads and correction workflows query the indexed source ID instead of loading a whole journal type and comparing reference text.

Migration `20260712183635_AddClientOwnedFinancialReadSpine` backfills legacy commercial journals from their source tables before adding the client foreign key. Payment references were historically non-unique, so legacy payment journals select the closest recorded payment by UTC timestamp; all new writes are unambiguous. The populated migration proof found five commercial journals owned, zero unowned, and five source IDs populated.

## Query Semantics

Every cursor is opaque and bound to its client, date window, search, and status/type filter. Reusing a cursor against another query returns validation status `400`.

All pages sort newest first and request one extra row to determine `HasMore`. `FilteredCount` is authoritative for the whole filtered register; the UI labels loaded rows separately and follows `NextCursor` on demand.

Currency summaries remain exact database aggregates. Financial activity is a database union of invoices, voids, receipts, reversals, credit notes, refunds, and applied credit. PostgreSQL computes per-currency running balances in chronological order before returning the newest page, so application memory never reconstructs the full ledger.

Invoice totals still derive from normalized invoice lines. Per-row journal lookup and journal-line aggregation happen after page selection, so detail work is limited to returned rows.

## Office UI Contract

`ClientStatement` is now only a frontend workspace composition. `getClientStatement` requests the scalar summary and the first page of each register concurrently. Client Desk, Client 360 billing, receipts, vouchers, access selection, and the GL register expose continuation controls. Exact counts no longer pretend that the loaded page is the complete register.

Refund and credit-application decisions use the aggregate financial reader directly. They no longer load four complete client repositories before accepting a write.

## Proof

The repeatable fixture at `tools/SafarSuite.ControlDesk.AccountingSmoke/financial-read-volume-seed.sql` added one client's high-growth history on top of the accounting smoke chain:

| Data | Rows |
| --- | ---: |
| Invoices | 20,002 |
| Payments | 6,667 |
| Client-owned journals | 26,671 |
| Journal lines | 53,345 |
| Unified financial activity | 26,672 |

Observed warm local HTTP timings against PostgreSQL on the development machine were:

| Read | Time |
| --- | ---: |
| Exact currency summary | 121 ms |
| 25 invoices | 159 ms |
| 25 payments | 75 ms |
| 25 activity rows with running balance | 351 ms |
| 20 client journals | 67 ms |
| 50 company journals | 54 ms |

These are boundary observations, not production SLAs. The proof also traversed two non-overlapping invoice pages, rejected a cursor after its search changed, and returned exact totals of 20,002 invoices and 13,335 open invoices.

The accounting smoke now proves, for both in-memory and clean PostgreSQL providers:

- exact invoice, paid, credit, and balance totals
- bounded invoice, payment, activity, client-journal, and company-journal pages
- source-owned journal evidence with bounded line counts
- non-overlapping continuation
- cursor rejection after filter changes
- refund and applied-credit behavior through aggregate balance reads

## Remaining Scale Work

This checkpoint bounds interactive payloads and removes global commercial journal scans. Exact all-time summaries and running-balance windows still perform work proportional to one client's selected history. If measured production targets exceed this proof, the next evolution is an incrementally maintained per-client/currency rollup and activity balance checkpoints, not larger response limits.

Trial balance, balance sheet, profit and loss, period-close readiness, and ledger-account activity still use complete accounting collections internally. They need a separate SQL reporting/read-model checkpoint. Retention and archival policy for old commercial evidence also remains open.
