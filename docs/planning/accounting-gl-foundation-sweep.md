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
  -> payment review if the method needs approval
  -> payment posting
       debit cash/bank
       credit accounts receivable
  -> payment reversal when a posted receipt must be undone
       debit accounts receivable
       credit cash/bank
  -> unpaid invoice void when an issued invoice must be cancelled before payment
       debit revenue/tax payable
       credit accounts receivable
  -> credit note when a paid invoice must be corrected
       debit revenue/tax payable
       credit accounts receivable
  -> credit application when client credit should settle a future invoice
       no new GL journal
       allocate existing credit to the open invoice in the client subledger
  -> client refund when credit should be paid back
       debit accounts receivable
       credit cash/bank
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
6. Add receipt/payment allocation that updates invoice balance and posts a receipt journal entry. Done for immediately approved methods and bank-transfer approval.
7. Add client accounting profile and cloud outbox so invoices can post to GL and publish to SafarSuite Control Cloud reliably. Done.
8. Add PostgreSQL persistence for payment records so receipt posting is durable end to end.
9. Add a client statement/receivables read model so client invoices, approved payments, open balance, and related journal postings can be reviewed from one screen. Done for the basic local view.
10. Add payment review and reversal so bank transfers can be held before GL posting and approved receipts can be reversed with their own balanced journal. Done for the local client desk loop.
11. Add tax percent on client charge rules, materialize tax invoice lines, and credit tax payable during invoice issue. Done for the local taxable invoice loop.
12. Add unpaid issued-invoice voiding with a reversing journal, statement visibility, and `InvoiceVoided` outbox message. Done for the local correction loop.
13. Add full credit notes for paid/partially paid invoice correction with a reversing sale journal, statement credit visibility, and `CreditNoteIssued` outbox message. Done for the local correction loop.
14. Add client refunds for credit balances created by credit notes with a balanced refund journal, statement visibility, and `ClientRefundIssued` outbox message. Done for the local correction loop.
15. Add settlement controls to apply client credit balances to future invoices. Done for the local correction loop.

## Implementation Progress

