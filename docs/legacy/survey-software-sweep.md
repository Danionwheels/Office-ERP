# Survey Access App Sweep

Date: 2026-06-30

## Sources

| Source | Path | Notes |
| --- | --- | --- |
| Access application | `E:/travel tour/survey/Actappl7.mdb` | Main legacy Survey/FAS Access shell. A working copy was exported from `work/survey-access-export/Actappl7-working.mdb`. |
| SQL script | `E:/travel tour/survey/Survey.sql` | SQL Server-style DDL script with Access upsizing metadata/extended properties. |
| SQL backup | `E:/travel tour/survey/ANI_backup_2025_11_19_120002_1863486.bak` | Present but not restored during this pass. |
| Archives | `Actappl7.rar`, `ANI_backup_2025_11_19_120002_1863486.rar` | Present but not extracted during this pass. |
| Linked MDBs | `ACTDATA7.mdb`, `VALDATA7.mdb`, `MYKDATA7.mdb`, `SCRDATA7.mdb` | Now present directly in the legacy folder. |
| Data archive | `SP.rar` | Lists the four linked MDB files above. |

Generated evidence:

- `work/survey-access-export/dao-inventory.json`
- `work/survey-access-export/tables.json`
- `work/survey-access-export/queries/`
- `work/survey-access-export/design/`

## Extraction Summary

`Actappl7.mdb` exported cleanly through the existing Access tooling:

| Object type | Count |
| --- | ---: |
| Tables | 111 |
| Queries | 122 |
| Forms | 178 |
| Reports | 183 |
| Modules | 6 |
| Macros/scripts | 3 |

`Survey.sql` contains 96 `CREATE TABLE` declarations. The script appears to represent a SQL Server/upsized form of the data model, while `Actappl7.mdb` remains the workflow/UI shell.

## Linked Data Shape

The Access app is mostly linked-table based:

| Linked target | Count | Likely responsibility |
| --- | ---: | --- |
| `E:\FAS_SURVEY\DATA\SP\ACTDATA7.mdb` | 52 | Accounting, customers, COA, sales/purchase documents, vouchers, assets, exchange rates. |
| `E:\FAS_SURVEY\DATA\SP\VALDATA7.mdb` | 26 | Survey valuation/docket setup, branches, document types, payment/service/parts/valuator setup, valuation invoice master/detail. |
| internal Access tables | 18 | Report working tables, application metadata, generated output buffers, temp/import tables. |
| `E:\FAS_SURVEY\DATA\SP\MYKDATA7.mdb` | 11 | Company, users, security, login/session, setup, backup paths, invoice signatures. |
| `E:\FAS_SURVEY\DATA\SP\SCRDATA7.mdb` | 3 | Screen/customer/docket shadow or support data. |
| `E:\FAS_SURVEY\SURVEY\MYKDATA7.mdb` | 1 | Legacy/alternate invoice-signature link. |

Implication: modernization should treat the linked MDB files or SQL backup as the source of business data, and `Actappl7.mdb` as the source of screen/report/workflow behavior.

## Startup And Security Flow

- `AUTOEXEC` runs `SYS_INI()`.
- `SYS_INI()` reads an encoded app/database configuration through `clsDB_Config`.
- The startup routine relinks tables using application metadata and database paths.
- It supports both Access-file and SQL Server-style connection paths in the code.
- The app title is `Software Professionals`.
- The startup form caption says `Financial Accounting System` and `with Survey Management System`.
- The startup form opens `MYKLOGIN`.
- `MYKLOGIN` validates users against `MYK_XO_USERS`.
- Security/permission checks are table-driven through `MYK_XM_SECURITY` and `MYK_XD_SECURITY`.
- Login/session state is written to `MYK_XO_LOGIN`.

Important migration point: this old security model should be replaced by the modern IdentityAccess/roles model, but the old program-code permissions are useful evidence for menu/module access mapping.

## Major Functional Areas

### Accounting/FAS

Evidence:

- `ACT_TM_VOUCHER`, `ACT_TD_VOUCHER`
- `ACT_TM_SI`, `ACT_TD_SI`
- `ACT_TM_PI`, `ACT_TD_PI`
- `ACT_TM_PR`, `ACT_TD_PR`
- `ACT_TM_SR`, `ACT_TD_SR`
- `ACT_TM_DC`, `ACT_TD_DC`
- `ACT_SD_COA_LEVEL3`
- `ACT_SM_COA_OPNBAL`, `ACT_SD_COA_OPNBAL`
- `ACT_SM_ASSET`, `ACT_SD_ASSET`
- `ACT_EXCHANGE_RATE`, `ACT_CONV_RATE`
- `ACT_DEBIT_NOTE`, `ACT_DEBIT_NOTE_A`

This confirms the survey app contains a full accounting backbone: chart of accounts, vouchers, opening balances, sales/purchase document flows, debit notes, assets, exchange rates, and financial reports.

### Survey / Valuation Operations

Evidence:

- `Docket`, `Docket_A`, `Docket_B`, `Docket_C`, `Docket_D`
- `VAL_INV_M`, `VAL_INV_D`
- `Valuators`, `Supervisers`
- `Ser_Type`, `Ser_Item`, `Ser_Category`
- `Parts`, `Parts_Status`
- `Wshop`
- `Request_by`
- `Payment_Type`
- `Document_Type`
- forms/reports such as `Docket`, `DocketQ`, `DSR`, `SUR_RPT`, `SUR_RPT_A`, `SUR_RPT_B`, `SUR_RPT_C`, `Inv_AV`, `Inv_VL`, `Inv_Fire`, `Inv_Takaful`, `Inv_Pledge`

