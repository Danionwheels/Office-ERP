# SafarSuite Final System Requirements And Deployment Contract

Date accepted: 2026-07-18

Status: Canonical for final product scope, deployment topology, component placement, and release acceptance.

## Purpose And Authority

This document exists to prevent product and deployment drift. It defines the final system that SafarSuite work must produce, where each component is allowed to run, and the evidence required before a deployment can be called complete.

Authority is applied in this order:

1. This document governs final product scope, deployment topology, component placement, and deployment acceptance.
2. [The product charter](product-charter-2026-07-11.md) governs business ownership, desired-versus-observed state, and the connected control model.
3. [The active roadmap](../planning/active-roadmap-2026-07-11.md) governs implementation order and current status.
4. Runbooks and Compose files implement these decisions; they do not redefine them.

If an implementation, runbook, test environment, or older planning note conflicts with this document, stop. Do not silently reinterpret the requirement. Record and approve a decision here before changing the system.

## Final Goal

The provider office must be able to operate **SafarSuite Control Desk completely from one dedicated office PC**.

That means:

- no separate office server machine is required;
- no Linux machine is required to operate Control Desk;
- no public domain, inbound port forwarding, public HTTPS endpoint, or SMTP provider is required for the office application itself;
- the Control Desk UI, Office Control API process, and authoritative office PostgreSQL database run on the same office PC;
- routine office use must not require an operator to manage Docker, PostgreSQL, command prompts, certificates, or service configuration;
- Control Desk uses outbound HTTPS when it needs SafarSuite Control Cloud and remains usable for its core office work during a cloud outage;
- SafarSuite Control Cloud, the SafarSuite Client Portal, and client-side SafarSuite runtimes remain separate systems with separate deployment requirements.

The completed platform must prove this closed loop without changing those placement rules:

```text
Dedicated office PC
  SafarSuite Control Desk desktop UI
  + local Office Control API/background process
  + local authoritative office PostgreSQL
      |
      | outbound signed HTTPS messages and status reads
      v
Cloud host
  SafarSuite Control Cloud
  + SafarSuite Client Portal
  + private cloud PostgreSQL
  + public HTTPS and outbound email
      |
      | signed entitlements, commands, heartbeats, acknowledgements
      v
Client premises
  SafarSuite local server/app
  + client operational database
```

## Terminology Guardrail

| Term | Meaning in this repository |
| --- | --- |
| Dedicated office PC | The single provider-office computer that runs Control Desk V1 and its local dependencies. |
| Office Control API/background process | Software running on the dedicated office PC. The word `API` does not imply a separate physical server or public website. |
| Office PostgreSQL | A database process on the dedicated office PC, bound locally and managed by the product lifecycle. The word `database server` does not authorize another machine. |
| Cloud host | Separate controlled Linux/cloud infrastructure for Control Cloud and the Client Portal. |
| SafarSuite local server/app | The client-premises runtime that enforces access. It is not the provider-office Control Desk backend. |
| Disposable Integration Lab | A demo-data test environment that may co-locate components temporarily but cannot redefine production placement. |

## Canonical Component Placement

| Component | Required location | Network exposure | Explicitly prohibited placement or role |
| --- | --- | --- | --- |
| SafarSuite Control Desk desktop UI | Dedicated office PC | Local machine only by default | Public website or Linux-hosted office application |
| Office Control API/application process | Same dedicated office PC | Loopback/local IPC by default | Separate physical office server for V1; public internet endpoint |
| Authoritative Office Control PostgreSQL | Same dedicated office PC | Loopback only | Cloud database; Linux test machine; shared database with Control Cloud |
| SafarSuite Control Cloud API | Controlled cloud/Linux host | Public HTTPS through a reverse proxy | Originating or editing office commercial truth |
| SafarSuite Client Portal | Same controlled cloud boundary as Control Cloud | Public HTTPS | Direct access to the office database |
| Control Cloud PostgreSQL | Private cloud-host network/loopback | Never public | Shared database with Control Desk or a client runtime |
| SMTP or transactional email provider | Control Cloud integration only | Outbound from Control Cloud | Control Desk runtime requirement |
| Public DNS and TLS termination | Control Cloud and Client Portal only | Public `80/443` as required | Requirement for the office Control Desk PC |
| SafarSuite local server/app | Client premises, or a disposable client-runtime test machine | Outbound to Control Cloud; local client access as designed | Provider commercial authority |
| Linux test machine | Cloud staging, client-runtime proof, or explicitly labelled disposable integration lab | Only what its selected test role requires | Final Control Desk office host |

