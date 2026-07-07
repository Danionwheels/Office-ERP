import {
  CalendarCheck2,
  ClipboardList,
  Hash,
  ListTree,
  Plus,
  Send,
  Trash2,
  X
} from "lucide-react";
import { type FormEvent, type KeyboardEvent, useState } from "react";
import type {
  AccountingPeriod,
  JournalVoucherNumberPreview,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  ManualJournalEntryLineInput
} from "../../types/accountingTypes";
import {
  amount,
  formatMoney,
  getPostingPeriodState,
  roundMoney
} from "../../utils/journalModel";
import {
  amountToInput,
  createEmptyJournalLine,
  filterJournalAccounts,
  formatJournalAccountContext,
  formatJournalAccountOption,
  formatSingleVoucherCashPosting,
  formatSingleVoucherSide,
  formatVoucherModeFact,
  getActivePostingAccounts,
  getOppositeSingleVoucherSide,
  getSingleVoucherCashSide,
  getSingleVoucherGuideStatus,
  getVoucherPostButtonTitle,
  getVoucherPostingReadiness,
  getVoucherState,
  getVoucherStateTone,
  getJournalLineState,
  isCashOrBankAccount,
  journalLineStatusLabel,
  journalLineTitle,
  normalizeSingleVoucherLines,
  singleVoucherCashDescription,
  toPostingSide,
  voucherTypeOptions,
  withSelectedJournalAccount,
  type SingleVoucherPostingSide,
  type SingleVoucherValue,
  type VoucherInputMode
} from "../../utils/journalWorkbenchModel";

type JournalEntryEditorPanelProps = {
  accounts: LedgerAccountSummary[];
  periods: AccountingPeriod[];
  value: ManualJournalEntryInput;
  manualVoucherPreview: JournalVoucherNumberPreview | null;
  isBusy: boolean;
  onValueChange: (value: ManualJournalEntryInput) => void;
  onSuggestVoucherNumber: () => Promise<void>;
  onPost: () => Promise<void>;
};

