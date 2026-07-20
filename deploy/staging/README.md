# SafarSuite Staging Compose

This folder is the first VPS-style staging bundle for SafarSuite Control Cloud, SafarSuite Control Desk, and the Control Desk UI.

It is meant for a disposable staging server with Docker Compose, two DNS names, and HTTPS through Caddy.

## Scope Warning

This bundle is classified as a **Disposable Integration Lab** by `docs/architecture/final-system-requirements-and-deployment-contract.md`.

It is not the final SafarSuite Control Desk deployment topology. Control Desk V1 must run entirely on one dedicated office PC with its local API and local PostgreSQL; it does not require Linux, a public domain, Caddy, router forwarding, or SMTP. The Linux services in this bundle may be used only with demo data for connected integration proof until the bundle is split or profiled into a cloud-only default.

## Current Target

Current LAN server:

```text
192.168.10.14
```

This is a private network address. For normal public HTTPS certificates, DNS should point to the router/public WAN IP and ports `80` and `443` should forward to `192.168.10.14`.

If DNS points directly to `192.168.10.14`, the names may work only inside the LAN and Caddy will not be able to complete a normal public HTTP certificate challenge.

## Services

| Service | Purpose |
| --- | --- |
| `caddy` | Public HTTPS reverse proxy |
| `control-cloud-api` | SafarSuite Control Cloud API and Client Portal preview |
| `control-desk-api` | SafarSuite Control Desk API |
| `control-desk-ui` | Built React office UI served through Nginx |
| `control-cloud-db` | PostgreSQL for Control Cloud |
| `control-desk-db` | PostgreSQL for Control Desk |

Control Cloud also gets a persistent `App_Data` volume because a few staging registers and delivery/audit files are still file-backed while the main cloud records use PostgreSQL.

## First Setup

1. Point DNS at the staging server:

   ```text
   cloud-staging.forgeaxis.tech -> router/public WAN IP
   desk-staging.forgeaxis.tech  -> router/public WAN IP
   ```

   Then forward router ports `80` and `443` to `192.168.10.14`.

2. Copy `.env.example` to `.env` on the server, replace every placeholder, and restrict the file to the deployment account.

   ```bash
   chmod 600 deploy/staging/.env
   ```

   Generate `CONTROL_DESK_SESSION_SIGNING_SECRET` independently with at least 32 characters; never reuse a database, publisher, provider-access, or Client Portal secret.

   Control Desk operator identities are persisted in PostgreSQL. Staging rejects configuration-based users; do not expose the disposable Control Desk lab until an operator has been provisioned through the approved bootstrap path.

3. Create the files listed in `secrets/README.md`.

4. Run the redacted staging preflight. It validates values, secret equality and uniqueness, and the ECDSA key pair without printing secret material.

   ```bash
   dotnet run --project tools/SafarSuite.StagingPreflight --configuration Release -- --staging-directory deploy/staging
   docker compose -f deploy/staging/docker-compose.yml config --quiet
   ```

5. Build and start databases first:

   ```bash
   docker compose -f deploy/staging/docker-compose.yml up -d control-cloud-db control-desk-db
   ```

   PostgreSQL is published only on the staging host's loopback interface. The default ports are `55432` for Control Cloud and `55433` for Control Desk; they are configurable in `.env` and are never exposed publicly.

6. Apply database migrations from the staging host with the .NET SDK and this source tree. Set both connection strings explicitly; the design-time factories otherwise fail closed. Never enable `SAFARSUITE_ALLOW_DEVELOPMENT_DB_FALLBACK` on the staging host.

   ```bash
   set +x
   read -rsp 'Control Cloud database password: ' CLOUD_DB_PASSWORD
   printf '\n'
   export SAFARSUITE_CONTROL_CLOUD_CONNECTION_STRING="Host=127.0.0.1;Port=55432;Database=safarsuite_control_cloud_staging;Username=safarsuite_cloud;Password=${CLOUD_DB_PASSWORD}"
   unset CLOUD_DB_PASSWORD
   dotnet tool restore
   dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --startup-project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --context ControlCloudDbContext
   unset SAFARSUITE_CONTROL_CLOUD_CONNECTION_STRING
   ```

   ```bash
   set +x
   read -rsp 'Control Desk database password: ' CONTROL_DESK_DB_PASSWORD
   printf '\n'
   export SAFARSUITE_CONTROL_DESK_CONNECTION_STRING="Host=127.0.0.1;Port=55433;Database=safarsuite_control_desk_staging;Username=safarsuite_desk;Password=${CONTROL_DESK_DB_PASSWORD}"
   unset CONTROL_DESK_DB_PASSWORD
   dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlDesk.Infrastructure --startup-project src/SafarSuite.ControlDesk.Api --context ControlDeskDbContext
   unset SAFARSUITE_CONTROL_DESK_CONNECTION_STRING
   ```

   The password prompts do not echo or put the values in shell history. Adjust names, users, and ports to the already-preflighted `.env` values when they differ from the defaults; do not paste passwords directly into command lines.

   Before each update, independently confirm the target database name and user. Afterward, verify the EF migration ledgers contain 23 Control Cloud migrations through `20260713221145_AddClientPortalPaymentBoundary` and 33 Control Desk migrations through `20260720035506_AddLocalOperatorAuthentication`; also verify `pg_trgm` exists in the Control Desk database.

7. Start the full staging stack:

   ```bash
   docker compose -f deploy/staging/docker-compose.yml up -d --build
   ```

8. Check the two public endpoints:

   ```text
   https://cloud-staging.forgeaxis.tech/health
   https://desk-staging.forgeaxis.tech/
   ```

## Notes

- Do not commit `.env` or real files under `secrets/`.
- `.dockerignore` excludes staging environment and secret files from every image build context; keep that boundary intact.
- Compose passes each service only the environment variables it needs; do not restore a bundle-wide `env_file` mapping.
- Database host ports bind to `127.0.0.1` only. Use the staging host or an SSH tunnel for administrative access.
- The Control Desk API currently needs inline environment variables for the publisher HMAC and transitional provider access shared secret.
- The UI sends all `/api` traffic to the Control Desk API through the Nginx proxy.
- The Control Desk API talks to Control Cloud through `https://${CONTROL_CLOUD_HOST}`.
- Use this only with demo clients until the production blockers in `docs/planning/staging-deployment-runbook.md` are closed.
