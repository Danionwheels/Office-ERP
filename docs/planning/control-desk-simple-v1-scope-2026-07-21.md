# SafarSuite Control Desk — Simple V1 Scope

**Date:** 2026-07-21  
**Status:** Proposed scope lock for the first usable office release

This document narrows the first usable product without changing the accepted
deployment boundary in [`final-system-requirements-and-deployment-contract.md`](../architecture/final-system-requirements-and-deployment-contract.md).
It is a scope reset, not a repository restart.

## Product outcome

An operator installs SafarSuite Control Desk on one dedicated Windows office PC
and can manage clients, client-specific prices, invoices, receivables, module
access, and client portal access from one desktop application. When the office
has internet, approved changes are delivered to SafarSuite Control Cloud. When
the connection is unavailable, the office remains usable and queued changes are
sent automatically after reconnection.

Clients use the SafarSuite Client Portal for their own account, invoice,
payment, and license information. Control Cloud and the Client Portal remain
separate cloud components; they are not installed on the office PC.

## V1 capabilities

### 1. Client management

- Create, edit, search, and deactivate a client/company.
- Store contacts, billing details, status, notes, and audit history.
- Select a client as the starting point for all commercial work.

### 2. Client-specific pricing

- Maintain a small product/module catalog with a default price.
- Override price, quantity, discount, currency, and effective date per client.
- Freeze the chosen prices into the approved contract/invoice; later catalog
  edits must not rewrite historical invoices.

### 3. Invoice and receivables

- Create and issue an invoice from an approved client price selection.
- Calculate subtotal, discount, tax (when enabled), total, and outstanding
  balance.
- Record payment status and payment references.
- Keep balanced provider accounting evidence internally, while exposing only a
  simple receivables view in the V1 UI.

### 4. Module and license control

- Select enabled modules and limits for each client.
- Approve a desired-access revision with effective dates and offline validity.
- Publish a signed entitlement through the durable outbox.
- Show desired, cloud-delivered, and client-observed status separately.

### 5. Client portal access

- Create a client portal identity tied to the client record.
- Support invitation, login, password reset, session expiry, and audit evidence.
- Permit the client to view invoices, receivables/payment status, profile data,
  and active modules/license status.
- Keep provider pricing, contract approval, and entitlement authority in
  Control Desk; the portal is a self-service projection, not the source of
  commercial truth.
- Use Brevo or another transactional provider later for invitation and reset
  email. Email delivery is a cloud-lane configuration, not a Control Desk
  installation prerequisite.

### 6. Cloud synchronization

- Push approved, versioned changes immediately when online.
- Queue them durably and retry idempotently after an outage.
- Retain delivery, acknowledgement, and observed-state evidence.
- Never require inbound access, public DNS, SMTP, or a Linux host for the office
  application.

### 7. Installable office product

- One Windows setup installs the desktop UI, local API, local PostgreSQL, and
  required services on the dedicated office PC.
- The setup creates the first operator, starts services automatically, and
  exposes a normal desktop shortcut.
- Routine use must not require Docker, PostgreSQL administration, a command
  prompt, or a second office server.

## Deliberately deferred from Simple V1

These capabilities may remain in the repository and underlying domain where
they support the V1 chain, but they are not release requirements for the first
usable UI:

- Full chart-of-accounts maintenance, period close, advanced reports, voucher
  register administration, opening-balance import, and mutating repair tools.
- Complex pricing-rule engines, automated tax jurisdiction logic, promotions,
  bundles, and multi-company accounting.
- Client portal authority to approve provider contracts, prices, or
  entitlements.
- Public hosting of Control Desk, a separate office server, Linux hosting for
  the office application, router forwarding, and office SMTP configuration.
- Survey/FAS work and unrelated legacy-module cloning.

## Minimum connected acceptance chain

The first release is accepted only when this small vertical slice works from a
clean install:

1. Operator creates a client.
2. Operator selects modules and a client-specific price.
3. Operator approves the contract/pricing selection.
4. Operator issues an invoice and sees the receivable balance.
5. Operator creates or invites the client portal identity.
6. Control Desk queues and publishes the approved access/invoice projection.
7. Control Cloud accepts it idempotently and issues the signed entitlement.
8. SafarSuite local server verifies and applies the entitlement.
9. The office sees delivery and observed status, including the offline/retry
   case.

The evidence must retain IDs, invoice totals, entitlement version, delivery
status, and audit records. Advanced accounting screens are not required for this
proof; balanced journal evidence is required behind the scenes.

## Delivery decision

Do **not** restart the repository. Keep the tested domain, auth, outbox,
entitlement, cloud, portal, and accounting foundation. Work from this scope in
small vertical slices:

1. Stabilize the Windows installer/runtime compatibility and complete the
   one-PC installation proof.
2. Hide or defer advanced UI areas and make the client-first workflow the
   default navigation.
3. Prove the minimum connected acceptance chain from the installed product.
4. Rehearse backup/restore, outage/retry, update rollback, and clean-PC setup.
5. Add advanced accounting and portal features only after Simple V1 is usable.

This scope is intentionally smaller than the implementation underneath it. A
smaller user experience is safer than deleting the already-tested security and
data-boundary foundations.