| Slice | Status | Notes |
| --- | --- | --- |
| Accounting domain primitives | Done | `LedgerAccount`, `LedgerAccountCode`, account type/status/normal balance enums |
| Journal domain primitives | Done | `JournalEntry`, `JournalLine`, source/status enums; posting requires balanced debit/credit totals |
| Billing charge catalog primitives | Done | `ChargeCode`, `ChargeCodeKey`, default price, revenue/tax account links |
| Dynamic client charge rules | Done | `ClientChargeRule` supports client/contract, charge code, price, quantity, tax percent, billing cycle, billing day, and effective period |
| Invoice line charge link | Done | `InvoiceLine` can now reference a `ChargeCodeId` and declare whether the line is a charge or tax line |
| Application use cases | In Progress | Create use cases added for clients, ledger accounts, charge codes, client charge rules, invoice draft generation, invoice issue/void/credit-note posting, invoice payment record/approve/reject/reverse, client refund issue, client credit application, journal listing, and ledger account activity |
| API endpoints | In Progress | Minimal create endpoints plus invoice draft, invoice issue/void/credit-note, invoice payment record/approve/reject/reverse, client refund issue, client credit application, journal listing, and ledger account activity endpoints are wired |
| Temporary in-memory repositories | Replaced for active control spine | Clients, contacts, support notes, client accounting profiles, client contracts, contract module allowances, ledger accounts, journal entries, charge codes, client charge rules, invoices, invoice lines, credit notes, payments, client refunds, client credit applications, entitlement snapshots, entitlement modules, and cloud outbox messages now have PostgreSQL repositories |
| Invoice draft generation | Done | `GenerateInvoiceDraft` creates draft invoices from effective client charge rules |
| GL posting use cases | In Progress | Invoice issue, taxable invoice posting, unpaid invoice voiding, full credit notes, client refunds, immediately approved invoice payment posting, bank-transfer approval posting, and payment reversal posting create posted balanced journal entries; credit application is handled as client subledger allocation without a new GL journal; reports remain pending |
| Accounting read models | Done | Journal entry listing and ledger account activity read models expose the accounting trail created by invoice and receipt posting |
| Survey invoice preparation bridge | Parked | Work exists, but SurveyValuation is no longer part of the active product path |
| Client accounting profile | Done | Link client to receivable/default currency/cloud identity and stop passing AR account manually |
| Cloud invoice outbox | Done for local durability | Enqueue persisted `InvoiceIssued` publish message when invoice is issued |
| Payment outbox events | Done for local durability | Enqueue persisted `PaymentRecorded` and `ClientPaidStatusChanged` messages when an approved receipt is posted; enqueue `PaymentReversed` and paid-status updates when a receipt is reversed |
| Local outbox publisher | Done for development | Manual dev endpoint builds signed envelopes and marks ready outbox messages sent/failed without calling the real cloud |
| Control Cloud publish readiness | Basic done | Signed v1 envelope, HTTP publisher adapter, publish mode config, retry attempt timestamps, and migration are wired |
| Local entitlement snapshots | Done for local durability | Issue persisted entitlement snapshots from paid invoices and enqueue `EntitlementSnapshotIssued` |
| Contract-driven entitlement defaults | Done for local durability | Entitlement issue can derive paid-until, grace/offline validity, device/branch limits, and modules from the paid invoice contract |
| Contract maintenance API | Done for backend | Create/read/list/suspend/replace active contract flows are wired against PostgreSQL |
| Client contract UI | Basic done | Client desk lists contracts and exposes create, replace-active, and suspend actions |
| Client billing setup UI | Basic done | Client desk exposes accounting profile, charge/tax setup, invoice draft, invoice issue, and unpaid invoice void workflows |
| Client payment and entitlement UI | Basic done | Client desk exposes payment receipt, bank-transfer approval/rejection, payment reversal, credit settlement, client refund, and local entitlement issue/refresh workflows |
| Client statement/receivables view | Basic done | Client desk exposes invoices, approved/reversed payments, credit notes, applied credit, client refunds, available credit summaries, running balance, and journal postings for the selected client |
| Payment review and reversal foundation | Basic done | Bank transfer receipts can wait in pending review without GL posting; approval posts the receipt journal, rejection closes the pending item, and reversal posts a dedicated reversal journal |
| Billing tax foundation | Basic done | Client charge rules carry tax percent, invoice drafts create tax lines, invoice issue credits tax payable, and EF migration persists `tax_percent` plus invoice `line_type` |
| Unpaid invoice void foundation | Basic done | Unpaid issued invoices can be voided with a reversing journal, pending `InvoiceVoided` outbox message, and statement lines that net the invoice to zero |
| Full credit note foundation | Basic done | Paid and partially paid invoices can receive one full credit note with a reversing sale journal, pending `CreditNoteIssued` outbox message, EF persistence, and statement credit lines |
| Client refund foundation | Basic done | Client credit balances can be refunded with a balanced AR/cash-bank journal, pending `ClientRefundIssued` outbox message, EF persistence, and statement debit lines |
| Client credit settlement foundation | Basic done | Unapplied client credit can be allocated to issued/partially paid invoices without a new GL journal, with pending `ClientCreditApplied` outbox message, EF persistence, invoice balance update, available-credit summaries, and zero-net statement allocation lines |

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
| `POST` | `/api/v1/billing/charge-codes` | Create a billable charge code linked to validated revenue/tax posting accounts |
| `POST` | `/api/v1/billing/client-charge-rules` | Create a dynamic charge/tax rule for a client |
| `POST` | `/api/v1/billing/invoice-drafts` | Generate a draft invoice from active client charge rules, including tax lines |
| `POST` | `/api/v1/billing/invoices/{invoiceId}/issue` | Issue a draft invoice and post the balanced AR/revenue/tax GL journal entry |
| `POST` | `/api/v1/billing/invoices/{invoiceId}/void` | Void an unpaid issued invoice, post the reversing GL journal entry, and enqueue `InvoiceVoided` |
| `POST` | `/api/v1/billing/invoices/{invoiceId}/credit-notes` | Issue one full credit note for a paid/partially paid invoice, post the reversing sale journal, and enqueue `CreditNoteIssued` |
| `POST` | `/api/v1/payments/client-refunds` | Refund a client credit balance, post the AR/cash-bank refund journal, and enqueue `ClientRefundIssued` |
| `POST` | `/api/v1/payments/client-credit-applications` | Apply unapplied client credit to an issued/partially paid invoice and enqueue `ClientCreditApplied` |
| `POST` | `/api/v1/payments/invoice-payments` | Record an invoice payment; immediately approved methods post the receipt journal, while bank transfers wait for review |
| `POST` | `/api/v1/payments/invoice-payments/{paymentId}/approve` | Approve a pending payment, update invoice balance, post the receipt journal, and enqueue payment events |
| `POST` | `/api/v1/payments/invoice-payments/{paymentId}/reject` | Reject a pending-review payment without GL posting |
| `POST` | `/api/v1/payments/invoice-payments/{paymentId}/reverse` | Reverse an approved payment, reopen invoice balance, post a reversal journal, and enqueue reversal events |
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
       credit tax payable when configured
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

