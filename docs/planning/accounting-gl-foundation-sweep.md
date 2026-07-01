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
3. Add application use cases for creating charge codes and client charge rules. Done.
4. Add invoice draft generation from client charge rules. Done.
5. Add invoice finalization that creates a balanced journal entry. Done for the basic debit receivable / credit revenue path.
6. Add receipt/payment allocation that updates invoice balance and posts a receipt journal entry. Done for immediately approved payment methods.
7. Add client accounting profile and cloud outbox so invoices can post to GL and publish to SafarSuite Control Cloud reliably. Done.
8. Add PostgreSQL persistence for payment records so receipt posting is durable end to end.
9. Add a client statement/receivables read model so client invoices, approved payments, open balance, and related journal postings can be reviewed from one screen. Done for the basic local view.

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
| Temporary in-memory repositories | Replaced for active control spine | Clients, contacts, support notes, client accounting profiles, client contracts, contract module allowances, ledger accounts, journal entries, charge codes, client charge rules, invoices, invoice lines, payments, entitlement snapshots, entitlement modules, and cloud outbox messages now have PostgreSQL repositories |
| Invoice draft generation | Done | `GenerateInvoiceDraft` creates draft invoices from effective client charge rules |
| GL posting use cases | In Progress | Invoice issue and immediately approved invoice payment posting create posted balanced journal entries; tax, review, reversal, and reports remain pending |
| Accounting read models | Done | Journal entry listing and ledger account activity read models expose the accounting trail created by invoice and receipt posting |
| Survey invoice preparation bridge | Parked | Work exists, but SurveyValuation is no longer part of the active product path |
| Client accounting profile | Done | Link client to receivable/default currency/cloud identity and stop passing AR account manually |
| Cloud invoice outbox | Done for local durability | Enqueue persisted `InvoiceIssued` publish message when invoice is issued |
| Payment outbox events | Done for local durability | Enqueue persisted `PaymentRecorded` and `ClientPaidStatusChanged` messages when an approved receipt is posted |
| Local outbox publisher | Done for development | Manual dev endpoint builds signed envelopes and marks ready outbox messages sent/failed without calling the real cloud |
| Control Cloud publish readiness | Basic done | Signed v1 envelope, HTTP publisher adapter, publish mode config, retry attempt timestamps, and migration are wired |
| Local entitlement snapshots | Done for local durability | Issue persisted entitlement snapshots from paid invoices and enqueue `EntitlementSnapshotIssued` |
| Contract-driven entitlement defaults | Done for local durability | Entitlement issue can derive paid-until, grace/offline validity, device/branch limits, and modules from the paid invoice contract |
| Contract maintenance API | Done for backend | Create/read/list/suspend/replace active contract flows are wired against PostgreSQL |
| Client contract UI | Basic done | Client desk lists contracts and exposes create, replace-active, and suspend actions |
| Client billing setup UI | Basic done | Client desk exposes accounting profile, charge setup, invoice draft, and invoice issue workflows |
| Client payment and entitlement UI | Basic done | Client desk exposes approved payment receipt plus local entitlement issue/refresh workflows |
| Client statement/receivables view | Basic done | Client desk exposes invoices, approved payments, currency summaries, running balance, and journal postings for the selected client |

Current control-spine endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/clients` | Create a SafarSuite client |
| `POST` | `/api/v1/contracts/client-contracts` | Create and activate a local client contract |
| `GET` | `/api/v1/contracts/client-contracts/{contractId}` | Read a local client contract |
| `GET` | `/api/v1/contracts/clients/{clientId}/client-contracts` | List local client contracts for a client |
| `POST` | `/api/v1/contracts/client-contracts/{contractId}/suspend` | Suspend a local client contract |
| `POST` | `/api/v1/contracts/client-contracts/replace-active` | Suspend the current active client contract and create the replacement |
| `POST` | `/api/v1/accounting/ledger-accounts` | Create a ledger account |
| `POST` | `/api/v1/billing/charge-codes` | Create a billable charge code linked to revenue/tax accounts |
| `POST` | `/api/v1/billing/client-charge-rules` | Create a dynamic charge rule for a client |
| `POST` | `/api/v1/billing/invoice-drafts` | Generate a draft invoice from active client charge rules |
| `POST` | `/api/v1/billing/invoices/{invoiceId}/issue` | Issue a draft invoice and post the balanced GL journal entry |
| `POST` | `/api/v1/payments/invoice-payments` | Record an approved invoice payment, update invoice balance, and post the receipt journal |
| `POST` | `/api/v1/entitlements/snapshots/from-paid-invoice` | Issue a local entitlement snapshot from a paid invoice |
| `POST` | `/api/v1/entitlements/snapshots/from-paid-invoice/defaults` | Issue a local entitlement snapshot using paid invoice contract defaults |
| `GET` | `/api/v1/entitlements/clients/{clientId}/latest-snapshot` | Read the latest local entitlement snapshot for a client |
| `GET` | `/api/v1/accounting/journal-entries` | List journal entries with optional date/source filters |
| `GET` | `/api/v1/accounting/ledger-accounts/{ledgerAccountId}/activity` | Show account activity with running balance |
| `GET` | `/api/v1/clients/{clientId}/statement` | Show a client statement with invoices, payments, balances, and related journal postings |
| `GET` | `/api/v1/control-cloud/outbox-messages` | List cloud outbox messages |
| `POST` | `/api/v1/control-cloud/outbox-messages/publish` | Publish ready outbox messages through the configured local or HTTP publisher |
| `POST` | `/api/v1/control-cloud/outbox-messages/publish-local` | Compatibility alias for the local development publishing flow |
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
  -> issue local entitlement snapshot from the paid invoice
  -> enqueue entitlement snapshot outbox message
  -> list journal entries
  -> view AR and cash ledger account activity
```