## Deployment Topology Requirements

| ID | Requirement | Acceptance evidence |
| --- | --- | --- |
| `TOP-001` | Control Desk V1 shall install and operate on one dedicated office PC. | Fresh-PC installation proof with no dependency on another office machine. |
| `TOP-002` | Control Desk V1 shall not require a separate physical or virtual office server. | The UI, local API process, and office database are healthy on the designated PC after reboot. |
| `TOP-003` | Office API and database listeners shall bind to loopback or an equally restrictive local transport by default. | Port/listener audit proves they are not publicly reachable. |
| `TOP-004` | Control Desk shall initiate outbound cloud communication; Control Cloud shall not require inbound access to the office PC. | Connected-chain proof passes with no router forwarding to the office PC. |
| `TOP-005` | Linux shall not be a production dependency for Control Desk. | Office deployment remains healthy with the Linux test machine powered off. |
| `TOP-006` | A lab may co-locate components only when labelled `Disposable Integration Lab`, using demo data, and never as proof of the final office topology. | Lab documentation contains the label and links back to this contract. |
| `TOP-007` | Office, cloud, and client-runtime databases shall remain physically and logically separate. | Configuration and connection audits show no shared database or schema authority. |
| `TOP-008` | DNS, HTTPS certificates, reverse proxying, and SMTP belong to the cloud/portal deployment lane. | Office-only installation succeeds without those values; cloud preflight requires them. |

## SafarSuite Control Desk Requirements

### Product And Workflow

| ID | Requirement |
| --- | --- |
| `CD-001` | The desktop application is the primary provider-office operating experience. |
| `CD-002` | It shall manage clients, contacts, contracts, custom pricing, billing, payments, provider accounting, product catalog, desired access, support history, and audit evidence. |
| `CD-003` | It shall show desired state, cloud delivery state, and client-observed state as separate facts. |
| `CD-004` | Normal work shall be organized around client and business workflows, not infrastructure terminology. |
| `CD-005` | It shall never store client operational business transactions that belong to the deployed SafarSuite product. |

### One-PC Operation

| ID | Requirement |
| --- | --- |
| `CD-101` | Installation shall provision or verify every local prerequisite without requiring a separate server. |
| `CD-102` | The Office Control API and database shall start automatically and recover after an office-PC reboot. |
| `CD-103` | Routine users shall open Control Desk through a desktop entry point and shall not manage background infrastructure manually. |
| `CD-104` | Local service health shall be visible through an authorized diagnostics surface without exposing secrets. |
| `CD-105` | The designated office PC shall remain authoritative during temporary Control Cloud or internet outages. |
| `CD-106` | Cloud-bound work shall use a durable, idempotent, retryable outbox and shall resume automatically after connectivity returns. |
| `CD-107` | V1 requires one dedicated office PC. Concurrent multi-PC office hosting is out of scope until explicitly approved here. Different authorized operators may use the designated PC according to roles and audit policy. |
| `CD-108` | The supported office operating system, CPU, memory, disk, and free-space minimums shall be documented and proven before release. |
| `CD-109` | Local API and database processes shall shut down cleanly, recover safely after interrupted shutdown, and expose actionable startup failure evidence. |

### Security, Data, And Recovery

