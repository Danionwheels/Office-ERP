# Accounting GL MVP Checkpoint - 2026-07-04

Purpose: pause the forward motion, freeze what the accounting/GL slice now actually does, and make the next moves deliberate instead of momentum-driven.

## Position

We are on track for a Control Desk accounting foundation. The work is not a full legacy accounting clone yet, but it is aligned with the legacy direction: controlled COA, account hierarchy, voucher/journal postings, period controls, report balances, and drill-through.

The current target remains single-company `MAIN`. We are not building multi-company accounting right now.

## Verified Current Surface

| Area | Current behavior | Verification |
| --- | --- | --- |
| Controlled COA setup | Company-scoped account-code ranges seed for `MAIN`; suggestions and ledger creation are range/type/balance/posting/level guarded | Accounting smoke and backend build |
| Legacy-style hierarchy | Header, Total, Master, Detail, Control, and Subsidiary levels are stored and surfaced; non-posting and parent rules are enforced | Accounting smoke |
| Reconciliation | COA reconciliation and repair-plan views flag loose or invalid accounts without mutating them | Accounting smoke and UI build |
| GL controls | Manual configuration exists, plus `Use defaults` wires retained earnings, income summary, and rounding accounts from configured ranges | Live API probe and accounting smoke |
| Periods | Accounting periods can be created, guarded for posting, closed, reopened, and attached to close artifacts | Accounting smoke |
| Journals | Manual journals, invoice/payment/correction journals, close journals, journal listing, focused journal line view, and source-document lookups exist | Accounting smoke and UI build |
| Reports | Trial balance, profit and loss, balance sheet, ledger activity, and report refresh are available in the Accounting workspace | Backend/UI builds and accounting smoke |
| Close handoff | Closing generates period close journals and balance sheet no longer shows current earnings after retained earnings handoff | Accounting smoke |
| Drill-in | Generated close journal rows can open the exact posted journal in the Journal workspace | UI build |

## Latest Checks

| Check | Result | Notes |
| --- | --- | --- |
| `dotnet build .\SafarSuite.ControlDesk.sln --no-restore -p:DebugSymbols=false -p:DebugType=None` | Passed | 0 warnings, 0 errors after the default controls slice. |
| `npm run build` in `apps/control-desk-ui` | Passed | Vite production build completed after the close-journal drill-in slice. |
| `dotnet run --project .\tools\SafarSuite.ControlDesk.AccountingSmoke\SafarSuite.ControlDesk.AccountingSmoke.csproj --no-build` | Passed | In-memory run `20260704150825671`; proves default controls and period-close balance-sheet handoff. |
| Live `POST /api/v1/accounting/accounting-controls/defaults` | Passed | Returned `MAIN`, `PKR`, `isConfigured: True`, reusing the existing `21000`, `23000`, and `61000` default accounts in the dev database. |
| `git diff --check` | Passed | Only existing line-ending warnings were reported. |

## Legacy Alignment

| Legacy/design evidence | Current alignment | Remaining gap |
| --- | --- | --- |
| `GL_Working.xlsx` account type legend: `H/T/M/D/C/S` | Implemented as persisted ledger account levels with posting and parent guards | Import/parity against a real workbook export is not done |
| `GL_Working.xlsx` controlled account ranges and account opening behavior | Implemented as persisted account-code ranges, setup-backed suggestions, and range validation | Broader account-opening UX is still compact, not full legacy form parity |
| `ACT_SD_COA_LEVEL3` controlled ledger account model | Modern `LedgerAccount` preserves code, type, balance, level, status, parent, and posting flag | Legacy nature/summary/company/currency field parity needs an audit pass |
| `ACT_TM_VOUCHER` / `ACT_TD_VOUCHER` voucher header/detail posting pattern | Modern `JournalEntry` and `JournalLine` enforce balanced postings and source references | Voucher numbering, voucher type setup, branch, bank, adjusted date, and audit field parity remain later |
| `ACT_TM_SI` / `ACT_TD_SI` and receipt/refund/debit-note families | Invoice, payment, void, credit note, settlement, refund, and close flows post through balanced journals | Debit notes, purchase/supplier flows, and asset flows are not part of this MVP |
| `VUActBalance` / `spActBalance` derived ledger balances | Trial balance, ledger activity, P&L, and balance sheet derive from posted journal lines | Opening balances, branch-aware reports, adjusted-date behavior, and legacy report layouts remain open |
| Travel evidence: customers/suppliers as ledger identities | Client receivable subsidiary setup follows the controlled party-ledger direction | Supplier/payable party-ledger flow is not started |

## MVP Acceptance Checklist

- [x] Operators do not type arbitrary daily ledger codes for normal setup.
- [x] Ledger creation rejects invalid range/type/balance/posting/level combinations.
- [x] Header, Total, Master, and Control accounts cannot receive postings.
- [x] Subsidiary accounts require a Control parent.
- [x] Posting actions create balanced journal entries.
- [x] Manual journal posting respects accounting-period guards.
- [x] Trial balance, profit and loss, and balance sheet can be refreshed from posted journals.
- [x] Period close can hand net income to retained earnings through generated close journals.
- [x] Generated close journals can be opened from the close artifact.
- [x] The single-company boundary is explicit: use `MAIN` or omit `companyCode`.
- [ ] Opening balances are not implemented yet.
- [ ] Voucher type/numbering setup is not implemented yet.
- [ ] Branch-aware and adjusted-date reporting are not implemented yet.
- [ ] Legacy field-by-field COA/voucher/report parity has not been completed.
- [ ] Legacy import dry-run and guided mutating repairs are still pending.

## Current Risks

| Risk | Why it matters | Mitigation |
| --- | --- | --- |
| Dirty worktree | Many slices are present together, so regression review is harder | Stabilize with focused diff review before adding another large feature |
| Legacy parity overclaim | We are directionally aligned, not fully cloned | Keep this checkpoint as the truth boundary |
| Report trust | Reports exist, but accounting users will expect exactness | Add fixture-based report assertions before deeper UX polish |
| Data migration | Old loose accounts may exist in dev/prod-like data | Keep reconciliation read-only until dry-run and repair actions are deliberately designed |

## Next Controlled Moves

1. Review the current dirty worktree by theme: accounting GL, product catalog/runtime, docs.
2. Add fixture-backed smoke assertions for report totals and close journal drill-in data.
3. Build a legacy COA import dry-run that reads candidate rows, maps `H/T/M/D/C/S`, and reports what would be inserted or rejected.
4. Add opening-balance support only after the import/dry-run rules are clear.
5. Then consider voucher type/numbering setup, branch/currency reporting, and legacy report layout parity.

Decision: do not start a new broad accounting feature until this checkpoint is accepted as the baseline.
