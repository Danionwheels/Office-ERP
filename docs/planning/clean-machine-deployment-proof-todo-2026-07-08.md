# Clean Machine Deployment Proof Todo

Date added: 2026-07-08

Purpose: keep the next production-readiness slice small, repeatable, and easy to follow.

## Goal

Prove that a fresh customer-style setup can go from Control Cloud bootstrap package to running LocalServer/app-runtime with stable secrets, activation, entitlement, heartbeat, and diagnostics.

## Todo

- [x] Define the proof environment: clean Docker/VM-like target, PostgreSQL-backed Control Cloud, file-backed provider/deployment secrets.
- [x] Generate one customer installation profile, setup token, and bootstrap package from Control Cloud.
- [x] Run the generated installer against the clean target using the app-runtime profile.
- [x] Verify LocalServer registration, entitlement pull, heartbeat, command polling, and diagnostics upload.
- [x] Verify app activation import and module access through the local module gateway.
- [x] Rerun the installer and confirm durable generated secrets are reused instead of rotated.
- [x] Capture the exact command sequence, expected outputs, artifacts, and failure triage notes.
- [ ] Decide what must move from "proof script" into the Control Desk customer setup UX.

## 2026-07-10 Rehearsal Pass

Ran a fresh generated-package rehearsal after the LocalServer pairing and installed-app manager Access Assignment proofs landed. This pass used `ComposeBootstrapProof run-compose` with the optional `app-runtime` profile enabled, generated a new proof package under `artifacts/codex/clean-machine-rehearsal-20260710`, started Docker Compose on fresh host ports, verified the LocalServer runtime, verified app-runtime health, and cleaned the compose stack down after the proof.

Command:

```powershell
dotnet run --project tools\SafarSuite.LocalServer.ComposeBootstrapProof\SafarSuite.LocalServer.ComposeBootstrapProof.csproj -- run-compose --repo-root C:\Users\Daniyal\Documents\Codex\provider-office-erp --output C:\Users\Daniyal\Documents\Codex\provider-office-erp\artifacts\codex\clean-machine-rehearsal-20260710 --port 51892 --local-port 18143 --app-port 18144 --include-app-runtime true --verify-app-runtime true --cleanup-compose true --runtime-wait-seconds 180
```

Verified runtime surface:

- Local API: `https://127.0.0.1:18143` with `GeneratedLocalCa`.
- App runtime: `http://127.0.0.1:18144`, `/health` returned HTTP `200`.
- Bootstrap registration: `Registered`.
- Cloud registration: `Active`.
- Entitlement pull: version `1`, license status `Active`.
- Heartbeat: `Received`.
- Module access: `Accounting` allowed with `Active` access state.
- Pairing mode: `ManagerApproval`.
- Pairing lifecycle proof: first manager device `Approved`, new device request `Pending`, approved device `Approved`, revoked device `Revoked`.
- Runtime state: bootstrap configuration and cached entitlement present.

Notes:

- The first command used a relative output path and the tool resolved it below the tool project folder; the final evidence run used an absolute output path under the repo-level `artifacts/codex` directory.
- This was a fast clean-package rehearsal against the proof stub cloud. The next deeper pass should run the generated real Control Cloud customer setup package, diagnostics command/upload, app activation import, and rerun-secret proof again with today’s app/pairing state.

## 2026-07-08 Final Live Proof Notes

Passed artifacts:

- Package root: `artifacts/codex/clean-machine-live-proof-20260708/package`.
- Target root: `artifacts/codex/clean-machine-live-proof-20260708/target`.
- Final Local API URL: `https://127.0.0.1:19080`.
- Final app runtime URL: `http://127.0.0.1:19081`.

Verified runtime surface:

- LocalServer bootstrap registration: `Registered`.
- Entitlement pull: `Active`, cached entitlement version `639190761734034536`.
- Heartbeat: `Received`.
- Module access: `Accounting` allowed through the module gateway.
- Diagnostics command: `request_diagnostics` applied and acknowledged; latest report `d93de61f-223d-4de5-b185-61e1f7af1b42` is `Received` with `Active` license status.
- App activation: `Active`, signing key `clean-proof-app-activation-2026-07`, with `Accounting` allowed.
- Rerun proof: `clean-machine-rerun-secret-proof.sh` passed and generated secret hashes matched across installer reruns.