| ID | Requirement |
| --- | --- |
| `CD-201` | Every office action shall require an authenticated operator and enforce explicit roles/scopes. |
| `CD-202` | The application shall retain actor, time, reason, source document, and revision evidence for protected commercial and access changes. |
| `CD-203` | Secrets and database credentials shall not be committed, displayed in diagnostics, or stored in ordinary application logs. |
| `CD-204` | The office database shall have an automated local backup schedule and a documented second-copy/off-PC backup destination. |
| `CD-205` | Restore shall be rehearsed onto a clean replacement PC before production acceptance. A backup without a successful restore proof is not sufficient. |
| `CD-206` | Desktop and local-service updates shall be versioned, integrity checked, recoverable, and capable of rolling back when a migration or startup fails. |
| `CD-207` | Office data shall not be copied to Control Cloud except through explicitly approved projections and versioned contracts. |
| `CD-208` | Operator credentials, local secrets, recovery procedures, and administrative access shall have explicit custody and replacement rules. |
| `CD-209` | Office backup frequency, recovery-point objective, and recovery-time objective shall be accepted and verified against a replacement-PC exercise. |

## SafarSuite Control Cloud And Client Portal Requirements

| ID | Requirement |
| --- | --- |
| `CLD-001` | Control Cloud shall run separately from the office PC on controlled cloud/Linux infrastructure. |
| `CLD-002` | It shall accept signed, versioned office projections but shall not independently originate commercial decisions. |
| `CLD-003` | It shall sign and distribute entitlements and commands, retain delivery evidence, and project acknowledgements and observed state. |
| `CLD-004` | It shall expose only authorized HTTPS endpoints through controlled DNS and TLS configuration. |
| `CLD-005` | Its PostgreSQL database shall be private, backed up, monitored, and independently restorable. |
| `CLD-006` | Signing keys and provider-access secrets shall have documented custody, rotation, replacement, and compromise procedures. |
| `CLD-007` | SMTP or a transactional provider such as Brevo shall be used only for Client Portal invitations, password resets, and other explicitly approved cloud-originated messages. |
| `CLD-008` | The Client Portal shall remain a client-facing projection and self-service boundary, not a source of commercial truth. |
| `CLD-009` | Portal identity, invitation, password-reset, MFA, session, rate-limit, and audit controls shall fail closed outside Development. |
| `CLD-010` | Control Desk and Control Cloud contracts shall be versioned and compatibility-tested so either side can be upgraded without silently corrupting or reinterpreting state. |
| `CLD-011` | Development, staging, and production cloud data, credentials, keys, email identities, and DNS names shall remain separated. |

## SafarSuite Local Server/App Requirements

| ID | Requirement |
| --- | --- |
| `LS-001` | The local server/app shall run at client premises and enforce signed access locally. |
| `LS-002` | It shall not make provider pricing, billing, contract, or entitlement-approval decisions. |
| `LS-003` | It shall remain operational through the approved offline-valid period without treating a missed heartbeat as immediate license failure. |
| `LS-004` | It shall communicate outbound to Control Cloud for registration, entitlement pull, heartbeat, command processing, diagnostics, and acknowledgement. |
| `LS-005` | It shall independently verify signatures, binding, validity, monotonic versions, and replay protections before applying control state. |
| `LS-006` | Client operational data shall remain outside the provider control plane unless a separate client-data platform is explicitly designed and approved. |

## Required Connected Business Chain

The first complete product must support this auditable chain:

1. An authorized office operator creates or selects a client.
2. The operator approves a contract revision with custom pricing, modules, and limits.
3. Control Desk issues an invoice and balanced provider-accounting evidence.
4. A payment is recorded or approved with correction and reversal controls.
5. Control Desk approves the next immutable desired-access revision and entitlement version.
6. A durable Office outbox publishes the signed/versioned message to Control Cloud.
7. Control Cloud accepts idempotently, signs, and distributes the entitlement or command.
8. SafarSuite local server verifies and applies or rejects the exact version deterministically.
9. The local server reports acknowledgement and observed state.
10. Control Desk displays the desired, delivered, and observed states with complete audit evidence.

