# Connected Acceptance Chain Proof — 17 July 2026

Status: **Passed**

This record closes the P0 connected acceptance gate for one clean client flow:

`client → contract/pricing → invoice/journal → payment/journal → entitlement → Office outbox → Control Cloud receipt/signing → Local Server import/enforcement → heartbeat → Control Desk reconciliation`

It does **not** declare production operational readiness. Backup/restore, monitoring, production key custody and rotation, recovery procedures, retention, and production configuration remain separate gates.

## Execution identity

| Field | Value |
| --- | --- |
| Run ID | `ca-20260717022147-1b0d6cc671e1476aa` |
| Started | `2026-07-17T02:21:47.7563114+00:00` |
| Completed | `2026-07-17T02:22:07.0559231+00:00` |
| API source baseline | `5842ef61960da5ae19993da954425d950dff74af` |
| Database | Fresh PostgreSQL database with all committed Control Desk and Control Cloud migrations |
| Hosts | Control Desk `127.0.0.1:5188`; Control Cloud `127.0.0.1:5127`; Local Server `127.0.0.1:51046` |
| Runner | `tools/SafarSuite.ConnectedAcceptance` |
| Passed assertions | `135` |
| Runner evidence SHA-256 (CRLF artifact) | `6f4a410f7fc215fcaab62783fe2a7e4f4faafeeb8c8c204b59470a1b1f45e46d` |
| Committed evidence SHA-256 (normalized LF) | `c57f9305533d4b50525db24b546caf4cfa2a5bbdc060f727d415f350c7dcf327` |

The run used a dedicated empty database and isolated Local Server state files. Earlier harness calibration runs were discarded; the IDs below belong only to the final clean run.

## Commercial and accounting chain

### Client, deployment, and contract

| Evidence | Value |
| --- | --- |
| Client ID | `b581e053-7460-4f0d-9ac8-e11029e5ea6b` |
| Client code | `ACC1E1476AA` |
| Created | `2026-07-17T02:21:49.841273+00:00` |
| Activated | `2026-07-17T02:21:50.0358493+00:00` |
| Deployment ID | `b9a48179-387b-4ba8-84c0-d828a6fee1ce` |
| Installation ID | `acceptance-ca-20260717022147-1b0d6cc671e1476aa` |
| Deployment topology | `OnlineBootstrap / CloudSyncMultiBranch / Hq / HQ` |
| Contract ID | `7d2464b0-8432-4353-bbca-bb1782e827ea` |
| Contract revision | `1` |
| Contract number | `ACC-CON-1E1476AA` |
| Contract dates | `2026-07-17` through `2027-07-16` |
| Product catalog revision | `9c1da88b-c763-4bb0-8dda-2d95fe63ec8f`, revision `1` |
| Recurring price | `PKR 12,500.00`, monthly |
| Approval | `Local Control Desk Admin` at `2026-07-17T02:21:50.3306006+00:00` |

Approved desired access:

- Modules: `CONTROL_DESK=true`, `PAYROLL=true`, `TOUR=false`.
- Limits: devices `12`, branches `4`, named users `80`, concurrent users `25`.
- Feature limit: `PAYROLL/MONTHLY_PAYSLIPS=500 COUNT`.

### Pricing, invoice, and payment

| Evidence | Value |
| --- | --- |
| Charge code ID | `157e967b-6fe1-4a4d-b6ef-6d9fd6b69eb4` |
| Client charge rule ID | `070e644b-ff2f-4722-a0b4-2dc0ea7b3d12` |
| Revenue account | `88b4d82c-2ce9-4cad-adca-685291263d0f` (`41000`, Subscription revenue) |
| Receivable account | `95ad9fb9-9988-45a8-bb3e-f3e02ae70754` (`151000001`, Client receivables) |
| Cash/bank account | `b55130e9-8b0f-4a56-8f51-3be0f7c85d51` (`14110`, Cash on hand and bank) |
| Invoice ID / number | `00e0e583-00b0-4dce-875a-1307c2fe3369` / `ACC-INV-1E1476AA` |
| Invoice result | `Issued`, `PKR 12,500.00` |
| Invoice journal | `ac0b151c-277f-4d3c-adf1-e7b77c63807c`, `Posted`, debit `12,500.00`, credit `12,500.00` |
| Payment ID / reference | `6502a7cf-2353-4ad5-9a0f-3aae558dad45` / `ACC-PAY-1E1476AA` |
| Payment method and transition | `BankTransfer`: `PendingReview → Approved` |
| Payment recorded | `2026-07-17T02:21:51.091020+00:00` |
| Paid invoice result | `Paid`, balance `0.00` |
| Payment journal | `b65266bd-725f-423a-9782-d22f553007ee`, `Posted`, debit `12,500.00`, credit `12,500.00` |

Both commercial postings balance exactly. The bank transfer produced no journal while pending; approval produced the payment journal and fully settled the invoice.

## Immutable entitlement

