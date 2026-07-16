# Staging Secrets

Place real staging secret files in this directory on the server only.

Expected files:

- `control-desk-publisher-hmac`
- `control-cloud-entitlement-hmac`
- `app-activation-public.pem`
- `app-activation-private.pem`
- `provider-access-shared-secret`
- `provider-access-session-hmac`
- `provider-access-totp-hmac`

Keep matching inline values in `.env` for the Control Desk settings that do not support `*File` yet:

- `CONTROL_DESK_PUBLISHER_SIGNING_SECRET`
- `PROVIDER_ACCESS_SHARED_SECRET`
- `CLIENT_PORTAL_SESSION_SIGNING_SECRET`
- `CLIENT_PORTAL_MFA_PROTECTION_SECRET`

## Required Client Portal environment

Outside `Development`, Control Cloud now fails startup unless all of the following are true:

- `Persistence__Provider=Postgres` and `ConnectionStrings__ControlCloud` identifies the Control Cloud PostgreSQL database. The staging compose file supplies both from the database variables in `.env`.
- `CLIENT_PORTAL_SESSION_SIGNING_SECRET` is an independently generated, non-placeholder secret of at least 32 UTF-8 bytes.
- `CLIENT_PORTAL_MFA_PROTECTION_SECRET` is a different independently generated, non-placeholder secret of at least 32 UTF-8 bytes.
- `CLIENT_PORTAL_PUBLIC_URL` is the non-loopback HTTPS URL of the Client Portal page, with no query string or fragment, for example `https://cloud.example.com/client-portal/index.html`.
- Provider shared-access, provider-session signing, and provider-TOTP protection secret files must each contain a different non-placeholder value of at least 32 UTF-8 bytes.
- The built-in development provider account is disabled outside Development. Bootstrap named provider operators through the protected provider-operator endpoint, then manage their scopes, MFA, and recovery codes as separate accounts.

SMTP is required outside `Development`. Set the provider to `Smtp` and configure the canonical mail environment variables:

- `SMTP_HOST`
- `SMTP_PORT` (1-65535; normally `587`)
- `FROM_ADDRESS` containing one mailbox address
- `SMTP_USER` and `SMTP_PASS` together, or leave both empty only for an anonymous SMTP relay

SMTP TLS is enabled by default and is mandatory outside Development. The compose mapping passes these names directly to Control Cloud.
