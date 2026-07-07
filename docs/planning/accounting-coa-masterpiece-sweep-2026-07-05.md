# Accounting COA Masterpiece Sweep - 2026-07-05

Purpose: consolidate the best chart-of-accounts design principles, the Peachtree/Sage-style legacy evidence, and the current SafarSuite accounting implementation into one practical blueprint for the next polish pass.

Provider-office product design follow-up: `docs/planning/provider-office-erp-accounting-experience-masterplan-2026-07-05.md`.

## Sources Inspected

Local evidence:

- `C:/Users/Daniyal/Downloads/GL_Working.xlsx`
  - Extracted workbook package under `artifacts/codex/gl-working-xlsx-extract-20260705`.
  - Structured extraction saved to `artifacts/codex/accounting-evidence.json`.
- `C:/Users/Daniyal/Downloads/Data.sql`
  - Parsed SQL Server-style legacy schema for accounting-related tables, views, and stored procedures.
- `E:/travel tour/travel/TRV.mde`
  - File is present, size `55,074,816` bytes, last modified `2026-06-10`.
  - Direct Access schema extraction was blocked because neither `Microsoft.ACE.OLEDB.12.0` nor `Microsoft.Jet.OLEDB.4.0` is installed in this environment.
  - Best-effort binary string recovery succeeded. Outputs are under `artifacts/codex/trv-mde-extract-20260705`.
  - Recovered text includes Access form/report classes, event/procedure names, embedded SQL fragments, report formulas, and accounting labels. Compiled VBA bodies in an MDE are still not fully reliable without the original MDB/ACCDB source or Access export tooling.
- `E:/travel tour/travel/TRV.mdb`
  - Original Access source database found beside the MDE, size `53,907,456` bytes, last modified `2026-07-05`.
  - Microsoft Access 2010 automation exported source text successfully under `artifacts/codex/trv-mdb-access-export-20260705`.
  - This supersedes the MDE limitation for forms, reports, macros, modules, and saved queries.
- SafarSuite code under:
  - `src/SafarSuite.ControlDesk.Domain/Modules/Accounting`
  - `src/SafarSuite.ControlDesk.Application/Modules/Accounting`
  - `src/SafarSuite.ControlDesk.Infrastructure/Persistence/EntityFramework/Configurations`
  - `apps/control-desk-ui/src/modules/accounting`
  - `tools/SafarSuite.ControlDesk.AccountingSmoke`

External reference points:

- IFRS Conceptual Framework: financial reporting is built around assets, liabilities, equity, income, expenses, recognition, measurement, presentation, and disclosure.
- Microsoft Dynamics 365 Finance: world-class ERP designs keep the main chart clean, then use financial dimensions for department, cost center, purpose, project, legal entity, and other analysis needs.
- Peachtree/Sage 50 lineage: Peachtree became the US Sage 50 product line, so it is a valid historical inspiration for SMB accounting ergonomics, but SafarSuite should learn from the model rather than clone old limitations.

## Executive Position

A great COA is not just a list of accounts. It is the controlled accounting language of the product.

The best design has eight layers:

1. Financial statement skeleton: assets, liabilities, equity, revenue/income, expense, gains/losses, contra accounts, and retained earnings.
2. Controlled numeric coding: stable ranges, reserved gaps, parent-aware suggestions, and no arbitrary daily ledger codes.
3. Hierarchy: non-posting report/group accounts and posting leaf accounts.
4. Subledger identity: customers, suppliers, employees, banks, and agents can be ledger identities without turning every operational attribute into a GL account.
5. Posting engine: immutable balanced journals/vouchers, source-document links, period controls, and reversal journals instead of silent edits.
6. Dimensions: branch, department, cost center, project/file, product/module, and tax context are dimensions or subledger fields, not uncontrolled account-code sprawl.
7. Reporting metadata: COA ranges and report-format rules produce trial balance, balance sheet, P&L, cash flow, comparative reports, and audit drill-down.
8. Governance: active/inactive status, lock rules, import dry-runs, repair plans, permissions, audit trail, and versioned setup changes.

SafarSuite is already strong on layers 1, 2, 3, 5, and part of 7. The masterpiece polish is mostly about import/parity, dimensions, voucher semantics, base/foreign currency, reporting rollups, and party/supplier breadth.

## Best COA Structure