Work that does not support, secure, operate, or make this chain usable is deferred unless this document is updated first.

## Approved Environment Shapes

| Environment | Allowed shape | Purpose | Must not be treated as |
| --- | --- | --- | --- |
| Local development | Developer-selected local processes, containers, test doubles, and file adapters | Fast implementation feedback | Deployment acceptance |
| Office V1 | One dedicated office PC running Control Desk UI, local API, and local PostgreSQL | Real provider-office operation | Public web deployment |
| Cloud staging | Control Cloud, Client Portal, cloud PostgreSQL, HTTPS, and test email on controlled Linux/cloud infrastructure | Cloud deployment rehearsal with demo data | Control Desk office host |
| Disposable Integration Lab | Optional co-location of Control Desk, Control Cloud, portal, and test databases | Reproducible connected-chain proof only | Final topology or production |
| Client-runtime lab | Disposable Linux/VM/client-like machine running SafarSuite local server/app | Customer installation, offline, entitlement, and diagnostics proof | Provider office authority |

The current `deploy/staging/docker-compose.yml` full-stack bundle is classified as **Disposable Integration Lab** until it is split or profiled into a cloud-only default. Its presence does not authorize Linux-hosted Control Desk production.

## Release And Deployment Gates

### Control Desk Office Gate

Control Desk is ready for the office only when all of the following pass from a clean designated PC:

- desktop/local installation succeeds without a Linux or separate office server dependency;
- local API and PostgreSQL start automatically after reboot;
- local listeners are not publicly exposed;
- operator login and role/scope enforcement pass;
- the primary business chain can be initiated from the desktop;
- core office reads and writes remain available during a simulated cloud outage;
- queued cloud delivery resumes without duplication after reconnection;
- backup, clean-machine restore, update, failed-update rollback, and uninstall/reinstall are rehearsed with retained evidence;
- routine operation requires no command-line or infrastructure administration.

### Control Cloud Gate

Control Cloud and the Client Portal are ready only when:

- DNS and HTTPS are valid for the chosen cloud host;
- cloud PostgreSQL migrations, backup, and clean restore pass;
- SMTP/test-email delivery is configured without making it an office Control Desk dependency;
- provider access, portal identity, MFA, rate limiting, secret custody, and key rotation pass;
- monitoring, alerting, log retention, recovery, and compromise procedures are rehearsed;
- no endpoint or database grants Cloud independent commercial authority.

### Connected Platform Gate

Production promotion additionally requires:

- CI passes from the exact clean commit/tag being promoted;
- the complete connected business chain passes with retained IDs and audit evidence;
- desired-versus-observed reconciliation reaches `InSync` with zero unexplained differences;
- office, cloud, and client-runtime databases are backed up and independently restorable;
- failure drills prove safe behavior during office/cloud disconnection, SMTP failure, message retry, migration failure, and key replacement.

## Explicit Non-Goals And Prohibitions

- Do not require a Linux machine or VPS to operate Control Desk.
- Do not expose the office Control Desk API or PostgreSQL directly to the public internet.
- Do not make `forgeaxis.tech`, Caddy, public TLS, router forwarding, or SMTP prerequisites for the office application.
- Do not treat a full-stack Docker Compose lab as the final product deployment.
- Do not share a database between Control Desk, Control Cloud, or a client runtime.
- Do not move provider commercial truth into Control Cloud or the Client Portal.
- Do not store client operational SafarSuite transactions in the provider control plane.
- Do not introduce microservices, client-specific code branches, or legacy Survey/FAS clone work without a separately approved requirement.
- Do not call external cloud services inside accounting transactions; publish through the durable outbox.
- Do not add infrastructure work merely because the test environment makes it convenient.

## Current Decisions