| Evidence | Value |
| --- | --- |
| Entitlement snapshot ID | `e2fe9e14-fcff-4e75-bc05-fde809330c66` |
| Client access revision ID | `034b7084-02ce-422f-b658-8c81daf0b29c` |
| Entitlement version | `1` |
| Source invoice | `00e0e583-00b0-4dce-875a-1307c2fe3369` |
| Source contract/revision | `7d2464b0-8432-4353-bbca-bb1782e827ea` / `1` |
| Source catalog/revision | `9c1da88b-c763-4bb0-8dda-2d95fe63ec8f` / `1` |
| Issued/effective | `2026-07-17T02:21:51.3219768+00:00` |
| Status | `Active` |
| Paid / grace / offline-valid until | `2027-07-16` / `2027-07-23` / `2027-07-30` |
| Approval | `Local Control Desk Admin`: `Paid invoice authorizes P0 connected acceptance` |

The snapshot preserved the approved modules and all four capacity limits plus the module-scoped feature limit exactly.

## Office outbox and Control Cloud receipts

Every target message moved from `Pending` to `Sent` in one attempt and returned a cloud reference plus HMAC-SHA256 envelope signature.

| Event | Outbox message ID | Sent at UTC | Cloud reference | Envelope signature |
| --- | --- | --- | --- | --- |
| `InvoiceIssued` | `f291b15a-d5ec-4f73-94a8-8cebb2282dbf` | `2026-07-17T02:21:53.995558+00:00` | `cc-f291b15ad5ec4f7394a88cebb2282dbf` | `Du9KkeIA2BoqHFVNZoyE3vNuFWFpFSnbrJ3DXPmpeMw=` |
| `PaymentRecorded` | `9463b733-2d9f-4922-99d3-fa3b6bf28f56` | `2026-07-17T02:21:54.028131+00:00` | `cc-9463b7332d9f492299d3fa3b6bf28f56` | `K82n9OWd4ArlXvu9Aspfi+fehrkwGkoWkcVjpGeKVsQ=` |
| `ClientPaidStatusChanged` | `4970490b-c528-44eb-99a5-e8004a129b74` | `2026-07-17T02:21:54.04626+00:00` | `cc-4970490bc52844eb99a5e8004a129b74` | `VVcrLoCWTlyg+pzN+6PDC225zVZxHavZAsJXPhDJj7c=` |
| `EntitlementSnapshotIssued` | `c0aa6321-7af8-49c2-96e9-f0dc43f93f24` | `2026-07-17T02:21:54.065006+00:00` | `cc-c0aa63217af849c296e9f0dc43f93f24` | `GhNPpDSIwSHKbxgSChe1OhAbisRqR6YoWiCmuOM24+c=` |

Control Cloud persisted these receipts under signing key `local-dev`:

| Event | Receipt ID | Status | Received at UTC |
| --- | --- | --- | --- |
| `InvoiceIssued` | `ab2f0ed0-d50d-4483-b036-dbed4f8d1a30` | `Accepted` | `2026-07-17T02:21:53.687783+00:00` |
| `PaymentRecorded` | `dc9096b7-51c1-4a61-97c3-e043be3080d3` | `Accepted` | `2026-07-17T02:21:54.006857+00:00` |
| `ClientPaidStatusChanged` | `c652b143-2350-4be2-94fd-204057be92df` | `Accepted` | `2026-07-17T02:21:54.037206+00:00` |
| `EntitlementSnapshotIssued` | `678c37bb-3df9-4afc-835e-1ee216c31a84` | `Accepted` | `2026-07-17T02:21:54.050943+00:00` |
| Replayed `EntitlementSnapshotIssued` | `dc578651-6b95-409b-9550-d0602da8c793` | `Duplicate` | `2026-07-17T02:21:54.094959+00:00` |

The replay reused message ID `c0aa6321-7af8-49c2-96e9-f0dc43f93f24` and idempotency key `SafarSuite.ControlDesk:c0aa63217af849c296e9f0dc43f93f24`. It returned the original cloud reference and persisted detail `Duplicate idempotency key.`, demonstrating idempotent receipt handling without claiming a separate projection-count measurement.

## Bootstrap, signed delivery, and local enforcement

| Evidence | Value |
| --- | --- |
| Bootstrap package ID | `7e7abbe4-1406-43bc-9b1c-e4e139be35aa` |
| Setup token ID | `6d1ff2fb-f5c7-47d5-a19b-436c11566bf9` |
| Bootstrap generated | `2026-07-17T02:21:56.2290274+00:00` |
| Bootstrap bundle SHA-256 | `21ab0ea14692fa70a6f256e04ea2cad3d1d9c4cec5b03d193fc9242e690c5160` |
| Bootstrap signature | `HMAC-SHA256`, key `local-entitlement-dev`, payload `e791320c11c9f3661c1c02e0722a599c7694dfc7a810ba5dd1b9b424254bda77` |
| Cloud registration | `Active` at `2026-07-17T02:21:58.4316323+00:00` |
| Signed entitlement bundle issue ID | `c5cdb013-ec1c-49da-830a-f44072dedbb5` |
| Signed entitlement payload SHA-256 | `f6c014751acfb09c6e4147e17ea89b4f6efb1202986b282ef3fe4ada4fc33653` |
| Local pull completed | `2026-07-17T02:21:58.508028+00:00` |
| Local import audit ID | `03129ac4-27fa-4f6f-8d4d-565af1bec539` |
| Local import result | `ControlCloudPull / Accepted / entitlement version 1` |

