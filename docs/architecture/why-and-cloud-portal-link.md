# Why We Are Building SafarSuite Control Desk

Date: 2026-06-30

## Purpose

SafarSuite Control Desk is our internal desktop system for managing the commercial side of SafarSuite.

It exists because SafarSuite clients will not all be on the same setup. Some may use offline local servers, some may use branch-to-HQ sync, some may use cloud sync, and some may use hosted SaaS. We still need one place in our office where we control:

- client records
- custom pricing per client
- subscription and renewal dates
- allowed modules
- allowed branches
- allowed users/devices
- invoices and receipts
- online card/bank-transfer payment status
- manual payment reconciliation
- activation and entitlement decisions
- support/admin notes
- GL/accounting entries for our own office

This should not live inside SafarSuite itself. SafarSuite is the product clients use. SafarSuite Control Desk is the system we use to operate the business around SafarSuite.

## Product Shape

The final product should be a desktop app.

Development can use a browser-hosted React UI for speed, but production should be packaged as a desktop app. The practical stack is:

```text
Desktop shell
  Tauri wrapper around the React TypeScript UI

Frontend
  React + TypeScript
  dense admin screens, forms, tables, reconciliation views

Backend
  .NET 10 API/application layer
  business rules, billing, contracts, entitlements, sync to cloud

Database
  local PostgreSQL for office data
```

Because this is for one office, it does not need a multi-branch internal deployment in V1. It should still be multi-user-capable for office staff, but not designed like a SaaS tenant platform.

## System Boundary

SafarSuite Control Desk is the source of truth for commercial decisions. SafarSuite Control Cloud is the online bridge. SafarSuite consumes signed decisions.

```text
SafarSuite Control Desk
  create/update client
  set custom pricing
  set allowed modules/devices/branches
  generate invoice
  reconcile payments
        |
        v
SafarSuite Control Cloud + SafarSuite Client Portal
  mirror client-facing invoice
  collect payment
  receive payment gateway callback
  issue entitlement/license/product commands
        |
        v
SafarSuite installations
  offline local server
  HQ-sync deployment
  cloud-sync deployment
  hosted SaaS deployment
```

## Key Distinction

There are two different clouds:

```text
Control Cloud
  billing, portal, payment, license, device, module, branch limits
  required for our business operations

Client Data Sync Cloud
  operational SafarSuite data from client branches/HQ
  optional, plan-dependent
```

A client can reject cloud data sync and still be managed by the Control Cloud for renewal, portal invoices, allowed devices, and signed entitlements.

## Expected Flows

### Client Setup

1. Office staff creates a client in SafarSuite Control Desk.
2. Staff defines pricing, modules, allowed devices, allowed branches, and payment terms.
3. Control Desk publishes the client contract snapshot to SafarSuite Control Cloud.
4. Cloud creates/updates the client portal account.
5. Cloud can issue the first activation token or wait for SafarSuite to request activation.

### Renewal And Payment

1. SafarSuite Control Desk generates a renewal invoice.
2. The invoice is published to the client portal.
3. Client pays by card or online bank transfer.
4. Payment gateway or bank-transfer confirmation updates SafarSuite Control Cloud.
5. Cloud sends payment/receipt status back to SafarSuite Control Desk.
6. SafarSuite Control Desk updates receivable/receipt/GL.
7. Cloud issues a fresh entitlement snapshot or product-kernel command.
8. SafarSuite receives the update and renews access.

### Offline Client

Offline clients still have records, invoices, payments, allowed modules, device limits, and renewals.

If the client system has periodic internet:

```text
SafarSuite local server -> checks Control Cloud -> downloads signed entitlement
```

If the client system has no internet:

```text
SafarSuite Control Desk -> generates/saves signed license file -> client imports it manually
```

### Cloud Sync Client

Cloud sync clients have two channels:

```text
Control channel
  plan, payment, license, allowed branches/devices/modules

Data channel
  branch/HQ operational sync and owner dashboard data
```

These should be separate. A payment failure should affect entitlement state, but it should not corrupt or mix with the client's operational sync data.

## First Version Scope

V1 should prove one complete commercial flow:

1. Create client.
2. Define custom monthly price.
3. Define allowed modules, branches, and devices.
4. Generate invoice.
5. Publish invoice to portal/cloud.
6. Mark payment as received.
7. Issue signed entitlement.
8. Import/apply entitlement in SafarSuite.
9. Show current client status back in SafarSuite Control Desk.

Do not start with every accounting/reporting feature from the old Survey app. Use Survey as workflow evidence, then migrate only the pieces needed for this flow.

## Non-Goals For V1

- SafarSuite Control Desk does not need to be a web app.
- SafarSuite Control Desk does not need multi-branch office deployment.
- Client business-data cloud sync is not mandatory for all clients.
- Payment provider should not be hard-coded directly into business workflows.
- SafarSuite should not contain provider-office accounting.

## Architectural Rule

SafarSuite Control Desk owns commercial truth. SafarSuite Control Cloud exposes and signs that truth. SafarSuite obeys signed entitlements.