Fixes made during the run:

- LocalServer runtime image now strips CRLF from shell entrypoint shims before `chmod`, fixing `/usr/bin/env: 'sh\r': No such file or directory`.
- Compose proof tool now loads PEM trust anchors with `X509Certificate2.CreateFromPem`, so generated CA files are readable by the verifier.
- Generated LocalServer env now includes bootstrap and entitlement trust keys matching the real Control Cloud bootstrap/entitlement signing key.
- Clean-machine install and verify scripts add `--ssl-no-revoke` when Git for Windows curl uses Schannel, while retaining CA-pinned TLS.
- Generated local CA config now marks the CA certificate with `basicConstraints = CA:TRUE`.
- LocalServer bootstrap import is idempotent for the same already-registered signed bundle, so reruns do not re-consume setup tokens.

Open items from this run:

- For Docker-host callbacks, local Control Cloud should bind to `0.0.0.0:5159` when the bundle advertises `http://host.docker.internal:5159`.
- For this proof, Control Cloud HMAC receiver and entitlement secrets were supplied inline to avoid ambiguity from development inline defaults plus file-backed overrides.
- The next product slice is deciding what moves from proof scripts into the Control Desk customer setup UX.

## Proof Environment V1

This proof is intentionally one notch stricter than the existing compose proof:

- Control Cloud runs from this workspace with `Persistence__Provider=Postgres` against the repository Docker PostgreSQL service on `127.0.0.1:54329`.
- Bootstrap package `CloudBaseUrl` is `http://host.docker.internal:5159` so LocalServer and app-runtime containers can call back to the host Control Cloud.
- The clean target is a Linux Docker host, WSL distro, or VM with no existing `safarsuite-local-server` compose project, no existing SafarSuite config/state directories, and both images available locally.
- The installer runs with `SAFARSUITE_START_COMPOSE=true`, `SAFARSUITE_IMPORT_BOOTSTRAP_BUNDLE_AFTER_START=true`, and `COMPOSE_PROFILES=app-runtime`.
- Control Cloud provider, receiver, entitlement/bootstrap/command, and app activation signing secrets come from files. Local deployment secrets are generated by `install.sh` under the target state directory and must survive reruns.

Proof workspace:

```text
artifacts/codex/clean-machine-deployment-proof/
  cloud-secrets/
  localserver-api-publish/
  package/
  target/
    etc/
    var/
    first-secret-sha256.txt
    second-secret-sha256.txt
```

Target ports:

```text
Control Cloud host API: http://127.0.0.1:5159
Container-visible cloud URL: http://host.docker.internal:5159
LocalServer API: https://127.0.0.1:18080
SafarSuite app runtime: http://127.0.0.1:18081
PostgreSQL: 127.0.0.1:54329
```

## Command Spine

Create proof secret files from PowerShell:

```powershell
$proofRoot = "artifacts/codex/clean-machine-deployment-proof"
$secretRoot = Join-Path $proofRoot "cloud-secrets"
New-Item -ItemType Directory -Force -Path $secretRoot | Out-Null

function New-ProofSecretFile($name) {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    [Convert]::ToHexString($bytes).ToLowerInvariant() |
        Set-Content -NoNewline (Join-Path $secretRoot $name)
}

New-ProofSecretFile "provider-access-shared-secret"
New-ProofSecretFile "provider-session-signing-secret"
New-ProofSecretFile "provider-session-2026-07"
New-ProofSecretFile "provider-totp-protection-secret"
New-ProofSecretFile "control-desk-receiver-secret"
New-ProofSecretFile "control-cloud-entitlement-secret"

$ecdsa = [System.Security.Cryptography.ECDsa]::Create([System.Security.Cryptography.ECCurve]::CreateFromFriendlyName("nistP256"))
[System.IO.File]::WriteAllText(
    (Join-Path $secretRoot "app-activation-public.pem"),
    $ecdsa.ExportSubjectPublicKeyInfoPem())
[System.IO.File]::WriteAllText(
    (Join-Path $secretRoot "app-activation-private.pem"),
    $ecdsa.ExportPkcs8PrivateKeyPem())
$ecdsa.Dispose()
```

