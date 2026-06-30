# SurveyValuation Domain Model

Date started: 2026-06-30

Status: Paused / reference only as of 2026-06-30.

Decision: SurveyValuation is not part of the active SafarSuite Control Desk goal. The code and notes remain as prior work, but new implementation should focus on `docs/planning/client-billing-cloud-chain.md`.

This model supports the first legacy form clone:

```text
Survey Job Entry + Survey Job Invoice Lines
```

Correction from the accounting sweep:

`SurveyJobInvoiceLine` represents prepared survey invoice detail from `Docket_A`. It is not the full invoice creation module. Actual valuation invoice generation and money posting must integrate with Billing and Accounting through `VAL_INV_M`, `VAL_INV_D`, `ACT_TM_SI`, `ACT_TD_SI`, `ACT_TM_VOUCHER`, `ACT_TD_VOUCHER`, and `Inv_GL_Setup`.

Legacy source:

```text
Docket + Docket_A
```

Legacy names are source references only. Current code uses professional names.

## Current Aggregate

| Current type | Legacy source | Purpose |
| --- | --- | --- |
| `SurveyJob` | `Docket` | Main survey/valuation job aggregate |
| `SurveyJobInvoiceLine` | `Docket_A` | Detail line for survey invoice preparation, not final accounting invoice posting |

## Supporting Value Objects

| Type | Purpose |
| --- | --- |
| `SurveyJobId` | strong id for the aggregate |
| `SurveyJobNumber` | professional code name for the visible legacy "Docket No" |
| `SurveyReferenceCode` | reference/master-data code used by lookup fields |
| `SurveyJobDates` | intimation, delivery, invoice, voucher, discount, purchase order dates |
| `InsuredParty` | insured person/company contact details |
| `SurveyAssignment` | surveyor, supervisor, claim type, request source, area, agency |
| `VehicleDetails` | make, registration, chassis, model, engine, workshop |
| `PolicyLossDetails` | loss, policy, and purchase order numbers |
| `SurveyDocumentChecklistItem` | document status/date checklist item |
| `SurveyJobInvoiceSummary` | invoice/payment values shown on the entry screen |
| `SurveyJobSettlement` | loss, labor, parts, deductible, depreciation, salvage values |

## Enums

| Type | Purpose |
| --- | --- |
| `SurveyJobStatus` | draft, received, pending, unsettled, delivered, settled, cancelled |
| `SurveyPaymentMode` | unknown, advance, single, master |
| `SurveyDocumentType` | claim form, registration book, CNIC, FIR, etc. |
| `SurveyDocumentStatus` | unknown, received, missing, not required |
| `SurveyInvoiceLineDescriptionType` | auto, manual, head1, head2, sales tax |

## Rules Captured

- `SurveyJobNumber` is required and capped at 32 characters.
- The UI may show "Docket No", but code uses `SurveyJobNumber`.
- Delivered date cannot be before intimation date.
- Invoice detail sequence numbers must be positive.
- Invoice detail descriptions are required.
- Invoice detail amounts cannot be negative.
- Only received documents can have a received date.
- Depreciation and salvage percentages must be between 0 and 100.
- Invoice lines are ordered by sequence number.
- The detail grid is part of the `SurveyJob` aggregate for the first clone.

## Application Port

`ISurveyJobRepository` exists in the Application layer with:

- `AddAsync`
- `GetByIdAsync`
- `GetByNumberAsync`
- `ExistsByNumberAsync`

Infrastructure will implement this later when persistence is added.

## Application Use Cases

| Use case | Status | Notes |
| --- | --- | --- |
| `CreateSurveyJob` | Done | Creates the first-save job row for Survey Job Entry using professional field names mapped from the legacy form spec. |
| `GetSurveyJobEntry` | Done | Loads a screen-shaped DTO by survey job id or survey job number. Requests must use one locator only. |
| `UpdateSurveyJob` | Done | Updates the main form sections: identity, dates, client/insured, assignment, vehicle, policy/loss, status, and remarks. |
| `UpdateSurveyJobDocuments` | Done | Replaces the required document checklist for the survey job. |
| `UpdateSurveyJobInvoiceLines` | Done | Replaces the `Docket_A` style invoice preparation grid for a survey job. |
| `CreateSurveyJobBillingDraft` | Done | Creates a shared Billing draft invoice from prepared survey invoice lines by mapping each billing head code to a `ChargeCode`. |

The first create command includes the job identity, dates, client/insured, survey assignment, vehicle, policy/loss, and remarks sections. Document checklist and invoice preparation lines remain separate use cases because the legacy form treats those as dense sub-workflows.

The load/update commands use `SurveyJobEntryDto` as the application shape for the API and React screen. The DTO includes document checklist and invoice line collections for display. Invoice preparation line mutation is now handled through `UpdateSurveyJobInvoiceLines`, and draft invoice creation goes through Billing via `CreateSurveyJobBillingDraft`.

## API Surface

Current Survey Job Entry endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/survey-valuation/jobs` | Create a survey job from first-save form fields |
| `GET` | `/api/v1/survey-valuation/jobs/{surveyJobId}` | Load a survey job entry by id |
| `GET` | `/api/v1/survey-valuation/jobs/by-number/{surveyJobNumber}` | Load a survey job entry by job number |
| `PUT` | `/api/v1/survey-valuation/jobs/{surveyJobId}` | Update the main Survey Job Entry sections |
| `PUT` | `/api/v1/survey-valuation/jobs/{surveyJobId}/documents` | Save required document statuses and received dates |
| `PUT` | `/api/v1/survey-valuation/jobs/{surveyJobId}/invoice-lines` | Save survey invoice preparation lines |
| `POST` | `/api/v1/survey-valuation/jobs/{surveyJobId}/billing-draft` | Create a shared Billing draft invoice from prepared survey lines |
| `GET` | `/api/v1/clients` | Lookup clients for billing draft selection |
| `GET` | `/api/v1/billing/charge-codes` | Lookup billing heads/charge codes for survey invoice lines |

The API uses versioned records under `SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.SurveyValuation`. Request contracts use stable strings for public enum values and the API maps them into domain enums.

## Infrastructure Status

The API currently uses `InMemorySurveyJobRepository` and `NoOpUnitOfWork` for development wiring. This is intentionally temporary and must be replaced by PostgreSQL persistence before production use.

Legacy check on first-save required fields:

- `Survey.sql` defines `Ser_Code` and `Doc_No` on the legacy job table as `NOT NULL`.
- `Sub_Client` is nullable in the legacy job table, so the first create command does not require insured name unless other insured contact details are provided.

## Deferred

- persistence mapping
- import mapping from legacy columns
- frontend controls for invoice preparation line save and Billing draft generation
- invoice batch creation
- report/print behavior
- tax-specific Billing/Accounting posting
