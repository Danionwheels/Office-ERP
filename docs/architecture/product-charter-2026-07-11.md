# SafarSuite Control Platform Product Charter

Date accepted: 2026-07-11

Status: Canonical. This document has authority over older planning, UI, legacy-clone, and implementation notes when they conflict.

Deployment authority: `final-system-requirements-and-deployment-contract.md` is canonical for final component placement and deployment acceptance. In particular, SafarSuite Control Desk V1 runs on one dedicated office PC; Linux/cloud infrastructure is not its production host.

## Product Decision

Build one connected provider-control system for operating SafarSuite at scale:

```text
Dedicated office PC
  -> SafarSuite Control Desk desktop application
  -> local Office Control API and local PostgreSQL
  -> SafarSuite Control Cloud
  -> SafarSuite Server at each client
  -> observed state returns through Control Cloud to Control Desk
```

The office control system is the source of truth for commercial and product-access decisions. Control Cloud distributes approved state. SafarSuite Server enforces that state locally and reports what it actually applied.

The product is not a legacy Survey/FAS clone, a generic CRM, a deployment dashboard, or a store for client operational business transactions.

## Problem We Are Solving

SafarSuite clients can have different:

- contracts and custom prices
- enabled products and modules
- user, device, branch, and feature limits
- payment terms, paid-through dates, grace rules, and support arrangements
- deployment modes and one or more registered installations

The provider office needs one durable system that can answer:

1. What did this client buy?
2. What have they been billed and what have they paid?
3. What access should they have now?
4. Which entitlement version was approved and distributed?
5. What is each SafarSuite Server actually enforcing?
6. Who changed any of this, when, and why?

## System Vocabulary

`Office Control System` means the complete authoritative office-side product:

- SafarSuite Control Desk desktop UI
- Office Control API/application layer
- PostgreSQL on the dedicated office PC
- provider accounting, commercial, control, and audit rules

The desktop application is the primary operating experience. For V1, one dedicated office PC hosts the local Office Control API and authoritative PostgreSQL database; no separate office server is required. Durability comes from controlled service lifecycle, automated backup, an off-PC second copy, and clean-machine restore evidence rather than moving the office authority to Linux or the public cloud.

## Ownership Boundaries

| System | Owns | Must Not Own |
| --- | --- | --- |
| Office Control System | Clients, contracts, pricing, invoices, payments, provider accounting, product catalog, desired module/limit state, entitlement approvals, support history, and provider audit | Client operational transactions or deployed runtime state |
| SafarSuite Control Cloud | Accepted projections, portal identity and views, entitlement/command signing and distribution, installation registry, delivery state, heartbeat, acknowledgements, and observed-state projection | Independent editing of commercial truth |
| SafarSuite Server | Local SafarSuite business runtime, cached signed entitlement verification, module and limit enforcement, local availability, and runtime observations | Pricing, billing policy, or provider commercial decisions |
| Client Data Platform | Optional future branch/HQ/cloud synchronization of client operational data | Provider billing, licensing, and entitlement authority |

No system shares another system's database. State crosses boundaries through explicit, versioned contracts.

## Source Of Truth Model

The Office Control System owns desired commercial and control state.

```text
Desired state
  client X may use Accounting and Payroll
  maximum 20 users, 5 devices, and 3 branches
  paid through 2027-01-31
  entitlement version 184 approved
```

SafarSuite Server owns observed runtime state.

```text
Observed state
  entitlement version 184 applied
  Accounting and Payroll active
  17 users and 4 devices observed
  last successful heartbeat at a recorded time
```

Control Cloud carries and projects both directions, but does not resolve commercial conflicts. A failed or delayed cloud delivery never silently changes the approved office state.

## Connected Control Contract

All approved office changes move through a durable outbox. Delivery is asynchronous, idempotent, retryable, and auditable.

All entitlement and control artifacts are:

- immutable after issue
- client- and installation-bound where required
- monotonically versioned
- signed by an approved Control Cloud key
- independently verifiable by SafarSuite Server
- safe to cache for the approved offline-valid period
- acknowledged with the applied or rejected version

Heartbeat health and license validity remain separate. A paid offline-capable client continues through the signed paid/offline-valid period even when Control Cloud is temporarily unreachable.

## Dynamic Product And Module Control

