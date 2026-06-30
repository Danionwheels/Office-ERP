# Form Clone Spec: Survey Job Entry

Date started: 2026-06-30

Status: Paused / reference only as of 2026-06-30.

Decision: Survey Job Entry is no longer part of the active product path. Keep this spec for legacy evidence only.

## Name Mapping

| Legacy | Current |
| --- | --- |
| Legacy form | `Docket` |
| Legacy caption | `DOCKET ENTRY` |
| Current screen name | Survey Job Entry |
| Current code name | `SurveyJobEntry` |
| Frontend route | `/survey-valuation/jobs/:jobId?` |
| Module | SurveyValuation |
| Primary record source | `Docket` |
| Related legacy subform | `Docket_A` |
| Related current component | `SurveyJobInvoiceLines` |

## Purpose

Clone the legacy docket/job intake and valuation workflow entry screen.

This screen is the operational center for survey/valuation work. It captures the client, insured person, survey details, vehicle, policy/loss, required documents, invoice preparation values, and workflow status.

## Layout Direction

Use a dense desktop form with grouped sections. Do not use a dashboard or marketing layout.

Recommended groups:

- Header/search strip
- Job identity
- Client and insured
- Survey administration
- Vehicle and workshop
- Policy and loss
- Required documents
- Invoice and payment
- Settlement and valuation
- Invoice preparation lines

## Legacy Field Groups

### Header/Search

| Legacy control | Legacy source | Current label | Current field name | Notes |
| --- | --- | --- | --- | --- |
| `EDIT_DOC` | row source from `Docket` | Search by Job No | `jobNumberSearch` | Legacy searches docket/service combined |
| `EDT_REG` | row source from `Docket.Veh_Reg` | Search by Registration No | `vehicleRegistrationSearch` | Search helper |
| `EDIT_CHASIS` | row source from `Docket.Veh_Chassis_No` | Search by Chassis No | `vehicleChassisSearch` | Correct spelling in current name |
| `EDIT_ENGINE` | row source from `Docket.Veh_Engine_No` | Search by Engine No | `vehicleEngineSearch` | Search helper |
| `EDIT_LOSS` | row source from `Docket.Loss_No` | Search by Loss No | `lossNumberSearch` | Search helper |
| `FINV` | row source from invoice fields | Search by Invoice No | `invoiceNumberSearch` | Search helper |

### Job Identity

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Doc_No` | Docket No | `surveyJobNumber` |
| `Ser_Code` / `Ser_Code_desc` | Survey Type | `surveyTypeCode` |
| `Branch` | Branch / Pay Mode | `companyBranchCode` |
| `CL_BR` | Client Branch | `clientBranchCode` |
| `BB` / `Billing_Br` | Billing Branch | `billingBranchCode` |
| `Job_Status` | Job Status | `jobStatus` |
| `ReInspection` | Re-Inspection | `isReInspection` |

Keep "Docket No" as the visible field label for now because legacy operators may recognize it, but use `surveyJobNumber` / `SurveyJobNumber` in code.

### Dates

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Intimation_Date` | Intimation Date | `intimationDate` |
| `Delv_DT` | Delivered Date | `deliveredDate` |
| `ReInspection_Date` | Re-Inspection Date | `reInspectionDate` |
| `Inv_DT` | Invoice Date | `invoiceDate` |
| `VOUCHER_DATE` | Voucher Date | `voucherDate` |
| `Discount_Date` | Discount Date | `discountDate` |
| `PO_DATE` | P.O. Date | `purchaseOrderDate` |

### Client And Insured

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Client` | Insurance Company | `clientCode` |
| `Sub_Client` | Insured Name | `insuredName` |
| `Sub_Client_Tel` | Phone | `insuredPhone` |
| `Sub_Client_Email` | Email | `insuredEmail` |
| `Address` | Area/Address | `insuredAddress` |
| `NIC` | CNIC | `insuredCnic` |
| `Cont_Person` | Contact Person | `contactPerson` |
| `desg_person` | Designation | `contactDesignationCode` |
| `Bnk_Ref_No` / `Ref_No` | Reference No | `referenceNumber` |
| `CC_NO` | CC No | `ccNumber` |

### Survey Administration

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Surveyor` | Surveyor | `surveyorCode` |
| `Sup_Code` | Supervisor | `supervisorCode` |
| `Claim_Type` | Claim Type | `claimTypeCode` |
| `Intimation_Name` | Intimation By | `requestSourceCode` |
| `AREA` | Area | `areaCode` |
| `Agency` | Agency | `agencyCode` |

