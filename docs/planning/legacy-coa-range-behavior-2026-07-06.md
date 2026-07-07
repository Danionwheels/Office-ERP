# Legacy COA Range Behavior Sweep

Date: 2026-07-06

## Evidence Used

- Screenshot pasted in Codex: legacy "General Accounts Setup" range panel with Assets, Capital & Liab, BSheet, Profit & Loss, Revenue, Expense.
- `artifacts/codex/trv-mdb-access-export-20260705/forms/CP.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/COA.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/ACCOUNTS.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/ACC_F1.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/ACC_F1_D.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/OP_BAL.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/forms/Opn_Bal_Master.txt`
- `artifacts/codex/trv-mdb-access-export-20260705/reports/COA.txt`
- `artifacts/codex/trv-mde-access-export-20260705/queries/COA.sql`
- `artifacts/codex/trv-mdb-access-export-20260705/queries/ACC_F1.sql`
- `C:/Users/Daniyal/Downloads/Data.sql`
- `artifacts/codex/accounting-evidence.json`

## Core Findings

The legacy range panel is a company accounting setup surface, not just a report filter. In `CP.txt`, the visible range fields map to persisted controls:

- Assets: `ASS_S`, `ASS_E`
- Capital and liabilities: `C_L_S`, `C_L_E`
- Balance sheet: `BS_S`, `BS_E`
- Profit and loss: `P_L_S`, `P_L_E`
- Revenue: `REV_S`, `REV_E`
- Expense: `EXP_S`, `EXP_E`

Each start field is a combo box sourced from non-subsidiary accounts:

```text
SELECT ACCOUNTS.ACC_CODE, ACCOUNTS.ACC_NAME
FROM ACCOUNTS
WHERE ACCOUNTS.ACC_TYPE <> "S"
ORDER BY ACCOUNTS.ACC_CODE
```

The end fields are locked text fields. This means the legacy user selected a structural start account, while the ending boundary was controlled by setup/data rules.

`Data.sql` shows the SQL Server version of the same idea in `MYK_XO_COMPANY`, using prefixed fields such as `CP_AST_S/E`, `CP_CAP_S/E`, `CP_LBL_S/E`, `CP_PL_S/E`, `CP_EXP_S/E`, plus total/result accounts such as `CP_TOT_CAP`, `CP_TOT_CL`, `CP_TOT_AST`, `CP_NET_INC`, `CP_GRS_INC`, and `CP_GRS_SAL`.

## COA Listing Behavior

The legacy COA report query is simple and important:

```sql
SELECT ACCOUNTS.ACC_CODE, ACCOUNTS.ACC_NAME, ACCOUNTS.ACC_TYPE, ACCOUNTS.ACC_LINK, ACCOUNTS.ACC_CHK, ACCOUNTS.ACC_MASK
FROM ACCOUNTS
WHERE ACCOUNTS.ACC_CODE BETWEEN FORMS!COA!FA AND FORMS!COA!TA
  AND ACCOUNTS.ACC_TYPE Like FORMS!COA!OPG
ORDER BY ACCOUNTS.ACC_CODE;
```

The `COA` form has:

- `FA`: from account
- `TA`: to account
- `OPG`: account type filter

Changing `OPG` requeries the from/to account selectors to only show matching account types. The report then renders the selected range.

The `COA` report changes visual weight by type:

- Master: largest/boldest
- Control: medium/bold
- Subsidiary: small/plain

The workbook evidence expands the taxonomy to include Header and Total, and mentions Detail as an entry-level account in some data notes. The Access `ACCOUNTS` form value list includes Header, Master, Control, Subsidiary, and Total. We should keep our model flexible enough for `H`, `T`, `M`, `C`, `S`, and possibly `D`.

## Parent Tree Visibility Rule

The legacy nested COA maintenance screen is not the same as the printable COA report. The nested form `ACC_F1` is the important evidence for "which accounts appear as parent/master rows".

`ACC_F1` is sourced from this query:

```sql
SELECT ACCOUNTS.*
FROM ACCOUNTS AS ACCOUNTS_1
INNER JOIN ACCOUNTS
  ON ACCOUNTS_1.ACC_CODE = ACCOUNTS.ACC_LINK
 AND ACCOUNTS_1.Br_Code = ACCOUNTS.Br_Code
WHERE ACCOUNTS.ACC_TYPE In ('M','C')
  AND ACCOUNTS.Br_Code = get_br()
  AND ACCOUNTS_1.ACC_TYPE = "T"
UNION
SELECT ACCOUNTS.*
FROM ACCOUNTS
WHERE ACCOUNTS.ACC_TYPE = 'M'
  AND ACCOUNTS.Br_Code = get_br();
```

That means the legacy parent/master surface is intentionally curated:

- A `Total (T)` row acts as the structural boundary.
- `Master (M)` and `Control (C)` accounts linked directly under a `Total (T)` account appear in the parent/master list.
- All `Master (M)` accounts also appear.
- The child subform `ACC_F1_D` is linked by `Br_Code;ACC_LINK` to the selected parent `Br_Code;ACC_CODE`.

