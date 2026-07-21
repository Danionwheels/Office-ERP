# Provider Office ERP Accounting Experience Masterplan - 2026-07-05

Purpose: turn the COA/accounting research, legacy Access source export, workbook/SQL evidence, and current Control Desk implementation into a provider-office-erp accounting design that feels minimal, detailed, accountant-readable, user-friendly, and trustworthy.

This is for `provider-office-erp` / SafarSuite Control Desk. It is not a generic travel-agency accounting clone. Legacy SafarSuite accounting gives us proven patterns, but this product's accounting exists to run the provider office: clients, contracts, invoices, payments, refunds, credit notes, settlements, entitlements, cloud publishing, and provider books.

## Product Boundary

Provider Office ERP accounting owns:

- Provider chart of accounts, periods, controls, journals, reports, opening balances, approvals, audit, and close.
- Client receivables, provider invoices, payments, credit notes, client credit settlement, and refunds.
- GL proof for cloud-facing commercial state: paid status, entitlement issue, portal invoice/payment projections, and local-server control messages.
- Provider revenue and tax visibility by client, module, contract, branch/site, and period.

Provider Office ERP accounting does not own:

- The client's operational travel books inside deployed SafarSuite runtime.
- Client travel ticket posting, airline BSP posting, tour package ledgers, or visa package ledgers unless we later build a separate SafarSuite client accounting module.
- Control Cloud as original accounting truth. Control Cloud receives signed projections; Control Desk remains the source.

## Experience Promise

The accounting workspace should feel like a quiet accounting desk:

1. A beginner can find the correct action without knowing GL language.
2. An accountant can inspect every posting, account, report number, source document, and period decision.
3. An owner can understand cash, receivables, revenue, client risk, and paid-status control at a glance.
4. A support/operator user can do routine work without being allowed to damage accounting truth.

The design target is not a flashy interface. The target is stronger: every serious user should feel that the system is clear, careful, fast, and hard to misuse.

## Core UX Principles

- One accounting workspace, not scattered pages.
- Every number drills down to evidence.
- Every source document shows its GL impact.
- Every GL entry shows its source document.
- Posting and approval are separate states.
- Reports can explain themselves.
- Setup is guided and guarded.
- Imports are always previewed before posting.
- Reversals and corrections are explicit.
- Role permissions are visible in the workflow, not discovered after failure.
- Expert depth is one click away, not always on screen.
- Tables are dense, aligned, and readable; visual styling stays restrained.
- Labels use accountant language first, with plain operational labels only where they reduce confusion.

## Current Baseline

The current app already has a strong GL foundation:

- `AccountingWorkspace` areas: Chart of Accounts, Controls, Periods, Journal, Reports, Reconcile.
- Controlled account ranges and code suggestions.
- `H/T/M/D/C/S` ledger account levels.
- Parent/posting hierarchy guards.
- Ledger account reconciliation and repair-plan dry run.
- Manual journals with voucher previews.
- Opening balance preview/post and text import.
- Voucher numbering rules.
- Accounting periods, close readiness, close preview, close/reopen.
- Trial balance, profit and loss, balance sheet, ledger activity, source-document drill-in.
- Billing/payment/credit/refund GL integration from the client desk.

The next step is not to replace this. The next step is to make it feel like a finished accounting product instead of a set of correct slices.

## Target Workspace

### 1. Accounting Overview

First screen for Accounting.

Primary panels:

- Period status: open period, next close date, close blockers, draft count.
- Receivables: total AR, overdue AR, client credit, refund pending.
- Cash and bank: unreviewed payments, approved receipts, refund outflows, bank book balance.
- Posting health: unbalanced drafts, unsupervised entries, missing controls, missing source links.
- Cloud-control alignment: commercial outbox pending, failed publishes, entitlement issued after payment.
- Quick actions: create journal, import opening balance, review payments, issue credit note, close period, run trial balance.