The payment review/reversal smoke test ran against the local API with in-memory persistence. It verified that a bank-transfer payment starts as `PendingReview` with no journal, approval changes it to `Approved`, posts the receipt journal, and changes the invoice to `Paid`. It then verified reversal changes the payment to `Reversed`, returns the invoice to `Issued`, restores balance due to `125.00`, shows three statement lines, shows three related journal postings, and creates one `PaymentReversal` journal for the payment reference.

The taxable invoice smoke test ran against the local API with in-memory persistence. It verified a `10%` taxable client charge rule with base amount `100.00`, tax amount `10.00`, invoice draft total `110.00`, two draft lines (`Charge` and `Tax`), issued invoice status `Issued`, balanced journal debit/credit totals of `110.00`, AR debit `110.00`, revenue credit `100.00`, tax payable credit `10.00`, and statement balance `110.00`.

The unpaid invoice void smoke test ran against the local API with in-memory persistence. It verified an issued taxable invoice at `110.00`, then voided it before payment. The void result set invoice status to `Void`, created one posted `BillingInvoiceVoid` journal with debit/credit totals of `110.00`, showed two invoice-related journal postings, showed two statement lines including one `Invoice void` credit line, returned statement balance `0.00`, and enqueued one pending `InvoiceVoided` outbox message.

The full credit-note smoke test ran against the local API with in-memory persistence. It verified a taxable invoice at `110.00`, posted an approved payment so the invoice became `Paid`, then issued a full credit note. The credit note created one posted `BillingCreditNote` journal with debit/credit totals of `110.00`, left the invoice as `Paid`, showed one `Credit note` statement line, returned statement balance `-110.00` as client credit, showed three related journal postings, and enqueued one pending `CreditNoteIssued` outbox message.

The client refund smoke test ran against the local API with in-memory persistence. It verified the taxable paid invoice plus full credit-note path, then issued a partial `60.00 PKR` client refund from the `-110.00 PKR` credit balance. The refund created one posted `ClientRefund` journal with debit/credit totals of `60.00`, returned client balance before refund `-110.00`, returned balance after refund `-50.00`, showed one `Client refund` statement debit line, showed one client refund journal through source filtering, and enqueued one pending `ClientRefundIssued` outbox message.

The client credit settlement smoke test ran against the local API with in-memory persistence. It verified a taxable paid invoice plus full credit-note path, then issued a second taxable invoice and applied `60.00 PKR` of the existing `110.00 PKR` unapplied credit. Before settlement, the client statement balance was `0.00` while available credit was `110.00`; after settlement, the second invoice became `PartiallyPaid` with balance `50.00`, available credit became `50.00`, client statement balance stayed `0.00`, the statement showed one zero-net `Applied credit` line with debit and credit both `60.00`, and one pending `ClientCreditApplied` outbox message was enqueued.

