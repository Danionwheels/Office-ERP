# SafarSuite Control Desk V1 Recovery Plan

Date accepted: 2026-07-21

Status: Active V1 execution authority. `REC-00` through `REC-02` are complete; `REC-03` is the only authorized next stage.

This plan controls feature priority and restructuring order for SafarSuite Control Desk V1. The canonical deployment topology remains governed by `docs/architecture/final-system-requirements-and-deployment-contract.md`. `AGENTS.md` remains the coding authority. Earlier `Now`, `CURRENT`, or queue labels in retained plans/trackers do not authorize work unless this plan maps them from its active stage.

## Objective

Deliver a small, dependable provider-office product with disciplined module boundaries:

```text
install and sign in
  -> create a client while online or offline
  -> set that client's price and allowed modules
  -> create and issue an invoice
  -> post accounts receivable automatically
  -> record a simple payment when needed
  -> approve and distribute module access
  -> synchronize automatically when internet is available
  -> invite the client to choose a password
  -> let the client see their own invoice, balance, and access in the portal
```

The normal operator must not need to understand journals, chart-of-accounts structure, outbox envelopes, signing versions, deployment topology, or infrastructure administration.

## Decisions Already Made

1. Do not restart the whole repository. Preserve working, tested behavior and replace one workflow at a time.
2. Do not perform a big-bang architecture rewrite.
3. Complete a read-only sweep before restructuring production code.
4. Keep automatic accounting evidence underneath invoices and payments, but remove advanced accounting from the normal V1 experience.
5. Keep the modular monolith. Do not create microservices or add a mediator/event-bus framework merely to appear clean.
6. Use direct in-process calls through explicit public module interfaces for synchronous work and the durable outbox for cloud work.
7. Tauri is required for the released Minimal V1 desktop entry. It remains a thin shell around the React UI and authenticated loopback API; browser entry is development/test support only.
8. A portal user chooses their own password. The office never creates or stores a plain client password.
9. Brevo is later delivery infrastructure. Manual invitation-link handoff is acceptable until email integration is selected.
10. Hide obsolete or advanced surfaces before deleting them. Delete only after their replacement passes acceptance.
11. The portal identity is a provider-issued, globally unique login ID. Email is contact/delivery information, not an internal client or installation identifier. The client chooses the password.
12. Portal invitation is an online-only Minimal V1 action after the client has synchronized. Offline client/profile/pricing/invoice work remains durable; a separate invitation queue is deferred.
13. Installed Office V1 uses installer-managed native PostgreSQL as a loopback-only Windows service. Docker Compose PostgreSQL remains development/disposable-lab tooling only.

## Minimal V1 Scope

| Required now | Kept underneath, hidden from normal work | Deferred |
| --- | --- | --- |
| One local office administrator login | Balanced journal evidence | Advanced GL workspace |
| Client company and primary contact | Durable outbox details | Manual journals and accounting periods |
| One simple client price/recurring charge | Audit evidence | Financial statements and broad reporting |
| Module selection | Entitlement version/signature detail | Credits, refunds, complex allocations, reversals |
| Draft and issue invoice | Cloud receipt/idempotency detail | Card/payment-gateway integration |
| Automatic accounts-receivable posting | Local Server verification detail | Portal bank-proof workflow until the core portal passes |
| One simple payment against an invoice | Security controls required for safe operation | Complex provider roles/MFA administration UI |
| Automatic offline queue and reconnect | Existing advanced implementation retained during replacement | Survey/FAS, generic CRM, operational-data sync |
| Portal invitation, password choice, and login | Package lifecycle evidence | Automated email and password-reset delivery |
| Portal invoice, balance, module/access view | Backup and restore internals | Automatic updater |
| One-PC setup, automatic startup, backup/restore | Versioned manual update/rollback evidence | Advanced deployment and diagnostics dashboards |
| Thin installable Tauri desktop shell | Native PostgreSQL/API service lifecycle | Browser-only production entry |

Basic payment recording remains in scope because otherwise receivables cannot be closed and paid access cannot be demonstrated. Payment claims, attachment proofs, refunds, and advanced reconciliation are not part of Minimal V1.

## Scope Alignment Gate

As of 2026-07-21, the canonical deployment contract, active-roadmap status summary, retained one-PC plan/tracker, and this plan distinguish:

- Minimal V1 behavior required for the first usable release;
- internal correctness/security that remains mandatory;
- later operator features retained in the repository but not required in the daily UI.

Future scope changes must update the canonical contract and this plan together; no implementation may silently reinterpret either one.

## Target Module Discipline

Each module is a house. Its Domain, repositories, handlers, database writes, and private frontend files are inside the house. Other modules may use only its public gate.