Use the legacy `H/T/M/D/C/S` model because it is expressive and matches the workbook:

| Code | Level | Posting | Meaning |
| --- | --- | --- | --- |
| `H` | Header | No | Starts a major statement section or group. |
| `T` | Total | No | Closes or sums a group for reporting. |
| `M` | Master | No | Groups detail/control accounts under a total. |
| `D` | Detail | Yes | Normal posting account under a master. |
| `C` | Control | No | Owns subsidiary accounts and sums into a master. |
| `S` | Subsidiary | Yes | Party-level or subsidiary posting account under a control. |

Legacy nesting rule to preserve:

- The COA register is an inline nested account tree, not a fixed five-level form workflow.
- `H/T/M/D/C/S` describe the account's accounting role and posting behavior; they must not cap the number of tree levels.
- `Total` rows are real visible COA rows in the list, not hidden report-only calculations. They can sit inside the hierarchy and participate in rollups.
- Users must be able to add child, grandchild, and deeper rows inline under the selected account context, with no practical nesting limit other than validation and readability.
- Store the tree with `parentAccountId` adjacency and computed rollups, then validate posting/non-posting behavior from the row's account role.

Recommended account master fields:

| Field | Why it matters |
| --- | --- |
| Company/legal entity | Required for future multi-company. Keep `MAIN` as the current boundary. |
| Code and display code | Store digits only; display `15100-0001` for subsidiaries. |
| Name/short name | Human-readable ledger identity. |
| Type | Asset, Liability, Equity, Revenue, Expense, Gain, Loss, Contra. SafarSuite currently has the first five. |
| Normal balance | Debit or credit, used by reports and validation. |
| Level | `H/T/M/D/C/S`, drives posting and rollup behavior. |
| Parent account id/code | Required for subsidiary/control and detail/master linkage. |
| Summary/total/report range | Needed for classic report format and rollup. |
| Posting flag | Must agree with the level. |
| Currency | Default account currency, with base/foreign posting support later. |
| Statement category | Balance sheet, P&L, cash flow, disclosure/report line. |
| Role | AR, AP, cash, bank, tax payable, retained earnings, income summary, rounding, revenue, discount, refund, etc. |
| Status and lock state | Active, inactive, archived/locked. Never delete a posted account. |
| Valid from/to | Useful for reorganizing a chart without corrupting history. |
| Audit fields | Created/updated/approved by and timestamps. |

## Recommended Code Ranges

Keep the workbook's range philosophy:

| Range | Meaning |
| --- | --- |
| `10000-19999` | Assets |
| `20000-29999` | Capital/equity |
| `30000-39999` | Liabilities |
| `40000-59999` | Revenue/income |
| `60000-99999` | Expenses |
| 5 digits | Normal header/total/master/detail/control accounts |
| 9 digits | Subsidiary accounts under a 5-digit control prefix |

Specific rule to preserve:

```text
15100      Accounts Receivable control
151000001  First customer/client subsidiary
151000002  Second customer/client subsidiary
UI display: 15100-0001, 15100-0002
Raw storage: 151000001, 151000002
```

Do not encode branch, project, travel file, module, department, or salesperson into the account number. Use dimensions/subledger fields. The account number should answer "what kind of balance is this?" not every reporting question.

## Workbook Evidence

`GL_Working.xlsx` has eight sheets:

| Sheet | Evidence |
| --- | --- |
| `List` | Setup menu: company, branch, GL setup, account opening, currency, exchange rate, voucher type, opening balances, invoice opening, voucher input, single voucher, multiple voucher. |
| `COA` | Account setup screen, hierarchy legend, code ranges, account mapping, sample chart rows, parent/link behavior, lock/edit notes. |
| `Opening` | Opening balance master/detail shape, P&L account for the year, branch, COA code, currency, debit/credit foreign/base totals. |
| `V-Journal` | Voucher type setup: code, description, type, renumber flag, sales tax flag, user/system behavior, journal mapping. |
| `Voucher` | Voucher list/view and voucher entry requirements, filters, auto voucher number, payment/receipt cash side behavior, account/currency/foreign debit/credit lines. |
| `Voucher-2` | Multiple voucher entry fields and parseable backend function names such as voucher save and voucher number generation. |
| `V-Single` | Single payment/receipt voucher flow with fixed cash/bank line and detail rows. |
| `Tables` | Supporting setup tables, branch/currency/company setup, report columns, AP/cash/bank/tax start/end setup. |