The repeatable accounting smoke runner now lives at `tools/SafarSuite.ControlDesk.AccountingSmoke`. It runs the core local chain through application handlers: ledger setup, client/profile/contract setup, taxable invoice draft, invoice issue, approved receipt, full credit note, partial client refund, second invoice, and client credit settlement. The smoke asserts balanced GL journals for invoice issue, receipt, credit note, refund, and the second invoice; it also asserts that client credit application remains a subledger allocation with no GL journal, updates available credit, appears as a zero-net statement line, and enqueues `ClientCreditApplied`.

Run the fast in-memory mode after building the solution:

```powershell
dotnet run --project tools\SafarSuite.ControlDesk.AccountingSmoke\SafarSuite.ControlDesk.AccountingSmoke.csproj --no-build
```

Run the PostgreSQL/EF mode after the local Docker Postgres service is running:

```powershell
dotnet run --project tools\SafarSuite.ControlDesk.AccountingSmoke\SafarSuite.ControlDesk.AccountingSmoke.csproj --no-build -- --provider postgres
```

The PostgreSQL mode applies EF migrations first and uses unique smoke document numbers, so old dev rows should not pollute the assertions. It has now passed locally against the Docker/PostgreSQL database on `localhost:54329`, confirming the same invoice/payment/credit/refund/settlement chain through EF mappings, migrations, and real transaction handling.

The client desk UI guardrail pass is basic done. Invoice issue, invoice void, credit note, receipt posting, payment approval/rejection/reversal, credit settlement, and client refund actions now require their minimum accounting inputs in the screen and ask for confirmation before the page handler calls the backend action.

## Transaction Policy

Accounting-sensitive actions must be atomic.

The application layer now exposes two unit-of-work shapes:

| Method | Use |
| --- | --- |
| `SaveChangesAsync` | Single-aggregate writes, such as creating one ledger account or one charge code |
| `ExecuteInTransactionAsync` | Multi-aggregate/table writes that must succeed or fail together |

Use `ExecuteInTransactionAsync` for:

- invoice issue/finalize plus journal entry creation
- unpaid invoice void plus reversing journal entry creation
- full credit note plus reversing sale journal entry creation
- client credit application plus invoice balance update and outbox message creation
- client refund plus refund journal entry and outbox message creation
- receipt/payment approval plus invoice allocation plus journal entry creation
- payment reversal plus ledger reversal
- opening balance import across accounts and invoices

The PostgreSQL/EF implementation now uses a real database transaction for persisted slices. This covers invoice issue end to end: invoice status update, posted journal entry, journal lines, and `InvoiceIssued` outbox message are saved in one transaction. Payment posting is also durable end to end: payment record, invoice balance/status update, posted receipt journal entry, journal lines, and payment/client-status outbox messages are saved in one transaction. Client refunds are also transactional: refund record, posted refund journal entry, journal lines, and `ClientRefundIssued` outbox message are saved together. Client credit applications are transactional as subledger allocations: application record, invoice balance/status update, and `ClientCreditApplied` outbox message are saved together.

Next accounting slice:

```text
local accounting hardening
  -> done: add a repeatable in-memory accounting smoke runner for correction flows
  -> done: run the same smoke runner against PostgreSQL migrations
  -> done: review UI wording and guardrails for invoice/payment/credit actions
  -> done: confirm and scaffold the Control Cloud receiver boundary
  -> done: wire the desk HTTP publisher to the local cloud receiver and publish real outbox rows
  -> next: build cloud-side projections for portal-readable invoice/payment/entitlement state
```

SurveyValuation bridge work exists but is parked. Do not extend it while the active goal is SafarSuite client billing/control/cloud.

## Guardrail

Do not build another invoice model inside `SurveyValuation`.

Survey invoice workflows may prepare and select survey jobs, but the money documents should land in the shared `Billing`, `Payments`, and `Accounting` modules.