export function JournalEntryEditorPanel({
  accounts,
  periods,
  value,
  manualVoucherPreview,
  isBusy,
  onValueChange,
  onSuggestVoucherNumber,
  onPost
}: JournalEntryEditorPanelProps) {
  const [voucherInputMode, setVoucherInputMode] = useState<VoucherInputMode>("multiple");
  const [voucherTypeCode, setVoucherTypeCode] = useState("J");
  const [singleVoucherValue, setSingleVoucherValue] = useState<SingleVoucherValue>({
    cashOrBankAccountId: "",
    referenceNo: "",
    chequeNo: "",
    paidTo: ""
  });
  const [accountLookupText, setAccountLookupText] = useState("");
  const postingAccounts = getActivePostingAccounts(accounts);
  const postingAccountsById = new Map<string, LedgerAccountSummary>(
    postingAccounts.map((account) => [account.ledgerAccountId, account])
  );
  const filteredPostingAccounts = filterJournalAccounts(postingAccounts, accountLookupText);
  const cashOrBankAccounts = postingAccounts.filter(isCashOrBankAccount);
  const singleVoucherAccountOptions =
    cashOrBankAccounts.length > 0 ? cashOrBankAccounts : postingAccounts;
  const filteredSingleVoucherAccountOptions =
    filterJournalAccounts(singleVoucherAccountOptions, accountLookupText);
  const selectedCashOrBankAccount =
    singleVoucherAccountOptions.find(
      (account) => account.ledgerAccountId === singleVoucherValue.cashOrBankAccountId
    )
    ?? singleVoucherAccountOptions[0]
    ?? null;
  const selectedCashOrBankAccountId =
    singleVoucherValue.cashOrBankAccountId !== ""
      ? singleVoucherValue.cashOrBankAccountId
      : selectedCashOrBankAccount?.ledgerAccountId ?? "";
  const visibleSingleVoucherAccountOptions = withSelectedJournalAccount(
    filteredSingleVoucherAccountOptions,
    selectedCashOrBankAccount
  );
  const selectedVoucherType =
    voucherTypeOptions.find((option) => option.code === voucherTypeCode) ?? voucherTypeOptions[0];
  const singleVoucherCashSide = getSingleVoucherCashSide(voucherTypeCode);
  const singleVoucherDetailSide = getOppositeSingleVoucherSide(singleVoucherCashSide);
  const singleVoucherDetailPostingSide = toPostingSide(singleVoucherDetailSide);
  const singleVoucherCashPostingSide = toPostingSide(singleVoucherCashSide);
  const singleVoucherDetailAmountSide = singleVoucherDetailPostingSide ?? "debit";
  const singleVoucherCashAmountSide = singleVoucherCashPostingSide ?? "credit";
  const isGuidedSingleVoucher =
    voucherInputMode === "single"
    && singleVoucherCashPostingSide !== null
    && singleVoucherDetailPostingSide !== null;
  const lineEntries = value.lines.map((line, index) => ({ line, index }));
  const displayedLineEntries = isGuidedSingleVoucher
    ? lineEntries.filter((entry) => entry.index > 0)
    : lineEntries;
  const singleVoucherDetailTotal = isGuidedSingleVoucher
    ? roundMoney(displayedLineEntries.reduce(
      (total, entry) => total + amount(entry.line[singleVoucherDetailAmountSide]),
      0
    ))
    : 0;
  const singleVoucherCashLine = value.lines[0] ?? createEmptyJournalLine();
  const singleVoucherCashAmount = isGuidedSingleVoucher
    ? amount(singleVoucherCashLine[singleVoucherCashAmountSide])
    : 0;
  const singleVoucherDetailDifference = roundMoney(singleVoucherCashAmount - singleVoucherDetailTotal);
  const singleVoucherGuideStatus = getSingleVoucherGuideStatus({
    cashAmount: singleVoucherCashAmount,
    detailLineCount: displayedLineEntries.length,
    detailTotal: singleVoucherDetailTotal,
    hasCashAccount: selectedCashOrBankAccountId !== "",
    incompleteDetailCount: displayedLineEntries.filter(
      (entry) => getJournalLineState(entry.line) !== "ready"
    ).length,
    isGuided: isGuidedSingleVoucher
  });
  const totalDebit = value.lines.reduce((total, line) => total + amount(line.debit), 0);
  const totalCredit = value.lines.reduce((total, line) => total + amount(line.credit), 0);
  const difference = roundMoney(totalDebit - totalCredit);
  const hasDebit = totalDebit > 0;
  const hasCredit = totalCredit > 0;
  const hasAccounts = value.lines.every((line) => line.ledgerAccountId.trim() !== "");
  const incompleteLineCount = value.lines.filter((line) => getJournalLineState(line) !== "ready").length;
  const postingPeriodState = getPostingPeriodState(value.entryDate, periods);
  const voucherState = getVoucherState({
    difference,
    hasAccounts,
    hasCredit,
    hasDebit,
    incompleteLineCount,
    postingBlocked: postingPeriodState.blocksPosting
  });
  const voucherStateTone = isGuidedSingleVoucher
    ? singleVoucherGuideStatus.tone
    : getVoucherStateTone({
      difference,
      hasAccounts,
      hasCredit,
      hasDebit,
      incompleteLineCount,
      postingBlocked: postingPeriodState.blocksPosting
    });
  const postingReadinessItems = getVoucherPostingReadiness({
    currencyCode: value.currencyCode,
    difference,
    entryDate: value.entryDate,
    hasAccounts,
    hasCredit,
    hasDebit,
    incompleteLineCount,
    lineCount: value.lines.length,
    postingPeriodState
  });
  const canPost =
    value.entryDate.trim() !== ""
    && value.currencyCode.trim() !== ""
    && value.lines.length >= 2
    && hasDebit
    && hasCredit
    && hasAccounts
    && difference === 0
    && !postingPeriodState.blocksPosting;
  const postButtonTitle = getVoucherPostButtonTitle(canPost, postingReadinessItems);

  async function handlePost(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onPost();
  }

  function updateLine(index: number, patch: Partial<ManualJournalEntryLineInput>) {
    const nextLines = value.lines.map((line, lineIndex) =>
      lineIndex === index ? { ...line, ...patch } : line);

    onValueChange({
      ...value,
      lines: isGuidedSingleVoucher ? syncSingleVoucherLines(nextLines) : nextLines
    });
  }

  function addLine() {
    const nextLines = [
      ...value.lines,
      createEmptyJournalLine()
    ];

    onValueChange({
      ...value,
      lines: isGuidedSingleVoucher ? syncSingleVoucherLines(nextLines) : nextLines
    });
  }

  function removeLine(index: number) {
    const nextLines = value.lines.filter((_, lineIndex) => lineIndex !== index);

    onValueChange({
      ...value,
      lines: isGuidedSingleVoucher ? syncSingleVoucherLines(nextLines) : nextLines
    });
  }

  function updateLineAmount(
    index: number,
    side: SingleVoucherPostingSide,
    nextValue: string
  ) {
    const oppositeSide: SingleVoucherPostingSide = side === "debit" ? "credit" : "debit";
    const line = value.lines[index] ?? createEmptyJournalLine();

    updateLine(index, {
      [side]: nextValue,
      [oppositeSide]: nextValue.trim() === "" ? line[oppositeSide] : ""
    });
  }

  function handleVoucherLineKeyDown(
    event: KeyboardEvent<HTMLDivElement>,
    index: number
  ) {
    if (event.key !== "Enter") {
      return;
    }

    event.preventDefault();

    const lastDisplayedLineIndex = displayedLineEntries[displayedLineEntries.length - 1]?.index;
    const line = value.lines[index];

    if (
      isBusy
      || lastDisplayedLineIndex !== index
      || line === undefined
      || getJournalLineState(line) !== "ready"
    ) {
      return;
    }

    addLine();
  }

  function handleVoucherInputModeChange(nextMode: VoucherInputMode) {
    if (nextMode === "multiple") {
      setVoucherInputMode(nextMode);
      return;
    }

    const nextVoucherTypeCode =
      getSingleVoucherCashSide(voucherTypeCode) === "manual" ? "P" : voucherTypeCode;
    const nextCashSide = getSingleVoucherCashSide(nextVoucherTypeCode);

    setVoucherInputMode(nextMode);
    setVoucherTypeCode(nextVoucherTypeCode);

    if (nextCashSide !== "manual") {
      const nextVoucherType =
        voucherTypeOptions.find((option) => option.code === nextVoucherTypeCode) ?? selectedVoucherType;

      onValueChange({
        ...value,
        lines: syncSingleVoucherLines(value.lines, singleVoucherValue, nextCashSide, nextVoucherType)
      });
    }
  }

  function handleVoucherTypeChange(nextVoucherTypeCode: string) {
    const nextCashSide = getSingleVoucherCashSide(nextVoucherTypeCode);
    const nextVoucherType =
      voucherTypeOptions.find((option) => option.code === nextVoucherTypeCode) ?? selectedVoucherType;

    setVoucherTypeCode(nextVoucherTypeCode);

    if (voucherInputMode === "single" && nextCashSide !== "manual") {
      onValueChange({
        ...value,
        lines: syncSingleVoucherLines(value.lines, singleVoucherValue, nextCashSide, nextVoucherType)
      });
    }
  }

  function updateSingleVoucherValue(patch: Partial<SingleVoucherValue>) {
    const nextSingleVoucherValue = {
      ...singleVoucherValue,
      ...patch
    };

    setSingleVoucherValue(nextSingleVoucherValue);

    if (isGuidedSingleVoucher) {
      onValueChange({
        ...value,
        lines: syncSingleVoucherLines(value.lines, nextSingleVoucherValue)
      });
    }
  }

  function syncSingleVoucherLines(
    lines: ManualJournalEntryLineInput[],
    nextSingleVoucherValue = singleVoucherValue,
    cashSide = singleVoucherCashSide,
    voucherType = selectedVoucherType
  ): ManualJournalEntryLineInput[] {
    const nextCashPostingSide = toPostingSide(cashSide);
    const nextDetailPostingSide = toPostingSide(getOppositeSingleVoucherSide(cashSide));

    if (nextCashPostingSide === null || nextDetailPostingSide === null) {
      return lines;
    }

    const nextLines = normalizeSingleVoucherLines(lines);
    const cashOrBankAccountId =
      nextSingleVoucherValue.cashOrBankAccountId.trim() !== ""
        ? nextSingleVoucherValue.cashOrBankAccountId
        : selectedCashOrBankAccountId;
    const nextInactiveDetailPostingSide = nextDetailPostingSide === "debit" ? "credit" : "debit";
    const detailLines = nextLines.slice(1).map((line) => {
      const activeAmount = amount(line[nextDetailPostingSide]);
      const inactiveAmount = amount(line[nextInactiveDetailPostingSide]);
      const carriedAmount = activeAmount > 0 ? activeAmount : inactiveAmount;

      return {
        ...line,
        [nextDetailPostingSide]: amountToInput(carriedAmount),
        [nextInactiveDetailPostingSide]: ""
      };
    });
    const detailTotal = roundMoney(detailLines.reduce(
      (total, line) => total + amount(line[nextDetailPostingSide]),
      0
    ));
    const cashLine = nextLines[0] ?? createEmptyJournalLine();

    return [
      {
        ...cashLine,
        ledgerAccountId: cashOrBankAccountId,
        debit: nextCashPostingSide === "debit" ? amountToInput(detailTotal) : "",
        credit: nextCashPostingSide === "credit" ? amountToInput(detailTotal) : "",
        description: singleVoucherCashDescription(nextSingleVoucherValue, voucherType)
      },
      ...detailLines
    ];
  }

  return (
    <section className={`client-panel journal-editor-panel ${voucherInputMode}`}>
      <form className="journal-form" onSubmit={handlePost}>
        <div className="client-panel-heading journal-window-heading">
          <div>
            <span>Voucher Master</span>
            <strong>{selectedVoucherType.code} - {selectedVoucherType.label}</strong>
          </div>
          <div className={`journal-period-status ${postingPeriodState.tone}`}>
            <CalendarCheck2 size={16} />
            <span>
              <small>{postingPeriodState.label}</small>
              <strong>{postingPeriodState.status}</strong>
              <em>{postingPeriodState.detail}</em>
            </span>
          </div>
        </div>

        <div className="voucher-mode-strip">
          <div className="voucher-mode-buttons" role="group" aria-label="Voucher input mode">
            <button
              className={voucherInputMode === "single" ? "active" : ""}
              type="button"
              onClick={() => handleVoucherInputModeChange("single")}
              disabled={isBusy}
              title="Single voucher input"
            >
              <ClipboardList size={14} />
              Single Voucher
            </button>
            <button
              className={voucherInputMode === "multiple" ? "active" : ""}
              type="button"
              onClick={() => handleVoucherInputModeChange("multiple")}
              disabled={isBusy}
              title="Multiple voucher input"
            >
              <ListTree size={14} />
              Multiple Voucher
            </button>
          </div>
          <label className="form-field voucher-type-field">
            <span>Voucher Type</span>
            <select
              value={voucherTypeCode}
              onChange={(event) => handleVoucherTypeChange(event.target.value)}
              disabled={isBusy}
            >
              {voucherTypeOptions.map((option) => (
                <option key={option.code} value={option.code}>
                  {option.code} - {option.label}
                </option>
              ))}
            </select>
          </label>
          <div className="voucher-mode-facts">
            <span>{voucherInputMode === "single" ? "Posting flow" : "Entry"}</span>
            <strong>{formatVoucherModeFact(
              voucherInputMode,
              singleVoucherCashSide,
              singleVoucherDetailSide
            )}</strong>
          </div>
        </div>

        <div className="billing-form-grid journal-header-fields voucher-master-fields">
          <label className="form-field">
            <span>Voucher No.</span>
            <div className="journal-inline-field">
              <input
                value={value.sourceReference}
                onChange={(event) =>
                  onValueChange({
                    ...value,
                    sourceReference: event.target.value
                  })
                }
                disabled={isBusy}
              />
              <button
                className="table-icon-button"
                type="button"
                onClick={() => void onSuggestVoucherNumber()}
                disabled={isBusy || value.entryDate.trim() === ""}
                title="Suggest next voucher number"
              >
                <Hash size={14} />
              </button>
            </div>
            {manualVoucherPreview !== null && (
              <small className="journal-reference-hint">
                {manualVoucherPreview.reference}
              </small>
            )}
          </label>
          <label className="form-field">
            <span>Voucher Date</span>
            <input
              type="date"
              value={value.entryDate}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  entryDate: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Type</span>
            <input
              value={`${selectedVoucherType.code} ${selectedVoucherType.label}`}
              readOnly
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Currency</span>
            <input
              value={value.currencyCode}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field wide">
            <span>Master Narration</span>
            <input
              value={value.memo}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  memo: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
        </div>

        {voucherInputMode === "single" && (
          <div className="single-voucher-panel">
            <label className="form-field wide">
              <span>Cash / Bank A/C</span>
              <select
                value={selectedCashOrBankAccountId}
                onChange={(event) =>
                  updateSingleVoucherValue({
                    cashOrBankAccountId: event.target.value
                  })
                }
                disabled={isBusy}
              >
                <option value="">Select account</option>
                {visibleSingleVoucherAccountOptions.map((account) => (
                  <option key={account.ledgerAccountId} value={account.ledgerAccountId}>
                    {formatJournalAccountOption(account)}
                  </option>
                ))}
              </select>
            </label>
            <div className="single-voucher-balance">
              <span>Account Class</span>
              <strong>{selectedCashOrBankAccount?.type ?? "-"}</strong>
              <small>
                {selectedCashOrBankAccount === null
                  ? "-"
                  : formatJournalAccountContext(selectedCashOrBankAccount)}
              </small>
            </div>
            <label className="form-field">
              <span>Chq / Slip</span>
              <input
                value={singleVoucherValue.chequeNo}
                onChange={(event) =>
                  updateSingleVoucherValue({
                    chequeNo: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field wide single-voucher-party-field">
              <span>Paid To / Received From</span>
              <input
                value={singleVoucherValue.paidTo}
                onChange={(event) =>
                  updateSingleVoucherValue({
                    paidTo: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Reference No.</span>
              <input
                value={singleVoucherValue.referenceNo}
                onChange={(event) =>
                  updateSingleVoucherValue({
                    referenceNo: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <div className={`single-voucher-flow ${singleVoucherGuideStatus.tone}`}>
              <span>
                <small>Cash side</small>
                <strong>{formatSingleVoucherSide(singleVoucherCashSide)}</strong>
              </span>
              <span>
                <small>Detail side</small>
                <strong>{formatSingleVoucherSide(singleVoucherDetailSide)}</strong>
              </span>
              <span>
                <small>Cash amount</small>
                <strong>{formatMoney(singleVoucherCashAmount)}</strong>
              </span>
              <span>
                <small>Detail total</small>
                <strong>{formatMoney(singleVoucherDetailTotal)}</strong>
              </span>
              <span>
                <small>Difference</small>
                <strong>{formatMoney(singleVoucherDetailDifference)}</strong>
              </span>
              <span>
                <small>Status</small>
                <strong>{singleVoucherGuideStatus.label}</strong>
              </span>
            </div>
            <div className="single-voucher-cash-preview">
              <span>
                <small>Auto cash line</small>
                <strong>{selectedCashOrBankAccount?.displayCode ?? "-"}</strong>
              </span>
              <span>
                <small>Posting</small>
                <strong>{formatSingleVoucherCashPosting(
                  singleVoucherCashSide,
                  singleVoucherCashAmount
                )}</strong>
              </span>
              <span className="wide">
                <small>Narration</small>
                <strong>{singleVoucherCashLine.description.trim() || "-"}</strong>
              </span>
            </div>
          </div>
        )}

        <div className="journal-account-lookup-row">
          <label className="form-field">
            <span>A/C Lookup</span>
            <input
              value={accountLookupText}
              onChange={(event) => setAccountLookupText(event.target.value)}
              disabled={isBusy}
              placeholder="Code, name, type, role"
            />
          </label>
          <div className="journal-account-lookup-count">
            <span>Matches</span>
            <strong>{filteredPostingAccounts.length}/{postingAccounts.length}</strong>
          </div>
          <button
            className="table-icon-button"
            type="button"
            onClick={() => setAccountLookupText("")}
            disabled={isBusy || accountLookupText.trim() === ""}
            title="Clear account lookup"
          >
            <X size={14} />
          </button>
        </div>

        <div className="journal-line-grid voucher-detail-grid">
          <div className="journal-line-head">Sr</div>
          <div className="journal-line-head">
            {isGuidedSingleVoucher ? "Account Details" : "Account Code / Description"}
          </div>
          <div className="journal-line-head">
            {isGuidedSingleVoucher && singleVoucherDetailAmountSide === "debit" ? "Debit Amount" : "Debit"}
          </div>
          <div className="journal-line-head">
            {isGuidedSingleVoucher && singleVoucherDetailAmountSide === "credit" ? "Credit Amount" : "Credit"}
          </div>
          <div className="journal-line-head">Line Narration</div>
          <div className="journal-line-head">State</div>
          <div className="journal-line-head"> </div>
          {displayedLineEntries.map(({ line, index }, displayIndex) => {
            const lineState = getJournalLineState(line);
            const selectedLineAccount = postingAccountsById.get(line.ledgerAccountId) ?? null;
            const visibleLineAccountOptions = withSelectedJournalAccount(
              filteredPostingAccounts,
              selectedLineAccount
            );

            return (
              <div
                className={`journal-line-row voucher-detail-row ${lineState}`}
                key={`${index}-${line.ledgerAccountId}`}
                onKeyDown={(event) => handleVoucherLineKeyDown(event, index)}
                title={journalLineTitle(lineState)}
              >
                <span className="voucher-line-number">{displayIndex + 1}</span>
                <label className="form-field journal-account-select">
                  <select
                    value={line.ledgerAccountId}
                    onChange={(event) =>
                      updateLine(index, {
                        ledgerAccountId: event.target.value
                      })
                    }
                    disabled={isBusy}
                  >
                    <option value="">Select account</option>
                    {visibleLineAccountOptions.map((account) => (
                      <option key={account.ledgerAccountId} value={account.ledgerAccountId}>
                        {formatJournalAccountOption(account)}
                      </option>
                    ))}
                  </select>
                  {selectedLineAccount !== null && (
                    <span className="journal-account-context">
                      <strong>{selectedLineAccount.displayCode}</strong>
                      <small>{formatJournalAccountContext(selectedLineAccount)}</small>
                    </span>
                  )}
                </label>
                <label className="form-field">
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={line.debit}
                    onChange={(event) =>
                      updateLineAmount(index, "debit", event.target.value)
                    }
                    disabled={isBusy || (isGuidedSingleVoucher && singleVoucherDetailAmountSide !== "debit")}
                  />
                </label>
                <label className="form-field">
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={line.credit}
                    onChange={(event) =>
                      updateLineAmount(index, "credit", event.target.value)
                    }
                    disabled={isBusy || (isGuidedSingleVoucher && singleVoucherDetailAmountSide !== "credit")}
                  />
                </label>
                <label className="form-field">
                  <input
                    value={line.description}
                    onChange={(event) =>
                      updateLine(index, {
                        description: event.target.value
                      })
                    }
                    disabled={isBusy}
                  />
                </label>
                <span className={`voucher-line-state ${lineState}`}>
                  {journalLineStatusLabel(lineState)}
                </span>
                <button
                  className="table-icon-button"
                  type="button"
                  onClick={() => removeLine(index)}
                  disabled={isBusy || displayedLineEntries.length <= 1}
                  title="Remove journal line"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            );
          })}
        </div>

        <div className="voucher-posting-readiness-row" aria-label="Voucher posting readiness">
          {postingReadinessItems.map((item) => (
            <span className={item.tone} key={item.label} title={item.detail}>
              <small>{item.label}</small>
              <strong>{item.status}</strong>
            </span>
          ))}
        </div>

        <div className="journal-total-row voucher-balance-row">
          <div>
            <span>Lines</span>
            <strong>{isGuidedSingleVoucher ? `${displayedLineEntries.length}+cash` : value.lines.length}</strong>
          </div>
          <div>
            <span>Debit</span>
            <strong>{formatMoney(totalDebit)}</strong>
          </div>
          <div>
            <span>Credit</span>
            <strong>{formatMoney(totalCredit)}</strong>
          </div>
          <div>
            <span>Difference</span>
            <strong>{formatMoney(difference)}</strong>
          </div>
          <div className={`voucher-state-card ${voucherStateTone}`}>
            <span>State</span>
            <strong>{isGuidedSingleVoucher ? singleVoucherGuideStatus.label : voucherState}</strong>
          </div>
          <div className="journal-actions">
            <button
              className="icon-button"
              type="button"
              onClick={addLine}
              disabled={isBusy}
              title="Add journal line"
            >
              <Plus size={16} />
              Line
            </button>
            <button
              className="icon-button primary"
              type="submit"
              disabled={isBusy || !canPost}
              title={postButtonTitle}
            >
              <Send size={16} />
              Post
            </button>
          </div>
        </div>
      </form>
    </section>
  );
}