```text
Operator UI
    |
    v
Use-case endpoint / workflow coordinator
    |
    +-- command/query -> Clients public gate
    +-- command/query -> Contracts public gate
    +-- command/query -> Billing public gate
    +-- command/query -> Accounting public gate
    +-- command/query -> Entitlements public gate
    |
    +-- versioned integration event -> outbox before commit -> dispatch after commit
```

### Ownership

| Module | Owns |
| --- | --- |
| Clients | Company identity, contacts, status |
| Contracts | Agreed client price, selected modules, commercial limits |
| Billing | Invoice lifecycle, invoice lines, balance due, charge templates |
| Payments | Payment record and status |
| Accounting | Receivable/revenue/cash posting and balanced journal proof |
| Entitlements | Approved desired access and immutable entitlement versions |
| ControlCloud | Outbox delivery, cloud receipts, portal projection adapters |
| Audit | Actor, time, reason, and protected-change evidence |
| Workflows | Sequence and combined read-model ports; owns no business data or writable repositories |

There must be only one authoritative client price. During the sweep, existing contract pricing and Billing charge rules must be assigned one clear meaning. The target is: Contracts owns the agreed price; Billing consumes an explicit immutable billing snapshot rather than reading Contracts repositories.

### Permitted roads

1. **Public command/query interface** for synchronous module behavior. Backend cross-module access is limited to `Modules.<ModuleName>.Public.*`.
2. **Use-case-specific workflow coordinator** for one user action spanning modules.
3. **Explicit read model** for a screen that combines data from several modules.
4. **Durable integration event/outbox** for Control Cloud and other post-commit work.

### Forbidden shortcuts

- A module may not inject another module's repository or handler.
- A module may not create or mutate another module's Domain entity.
- A module may not write another module's tables.
- A module may carry another module's stable identity as a primitive or approved public identity contract, but may not import the foreign Domain value object or navigate into its aggregate.
- Cross-module application records use primitives or stable public contract types, not private Domain types.
- Infrastructure composes adapters; business workflow decisions do not live there.
- API endpoints map requests and results only; they do not coordinate repositories.
- A frontend module may not import another module's private `api`, `types`, `components`, `hooks`, or `utils` paths.
- Cross-module frontend screens live under `app/workflows` and consume public module exports or, preferably, one backend workflow/read-model endpoint.
- No external HTTP call occurs inside the office business transaction. The owning module/workflow creates a versioned business integration event, and a generic outbox gate serializes and appends it before commit.
- No generic `SystemManager`, `EverythingService`, service locator, or global event bus is permitted.
- A combined screen may use a workflow-owned read-model port implemented in Infrastructure. That port is read-only and returns deliberate DTOs, never mutable Domain entities.
- Protected commercial/access changes append Audit evidence through the Audit gate inside the same transaction.

### Transaction rule

A workflow that changes several modules owns one outer `IUnitOfWork.ExecuteInTransactionAsync` boundary. Coordinated mutation gates share the same scoped unit of work/DbContext, do not start or commit their own transaction, do not call `SaveChanges` independently, do not call external services, and return before the workflow's single commit. Any failure rolls back the whole local business action. The outbox event is appended before commit; dispatch begins only after the local commit.

Example:

```text
IssueClientInvoiceWorkflow
  -> Billing issues invoice
  -> Accounting posts receivable
  -> generic outbox records the Billing-owned InvoiceIssuedIntegrationEvent
  -> commit once
```

Billing must not construct `JournalEntry`. Accounting must not alter the invoice. The Billing public result/workflow defines the versioned `InvoiceIssuedIntegrationEvent`; the generic outbox adapter only serializes, envelopes, persists, retries, and delivers it. ControlCloud must not decide invoice meaning.

## Current Sweep Baseline

The 2026-07-21 read-only sweep found:

| Boundary debt | Baseline |
| --- | ---: |
| Cross-module imports inside `ControlDesk.Application/Modules` | 147 |
| Cross-module imports inside `ControlDesk.Domain/Modules` | 26 |
| Cross-module imports inside frontend `src/modules` | 92 |
| Frontend module files larger than 4,000 lines | 2 |
| Enforced backend architecture-boundary tests | 0 |
| Enforced frontend import-boundary rules | 0 |

These counts are a shrinking baseline, not a reason for one massive rewrite. A new violation fails immediately. Existing violations are removed only through the active vertical workflow.

The reproducible evidence, definitions, exclusions, and exact project/module edges are retained in `docs/planning/control-desk-v1-sweep-register-2026-07-21.md`; its checked-in command is `tools/Get-ControlDeskDependencyInventory.ps1`.

`REC-02` must store the exact counting commands, exclusions, and definition of a violation with the sweep evidence so the baseline is reproducible. By release, the active Minimal V1 path must have zero private cross-module exceptions. Any exception remaining solely in parked code must be explicitly classified and frozen; it cannot be used by the released path.

Known product gaps that the sweep must retain explicitly:

- several primary navigation areas are placeholder cards;
- client pricing entered in the main workspace does not create the charge rule invoice drafting consumes;
- client create/update does not enqueue a dedicated client-profile cloud projection;
- portal invitations currently require a live Control Cloud call instead of durable queued intent;
- portal login exposes internal client/installation identifiers;
- the Windows package is an engineering artifact, not yet a normal operator installer.

## Execution Rules

1. Work-in-progress limit is one small task inside the `NEXT` recovery stage.
2. Every small task within a recovery stage must deliver one observable outcome and remain independently reversible.
3. Do not combine a module-boundary extraction, a UI redesign, and deployment work in one small task.
4. Do not refactor a module merely because it looks untidy. Refactor it when the active V1 workflow reaches that boundary.
5. Do not add a dependency or abstraction for an imagined future use. The second real use case justifies reuse.
6. Characterize working behavior with tests before moving it.
7. Keep the build green after every small task.
8. Characterize, replace, verify the replacement, hide the old path, and retire it only later.
9. New requests enter the parking lot unless they replace an accepted item through an explicit scope decision.
10. A stage may become `DONE` only when every small task in it is done and its evidence is linked in this plan.

## Recovery Sequence

Only the `NEXT` stage may begin. Within that stage, only one small task may be `IN_PROGRESS`. Later rows may be clarified but not implemented early.

| ID | Status | Working outcome |
| --- | --- | --- |
| `REC-00` | `DONE` | Recovery direction, baseline build, and this plan exist. Local evidence: UI production build and 114/114 solution tests passed on 2026-07-21. |
| `REC-01` | `DONE` | Canonical Minimal V1 scope, required Tauri desktop entry, native PostgreSQL production boundary, acceptance journey, and execution authority are aligned. |
| `REC-02` | `DONE` | Every top-level area is classified, the complete Minimal V1 chain is deeply swept, and confirmed placeholder navigation is hidden without deleting implementation. |
| `REC-03` | `IN_PROGRESS` | Automated guardrails prevent new backend and frontend boundary violations. `REC-03A` through `REC-03E` are complete; next is final guardrail integration review. |
| `REC-04` | `IN_PROGRESS` | Native local PostgreSQL install/migrate/restart/rerun behavior is being verified before more database work accumulates. Hermetic lifecycle proof passes; native Windows proof remains. |
| `REC-05` | `PENDING` | A small client workspace creates, edits, searches, persists, and reloads clients. |
| `REC-06` | `PENDING` | The same client workspace saves one authoritative price and module selection. |
| `REC-07` | `PENDING` | The same workspace issues an invoice and posts AR atomically without showing accounting setup. |
| `REC-08` | `PENDING` | Client, price/module, and invoice changes queue offline and synchronize automatically with idempotent effects. |
| `REC-09` | `PENDING` | One simple payment closes/reduces AR and drives paid module access without advanced screens. |
| `REC-10A` | `PENDING` | A minimal secure Control Cloud/Portal environment is ready for the real portal journey. |
| `REC-10B` | `PENDING` | A client chooses a password and sees only their own invoice, balance, and module/access state in the portal. |
| `REC-11` | `PENDING` | Placeholder and superseded daily UI paths are hidden; retained advanced tools are support-only. |
| `REC-12A` | `PENDING` | Native PostgreSQL, local API service, protected secrets, and first administrator are install-ready and survive reboot. |
| `REC-12B` | `PENDING` | The thin Tauri shell and one Windows setup reach normal desktop login without a terminal. |
| `REC-12C` | `PENDING` | Scheduled backup and clean replacement-PC restore preserve the accepted office state. |
| `REC-12D` | `PENDING` | A versioned manual Tauri/API/database update and forced rollback preserve the accepted office state. |
| `REC-13` | `PENDING` | Clean-PC, offline/reconnect, portal, security, recovery, update, and reinstall acceptance all pass. |

## Active Risks And Known Warnings

Detailed command transcripts remain in the sweep register. This table owns the lifecycle of unresolved warnings so that none disappear inside build output. An open blocking item must be closed with its stated evidence before the affected recovery work proceeds.

| ID | Status | Finding | Required disposition and closure evidence |
| --- | --- | --- | --- |
| `SEC-001` | `CLOSED — 2026-07-21` | `System.Security.Cryptography.Xml` 10.0.9 was a direct reference in both Control Desk Infrastructure and Control Cloud Infrastructure. The solution-wide NuGet audit reported five high-severity advisories: `GHSA-cvvh-rhrc-wg4q`, `GHSA-g8r8-53c2-pm3f`, `GHSA-23rf-6693-g89p`, `GHSA-8q5v-6pqq-x66h`, and `GHSA-mmjf-rqrv-855v`. | Removal-first verification exposed the EF Design build-time path to XML/PKCS 9.0.0, so both projects now deliberately override it with 10.0.10. XML and PKCS resolve to 10.0.10; the final full-solution vulnerability audit is clean; 114 Release tests, the UI build, both API publishes, and the unchanged dependency inventory passed. Detailed evidence is in the sweep register. |
| `ENV-001` | `OPEN — blocks deployment/installer acceptance` | The current development PC has .NET 10.0.9 shared runtimes installed. This is environment state, separate from the two repository package references. | Update the development and eventual office runtime baseline to a serviced 10.0.10-or-newer .NET 10 release before deployment or installer acceptance, then record `dotnet --list-runtimes` and installed-app smoke evidence. Do not treat removal of the NuGet reference as proof that the machine runtime is patched. |
| `BUILD-001` | `MONITOR — non-blocking` | The Control Desk UI production build reports a minified main JavaScript chunk of approximately 988 KB. | Recheck on each UI production build. Optimize only within an active UI stage when measurement shows it materially affects the accepted operator journey; this warning does not interrupt `SEC-001` or the architecture-boundary work. |

