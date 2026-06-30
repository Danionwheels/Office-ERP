# Survey Object Clone Register

Date started: 2026-06-30

Use this register to decide what happens to every important Survey/FAS object.

Decision values:

- `Clone`
- `Modernize`
- `Replace`
- `Archive`
- `Ignore`
- `New`

Status values:

- `Unreviewed`
- `Mapped`
- `Designed`
- `Implemented`
- `Verified`
- `Deferred`

## Summary Counts

| Source | Count | Status |
| --- | ---: | --- |
| Access tables | 111 | Inventory captured |
| SQL script tables | 96 | Inventory sampled; accounting/billing column sweep completed |
| Queries | 122 | Exported |
| Forms | 178 | Exported |
| Reports | 183 | Exported |
| Modules | 6 | Exported |
| Macros/scripts | 3 | Exported |

## Table Mapping

| Legacy Object | Source | Domain | Modern Destination | Decision | Status | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `MYK_XO_COMPANY` | `MYKDATA7.mdb` / `Survey.sql` | Company setup | `office.company_profile`, `accounting.account_ranges` | Modernize | Unreviewed | `survey-software-sweep.md` | Preserve business setup, not old schema shape |
| `MYK_XO_USERS` | `MYKDATA7.mdb` / `Survey.sql` | Users | `identity.users` | Replace | Unreviewed | `MYKLOGIN`, `MYK_GLOBAL` | Do not clone old password model |
| `MYK_XM_SECURITY` | `MYKDATA7.mdb` / `Survey.sql` | Security header | `identity.roles`, `identity.permission_profiles` | Modernize | Unreviewed | `MYK_GLOBAL`, `Sec_Pro` | Use as role/permission evidence |
| `MYK_XD_SECURITY` | `MYKDATA7.mdb` / `Survey.sql` | Security details | `identity.role_permissions` | Modernize | Unreviewed | `MYK_GLOBAL`, `Sec_Pro` | Map program codes to new permissions |
| `ACT_SD_COA_LEVEL3` | `ACTDATA7.mdb` / `Survey.sql` | Chart of accounts | `accounting.ledger_accounts` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, queries `ACTAL*`, `ACTAR*` | Preserve account codes, account nature, summary code, and hierarchy |
| `ACT_TM_VOUCHER` | `ACTDATA7.mdb` / `Survey.sql` | Voucher header | `accounting.journal_entries` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, queries `ACTAL01`, `ACTAR*` | Preserve posting meaning |
| `ACT_TD_VOUCHER` | `ACTDATA7.mdb` / `Survey.sql` | Voucher lines | `accounting.journal_lines` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, queries `ACTAL01A`, `ControlTotal` | Must become balanced debit/credit decimal lines |
| `ACT_TM_SI` | `ACTDATA7.mdb` / `Survey.sql` | Sales invoice header | `billing.invoices` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, queries `ACTAL02`, `ACTAL08` | Shared invoice model for provider and survey invoices |
| `ACT_TD_SI` | `ACTDATA7.mdb` / `Survey.sql` | Sales invoice lines | `billing.invoice_lines` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, queries `ACTAL02A`, `ACTAR23` | Preserve tax/discount behavior through charge rules and tax rules |
| `ACT_TM_SR` | `ACTDATA7.mdb` / `Survey.sql` | Sales receipt header | `payments.receipts` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, SQL/table inventory | Receipt/payment header |
| `ACT_TD_SR` | `ACTDATA7.mdb` / `Survey.sql` | Sales receipt detail | `payments.receipt_allocations` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, SQL/table inventory | Allocation detail evidence |
| `ACT_SM_ASSET` | `ACTDATA7.mdb` / `Survey.sql` | Asset header | `assets.assets` | Modernize | Unreviewed | SQL/table inventory | Office asset management candidate |
| `ACT_SD_ASSET` | `ACTDATA7.mdb` / `Survey.sql` | Asset detail | `assets.asset_events` | Modernize | Unreviewed | SQL/table inventory | Confirm depreciation/transaction use |
| `ACT_SO_CUSTOMER` | `ACTDATA7.mdb` / `Survey.sql` | Customers | `clients.clients`, `accounting.party_ledger_accounts` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, queries `ACTAL02`, `ACTAL13` | Legacy customers double as ledger accounts; provider clients need extra contract fields |
| `Docket` | `VALDATA7.mdb` / `Survey.sql` | Survey jobs | `survey.jobs` or archive | Modernize | Unreviewed | queries `Docket_Crosstab`, `CL_SR_INV` | Decide if operational survey workflow still needed |
| `Docket_A` | `VALDATA7.mdb` / `Survey.sql` | Survey invoice preparation | `survey.invoice_preparation_lines` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, report/query evidence | Preparation rows only; final invoice goes through Billing/Accounting |
| `Docket_B` | `VALDATA7.mdb` / `Survey.sql` | Survey job detail | `survey.job_details` or archive | Modernize | Unreviewed | report/query evidence | Needs workflow review |
| `Docket_C` | `VALDATA7.mdb` / `Survey.sql` | Survey job detail | `survey.job_details` or archive | Modernize | Unreviewed | report/query evidence | Needs workflow review |
| `Docket_D` | `VALDATA7.mdb` / `Survey.sql` | Survey job detail | `survey.job_details` or archive | Modernize | Unreviewed | report/query evidence | Needs workflow review |
| `VAL_INV_M` | `VALDATA7.mdb` / `Survey.sql` | Valuation invoice header | `survey.valuation_invoices` linked to `billing.invoices` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, SQL/table inventory | Retain as survey batch workflow evidence |
| `VAL_INV_D` | `VALDATA7.mdb` / `Survey.sql` | Valuation invoice detail | `survey.valuation_invoice_lines` linked to `billing.invoice_lines` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, SQL/table inventory | Docket lines selected into valuation invoice |
| `Valuators` | `VALDATA7.mdb` / `Survey.sql` | Survey reference | `survey.valuators` or archive | Modernize | Unreviewed | SQL/table inventory | Only if survey workflow continues |
| `Supervisers` | `VALDATA7.mdb` / `Survey.sql` | Survey reference | `survey.supervisors` or archive | Modernize | Unreviewed | SQL/table inventory | Correct spelling in new schema |
| `Ser_Type` | `VALDATA7.mdb` / `Survey.sql` | Service setup | `survey.service_types` or `billing.charge_codes` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, queries `CL_SR_INV` | May become charge catalog evidence |
| `Ser_Item` | `VALDATA7.mdb` / `Survey.sql` | Service setup | `survey.service_items` or `billing.charge_codes` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, SQL/table inventory | Review relation to SafarSuite plans |
| `Payment_Type` | `VALDATA7.mdb` / `Survey.sql` | Payment setup | `payments.payment_methods` | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, SQL/table inventory | Add card/bank transfer/manual modes |