Quiet detail:

- A "why this matters" drilldown on each number, without visible instructional copy on the main screen.
- Health chips that open the exact rows causing risk.
- Timeline of recent GL-impacting events: invoice issued, payment approved, payment reversed, credit issued, refund posted, entitlement published.

### 2. Chart Of Accounts

Replace the feeling of a flat account list with a maintainable accounting tree that an accountant can scan quickly.

Core areas:

- Left: nested `H/T/M/D/C/S` account tree with collapse, search, type, status, role, and health filters.
- Middle: selected account profile with code, display code, name, type, normal balance, level, parent, posting flag, status, range role, and activity summary.
- Right: setup/range inspector with role range, suggested next code, parent rule, and conflicts.
- Bottom drawer: activity lines, source entries, account audit, and repair suggestions.

Account creation should happen inside the nested tree, not in a detached create form. The user selects the header/master/control account or range context, chooses add child/add sibling, and the new row opens inline with the next suggested code, inherited parent, expected level, type, normal balance, and posting flag already filled. This matches the legacy COA working style and keeps the accountant oriented.

Implementation note, 2026-07-05: first UI slice is now wired in `ChartOfAccountsPanel`. The tree exposes contextual add-child/add-sibling actions, opens an inline create row, and carries range, resolved parent, level, posting flag, type, and normal balance into the existing save flow.

Implementation note, 2026-07-05: standard COA bootstrap is now available through `POST /api/v1/accounting/accounting-setup/standard-chart-of-accounts` and the COA toolbar `Load COA` action. The bootstrap creates the provider-office skeleton once, reuses existing codes on later runs, and reconciles cleanly after loading.

Inline creation modes:

- Add child under selected header/master/control account.
- Add sibling in the same range/level.
- Add subsidiary under a control account.
- Add missing setup account from a setup checklist issue.
- Convert a suggested placeholder into a real account after import preview.

Inline row rules:

- Show only fields needed for the selected level.
- Keep code, name, level, type, normal balance, posting flag, and status visible in one row.
- Put advanced fields in a side drawer.
- Validate range, parent, level, posting flag, and duplicate code before save.
- After save, keep the account selected and show activity/setup impact immediately.

Deferred legacy COA nesting rule, saved from the 2026-07-05 review:

- The legacy COA should be treated as an open-ended nested register, not a detached create form and not a fixed-depth tree.
- `Total` accounts appear as normal visible rows in the COA list and may sit inside the hierarchy.
- Account type choices such as Header, Control, Subsidiary, Total, and Master define accounting behavior, while parent/child depth stays flexible.
- The user should be able to create child, grandchild, and deeper accounts inline under the selected row, with parent-aware code suggestion and rollup validation.
- This is deferred until the current accounting flow is finished, then should guide the next COA tree polish pass.

Provider-office account roles:

- AR control
- Client subsidiary AR
- Cash on hand
- Bank accounts
- Payment gateway clearing
- Bank charges
- Tax payable
- Withholding tax payable/receivable
- Subscription revenue
- Setup/implementation revenue
- Support/service revenue
- Module add-on revenue
- Discount and concession
- Refund clearing
- Client credit clearing
- Unearned/deferred revenue if prepaid contracts are later supported
- Retained earnings
- Income summary
- Rounding difference
- Cloud/control operating cost categories if needed later

Guardrails:

- Non-posting levels cannot receive lines.
- Posting flag must match level.
- Child code must fit parent/control range.
- Account cannot be deleted after activity.
- Inactivation requires no open posting dependency.
- Parent lock flows to children.
- Account role conflicts are visible before save.

### 3. Accounting Setup Center

The legacy `CP` setup concept becomes a provider-office setup center.

Setup sections:

- Company/base currency.
- Account code ranges.
- Control accounts.
- Revenue accounts by module/charge category.
- Tax and withholding accounts.
- Cash/bank/payment gateway accounts.
- Refund and client-credit accounts.
- Retained earnings, income summary, rounding.
- Voucher types and numbering.
- Approval policy.
- Import policy.