This looks like a survey/valuation workflow around docket/job intake, customer/client, service types, valuators/supervisors, parts/workshops, reports, and valuation invoices.

### Customer And Branch Setup

Evidence:

- `ACT_SO_CUSTOMER`
- `ACT_SO_CUSTOMER_GROUP`
- `ACT_CUSTOMER_CITY`
- `ACT_CUSTOMER_COUNTRY`
- `ACT_CUSTOMER_ZONE`
- `Branches`
- `CL_Branches`
- `CL_Category`

This should map into a modern customer/client master, branch dimension, and reporting/customer segmentation model.

### Reporting

Evidence:

- 183 exported Access reports.
- Accounting report families: `ACTAL*`, `ACTAR*`, `ACTMR*`, `ACTBR*`.
- Survey/valuation report families: `Inv_*`, `Sur_Rpt*`, `Docket*`, `Grp_Rpts_*`, `PBA_RPT_*`, `CL_SR_AGING*`.
- Query evidence includes customer/job aging, docket crosstab, invoice reports, ledger/voucher reports, and tax/sales reports.

Migration risk: report parity will matter. The first reconciliation set should include customer aging, docket/job status summaries, valuation invoice totals, ledger, trial balance/balance sheet/P&L, and tax reports.

## Modernization Implications

1. Survey should probably be a first-party business module, not only custom screens.
2. The current app mixes office accounting and operational survey/valuation work. In the new product, keep accounting as shared core and survey as an optional module.
3. Existing `ACT_*` accounting structures overlap with the current SafarSuite accounting concepts, so we should avoid duplicating accounting models for Survey.
4. Survey-specific data should map into a module boundary such as `SurveyValuation`:
   - dockets/jobs
   - job status
   - service types/items/categories
   - valuators/supervisors
   - valuation invoices
   - survey reports
5. Legacy security/menu permissions should be imported as role templates, not kept as direct table-driven Access permission logic.
6. The linked MDB split suggests deployment historically separated:
   - accounting data
   - valuation/survey data
   - security/company data
   - screen/support data
   A modern schema can keep these as module schemas while using one local PostgreSQL database.

## Candidate Module Mapping

| Legacy area | Modern module |
| --- | --- |
| `MYK_*` users/security/company/setup | IdentityAccess, TenantBranch, PlatformSettings |
| `ACT_SD_COA_LEVEL3`, opening balance, vouchers | Accounting/Ledger |
| `ACT_TM_*`, `ACT_TD_*` sales/purchase/receipt/document flows | Accounting documents, invoicing, receivables/payables |
| `ACT_SM_ASSET`, `ACT_SD_ASSET` | Assets |
| `Docket*` | SurveyValuation jobs/dockets |
| `VAL_INV_M`, `VAL_INV_D` | SurveyValuation invoicing, integrated with Accounting |
| `Valuators`, `Supervisers`, `Wshop`, `Parts` | SurveyValuation reference/master data |
| `Ser_*`, `Document_Type`, `Payment_Type` | SurveyValuation setup/reference data |
| `Inv_*`, `Sur_Rpt*`, `Grp_Rpts_*` reports | Reporting, with survey-specific report catalog |

## Recommended Next Steps

1. Restore or inspect `ANI_backup_2025_11_19_120002_1863486.bak` only if we need row counts/sample data or SQL Server constraints beyond `Survey.sql`.
2. Inspect the linked MDB files now present in the folder (`ACTDATA7.mdb`, `VALDATA7.mdb`, `MYKDATA7.mdb`, `SCRDATA7.mdb`) for row counts/sample data when database tooling is available.
3. Build a focused workflow map for:
   - docket/job creation
   - valuation invoice creation
   - receipt/payment posting
   - GL posting
   - customer/job aging
4. Pick 5-7 critical reports for parity testing.
5. Decide whether Survey is part of V1, V2, or a later paid module under the cloud entitlement system.

## Initial Decision

Treat Survey as a separate optional business module that reuses the same accounting, customer, identity, reporting, licensing, and sync foundations. Do not fork a second accounting engine for it.

## Accounting-Focused Second Pass

The invoice creation module should not be treated as only the `Docket_A` detail grid.

The updated accounting sweep found this split:

| Workflow level | Legacy evidence | Modern meaning |
| --- | --- | --- |
| Invoice preparation inside survey job | `Docket_A` | Prepared rows and GL/tax hints for a survey job |
| Valuation invoice batch generation | `INV_GEN`, `VAL_INV_M`, `VAL_INV_D` | Select dockets and create valuation invoice master/detail records |
| Sales invoice, receipt, and ledger | `ACT_TM_SI`, `ACT_TD_SI`, `ACT_TM_SR`, `ACT_TD_SR`, `ACT_TM_VOUCHER`, `ACT_TD_VOUCHER` | Shared Billing, Payments, and Accounting foundation |
| Posting setup | `Inv_GL_Setup`, `Tax`, `ACT_SO_ITEM`, `ACT_SO_CATEGORY` | Charge/tax/posting profiles |

Corrected implementation direction:

1. Build a basic `Accounting` module for chart of accounts, ledger accounts, journal entries, and balanced journal lines.
2. Extend `Billing` for dynamic client charges and invoice drafts.
3. Finalize invoices through Accounting postings.
4. Record receipts/payments with allocation and Accounting postings.
5. Return to valuation invoice batch creation only after the shared billing/accounting path exists.

Detailed note:

```text
docs/planning/accounting-gl-foundation-sweep.md
```