Dynamic control is data-driven, not implemented through client-specific code branches.

```text
Versioned product/module catalog
  -> client contract and commercial terms
  -> approved desired access state
  -> immutable entitlement version
  -> signed Control Cloud bundle
  -> SafarSuite Server module and limit gates
```

A module definition has a stable code, lifecycle state, compatibility metadata, and optional billing defaults. A client contract selects modules and limits. Changing a client's access creates a new approved version; it never rewrites an entitlement already issued.

Module entitlement enables code already present in a compatible SafarSuite release. Shipping new executable capability remains a software deployment concern, not an entitlement toggle.

## Scale Boundary

The Office Control System is designed for many clients, contracts, invoices, payments, installations, entitlement versions, heartbeats, commands, support events, and years of audit history.

It does not ingest every client's operational SafarSuite transactions. That data remains on SafarSuite Server or moves through a separately designed client-data platform.

Scale rules from the beginning:

- PostgreSQL on the dedicated office PC is the authoritative office database
- all large registers use server-side filtering, sorting, and pagination
- client, contract, installation, status, and effective-date keys are indexed deliberately
- entitlement, delivery, acknowledgement, and audit histories are append-only
- files and large diagnostic artifacts use object/file storage with database metadata
- summary screens use explicit read models instead of loading complete histories
- retention, archival, and partitioning are introduced from measured volume, not guessed prematurely
- application modules remain a modular monolith until independent deployment is justified

## Primary Operating Flow

```text
Create client
  -> define contract, custom pricing, modules, and limits
  -> issue invoice and accounting proof
  -> record or approve payment
  -> approve desired access state
  -> issue a new entitlement version
  -> publish through the durable outbox
  -> Control Cloud validates, signs, and distributes
  -> SafarSuite Server verifies and applies
  -> server acknowledges the version and reports observed state
  -> Control Desk shows desired versus observed state
```

This is the first product acceptance chain. Work that does not support or safely operate this chain is deferred.

## Control Desk Experience

Control Desk is a serious desktop operating tool for provider staff.

- Organize normal work around real business workflows and client context.
- Keep contracts, money, access, installation, and support connected without turning the product into a generic CRM.
- Keep accounting traceable and accountant-readable while hiding unnecessary GL mechanics from routine actions.
- Show desired state, delivery state, and observed state as different facts.
- Use dense, searchable registers and focused workspaces for repeated office work.
- Keep technical deployment and security detail available to authorized operators, but out of the normal commercial path.
- Do not make infrastructure terminology the primary navigation model.

The recent Command Center, Setup, Client 360, and Legacy Desk shell is exploratory work, not an accepted product structure.

## Legacy Software Decision

Legacy Survey/FAS and Travel software are research evidence only.

Use them to identify necessary accounting controls, correction flows, audit expectations, reports, and migration requirements. Do not preserve their forms, navigation, schema, terminology, or implementation unless a current SafarSuite provider workflow independently requires the behavior.

The result matters more than legacy parity.

## V1 Acceptance

V1 is accepted when an authorized office operator can complete the primary operating flow for a real test client and the system proves:

- one authoritative client, contract, price, module, and limit state
- explainable invoice, payment, and provider accounting records
- a durable published entitlement version
- Control Cloud acceptance and signed distribution
- SafarSuite Server verification and enforcement
- offline-safe behavior within policy
- acknowledgement and observed state returned to Control Desk
- complete audit from office decision to server application
- safe authenticated use by authorized operators on the dedicated office PC; concurrent multi-PC hosting requires a later approved topology
- backup and restore of authoritative office data

## Explicit Non-Goals For V1

- cloning the complete legacy office system
- storing client operational business transactions in Control Desk
- building client-data cloud synchronization
- microservices for each module
- a generic CRM activity platform
- making deployment proofs the main Control Desk experience
- arbitrary remote code execution through the command channel
- optimizing for unmeasured hyperscale

## Drift Test

Before accepting work, answer:

1. Which product outcome or V1 acceptance item does this support?
2. Which system owns the authoritative state?
3. Is this desired state, delivery state, or observed state?
4. Is the change versioned, auditable, retryable, and safe when offline?
5. Does this introduce client operational data into the control plane?
6. Is infrastructure becoming more visible than the operator's business task?

If ownership or product value is unclear, stop and update this charter before implementation.
