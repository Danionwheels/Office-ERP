import { FilePlus2, Plus, RefreshCcw, Save, Trash2 } from "lucide-react";
import {
  type ChargeCodeLookupOption,
  type ClientLookupOption,
  surveyInvoiceLineDescriptionTypes,
  type EditableSurveyJobInvoiceLine,
  type SurveyBillingDraftFields,
  type SurveyBillingDraftResult,
  surveyDocumentStatuses,
  surveyDocumentTypeLabels,
  type SurveyDocumentChecklistItem,
  type SurveyJobEntry
} from "../types/surveyJobEntryTypes";

type SurveyJobEntryDetailsProps = {
  entry: SurveyJobEntry | null;
  documents: SurveyDocumentChecklistItem[];
  invoiceLines: EditableSurveyJobInvoiceLine[];
  billingDraftFields: SurveyBillingDraftFields;
  clientOptions: ClientLookupOption[];
  chargeCodeOptions: ChargeCodeLookupOption[];
  lastBillingDraft: SurveyBillingDraftResult | null;
  isBusy: boolean;
  canSaveDocuments: boolean;
  canSaveInvoiceLines: boolean;
  canCreateBillingDraft: boolean;
  onDocumentChange: (
    type: SurveyDocumentChecklistItem["type"],
    changes: Partial<Pick<SurveyDocumentChecklistItem, "status" | "receivedOn">>
  ) => void;
  onSaveDocuments: () => void;
  onInvoiceLineChange: <TField extends keyof EditableSurveyJobInvoiceLine>(
    index: number,
    name: TField,
    value: EditableSurveyJobInvoiceLine[TField]
  ) => void;
  onAddInvoiceLine: () => void;
  onRemoveInvoiceLine: (index: number) => void;
  onSaveInvoiceLines: () => void;
  onBillingDraftFieldChange: <TField extends keyof SurveyBillingDraftFields>(
    name: TField,
    value: SurveyBillingDraftFields[TField]
  ) => void;
  onCreateBillingDraft: () => void;
  onRefreshLookups: () => void;
};

