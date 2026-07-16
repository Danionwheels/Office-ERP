# SafarSuite Staging Compose

This folder is the first VPS-style staging bundle for SafarSuite Control Cloud, SafarSuite Control Desk, and the Control Desk UI.

It is meant for a disposable staging server with Docker Compose, two DNS names, and HTTPS through Caddy.

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

2. Copy `.env.example` to `.env` on the server and replace every placeholder.

   Generate `CONTROL_DESK_SESSION_SIGNING_SECRET` independently with at least 32 characters; never reuse a database, publisher, provider-access, or Client Portal secret.

   Fill the `CONTROL_DESK_OPERATOR_*` values with a non-development operator, including a PBKDF2 password hash and explicit role/scope. The built-in `local-control-desk-admin` identity is rejected outside Development.

3. Create the files listed in `secrets/README.md`.

4. Build and start databases first:

   ```bash
   docker compose -f deploy/staging/docker-compose.yml up -d control-cloud-db control-desk-db
   ```

5. Apply database migrations from a machine with the .NET SDK and this source tree:

   ```bash
   export SAFARSUITE_CONTROL_CLOUD_CONNECTION_STRING='Host=<server>;Port=<port>;Database=safarsuite_control_cloud_staging;Username=safarsuite_cloud;Password=<password>'
   dotnet tool restore
   dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --startup-project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --context ControlCloudDbContext
   ```

   ```bash
   export SAFARSUITE_CONTROL_DESK_CONNECTION_STRING='Host=<server>;Port=<port>;Database=safarsuite_control_desk_staging;Username=safarsuite_desk;Password=<password>'
   dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlDesk.Infrastructure --startup-project src/SafarSuite.ControlDesk.Api --context ControlDeskDbContext
   ```

6. Start the full staging stack:

   ```bash
   docker compose -f deploy/staging/docker-compose.yml up -d --build
   ```

7. Check the two public endpoints:

   ```text
   https://cloud-staging.forgeaxis.tech/health
   https://desk-staging.forgeaxis.tech/
   ```

## Notes

- Do not commit `.env` or real files under `secrets/`.
- The Control Desk API currently needs inline environment variables for the publisher HMAC and transitional provider access shared secret.
- The UI sends all `/api` traffic to the Control Desk API through the Nginx proxy.
- The Control Desk API talks to Control Cloud through `https://${CONTROL_CLOUD_HOST}`.
- Use this only with demo clients until the production blockers in `docs/planning/staging-deployment-runbook.md` are closed.
