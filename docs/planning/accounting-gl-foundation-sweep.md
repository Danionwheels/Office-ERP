# Accounting And GL Foundation Sweep

Date: 2026-06-30

## Why This Sweep Happened

The invoice preparation detail grid was not the whole invoice creation module.

Legacy evidence shows three related but separate concerns:

| Legacy concern | Legacy evidence | Meaning |
| --- | --- | --- |
| Survey job invoice preparation | `Docket_A` | Detail rows prepared inside or near Survey Job Entry |
| Valuation invoice generation | `INV_GEN`, `VAL_INV_M`, `VAL_INV_D` | Batch creation of valuation invoices from selected dockets |
| Accounting and receivables | `ACT_TM_SI`, `ACT_TD_SI`, `ACT_TM_SR`, `ACT_TD_SR`, `ACT_TM_VOUCHER`, `ACT_TD_VOUCHER` | Sales invoices, receipts, vouchers, and ledger postings |

The corrected direction is to build a basic accounting and billing foundation before continuing deeper invoice UI work.

## Legacy Folder Sweep

Canonical source:

```text
E:/travel tour/survey
```

Files present during this sweep:

| File | Meaning |
| --- | --- |
| `Actappl7.mdb` | Access app shell with forms/reports/modules |
| `Survey.sql` | SQL Server-style schema export with table definitions |
| `ACTDATA7.mdb` | Accounting/customer/COA/sales/receipt/voucher data |
| `VALDATA7.mdb` | Survey/docket/valuation invoice data |
| `MYKDATA7.mdb` | Company/security/settings/signature data |
| `SCRDATA7.mdb` | Support/shadow data |
| `SP.rar` | Archive containing `ACTDATA7.mdb`, `MYKDATA7.mdb`, `SCRDATA7.mdb`, `VALDATA7.mdb` |
| `ANI_backup_2025_11_19_120002_1863486.bak` | SQL Server backup for later sample data or reconciliation |

Direct DAO inspection is not available in this environment. `Access.Application` is registered, but direct database-engine inspection of the app shell did not complete cleanly because of the legacy linked database state. Use the previous Access export or a fresh exported object dump when form event behavior is needed.

## Accounting Objects Found

| Legacy object | Purpose | Modern destination |
| --- | --- | --- |
| `ACT_SD_COA_LEVEL3` | Chart/account master | `Accounting.ChartOfAccounts` / `LedgerAccount` |
| `ACT_SO_CUSTOMER` | Customer master and customer ledger identity | `Clients.Client` plus `Accounting.PartyLedgerAccount` |
| `ACT_SO_ITEM` | Billing/service item with debit/credit/category setup | `Billing.ChargeCode` / `ChargeCatalogItem` |
| `ACT_SO_CATEGORY` | Sales/purchase category and tax/GL defaults | `Billing.ChargeCategory` / posting setup |
| `ACT_SO_VTYPE` | Voucher/document type | `Accounting.JournalDocumentType` |
| `ACT_TM_VOUCHER` | Voucher header | `Accounting.JournalEntry` |
| `ACT_TD_VOUCHER` | Voucher debit/credit lines | `Accounting.JournalLine` |
| `ACT_TM_SI` | Sales invoice header | `Billing.Invoice` |
| `ACT_TD_SI` | Sales invoice lines | `Billing.InvoiceLine` |
| `ACT_TM_SR` | Sales receipt header | `Payments.Receipt` |
| `ACT_TD_SR` | Sales receipt lines/details | `Payments.ReceiptAllocation` |
| `ACT_TD_OPN_INV` | Opening invoices | `Accounting.OpeningReceivable` |
| `ACT_DEBIT_NOTE`, `ACT_DEBIT_NOTE_A` | Debit note / archived debit note | `Billing.DebitNote` or later Accounting document |
| `Tax` | Tax rates/sections/categories | `Accounting.TaxCode` / `Billing.TaxRule` |
| `Inv_GL_Setup` | Valuation invoice GL and sales tax mapping | `Accounting.InvoicePostingProfile` |
| `Inv_Desc` | Invoice description setup | `Billing.InvoiceDescriptionTemplate` |
| `VAL_INV_M` | Valuation invoice header | `SurveyValuation.ValuationInvoice` linked to `Billing.Invoice` |
| `VAL_INV_D` | Valuation invoice docket lines | `SurveyValuation.ValuationInvoiceLine` |
| `Docket_A` | Prepared invoice rows before valuation invoice creation | `SurveyValuation.SurveyJobInvoiceLine` as a preparation line |