## Form Mapping

| Legacy Form | Domain | Modern Screen | Decision | Status | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Startup` | Startup/splash | Not needed or desktop splash | Replace | Mapped | `forms/Startup.txt` | Old splash opens `MYKLOGIN` |
| `MYKLOGIN` | Login | Modern login screen | Replace | Mapped | `forms/MYKLOGIN.txt` | Use modern auth, not old DLookup password |
| `MYKMS01` | Company setup | Company/accounting setup | Modernize | Unreviewed | `forms/MYKMS01.txt` | Contains account ranges and tax setup |
| `Docket` | Survey jobs | Survey/job screen or archive viewer | Modernize | Unreviewed | `forms/Docket.txt` | Needs workflow review |
| `INV_GEN` | Invoice generation | Valuation Invoice Batch Creator | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, exported form | Important for provider billing pattern, but build shared Billing/Accounting first |
| `INV_EDIT` | Invoice edit | Valuation Invoice Metadata Editor | Modernize | Mapped | `accounting-gl-foundation-sweep.md`, exported form | Compare with new billing needs |
| `CL_SR_AGING` | Aging report filter | Client receivable aging | Modernize | Unreviewed | exported form/queries | Important report candidate |
| `Grp_Rpts` | Group reports | Report catalog/filter screen | Replace | Unreviewed | exported form/reports | Modern report catalog preferred |

## Report Mapping

| Legacy Report | Domain | Modern Report | Decision | Status | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ACTAL01` | Voucher listing | Voucher register | Modernize | Unreviewed | query `ACTAL01.sql` | Candidate accounting parity report |
| `ACTAL02` | Sales invoice | Invoice print/view | Modernize | Unreviewed | query `ACTAL02.sql` | Candidate invoice parity report |
| `ACTAR05` | Voucher imbalance/control | Voucher validation report | Modernize | Unreviewed | query `ACTAL05A.sql` | Useful validation behavior |
| `CL_SR_AGING` | Client aging | Receivable aging | Modernize | Unreviewed | queries `CL_SR_INV`, `CL_SR_REC` | High-value provider report |
| `DocketQ` | Docket report | Survey job report or archive | Modernize | Unreviewed | exported report | Decide if survey workflow remains active |
| `Sur_Rpt` | Survey report | Survey report or archive | Modernize | Unreviewed | exported report | Needs business confirmation |
| `Inv_AV` | Valuation invoice/report | Valuation invoice/report | Modernize | Unreviewed | exported report | Needs workflow confirmation |
| `PBA_RPT_A` | Survey/account report | TBD | Unreviewed | Unreviewed | exported report | Needs classification |

