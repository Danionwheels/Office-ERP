# Product Direction

Date added: 2026-07-02

This is the canonical direction note for SafarSuite Control Desk, SafarSuite Control Cloud, SafarSuite Client Portal, and the SafarSuite client product.

Use this document when deciding where a feature belongs, which system owns a decision, and how to avoid mixing provider-control work with client business-data work.

## North Star

Build a provider-controlled commercial, licensing, entitlement, deployment, and support spine for SafarSuite.

```text
SafarSuite Control Desk
  decides what a client has bought, paid, and is allowed to use

SafarSuite Control Cloud + SafarSuite Client Portal
  mirrors approved client-facing state, signs entitlement/control artifacts, and receives local-server status

SafarSuite client systems
  run the client's business and obey signed entitlements locally
```

The active product chain is:

```text
create client
  -> define contract, pricing, modules, devices, branches, users
  -> issue invoice
  -> record/review payment
  -> publish approved state to Control Cloud
  -> issue signed entitlement or command
  -> SafarSuite local server/app applies access rules
  -> Control Desk and Client Portal show status
```

## System Responsibilities

| System | Owns | Does Not Own |
| --- | --- | --- |
| SafarSuite Control Desk | Provider office source of truth for clients, contracts, pricing, billing, payments, accounting, module/device/branch limits, support notes, and entitlement decisions | Client portal passwords, hosted client business data sync, or deployed SafarSuite runtime state |
| SafarSuite Control Cloud | Online control plane for accepted commercial projections, signed entitlement bundles, local-server setup/registration, command queue, heartbeat/status, portal state, and audit | Original accounting truth or direct editing of client contracts/pricing without Control Desk approval |
| SafarSuite Client Portal | Client-facing view of invoices, payment state, subscription/license status, setup packages, renewal files, and permitted self-service | Provider-only accounting decisions or local SafarSuite feature enforcement |
| SafarSuite local server/app | Client business runtime, cached signed entitlement verification, module/limit enforcement, heartbeat, command pull/acknowledgement, diagnostics export | Pricing, billing policy, portal identity, or provider commercial decisions |

## Control Desk Goal

SafarSuite Control Desk is our internal desktop app for office use. The browser-hosted React UI is a development surface; the final product should be packaged as a desktop app.

Control Desk must help the provider:

- maintain clients, contacts, notes, contracts, and lifecycle status
- define custom pricing and module plans per client
- manage invoices, payments, reversals, credit notes, refunds, and settlements
- keep accounting/receivables consistent with client billing
- decide which modules, users, devices, and branches a client may use
- publish approved commercial/control events to Control Cloud through a durable outbox
- show the current portal/license/installation status without becoming the cloud itself

## Control Cloud Goal

SafarSuite Control Cloud is the single production online control plane.

Control Cloud must:

- receive signed approved events from Control Desk
- project client-facing invoice, payment, credit, refund, paid-status, and entitlement state
- own Client Portal identity/session boundaries
- issue signed entitlement bundles and offline renewal files
- bind entitlements to registered installations
- queue signed local-server commands and store acknowledgements
- receive local-server heartbeat/license reports separately from license validity
- expose installation status to Control Desk and permitted portal users
- audit every setup, entitlement, command, heartbeat, renewal, invite, and support override

The older SafarSuite workspace CloudServer is reference material only. It is not a second production cloud.

## SafarSuite Goal

SafarSuite is the client product. It runs the client's real office/business workflows.

SafarSuite must not hard-code provider pricing, subscription state, or client-specific module access. It should consume signed entitlements and enforce them locally.

SafarSuite should:

- verify signed entitlement bundles without requiring internet at runtime
- enable or disable modules based on entitlement state
- enforce user, device, branch, and feature limits
- keep working through the paid offline-valid period
- warn near renewal, then move through grace/restricted/expired policy states
- pull commands and entitlement updates when internet is available
- report heartbeat and local license state to Control Cloud
- import signed offline renewal files when direct internet is unavailable near expiry

## Module Control

SafarSuite is module-composed. Each client can have a different enabled module set and different limits.

Example:

```text
Client A
  Core: active
  Payroll: active
  Tour: inactive
  allowed devices: 5
  allowed branches: 2

Client B
  Core: active
  Payroll: inactive
  Tour: active
  allowed devices: 2
  allowed branches: 1
```

The module-control chain is:

```text
Control Desk product module catalog
  -> client contract module allowances
  -> invoice/billing rules where needed
  -> entitlement snapshot
  -> Control Cloud signed entitlement bundle
  -> SafarSuite local module gates
```

Rules:

- Control Desk defines and approves module access.
- Control Cloud signs and distributes module access.
- SafarSuite enforces module access.
- Client Portal displays module/subscription state but does not become the source of truth.
- Module access must support included-for-all modules and paid add-ons.
- A deployed SafarSuite feature should check entitlement/module state before exposing paid or restricted behavior.

See `docs/architecture/product-module-catalog-boundary.md` for the catalog boundary.

## Communication Boundaries

Use these boundaries to avoid drift:

| Direction | Purpose |
| --- | --- |
| Control Desk -> Control Cloud | Signed outbox messages for approved commercial/control changes |
| Control Cloud -> Client Portal | Client-facing projections, signed entitlement downloads, setup/renewal visibility |
| SafarSuite local server -> Control Cloud | Registration, entitlement pull, heartbeat, command pull, command acknowledgement, diagnostics upload |
| Control Cloud -> SafarSuite local server | Signed bundles, signed commands, bootstrap/setup artifacts |
| Control Desk/Client Portal -> Control Cloud | Shared installation status reads |

Do not call Control Cloud inside accounting transactions. Control Desk should enqueue durable outbox messages and publish them separately.

## Client Business Data Sync Is Separate

Do not mix these systems:

```text
SafarSuite Control Cloud
  billing, licensing, portal, entitlements, setup, commands, audit

Client Data Sync Cloud
  optional sync of the client's operational SafarSuite business data
```

Client business-data cloud sync may be offered by plan later, but it is not required for Control Desk billing/licensing to work.

Deployment and sync vocabulary is defined in:

```text
docs/architecture/client-deployment-and-data-sync-boundary.md
```

## What We Are Not Building First

- We are not recreating all legacy Survey/FAS forms before proving the client-control chain.
- We are not merging Control Desk into the SafarSuite client product.
- We are not making client business-data cloud sync mandatory.
- We are not making missed heartbeat equal license failure.
- We are not storing client server root credentials or relying on inbound access to client offices.
- We are not hard-coding one payment provider or one final module lineup into the core domain.

## Drift Check

When adding a feature, answer these questions before coding:

1. Is this a provider commercial decision, cloud control-plane action, portal user action, or SafarSuite runtime behavior?
2. Which system is the source of truth?
3. Does the data move through the signed outbox, a portal/session boundary, a local-server pull, or a local runtime cache?
4. Does this affect modules, devices, branches, users, paid dates, heartbeat, or offline validity?
5. Is this client business-data sync? If yes, keep it separate from Control Cloud billing/licensing.

If the answer is unclear, stop and update this document or the relevant boundary doc before expanding implementation.
