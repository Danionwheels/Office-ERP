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
- [x] Download a Control Cloud-issued SafarSuite Windows app pairing descriptor JSON from the generated bootstrap package for cloud-assisted/offline-assisted LocalServer discovery.
- [x] Promote the status controls into a guided setup checklist: deployment profile saved, setup packet ready, registration active, heartbeat received, entitlement pulled, diagnostics received, app activation issued.
- [x] Add customer-safe Windows/PowerShell handoff wording if the installer is run from a Windows terminal.
- [x] Let operators mark external handoff state without exposing setup-token plaintext after package creation.
- [x] Show setup packet handoff evidence as its own guided checklist step before local-server registration.
- [x] Add clean-target Docker volume preflight warnings to the downloaded customer setup guide after the real clean-machine proof exposed stale-volume failure risk.

## Notes

The first UX slice is intentionally browser-side and uses the existing bootstrap package response. It does not change Control Cloud package generation or persistence.

The setup checklist is evidence-backed from existing cloud state instead of manual ticks. Entitlement pulled is complete only when the latest heartbeat carries an entitlement version, so the desk can distinguish cloud issuance from local-server pickup.

The downloaded setup guide now calls out that the install command is Bash, gives PowerShell launch commands for Git Bash/WSL, and tells operators to regenerate the package if the guide or bundle is sent to the wrong destination.

The downloaded setup guide also carries a clean-target preflight section with the generated compose project/state directory, warns operators not to reuse stale database volumes from another customer/package/PostgreSQL major version, and scopes any retry cleanup to the previous SafarSuite setup stack for the same installation. This came directly from the 2026-07-10 real Control Cloud clean-machine pass, where a stale local PostgreSQL volume broke an otherwise clean generated package run.

The setup packet now also includes a non-secret pairing descriptor. The normal Descriptor button asks Control Cloud to issue and sign the descriptor through the bootstrap signing-key lane, then falls back to a local unsigned hint only if the cloud export is unavailable. It carries URL candidates, client/install/site metadata, bootstrap package identifiers, bundle/signature metadata, and optional app-server identity/fingerprint pins when a current activation issue is known. It does not carry setup-token plaintext, provider credentials, database credentials, app activation tokens, or signing private keys. The SafarSuite Windows app imports the descriptor as a candidate hint, then still validates the live LocalServer hello response and requires fingerprint confirmation before writing trust.

The deployment package register now lets operators mark a package as handed off with channel, recipient, actor, and note fields. The durable state is a `BootstrapPackageHandedOff` Control Cloud audit event matched by package id, so the package list can show "Handed off" evidence without returning setup-token plaintext after creation.

The guided setup checklist now treats handoff as its own evidence-backed step between setup packet generation and local-server registration. It uses the latest loaded package and the matching `BootstrapPackageHandedOff` audit event, so operators can see when the customer-facing delivery step is still waiting even if the package itself has already been generated.

The live PostgreSQL proof now marks a generated package as handed off, reads back the `BootstrapPackageHandedOff` audit event, and asserts the audit detail identifies the package and installation without leaking setup-token plaintext. The Control Desk provider-access proxy proof now verifies the same handoff and audit path through the operator-facing API. A browser pass on the Control Desk Cloud tab created a bootstrap package, marked handoff from the package register, showed the "Handed off" badge/timestamp, and displayed the handoff audit event without the setup token id.