## Module/Code Mapping

| Legacy Module | Purpose | Modern Destination | Decision | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| `MYK_GLOBAL` | Startup, globals, relinking, security helpers | Split across configuration, auth, data-source migration | Replace/Modernize | Mapped | Keep behavior evidence only |
| `Sec_Pro` | Security/menu helpers | IdentityAccess/permissions | Replace | Unreviewed | Program-code mapping may be useful |
| `clsDB_Config` | Encoded database config | App configuration/secrets | Replace | Mapped | Do not clone encoding as security |
| `FileSys` | File browsing/helpers | Desktop file picker/service | Replace | Unreviewed | Tauri/native file picker later |
| `mdoValidate` | Validation structs/helpers | Application validators | Modernize | Unreviewed | Review before discarding |
| `TMP_MODULE` | Temporary/misc code | TBD | Unreviewed | Unreviewed | Inspect before deciding |

## New Objects Required

| New Object | Domain | Why Needed | Status |
| --- | --- | --- | --- |
| Client contract | Provider control | Store custom prices and plan rules per SafarSuite client | Unreviewed |
| Subscription/renewal | Provider control | Track due dates, grace, paid-until, renewal state | Unreviewed |
| Plan/module catalog | Provider control | Define allowed SafarSuite modules and bundles | Unreviewed |
| Device allowance | Provider control | Track number of allowed devices/users/branches | Unreviewed |
| Portal invoice mirror | Cloud portal | Publish client-facing invoices | Unreviewed |
| Payment transaction | Payments | Track card/bank transfer/manual payments | Unreviewed |
| Charge code | Billing | Define dynamic billable items for each client | Mapped |
| Client charge rule | Billing/Contracts | Store client-specific dynamic charges, tax behavior, and effective dates | Mapped |
| Ledger account | Accounting | Preserve COA and support invoice/payment postings | Mapped |
| Journal entry | Accounting | Store balanced postings for invoices, receipts, adjustments, and opening balances | Mapped |
| Entitlement snapshot | Licensing | Sign what SafarSuite is allowed to do | Concept exists |
| Product-kernel command | Licensing | Push module/device/status changes to SafarSuite | Concept exists |
