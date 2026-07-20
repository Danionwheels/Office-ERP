# SafarSuite Control Desk V1 Production Configuration Contract

Status: accepted for `SVC05-05A`

The installed `appsettings.Production.json` is generated under `%ProgramData%\SafarSuite\ControlDesk\Config` and contains only non-secret values: PostgreSQL provider selection, loopback API URL, bounded logging, session duration, and disabled-by-default cloud outbox automation.

The file contains no database connection string, password, operator identity, password hash, signing secret, provider token, API key, or private key. Production authentication resolves the session signing key only through the Windows machine-secret provider at the canonical protected envelope path. Missing, invalid, undecryptable, or ACL-invalid machine-secret state fails closed during startup.

The generated configuration is loopback-only and does not introduce DNS, HTTPS, SMTP, Linux, Docker, or cloud runtime prerequisites.