Key design:

- Show setup as a checklist with severity, not as isolated forms.
- Each setup row has: status, linked account, expected account type, expected normal balance, and impact.
- Offer "use defaults" for a new provider office, but show exactly what will be created.

### 4. Voucher And Journal Workbench

This becomes the operator/accountant's daily ledger surface.

States:

- Draft
- Posted
- Supervised or Approved
- Published to Cloud, where relevant
- Reversed
- Voided
- Contra hidden, for specific reporting scenarios

Core capabilities:

- Manual journals.
- System-generated journals from invoices/payments/credits/refunds.
- Voucher type selector.
- Voucher number preview.
- Source-document side panel.
- Attachments/reference fields.
- Branch/site and adjusted-date fields.
- Base and foreign amount columns.
- Copy voucher workflow.
- Reverse/void workflow.
- Approval/supervision workflow.

Quiet detail:

- A line-level balance strip that always shows debit, credit, and difference.
- Account picker shows account type, normal balance, role, status, and recent use.
- Source document preview sits beside the journal: client, invoice, payment, refund, entitlement consequence.
- Posting preview explains the GL impact before commit.

### 5. Client Money Chain

Provider-office accounting should be best-in-class where it touches clients.

Each client should have a complete money chain:

```text
Contract
  -> Invoice draft
  -> Issued invoice
  -> AR journal
  -> Payment pending review
  -> Receipt approval
  -> Cash/bank journal
  -> Paid status change
  -> Entitlement snapshot
  -> Control Cloud signed projection
```

Corrections:

```text
Invoice void
Credit note
Payment reversal
Client credit settlement
Client refund
```

Every step should show:

- Business document.
- GL journal.
- Client statement effect.
- Cloud outbox effect.
- Audit actor/time.

### 6. Opening Balance And Import Cockpit

Imports must feel safe, not scary.

Supported import modes:

- Manual grid.
- CSV/text paste.
- Excel workbook import.
- Legacy SQL/Access import later.

Preview stages:

- Parse result.
- Account matching.
- Branch/site matching.
- Currency/rate matching.
- Debit/credit totals.
- Duplicate detection.
- Posting preview.
- Blockers/warnings.

Rules:

- A row can have debit or credit, not both.
- Totals must balance by base currency.
- Foreign currency rows must have currency and rate.
- Unknown accounts cannot auto-create unless the user explicitly chooses a guided COA import flow.
- Import batches get durable ids and audit.

### 7. Period Close Board

Close should feel like a board, not a button.

Sections:

- Period timeline.
- Readiness checks.
- Draft/unapproved journal list.
- Unreviewed payments.
- Failed cloud outbox rows.
- Unbalanced currency totals.
- Missing control accounts.
- Net income preview.
- Retained earnings journal preview.
- Close artifact.

Close action:

- Requires preview.
- Requires permission.
- Creates explicit close journal/artifact.
- Locks posting into closed dates.
- Reopen requires reason and audit.

### 8. Reports And Drilldown

Reports should be calm, dense, and explainable.

Core reports:

- Trial balance.
- General ledger.
- Profit and loss.
- Balance sheet.
- Cash/bank book.
- Client AR aging.
- Client statement.
- Revenue by module/charge type.
- Tax summary.
- Payment review/reversal register.
- Credit/refund register.
- Period close package.
- Cloud commercial reconciliation: GL vs projected cloud state.

Report UX:

- Filters: period, date basis, adjusted date, branch/site, client, module, account range, status.
- Click report line -> account activity.
- Click activity -> journal.
- Click journal -> source document.
- Click source document -> client money chain.
- Export clean PDF/Excel later.

Provider-office niche reports:

- Paid clients with unpaid GL risk.
- Paid GL state not yet published to cloud.
- Entitlement issued without approved receipt.
- Client credit balance with active entitlement.
- Module revenue vs enabled module entitlements.
- Failed outbox with financial impact.

### 9. Reconciliation And Health

This is the accounting safety layer.

Health checks:

- Account range mismatch.
- Wrong parent.
- Wrong posting flag.
- Missing control account.
- Inactive account used by active billing rule.
- Journal without source link.
- Source document without journal.
- Payment approved without receipt journal.
- Paid status without payment journal.
- Entitlement issued before paid-state proof.
- Cloud outbox failed for accounting event.
- Unsupervised posted entries.
- Branch/date/currency mismatch.

Repair plan:

- Dry-run first.
- Categorize as safe automatic, guided, or manual.
- Show before/after.
- Require approval for mutating repairs.

### 10. Audit And Permissions

Every important accounting action needs an audit trail:

- Setup changed.
- Account created/edited/inactivated.
- Journal posted/reversed/voided.
- Voucher approved/unsupervised.
- Period closed/reopened.
- Import preview posted.
- Payment approved/reversed.
- Credit/refund issued.
- Outbox published/failed/retried.

Permissions:

- View accounting.
- Maintain COA.
- Maintain controls.
- Post manual journal.
- Approve/supervise journal.
- Reverse/void.
- Import opening balances.
- Close/reopen period.
- View reports.
- Export reports.
- Repair accounting setup.

## Data Model Extensions To Plan

Near-term additions:

- Branch/site dimension on journal header and/or lines.
- Adjusted date on journal lines.
- Voucher type aggregate.
- Approval/supervision state separate from posted state.
- Source document link table.
- Import batch table.
- Base and foreign debit/credit fields.
- Dimension set table.
- Journal audit table or richer domain event projection.

Later additions:

- Report format/range mapping.
- Budget table.
- Tax code table.
- Bank account profile.
- Payment gateway clearing profile.
- Multi-company support beyond `MAIN`.
- Report snapshot package for period close.

## Build Roadmap

### Phase 1: Provider Accounting Experience Frame

- Add the Accounting Overview.
- Rework workspace navigation into clearer accounting sections.
- Add source-document drilldown consistency across reports, activity, and journal.
- Add health chips and accounting risk summary.
- Make current GL features feel like one product.

### Phase 2: Setup And COA Mastery

- Expand accounting controls into full provider-office control setup.
- Add richer account role ranges and setup checklist.
- Add COA import/preview from workbook and legacy evidence.
- Add guided repair execution after dry-run.
- Add account lock/inactivation rules.

### Phase 3: Voucher, Approval, Date, Currency

- Add voucher type aggregate.
- Add approval/supervision state.
- Add adjusted date.
- Add branch/site dimensions.
- Add base/foreign amount support.
- Add voucher copy and reversal/void polish.

### Phase 4: Reports That Explain Themselves

- Add general ledger and cash/bank book.
- Add client AR aging and module revenue reports.
- Add cloud commercial reconciliation.
- Add report drilldown breadcrumbs.
- Add period close package.

### Phase 5: Legacy Import And Migration Confidence

- Add Access/SQL import mapping previews.
- Add COA, opening balance, voucher, and client ledger import dry-runs.
- Add import batch audit and rollback strategy.
- Add parity dashboards comparing legacy totals to provider-office ERP totals.

## Acceptance Standard

The accounting system is ready when:

- A new operator can issue invoices, record payments, and find client balances without asking where to go.
- An accountant can trace any report number down to source evidence in under four clicks.
- A provider owner can see cash, AR, revenue, risk, and cloud-publish health from one screen.
- Bad accounting actions are blocked with specific reasons.
- Corrections are explicit and audited.
- Imports are previewed and explain their totals.
- Period close produces a durable package.
- Control Cloud never receives a financial projection that Control Desk cannot explain from GL/source documents.
