import { FormField } from "../../../shared/components/FormField";
import { Section } from "../../../shared/components/Section";
import type {
  SurveyJobEntryFields,
  SurveyPaymentMode
} from "../types/surveyJobEntryTypes";

type SurveyJobEntryFormProps = {
  fields: SurveyJobEntryFields;
  isBusy: boolean;
  onFieldChange: <TField extends keyof SurveyJobEntryFields>(
    name: TField,
    value: SurveyJobEntryFields[TField]
  ) => void;
};

const paymentModes: SurveyPaymentMode[] = ["Unknown", "Advance", "Single", "Master"];

export function SurveyJobEntryForm({
  fields,
  isBusy,
  onFieldChange
}: SurveyJobEntryFormProps) {
  return (
    <div className="entry-form">
      <Section title="Job Identity">
        <TextField
          label="Survey Type"
          value={fields.surveyTypeCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("surveyTypeCode", value)}
        />
        <TextField
          label="Client"
          value={fields.clientCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("clientCode", value)}
        />
        <TextField
          label="Client Branch"
          value={fields.clientBranchCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("clientBranchCode", value)}
        />
        <TextField
          label="Company Branch"
          value={fields.companyBranchCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("companyBranchCode", value)}
        />
        <TextField
          label="Billing Branch"
          value={fields.billingBranchCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("billingBranchCode", value)}
        />
        <FormField label="Payment Mode">
          <select
            value={fields.paymentMode}
            onChange={(event) =>
              onFieldChange("paymentMode", event.target.value as SurveyPaymentMode)
            }
            disabled={isBusy}
          >
            {paymentModes.map((mode) => (
              <option key={mode} value={mode}>
                {mode}
              </option>
            ))}
          </select>
        </FormField>
        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={fields.isReInspection}
            onChange={(event) => onFieldChange("isReInspection", event.target.checked)}
            disabled={isBusy}
          />
          Re-Inspection
        </label>
      </Section>

      <Section title="Dates">
        <DateField
          label="Intimation Date"
          value={fields.intimationDate}
          disabled={isBusy}
          onChange={(value) => onFieldChange("intimationDate", value)}
        />
        <DateField
          label="Delivered Date"
          value={fields.deliveredDate}
          disabled={isBusy}
          onChange={(value) => onFieldChange("deliveredDate", value)}
        />
        <DateField
          label="Re-Inspection Date"
          value={fields.reInspectionDate}
          disabled={isBusy}
          onChange={(value) => onFieldChange("reInspectionDate", value)}
        />
        <DateField
          label="Invoice Date"
          value={fields.invoiceDate}
          disabled={isBusy}
          onChange={(value) => onFieldChange("invoiceDate", value)}
        />
        <DateField
          label="Voucher Date"
          value={fields.voucherDate}
          disabled={isBusy}
          onChange={(value) => onFieldChange("voucherDate", value)}
        />
        <DateField
          label="Discount Date"
          value={fields.discountDate}
          disabled={isBusy}
          onChange={(value) => onFieldChange("discountDate", value)}
        />
        <DateField
          label="P.O. Date"
          value={fields.purchaseOrderDate}
          disabled={isBusy}
          onChange={(value) => onFieldChange("purchaseOrderDate", value)}
        />
      </Section>

      <Section title="Client And Insured">
        <TextField
          label="Insured Name"
          value={fields.insuredName}
          disabled={isBusy}
          onChange={(value) => onFieldChange("insuredName", value)}
        />
        <TextField
          label="Phone"
          value={fields.insuredPhone}
          disabled={isBusy}
          onChange={(value) => onFieldChange("insuredPhone", value)}
        />
        <TextField
          label="Email"
          value={fields.insuredEmail}
          disabled={isBusy}
          onChange={(value) => onFieldChange("insuredEmail", value)}
        />
        <TextField
          label="CNIC"
          value={fields.insuredCnic}
          disabled={isBusy}
          onChange={(value) => onFieldChange("insuredCnic", value)}
        />
        <TextField
          label="Contact Person"
          value={fields.contactPerson}
          disabled={isBusy}
          onChange={(value) => onFieldChange("contactPerson", value)}
        />
        <TextField
          label="Designation"
          value={fields.contactDesignationCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("contactDesignationCode", value)}
        />
        <TextField
          label="Reference No"
          value={fields.referenceNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("referenceNumber", value)}
        />
        <TextField
          label="CC No"
          value={fields.ccNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("ccNumber", value)}
        />
        <FormField label="Area / Address">
          <textarea
            value={fields.insuredAddress}
            onChange={(event) => onFieldChange("insuredAddress", event.target.value)}
            disabled={isBusy}
            rows={2}
          />
        </FormField>
      </Section>

      <Section title="Survey Administration">
        <TextField
          label="Surveyor"
          value={fields.surveyorCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("surveyorCode", value)}
        />
        <TextField
          label="Supervisor"
          value={fields.supervisorCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("supervisorCode", value)}
        />
        <TextField
          label="Claim Type"
          value={fields.claimTypeCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("claimTypeCode", value)}
        />
        <TextField
          label="Intimation By"
          value={fields.requestSourceCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("requestSourceCode", value)}
        />
        <TextField
          label="Area"
          value={fields.areaCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("areaCode", value)}
        />
        <TextField
          label="Agency"
          value={fields.agencyCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("agencyCode", value)}
        />
      </Section>

      <Section title="Vehicle And Workshop">
        <TextField
          label="Make"
          value={fields.vehicleMake}
          disabled={isBusy}
          onChange={(value) => onFieldChange("vehicleMake", value)}
        />
        <TextField
          label="Registration No"
          value={fields.vehicleRegistrationNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("vehicleRegistrationNumber", value)}
        />
        <TextField
          label="Chassis No"
          value={fields.vehicleChassisNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("vehicleChassisNumber", value)}
        />
        <TextField
          label="Model"
          value={fields.vehicleModel}
          disabled={isBusy}
          onChange={(value) => onFieldChange("vehicleModel", value)}
        />
        <TextField
          label="Engine No"
          value={fields.vehicleEngineNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("vehicleEngineNumber", value)}
        />
        <TextField
          label="Workshop"
          value={fields.workshopCode}
          disabled={isBusy}
          onChange={(value) => onFieldChange("workshopCode", value)}
        />
      </Section>

      <Section title="Policy And Loss">
        <TextField
          label="Loss No"
          value={fields.lossNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("lossNumber", value)}
        />
        <TextField
          label="Policy No"
          value={fields.policyNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("policyNumber", value)}
        />
        <TextField
          label="P.O. No"
          value={fields.purchaseOrderNumber}
          disabled={isBusy}
          onChange={(value) => onFieldChange("purchaseOrderNumber", value)}
        />
        <FormField label="Remarks">
          <textarea
            value={fields.remarks}
            onChange={(event) => onFieldChange("remarks", event.target.value)}
            disabled={isBusy}
            rows={2}
          />
        </FormField>
      </Section>
    </div>
  );
}

type TextFieldProps = {
  label: string;
  value: string;
  disabled: boolean;
  onChange: (value: string) => void;
};

function TextField({ label, value, disabled, onChange }: TextFieldProps) {
  return (
    <FormField label={label}>
      <input value={value} onChange={(event) => onChange(event.target.value)} disabled={disabled} />
    </FormField>
  );
}

function DateField({ label, value, disabled, onChange }: TextFieldProps) {
  return (
    <FormField label={label}>
      <input
        type="date"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
      />
    </FormField>
  );
}
