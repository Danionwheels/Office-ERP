# Legacy Source Reference

Date confirmed: 2026-06-30

Use this folder as the canonical legacy application reference for cloning core SafarSuite Control Desk behavior:

```text
E:/travel tour/survey
```

## Source Files

| File | Purpose | Size | Last modified |
| --- | --- | ---: | --- |
| `Actappl7.mdb` | Access application shell: forms, reports, modules, startup/login flow, linked-table behavior | 20,803,584 bytes | 2026-06-24 11:38:02 |
| `Survey.sql` | SQL Server-style schema export / upsized data model | 8,428,856 bytes | 2026-06-30 14:08:55 |
| `ACTDATA7.mdb` | Linked accounting data: COA, customers, vouchers, sales invoices, receipts, assets, rates | 2,691,072 bytes | 2026-06-24 10:58:39 |
| `VALDATA7.mdb` | Linked survey/valuation data: dockets, setup, valuation invoices | 7,217,152 bytes | 2026-02-26 13:01:24 |
| `MYKDATA7.mdb` | Linked company/security/settings/signature data | 9,568,256 bytes | 2026-06-24 10:58:54 |
| `SCRDATA7.mdb` | Linked screen/support/shadow data | 868,352 bytes | 2019-02-11 12:30:19 |
| `ANI_backup_2025_11_19_120002_1863486.bak` | SQL Server backup, likely useful for row counts/sample data if restored later | 18,960,896 bytes | 2025-11-20 01:00:02 |
| `Actappl7.rar` | Archive copy of the Access app | 2,083,518 bytes | 2026-06-30 13:23:19 |
| `SP.rar` | Archive containing `ACTDATA7.mdb`, `MYKDATA7.mdb`, `SCRDATA7.mdb`, `VALDATA7.mdb` | 602,985 bytes | 2026-06-30 20:32:40 |
| `ANI_backup_2025_11_19_120002_1863486.rar` | Archive copy of the SQL backup | 1,892,519 bytes | 2026-06-30 13:20:16 |

`Survey.sql` currently contains 96 `CREATE TABLE` declarations. `Actappl7.mdb` was previously exported and inventoried in the old SafarSuite workspace; those tracker docs have been copied into this project under `docs/planning`.

## Clone Rule

Clone the business meaning and core workflows, not the old Access implementation.

Core behavior to preserve:

- company and office setup
- customer/client master data
- chart of accounts and GL posting meaning
- vouchers, sales invoices, receipts, purchase documents, debit notes, opening balances
- payment methods and receivable allocation behavior
- assets and related accounting movement
- survey/valuation docket workflow where still commercially useful
- valuation invoice behavior
- high-value reports such as invoice printouts, receivable aging, voucher registers, ledger, trial balance, balance sheet, profit and loss, and selected survey reports

Behavior to replace or modernize:

- Access startup/relinking mechanics
- old password/security storage
- old table-driven menu permission implementation
- temporary/report-buffer tables unless needed for historical import or report parity
- exact Access form layout where a modern desktop workflow is clearer

## First Reference Pass

Start schema and workflow mapping from these legacy object families:

- `MYK_*`: company, user, login, security/setup evidence
- `ACT_SD_COA_LEVEL3`, `ACT_TM_VOUCHER`, `ACT_TD_VOUCHER`: accounting foundation
- `ACT_TM_SI`, `ACT_TD_SI`, `ACT_TM_SR`, `ACT_TD_SR`: invoice and receipt behavior
- `ACT_SO_CUSTOMER`, customer city/country/zone/group tables: client and party master data
- `ACT_SM_ASSET`, `ACT_SD_ASSET`: asset management
- `Docket`, `Docket_A`, `Docket_B`, `Docket_C`, `Docket_D`: survey/valuation jobs
- `VAL_INV_M`, `VAL_INV_D`: valuation invoices
- `Ser_Type`, `Ser_Item`, `Ser_Category`, `Payment_Type`, `Document_Type`: setup/reference data

Keep SafarSuite Control Desk focused on the new SafarSuite provider-control vertical slice first, then pull legacy accounting and survey workflows forward in ranked order.

## Accounting Correction

The invoice creation workflow is broader than `Docket_A`.

Use `Docket_A` as survey invoice preparation evidence, but use `VAL_INV_M`, `VAL_INV_D`, `ACT_TM_SI`, `ACT_TD_SI`, `ACT_TM_SR`, `ACT_TD_SR`, `ACT_TM_VOUCHER`, `ACT_TD_VOUCHER`, and `Inv_GL_Setup` as the accounting/billing evidence for the modern foundation.

The current direction is documented in:

```text
docs/planning/accounting-gl-foundation-sweep.md
```