Start PostgreSQL and build the LocalServer runtime image:

```powershell
docker compose up -d safarsuite-control-desk-postgres

dotnet publish src/SafarSuite.LocalServer.Api/SafarSuite.LocalServer.Api.csproj `
  --configuration Release `
  --output "$proofRoot/localserver-api-publish"

docker build `
  -f src/SafarSuite.LocalServer.Api/Dockerfile `
  --target runtime-from-publish `
  --build-arg "SAFARSUITE_LOCAL_SERVER_PUBLISH_DIR=$proofRoot/localserver-api-publish" `
  -t safarsuite-local-server:clean-proof `
  .
```

Start Control Cloud in a separate PowerShell terminal:

```powershell
$proofRoot = "artifacts/codex/clean-machine-deployment-proof"
$secretRoot = Join-Path $proofRoot "cloud-secrets"

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5159"
$env:Persistence__Provider = "Postgres"
$env:ConnectionStrings__ControlCloud = "Host=127.0.0.1;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password"
$env:ControlCloud__BootstrapPackages__CloudBaseUrl = "http://host.docker.internal:5159"
$env:ControlCloud__Receiver__SigningKeys__0__KeyId = "local-dev"
$env:ControlCloud__Receiver__SigningKeys__0__SecretFile = (Resolve-Path (Join-Path $secretRoot "control-desk-receiver-secret")).Path
$env:ControlCloud__EntitlementSigning__ActiveKeyId = "clean-proof-entitlement-2026-07"
$env:ControlCloud__EntitlementSigning__SigningKeys__0__KeyId = "clean-proof-entitlement-2026-07"
$env:ControlCloud__EntitlementSigning__SigningKeys__0__SecretFile = (Resolve-Path (Join-Path $secretRoot "control-cloud-entitlement-secret")).Path
$env:ControlCloud__AppActivationSigning__ActiveKeyId = "clean-proof-app-activation-2026-07"
$env:ControlCloud__AppActivationSigning__PublicKeyPemFile = (Resolve-Path (Join-Path $secretRoot "app-activation-public.pem")).Path
$env:ControlCloud__AppActivationSigning__PrivateKeyPemFile = (Resolve-Path (Join-Path $secretRoot "app-activation-private.pem")).Path
$env:ClientPortal__ProviderAccess__SharedSecretFile = (Resolve-Path (Join-Path $secretRoot "provider-access-shared-secret")).Path
$env:ClientPortal__ProviderAccess__SessionSigningSecretFile = (Resolve-Path (Join-Path $secretRoot "provider-session-signing-secret")).Path
$env:ClientPortal__ProviderAccess__TotpProtectionSecretFile = (Resolve-Path (Join-Path $secretRoot "provider-totp-protection-secret")).Path
$env:ClientPortal__ProviderAccess__ActiveSessionSigningKeyId = "provider-session-2026-07"
$env:ClientPortal__ProviderAccess__SessionSigningKeys__0__KeyId = "provider-session-2026-07"
$env:ClientPortal__ProviderAccess__SessionSigningKeys__0__SecretFile = (Resolve-Path (Join-Path $secretRoot "provider-session-2026-07")).Path

dotnet run --no-launch-profile --project src/SafarSuite.ControlCloud.Api/SafarSuite.ControlCloud.Api.csproj
```

Generate a real Control Cloud bootstrap package from the first terminal:

```powershell
dotnet run --project tools/SafarSuite.LocalServer.ComposeBootstrapProof/SafarSuite.LocalServer.ComposeBootstrapProof.csproj -- `
  generate-real-cloud `
  --control-cloud-api-url http://127.0.0.1:5159 `
  --output "$proofRoot/package" `
  --client-id 11111111-1111-1111-1111-111111111111 `
  --installation-id clean-machine-main `
  --site-id hq-clean-machine `
  --local-server-version clean-proof-localserver `
  --app-version 0.1.0 `
  --local-server-image safarsuite-local-server:clean-proof `
  --app-image ghcr.io/danionwheels/localserver:0.1.0 `
  --local-port 18080 `
  --app-port 18081 `
  --control-desk-signing-secret-file "$secretRoot/control-desk-receiver-secret" `
  --provider-access-secret-file "$secretRoot/provider-access-shared-secret"
```

