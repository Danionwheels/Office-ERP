# Control Desk Customer Setup UX

Date added: 2026-07-09

Purpose: move the clean-machine deployment proof into Control Desk in small, operator-facing slices.

## Goal

Let a provider operator create a customer deployment package, hand off everything needed for installation, and then confirm registration, entitlement, diagnostics, and app activation from the same Cloud workspace.

## Progress

- [x] Show generated bootstrap package evidence in the Cloud workspace.
- [x] Copy the exact install command from the generated package.
- [x] Download the signed bootstrap bundle from the generated package.
- [x] Download each generated runtime artifact from the generated package.
- [x] Download a customer setup guide containing the install command, package ids, bundle hash, versions, verification sequence, and artifact checksums.
- [x] Promote the status controls into a guided setup checklist: deployment profile saved, setup packet ready, registration active, heartbeat received, entitlement pulled, diagnostics received, app activation issued.
- [ ] Add customer-safe Windows/PowerShell handoff wording if the installer is run from a Windows terminal.
- [ ] Let operators mark external handoff state without exposing setup-token plaintext after package creation.

## Notes

The first UX slice is intentionally browser-side and uses the existing bootstrap package response. It does not change Control Cloud package generation or persistence.

The setup checklist is evidence-backed from existing cloud state instead of manual ticks. Entitlement pulled is complete only when the latest heartbeat carries an entitlement version, so the desk can distinguish cloud issuance from local-server pickup.
