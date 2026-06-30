# Current Cloud Server Reuse Assessment

Date: 2026-06-30

Source project:

```text
C:/Users/Daniyal/Documents/Codex/2026-06-09/hello-there-2/src/CloudServer
```

## Short Answer

The current CloudServer is useful and should not be thrown away.

It is not yet the full SafarSuite Control Cloud, but it already contains the security foundation we need:

- activation requests
- signed activation tokens
- signed first-manager setup tokens
- signed product-kernel commands
- owner dashboard authentication
- owner action audit logging
- local server activation-state import
- module entitlement snapshot import
- command replay protection on the local server

The right move is to reuse the ideas and contracts, then harden/persist/extend them inside the new SafarSuite Control Cloud.

## What Exists Today

### Activation Requests

Current endpoint group:

```text
/api/activation/server-requests
```

Current behavior:

- local server submits activation request
- cloud stores request in `ActivationRegistry`
- owner can list/read pending requests
- owner can approve or block
- approval returns a signed activation token

Useful for SafarSuite Control Desk because activation should ultimately be tied to the client contract and renewal status.

### Signed Activation Tokens

Current signer:

```text
src/Shared/Security/ActivationTokenSigner.cs
```

The token includes:

- activation request id
- local server installation id
- fingerprint hash
- local server public key placeholder
- tenant/branch ids
- customer code/name
- branch name
- entitlement status
- paid-until date
- grace date
- offline-valid-until date
- module entitlements
- signing key id
- not-before/expires-at validity window

This is exactly the shape we need for offline and semi-online clients.

### Local Server Trust

Current local service:

```text
src/LocalServer/Modules/Platform/Trust/LocalServerActivationService.cs
```

The local server:

- creates/stores a server installation identity
- computes a machine fingerprint hash
- requests activation
- verifies signed activation tokens
- binds tokens to server installation id, fingerprint, and public key
- imports entitlement data
- blocks writes when activation state is not active

This is very valuable. It means SafarSuite already has a control point where provider-issued entitlements can be enforced.

### Product Kernel Commands

Current endpoint:

```text
/api/product-kernel/vendor-commands
```

Current command types include:

- set module enabled
- set entitlement snapshot

This is the right direction for future changes such as:

- enable/disable cloud sync
- adjust allowed device count
- renew entitlement
- move to read-only/suspended
- enable hosted SaaS features

### Owner Dashboard And Audit

Current code supports:

- owner login
- roles
- auth-check
- operator management
- session revocation
- owner action audit log

This can evolve into the admin side of SafarSuite Control Cloud, but SafarSuite Control Desk should become the primary internal operator UI.

## Current Gaps

### In-Memory Activation Registry

`ActivationRegistry` stores activation requests in memory. That is okay for a prototype, but production needs PostgreSQL persistence.

Required tables:

- clients
- contracts
- subscriptions
- invoices
- receipts/payments
- activation_requests
- entitlement_snapshots
- product_kernel_commands
- device_allocations
- branch_allocations
- audit_events

### No Billing Model Yet

The current cloud has activation and entitlement primitives, but not:

- custom pricing per client
- billing cycles
- invoice generation
- payment provider callbacks
- bank transfer review
- receipt reconciliation
- arrears/grace policy

Those belong primarily in SafarSuite Control Desk, with cloud mirrors for the portal.

### Subscription State Is Stubbed

The current endpoint:

```text
/api/tenants/{tenantId}/subscription-state
```

currently returns active status by default. This must become real and derive from SafarSuite Control Desk / SafarSuite Control Cloud subscription records.

### Device Revocations Are Stubbed

The current endpoint:

```text
/api/branches/{branchId}/device-revocations
```

currently returns an empty list. This should become a real device policy endpoint.

### Sync Gateway Is Only A Start

The current sync gateway has:

```text
/api/sync/branches/{branchId}/events
```

with an in-memory event store.

This is useful as a proof of concept, but client business-data sync should be its own data-plane module, separate from billing/license control.

## Recommended Reuse Plan

### Keep

- signed activation-token design
- signing-key model
- activation request lifecycle
- local activation enforcement
- entitlement snapshot shape
- product-kernel command mechanism
- owner audit concept
- owner dashboard auth roles as a starting point

### Replace Or Upgrade

- in-memory activation registry -> PostgreSQL-backed registry
- development signing keys -> production key storage and rotation
- public key placeholder -> real local server key pair
- hard-coded subscription endpoint -> subscription service
- empty device revocations -> device allowance/revocation service
- single CloudServer role -> split by module boundaries inside one modular monolith

### Add

- SafarSuite Control Desk sync API
- client portal account model
- invoice mirror
- payment gateway adapter
- online bank transfer proof/review flow
- entitlement issuance based on paid status
- allowed device/user/branch limits
- plan/module catalog
- SaaS renewal flow
- hosted SaaS tenant provisioning hooks

## Suggested Future Shape

Keep one SafarSuite Control Cloud modular monolith:

```text
ProductControlCloud
  ProviderApi
  ClientPortal
  Billing
  Payments
  Contracts
  Entitlements
  Activation
  DevicePolicy
  BranchPolicy
  SaaSProvisioning
  Audit
  OwnerIdentity
```

Keep client business-data sync separate:

```text
SafarSuiteDataSync
  BranchEventIngest
  HQAggregation
  CloudDataStore
  OwnerMobileDashboardApi
```

They can run in the same deployed app at first, but the code boundaries should stay separate.

## Decision

Use the current CloudServer as the seed for SafarSuite Control Cloud, especially activation and signed entitlements.

Do not make SafarSuite Control Desk depend directly on the current in-memory CloudServer implementation. SafarSuite Control Desk should talk to a stable SafarSuite Control Cloud API, and the current CloudServer should be upgraded until it provides that API.