`SEC-001` is closed. `ENV-001` remains a later deployment/installer gate, while `BUILD-001` remains non-blocking. The next bounded task is the first architecture-boundary task in `REC-03`.

### Completed Task Brief — `SEC-001`

```text
Ticket: SEC-001
User-visible outcome: SafarSuite builds and packages without the known high-severity
  System.Security.Cryptography.Xml 10.0.9 dependency warning; behavior is unchanged.
Owning module: Infrastructure composition for Control Desk and Control Cloud; no business module.
Public gate used or introduced: None; this task changes no module communication.
Transaction boundary: None; no runtime write or database behavior changes.
Audit evidence: Before/after NuGet graphs, vulnerable-package audit, tests, UI build,
  and API publish results recorded in the sweep register; no product audit event applies.
Integration event/outbox effect: None.
Files/old path being replaced: The two direct System.Security.Cryptography.Xml 10.0.9 references.
  Removal-first verification exposed the EF Design build-time dependency on XML/PKCS 9.0.0;
  the final path therefore uses deliberate 10.0.10 overrides in both Infrastructure projects.
Success test: Restore succeeds; XML and PKCS resolve to 10.0.10 throughout the graph;
  the vulnerable-package audit is clean; 114 solution tests, UI build, and both API publishes pass.
Failure/rollback test: If compilation, tests, or publish reject 10.0.10, investigate the owning
  EF Design path and use another non-vulnerable resolution; never restore 10.0.9 or 9.0.0.
Explicitly out of scope: Installing/updating the PC runtime, changing business behavior,
  adding architecture guardrails, unrelated package upgrades, Tauri, PostgreSQL, or UI redesign.
```

### `REC-01` — Align The Canonical Scope

Small steps:

1. Amend the canonical requirements without changing the accepted one-PC/cloud topology.
2. Mark every wider feature as `Minimal V1`, `internal mandatory`, or `later`.
3. Record the required thin Tauri desktop entry and native PostgreSQL office boundary without moving business or service-lifecycle logic into Tauri.
4. Record the provider-issued, globally unique portal login-ID rule and duplicate-contact-email behavior.
5. Align the active roadmap, one-PC deployment plan/tracker, and this recovery plan so there is one execution authority.
6. Freeze the exact acceptance journey in this plan and the contract.

Done when there is no scope, desktop-entry, or execution-authority conflict across the canonical documents.

Completed 2026-07-21: the canonical contract now classifies Minimal V1, mandatory internal controls, and later capabilities; requires the thin Tauri shell; fixes native PostgreSQL as the office runtime while retaining Docker for development/labs; and makes this recovery plan the sole implementation-order authority. The active roadmap is a status summary, while the one-PC plan and small-task tracker are retained deployment evidence invoked only by mapped recovery stages.

### `REC-02` — Complete The Sweep

Create one sweep register, not several competing plans. Classify every top-level area, then deeply trace only the Minimal V1 chain and code reachable from it.

1. `REC-02A`: mechanically generate the module/dependency inventory, including the API-to-Infrastructure project-reference exception, and store reproducible counting commands/exclusions.
2. `REC-02B`: classify API endpoints and frontend routes as working, partial, placeholder, duplicate, excluded, or support-only.
3. `REC-02C`: trace database write ownership, transactions, audit, and outbox behavior for client, offering, invoice, payment, access, and invitation only.
4. `REC-02D`: map existing tests and deployment evidence to the Minimal V1 journey.
5. `REC-02E`: assign every top-level excluded area a simple disposition of `hide`, `freeze`, or `retire after V1`; do not perform forensic analysis of excluded internals.
6. `REC-02F`: hide already-confirmed placeholder navigation after its disposition is recorded; do not delete implementation.