The screenshot workflow also shows that accountants think in terms of selecting a visible structural row and entering children directly under it. For provider-office-erp, this should become a backend-owned rule, not just a frontend filter:

- Parent tree seed rows should be returned by an explicit COA parent-view rule.
- The rule should keep the user's "Total rows become the visible accounting boundary" mental model.
- Children/grandchildren should be displayed under the selected row by `ParentAccountId` first and code/range inference only as a fallback.
- New account creation should only add a child under the selected parent. There should be no sibling shortcut; to create a sibling, select the parent and add another child.
- Posting rows for opening balances and journals must be limited to true posting levels: `Subsidiary (S)` and `Detail (D)` where present.

## Account Record Behavior

Legacy `ACCOUNTS` rows are centered around these fields:

- `Br_Code`
- `ACC_CODE`
- `ACC_NAME`
- `ACC_TYPE`
- `ACC_LINK`
- `ACC_CHK`
- `ACC_MASK`
- `UPD_CHK`
- `NNR`
- `CUR`

Important rules from `ACCOUNTS.txt`:

- `ACC_LINK` is the parent account and its selector excludes subsidiary accounts.
- Type editing is locked once the account already has children.
- Delete is blocked when another account links to this account.
- If a new row has no code, the system sets `ACC_CODE = DMax("ACC_CODE", "ACCOUNTS", "ACC_LINK='<parent>'") + 1`, falling back to the parent code.
- `ACC_CHK` and `NNR` cannot be weaker than the parent. If parent lock/NNR is enabled, children cannot disable it.
- Changing lock/NNR on a structural account propagates through descendants.

This supports the SafarSuite legacy mental model: select a parent, add a child inside the hierarchy, let the code suggest from the parent/range context.

## Range Usage Beyond COA

The range setup drives many modules:

- Balance sheet reports use `BS_S` and `BS_E`.
- Profit/loss calculations use `P_L_S` and `P_L_E`.
- Cash/bank selectors use `CB_S` and `CB_E`.
- Expenses use `EXP_S` and `EXP_E`.
- Client/creditor/receivable screens use `CR_S`, `CR_E`, `REC_S`, and `REC_E`.
- Posting initialization loads range globals such as `CB_S`, `BS_S`, `P_L_S`, `C_L_S`.

There is also a separate statement-format range system in `Data.sql`:

- `ACT_SD_FORMAT`
- `ACT_SD_FORMAT_TOT`
- `ACT_SD_FORMAT_DTL`

`ACT_SD_FORMAT_DTL` stores account bands with `DFD_COA3_FM` and `DFD_COA3_TO`. This should remain separate from company default ranges. Company ranges describe broad accounting boundaries; statement format ranges describe report layouts and subtotal sections.

## Opening Balance Formats

There are two legacy opening-balance concepts that should not be mixed up.

Format 1: Access table-style opening balances.

- Form: `OP_BAL`
- Record source: `OP_BAL`
- Account selector: `ACCOUNTS.ACC_TYPE IN ('S','D')`
- Branch/user filtering excludes client accounts outside the active branch.
- Currency defaults from company currency, then from the selected account currency.
- Validation blocks blank/zero lines and blocks debit plus credit on the same line.
- On close, the form warns when base-currency debit and credit totals do not match.
- Reports such as `ACTAR01` read `OP_BAL` grouped by `ACC_CODE`, branch, and currency, then combine it with voucher activity.
- `OP_BAL` can open `Opn_Inv`; returning from opening invoices recalculates the selected client's opening amount from invoice gross minus adjustments.

Format 2: Financial-year opening profile plus detail balances.

- Form: `Opn_Bal_Master`
- Record source: `OP_BAL_MASTER`
- SQL master table equivalent: `ACT_SM_COA_OPNBAL`
- SQL detail table equivalent: `ACT_SD_COA_OPNBAL`
- Master fields include company, from year, to year, status, transaction allowed, and profit/loss carry-forward account.
- Detail fields include company, branch, year range, account code, department/system concern, currency, debit amount, and credit amount.
- `ACT_SD_COA_OPNBAL` references the COA table and the opening-balance year master.
- `VUActBalance` unions `ACT_TD_VOUCHER` activity with `ACT_SD_COA_OPNBAL`, marking voucher rows as `T` and opening rows as `O`.
- Inventory/item openings also use the same financial-year master through `ACT_SD_DIT_OPNBAL` and `ACT_SD_DIT_OPNBAL_WAVG`. These are not GL account lines, but they are part of the same opening-year boundary.

Current provider-office-erp already has a clean journal approach:

- `PostOpeningBalanceImportHandler` creates a posted journal with `JournalSourceType.OpeningBalance`.
- `PreviewOpeningBalanceImportHandler` validates date, currency, balanced debit/credit totals, duplicate/invalid lines, active accounts, and posting accounts only.
- `GetTrialBalanceHandler` and `GetLedgerAccountActivityHandler` include opening-balance journal lines in balances and activity.