Local enforcement results:

- `PAYROLL`: allowed, state `Active`, checked `2026-07-17T02:22:00.6855255+00:00`.
- `TOUR`: denied, state `ModuleDisabled`, reason `Requested module is not enabled in the cached entitlement.`, checked `2026-07-17T02:22:00.6953978+00:00`.
- Local limits were usable and exactly `12 / 4 / 80 / 25`, with `PAYROLL/MONTHLY_PAYSLIPS=500 COUNT`.

## Acknowledgement and closed-loop reconciliation

| Evidence | Value |
| --- | --- |
| Heartbeat ID | `794db3a4-205c-41c8-a4b2-159b58f529c3` |
| Heartbeat reported / received | `2026-07-17T02:22:00.711194+00:00` / `2026-07-17T02:22:02.754292+00:00` |
| Heartbeat status / license | `Received / Active` |
| Desired / signed / observed versions | `1 / 1 / 1` |
| Version sync | `InSync` |
| Reconciliation evaluated | `2026-07-17T02:22:04.9155898+00:00` |
| Reconciliation result | `InSync`; `0` differences |

Authoritative value comparison:

| Field | Desired | Delivered | Observed |
| --- | --- | --- | --- |
| Version | `1` | `1` | `1` |
| Effective from | `2026-07-17T02:21:51.3219768+00:00` | same | same |
| Status | `Active` | `Active` | `Active` |
| Paid until | `2027-07-16` | same | same |
| Grace until | `2027-07-23` | same | same |
| Offline-valid until | `2027-07-30` | same | same |
| Devices / branches | `12 / 4` | same | same |
| Named / concurrent users | `80 / 25` | same | same |
| Modules | `CONTROL_DESK=true; PAYROLL=true; TOUR=false` | same | same |
| Feature limits | `PAYROLL/MONTHLY_PAYSLIPS=500 COUNT` | same | same |

`WarningStartsAt` is derived during signing rather than stored in desired state. Delivered and observed both contained `2027-07-09`; the signed bundle record contained the same value. The reconciler reported no difference.

Control Desk also returned the installation audit chain:

| Audit event | Audit ID | Occurred at UTC |
| --- | --- | --- |
| `SetupTokenCreated` | `e70b2165-4ead-4a5e-b533-6b4dc29c51ba` | `2026-07-17T02:21:56.1765233+00:00` |
| `BootstrapPackageGenerated` | `1f28cbbb-a042-4d11-9331-898dfe93f75d` | `2026-07-17T02:21:56.2290274+00:00` |
| `LocalServerRegistrationAccepted` | `ed2d3b9c-0ffd-483a-b6e0-42e071b53099` | `2026-07-17T02:21:58.4316323+00:00` |

## Acceptance decision

- [x] One active client was created and retained.
- [x] An approved contract revision, custom price, catalog revision, modules, and limits were retained.
- [x] An invoice was issued with a posted, balanced provider journal.
- [x] A bank-transfer payment was reviewed, approved, posted, and settled the invoice.
- [x] An immutable entitlement/access revision was issued from that paid invoice.
- [x] All Office outbox messages were signed, sent, and read back as sent.
- [x] Control Cloud accepted each event and handled a replay idempotently as `Duplicate`.
- [x] Control Cloud produced a signed bundle and registered the intended installation.
- [x] Local Server imported the real bundle and enforced one deterministic allow and deny.
- [x] Heartbeat acknowledgement returned exact observed values.
- [x] Control Desk showed desired, signed, and observed version `1`, exact authoritative value agreement, `InSync`, and zero differences.
- [x] Exact IDs, timestamps, journal totals, signatures, hashes, receipt rows, and audit IDs were retained.

Result: the **Connected Acceptance Chain gate passes**. The next gate is product/operational readiness, not production deployment.

## Evidence handling and reproduction

The runner wrote a sanitized JSON ledger and a sibling SHA-256 file under the ignored `.codex-run` directory. An equivalent LF-normalized copy is committed at `docs/planning/evidence/connected-acceptance-chain-2026-07-17.json` with its sibling `.sha256` file so a clean checkout can recompute it. The two JSON documents have identical values; only line endings differ.

The ledger intentionally excludes the operator bearer token, operator password, Cloud signing secret, and one-time setup token. It retains the non-secret setup-token ID, key IDs, signatures, payload hashes, and all business/audit IDs.

Re-run against healthy isolated hosts with credentials supplied only through environment variables:

```powershell
dotnet run --project tools/SafarSuite.ConnectedAcceptance/SafarSuite.ConnectedAcceptance.csproj --configuration Release -- `
  --evidence-path <isolated-output.json> `
  --local-import-audit-path <isolated-local-import-audit.json>
```

Required environment variable names are documented by `--help`; no credential values are committed.
