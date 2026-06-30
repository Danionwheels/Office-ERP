# Legacy Form Name Map

Date started: 2026-06-30

Use this tracker whenever a legacy Access form is cloned or referenced.

Legacy names are evidence. New code, routes, screen titles, and module names should use professional domain-first names.

## Naming Rules

| Target | Convention | Example |
| --- | --- | --- |
| Backend domain/application type | PascalCase, domain noun first | `SurveyJob`, `ValuationInvoiceBatch` |
| Frontend module folder | kebab-case module name | `survey-valuation` |
| Frontend page/component | PascalCase | `SurveyJobEntryPage`, `ValuationInvoiceBatchCreatorPage` |
| API route | kebab-case plural resource | `/api/survey-jobs`, `/api/valuation-invoices` |
| User-facing screen title | Clear business language | `Survey Job Entry`, `Valuation Invoice Batch` |
| Legacy reference | Original Access name in tracker/spec only | `Docket`, `INV_GEN` |

Avoid:

- carrying old abbreviations into new code, such as `ACTAS`, `ACTMS`, `INV_GEN`, `CL_SR`
- one-letter prefixes copied from Access
- table-driven names as UI names
- misspellings from the legacy app unless required for imported data compatibility

## Status Values

- Proposed
- Accepted
- In Progress
- Implemented
- Verified
- Deferred

## Survey/Valuation First Batch

| Legacy form | Legacy caption | Current screen name | Current code name | Module | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Docket` | DOCKET ENTRY | Survey Job Entry | `SurveyJobEntry` | SurveyValuation | Accepted | Main job/docket entry screen |
| `Docket_A` | Invoice Preparation | Survey Job Invoice Preparation Lines | `SurveyJobInvoiceLines` | SurveyValuation | Accepted | Existing code name; preparation detail grid, not final invoice creation |
| `INV_GEN` | Valuation Invoice | Valuation Invoice Batch Creator | `ValuationInvoiceBatchCreator` | SurveyValuation/Billing | Accepted | Select dockets and create valuation invoice |
| `INV_EDIT` | blank/utility | Valuation Invoice Metadata Editor | `ValuationInvoiceMetadataEditor` | SurveyValuation/Billing | Accepted | Update invoice descriptions/contact/billing branch |
| `Inv_Del` | Valuation Inv Deletion | Valuation Invoice Cancellation | `ValuationInvoiceCancellation` | SurveyValuation/Billing | Proposed | Delete/cancel invoice workflow |
| `Inv_Desc` | INVOICE DESCRIPTION | Invoice Description Setup | `InvoiceDescriptionSetup` | SurveyValuation/Setup | Proposed | Setup for invoice description A/B/C/manual text |
| `Inv_GL_Setup` | Invoice GL Setup | Invoice Posting Setup | `InvoicePostingSetup` | SurveyValuation/Accounting | Proposed | Maps service/invoice heads to GL |
| `CL_SR_AGING` | Client Service Wise Aging | Client Service Aging Report | `ClientServiceAgingReport` | Reporting | Proposed | Date range and sales option filter |
| `DSR` | Daily Sale Report | Daily Sales Report | `DailySalesReport` | Reporting | Proposed | Daily report/filter screen |
| `SUR_RPT` | Job Status | Survey Job Status Report | `SurveyJobStatusReport` | Reporting | Proposed | Main survey report filter |
| `SUR_RPT_A` | Parts to be Repaired | Repair Parts Report Detail | `RepairPartsReportDetail` | Reporting | Proposed | Report/detail subform |
| `SUR_RPT_B` | Parts to be Replaced | Replacement Parts Report Detail | `ReplacementPartsReportDetail` | Reporting | Proposed | Report/detail subform |
| `SUR_RPT_C` | ANNEXURES | Survey Annexure Report Detail | `SurveyAnnexureReportDetail` | Reporting | Proposed | Report/detail subform |
| `PM_INV` | Survey Invoice | Survey Invoice Print | `SurveyInvoicePrint` | Reporting/Billing | Proposed | Invoice print/filter screen |
| `PM_RPT` | Survey Report | Survey Report Print | `SurveyReportPrint` | Reporting | Proposed | Survey report print/filter screen |
| `DocketQ` | Docket Query | Survey Job Search | `SurveyJobSearch` | SurveyValuation | Proposed | Query/search screen |

## Setup/Master First Batch

| Legacy form | Legacy caption | Current screen name | Current code name | Module | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Branches` | Branches | Company Branch Setup | `CompanyBranchSetup` | Organization | Proposed | Internal branch master |
| `CL_Branches` | Insurance Branches | Client Branch Setup | `ClientBranchSetup` | Clients/SurveyValuation | Proposed | Client/insurance branch master |
| `CL_CAT` | Client Category | Client Category Setup | `ClientCategorySetup` | Clients | Proposed | Client grouping/category |
| `Valuators` | Surveyors | Surveyor Setup | `SurveyorSetup` | SurveyValuation/Setup | Proposed | Legacy uses valuator/surveyor |
| `Supervisers` | Supervisors | Supervisor Setup | `SupervisorSetup` | SurveyValuation/Setup | Proposed | Correct spelling in new code |
| `Ser_Type` | Survey Types | Survey Type Setup | `SurveyTypeSetup` | SurveyValuation/Setup | Proposed | Service/survey type setup |
| `SER_ITEM` | Claim Type | Claim Type Setup | `ClaimTypeSetup` | SurveyValuation/Setup | Proposed | Claim/service item setup |
| `Ser_Category` | Service Category | Service Category Setup | `ServiceCategorySetup` | SurveyValuation/Setup | Proposed | Service category setup |
| `Payment_Type` | Payment Type Opening | Payment Method Setup | `PaymentMethodSetup` | Payments/Setup | Proposed | Modernize naming to payment method |
| `Document_Type` | Document Type Opening | Document Type Setup | `DocumentTypeSetup` | SurveyValuation/Setup | Proposed | Document reference setup |
| `Wshop` | Workshop Opening | Workshop Setup | `WorkshopSetup` | SurveyValuation/Setup | Proposed | Workshop master |
| `Parts` | Parts Opening | Part Setup | `PartSetup` | SurveyValuation/Setup | Proposed | Part master |
| `Parts_Status` | Parts Status Opening | Part Status Setup | `PartStatusSetup` | SurveyValuation/Setup | Proposed | Part status master |
| `Agency` | Agency Opening | Agency Setup | `AgencySetup` | SurveyValuation/Setup | Proposed | Agency master |
| `Request_by` | Request by Opening | Request Source Setup | `RequestSourceSetup` | SurveyValuation/Setup | Proposed | Intimation/request source |
| `Designation` | Valuators | Designation Setup | `DesignationSetup` | Organization/Setup | Proposed | Shared designation setup |
| `TAX` | Tax Opening | Tax Setup | `TaxSetup` | Accounting/Setup | Proposed | Tax rates/sections |
| `Inv_Sign` | Invoice Signatures | Invoice Signature Setup | `InvoiceSignatureSetup` | Billing/Setup | Proposed | Invoice signature text/people |