`generate-real-cloud` now writes these clean-target helpers beside the package artifacts:

```text
clean-machine-install.sh
clean-machine-verify.sh
clean-machine-rerun-secret-proof.sh
```

Preferred clean-target path:

```bash
proof_root="$PWD/artifacts/codex/clean-machine-deployment-proof"
package_dir="$proof_root/package"
target_root="$proof_root/target"

chmod +x "$package_dir"/clean-machine-*.sh
SAFARSUITE_CLEAN_TARGET_ROOT="$target_root" "$package_dir/clean-machine-install.sh"
SAFARSUITE_CLEAN_TARGET_ROOT="$target_root" "$package_dir/clean-machine-verify.sh"
```

Run the generated installer on the clean target from Git Bash, WSL, or the Linux VM:

```bash
set -euo pipefail

proof_root="$PWD/artifacts/codex/clean-machine-deployment-proof"
package_dir="$proof_root/package"
target_root="$proof_root/target"
mkdir -p "$target_root/etc" "$target_root/var"

client_id="$(jq -r '.clientId' "$package_dir/control-cloud-bootstrap-package.json")"
installation_id="$(jq -r '.installationId' "$package_dir/control-cloud-bootstrap-package.json")"
setup_token="$(jq -r '.setupToken' "$package_dir/control-cloud-bootstrap-package.json")"
bundle_sha256="$(sha256sum "$package_dir/bootstrap-bundle.json" | awk '{print $1}')"

curl -fsSL "http://127.0.0.1:5159/install/safarsuite-local-server/install.sh" \
  -o "$package_dir/safarsuite-install.sh"
chmod +x "$package_dir/safarsuite-install.sh"

COMPOSE_PROFILES=app-runtime \
SAFARSUITE_CONTROL_CLOUD_URL="http://host.docker.internal:5159" \
SAFARSUITE_CLIENT_ID="$client_id" \
SAFARSUITE_INSTALLATION_ID="$installation_id" \
SAFARSUITE_SETUP_TOKEN="$setup_token" \
SAFARSUITE_BOOTSTRAP_MODE="OnlineBootstrap" \
SAFARSUITE_CLIENT_DEPLOYMENT_MODE="CloudSyncMultiBranch" \
SAFARSUITE_SITE_ID="hq-clean-machine" \
SAFARSUITE_SITE_ROLE="Hq" \
SAFARSUITE_BRANCH_CODE="HQ" \
SAFARSUITE_SYNC_TOPOLOGY_ID="clean-machine-proof" \
SAFARSUITE_LOCAL_SERVER_VERSION="clean-proof-localserver" \
SAFARSUITE_APP_VERSION="0.1.0" \
SAFARSUITE_LOCAL_SERVER_IMAGE="safarsuite-local-server:clean-proof" \
SAFARSUITE_APP_IMAGE="ghcr.io/danionwheels/localserver:0.1.0" \
SAFARSUITE_LOCAL_SERVER_CONFIG_DIR="$target_root/etc" \
SAFARSUITE_LOCAL_SERVER_STATE_DIR="$target_root/var" \
SAFARSUITE_LOCAL_SERVER_HTTP_PORT="18080" \
SAFARSUITE_APP_HTTP_PORT="18081" \
SAFARSUITE_LOCAL_API_PUBLIC_URL="https://127.0.0.1:18080" \
SAFARSUITE_BOOTSTRAP_BUNDLE_FILE="$package_dir/bootstrap-bundle.json" \
SAFARSUITE_BOOTSTRAP_BUNDLE_SHA256="$bundle_sha256" \
SAFARSUITE_START_COMPOSE="true" \
SAFARSUITE_IMPORT_BOOTSTRAP_BUNDLE_AFTER_START="true" \
bash "$package_dir/safarsuite-install.sh"
```

Verify the running target:

```bash
curl -fsS --cacert "$target_root/etc/certs/trust/local-api-ca.pem" https://127.0.0.1:18080/health
curl -fsS --cacert "$target_root/etc/certs/trust/local-api-ca.pem" -X POST https://127.0.0.1:18080/api/v1/local-server/entitlement/pull
curl -fsS --cacert "$target_root/etc/certs/trust/local-api-ca.pem" -X POST https://127.0.0.1:18080/api/v1/local-server/heartbeat
curl -fsS --cacert "$target_root/etc/certs/trust/local-api-ca.pem" "https://127.0.0.1:18080/api/v1/local-server/modules/Accounting/access?requestedBy=clean-machine-proof"
```

Queue and process diagnostics:

```powershell
$package = Get-Content "$proofRoot/package/control-cloud-bootstrap-package.json" | ConvertFrom-Json
$command = @{
  commandType = "request_diagnostics"
  payload = @{
    reason = "clean-machine deployment proof"
    requestedBy = "codex"
  }
  expiresAtUtc = [DateTimeOffset]::UtcNow.AddHours(1)
  idempotencyKey = "clean-machine-proof:diagnostics:001"
}

Invoke-RestMethod `
  -Method Post `
  -Uri "http://127.0.0.1:5159/api/v1/control-cloud/clients/$($package.clientId)/installations/$($package.installationId)/commands" `
  -Body ($command | ConvertTo-Json -Depth 8) `
  -ContentType "application/json"
```

```bash
curl -fsS --cacert "$target_root/etc/certs/trust/local-api-ca.pem" \
  -X POST https://127.0.0.1:18080/api/v1/local-server/commands/process
```

Activate app runtime after the app reports healthy:

```powershell
dotnet run --project tools/SafarSuite.LocalServer.ComposeBootstrapProof/SafarSuite.LocalServer.ComposeBootstrapProof.csproj -- `
  activate-app-runtime `
  --control-cloud-api-url http://127.0.0.1:5159 `
  --client-id 11111111-1111-1111-1111-111111111111 `
  --installation-id clean-machine-main `
  --app-port 18081 `
  --provider-access-secret-file "$secretRoot/provider-access-shared-secret"
```

Capture rerun-stability evidence on the clean target:

```bash
SAFARSUITE_CLEAN_TARGET_ROOT="$target_root" "$package_dir/clean-machine-rerun-secret-proof.sh"
```

Expanded manual equivalent:

```bash
find "$target_root/var/secrets" "$target_root/var/certs-private/local-api" -type f -print0 |
  sort -z |
  xargs -0 sha256sum > "$target_root/first-secret-sha256.txt"

# Run the same installer command again.

find "$target_root/var/secrets" "$target_root/var/certs-private/local-api" -type f -print0 |
  sort -z |
  xargs -0 sha256sum > "$target_root/second-secret-sha256.txt"

diff -u "$target_root/first-secret-sha256.txt" "$target_root/second-secret-sha256.txt"
```

Expected rerun result: no diff. The installer may rewrite `local-server.env`, `bootstrap.env`, compose, and runtime manifest, but generated secret files must keep the same content hashes.

## Failure Triage

| Symptom | First check |
| --- | --- |
| Containers cannot reach Control Cloud | Confirm package `cloudBaseUrl` is `http://host.docker.internal:5159`, not `127.0.0.1`. |
| App container does not start | Confirm `COMPOSE_PROFILES=app-runtime` was present when `install.sh` called `docker compose up`. |
| Local API HTTPS curl fails | Use `$target_root/etc/certs/trust/local-api-ca.pem`; do not use `-k` for proof evidence. |
| Bundle import fails with setup-token errors | Use a newly generated package; setup tokens are one-time. |
| Diagnostics command applies but no cloud report exists | Check `SAFARSUITE_DIAGNOSTICS_URL` in `$target_root/etc/local-server.env` and Control Cloud latest diagnostics endpoint. |
| Rerun diff changes under `var/secrets` | Treat as production blocker; generated deployment secrets rotated. |

## Done When

- [ ] The proof can be repeated from a clean target without manual code edits.
- [ ] All required secrets come from generated or file-backed custody, not inline development defaults.
- [ ] The final notes clearly separate real production blockers from later polish.
