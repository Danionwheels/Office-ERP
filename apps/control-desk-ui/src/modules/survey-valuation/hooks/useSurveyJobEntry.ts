import { useCallback, useEffect, useState } from "react";
import { ApiError } from "../../../shared/api/apiError";
import {
  createSurveyJobBillingDraft,
  createSurveyJob,
  getSurveyJobEntryById,
  getSurveyJobEntryByNumber,
  listChargeCodeOptions,
  listClientOptions,
  updateSurveyJob,
  updateSurveyJobDocuments,
  updateSurveyJobInvoiceLines
} from "../api/surveyJobEntryApi";
import {
  fieldsFromSurveyJobEntry,
  statusFromSurveyJobEntry
} from "../mappers/surveyJobEntryMapper";
import {
  defaultSurveyDocuments,
  emptySurveyBillingDraftFields,
  emptySurveyJobEntryFields,
  type ChargeCodeLookupOption,
  type ClientLookupOption,
  type EditableSurveyJobInvoiceLine,
  type SurveyBillingDraftFields,
  type SurveyBillingDraftResult,
  type SurveyDocumentChecklistItem,
  type SurveyJobEntry,
  type SurveyJobEntryFields,
  type SurveyJobStatus
} from "../types/surveyJobEntryTypes";

type FieldName = keyof SurveyJobEntryFields;

