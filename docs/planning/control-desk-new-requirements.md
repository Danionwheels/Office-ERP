# SafarSuite Control Desk New Requirements

Date started: 2026-06-30

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
| Renewal rules | Paid-until, grace, read-only, suspended, blocked | High | Open |
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
| Audit every change | Record who changed pricing/license/payment status | High | Partial in CloudServer |
| Key rotation | Signing keys must support production rotation | High | Partial concept |

## Current Focus

The active implementation path is:

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
| Offline local | Client uses local SafarSuite only | Signed entitlement file or periodic check | Open |
| HQ sync | Branches sync to one client HQ server | Entitlement controls branch/device limits | Open |
| Cloud sync | Branches/HQ sync to SafarSuite cloud data plane | Entitlement controls cloud-sync module | Open |
| Hosted SaaS | Client uses hosted SafarSuite | Subscription renews hosted tenant access | Open |

## Desktop App Requirements

| Requirement | Description | Priority | Status |
| --- | --- | --- | --- |
| Desktop packaging | Final product must run as desktop app | High | Open |
| Dev browser UI | React UI may run in browser during development | High | Client desk active |
| Local database | Office data stored locally in PostgreSQL | High | Open |
| Backup/export | Office can back up and export critical data | High | Open |
| Multi-user office access | Multiple office staff can use app if needed | Medium | Open |
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

## Questions To Decide Soon

| Question | Why It Matters | Status |
| --- | --- | --- |
| Which payment gateway first? | Determines callback and settlement design | Open |
| Is branch allowance billed separately? | Affects pricing model | Open |
| Is device count or named-user count the main limit? | Affects entitlement/device policy | Open |
| Do we allow overuse during grace? | Affects support experience | Open |
| Should cloud sign all entitlement files, or can Provider ERP sign offline files directly? | Affects key custody/security | Open |