Important rows extracted:

- `COA` defines `H` Header, `T` Total, `M` Master, `D` Detail, `C` Control, `S` Subsidiary.
- `COA` says non-subsidiary accounts use 5-digit control/account codes; subsidiary accounts are a 5 plus 4 or 5 plus 5 child code.
- `COA` shows `15100` as Accounts Receivable and `15100-0001` as a subsidiary-style display.
- `COA` requires selected header/account context to constrain the next code to the range or auto `+1`.
- `Opening` requires branch, account code, currency, debit amount, credit amount, and total debit/credit in foreign/base currency.
- `V-Journal` includes type meanings: Journal `J`, Payment `P`, Receipt `R`, Purchase `A`, Purchase Return `B`, Sales `C`, Sales Return `D`, Transfer `E`, Challan `F`, Production `G`, Order `H`, Fuel `I`, Purchase Order `K`, Debit Note `L`, Credit Note `M`.
- `Voucher` and `Voucher-2` preserve voucher number, date, voucher type, bank/cash account, account line, reference type, foreign debit/credit, currency, rate, base debit/credit, cheque/deposit/reference/file details.
- `Tables` has company/accounting setup ranges for AP, cash, bank, report columns, and tax deduction start/end.

## SQL Evidence

Key legacy accounting objects from `Data.sql`:

| Object | Meaning |
| --- | --- |
| `ACT_SD_COA_LEVEL3` | COA master: company, 9-digit code, description, nature, 5-digit summary code, type, flag, fixed asset fields, tax/registration fields, currency. |
| `ACT_SO_CUSTOMER` | Customer master as a party ledger: 9-digit customer code, 5-digit summary/control code, branch, currency, tax ids, credit limit, terms. |
| `ACT_SO_SUPPLIER` | Supplier master as a party ledger: 9-digit supplier code, 5-digit summary/control code, branch, currency, tax/category fields, credit limit, terms. |
| `ACT_SO_VTYPE` | Voucher type setup: code, name, document/posting type, renumber flag, sales tax flag, user/system flag, journal mappings. |
| `ACT_TM_VOUCHER` | Voucher header: company, branch, voucher number/date/type, bank code, cheque/deposit, status, adjusted date, insert/update user/time, totals, payee/print metadata. |
| `ACT_TD_VOUCHER` | Voucher detail: sequence, account code, debit/credit base, debit/credit foreign, currency, rate, adjusted date, file number, tax, cheque/description. |
| `ACT_TM_SI` / `ACT_TD_SI` | Sales invoice header/detail with customer, gross/discount/tax/receipt amounts, currency/rate, file and line tax data. |
| `ACT_TM_SR` / `ACT_TD_SR` | Sales receipt/return family with the same accounting shape. |
| `ACT_SM_COA_OPNBAL` / `ACT_SD_COA_OPNBAL` | Opening balance master/detail with financial year, branch, COA code, currency, debit/credit. |
| `ACT_SD_COA_BUDGET` | Budget by account, period/month, debit/credit. |
| `Tax` | Tax code, percent, type, description, category, section. |
| `Currency` / `EX_RATE` | Currency master and dated exchange rates. |
| `ACT_SD_FORMAT` / `ACT_SD_FORMAT_DTL` | Financial report formatting and range mapping by COA from/to. |
| `VUActBalance` / `spActBalance` | Balance derivation from posted voucher lines plus opening balances, with branch merge and adjusted-date options. |

The crucial SQL lesson: balances are derived from opening balances plus posted voucher lines. They are not trusted as casual mutable account totals.

## TRV.mde Recovery Evidence

Recovered artifacts:

- `artifacts/codex/trv-mde-extract-20260705/trv-mde-readable-strings.txt`
- `artifacts/codex/trv-mde-extract-20260705/trv-mde-accounting-strings.txt`
- `artifacts/codex/trv-mde-extract-20260705/trv-mde-accounting-summary.json`
- `artifacts/codex/trv-mde-extract-20260705/trv-mde-accounting-contexts.md`
- `artifacts/codex/trv-mde-access-export-20260705/access-export-summary.json`
- `artifacts/codex/trv-mde-access-export-20260705/queries/*.sql`

Extraction counts:

| Metric | Count |
| --- | ---: |
| ASCII strings | 154,876 |
| UTF-16LE strings | 129,843 |
| Unique strings | 52,518 |
| Accounting-related strings | 1,377 |
| Discovered accounting names | 130 |
| DAO object names found | 987 |
| Saved query SQL definitions exported | 301 |

Access automation result:

- DAO could enumerate `279` forms, `365` reports, `4` macros, `38` modules, and `301` saved queries.
- Saved query SQL exported successfully.
- `SaveAsText` for forms/reports/macros/modules failed from the MDE, so compiled source bodies are still not recovered here. This is expected for MDE-era compiled objects.
- Macro/module names are still useful: `AUTOEXEC`, `Posting`, `Qry_Builder`, `Get_Set_Pro`, `Sec_Pro`, `Tmp_Pro`, `Gr_Pro`, `Dsp_Pro`, `Conv_Pro`, and printing/import modules were all enumerated.

Important recovered Access objects and procedure/event names:

| Recovered name | What it indicates |
| --- | --- |
| `Form_COA`, `Report_COA`, `Form_ACCOUNTS` | COA/account maintenance and reporting screens exist in the MDE. |
| `Form_GL`, `Report_RGL_F1`, `Report_RGL_F2`, `Report_RGL_F3`, `Report_RGL_F4` | General ledger and report variants. |
| `Form_Voucher_List`, `Form_Voucher_Copy`, `Form_Voucher_Contra`, `Form_Superwise_Voucher` | Voucher search, copy, contra filtering, and supervise/unsupervise workflows. |
| `Form_OP_BAL`, `Form_Staff_Opening`, `Form_City_Opening`, `Visa_Rates_Opening` | Opening-balance/opening-setup screens beyond plain GL balances. |
| `Form_VT`, `ACT_SO_VTYPE` | Voucher type setup is first-class. |
| `UPD_COA3`, `UPD_COA31`, `COA_CRS`, `COA1_CRS` | COA update/refresh routines and cursors. |
| `GET_COA3_BAL`, `GET_COA3_BAL_` | COA balance lookup routines. |
| `MVH_COA3_CODE_AfterUpdate`, `LC_MVH_COA3_CODE_Enter`, `OCT_COA3_CODE_DblClick` | Form events around account-code entry and lookup. |
| `btnGenerateVoucher_Click` | Voucher generation is driven by a UI command routine. |
| `TransferSpreadsheet`, `RunSQL`, `OpenForm`, `OpenReport`, `RecordSource`, `RowSource` | The MDE contains classic Access macro/VBA actions and dynamic SQL wiring. |

Important DAO object inventory:

| Object type | Recovered accounting names |
| --- | --- |
| Forms | `ACCOUNTS`, `ACTAR01`, `ACTAR06`, `ACTAR07`, `Balsh`, `Balsh_Map`, `City_Opening`, `COA`, `Credit_Policy`, `Creditor_List`, `Currency`, `CVOU`, `DVOU`, `Ex_Rate`, `GL`, `MVOU`, `OP_BAL`, `Opn_Bal_Master`, `Opn_Inv`, `Rec_Vou`, `Staff_Opening`, `SUP_VOUCHER`, `Superwise_Voucher`, `Vou_SE`, `Voucher_Contra`, `Voucher_Copy`, `Voucher_List`, `VT`, `VTR`. |
| Reports | `ACTAR01`, `ACTAR06`, `ACTAR07`, `BALSH`, `Bank_Rec`, `CB_6COL`, `CB_BK`, `COA`, `dvou`, `DVOU_FC`, `mvou`, `MVOU_FC`, `RCVOU`, `Receipt`, `RGL_F1`, `RGL_F2`, `RGL_F3`, `RGL_F4`. |
| Macros | `AUTOEXEC`, `Mk_prp`, `RPT_PRN_ST`, `TMP`. |
| Modules | `Posting`, `Qry_Builder`, `Get_Set_Pro`, `Sec_Pro`, `Tmp_Pro`, `Gr_Pro`, `Dsp_Pro`, `Conv_Pro`, `PRN_SETTING`, `FileSys`, `mod_EmailSMS`, `modPNRImport`, PNR/import reader classes. |

Important recovered SQL/formula behavior:

- COA screen logic reads `ACCOUNTS` and `ACT_SD_COA_LEVEL3`, including `ACC_CODE`, `ACC_NAME`, `ACC_TYPE`, `ACC_LINK`, `ACC_CHK`, `ACC_MASK`, `CUR`, `COA3_CODE`, `COA3_DESC`, `COA3_TYPE`, and `COA3_SUM_CODE`.
- The exported `COA.sql` query filters account browsing by `FORMS!COA!FA`, `FORMS!COA!TA`, and `FORMS!COA!OPG`, then orders by account code. The UI range/type filter is part of the COA workflow, not just a report convenience.
- Subsidiary/posting account filters appear repeatedly, for example `ACC_TYPE = 'S'`, `ACC_TYPE IN ('S','D')`, and ranges such as `CR_S` to `CR_E`.
- COA maintenance includes lock/unlock and delete checks: recovered strings include `UPDATE ACCOUNTS SET ACC_CHK=`, `You Can't Unlock this Account because its top level is locked!`, `DELETE FROM ACCOUNTS WHERE ACC_CODE =`, and `Accounts code does not exist in Account Table`.
- `ACCOUNTS Without Matching DVOU.sql` finds accounts with no voucher detail rows, which is useful for safe inactive/archive/delete decisions.
- Voucher reporting reads `MVOU`/`DVOU` plus `ACCOUNTS`, with fields for voucher number/date, cheque number, remarks, bank account, debit/credit foreign amounts, currency, exchange rate, deposit slip, and adjustment date.
- Supervision workflow is explicit: recovered strings include `Voucher has been supervised successfully`, `Voucher has been un-supervised successfully`, `LC_Unsupervise_Vouchers`, `ATH_FLAG`, and report filters for supervised/unsupervised vouchers.
- `Voucher_List.sql` only includes branch-scoped user vouchers, excludes debit-note/invoice/credit-note categories, filters `ATH_FLAG='Y'`, and excludes tour vouchers.
- Trial balance/balance reports use temp/report tables such as `ACT_RO_BALANCE`, `ACT_RO_LEDGER`, and `ACT_RO_FNCL_RPT`, deleting prior user/company rows before regenerating report data.
- Report formulas sum opening/activity/closing columns from `ACT_RO_BALANCE` with `RPT_TYPE IN ('D','S')` and labels such as `Opening Balance As On`, `Closing Balance As Of`, `Trial Balance`, `Balance Sheet`, and `Balance Sheet Auto`.
- `ACT_RO_LEDGER` is populated through an `INSERT INTO ACT_RO_LEDGER (...)` flow with fields for company, user, COA code, voucher number, invoice/reference number, date, remark, and amount.
- `GET_BR()`, `GETUS()`, and `GET_GLOBAL("C")` appear throughout, confirming branch/company/user scoped reporting and filtering.
- Legacy voucher lists support both voucher date and adjustment date criteria, including labels such as `As per Adjustment Date`, `As per Voucher Date`, and `Adjustment Date for Vouchers`.
- `CB_BK.sql` and `CB_BK_V.sql` confirm cash/bank book reports can switch between voucher date and adjustment date with `IIf(GETCA()=True,[ADT],[VDT])`, use account-code ranges via `GETFA()`/`GETTA()`, and include only supervised vouchers.
- `QREC.sql` confirms receipt/payment voucher detail has foreign amount and currency fields: `CUR`, `DAMT_FC`, `CAMT_FC`, and `CUR_RATE`.
- Cash/bank selection is setup-driven using CP fields such as `CASH_M`, `BANK_M`, `cb2`, `cb3`, `cb4`, and `cb5`.
- Travel-domain accounting links are strong: airline GL code, client account, supplier/creditor, staff/SPO commission, city tax, visa package/account, debit note, invoice, refund, and receipt/payment vouchers all connect back to COA/account codes.

The MDE recovery strengthens the target design in three ways:

1. SafarSuite should keep voucher supervision/approval as a real state, not just posted/unposted.
2. Reporting should support user-scoped regeneration tables or deterministic query snapshots for trial balance, ledger, balance sheet, and P&L.
3. COA polish should include UI-grade account lookup events, lock inheritance, setup-range validation, and account-code conflict checks before import or posting.

## TRV.mdb Source Export Evidence

The folder also contains `TRV.mdb`, which is the stronger source. Access automation exported:

| Object type | Exported | Failed |
| --- | ---: | ---: |
| Forms | 279 | 0 |
| Reports | 365 | 1 |
| Macros | 4 | 0 |
| Modules | 38 | 0 |
| Query SQL | 301 | 0 |
| Query text | 301 | 0 |

Important exported source artifacts:

- `artifacts/codex/trv-mdb-access-export-20260705/modules/Posting.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/modules/Ini_Pro.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/modules/Get_Set_Pro.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/modules/Sec_Pro.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/modules/Qry_Builder.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/COA.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/OP_BAL.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/Superwise_Voucher.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/Voucher_Copy.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/Voucher_Contra.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/macros/AUTOEXEC.txt`

Important source-level findings:

- `AUTOEXEC` calls `Sys_INI()`, so startup is centralized in initialization code rather than scattered per-form startup.
- `Posting.CP_INI()` loads control/setup accounts from `CP`: KB, SP, COM, SP2, COM2, SPO, city-tax categories `CTXA` to `CTXE`, WHT, visa, difference-of-fare, self-credit-card, cash/bank/report ranges, balance-sheet/P&L ranges, and closing-list ranges.
- `Posting.TAX_INI()` reads `Taxes_Setup`, builds a dynamic tax array, and creates dynamic aggregate query fragments. Tax behavior is data-driven.
- `Posting.POS()` and `Posting.XPOS()` convert sales invoices (`SI`) and credit notes/refunds (`CN`) into `MVOU`/`DVOU` vouchers inside a DAO transaction.
- Posting aggregates operational ticket fields before voucher creation: fare, SP/SP2, KB, commission/commission2, SPO, WHT, visa, visa payable to agent/airline, difference of fare, city taxes A-E, other tax, airline GL code, staff code, service charge, client code, adjustment date, branch code, currency, and currency rate.
- Sales invoice and credit-note postings reverse debit/credit direction by selecting `DAMT/CAMT` and `DAMT_FC/CAMT_FC` pairs based on the document type.
- Voucher headers created by posting include `Br_Code`, `Branch_Code`, `VNO`, `VDT`, `VC`, financial year/period marker `FN`, work-year `WY`, user, `ATH_FLAG`, and `CONTRA_FLAG`.
- `ADV()` is the reusable detail-line writer. It inserts `DVOU` lines with branch, voucher number, account code, adjustment date, remarks, reference, base debit/credit, foreign debit/credit, currency, rate, file/reference number, and document type.
- `ADV()` flips debit/credit when the amount is negative, rounds base amounts as `amount * currency rate`, and stores the original amount in the foreign-currency field.
- Posting calls `INV_spInsertInvDetail_Base` after voucher header creation, so SafarSuite should model source-document posting as a workflow with explicit source-link side effects, not only a journal row insert.
- `OP_BAL.txt` validates opening balances so each row has exactly one side, defaults account currency from account/company setup, and warns when base-currency debit and credit totals are not equal.
- `Superwise_Voucher.txt` toggles `ATH_FLAG` for voucher, sales invoice, sales refund, debit note, and booking ranges, with privilege checks for supervise and unsupervise actions.
- `Voucher_Copy.txt` copies voucher headers/details and regenerates voucher numbers from voucher type/date/sequence rules.
- `Voucher_Contra.txt` sets `CONTRA_FLAG` for selected voucher pairs so reports can hide contra vouchers.
- `Sec_Pro.txt` centralizes role/action permissions using `D_ROLES` and checks execute/insert/update/query/delete privileges before opening forms/reports or changing voucher supervision.
- `Get_Set_Pro.txt` holds process-wide report/filter state for from/to voucher, from/to date, voucher code, account range, branch, adjustment-date mode, and posted/supervised mode.
- `Ini_Pro.txt` contains the global setup contract. The commented `GET_GLOBAL` cases show the legacy role map clearly: AR/AP/cash/bank/fixed asset ranges, retained earnings, invoice/voucher numbering, report headers/footers, debit-note commission/discount accounts, and company/branch metadata.
- `Qry_Builder.txt` builds tax/accounting query fragments from setup rows and extracts SQL connection parameters from linked accounting tables such as `MVOU`.

SafarSuite implications from the MDB source:

