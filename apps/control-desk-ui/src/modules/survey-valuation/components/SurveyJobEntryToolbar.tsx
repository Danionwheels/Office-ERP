import { Plus, RefreshCcw, Save, Search } from "lucide-react";
import type { SurveyJobStatus } from "../types/surveyJobEntryTypes";

type SurveyJobEntryToolbarProps = {
  surveyJobId: string;
  surveyJobNumber: string;
  jobNumberSearch: string;
  status: SurveyJobStatus;
  isBusy: boolean;
  onSurveyJobNumberChange: (value: string) => void;
  onJobNumberSearchChange: (value: string) => void;
  onStatusChange: (value: SurveyJobStatus) => void;
  onNew: () => void;
  onSearch: () => void;
  onSave: () => void;
};

const statuses: SurveyJobStatus[] = [
  "Draft",
  "Received",
  "Pending",
  "Unsettled",
  "Delivered",
  "Settled",
  "Cancelled"
];

export function SurveyJobEntryToolbar({
  surveyJobId,
  surveyJobNumber,
  jobNumberSearch,
  status,
  isBusy,
  onSurveyJobNumberChange,
  onJobNumberSearchChange,
  onStatusChange,
  onNew,
  onSearch,
  onSave
}: SurveyJobEntryToolbarProps) {
  return (
    <div className="entry-toolbar">
      <div className="toolbar-title">
        <span>Survey Job Entry</span>
        <strong>{surveyJobId === "" ? "New" : surveyJobNumber}</strong>
      </div>

      <label className="toolbar-field short">
        <span>Job No</span>
        <input
          value={surveyJobNumber}
          onChange={(event) => onSurveyJobNumberChange(event.target.value)}
          disabled={surveyJobId !== "" || isBusy}
        />
      </label>

      <label className="toolbar-field">
        <span>Find Job</span>
        <input
          value={jobNumberSearch}
          onChange={(event) => onJobNumberSearchChange(event.target.value)}
        />
      </label>

      <button type="button" className="icon-button" onClick={onSearch} disabled={isBusy}>
        <Search size={16} />
        Search
      </button>

      <label className="toolbar-field short">
        <span>Status</span>
        <select
          value={status}
          onChange={(event) => onStatusChange(event.target.value as SurveyJobStatus)}
          disabled={isBusy || surveyJobId === ""}
        >
          {statuses.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </label>

      <button type="button" className="icon-button" onClick={onNew} disabled={isBusy}>
        <Plus size={16} />
        New
      </button>

      <button type="button" className="icon-button primary" onClick={onSave} disabled={isBusy}>
        {isBusy ? <RefreshCcw size={16} /> : <Save size={16} />}
        Save
      </button>
    </div>
  );
}