export function useSurveyJobEntry() {
  const [surveyJobId, setSurveyJobId] = useState("");
  const [surveyJobNumber, setSurveyJobNumber] = useState("");
  const [jobNumberSearch, setJobNumberSearch] = useState("");
  const [status, setStatus] = useState<SurveyJobStatus>("Draft");
  const [fields, setFields] = useState<SurveyJobEntryFields>({ ...emptySurveyJobEntryFields });
  const [documents, setDocuments] = useState<SurveyDocumentChecklistItem[]>([
    ...defaultSurveyDocuments
  ]);
  const [invoiceLines, setInvoiceLines] = useState<EditableSurveyJobInvoiceLine[]>([]);
  const [billingDraftFields, setBillingDraftFields] = useState<SurveyBillingDraftFields>({
    ...emptySurveyBillingDraftFields
  });
  const [clientOptions, setClientOptions] = useState<ClientLookupOption[]>([]);
  const [chargeCodeOptions, setChargeCodeOptions] = useState<ChargeCodeLookupOption[]>([]);
  const [lastBillingDraft, setLastBillingDraft] = useState<SurveyBillingDraftResult | null>(null);
  const [entry, setEntry] = useState<SurveyJobEntry | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const setField = useCallback(
    <TField extends FieldName>(name: TField, value: SurveyJobEntryFields[TField]) => {
      setFields((current) => ({ ...current, [name]: value }));
    },
    []
  );

  const loadLookups = useCallback(async () => {
    await run(async () => {
      const [clients, chargeCodes] = await Promise.all([
        listClientOptions(),
        listChargeCodeOptions()
      ]);

      setClientOptions(clients);
      setChargeCodeOptions(chargeCodes);
      setMessage("Lookup lists refreshed.");
    });
  }, []);

  useEffect(() => {
    void loadLookups();
  }, [loadLookups]);

  const reset = useCallback(() => {
    setSurveyJobId("");
    setSurveyJobNumber("");
    setJobNumberSearch("");
    setStatus("Draft");
    setFields({ ...emptySurveyJobEntryFields });
    setDocuments([...defaultSurveyDocuments]);
    setInvoiceLines([]);
    setBillingDraftFields({ ...emptySurveyBillingDraftFields });
    setLastBillingDraft(null);
    setEntry(null);
    setMessage("New entry ready.");
    setError("");
  }, []);

  const loadByNumber = useCallback(async () => {
    const number = jobNumberSearch.trim();

    if (number === "") {
      setError("Enter a job number to search.");
      return;
    }

    await run(async () => {
      const loaded = await getSurveyJobEntryByNumber(number);
      applyEntry(loaded);
      setMessage(`Loaded ${loaded.surveyJobNumber}.`);
    });
  }, [jobNumberSearch]);

  const save = useCallback(async () => {
    if (surveyJobId === "") {
      if (surveyJobNumber.trim() === "") {
        setError("Survey job number is required.");
        return;
      }

      await run(async () => {
        const created = await createSurveyJob(surveyJobNumber.trim(), fields);
        const loaded = await getSurveyJobEntryById(created.surveyJobId);
        applyEntry(loaded);
        setMessage(`Created ${created.surveyJobNumber}.`);
      });

      return;
    }

    await run(async () => {
      const updated = await updateSurveyJob(surveyJobId, status, fields);
      applyEntry(updated);
      setMessage(`Saved ${updated.surveyJobNumber}.`);
    });
  }, [fields, status, surveyJobId, surveyJobNumber]);

  const setDocument = useCallback(
    (
      type: SurveyDocumentChecklistItem["type"],
      changes: Partial<Pick<SurveyDocumentChecklistItem, "status" | "receivedOn">>
    ) => {
      setDocuments((current) =>
        current.map((document) => {
          if (document.type !== type) {
            return document;
          }

          const nextStatus = changes.status ?? document.status;

          return {
            ...document,
            ...changes,
            receivedOn:
              nextStatus === "Received"
                ? changes.receivedOn ?? document.receivedOn
                : null
          };
        })
      );
    },
    []
  );

  const saveDocuments = useCallback(async () => {
    if (surveyJobId === "") {
      setError("Save the survey job before saving documents.");
      return;
    }

    await run(async () => {
      const updated = await updateSurveyJobDocuments(surveyJobId, documents);
      applyEntry(updated);
      setMessage(`Saved documents for ${updated.surveyJobNumber}.`);
    });
  }, [documents, surveyJobId]);

  const setInvoiceLine = useCallback(
    <TField extends keyof EditableSurveyJobInvoiceLine>(
      index: number,
      name: TField,
      value: EditableSurveyJobInvoiceLine[TField]
    ) => {
      setInvoiceLines((current) =>
        current.map((line, lineIndex) =>
          lineIndex === index ? { ...line, [name]: value } : line
        )
      );
    },
    []
  );

  const addInvoiceLine = useCallback(() => {
    setInvoiceLines((current) => [
      ...current,
      {
        sequenceNumber:
          current.length === 0
            ? 1
            : Math.max(...current.map((line) => line.sequenceNumber)) + 1,
        descriptionType: "Manual",
        description: "",
        amount: "0.00",
        currencyCode: "PKR",
        billingHeadCode: "",
        taxCode: "",
        categoryCode: ""
      }
    ]);
  }, []);

  const removeInvoiceLine = useCallback((index: number) => {
    setInvoiceLines((current) => current.filter((_, lineIndex) => lineIndex !== index));
  }, []);

  const saveInvoiceLines = useCallback(async () => {
    if (surveyJobId === "") {
      setError("Save the survey job before saving invoice lines.");
      return;
    }

    await run(async () => {
      const updated = await updateSurveyJobInvoiceLines(surveyJobId, invoiceLines);
      applyEntry(updated);
      setMessage(`Saved invoice lines for ${updated.surveyJobNumber}.`);
    });
  }, [invoiceLines, surveyJobId]);

  const setBillingDraftField = useCallback(
    <TField extends keyof SurveyBillingDraftFields>(
      name: TField,
      value: SurveyBillingDraftFields[TField]
    ) => {
      setBillingDraftFields((current) => ({ ...current, [name]: value }));
    },
    []
  );

  const createBillingDraft = useCallback(async () => {
    if (surveyJobId === "") {
      setError("Save the survey job before creating a billing draft.");
      return;
    }

    await run(async () => {
      const draft = await createSurveyJobBillingDraft(surveyJobId, billingDraftFields);
      setLastBillingDraft(draft);
      applyEntry(draft.surveyJob);
      setMessage(`Created billing draft ${draft.invoiceNumber}.`);
    });
  }, [billingDraftFields, surveyJobId]);

  async function run(action: () => Promise<void>) {
    setIsBusy(true);
    setError("");
    setMessage("");

    try {
      await action();
    } catch (caught) {
      setError(toMessage(caught));
    } finally {
      setIsBusy(false);
    }
  }

  function applyEntry(loaded: SurveyJobEntry) {
    setEntry(loaded);
    setSurveyJobId(loaded.surveyJobId);
    setSurveyJobNumber(loaded.surveyJobNumber);
    setJobNumberSearch(loaded.surveyJobNumber);
    setStatus(statusFromSurveyJobEntry(loaded));
    setFields(fieldsFromSurveyJobEntry(loaded));
    setDocuments(mergeDocuments(loaded.documents));
    setInvoiceLines(invoiceLinesFromEntry(loaded));
    setBillingDraftFields((current) => ({
      ...current,
      invoiceNumber: loaded.invoiceSummary.invoiceNumber ?? current.invoiceNumber,
      issueDate: loaded.dates.invoiceDate ?? current.issueDate
    }));
  }

  return {
    surveyJobId,
    surveyJobNumber,
    setSurveyJobNumber,
    jobNumberSearch,
    setJobNumberSearch,
    status,
    setStatus,
    fields,
    setField,
    documents,
    setDocument,
    invoiceLines,
    setInvoiceLine,
    addInvoiceLine,
    removeInvoiceLine,
    saveInvoiceLines,
    billingDraftFields,
    setBillingDraftField,
    createBillingDraft,
    clientOptions,
    chargeCodeOptions,
    loadLookups,
    lastBillingDraft,
    entry,
    isBusy,
    message,
    error,
    reset,
    loadByNumber,
    save,
    saveDocuments
  };
}

function mergeDocuments(
  documents: SurveyDocumentChecklistItem[]
): SurveyDocumentChecklistItem[] {
  return defaultSurveyDocuments.map((defaultDocument) => {
    const saved = documents.find((document) => document.type === defaultDocument.type);

    return saved ?? defaultDocument;
  });
}

function invoiceLinesFromEntry(entry: SurveyJobEntry): EditableSurveyJobInvoiceLine[] {
  return entry.invoiceLines.map((line) => ({
    sequenceNumber: line.sequenceNumber,
    descriptionType: line.descriptionType,
    description: line.description,
    amount: line.amount.amount.toFixed(2),
    currencyCode: line.amount.currencyCode,
    billingHeadCode: line.billingHeadCode ?? "",
    taxCode: line.taxCode ?? "",
    categoryCode: line.categoryCode ?? ""
  }));
}

function toMessage(caught: unknown): string {
  if (caught instanceof ApiError && caught.errors.length > 0) {
    return caught.errors.map((item) => item.message).join(" ");
  }

  if (caught instanceof Error) {
    return caught.message;
  }

  return "The request could not be completed.";
}
