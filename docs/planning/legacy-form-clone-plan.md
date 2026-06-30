# Legacy Form Clone Plan

Date started: 2026-06-30

Status: Paused / reference only as of 2026-06-30.

Decision: Legacy Survey/FAS form cloning is no longer the active product path. Keep this file for evidence and naming history only. Active work now follows `docs/planning/client-billing-cloud-chain.md`.

This plan captures how we will clone the legacy Survey/FAS forms from `Actappl7.mdb`.

The source form definitions are exported at:

```text
C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2/work/survey-access-export/design/forms
```

The canonical legacy application source remains:

```text
E:/travel tour/survey/Actappl7.mdb
```

## Current Understanding

The legacy app is a dense Microsoft Access desktop application. The forms are not portal-style screens. They are compact office/operator screens with:

- gray Access-style backgrounds
- dense labels, text boxes, combo boxes, option lists, and command buttons
- record-bound forms
- lookup combo boxes backed by SQL row sources
- report filter forms with Preview/Exit buttons
- transaction header/detail forms with subforms
- event-driven behavior through VBA event procedures
- survey/valuation forms mixed with accounting/FAS forms

There are 178 exported form definitions.

## Form Family Counts

| Family | Count | Meaning |
| --- | ---: | --- |
| `ACTAL*` | 15 | accounting ledger/report filter forms |
| `ACTAR*` | 44 | accounting report filter/forms |
| `ACTMS*` | 25 | accounting setup/master forms |
| `ACTAS*` | 24 | accounting transaction forms |
| named Survey/valuation forms | 16 | docket, valuation invoice, survey report, aging forms |
| named setup/master forms | 19 | branches, valuators, service setup, payment/document types |
| total exported forms | 178 | full Access form inventory |

## Clone Rule

For now, clone the core forms closely enough that an operator recognizes the workflow.

Use professional current names for new code and screens. Legacy form names must be tracked in `docs/planning/legacy-form-name-map.md` and should not become new class/component/route names.

## Ambiguity Rule

When a field, validation rule, lookup, button action, or workflow status is unclear, look back at the legacy evidence before choosing new behavior.

Use these sources in order:

1. `docs/planning/form-specs/*`
2. exported Access forms under `C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2/work/survey-access-export/design/forms`
3. `E:/travel tour/survey/Survey.sql`
4. `E:/travel tour/survey/Actappl7.mdb`

If the legacy source proves the behavior, record it in the relevant clone spec. If it does not prove the behavior, choose the least restrictive modern default and mark the question for follow-up.

Clone:

- screen purpose
- field groups
- captions/labels where useful
- record source meaning
- combo box choices/lookups
- command buttons and workflow actions
- subform relationships
- report/filter parameters

Modernize carefully:

- layout responsiveness
- validation messages
- keyboard navigation where it helps
- role/security implementation
- storage/persistence implementation

Do not clone blindly:

- Access relinking/startup mechanics
- old password storage
- old global-state hacks unless they represent real workflow state
- printer blobs and Access-specific layout noise
- exact twip-based coordinates when a structured desktop layout is clearer

## First Clone Set

Start with the Survey/valuation workflow because it is concrete, important, and strongly represented in the exported forms.

### 1. Supporting Setup Forms

These are needed to make Survey Job Entry and invoice workflows usable:

| Legacy form | Caption | Record source | Current screen name |
| --- | --- | --- | --- |
| `Branches` | Branches | `Branches` | Company Branch Setup |
| `CL_Branches` | Insurance Branches | `CL_Branches` | Client Branch Setup |
| `CL_CAT` | Client Category | `Cl_Category` | Client Category Setup |
| `Valuators` | Surveyors | `Valuators` | Surveyor Setup |
| `Supervisers` | Supervisors | `Supervisers` | Supervisor Setup |
| `Ser_Type` | Survey Types | `Ser_Type` | Survey Type Setup |
| `SER_ITEM` | Claim Type | `Ser_Item` | Claim Type Setup |
| `Ser_Category` | Service Category | `Ser_Category` | Service Category Setup |
| `Payment_Type` | Payment Type Opening | `Payment_Type` | Payment Method Setup |
| `Document_Type` | Document Type Opening | `Document_Type` | Document Type Setup |
| `Wshop` | Workshop Opening | `Wshop` | Workshop Setup |
| `Parts` | Parts Opening | `Parts` | Part Setup |
| `Parts_Status` | Parts Status Opening | `Parts_Status` | Part Status Setup |
| `Agency` | Agency Opening | `Agency` | Agency Setup |
| `Request_by` | Request by Opening | `Request_by` | Request Source Setup |
| `Designation` | Valuators | `Designation` | Designation Setup |