The smoke test verified an issued invoice with a posted journal entry, then a posted payment journal entry, where total debit equals total credit for both postings. The read-model smoke test verified two journal entries, one payment receipt journal entry through source filtering, AR ending balance `0.00`, and cash ending balance `251.00`.

The PostgreSQL persistence smoke test verified client accounting profile resolution, charge code/rule persistence, invoice draft persistence, invoice issue, posted balanced journal entry, and a pending persisted `InvoiceIssued` outbox message. The issued invoice posted `3000.00` debit and `3000.00` credit, and direct PostgreSQL inspection confirmed rows in `client_accounting_profiles`, `charge_codes`, `client_charge_rules`, `invoices`, `invoice_lines`, and `cloud_outbox_messages`.

The payment persistence smoke test verified a persisted approved invoice payment, invoice status changing to `Paid`, invoice balance due changing to `0.00`, and a posted balanced receipt journal entry. Direct PostgreSQL inspection confirmed the row in `payments` plus the paid invoice state.

The payment outbox smoke test verified that approved receipt posting also creates pending persisted `PaymentRecorded` and `ClientPaidStatusChanged` outbox messages. Direct PostgreSQL inspection confirmed both rows in `cloud_outbox_messages` with the invoice and payment IDs in the JSON payload.

The local outbox publisher smoke test verified that `POST /api/v1/control-cloud/outbox-messages/publish-local?batchSize=5` marked five pending messages `Sent` with zero failures. Direct PostgreSQL inspection confirmed sent rows for `InvoiceIssued`, `PaymentRecorded`, and `ClientPaidStatusChanged`.

The local entitlement smoke test verified that a paid invoice can issue an `Active` entitlement snapshot, persist two module rows, read the latest client snapshot, and enqueue a pending `EntitlementSnapshotIssued` outbox message.

The contract-driven entitlement smoke test verified that an active persisted contract can feed entitlement defaults: paid-until came from the contract end date, grace/offline dates were derived locally, device/branch limits and module allowances were copied from the contract, and the latest entitlement snapshot matched the issued snapshot.

The contract maintenance smoke test verified create, read, list, replace-active, and suspend flows. Replace-active suspended the previous active contract and created a new active contract; direct PostgreSQL inspection confirmed both contract statuses and their module allowance rows.

The client contract UI smoke test verified that the Vite client desk renders the contract panel and create/replace actions with no browser console errors.

The client statement smoke test verified both an invoice-only client and a fully paid client. The invoice-only statement showed one invoice, one statement line, one journal posting, and a `3000.00 PKR` balance. The paid-client statement showed one invoice, one approved payment, two statement lines, two journal postings, and a `0.00 PKR` ending balance.

The cloud-readiness smoke test applied migration `20260701090000_AddCloudOutboxPublishReadiness`, then published three ready outbox messages through `POST /api/v1/control-cloud/outbox-messages/publish?batchSize=3`. The response returned three sent messages, attempt count `1`, sent timestamps, cloud references based on the idempotency key, and HMAC envelope signatures.

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

The PostgreSQL/EF implementation now uses a real database transaction for persisted slices. This covers invoice issue end to end: invoice status update, posted journal entry, journal lines, and `InvoiceIssued` outbox message are saved in one transaction. Payment posting is also durable end to end: payment record, invoice balance/status update, posted receipt journal entry, journal lines, and payment/client-status outbox messages are saved in one transaction.

Next accounting slice:

```text
pre-cloud boundary decision
  -> confirm whether to scaffold SafarSuite Control Cloud next
  -> if yes, add a receiver skeleton with signature validation and idempotency
  -> if no, stay local and add payment review/reversal plus tax posting
  -> keep real cloud publication out of accounting transactions
```

SurveyValuation bridge work exists but is parked. Do not extend it while the active goal is SafarSuite client billing/control/cloud.

Payment review, reversals, and bank-transfer approval are still separate workflows.

## Guardrail

Do not build another invoice model inside `SurveyValuation`.

Survey invoice workflows may prepare and select survey jobs, but the money documents should land in the shared `Billing`, `Payments`, and `Accounting` modules.