## Accounting Families

These need deeper classification before individual names are finalized.

| Legacy family | Current family name | Module | Status | Notes |
| --- | --- | --- | --- | --- |
| `ACTAS*` | Accounting Transaction Screens | Accounting | Proposed | Voucher, purchase/sale invoice, receipt, debit/credit transaction screens |
| `ACTMS*` | Accounting Master Setup Screens | Accounting/Setup | Proposed | COA, customer, supplier, item, location, area, sales/marketing setup |
| `ACTAR*` | Accounting Report Filters | Reporting/Accounting | Proposed | Report filters and report previews |
| `ACTAL*` | Ledger Report Filters | Reporting/Accounting | Proposed | Ledger/report filter screens |
| `MYK*` | Identity and System Setup Screens | IdentityAccess/PlatformSettings | Proposed | Replace security logic, preserve permission evidence |

## Accounting/Billing Objects

| Legacy object | Current name | Module | Status | Notes |
| --- | --- | --- | --- | --- |
| `ACT_SD_COA_LEVEL3` | Ledger Account / Chart Of Accounts | Accounting | Accepted | Core GL account source |
| `ACT_TM_VOUCHER` | Journal Entry | Accounting | Accepted | Voucher header |
| `ACT_TD_VOUCHER` | Journal Line | Accounting | Accepted | Debit/credit voucher lines |
| `ACT_TM_SI` | Billing Invoice | Billing | Accepted | Sales invoice header |
| `ACT_TD_SI` | Billing Invoice Line | Billing | Accepted | Sales invoice line/detail |
| `ACT_TM_SR` | Receipt | Payments | Accepted | Sales receipt header |
| `ACT_TD_SR` | Receipt Allocation | Payments | Accepted | Receipt detail/allocation evidence |
| `ACT_SO_ITEM` | Charge Code | Billing | Accepted | Billable item/service setup |
| `Inv_GL_Setup` | Invoice Posting Profile | Accounting/Billing | Accepted | Maps invoice heads to GL and tax |

## Open Naming Questions

| Question | Default decision | Status |
| --- | --- | --- |
| Should the user-facing term stay "Docket"? | Use "Survey Job" as the screen title, keep "Docket No" as a field label because operators may know that term | Accepted |
| Should "Valuator" remain in code? | Use "Surveyor" in UI/code, record legacy `Valuators` as source table/form | Accepted |
| Should "Insurance Branches" become "Client Branches"? | Use "Client Branch" unless business users specifically prefer insurance branch | Proposed |
| Should Access abbreviations appear in routes/code? | No, only in legacy reference fields and migration docs | Accepted |