| Decision | Accepted result |
| --- | --- |
| `DEC-001` | SafarSuite Control Desk V1 runs completely on one dedicated office PC. |
| `DEC-002` | No separate office server or Linux host is required for Control Desk V1. |
| `DEC-003` | Control Cloud and the Client Portal are separate public/cloud deployments. |
| `DEC-004` | Public DNS, HTTPS, and SMTP belong only to the cloud/portal lane. |
| `DEC-005` | `forgeaxis.tech` may be used for cloud staging; it is not the Control Desk deployment target. |
| `DEC-006` | Brevo is an acceptable staging-email candidate, but provider selection/configuration is deferred until the cloud lane needs to start. |
| `DEC-007` | The Linux machine at `192.168.10.14` is a test host, not the office Control Desk host. |
| `DEC-008` | The current full-stack staging Compose bundle is an integration-lab artifact and must not redefine the production topology. |

## Open Implementation Decisions

These decisions remain open, but none may violate the accepted topology above:

| ID | Decision still required | Constraint |
| --- | --- | --- |
| `OPEN-001` | Desktop packaging and local-service installation mechanism | Must produce one-PC operation and hide infrastructure from routine users. |
| `OPEN-002` | How local PostgreSQL is installed, upgraded, and repaired | Must remain local-only, automated, backed up, and recoverable. |
| `OPEN-003` | Office backup destination and retention schedule | Must include a second copy outside the office PC and a clean restore proof. |
| `OPEN-004` | Desktop update channel, signing, and rollback mechanism | Must be integrity checked and recoverable. |
| `OPEN-005` | Future concurrent multi-PC office access | Not required for V1; requires an explicit topology and security revision here. |
| `OPEN-006` | Production cloud host and transactional email provider | Must remain independent of office-only operation. |
| `OPEN-007` | Initially supported office operating system and minimum hardware | Must be fixed before clean-PC packaging acceptance. |
| `OPEN-008` | Office recovery-point and recovery-time objectives | Must be measurable and proven through replacement-PC restore. |

## Immediate Alignment Work

1. Do not start Control Desk API/UI on the current Linux test host as an office deployment.
2. Keep or remove the Linux Control Desk test database only through an explicit disposable-lab cleanup decision; never place real office data in it.
3. Split or profile the staging Compose bundle so cloud-only staging is the default and full-stack colocation is explicitly labelled as an integration lab.
4. Define and implement the one-PC Control Desk installer, local service lifecycle, local PostgreSQL lifecycle, backup, restore, update, and rollback path.
5. Use the Linux machine for Control Cloud/Portal staging or client-runtime proof only.
6. Resume cloud DNS, HTTPS, and Brevo work only inside the cloud deployment lane.

## Drift-Control Checklist

Every proposed feature, infrastructure change, deployment task, and pull request must answer:

1. Which requirement ID in this document does it satisfy?
2. Which machine or system is allowed to host it?
3. Does it accidentally make Linux, DNS, HTTPS, SMTP, or a separate server mandatory for Control Desk?
4. Does it preserve Office Control as the commercial source of truth?
5. Is the change part of office deployment, cloud deployment, client-runtime deployment, or only an integration lab?
6. What executable acceptance evidence will prove it?
7. Does it introduce a new topology or product decision that must be approved here first?

If an answer is unclear, stop implementation and update this contract before proceeding.

## Detailed Supporting Documents

- [SafarSuite Control Platform Product Charter](product-charter-2026-07-11.md)
- [SafarSuite Control Platform Active Roadmap](../planning/active-roadmap-2026-07-11.md)
- [Control Desk New Requirements](../planning/control-desk-new-requirements.md)
- [Client Deployment And Data-Sync Boundary](client-deployment-and-data-sync-boundary.md)
- [Client Portal Identity Boundary](client-portal-identity-boundary.md)
- [SafarSuite Runtime Integration Boundary](safarsuite-runtime-integration-boundary.md)
- [Connected Acceptance Chain Proof](../planning/connected-acceptance-chain-proof-2026-07-17.md)
- [Staging Deployment Runbook](../planning/staging-deployment-runbook.md)

Supporting documents may add detail and evidence. They may not silently contradict this contract.