Completed 2026-07-21: `REC-02A` through `REC-02F` are complete in `docs/planning/control-desk-v1-sweep-register-2026-07-21.md`. The dependency baseline confirms 8 Control Desk project-reference edges with one API-to-Infrastructure exception, 26 Domain cross-module imports, 147 Application cross-module imports, and 92 frontend cross-module declarations. The surface inventory classifies all 144 API registration sites/declarations and all nine primary frontend hash routes. The write trace confirms useful atomic invoice/payment/access foundations, but no persisted local Audit, two disconnected offering price writes, missing client/offering outbox events, concurrency/idempotency gaps, and a synchronous Clients-owned invitation call. The evidence map records a green 114-case xUnit baseline, useful PostgreSQL and connected-lab smokes, incomplete native database proof, and no Tauri/setup/backup/update/physical acceptance. The disposition register assigns all nine signed-in top-level routes and all 116 non-Minimal API/host surfaces exactly one of `hide`, `freeze`, or `retire after V1`, while protecting mandatory internals and selecting the Clients public gate as the first business extraction. The navigation-only final step removes Commercial, Accounting, Deployment & Cloud, and Access & Security from normal links while retaining direct route metadata and fallbacks. Production UI build, 114 solution tests, unchanged dependency counts, and an authenticated local browser smoke passed.

`REC-02A` through `REC-02E` changed no product behavior. `REC-02F` changed only the permitted visible navigation; it did not delete route implementation or change backend behavior.

Done when every top-level area is classified, the full Minimal V1 path has a dependency/write/test map, and the first boundary extraction is selected from evidence.

### `REC-03` — Lock The Roads

### Active Task Brief — `REC-03A`

```text
Ticket: REC-03A
User-visible outcome: No runtime behavior changes; a new private backend module dependency
  fails the automated test while today's known violations remain explicitly baselined.
Owning module: Solution-level architecture validation; no business module owns this test.
Public gate used or introduced: Modules.<ModuleName>.Public.* is recognized as the permitted
  cross-module namespace, but this task introduces no production public contract.
Transaction boundary: None; test-only change with no database or runtime write.
Audit evidence: Checked-in exact baseline, focused scanner tests, solution test result,
  UI production build, and reproduced dependency inventory recorded in the sweep register.
Integration event/outbox effect: None.
Files/old path being replaced: The absence of an automated backend module-boundary check;
  no production path is replaced.
Success test: The focused architecture suite passes against the checked-in current baseline;
  synthetic same-module/public examples pass and a synthetic new private dependency is rejected;
  full solution tests, UI build, and dependency inventory remain green.
Failure/rollback test: The scanner's comparison test proves an unbaselined private dependency
  is reported with an actionable key; reverting the isolated test project/solution entry restores
  the prior behavior without touching production code.
Explicitly out of scope: Repairing existing violations, adding production public gates,
  frontend import enforcement, project-layer enforcement, module extraction, or business changes.
```

Small steps:

1. Add a lightweight backend architecture test with a checked-in baseline of current violations.
2. Fail the build on any new module-to-module private dependency.
3. Establish `Modules.<ModuleName>.Public.*` as the only backend namespace allowed for cross-module use.
4. Fail the UI build on any new private cross-module import.
5. Add minimal public exports only for active/retained frontend modules; do not expose private files merely to make the check pass.
6. Add a project-layer check and baseline the existing API-to-Infrastructure reference for deliberate resolution.

Do not introduce a third-party mediator or split every module into separate assemblies. Project extraction is allowed later only if the lightweight gates cannot enforce the accepted boundary.

`REC-03A` completed 2026-07-21. Added the solution architecture-test project, scanner tests, and a sorted checked-in baseline of the current private backend dependencies. The focused suite passes 11/11; the baseline comparison reports actionable added/removed entries, so new private dependencies fail without changing production behavior. A missing `Microsoft.CodeAnalysis.Text` import found during the first compile was corrected. Existing dependency counts remain unchanged.

`REC-03` guardrail integration review completed 2026-07-21: backend architecture tests pass 13/13, frontend private-import baseline passes with 59 entries, and `git diff --check` reports only existing line-ending notices. The UI production build was attempted from the correct app directory but Vite/esbuild reported an access-denied path while resolving the existing `vite.config.js`; this is recorded as an environment/tooling warning and is not caused by the guardrail changes. No production behavior changed.

`REC-04` preflight completed 2026-07-21: `Test-OfficeDatabaseLifecycle.Hermetic.ps1` passed. The native proof was not attempted because its required prepared package directory and pinned PostgreSQL/VC++ vendor artifacts are absent from the workspace (`artifacts`/`deploy`). Native install/migrate/restart/rerun acceptance remains open and requires those inputs; no database lifecycle code was changed.

Native proof input contract recorded: PostgreSQL 17.10 archive `postgresql-17.10-1-windows-x64-binaries.zip` (SHA-256 `F9AAFCA58E7026A1EF2CAEEE711ACF761671E57904D430ADC85F468374F5A821`) and Microsoft VC++ x64 redistributable `vc_redist.x64.exe` (SHA-256 `843068991DAAA1F73AD9F6239BCE4D0F6A07A51F18C37EA2A867E9BECA71295C`) must be supplied to `New-OfficeDatabasePackage.ps1` before `Test-OfficeDatabaseLifecycle.NativeCi.ps1` can run.

