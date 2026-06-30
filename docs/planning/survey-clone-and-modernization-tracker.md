# Survey Clone And Modernization Tracker

Date started: 2026-06-30

Status: Paused / reference only as of 2026-06-30.

Decision: Survey/FAS clone work is not part of the active SafarSuite Control Desk goal. Keep this document as legacy research, but do not use it to select next implementation work. Active work now follows `docs/planning/client-billing-cloud-chain.md`.

## Goal

Clone the useful business behavior from the Survey/FAS legacy software into the new SafarSuite Control Desk desktop app, then add the new provider-control requirements for SafarSuite clients, portal billing, online payments, activation, devices, modules, and renewals.

This tracker covers both:

```text
Legacy clone work
  forms, tables, queries, reports, modules, accounting behavior

New provider-control work
  client pricing, portal publishing, payments, entitlements, device/module limits
```

## Product Target

```text
SafarSuite Control Desk
  desktop app for our office
  .NET backend/application layer
  React TypeScript UI during development
  Tauri desktop wrapper for production
  PostgreSQL local database
  syncs selected commercial/control data to SafarSuite Control Cloud
```

## Source Evidence

| Evidence | Location | Status |
| --- | --- | --- |
| Survey app sweep | `survey-software-sweep.md` | Captured |
| Access export | `work/survey-access-export/` | Captured in current workspace |
| Survey SQL script | `E:/travel tour/survey/Survey.sql` | Sampled/inventoried |
| Access app | `E:/travel tour/survey/Actappl7.mdb` | Exported via working copy |
| Linked Access data files | `E:/travel tour/survey/ACTDATA7.mdb`, `VALDATA7.mdb`, `MYKDATA7.mdb`, `SCRDATA7.mdb` | Present in legacy folder |
| Linked data archive | `E:/travel tour/survey/SP.rar` | Contains the four linked MDB files |
| SQL backup | `E:/travel tour/survey/ANI_backup_2025_11_19_120002_1863486.bak` | Not restored yet |

## Workstreams

| Workstream | Purpose | Status | Next Step |
| --- | --- | --- | --- |
| Legacy object inventory | Confirm all forms, tables, queries, reports, modules | Started | Fill `survey-object-clone-register.md` by domain |
| Schema migration | Design modern PostgreSQL schema from legacy tables | Not started | Classify tables as clone, transform, archive, ignore |
| Form cloning | Recreate high-value Access forms in modern UI | Started | Start with `Docket` + `Docket_A` clone spec |
| Report parity | Match key old reports before replacing Access | Not started | Pick first 5-7 reports |
| Accounting foundation | Preserve GL, voucher, invoice, receipt, asset meaning | Started | Build basic COA, journal, invoice posting, and receipt allocation model |
| Provider client control | Add SafarSuite client pricing/contracts/renewals | Started conceptually | Extend pricing with dynamic charge rules before invoice generation |
| Portal/cloud linkage | Publish invoices and entitlements to SafarSuite Control Cloud | Not started | Define cloud API contracts |
| Payment integration | Card and online bank transfer flow | Not started | Define payment provider adapter and manual bank review |
| Activation/device limits | Control devices, modules, branches, expiry/grace | Started conceptually | Reuse CloudServer activation/product-kernel concepts |
| Desktop packaging | Ship as desktop app, not office web app | Not started | Decide Tauri package shape after first UI slice |

## Decision Values

Use these values in the register:

| Decision | Meaning |
| --- | --- |
| Clone | Rebuild behavior very close to old app |
| Modernize | Preserve business meaning but improve UX/schema/rules |
| Replace | Use a new workflow because old design is not suitable |
| Archive | Import/preserve only for history or reporting evidence |
| Ignore | Do not carry forward |
| New | New requirement not present in Survey |

## Status Values

| Status | Meaning |
| --- | --- |
| Unreviewed | Object/requirement exists but has not been studied |
| Mapped | We know its purpose and destination module |
| Designed | New workflow/schema/API shape is documented |
| Implemented | Built in the new project |
| Verified | Compared to old behavior or tested against acceptance criteria |
| Deferred | Intentionally moved to a later phase |