The missing piece is the legacy financial-year profile and data-alignment layer. We should keep the journal architecture, but add an opening-balance setup/profile that behaves like `OP_BAL_MASTER`/`ACT_SM_COA_OPNBAL` and owns:

- financial year from/to
- status: open/closed
- transaction allowed flag
- retained earnings/profit-loss carry-forward account
- company and branch scope
- posting/import lock once opening is finalized
- a posting-account-only line editor that feels like `OP_BAL`, but posts through our journal model

## Journal and Balance Linkage

`Data.sql` shows the newer SQL accounting schema:

- `ACT_SD_COA_LEVEL3` stores COA accounts with `COA3_CODE`, `COA3_DESC`, `COA3_SUM_CODE`, `COA3_TYPE`, `COA3_CUR_CODE`, fixed-asset fields, and note fields.
- `ACT_SD_COA_OPNBAL` stores opening balances by `DCO_COA3_CODE`.
- `ACT_TM_VOUCHER` is the voucher header.
- `ACT_TD_VOUCHER` is the voucher detail and references `DVH_COA3_CODE`.
- `VUActBalance` unions voucher detail rows and opening balances into a single balance source.
- Sales, purchase, debit/credit, customer, and supplier documents also reference `ACT_SD_COA_LEVEL3`.

This means COA type, parent, lock, range, and currency behavior must be protected because they flow into balances, statements, vouchers, customer/supplier documents, and operational posting.

## Design Implications For Provider Office ERP

The permanent right-side COA form should not remain as the main create surface. It makes the page feel congested and conflicts with the legacy tree workflow.

Recommended direction:

- Make the COA tree the main working area.
- Move range selection into the COA sub-page toolbar.
- Add a compact range selector with options like All, Assets, Capital & Liab, BSheet, Profit & Loss, Revenue, Expense, Cash/Bank, Receivable, Creditor, Fixed Assets, Custom statement range.
- Keep range limits visible in each tree row where relevant: start/end band, next code, child code limit, posting eligibility, lock/NNR inheritance.
- Keep account creation inline under the selected parent only.
- Treat the right side as an optional inspector or edit drawer, not as a permanent form.
- Keep range maintenance as a setup sub-view. It should edit the company accounting map and, separately, statement/report format ranges.
- Tree row badges should be accountant-readable: `Parent`, `Range`, `Next`, `Lock`, `NNR`, `Currency`, `Posting`, `Statement`.
- Add a backend parent-tree list mode so the COA "parent/master" visibility rule is stable across UI, import, reports, and tests.
- Keep the frontend parent tree as a view of the backend rule. It can include descendants for readability, but it should not decide which accounts qualify as parent/master rows by itself.

## Data Model Direction

Provider Office ERP should model these concepts explicitly:

- Account:
  - company/branch
  - code
  - name
  - type
  - parent code
  - currency
  - lock flag
  - NNR flag
  - mask/display code
  - status
  - fixed asset fields where needed
  - statement note fields where needed

- Company range profile:
  - range key
  - label
  - start account
  - end account
  - expected account types
  - purpose: financial statement, operational posting, special account setup

- Statement format range:
  - statement code
  - sequence
  - heading
  - from account
  - to account
  - nature/type
  - subtotal links

- Opening balance profile:
  - company
  - branch scope
  - financial year from/to
  - status
  - transaction allowed
  - retained earnings/profit-loss account
  - source reference policy
  - finalized/posted journal reference

- Opening balance line:
  - profile id
  - posting account
  - branch
  - currency
  - debit
  - credit
  - description
  - optional department/system concern

## Open Questions

- Should `Detail (D)` be kept as a first-class account type? The workbook evidence mentions it, but the Access `ACCOUNTS` form list does not include it.
- Should Header/Total rows be editable in the same tree as Master/Control/Subsidiary, or should they be protected setup rows?
- In the provider-office-erp parent tree, should the visible seed rows be strictly `Total (T)` as shown in the user's screenshot, or the exact legacy `ACC_F1` rule: `M/C` under `T`, plus all `M`?
- Should the range end value be manually editable in provider-office-erp, or derived from the selected range parent and structural rule?
- Should range conflicts be hard-blocking or shown as reconciliation warnings first?
- Should opening balances be stored only as posted journals, or should we also persist a profile/detail draft table before posting to preserve the `OP_BAL_MASTER` and `OP_BAL` workflow?

## Build Recommendation

Next build pass should reshape the COA page into:

1. Full-width tree first.
2. Toolbar range selector and type/view selector.
3. Row-level range chips.
4. Inline child creation only.
5. Optional inspector/drawer for edit/details.
6. Separate range setup sub-view for company ranges and statement format ranges.

Then add the accounting logic pass:

1. Backend parent-tree query mode for the COA nested maintenance view.
2. Seed/data update so `Total`, `Master`, `Control`, `Subsidiary`, and optional `Detail` accounts line up with the selected legacy rule.
3. Opening-balance profile model and API.
4. Opening-balance line editor that only accepts posting accounts.
5. Posting bridge from opening-balance profile/details into `JournalSourceType.OpeningBalance`.
