import {
  CheckCircle2,
  Download,
  Landmark,
  RefreshCw,
  Save,
  XCircle
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import {
  downloadPortalPaymentClaimProof,
  getProviderBankDetails,
  importPortalPaymentClaims,
  listPortalPaymentClaims,
  rejectPortalPaymentClaim,
  updateProviderBankDetails,
  verifyPortalPaymentClaim
} from "../api/paymentApi";
import type {
  PortalPaymentClaim,
  ProviderBankDetails,
  UpdateProviderBankDetailsInput
} from "../types/paymentTypes";

type PortalPaymentClaimsPanelProps = {
  clientId: string;
  cashOrBankAccountId: string;
  accountsReceivableAccountId: string;
  postingDate: string;
};

const emptyBankDetails: ProviderBankDetails = {
  isConfigured: false,
  bankName: "",
  accountTitle: "",
  accountNumber: "",
  iban: "",
  branchOrRoutingInfo: ""
};

export function PortalPaymentClaimsPanel({
  clientId,
  cashOrBankAccountId,
  accountsReceivableAccountId,
  postingDate
}: PortalPaymentClaimsPanelProps) {
  const [claims, setClaims] = useState<PortalPaymentClaim[]>([]);
  const [bankDetails, setBankDetails] = useState<ProviderBankDetails>(emptyBankDetails);
  const [decisionNotes, setDecisionNotes] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [busyClaimId, setBusyClaimId] = useState<string | null>(null);
  const [isSavingBankDetails, setIsSavingBankDetails] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const orderedClaims = useMemo(() => [...claims].sort((left, right) => {
    const statusOrder = (value: string) => normalizeStatus(value) === "pending_verification" ? 0 : 1;
    return statusOrder(left.status) - statusOrder(right.status)
      || Date.parse(right.submittedAtUtc) - Date.parse(left.submittedAtUtc);
  }), [claims]);

  useEffect(() => {
    if (clientId === "") {
      setClaims([]);
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setError("");

    Promise.all([listPortalPaymentClaims(clientId), getProviderBankDetails()])
      .then(async ([claimPage, details]) => {
        if (cancelled) return;

        setClaims(claimPage.claims ?? []);
        setBankDetails(details);

        try {
          const synced = await importPortalPaymentClaims(clientId);
          if (!cancelled) {
            setClaims(synced.claims ?? []);
            if (synced.importedCount > 0) {
              setMessage(
                `${synced.importedCount} new portal payment claim${synced.importedCount === 1 ? "" : "s"} imported.`
              );
            }
          }
        } catch (caught) {
          if (!cancelled) {
            setError(`Showing saved claims. Control Cloud refresh failed: ${toMessage(caught)}`);
          }
        }
      })
      .catch((caught: unknown) => {
        if (!cancelled) setError(toMessage(caught));
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [clientId]);

  async function handleImport() {
    setIsLoading(true);
    setError("");
    setMessage("");
    try {
      const result = await importPortalPaymentClaims(clientId);
      setClaims(result.claims ?? []);
      setMessage(
        result.importedCount > 0
          ? `${result.importedCount} portal payment claim${result.importedCount === 1 ? "" : "s"} imported.`
          : "Portal payment claims are up to date."
      );
    } catch (caught) {
      setError(toMessage(caught));
    } finally {
      setIsLoading(false);
    }
  }

  async function handleVerify(claim: PortalPaymentClaim) {
    setBusyClaimId(claim.claimId);
    setError("");
    setMessage("");
    try {
      const result = await verifyPortalPaymentClaim(claim.claimId, {
        cashOrBankAccountId,
        accountsReceivableAccountId,
        postingDate,
        decisionNote: decisionNotes[claim.claimId]?.trim() || null
      });
      replaceClaim(result.claim);
      setMessage(`Claim ${claim.transferReferenceNumber} verified and posted.`);
    } catch (caught) {
      setError(toMessage(caught));
    } finally {
      setBusyClaimId(null);
    }
  }

  async function handleReject(claim: PortalPaymentClaim) {
    const reason = decisionNotes[claim.claimId]?.trim() ?? "";
    if (reason === "") {
      setError("Enter a rejection reason before rejecting the claim.");
      return;
    }

    setBusyClaimId(claim.claimId);
    setError("");
    setMessage("");
    try {
      replaceClaim(await rejectPortalPaymentClaim(claim.claimId, reason));
      setMessage(`Claim ${claim.transferReferenceNumber} rejected.`);
    } catch (caught) {
      setError(toMessage(caught));
    } finally {
      setBusyClaimId(null);
    }
  }

  async function handleProofDownload(claim: PortalPaymentClaim) {
    setBusyClaimId(claim.claimId);
    setError("");
    try {
      await downloadPortalPaymentClaimProof(
        claim.claimId,
        claim.proofAttachment?.fileName ?? `${claim.transferReferenceNumber}-proof`
      );
    } catch (caught) {
      setError(toMessage(caught));
    } finally {
      setBusyClaimId(null);
    }
  }

  async function handleBankDetailsSave() {
    setIsSavingBankDetails(true);
    setError("");
    setMessage("");
    try {
      const input: UpdateProviderBankDetailsInput = {
        bankName: bankDetails.bankName,
        accountTitle: bankDetails.accountTitle,
        accountNumber: bankDetails.accountNumber,
        iban: bankDetails.iban,
        branchOrRoutingInfo: bankDetails.branchOrRoutingInfo
      };
      setBankDetails(await updateProviderBankDetails(input));
      setMessage("Portal bank details saved and queued for Control Cloud publishing.");
    } catch (caught) {
      setError(toMessage(caught));
    } finally {
      setIsSavingBankDetails(false);
    }
  }

  function replaceClaim(next: PortalPaymentClaim) {
    setClaims((current) => current.map((claim) => claim.claimId === next.claimId ? next : claim));
  }

  const postingReady = cashOrBankAccountId !== ""
    && accountsReceivableAccountId !== ""
    && postingDate !== "";

  return (
    <section className="payment-workspace portal-claims-workspace">
      <header className="billing-step-header">
        <div>
          <span>Client Portal</span>
          <h2>Bank-transfer claims</h2>
        </div>
        <button
          className="icon-button"
          type="button"
          onClick={() => void handleImport()}
          disabled={clientId === "" || isLoading}
        >
          <RefreshCw size={16} />
          {isLoading ? "Refreshing" : "Refresh claims"}
        </button>
      </header>

      {(message !== "" || error !== "") && (
        <div className={`portal-claim-message ${error === "" ? "success" : "error"}`} role="status">
          {error || message}
        </div>
      )}

      <section className="client-panel billing-light-panel">
        <div className="client-panel-heading">
          <div>
            <span>Review queue</span>
            <strong>Submitted transfer evidence</strong>
          </div>
          <span className="status-pill draft">
            {orderedClaims.filter((claim) => normalizeStatus(claim.status) === "pending_verification").length} pending
          </span>
        </div>

        {!postingReady && (
          <p className="portal-claim-hint">
            Select the cash/bank account, receivable account, and posting date in the Receipt step before verification.
          </p>
        )}

        {orderedClaims.length === 0 ? (
          <div className="portal-claim-empty">No portal payment claims for this client.</div>
        ) : (
          <div className="portal-claim-list">
            {orderedClaims.map((claim) => {
              const status = normalizeStatus(claim.status);
              const pending = status === "pending_verification";
              const busy = busyClaimId === claim.claimId;
              return (
                <article className="portal-claim-card" key={claim.claimId}>
                  <div className="portal-claim-card-heading">
                    <div>
                      <span>{claim.invoiceNumber}</span>
                      <strong>{formatMoney(claim.amount, claim.currencyCode)}</strong>
                    </div>
                    <span className={`status-pill ${statusClass(status)}`}>{statusLabel(status)}</span>
                  </div>
                  <dl className="payment-result-facts portal-claim-facts">
                    <div><dt>Transfer reference</dt><dd>{claim.transferReferenceNumber}</dd></div>
                    <div><dt>Submitted</dt><dd>{formatDateTime(claim.submittedAtUtc)}</dd></div>
                    <div><dt>Proof</dt><dd>{claim.proofAttachment?.fileName ?? "Not attached"}</dd></div>
                    {claim.rejectionReason && (
                      <div><dt>Rejection reason</dt><dd>{claim.rejectionReason}</dd></div>
                    )}
                  </dl>
                  {claim.proofAttachment && (
                    <button
                      className="icon-button"
                      type="button"
                      onClick={() => void handleProofDownload(claim)}
                      disabled={busy}
                    >
                      <Download size={15} /> Download proof
                    </button>
                  )}
                  {pending && (
                    <div className="portal-claim-decision">
                      <label className="form-field">
                        <span>Decision note / rejection reason</span>
                        <textarea
                          rows={2}
                          value={decisionNotes[claim.claimId] ?? ""}
                          onChange={(event) => setDecisionNotes((current) => ({
                            ...current,
                            [claim.claimId]: event.target.value
                          }))}
                          maxLength={1000}
                        />
                      </label>
                      <div className="portal-claim-actions">
                        <button
                          className="icon-button primary"
                          type="button"
                          disabled={!postingReady || busy}
                          onClick={() => void handleVerify(claim)}
                        >
                          <CheckCircle2 size={15} /> Verify + post
                        </button>
                        <button
                          className="icon-button danger"
                          type="button"
                          disabled={busy || (decisionNotes[claim.claimId]?.trim() ?? "") === ""}
                          onClick={() => void handleReject(claim)}
                        >
                          <XCircle size={15} /> Reject
                        </button>
                      </div>
                    </div>
                  )}
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="client-panel billing-light-panel">
        <div className="client-panel-heading">
          <div>
            <span>Portal settings</span>
            <strong>Provider bank details</strong>
          </div>
          <span className={`status-pill ${bankDetails.isConfigured ? "active" : "draft"}`}>
            {bankDetails.isConfigured ? "Published source" : "Needs setup"}
          </span>
        </div>
        <div className="payment-form-grid receipt portal-bank-details-form">
          <BankField label="Bank name" value={bankDetails.bankName} onChange={(bankName) => setBankDetails({ ...bankDetails, bankName })} />
          <BankField label="Account title" value={bankDetails.accountTitle} onChange={(accountTitle) => setBankDetails({ ...bankDetails, accountTitle })} />
          <BankField label="Account number" value={bankDetails.accountNumber} onChange={(accountNumber) => setBankDetails({ ...bankDetails, accountNumber })} />
          <BankField label="IBAN" value={bankDetails.iban} onChange={(iban) => setBankDetails({ ...bankDetails, iban })} />
          <BankField label="Branch / routing" value={bankDetails.branchOrRoutingInfo} onChange={(branchOrRoutingInfo) => setBankDetails({ ...bankDetails, branchOrRoutingInfo })} wide />
        </div>
        <button
          className="icon-button primary portal-bank-save"
          type="button"
          onClick={() => void handleBankDetailsSave()}
          disabled={isSavingBankDetails}
        >
          {isSavingBankDetails ? <RefreshCw size={15} /> : <Save size={15} />}
          {isSavingBankDetails ? "Saving" : "Save portal bank details"}
        </button>
        <p className="portal-claim-hint">
          <Landmark size={14} /> These client-visible instructions contain no ledger or journal identifiers.
        </p>
      </section>
    </section>
  );
}

function BankField({
  label,
  value,
  onChange,
  wide = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  wide?: boolean;
}) {
  return (
    <label className={`form-field${wide ? " wide" : ""}`}>
      <span>{label}</span>
      <input value={value} onChange={(event) => onChange(event.target.value)} maxLength={160} />
    </label>
  );
}

function normalizeStatus(value: string): string {
  return value.trim().replace(/([a-z])([A-Z])/g, "$1_$2").replace(/[ -]+/g, "_").toLowerCase();
}

function statusLabel(status: string): string {
  if (status === "pending_verification") return "Pending verification";
  if (status === "verified") return "Verified";
  if (status === "rejected") return "Rejected";
  return status.replaceAll("_", " ");
}

function statusClass(status: string): string {
  if (status === "verified") return "active";
  if (status === "rejected") return "suspended";
  return "draft";
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

function formatDateTime(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function toMessage(caught: unknown): string {
  return caught instanceof Error ? caught.message : "The request could not be completed.";
}
