# SafarSuite Control Desk New Requirements

Date started: 2026-06-30

Direction update: 2026-07-18. Active product and scale requirements are governed by `docs/architecture/product-charter-2026-07-11.md`; final topology and deployment acceptance are governed by `docs/architecture/final-system-requirements-and-deployment-contract.md`; execution is tracked in `docs/planning/active-roadmap-2026-07-11.md`.

These requirements are not simply old Survey/FAS clone work. They are the reason the new SafarSuite Control Desk exists.

## Commercial Control

| Requirement | Description | Priority | Status |
| --- | --- | --- | --- |
| Client master | Maintain every SafarSuite client/company | High | Basic done with UI |
| Client contacts | Maintain owner, billing, support, technical, and accounts contacts per client | High | Basic done |
| Client accounting profile | Link client to AR ledger, default currency, and cloud identity | High | Basic done |
| Contract per client | Store custom commercial terms per client | High | Open |
| Dynamic pricing | Allow each client to have different charges | High | Partial |
| Plan/module selection | Track which SafarSuite modules are allowed | High | Open |
| Branch allowance | Track allowed branches/HQ/cloud sync limits | High | Open |
| Device/user allowance | Track allowed devices and/or users by contract | High | Open |
| Renewal rules | Paid-until, warning window, grace, read-only, suspended, blocked | High | Designed in `offline-entitlement-control-rules.md` |
| Offline entitlement lease | Keep paid offline clients working through paid period without heartbeat disturbance | High | Designed |
| Support notes | Internal notes for client support/admin history | Medium | Basic done |

## Billing And Payments

| Requirement | Description | Priority | Status |
| --- | --- | --- | --- |
| Invoice generation | Generate recurring/manual invoices for clients | High | Partial |
| Portal publishing | Publish invoice/balance to client portal | High | Partial: invoice outbox queued locally |
| Card payments | Integrate Pakistani card payment provider | High | Open |
| Online bank transfer | Support bank transfer proof or bank API later | High | Open |
| Manual reconciliation | Office staff can approve/reject bank transfer/payment | High | Open |
| Receipt posting | Payment updates office receivable/GL | High | Partial |
| Payment reversal | Handle failed/reversed/duplicate payments | Medium | Open |

## SafarSuite Control Cloud Link

| Requirement | Description | Priority | Status |
| --- | --- | --- | --- |
| Publish client snapshot | ERP sends approved client/contract data to cloud | High | Open |
| Receive payment status | Cloud sends payment result back to ERP | High | Open |
| Issue entitlement | Cloud signs renewed entitlement after payment | High | Concept exists |
| Issue product command | Cloud signs module/status/device changes | High | Concept exists |
| Heartbeat and command acknowledgement | Client local server pulls entitlement/product commands and acknowledges applied versions | High | Designed |
| Offline renewal file | Allow paid clients to renew by importing a signed file when the local server has no internet | High | Designed |
| Client data sync boundary | Keep branch/HQ/cloud operational data sync separate from billing, license, and entitlement control | High | Boundary/profile foundation done; data-plane implementation open |
| Audit every change | Record who changed pricing/license/payment status | High | Partial in CloudServer |
| Key rotation | Signing keys must support production rotation | High | Partial concept |

## Original Implementation Focus

The implementation path recorded when this note was created was:

```text
client maintenance (basic done)
  -> client accounting profile (basic done)
  -> charge rules
  -> invoice issue with GL posting
  -> cloud outbox publish (basic invoice outbox done)
  -> payment posting
  -> entitlement/status publish
```

Survey/FAS clone work is removed from the active API surface because it does not support this first acceptance flow.

## SafarSuite Modes To Support

| Mode | Description | Control Approach | Status |
| --- | --- | --- | --- |
| Offline local | Client uses local SafarSuite only | Signed entitlement valid through paid period, heartbeat when available, offline renewal file fallback | Designed |
| HQ sync | Branches sync to one client HQ server | Entitlement controls branch/device limits | Open |
| Cloud sync | Branches/HQ sync to SafarSuite cloud data plane | Entitlement controls cloud-sync module | Open |
| Hosted SaaS | Client uses hosted SafarSuite | Subscription renews hosted tenant access | Open |

These are deployment modes for the same SafarSuite product, not separate products. Offline local is one supported mode, but the product must also support multi-branch clients where branches sync to HQ or to a cloud data plane. The current control work should therefore keep branch/site identity, allowed branch limits, cloud-sync module access, heartbeat, diagnostics, and signed commands compatible with future multi-branch sync without mixing operational business-data sync into the billing/license channel.

The canonical deployment and data-sync boundary is documented in:

```text
docs/architecture/client-deployment-and-data-sync-boundary.md
```

The first installation/deployment profile foundation now exists in Control Cloud setup and status contracts. The Control Desk client page can save a deployment profile, create a cloud setup token from that profile, create a bootstrap package/install command from that profile, and display the stored cloud profile returned by installation status.

## Desktop App Requirements

| Requirement | Description | Priority | Status |
| --- | --- | --- | --- |
| Desktop packaging | Final product must install and operate on one dedicated office PC without a separate server or Linux dependency | High | Open |
| Dev browser UI | React UI may run in browser during development | High | Client desk active |
| Office-local database | Office truth stored in PostgreSQL on the same dedicated office PC behind the local Office Control API | High | Topology accepted; packaging and recovery open |
| Backup/export | Office can back up and export critical data | High | Open |
| Authorized office operators | Authorized staff may use the designated office PC; concurrent multi-PC hosting is deferred until explicitly approved | Medium | V1 boundary accepted |
| Role permissions | Owner/admin/accounts/support roles | High | Open |

## First Acceptance Flow

The first complete version is acceptable when this works end to end:

```text
Office staff creates client
Office staff sets custom price and allowed modules/devices/branches
ERP generates renewal invoice
ERP publishes invoice to portal/cloud
Client pays by card or bank transfer
Cloud confirms payment
ERP records receipt/accounting status
Cloud issues signed entitlement or product-kernel command
SafarSuite applies renewed access
ERP shows client as active/paid
```

## Offline Entitlement Rules

The accepted rule set is documented in:

```text
docs/planning/offline-entitlement-control-rules.md
```

Summary:

- Heartbeat status and license validity are separate.
- A paid one-month offline-capable client must keep working for the paid month even if the local server never reaches the internet during that month.
- Warnings start close to expiry, not at the first missed heartbeat.
- After expiry, the system enters grace and then restricted/read-only mode by policy.
- Normal renewals refresh the signed entitlement on heartbeat.
- Offline renewal files are required for clients whose local server cannot connect near expiry.
- Revocation during an offline paid period is enforced on next heartbeat or renewal boundary unless the client is assigned a shorter high-risk lease.
- Every entitlement, command, heartbeat, renewal file, and support override must be audited.

## Questions To Decide Soon

| Question | Why It Matters | Status |
| --- | --- | --- |
| Which payment gateway first? | Determines callback and settlement design | Open |
| Is branch allowance billed separately? | Affects pricing model | Open |
| Is device count or named-user count the main limit? | Affects entitlement/device policy | Open |
| Do we allow overuse during grace? | Affects support experience | Open; default should be read-only/no-new-transactions after grace |
| Should cloud sign all entitlement files, or can Control Desk sign emergency offline files directly? | Affects key custody/security | Open; default preference is Control Cloud signing with audited emergency fallback |