export function SurveyJobEntryDetails({
  entry,
  documents,
  invoiceLines,
  billingDraftFields,
  clientOptions,
  chargeCodeOptions,
  lastBillingDraft,
  isBusy,
  canSaveDocuments,
  canSaveInvoiceLines,
  canCreateBillingDraft,
  onDocumentChange,
  onSaveDocuments,
  onInvoiceLineChange,
  onAddInvoiceLine,
  onRemoveInvoiceLine,
  onSaveInvoiceLines,
  onBillingDraftFieldChange,
  onCreateBillingDraft,
  onRefreshLookups
}: SurveyJobEntryDetailsProps) {
  const total = invoiceLines.reduce((sum, line) => sum + Number(line.amount || 0), 0);

  return (
    <div className="detail-zone">
      <section className="entry-section detail-section">
        <div className="section-heading-row">
          <h2>Required Documents</h2>
          <button
            type="button"
            className="mini-button"
            onClick={onSaveDocuments}
            disabled={isBusy || !canSaveDocuments}
          >
            <Save size={14} />
            Save
          </button>
        </div>
        <table>
          <thead>
            <tr>
              <th>Document</th>
              <th>Status</th>
              <th>Received On</th>
            </tr>
          </thead>
          <tbody>
            {documents.map((document) => (
              <tr key={document.type}>
                <td>{surveyDocumentTypeLabels[document.type]}</td>
                <td>
                  <select
                    value={document.status}
                    onChange={(event) =>
                      onDocumentChange(document.type, {
                        status: event.target.value as SurveyDocumentChecklistItem["status"]
                      })
                    }
                    disabled={isBusy || !canSaveDocuments}
                  >
                    {surveyDocumentStatuses.map((status) => (
                      <option key={status} value={status}>
                        {status}
                      </option>
                    ))}
                  </select>
                </td>
                <td>
                  <input
                    type="date"
                    value={document.receivedOn ?? ""}
                    onChange={(event) =>
                      onDocumentChange(document.type, {
                        receivedOn: event.target.value === "" ? null : event.target.value
                      })
                    }
                    disabled={isBusy || !canSaveDocuments || document.status !== "Received"}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="entry-section detail-section">
        <div className="section-heading-row">
          <h2>Invoice Preparation</h2>
          <div className="section-actions">
            <span>{total.toFixed(2)} PKR</span>
            <button
              type="button"
              className="mini-button"
              onClick={onAddInvoiceLine}
              disabled={isBusy || !canSaveInvoiceLines}
            >
              <Plus size={14} />
              Line
            </button>
            <button
              type="button"
              className="mini-button"
              onClick={onSaveInvoiceLines}
              disabled={isBusy || !canSaveInvoiceLines}
            >
              <Save size={14} />
              Save
            </button>
          </div>
        </div>
        <table className="invoice-lines-table">
          <thead>
            <tr>
              <th>S/No</th>
              <th>Description Type</th>
              <th>Description</th>
              <th className="numeric">Amount</th>
              <th>Billing Head</th>
              <th>Tax</th>
              <th>Category</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {invoiceLines.length === 0 ? (
              <tr>
                <td colSpan={8}>No invoice preparation lines yet.</td>
              </tr>
            ) : (
              invoiceLines.map((line, index) => (
                <tr key={`${line.sequenceNumber}-${index}`}>
                  <td>
                    <input
                      type="number"
                      min={1}
                      value={line.sequenceNumber}
                      onChange={(event) =>
                        onInvoiceLineChange(index, "sequenceNumber", Number(event.target.value))
                      }
                      disabled={isBusy || !canSaveInvoiceLines}
                    />
                  </td>
                  <td>
                    <select
                      value={line.descriptionType}
                      onChange={(event) =>
                        onInvoiceLineChange(
                          index,
                          "descriptionType",
                          event.target.value as EditableSurveyJobInvoiceLine["descriptionType"]
                        )
                      }
                      disabled={isBusy || !canSaveInvoiceLines}
                    >
                      {surveyInvoiceLineDescriptionTypes.map((type) => (
                        <option key={type} value={type}>
                          {type}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input
                      value={line.description}
                      onChange={(event) =>
                        onInvoiceLineChange(index, "description", event.target.value)
                      }
                      disabled={isBusy || !canSaveInvoiceLines}
                    />
                  </td>
                  <td>
                    <input
                      className="numeric"
                      type="number"
                      min={0}
                      step="0.01"
                      value={line.amount}
                      onChange={(event) =>
                        onInvoiceLineChange(index, "amount", event.target.value)
                      }
                      disabled={isBusy || !canSaveInvoiceLines}
                    />
                  </td>
                  <td>
                    <select
                      value={line.billingHeadCode}
                      onChange={(event) => {
                        const chargeCode = chargeCodeOptions.find(
                          (option) => option.code === event.target.value
                        );

                        onInvoiceLineChange(index, "billingHeadCode", event.target.value);

                        if (chargeCode) {
                          onInvoiceLineChange(index, "currencyCode", chargeCode.currencyCode);

                          if (Number(line.amount) === 0 && chargeCode.defaultUnitPriceAmount > 0) {
                            onInvoiceLineChange(
                              index,
                              "amount",
                              chargeCode.defaultUnitPriceAmount.toFixed(2)
                            );
                          }
                        }
                      }}
                      disabled={isBusy || !canSaveInvoiceLines}
                    >
                      <option value=""></option>
                      {chargeCodeOptions.map((option) => (
                        <option
                          key={option.chargeCodeId}
                          value={option.code}
                          disabled={option.status !== "Active"}
                        >
                          {option.code} - {option.name}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input
                      value={line.taxCode}
                      onChange={(event) =>
                        onInvoiceLineChange(index, "taxCode", event.target.value)
                      }
                      disabled={isBusy || !canSaveInvoiceLines}
                    />
                  </td>
                  <td>
                    <input
                      value={line.categoryCode}
                      onChange={(event) =>
                        onInvoiceLineChange(index, "categoryCode", event.target.value)
                      }
                      disabled={isBusy || !canSaveInvoiceLines}
                    />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="table-icon-button"
                      onClick={() => onRemoveInvoiceLine(index)}
                      disabled={isBusy || !canSaveInvoiceLines}
                      title="Remove invoice line"
                      aria-label="Remove invoice line"
                    >
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        <div className="billing-draft-panel">
          <div className="billing-draft-header">
            <strong>Billing Draft</strong>
            <button
              type="button"
              className="mini-button"
              onClick={onRefreshLookups}
              disabled={isBusy}
            >
              <RefreshCcw size={14} />
              Lookups
            </button>
          </div>
          <div className="billing-draft-grid">
            <label className="form-field">
              <span>Client</span>
              <select
                value={billingDraftFields.clientId}
                onChange={(event) =>
                  onBillingDraftFieldChange("clientId", event.target.value)
                }
                disabled={isBusy || !canCreateBillingDraft}
              >
                <option value=""></option>
                {clientOptions.map((option) => (
                  <option key={option.clientId} value={option.clientId}>
                    {option.code} - {option.displayName}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span>Contract Id</span>
              <input
                value={billingDraftFields.contractId}
                onChange={(event) =>
                  onBillingDraftFieldChange("contractId", event.target.value)
                }
                disabled={isBusy || !canCreateBillingDraft}
              />
            </label>
            <label className="form-field">
              <span>Invoice No</span>
              <input
                value={billingDraftFields.invoiceNumber}
                onChange={(event) =>
                  onBillingDraftFieldChange("invoiceNumber", event.target.value)
                }
                disabled={isBusy || !canCreateBillingDraft}
              />
            </label>
            <label className="form-field">
              <span>Issue Date</span>
              <input
                type="date"
                value={billingDraftFields.issueDate}
                onChange={(event) =>
                  onBillingDraftFieldChange("issueDate", event.target.value)
                }
                disabled={isBusy || !canCreateBillingDraft}
              />
            </label>
            <label className="form-field">
              <span>Due Date</span>
              <input
                type="date"
                value={billingDraftFields.dueDate}
                onChange={(event) =>
                  onBillingDraftFieldChange("dueDate", event.target.value)
                }
                disabled={isBusy || !canCreateBillingDraft}
              />
            </label>
            <label className="form-field">
              <span>Currency</span>
              <input
                value={billingDraftFields.currencyCode}
                onChange={(event) =>
                  onBillingDraftFieldChange("currencyCode", event.target.value)
                }
                disabled={isBusy || !canCreateBillingDraft}
              />
            </label>
          </div>
          <div className="billing-draft-actions">
            <button
              type="button"
              className="icon-button primary"
              onClick={onCreateBillingDraft}
              disabled={isBusy || !canCreateBillingDraft || invoiceLines.length === 0}
            >
              <FilePlus2 size={16} />
              Create Billing Draft
            </button>
            <span>
              {lastBillingDraft
                ? `${lastBillingDraft.invoiceNumber} ${lastBillingDraft.status} ${lastBillingDraft.totalAmount.toFixed(2)} ${lastBillingDraft.currencyCode}`
                : entry?.invoiceSummary.invoiceNumber
                  ? `Linked invoice ${entry.invoiceSummary.invoiceNumber}`
                  : ""}
            </span>
          </div>
        </div>
      </section>
    </div>
  );
}