1. Posting should be a transaction-level workflow that creates a header, balanced detail lines, source links, and derived operational side effects together.
2. Accounting setup should grow from simple account ranges into a typed control-account contract: revenue/commission/tax/WHT/visa/service charge/cash/bank/retained earnings/numbering/reporting roles.
3. Approval should be distinct from posting. Legacy `ATH_FLAG` means reports and workflows depend on supervised vs unsupervised state.
4. Adjustment date is not optional polish; it is a core reporting axis used by cash/bank book, receipt/payment, invoice/refund, and balance reports.
5. Foreign-currency lines need both base and foreign amounts on every voucher detail.
6. Voucher copy, contra hiding, and source-document reposting are real accounting operations and should become explicit SafarSuite workflows with audit trails.

## SafarSuite Current Fit

Already strong:

- `LedgerAccount` stores code, name, type, normal balance, level, parent account, posting flag, status, created date.
- `AccountCodeRange` stores company, role, display name, prefix, range start/end, length, type, normal balance, posting default, parent code, active flag.
- `AccountingSetupDefaults` seeds `MAIN` with controlled ranges and role-based suggestions.
- `CreateLedgerAccountHandler` rejects non-numeric/out-of-range codes and enforces type, normal balance, posting flag, hierarchy level, and parent rules.
- `SuggestLedgerAccountCodeHandler` generates the next available code from setup-backed ranges.
- `GetLedgerAccountReconciliationHandler` flags range mismatch, posting/level mismatch, orphan subsidiaries, wrong parents, inactive ranges.
- `JournalEntry` enforces at least two lines, positive debit/credit totals, and balanced postings before `Posted`.
- `PostManualJournalEntryHandler` validates open periods, posting accounts, active accounts, and auto voucher references.
- `JournalVoucherNumberService` supports yearly sequences and configurable prefixes by journal source type.
- Opening balance preview/posting exists, including text import with delimiter/header detection.
- Accounting periods, close/reopen, close artifacts, retained earnings/income summary/rounding controls exist.
- Trial balance, P&L, balance sheet, ledger activity, journal register, source-document drill-in, and nested COA UI exist.
- The accounting smoke runner verifies controlled setup, vouchers, corrections, reports, and period close behavior.

Main gaps against the legacy/masterpiece target:

| Gap | Why it matters |
| --- | --- |
| Single-company `MAIN` only | Correct for now, but branch/company evidence is everywhere in legacy accounting. |
| No branch dimension on journal lines/reports | Legacy vouchers and balances are branch-aware. |
| No adjusted date | Legacy `ADT`/`MVH_ADJ_DATE` supports adjusted-date reporting. |
| Single journal currency only | Legacy detail rows store base and foreign debit/credit plus currency/rate. |
| No explicit supervised/approved accounting state | Legacy `ATH_FLAG` controls which vouchers/documents are visible to reports and workflows. |
| Voucher type is not a full setup aggregate | Current numbering maps source types; legacy has voucher type codes, behavior, tax flags, renumber flags, system/user rules. |
| No generated posting workflow parity | Legacy `Posting.POS`/`XPOS` generate voucher headers and detail lines from invoices/refunds inside a transaction. |
| No voucher copy/contra workflow | Legacy has source-level voucher copy and contra-hide operations. |
| Reports do not yet roll up by `H/T/M/D/C/S` and report-format tables | Current reports are accurate by posting lines but not full classic statement layout. |
| Account ranges are role setup, not full company GL setup | Legacy setup includes AR/AP/cash/bank/tax/WHT/commission/visa/service charge/retained earnings/report roles. |
| No formal dimensions model | Department/cost center/project/file/branch should be dimensions, not new account ranges. |
| Source document link is reference-parsing based | Safer design is an explicit journal-source-link table with document kind/id/reference. |
| Opening balance import is text/manual only | Need workbook/SQL import dry-run with branch/currency/base/foreign parity. |
| Reconciliation repair is dry-run only | Good safety posture, but guided approved repair actions are still needed. |
| Party-ledger breadth is client-focused | Supplier/AP, employee/advance/loan, bank, and agent party-ledger flows remain future. |
| Tax is basic | Legacy has tax code/category/section and multiple tax amount columns. |
| Budgets not modeled | `ACT_SD_COA_BUDGET` evidence exists. |

## Recommended Target Model

### Core setup

- `ledger_accounts`
  - Keep current fields.
  - Add later: company code, default currency code, reporting category, cash flow category, valid from/to, lock status, external legacy code, audit metadata.