2026-07-21 native-package attempt: both vendor files were found in `.codex-run` and matched the pinned hashes. The initial EF bundle failure was confirmed as sandbox access to the user NuGet probe path; the elevated retry succeeded. A fresh package was then built successfully with a 32-migration bundle ending at `20260713220254_AddPortalPaymentBoundary`, runtime hash `044C307E411CE9120C6C92A0D58D63E4E848171795314B80826CDC5FF6BF9E94`, and bundle hash `B9F8877067B38F67AA8DB6FF588417A724DCBC49648D0AFEEFCE448FDFEE17E7`. The native lifecycle script intentionally refuses non-GitHub/non-disposable-runner execution, so physical native install/migrate/restart/rerun remains pending its designated Windows runner gate. No lifecycle or production code was changed.

The fresh package also passed `Test-OfficePostgresRuntimeDependencies.ps1`: raw `initdb.exe` and `postgres.exe` probes exited `0` with matching PostgreSQL 17.10 versions. This confirms the sealed runtime dependency boundary before the protected native service lifecycle gate.

Final local sanity pass: `dotnet test SafarSuite.ControlDesk.sln --configuration Release --no-restore` passed 127 tests, including 13 architecture tests. This is regression evidence only; it does not replace the designated disposable Windows native-service proof.

CI run `29866597537` on commit `10f6a8f` completed UI, backend, deployment, office packaging, package smoke, and raw runtime probes successfully, then reproduced `initdb.exe` `0xC0000135` during the native lifecycle step. Uploaded boundary evidence shows all 12 fresh/restricted/installed version probes passed and VC++ was already satisfied; outcome is `FullInitdbInvocationBoundary` / `VersionProbesPassButLifecycleFails`.

The focused runtime-PATH workaround in commit `68b72f6` was tested in CI run `29867228511` and produced the identical `initdb.exe` `0xC0000135` failure. It was reverted in commit `fb34f38`; no unproven lifecycle workaround remains. This confirms the retained DB03-F09 boundary diagnosis and prevents speculative fixes from accumulating.

Working-directory diagnostic commit `57550af` was tested in CI run `29868373349`; it also produced the identical `initdb.exe` `0xC0000135` failure after all package/probe gates passed. It was reverted in commit `25c0edfe`. Both candidate variables are now explicitly disproven; the remaining investigation must instrument the full `initdb` child/dependency path before any corrective lifecycle change.

CI handoff check 2026-07-22: the repository remote is `https://github.com/Danionwheels/Office-ERP.git`, but local `gh auth status` reports the stored GitHub token is invalid. No workflow dispatch, push, or remote mutation was attempted. Re-authentication is required before the disposable Windows native lifecycle proof can be started from this workspace.

While the external native proof remains pending, a read-only REC-05 characterization was recorded in `docs/planning/rec05-client-characterization-2026-07-22.md`. It inventories the existing 21 client routes and narrows the first replacement workspace to identity, search/list, selection, update, and primary-contact maintenance. No REC-05 production implementation or database change has started.

Done when new disorder is mechanically blocked while all current tests still pass and baseline measurement is reproducible.

### `REC-04` — De-risk Native Local PostgreSQL

Use the existing package/database lifecycle work. Do not redesign it.

Small steps:

1. Run the exact native Windows PostgreSQL install/migrate/start/restart/rerun gate from the current clean source.
2. Fix only a defect reproduced by that gate.
3. Prove loopback binding, migration identity, data persistence, clean shutdown, and actionable startup failure evidence.
4. Stop expanding diagnostic/proof machinery once the gate passes.

Done when the real local database foundation is proven before new workflow migrations accumulate.

### `REC-05` — Client Vertical Slice

Small steps:

1. Characterize current client create/update/search behavior.
2. Introduce the Clients public gate without changing results.
3. Add one small client register/workspace endpoint and read model.
4. Build a small client list/create/edit UI using only that public surface.
5. Reload after process restart and prove data persisted.
6. Append Audit evidence for protected client changes through the Audit gate.

Done when the old client pages are no longer required for client identity work.

### `REC-06` — Price And Modules Vertical Slice

Small steps:

1. Resolve the current Contract-price versus Billing-charge-rule duplication.
2. Introduce the Contracts public gate for one client offering: price, currency, billing period, selected modules.
3. Make Billing consume an explicit approved billing snapshot.
4. Seed a small versioned module catalog automatically; defer catalog-administration UI.
5. Add simple price fields and module checkboxes to the client workspace.
6. Append Audit evidence in the same transaction.
7. Prove the saved amount is exactly what invoice drafting consumes.
8. Prove clients A and B can have different prices and changing A never changes B.

