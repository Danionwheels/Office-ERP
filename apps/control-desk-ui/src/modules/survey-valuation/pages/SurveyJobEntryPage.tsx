import { AlertCircle, CheckCircle2 } from "lucide-react";
import { SurveyJobEntryDetails } from "../components/SurveyJobEntryDetails";
import { SurveyJobEntryForm } from "../components/SurveyJobEntryForm";
import { SurveyJobEntryToolbar } from "../components/SurveyJobEntryToolbar";
import { useSurveyJobEntry } from "../hooks/useSurveyJobEntry";

export function SurveyJobEntryPage() {
  const surveyJobEntry = useSurveyJobEntry();

  return (
    <div className="entry-workspace">
      <SurveyJobEntryToolbar
        surveyJobId={surveyJobEntry.surveyJobId}
        surveyJobNumber={surveyJobEntry.surveyJobNumber}
        jobNumberSearch={surveyJobEntry.jobNumberSearch}
        status={surveyJobEntry.status}
        isBusy={surveyJobEntry.isBusy}
        onSurveyJobNumberChange={surveyJobEntry.setSurveyJobNumber}
        onJobNumberSearchChange={surveyJobEntry.setJobNumberSearch}
        onStatusChange={surveyJobEntry.setStatus}
        onNew={surveyJobEntry.reset}
        onSearch={surveyJobEntry.loadByNumber}
        onSave={surveyJobEntry.save}
      />

      <div className="status-line" aria-live="polite">
        {surveyJobEntry.error !== "" && (
          <span className="status-error">
            <AlertCircle size={16} />
            {surveyJobEntry.error}
          </span>
        )}
        {surveyJobEntry.message !== "" && (
          <span className="status-success">
            <CheckCircle2 size={16} />
            {surveyJobEntry.message}
          </span>
        )}
      </div>

      <SurveyJobEntryForm
        fields={surveyJobEntry.fields}
        isBusy={surveyJobEntry.isBusy}
        onFieldChange={surveyJobEntry.setField}
      />

      <SurveyJobEntryDetails
        entry={surveyJobEntry.entry}
        documents={surveyJobEntry.documents}
        invoiceLines={surveyJobEntry.invoiceLines}
        billingDraftFields={surveyJobEntry.billingDraftFields}
        clientOptions={surveyJobEntry.clientOptions}
        chargeCodeOptions={surveyJobEntry.chargeCodeOptions}
        lastBillingDraft={surveyJobEntry.lastBillingDraft}
        isBusy={surveyJobEntry.isBusy}
        canSaveDocuments={surveyJobEntry.surveyJobId !== ""}
        canSaveInvoiceLines={surveyJobEntry.surveyJobId !== ""}
        canCreateBillingDraft={surveyJobEntry.surveyJobId !== ""}
        onDocumentChange={surveyJobEntry.setDocument}
        onSaveDocuments={surveyJobEntry.saveDocuments}
        onInvoiceLineChange={surveyJobEntry.setInvoiceLine}
        onAddInvoiceLine={surveyJobEntry.addInvoiceLine}
        onRemoveInvoiceLine={surveyJobEntry.removeInvoiceLine}
        onSaveInvoiceLines={surveyJobEntry.saveInvoiceLines}
        onBillingDraftFieldChange={surveyJobEntry.setBillingDraftField}
        onCreateBillingDraft={surveyJobEntry.createBillingDraft}
        onRefreshLookups={surveyJobEntry.loadLookups}
      />
    </div>
  );
}