## Important Legacy Details

`ACT_SD_COA_LEVEL3` contains account code, description, nature, summary code, account type, and flag fields. This is the minimum chart of accounts evidence.

`ACT_TD_VOUCHER` has `DVH_COA3_CODE`, `DVH_DBT_AMT`, and `DVH_CRD_AMT`. The modern ledger must enforce balanced journal entries.

`ACT_TM_SI` has customer, invoice date, gross amount, discount, tax amounts, receipt amount, status, and remarks.

`ACT_TD_SI` has item, quantity, rate, discounts, tax percentages, and line amount.

`ACT_TM_SR` and `ACT_TD_SR` mirror the sales document shape for receipts.

`Inv_GL_Setup` maps invoice setup codes to `GL_Code`, `Sales_Tax_GL`, `Sales_Tax_PC`, service code, category, description, and active flag. This is the clearest bridge from survey invoice preparation to accounting.

`VAL_INV_M` and `VAL_INV_D` prove that valuation invoice generation is a separate workflow from `Docket_A`.

## Corrected Modern Model Direction

Keep the module boundaries clear:

| Module | Owns |
| --- | --- |
| `Accounting` | chart of accounts, ledger accounts, journal entries, journal lines, posting profiles, trial balance |
| `Billing` | invoices, invoice lines, charge catalog, dynamic client charge rules, invoice statuses |
| `Payments` | receipts, payment decisions, receipt allocations, reversals |
| `Clients` | SafarSuite customer/client master data |
| `Contracts` | client contract, modules, devices, branches, commercial terms |
| `SurveyValuation` | survey jobs, prepared survey invoice rows, valuation invoice batch workflow |

Billing should create business invoices. Accounting should receive balanced posting requests from Billing and Payments. SurveyValuation should not own a separate accounting engine.

## Minimum GL Flow For SafarSuite Control Desk

The first useful accounting flow should be:

```text
client contract and dynamic charges
  -> invoice draft
  -> invoice issue/finalize
  -> journal posting
       debit accounts receivable
       credit revenue
       credit tax payable if tax exists
  -> payment receipt
  -> payment posting
       debit cash/bank
       credit accounts receivable
  -> receivable balance and ledger reports
```

This supports SafarSuite client billing and renewals. Survey/FAS valuation invoice work is now paused and should not drive near-term design.

## Dynamic Charge Model

The current `ContractPricing` model only supports a recurring amount. The corrected billing model needs dynamic charge rules:

| Concept | Purpose |
| --- | --- |
| `ChargeCode` | Named billable item such as base subscription, extra branch, extra device, module add-on, support fee, survey valuation fee |
| `ClientChargeRule` | Client-specific price, tax rule, billing cycle, and effective dates |
| `InvoiceDraft` | Generated invoice before issue/posting |
| `InvoicePostingProfile` | Revenue, receivable, tax, and discount account mappings |
| `JournalEntry` | Balanced accounting document created when invoice or payment is finalized |

## Next Implementation Slice

Pause deeper `SurveyJobInvoiceLines` work for now.

Build the accounting and billing foundation in this order:

1. Add an `Accounting` domain module with `LedgerAccount`, `JournalEntry`, `JournalLine`, and account type/nature enums. Done.
2. Extend Billing with `ChargeCode` and `ClientChargeRule` so different clients can have different charges. Done.
3. Add application use cases for creating charge codes and client charge rules.
4. Add invoice draft generation from client charge rules.
5. Add invoice finalization that creates a balanced journal entry. Done for the basic debit receivable / credit revenue path.
6. Add receipt/payment allocation that updates invoice balance and posts a receipt journal entry. Done for immediately approved payment methods.
7. Add client accounting profile and cloud outbox so invoices can post to GL and publish to SafarSuite Control Cloud reliably.

## Implementation Progress

