# Client Portal Identity Boundary

Date added: 2026-07-02

This note records how SafarSuite clients should receive Client Portal access.

## Decision

Creating a client in SafarSuite Control Desk must not directly create a plain password.

Control Desk owns the client record, contacts, contracts, invoices, payments, and accounting workflow.

SafarSuite Control Cloud owns Client Portal identities, sessions, roles, invitation tokens, password reset, MFA later, and portal audit.

## Intended Flow

```text
Control Desk creates or updates client
  -> Control Desk publishes client/commercial state to Control Cloud
  -> provider user chooses which client contact gets portal access
  -> Control Cloud creates a one-time invitation
  -> client contact accepts invite, sets credentials, and gets a role
  -> portal session is scoped to that client
```

## Roles

Initial roles should stay simple:

| Role | Purpose |
| --- | --- |
| ClientOwner | Manage client portal users and see billing/license state |
| ClientBilling | See invoices, balances, payments, and renewal information |
| ClientTechnical | See deployment, heartbeat, license, and setup status |
| ClientViewer | Read-only commercial/license visibility |

Provider support/admin users are separate from client users.

## Current Implementation

The current code has the first invitation and session boundary:

```text
POST /api/v1/clients/{clientId}/contacts/{clientContactId}/portal-invitation
POST /api/v1/client-portal/invitations
POST /api/v1/client-portal/invitations/accept
POST /api/v1/client-portal/sessions
```

The Control Desk client profile can request a portal invitation for a selected contact. Control Desk validates the local client/contact, maps the contact role to a simple portal role, and calls Control Cloud with the provider invitation key.

Control Cloud requires `X-SafarSuite-Provider-Key` before creating an invitation. This is a basic internal/provider gate for the local phase, not the final provider-user authorization model.

Invitation creation stores only a hash of the one-time invitation token.

Invitation acceptance creates a client-scoped portal user with a hashed password.

```text
email + password
```

The login response is a signed, expiring bearer token scoped to one client.

```text
GET /api/v1/client-portal/clients/{clientId}/commercial-summary
GET /api/v1/client-portal/clients/{clientId}/entitlement-bundle?installationId={installationId}
GET /api/v1/client-portal/clients/{clientId}/installations/{installationId}/status
```

The minimal portal preview can accept an invite token, set a password, and then log in with email/password.

Local-server entitlement renewal does not use a human portal session. It stays on the machine-facing route:

```text
GET /api/v1/local-server/installations/{installationId}/entitlement-bundle?clientId={clientId}
```

## Pending Hardening

- Real provider/admin users and role checks before creating invitations
- Email delivery for invitation links
- Invitation resend/revoke/list endpoints
- Session audit records
- Password reset
- MFA
- Rate limiting and lockout

## Guardrails

- Portal credentials are cloud-owned, not stored in the Control Desk operational database.
- Control Desk may request or display invitation status later, but Control Cloud issues and validates credentials.
- Portal sessions must be scoped to a client id.
- Client users must not be allowed to read another client's commercial, license, deployment, or heartbeat data.
- Local server entitlement pull remains a machine-to-cloud path and should not depend on a human Client Portal session.
- Payment/self-service actions must wait until provider/admin authorization, role checks, session audit, and payment-specific audit are in place.