Done when there is one source of price truth and no comma-separated module entry in the normal flow.

### `REC-07` — Invoice And Receivable Vertical Slice

Small steps:

1. Characterize invoice issue, AR, revenue, and rollback behavior.
2. Add `IssueClientInvoiceWorkflow` as the transaction coordinator.
3. Move invoice behavior behind the Billing gate.
4. Move receivable posting behind the Accounting gate.
5. Have the Billing result/workflow create a versioned `InvoiceIssuedIntegrationEvent`; keep the outbox adapter generic.
6. Append Audit evidence in the same transaction.
7. Add draft/review/issue actions with automatic invoice numbering, issue/due dates, one recurring line, and seeded AR/revenue mappings.

Done when invoice issue either commits invoice + balanced AR evidence + outbox together or commits nothing, and the operator never selects a ledger account.

### `REC-08` — Offline And Automatic Cloud Synchronization

Small steps:

1. Add explicit client-profile and client-offering integration events.
2. Ensure client, offering, and invoice events enter the durable outbox in their local transaction.
3. Show only `Waiting for internet`, `Synced`, or `Needs attention` in the normal UI.
4. Prove offline create/restart/reconnect may retry delivery but stable message IDs create exactly one accepted cloud business effect without a manual button.
5. While already online, prove a new change reaches `Synced` within an accepted bounded interval without a manual button.
6. Prove the dedicated client profile/name/contact projection reaches Cloud independently of `InvoiceIssued`.

Done when loss of internet never blocks local work and reconnection requires no operator action.

### `REC-09` — Minimal Payment And Paid Access

Small steps:

1. Characterize the existing simple payment and access-issue behavior.
2. Add one workflow for recording a payment against one invoice.
3. Put Payments, Billing balance update, Accounting receipt posting, and outbox work behind their public gates and one transaction.
4. Issue access from explicit paid-invoice and approved-contract snapshots rather than foreign repositories.
5. Append versioned payment/access integration events and Audit evidence in the same transaction.
6. Prove enabled module allowed and disabled module denied in the client runtime.

Done when the basic receivable lifecycle closes without exposing claims, allocations, refunds, or reconciliation tools.

### `REC-10A` — Minimal Cloud And Portal Readiness

Small steps:

1. Deploy only Control Cloud, Client Portal, private cloud PostgreSQL, and HTTPS for the accepted environment.
2. Configure production-grade secrets, tenant isolation, sessions, and basic rate limiting without exposing them in UI or logs.
3. Prove Cloud accepts each stable outbox message idempotently and projects the accepted client, invoice, and access state.
4. Keep office operation independent of DNS, SMTP, and the cloud host.

Done when the real portal journey has a safe environment and Cloud still cannot originate commercial decisions.

### `REC-10B` — Minimal Client Portal

Small steps:

1. Permit invitation only when online and after the client projection is synchronized; show a clear `Connect to invite` state otherwise.
2. Put invitation behind a public gate without exposing internal client/installation identifiers.
3. Sign in with the provider-issued login ID plus password.
4. Let the invited client choose their password through a secure single-use link handed off manually until Brevo.
5. Show only that client's identity, invoices, balance, selected modules, and access status.
6. Add a browser-level acceptance proof against real cloud projection data, including cross-client denial.

Done when a client completes the journey without provider help or knowledge of internal identifiers.

### `REC-11` — Remove Daily Clutter

Small steps:

1. Route normal users only to Clients and the accepted client workspace.
2. Move diagnostics and retained advanced tools behind one support/admin entry.
3. Hide placeholder navigation and duplicate pages.
4. Remove SurveyValuation and other excluded work from routes/navigation/build entry surfaces; physical deletion remains post-V1 work.
5. Split only active Minimal V1 files when required by the accepted replacement flow; do not start a repository-wide file-size cleanup.

Done when every visible control works and every visible term describes an operator task.

### `REC-12A` — Installed Local Service Boundary

Use the existing package foundation; do not restart packaging.

Small steps:

1. Preflight the accepted Windows version, architecture, memory, and free disk space.
2. Prepare installer-owned components in this order: VC++ prerequisite, native PostgreSQL/service, database/migrations, protected machine secrets, first administrator, API service/dependency/readiness, install receipt.
3. Add narrowly owned service/database repair and data-preserving uninstall/reinstall behavior.
4. Prove loopback-only listeners, no public/inbound firewall exposure, secret-safe ACLs/logs/diagnostics, controlled shutdown, startup-failure guidance, and service recovery after reboot.

Done when the local service boundary is install-ready and a routine operator never administers PostgreSQL, services, ports, or command lines.

### `REC-12B` — Thin Tauri Shell And One Setup

Small steps:

1. Create the Tauri project under `desktop/tauri` as a thin shell with no Domain/Application rules, database access, cloud credentials, outbox logic, or service-lifecycle ownership.
2. Wait for the local API readiness boundary, display only the authenticated loopback React UI, restrict unintended external navigation/native capabilities, and show operator-friendly recovery guidance.
3. Compose one elevated Windows setup that installs the local-service components, required WebView runtime, Tauri payload, Start-menu/optional desktop entries, and retained ownership receipt.
4. Add Tauri-aware repair and data-preserving uninstall/reinstall behavior.
5. Prove first login and the accepted client smoke through the installed Tauri executable after a cold reboot.

Done when the normal operator uses one installed desktop application and no browser, terminal, SDK, Docker, WSL, or Linux runtime is required.

### `REC-12C` — Backup And Replacement Restore

Small steps:

1. Accept a simple schedule, local retention, off-PC destination class, and visible destination-loss policy; the destination is a second copy, not an office-server dependency.
2. Produce verified database and protected-configuration backups with checksums.
3. Prove a clean replacement-PC restore and reconcile stable office, operator, client, invoice, outbox, and access IDs.

Done when a failed or missing destination is visible and the latest verified backup restores independently.

### `REC-12D` — Versioned Manual Update And Rollback

Small steps:

1. Require a fresh verified backup and restore proof before update.
2. Verify version/manifest/Tauri/API/database payload integrity before changing installed state.
3. Apply one manual update and prove the accepted business smoke afterward.
4. Force an update/startup failure and restore the previous healthy application/data pair.

Automatic updating remains deferred.

Done when update success and rollback both preserve the accepted office state.

### `REC-13` — Release Acceptance

On a clean Windows PC:

1. prove there is no .NET SDK, Node.js, Docker/WSL, Linux host, DNS, SMTP, or separate office-server dependency;
2. install, open the Tauri desktop application, create the first administrator, and verify loopback listeners, firewall boundary, and secret custody;
3. disconnect internet;
4. create client and contact, set price and one enabled/one disabled module, and issue invoice;
5. verify exact invoice amount, balance/AR evidence, Audit evidence, dedicated client projection queued, and no cross-client price leakage;
6. restart offline and verify data plus queued work;
7. reconnect and verify automatic retry with exactly one accepted business effect and the correct client profile in Cloud;
8. make a second change while online and verify automatic synchronization within the accepted bounded interval;
9. invite client, choose password, sign in, and verify tenant-isolated portal data;
10. record payment, issue access, and verify module allow/deny;
11. reboot, controlled-shutdown, and startup-failure recovery checks;
12. back up, restore onto a replacement PC, and reconcile retained IDs;
13. apply a versioned Tauri/API/database update, force a failed update, and prove rollback;
14. run data-preserving uninstall/reinstall and reopen the same office state.

V1 is not accepted from unit tests or an integration harness alone. This physical journey must pass, and every private cross-module exception reachable from the released Minimal V1 path must be removed.

## Definition Of Done For Every Small Task

A small task is `DONE` only when all applicable items pass:

- the operator can complete the stated action through the real UI;
- data is stored in the authoritative PostgreSQL path, not a mock or browser state;
- restart does not lose committed work;
- the main success rule and important failure/rollback rule have automated tests;
- `dotnet test SafarSuite.ControlDesk.sln --no-restore` passes;
- `npm run build` passes for the Control Desk UI;
- after `REC-12B` introduces Tauri, its compile/package check and installed-shell smoke also pass;
- no new architecture-baseline exception is added without an explicit recorded decision;
- offline behavior is tested when cloud communication is involved;
- the old path is hidden only after the replacement passes;
- evidence and status are updated in this plan.

A compile-only result, placeholder screen, disconnected button, mocked happy path, or undocumented manual workaround is not done.

## Task Template

Every recovery task must state before coding:

```text
Ticket:
User-visible outcome:
Owning module:
Public gate used or introduced:
Transaction boundary:
Audit evidence:
Integration event/outbox effect:
Files/old path being replaced:
Success test:
Failure/rollback test:
Explicitly out of scope:
```

If those answers are unclear, stop and finish the sweep decision first.

## Parking Lot

Do not begin these until `REC-13` passes or this plan is explicitly revised:

- advanced accounting UI and reports;
- payment claims, attachments, credits, refunds, and complex reconciliation;
- product-catalog revision administration;
- device/branch/user/feature-limit administration beyond the one needed module proof;
- Command Center expansion and generic dashboards;
- Survey/FAS or legacy form work;
- automatic updater;
- multi-PC office hosting;
- client operational-data synchronization;
- new cloud/deployment proof frameworks not required by an active acceptance failure.

## Drift Check

Before starting any task, answer:

1. Which `REC-*` stage is active, and which one small task inside it is in progress?
2. Which Minimal V1 outcome does this change make usable?
3. Which module owns the decision and which public gate is used?
4. What proves success, rollback, restart, and offline behavior where relevant?
5. What existing path becomes replaceable after this passes?

If the task cannot answer all five, it does not start.