| Slice | Status | Notes |
| --- | --- | --- |
| Accounting domain primitives | Done | `LedgerAccount`, `LedgerAccountCode`, account type/status/normal balance enums |
| Journal domain primitives | Done | `JournalEntry`, `JournalLine`, source/status enums; posting requires balanced debit/credit totals |
| Billing charge catalog primitives | Done | `ChargeCode`, `ChargeCodeKey`, default price, revenue/tax account links |
| Dynamic client charge rules | Done | `ClientChargeRule` supports client/contract, charge code, price, quantity, billing cycle, billing day, and effective period |
| Invoice line charge link | Done | `InvoiceLine` can now optionally reference a `ChargeCodeId` |
| Application use cases | In Progress | Create use cases added for clients, ledger accounts, charge codes, client charge rules, invoice draft generation, invoice issue posting, invoice payment posting, journal listing, and ledger account activity |
| API endpoints | In Progress | Minimal create endpoints plus invoice draft, invoice issue, invoice payment, journal listing, and ledger account activity endpoints are wired |
| Temporary in-memory repositories | Done | Added for clients, ledger accounts, journal entries, invoices, payments, charge codes, and client charge rules |
| Invoice draft generation | Done | `GenerateInvoiceDraft` creates draft invoices from effective client charge rules |
| GL posting use cases | In Progress | Invoice issue and immediately approved invoice payment posting create posted balanced journal entries; tax, review, reversal, and reports remain pending |
| Accounting read models | Done | Journal entry listing and ledger account activity read models expose the accounting trail created by invoice and receipt posting |
| Survey invoice preparation bridge | Parked | Work exists, but SurveyValuation is no longer part of the active product path |
| Client accounting profile | Proposed | Link client to receivable/default currency/cloud identity and stop passing AR account manually |
| Cloud invoice outbox | Proposed | Enqueue invoice publish message when invoice is issued |

Current create endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/clients` | Create a SafarSuite client |
| `POST` | `/api/v1/accounting/ledger-accounts` | Create a ledger account |
| `POST` | `/api/v1/billing/charge-codes` | Create a billable charge code linked to revenue/tax accounts |
| `POST` | `/api/v1/billing/client-charge-rules` | Create a dynamic charge rule for a client |
| `POST` | `/api/v1/billing/invoice-drafts` | Generate a draft invoice from active client charge rules |
| `POST` | `/api/v1/billing/invoices/{invoiceId}/issue` | Issue a draft invoice and post the balanced GL journal entry |
| `POST` | `/api/v1/payments/invoice-payments` | Record an approved invoice payment, update invoice balance, and post the receipt journal |
| `GET` | `/api/v1/accounting/journal-entries` | List journal entries with optional date/source filters |
| `GET` | `/api/v1/accounting/ledger-accounts/{ledgerAccountId}/activity` | Show account activity with running balance |
| `PUT` | `/api/v1/survey-valuation/jobs/{surveyJobId}/invoice-lines` | Parked SurveyValuation endpoint |
| `POST` | `/api/v1/survey-valuation/jobs/{surveyJobId}/billing-draft` | Parked SurveyValuation endpoint |

## Current Verified Flow

The backend smoke test now proves this chain:

```text
create revenue ledger account
  -> create accounts receivable ledger account
  -> create cash/bank ledger account
  -> create client
  -> create charge code
  -> create client charge rule
  -> generate invoice draft from the active rule
  -> issue invoice
  -> create posted balanced journal entry
       debit accounts receivable
       credit revenue
  -> record invoice payment
  -> create posted balanced journal entry
       debit cash/bank
       credit accounts receivable
  -> invoice status becomes paid when balance reaches zero
  -> list journal entries
  -> view AR and cash ledger account activity
```

The smoke test verified an issued invoice with a posted journal entry, then a posted payment journal entry, where total debit equals total credit for both postings. The read-model smoke test verified two journal entries, one payment receipt journal entry through source filtering, AR ending balance `0.00`, and cash ending balance `251.00`.

## Transaction Policy

Accounting-sensitive actions must be atomic.

The application layer now exposes two unit-of-work shapes:

| Method | Use |
| --- | --- |
| `SaveChangesAsync` | Single-aggregate writes, such as creating one ledger account or one charge code |
| `ExecuteInTransactionAsync` | Multi-aggregate/table writes that must succeed or fail together |

Use `ExecuteInTransactionAsync` for:

- invoice issue/finalize plus journal entry creation
- receipt/payment approval plus invoice allocation plus journal entry creation
- payment reversal plus ledger reversal
- opening balance import across accounts and invoices

The current in-memory implementation only simulates this boundary. The PostgreSQL/EF implementation must use a real database transaction and rollback on failure.

Next accounting slice:

```text
client accounting profile
  -> link client to AR/default currency/cloud identity
  -> issue invoice without manually passing AR account each time
  -> enqueue cloud invoice publish message in the same transaction
```

SurveyValuation bridge work exists but is parked. Do not extend it while the active goal is SafarSuite client billing/control/cloud.

Payment review, reversals, and bank-transfer approval are still separate workflows.

## Guardrail

Do not build another invoice model inside `SurveyValuation`.

Survey invoice workflows may prepare and select survey jobs, but the money documents should land in the shared `Billing`, `Payments`, and `Accounting` modules.