## Phase Plan

### Phase 0: Control Docs

Outcome:

- approach tracker exists
- object clone register exists
- new requirements file exists
- legacy evidence location is known

Status: started.

### Phase 1: Read The Old System Properly

Outcome:

- classify all 111 Access tables
- classify all 96 SQL script tables
- classify all 178 forms
- classify all 183 reports
- identify top workflows
- identify dead/temp/report-only objects

Deliverable:

- completed `survey-object-clone-register.md`
- first workflow map

### Phase 2: Design The New Internal ERP

Outcome:

- modern PostgreSQL schema
- modules and boundaries
- desktop app navigation
- user/role model
- GL/accounting foundation
- provider-client commercial model

Deliverable:

- architecture and schema notes
- first migration scripts

### Phase 3: First Vertical Slice

Outcome:

One complete provider-control flow works:

```text
create SafarSuite client
set custom pricing
set allowed modules/devices/branches
generate invoice
publish to portal/cloud
record payment
issue entitlement/product-kernel command
show renewed status
```

This phase now includes a minimal accounting foundation, because dynamic client billing needs ledger-ready invoice and receipt behavior from the start.

### Phase 4: Legacy Accounting And Office Management

Outcome:

- GL
- vouchers
- receipts/payments
- expenses
- assets
- client ledgers
- office reports

### Phase 5: Survey/FAS Workflow Parity

Outcome:

- selected old forms recreated
- selected old reports reconciled
- old data import path tested

## First Objects To Prioritize

| Area | Legacy Objects | Why First |
| --- | --- | --- |
| Company/setup | `MYK_XO_COMPANY`, `SETUP`, `SM_APPLICATION`, `SD_APPLICATION` | Needed to understand old configuration and relinking |
| Security | `MYK_XO_USERS`, `MYK_XM_SECURITY`, `MYK_XD_SECURITY` | Needed to map roles, but passwords/security logic should be replaced |
| Customers | `ACT_SO_CUSTOMER`, `SCR_SO_CUSTOMER`, customer city/country/zone tables | Needed for provider client and office accounting |
| COA/GL | `ACT_SD_COA_LEVEL3`, `ACT_TM_VOUCHER`, `ACT_TD_VOUCHER` | Core accounting foundation |
| Invoices/receipts | `ACT_TM_SI`, `ACT_TD_SI`, `ACT_TM_SR`, `ACT_TD_SR` | Needed for billing/receivable flows |
| Assets | `ACT_SM_ASSET`, `ACT_SD_ASSET` | Office asset tracking |
| Survey jobs | `Docket`, `Docket_A/B/C/D` | Key old operational workflow |
| Valuation invoices | `VAL_INV_M`, `VAL_INV_D` | Survey billing evidence |
| Reports | `ACTAL*`, `ACTAR*`, `Inv_*`, `Sur_Rpt*`, `CL_SR_AGING*` | Need parity decisions early |

## Open Questions

| Question | Owner | Status |
| --- | --- | --- |
| Which Survey forms are used daily today? | Daniyal/business users | Open |
| Does the new office ERP need to import all historical Survey transactions or only opening/current balances? | Daniyal | Open |
| Should SafarSuite Control Desk generate activation files directly, or always ask SafarSuite Control Cloud to sign them? | Architecture | Open |
| Which Pakistani payment provider will be used first? | Business/finance | Open |
| Will bank transfer confirmation be manual review first or bank API integration? | Business/finance | Open |
| What are the first three SafarSuite plans/packages? | Product owner | Open |
| How strict should device/branch overuse enforcement be during grace period? | Product owner | Open |

## Current Recommendation

Start with the new provider-control vertical slice plus a minimal Accounting/Billing foundation before cloning every legacy form.

Reason:

The old Survey app teaches us that invoice creation depends on COA, vouchers, receipts, posting setup, and aging behavior. SafarSuite Control Desk needs that foundation to charge different clients with dynamic charges, then publish and reconcile invoices cleanly.