### Vehicle And Workshop

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Veh_Make` | Make | `vehicleMake` |
| `Veh_Reg` | Registration No | `vehicleRegistrationNumber` |
| `Veh_Chassis_No` | Chassis No | `vehicleChassisNumber` |
| `Veh_Model` | Model | `vehicleModel` |
| `Veh_Engine_No` | Engine No | `vehicleEngineNumber` |
| `Wshop_Name` / `Wshop_Name1` | Workshop | `workshopCode` |

### Policy And Loss

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Loss_No` | Loss No | `lossNumber` |
| `Policy_No` | Policy No | `policyNumber` |
| `PO_NO` | P.O. No | `purchaseOrderNumber` |
| `Claim_Type` | Claim Type | `claimTypeCode` |

### Required Documents

Use a repeatable document checklist internally instead of hard-coding each flag into unrelated UI logic.

| Legacy flag | Legacy date | Current document name |
| --- | --- | --- |
| `Doc_CF` | `Doc_CF_DT` | Claim Form |
| `Doc_RB` | `Doc_RB_DT` | Registration Book |
| `Doc_DL` | `Doc_DL_DT` | Driving License |
| `Doc_IP` | `Doc_IP_DT` | Insurance Policy |
| `Doc_NIC` | `Doc_NIC_DT` | CNIC |
| `Doc_P_Report` | `Doc_P_Report_DT` | Police Report |
| `Doc_FIR` | `Doc_FIR_DT` | FIR |
| `Doc_DS` | `Doc_DS_DT` | Discharge Sheet |
| `Doc_Final_FIR` | `Doc_Final_FIR_DT` | Final FIR |
| `Doc_PR` | `Doc_PR_DT` | Purchase Receipt |
| `Doc_TR` | `Doc_TR_DT` | Tax Paid Receipt |
| `Doc_OS` | `Doc_OS_DT` | Owner Status |
| `Doc_TL` | `Doc_TL_DT` | Transfer Letter |
| `Doc_VK` | `Doc_VK_DT` | Vehicle Keys |
| `Doc_OC` | `Doc_OC_DT` | Owner Certificate |

Legacy values include `Y/Yes`, `N/No`, and for transfer letter also `R/N-R`. Model this as a document status, not a raw string, after confirming business usage.

### Invoice And Payment

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Inv_No` | Invoice No | `invoiceNumber` |
| `Inv_DT` | Invoice Date | `invoiceDate` |
| `Inv_Amt` | Invoice Net | `invoiceNetAmount` |
| `Gross_Amt` | Invoice Gross | `invoiceGrossAmount` |
| `Discount` | Discount | `discountAmount` |
| `GST_P` | Sales Tax % | `salesTaxPercent` |
| `GST_V` | Sales Tax Amount | `salesTaxAmount` |
| `Pay_Mode` | Payment Mode | `paymentMode` |
| `Payment_Wshop` | Payment Make | `paymentWorkshopAmount` |
| `VOUCHER_NO` | Voucher No | `voucherNumber` |
| `VOUCHER_DATE` | Voucher Date | `voucherDate` |
| `Journal` | Journal | `journalCode` |
| `Discount_Journal` | Discount Journal | `discountJournalCode` |

### Settlement And Valuation

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Loss_Amount` | Loss Amount | `lossAmount` |
| `Settled_Labor` | Settled Labor | `settledLaborAmount` |
| `Approved_Parts` | Approved Parts | `approvedPartsAmount` |
| `Policy_Deductable` | Policy Deductible | `policyDeductibleAmount` |
| `Labor_Bill_Date` | Labor Bill Date | `laborBillDate` |
| `Labor_Bill_Value` | Labor Bill Value | `laborBillAmount` |
| `Parts_Bill_Date` | Parts Bill Date | `partsBillDate` |
| `Parts_Bill_Value` | Parts Bill Value | `partsBillAmount` |
| `Dep_Percent` | Depreciation % | `depreciationPercent` |
| calculated `Dep_Val` | Depreciation Amount | `depreciationAmount` |
| `Salvg_Percent` | Salvage % | `salvagePercent` |
| calculated `Salvg_Value` | Salvage Amount | `salvageAmount` |

## Related Detail Grid: Survey Job Invoice Lines

Accounting correction:

This grid is only the survey invoice preparation detail from `Docket_A`. It should not become the full invoice creation engine. Final invoice creation must go through the shared Billing/Accounting model described in `docs/planning/accounting-gl-foundation-sweep.md`.