### 2. Survey Job Entry

Legacy form:

```text
Docket
```

Caption:

```text
DOCKET ENTRY
```

Record source:

```text
Docket
```

Current screen name:

```text
Survey Job Entry
```

Current code name:

```text
SurveyJobEntry
```

Key groups to clone:

- docket identity: docket no, service code, branch
- dates: intimation, delivery, invoice, re-inspection, voucher dates
- client/insured: insurance company, insured name, branch, contact person, phone, email, address, CNIC
- survey/admin: surveyor, supervisor, job status, survey type, claim type, request/intimation by
- vehicle: make, registration no, chassis no, model, engine no, workshop
- policy/loss: loss no, policy no, claim form/reference fields
- documents: claim form, registration book, driving license, insurance policy, CNIC, FIR, discharge sheet, tax receipt, transfer letter, vehicle keys, owner certificate, and their dates
- invoice/payment: invoice no/date, gross/net/loss amount, discount, sales tax, payment mode, agency, billing branch
- valuation settlement: settled labor, approved parts, policy deductible, labor/parts bill values, depreciation, salvage
- search helpers: search by docket no, registration no, chassis no, engine no, loss no, invoice no

Important embedded/related form:

```text
Docket_A
```

Caption:

```text
Invoice Preparation
```

Current component name:

```text
SurveyJobInvoiceLines
```

Purpose:

- detail lines for valuation invoice preparation
- fields include sequence, description type, description, amount, billing head/GL, sales tax, category, and total

Correction:

This is not the complete invoice creation module. Treat it as preparation detail. Valuation invoice generation is handled by `INV_GEN` / `VAL_INV_M` / `VAL_INV_D`, and accounting posting is handled through the `ACT_*` invoice, receipt, voucher, COA, and setup objects.

Important buttons/actions:

- Create Master Invoice
- Prepare
- Generate
- Print
- Delete

### 3. Valuation Invoice Generation

Legacy form:

```text
INV_GEN
```

Caption:

```text
Valuation Invoice
```

Current screen name:

```text
Valuation Invoice Batch Creator
```

Workflow:

- choose filters
- show available dockets
- move dockets from "Dockets To Select List" to "Selected Dockets To Be Billed"
- set invoice date/no and invoice descriptions
- make valuation invoice

Key fields/actions:

- from date / to date
- invoice date
- document quantity
- client/bank
- job status
- service type
- payment mode
- supervisor
- service category
- contact person
- designation
- invoice descriptions A/B/C
- billing branch
- Show
- Make
- forward/back one or all selected dockets

### 4. Valuation Invoice Edit

Legacy form:

```text
INV_EDIT
```

Current screen name:

```text
Valuation Invoice Metadata Editor
```

Purpose:

- utility screen to update invoice-related metadata after creation

Key fields/actions:

- edit type
- selected invoice
- service category
- contact person
- designation
- invoice description A/B/C
- manual invoice description
- billing branch
- Update Invoice

### 5. First Report/Filter Forms

Clone these after the entry/invoice forms:

| Legacy form | Caption | Purpose |
| --- | --- | --- |
| `CL_SR_AGING` | Client Service Wise Aging | Client Service Aging Report |
| `DSR` | Daily Sale Report | Daily Sales Report |
| `SUR_RPT` | Job Status | Survey Job Status Report |
| `PM_INV` | Survey Invoice | Survey Invoice Print |
| `PM_RPT` | Survey Report | Survey Report Print |

## Clone Implementation Approach

For each form, create a clone spec before coding:

```text
legacy form name
current screen/code name
caption
record source
field groups
field list
combo/list sources
buttons/actions
subforms
validation/event behavior
modern module destination
open questions
```

Then implement in this order:

1. domain model for the form's record source
2. application commands/queries
3. persistence mapping
4. API endpoint
5. frontend module screen
6. parity checklist against exported form definition

## Frontend Clone Style

The React UI should feel like a modernized desktop clone:

- dense admin screens
- compact labels and inputs
- grouped fieldsets/tabs
- tables/subforms for detail rows
- predictable Save/New/Delete/Print actions
- keyboard-friendly forms
- minimal decorative UI

Do not create marketing-style pages, oversized hero sections, or card-heavy dashboards for these clone screens.

## Immediate Decision

The next screen work should not continue deeper invoice UI until the shared Billing/Accounting foundation is in place.

The current completed first clone spec is:

```text
SurveyJobEntry + SurveyJobInvoiceLines
```

Next recommended architecture slice:

```text
Accounting/Billing foundation
```

Reason:

Dynamic client charges, valuation invoices, receipts, receivables, and GL postings all need the same shared foundation.
