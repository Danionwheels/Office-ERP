# REC-05 Client Characterization

Date: 2026-07-22

This is a read-only characterization of the existing client API. It does not introduce the new public gate or change runtime behavior.

## Existing client route surface

`ClientEndpoints` currently exposes 21 routes under `/api/v1/clients`, all protected by `ClientsManage`:

- create, list, get, update, activate, suspend;
- contacts and portal invitations;
- support notes;
- accounting profile;
- deployments;
- financial summary, invoices, payments, financial activity, and journal postings.

## Minimal V1 slice to preserve first

The first replacement workspace needs only:

1. `POST /api/v1/clients/` — create company identity;
2. `GET /api/v1/clients/` — search/list client summaries;
3. `GET /api/v1/clients/{clientId}` — select and read a client;
4. `PUT /api/v1/clients/{clientId}` — update company identity;
5. `POST/GET /api/v1/clients/{clientId}/contacts` — maintain the primary contact.

Activation, suspension, support notes, accounting, deployment, financial, and portal-invitation routes remain retained implementation but are outside the first workspace. They must not be imported into the new Clients public gate.

## Boundary target

The replacement gate returns deliberate client summaries/details and stable identifiers. It must not expose EF entities, repositories, accounting records, deployment records, invoice/payment types, or direct Control Cloud calls. Audit and client-projection outbox work will be added only when the first protected write is implemented.