- `account_code_ranges`
  - Keep current fields.
  - Add `level`, `sequence`, `reserved`, `allow_manual_code`, overlap/gap validation, and effective dates.
- `accounting_control_settings`
  - Extend from close controls into full GL setup roles: AR control, AP control, cash range, bank range, tax payable, withholding tax, discount, refund clearing, currency gain/loss, rounding, retained earnings, income summary.
- `voucher_types`
  - Code, name, category (`J/P/R/A/B/C/D/E/F/G/H/I/K/L/M`), source type mapping, prefix, renumber flag, sales-tax flag, system/user flag, single/multi mode, active flag, valid dates.
- `financial_dimensions`
  - Branch, department, cost center, project/file, module/product, sales person, region, legal entity.
  - Values can be custom or entity-backed.
- `journal_entry_source_links`
  - Journal id, document kind, document id, source reference, module, action, client/supplier id, immutable snapshot.

### Journal header

Add over time:

- Company/legal entity
- Branch/default dimension set
- Voucher type id/code
- Entry date and adjusted date
- Base currency
- Source document link
- Bank/check/deposit/reference metadata where relevant
- Posted/voided/reversed/approved metadata
- Import batch id and legacy references

### Journal line

Add over time:

- Line number
- Dimension set id
- Base debit/base credit
- Foreign debit/foreign credit
- Line currency and exchange rate
- Tax code/tax percent/tax amount
- File/reference number
- Party/subledger id when applicable

### Reporting

- Use journal lines for truth.
- Use COA hierarchy and report-format mappings for presentation.
- Trial balance should support:
  - as-of date
  - from/to date
  - branch merge/detail
  - adjusted date vs voucher date
  - base and foreign currency
  - posting-only or include summary rows
- Balance sheet/P&L should support:
  - `H/T/M/D/C/S` rollup
  - report line/range layout
  - current period and comparative period
  - source drill-down to account activity and voucher lines
- Cash flow should be mapping-driven, not inferred from account names.

## Recommended Polish Roadmap

### Phase 1 - COA import and setup polish

1. Build `PreviewChartOfAccountsImport` from workbook/CSV text:
   - Parse account code, name, type, link/parent, currency, active/update flags.
   - Validate code length, range, level, posting flag, parent existence, duplicate code, currency.
   - Produce insert/update/reject actions without writing.
2. Add `PostChartOfAccountsImport` only after the dry-run has fixture tests.
3. Add range overlap/gap validation and show coverage warnings in Accounting Setup.
4. Add an account-mapping screen for AR/AP/cash/bank/tax/revenue/discount/refund/close roles.
5. Replace raw parent display in the account editor with a Master/Control picker filtered by valid parent level.

### Phase 2 - Voucher parity

1. Create `VoucherType` aggregate and UI based on `V-Journal` evidence.
2. Let manual voucher entry choose voucher type code, not only journal source type.
3. Persist branch, adjusted date, cheque/deposit/reference/file fields.
4. Add line-level base/foreign debit/credit, currency, and rate.
5. Add explicit journal-source-link persistence for every generated accounting entry.

### Phase 3 - Subledgers and tax

1. Create `PartyLedgerAccount` linking clients/suppliers/employees/agents to control/subsidiary accounts.
2. Add supplier/AP and debit-note foundations.
3. Add tax code/category/section setup and journal tax metadata.
4. Add budget setup/import and budget-vs-actual reports.

### Phase 4 - Reporting quality

1. Implement report-format/range mappings inspired by `ACT_SD_FORMAT` and `ACT_SD_FORMAT_DTL`.
2. Roll reports up through `H/T/M/D/C/S`, with expand/collapse and drill-down.
3. Add branch merge/detail, adjusted-date reporting, comparative periods, and base/foreign views.
4. Add fixture-backed assertions from `GL_Working.xlsx`/`Data.sql` samples.

## Immediate Acceptance Standard

The next slice should not be "more accounting screens." It should make the COA spine trustworthy:

- import dry-run proves the legacy chart can be understood before writing anything;
- reports can explain totals through hierarchy and drill-down;
- voucher setup knows real voucher types and source behavior;
- branch/date/currency choices are modeled deliberately;
- every generated journal can point back to a source document without relying only on a string parse.

That is the difference between a working GL MVP and the accounting backbone SafarSuite deserves.
