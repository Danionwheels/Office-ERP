# Control Desk Operator Register UI Review

Date: 2026-07-05

Purpose: keep the current Control Desk operator-readability UI changes separate from the platform-spine stabilization checkpoint.

## Scope

This lane covers two UI readability upgrades:

- Cloud installation status now has an operator control register summarizing installation, heartbeat, entitlement, commands, diagnostics, deployment, and history.
- Client statement now uses register tables for statement controls, currency summaries, invoices, payments, and statement lines instead of compact card lists.

Changed files:

- `apps/control-desk-ui/src/modules/control-cloud/components/CloudInstallationStatusPanel.tsx`
- `apps/control-desk-ui/src/modules/statements/components/ClientStatementPanel.tsx`
- `apps/control-desk-ui/src/styles.css`

## Intent

The goal is operational scanning, not a new domain behavior:

- make cloud/local support state easier to read in one pass
- make receivable statement evidence easier to compare row-by-row
- keep dense, office-style tables for repeated operational records
- avoid mixing these visual/readability changes into the module-gateway/product-catalog platform spine

## Review Checks

- Control Desk UI production build must pass.
- `git diff --check` must pass for the touched UI files.
- No API contract or persistence behavior should be required for this lane.
- If this lane is accepted, record it as an operator console readability slice.
- If this lane is deferred, keep it out of the platform-spine checkpoint and review separately.

## Current Status

Validated in this stabilization pass:

- `npm run build` in `apps/control-desk-ui` passed with the known Vite large-chunk warning.
- `git diff --check` passed for the touched UI files and this review note, with only LF-to-CRLF working-copy warnings.

Decision: keep this as a separate operator console readability lane. It can be accepted or deferred independently from the module-gateway/product-catalog platform-spine checkpoint.