Legacy form:

```text
Docket_A
```

Current code name:

```text
SurveyJobInvoiceLines
```

Record source:

```text
Docket_A
```

Fields:

| Legacy control/source | Current label | Current field name |
| --- | --- | --- |
| `Seq_No` | S/No | `sequenceNumber` |
| `Desc_Type` | Description Type | `descriptionType` |
| `Desc` | Description | `description` |
| `Amount` | Amount | `amount` |
| `GL_Code` | Billing Head / GL | `billingHeadCode` |
| `Tax` | Sales Tax | `taxCode` |
| `CAT` | Category | `categoryCode` |
| calculated `LC_Amount` | Total | `totalAmount` |

## Lookup Sources To Recreate

| Current field | Legacy source |
| --- | --- |
| `clientCode` | `ACT_SO_CUSTOMER` |
| `clientBranchCode` / `billingBranchCode` | `CL_Branches` |
| `surveyTypeCode` | `Ser_Type` |
| `claimTypeCode` | `Ser_Item` |
| `serviceCategoryCode` | `Ser_Category` |
| `surveyorCode` | `Valuators` |
| `supervisorCode` | `Supervisers` |
| `requestSourceCode` | `Request_by` |
| `workshopCode` | `Wshop` |
| `agencyCode` | `Agency` |
| `companyBranchCode` | `Branches` |
| `contactDesignationCode` | `Designation` |
| `billingHeadCode` | `Inv_GL_Setup` |
| `taxCode` | `Tax` |

## Actions To Clone

| Legacy button/action | Current action name | Notes |
| --- | --- | --- |
| `Gen_Inv` / Create Master Invoice | `createMasterInvoice` | Opens/starts valuation invoice workflow |
| `Add_Inv` / Prepare | `prepareInvoiceLines` | Save prepared detail lines through `/invoice-lines` |
| `Add_Detail` / Generate | `generateInvoiceLines` | Generate detail values |
| `Print_Inv` / Print | `printInvoice` | Later reporting integration |
| `Del_Inv` / Delete | `cancelInvoice` | Prefer cancellation workflow over hard delete |
| form save | `saveSurveyJob` | Modern Save command |

## Current Open Questions

| Question | Default for now |
| --- | --- |
| Should "Docket No" remain visible? | Yes, visible label stays `Docket No`; code uses `surveyJob` / `surveyJobNumber` |
| Should invoice delete be hard delete? | No, model as cancellation unless legacy data proves otherwise |
| Should all document flags become a repeatable checklist? | Yes, unless exact legacy columns are needed for import parity |
| Should `Valuators` become `Surveyors` everywhere? | Yes in code/UI; legacy name remains in import/source mapping |
| Should `Supervisers` spelling be preserved? | No, use `Supervisors` in current names |

## Acceptance Checklist

- Current names match `legacy-form-name-map.md`
- Field groups match the legacy form's operator workflow
- All important legacy lookup sources are represented
- `SurveyJobInvoiceLines` is available as a detail grid inside Survey Job Entry
- Old Access names appear only in specs/import/mapping docs
- No new code uses `Docket` as a type/component/page name except in migration/source context

## Implementation Progress

| Slice | Status | Notes |
| --- | --- | --- |
| Domain aggregate | Done | `SurveyJob` models the main record, documents, and invoice lines |
| Create main job | Done | `CreateSurveyJob` covers first-save fields from the main form |
| Load entry screen | Done | `GetSurveyJobEntry` returns the screen-shaped DTO by id or job number |
| Update main job | Done | `UpdateSurveyJob` edits the main form sections and keeps detail workflows separate |
| API endpoints | Done | Create/load/update routes exposed under `/api/v1/survey-valuation/jobs` |
| Temporary persistence | Done | In-memory repository supports API smoke testing until PostgreSQL mapping is added |
| React clone screen | Done | Dense grouped form created under `apps/control-desk-ui/src/modules/survey-valuation` |
| Document checklist commands | Done | Required document statuses and received dates can be saved independently |
| Invoice preparation lines | Done | Prepared lines can be saved through `UpdateSurveyJobInvoiceLines` |
| Billing draft bridge | Done | Prepared survey lines can create a shared Billing draft through `CreateSurveyJobBillingDraft`; issuing/posting stays in Billing/Accounting |
| Invoice preparation UI | Done | Survey Job Entry screen has editable preparation rows and Billing draft fields/actions |
| First lookup selectors | Done | Billing draft client and invoice-line billing head fields use API-backed selectors |
