#!/usr/bin/env bash
set -euo pipefail

require_env() {
  name="$1"
  value="${!name:-}"

  if [ -z "$value" ]; then
    echo "Missing required environment variable: $name" >&2
    exit 2
  fi
}

json_escape() {
  printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

wait_for_local_api() {
  api_base_url="$1"
  attempts="${SAFARSUITE_LOCAL_API_WAIT_ATTEMPTS:-30}"
  sleep_seconds="${SAFARSUITE_LOCAL_API_WAIT_SECONDS:-2}"
  attempt=1

  while [ "$attempt" -le "$attempts" ]; do
    if curl -fsS "${local_api_curl_args[@]}" "$api_base_url/health" >/dev/null 2>&1; then
      return 0
    fi

    sleep "$sleep_seconds"
    attempt=$((attempt + 1))
  done

  echo "Local API did not become healthy at $api_base_url before timeout." >&2
  exit 7
}

local_api_has_current_bootstrap() {
  api_base_url="$1"

  bootstrap_status="$(curl -fsS "${local_api_curl_args[@]}" "$api_base_url/api/v1/local-server/bootstrap" 2>/dev/null || true)"
  if [ -z "$bootstrap_status" ]; then
    return 1
  fi

  printf '%s' "$bootstrap_status" | grep -Fq '"hasBootstrapConfiguration":true' &&
    printf '%s' "$bootstrap_status" | grep -Fq "\"installationId\":\"$installation_id\""
}

append_local_api_curl_compat_args() {
  if curl --version 2>/dev/null | head -n 1 | grep -qi 'Schannel' &&
     curl --help all 2>/dev/null | grep -q -- '--ssl-no-revoke'; then
    local_api_curl_args+=(--ssl-no-revoke)
  fi
}

generate_secret() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -hex 32
    return
  fi

  if [ -r /dev/urandom ] && command -v od >/dev/null 2>&1; then
    od -An -tx1 -N32 /dev/urandom | tr -d ' \n'
    return
  fi

  echo "Unable to generate a deployment secret; set it before running install." >&2
  exit 8
}

read_or_generate_secret() {
  secret_file="$1"
  provided_value="$2"

  if [ -n "$provided_value" ] && [ "$provided_value" != "change-me-before-start" ]; then
    printf '%s' "$provided_value"
    return
  fi

  if [ -f "$secret_file" ]; then
    cat "$secret_file"
    return
  fi

  generated_secret="$(generate_secret)"
  printf '%s' "$generated_secret" > "$secret_file"
  chmod 600 "$secret_file"
  printf '%s' "$generated_secret"
}

normalize_local_api_tls_mode() {
  case "$1" in
    ""|HttpOnly|http-only|http_only|httponly)
      printf '%s' "HttpOnly"
      ;;
    GeneratedLocalCa|generated-local-ca|generated_local_ca|generatedlocalca)
      printf '%s' "GeneratedLocalCa"
      ;;
    SuppliedCertificate|supplied-certificate|supplied_certificate|suppliedcertificate)
      printf '%s' "SuppliedCertificate"
      ;;
    *)
      echo "Unsupported SAFARSUITE_LOCAL_API_TLS_MODE: $1" >&2
      exit 9
      ;;
  esac
}

write_local_api_ca_certificate_config() {
  ca_config_file="$1"
  subject_cn="$2"

cat > "$ca_config_file" <<EOF
[req]
distinguished_name = req_distinguished_name
x509_extensions = v3_ca
prompt = no

[req_distinguished_name]
CN = $subject_cn

[v3_ca]
basicConstraints = critical, CA:TRUE, pathlen:0
keyUsage = critical, keyCertSign, cRLSign
subjectKeyIdentifier = hash
EOF
}

write_local_api_certificate_config() {
  cert_config_file="$1"
  dns_names="$2"
  ip_addresses="$3"
  subject_cn="$4"

  cat > "$cert_config_file" <<EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
CN = $subject_cn

[v3_req]
basicConstraints = critical, CA:FALSE
keyUsage = critical, digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
EOF

  dns_index=1
  IFS=',' read -r -a dns_name_parts <<< "$dns_names"
  for dns_name in "${dns_name_parts[@]}"; do
    dns_name="$(printf '%s' "$dns_name" | xargs)"
    if [ -n "$dns_name" ]; then
      printf 'DNS.%s = %s\n' "$dns_index" "$dns_name" >> "$cert_config_file"
      dns_index=$((dns_index + 1))
    fi
  done

  ip_index=1
  IFS=',' read -r -a ip_address_parts <<< "$ip_addresses"
  for ip_address in "${ip_address_parts[@]}"; do
    ip_address="$(printf '%s' "$ip_address" | xargs)"
    if [ -n "$ip_address" ]; then
      printf 'IP.%s = %s\n' "$ip_index" "$ip_address" >> "$cert_config_file"
      ip_index=$((ip_index + 1))
    fi
  done
}

generate_local_api_certificates() {
  if ! command -v openssl >/dev/null 2>&1; then
    echo "openssl is required when SAFARSUITE_LOCAL_API_TLS_MODE=GeneratedLocalCa." >&2
    exit 10
  fi

  local_api_cert_dir="$1"
  local_api_trust_dir="$2"
  local_api_private_dir="$3"
  certificate_password="$4"
  dns_names="$5"
  ip_addresses="$6"
  certificate_days="$7"

  ca_key_file="$local_api_private_dir/local-api-ca.key"
  ca_cert_file="$local_api_trust_dir/local-api-ca.pem"
  ca_serial_file="$local_api_private_dir/local-api-ca.srl"
  ca_config_file="$local_api_private_dir/local-api-ca.openssl.cnf"
  server_key_file="$local_api_private_dir/local-api-server.key"
  server_csr_file="$local_api_private_dir/local-api-server.csr"
  server_cert_file="$local_api_cert_dir/local-api-server.crt"
  server_pfx_file="$local_api_cert_dir/local-api-server.pfx"
  server_config_file="$local_api_private_dir/local-api-server.openssl.cnf"

  if [ -f "$server_pfx_file" ] && [ -f "$ca_cert_file" ]; then
    return
  fi

  openssl genrsa -out "$ca_key_file" 4096 >/dev/null 2>&1
  write_local_api_ca_certificate_config "$ca_config_file" "SafarSuite Local API $installation_id CA"
  openssl req \
    -x509 \
    -new \
    -nodes \
    -key "$ca_key_file" \
    -sha256 \
    -days "$certificate_days" \
    -config "$ca_config_file" \
    -out "$ca_cert_file" >/dev/null 2>&1

  openssl genrsa -out "$server_key_file" 2048 >/dev/null 2>&1
  write_local_api_certificate_config "$server_config_file" "$dns_names" "$ip_addresses" "local-api"
  openssl req \
    -new \
    -key "$server_key_file" \
    -out "$server_csr_file" \
    -config "$server_config_file" >/dev/null 2>&1
  openssl x509 \
    -req \
    -in "$server_csr_file" \
    -CA "$ca_cert_file" \
    -CAkey "$ca_key_file" \
    -CAcreateserial \
    -CAserial "$ca_serial_file" \
    -out "$server_cert_file" \
    -days "$certificate_days" \
    -sha256 \
    -extensions v3_req \
    -extfile "$server_config_file" >/dev/null 2>&1
  openssl pkcs12 \
    -export \
    -out "$server_pfx_file" \
    -inkey "$server_key_file" \
    -in "$server_cert_file" \
    -certfile "$ca_cert_file" \
    -passout "pass:$certificate_password" >/dev/null 2>&1

  chmod 600 "$local_api_private_dir"/* 2>/dev/null || true
  chmod 644 "$ca_cert_file" "$server_cert_file" "$server_pfx_file" 2>/dev/null || true
}

require_env SAFARSUITE_CONTROL_CLOUD_URL
require_env SAFARSUITE_CLIENT_ID
require_env SAFARSUITE_INSTALLATION_ID
require_env SAFARSUITE_SETUP_TOKEN

control_cloud_url="${SAFARSUITE_CONTROL_CLOUD_URL%/}"
client_id="$SAFARSUITE_CLIENT_ID"
installation_id="$SAFARSUITE_INSTALLATION_ID"
setup_token="$SAFARSUITE_SETUP_TOKEN"
bootstrap_mode="${SAFARSUITE_BOOTSTRAP_MODE:-OnlineBootstrap}"
client_deployment_mode="${SAFARSUITE_CLIENT_DEPLOYMENT_MODE:-OfflineLocal}"
site_id="${SAFARSUITE_SITE_ID:-$installation_id}"
site_role="${SAFARSUITE_SITE_ROLE:-Standalone}"
parent_site_id="${SAFARSUITE_PARENT_SITE_ID:-}"
branch_code="${SAFARSUITE_BRANCH_CODE:-}"
sync_topology_id="${SAFARSUITE_SYNC_TOPOLOGY_ID:-}"
local_server_version="${SAFARSUITE_LOCAL_SERVER_VERSION:-latest}"
safarsuite_app_version="${SAFARSUITE_APP_VERSION:-$local_server_version}"
register_now="${SAFARSUITE_REGISTER_NOW:-true}"
start_compose="${SAFARSUITE_START_COMPOSE:-false}"
import_bundle_after_start="${SAFARSUITE_IMPORT_BOOTSTRAP_BUNDLE_AFTER_START:-true}"
config_dir="${SAFARSUITE_LOCAL_SERVER_CONFIG_DIR:-/etc/safarsuite/local-server}"
state_dir="${SAFARSUITE_LOCAL_SERVER_STATE_DIR:-/var/lib/safarsuite/local-server}"
bundle_file="${SAFARSUITE_BOOTSTRAP_BUNDLE_FILE:-}"
bundle_sha256="${SAFARSUITE_BOOTSTRAP_BUNDLE_SHA256:-}"
local_server_image="${SAFARSUITE_LOCAL_SERVER_IMAGE:-ghcr.io/safarsuite/local-server:$local_server_version}"
local_server_http_port="${SAFARSUITE_LOCAL_SERVER_HTTP_PORT:-8080}"
local_server_container_config_dir="${SAFARSUITE_LOCAL_SERVER_CONTAINER_CONFIG_DIR:-/etc/safarsuite/local-server}"
local_server_container_state_dir="${SAFARSUITE_LOCAL_SERVER_CONTAINER_STATE_DIR:-/var/lib/safarsuite/local-server}"
safarsuite_app_image="${SAFARSUITE_APP_IMAGE:-ghcr.io/danionwheels/localserver:$safarsuite_app_version}"
safarsuite_app_http_port="${SAFARSUITE_APP_HTTP_PORT:-5280}"
local_api_access_key="${SAFARSUITE_LOCAL_API_ACCESS_KEY:-}"
manager_session_signing_key_id="${SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_KEY_ID:-${LocalServer__ManagerSessions__SigningKeyId:-safarsuite-local-manager-session}}"
manager_session_signing_secret="${SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_SECRET:-${LocalServer__ManagerSessions__SigningSecret:-}}"
manager_session_minutes="${SAFARSUITE_LOCAL_MANAGER_SESSION_MINUTES:-${LocalServer__ManagerSessions__SessionMinutes:-60}}"
local_api_tls_mode="$(normalize_local_api_tls_mode "${SAFARSUITE_LOCAL_API_TLS_MODE:-GeneratedLocalCa}")"
if [ -n "${SAFARSUITE_LOCAL_API_BASE_URL:-}" ]; then
  local_api_base_url="$SAFARSUITE_LOCAL_API_BASE_URL"
elif [ "$local_api_tls_mode" = "HttpOnly" ]; then
  local_api_base_url="http://local-api:8080"
else
  local_api_base_url="https://local-api:8080"
fi
if [ -n "${SAFARSUITE_LOCAL_API_PUBLIC_URL:-}" ]; then
  local_api_public_url="$SAFARSUITE_LOCAL_API_PUBLIC_URL"
elif [ "$local_api_tls_mode" = "HttpOnly" ]; then
  local_api_public_url="http://127.0.0.1:$local_server_http_port"
else
  local_api_public_url="https://127.0.0.1:$local_server_http_port"
fi
if [ -n "${SAFARSUITE_LOCAL_API_ASPNETCORE_URLS:-}" ]; then
  local_api_aspnetcore_urls="$SAFARSUITE_LOCAL_API_ASPNETCORE_URLS"
elif [ "$local_api_tls_mode" = "HttpOnly" ]; then
  local_api_aspnetcore_urls="http://0.0.0.0:8080"
else
  local_api_aspnetcore_urls="https://0.0.0.0:8080"
fi
local_api_certificate_path="${SAFARSUITE_LOCAL_API_CERTIFICATE_PATH:-}"
local_api_certificate_password="${SAFARSUITE_LOCAL_API_CERTIFICATE_PASSWORD:-}"
local_api_ca_certificate_path="${SAFARSUITE_LOCAL_API_CA_CERTIFICATE_PATH:-}"
local_api_certificate_dns_names="${SAFARSUITE_LOCAL_API_CERTIFICATE_DNS_NAMES:-local-api,localhost}"
local_api_certificate_ip_addresses="${SAFARSUITE_LOCAL_API_CERTIFICATE_IP_ADDRESSES:-127.0.0.1}"
local_api_certificate_days="${SAFARSUITE_LOCAL_API_CERTIFICATE_DAYS:-825}"
module_gateway_url="${SAFARSUITE_MODULE_GATEWAY_URL:-$local_api_base_url}"
runtime_manifest_path="${SAFARSUITE_RUNTIME_MANIFEST_PATH:-$local_server_container_config_dir/runtime-services.manifest.json}"
local_db_image="${SAFARSUITE_LOCAL_DB_IMAGE:-postgres:16-alpine}"
local_db_name="${SAFARSUITE_LOCAL_DB_NAME:-safarsuite_local}"
local_db_user="${SAFARSUITE_LOCAL_DB_USER:-safarsuite}"
local_db_password="${SAFARSUITE_LOCAL_DB_PASSWORD:-}"
entitlement_trust_signing_key_id="${SAFARSUITE_ENTITLEMENT_SIGNING_KEY_ID:-${LocalServer__EntitlementTrust__SigningKeys__0__KeyId:-local-entitlement-dev}}"
entitlement_trust_signing_secret="${SAFARSUITE_ENTITLEMENT_SIGNING_SECRET:-${LocalServer__EntitlementTrust__SigningKeys__0__Secret:-local-entitlement-signing-secret-change-before-cloud}}"
bootstrap_trust_signing_key_id="${SAFARSUITE_BOOTSTRAP_TRUST_SIGNING_KEY_ID:-${SAFARSUITE_BOOTSTRAP_SIGNING_KEY_ID:-${LocalServer__BootstrapTrust__SigningKeys__0__KeyId:-$entitlement_trust_signing_key_id}}}"
bootstrap_trust_signing_secret="${SAFARSUITE_BOOTSTRAP_TRUST_SIGNING_SECRET:-${SAFARSUITE_BOOTSTRAP_SIGNING_SECRET:-${LocalServer__BootstrapTrust__SigningKeys__0__Secret:-$entitlement_trust_signing_secret}}}"
app_activation_signing_key_id="${SAFARSUITE_APP_ACTIVATION_SIGNING_KEY_ID:-${ActivationSigning__SigningKeyId:-}}"
app_activation_public_key_pem="${SAFARSUITE_APP_ACTIVATION_PUBLIC_KEY_PEM:-${ActivationSigning__PublicKeyPem:-}}"
app_device_signing_key_id="${SAFARSUITE_APP_DEVICE_SIGNING_KEY_ID:-${DeviceCredentials__SigningKeyId:-safarsuite-app-device-local}}"
app_device_signing_secret="${SAFARSUITE_APP_DEVICE_SIGNING_SECRET:-${DeviceCredentials__SigningSecret:-}}"
app_device_credential_expires_days="${SAFARSUITE_APP_DEVICE_CREDENTIAL_EXPIRES_DAYS:-${DeviceCredentials__ExpiresInDays:-3650}}"
app_session_signing_key_id="${SAFARSUITE_APP_SESSION_SIGNING_KEY_ID:-${UserSessions__SigningKeyId:-safarsuite-app-session-local}}"
app_session_signing_secret="${SAFARSUITE_APP_SESSION_SIGNING_SECRET:-${UserSessions__SigningSecret:-}}"
first_manager_allow_setup_code_fallback="${SAFARSUITE_FIRST_MANAGER_ALLOW_SETUP_CODE_FALLBACK:-${FirstManagerBootstrap__AllowSetupCodeFallback:-false}}"

if [ -n "$bundle_file" ] && [ -n "$bundle_sha256" ]; then
  if ! command -v sha256sum >/dev/null 2>&1; then
    echo "sha256sum is required to verify SAFARSUITE_BOOTSTRAP_BUNDLE_FILE." >&2
    exit 3
  fi

  actual_sha256="$(sha256sum "$bundle_file" | awk '{print $1}')"

  if [ "$actual_sha256" != "$bundle_sha256" ]; then
    echo "Bootstrap bundle checksum mismatch." >&2
    exit 4
  fi
fi

local_api_host_cert_dir="$config_dir/certs/local-api"
local_api_host_trust_dir="$config_dir/certs/trust"
local_api_host_private_dir="$state_dir/certs-private/local-api"
deployment_secret_dir="$state_dir/secrets"
local_api_container_cert_dir="$local_server_container_config_dir/certs/local-api"
local_api_container_trust_dir="$local_server_container_config_dir/certs/trust"
local_api_curl_args=()

mkdir -p "$config_dir" "$state_dir" "$local_api_host_cert_dir" "$local_api_host_trust_dir" "$local_api_host_private_dir" "$deployment_secret_dir"
chmod 700 "$config_dir" "$state_dir" "$local_api_host_private_dir" "$deployment_secret_dir"
chmod 755 "$config_dir/certs" "$local_api_host_cert_dir" "$local_api_host_trust_dir"

local_api_access_key="$(read_or_generate_secret "$deployment_secret_dir/local-api-access-key" "$local_api_access_key")"
manager_session_signing_secret="$(read_or_generate_secret "$deployment_secret_dir/local-manager-session-signing-secret" "$manager_session_signing_secret")"
local_db_password="$(read_or_generate_secret "$deployment_secret_dir/local-db-password" "$local_db_password")"
app_device_signing_secret="$(read_or_generate_secret "$deployment_secret_dir/app-device-signing-secret" "$app_device_signing_secret")"
app_session_signing_secret="$(read_or_generate_secret "$deployment_secret_dir/app-session-signing-secret" "$app_session_signing_secret")"

if [ "$local_api_tls_mode" = "GeneratedLocalCa" ]; then
  local_api_certificate_password_file="$local_api_host_private_dir/local-api-server.pfx.password"

  if [ -z "$local_api_certificate_password" ] && [ -f "$local_api_certificate_password_file" ]; then
    local_api_certificate_password="$(cat "$local_api_certificate_password_file")"
  fi

  if [ -z "$local_api_certificate_password" ]; then
    local_api_certificate_password="$(generate_secret)"
    printf '%s' "$local_api_certificate_password" > "$local_api_certificate_password_file"
    chmod 600 "$local_api_certificate_password_file"
  fi

  if [ -z "$local_api_certificate_path" ]; then
    local_api_certificate_path="$local_api_container_cert_dir/local-api-server.pfx"
  fi

  if [ -z "$local_api_ca_certificate_path" ]; then
    local_api_ca_certificate_path="$local_api_container_trust_dir/local-api-ca.pem"
  fi

  generate_local_api_certificates \
    "$local_api_host_cert_dir" \
    "$local_api_host_trust_dir" \
    "$local_api_host_private_dir" \
    "$local_api_certificate_password" \
    "$local_api_certificate_dns_names" \
    "$local_api_certificate_ip_addresses" \
    "$local_api_certificate_days"
  local_api_curl_args=(--cacert "$local_api_host_trust_dir/local-api-ca.pem")
  append_local_api_curl_compat_args
elif [ "$local_api_tls_mode" = "SuppliedCertificate" ]; then
  if [ -z "$local_api_certificate_path" ]; then
    echo "SAFARSUITE_LOCAL_API_CERTIFICATE_PATH is required when SAFARSUITE_LOCAL_API_TLS_MODE=SuppliedCertificate." >&2
    exit 11
  fi
fi

registration_url="$control_cloud_url/api/v1/local-server/installations/$installation_id/registration"
entitlement_url="$control_cloud_url/api/v1/local-server/installations/$installation_id/entitlement-bundle?clientId=$client_id"
heartbeat_url="$control_cloud_url/api/v1/local-server/installations/$installation_id/heartbeat"
pending_commands_url="$control_cloud_url/api/v1/local-server/installations/$installation_id/commands/pending"
diagnostics_url="$control_cloud_url/api/v1/local-server/installations/$installation_id/diagnostics"
config_file="$config_dir/bootstrap.env"
compose_file="$config_dir/docker-compose.yml"
env_file="$config_dir/local-server.env"
runtime_manifest_file="$config_dir/runtime-services.manifest.json"

cat > "$config_file" <<EOF
SAFARSUITE_CONTROL_CLOUD_URL=$control_cloud_url
SAFARSUITE_CLIENT_ID=$client_id
SAFARSUITE_INSTALLATION_ID=$installation_id
SAFARSUITE_BOOTSTRAP_MODE=$bootstrap_mode
SAFARSUITE_CLIENT_DEPLOYMENT_MODE=$client_deployment_mode
SAFARSUITE_SITE_ID=$site_id
SAFARSUITE_SITE_ROLE=$site_role
SAFARSUITE_PARENT_SITE_ID=$parent_site_id
SAFARSUITE_BRANCH_CODE=$branch_code
SAFARSUITE_SYNC_TOPOLOGY_ID=$sync_topology_id
SAFARSUITE_LOCAL_SERVER_VERSION=$local_server_version
SAFARSUITE_REGISTRATION_URL=$registration_url
SAFARSUITE_ENTITLEMENT_BUNDLE_URL=$entitlement_url
SAFARSUITE_HEARTBEAT_URL=$heartbeat_url
SAFARSUITE_PENDING_COMMANDS_URL=$pending_commands_url
SAFARSUITE_DIAGNOSTICS_URL=$diagnostics_url
EOF

cat > "$env_file" <<EOF
SAFARSUITE_CONTROL_CLOUD_URL=$control_cloud_url
SAFARSUITE_CLIENT_ID=$client_id
SAFARSUITE_INSTALLATION_ID=$installation_id
SAFARSUITE_BOOTSTRAP_MODE=$bootstrap_mode
SAFARSUITE_CLIENT_DEPLOYMENT_MODE=$client_deployment_mode
SAFARSUITE_SITE_ID=$site_id
SAFARSUITE_SITE_ROLE=$site_role
SAFARSUITE_PARENT_SITE_ID=$parent_site_id
SAFARSUITE_BRANCH_CODE=$branch_code
SAFARSUITE_SYNC_TOPOLOGY_ID=$sync_topology_id
SAFARSUITE_LOCAL_SERVER_VERSION=$local_server_version
SAFARSUITE_LOCAL_SERVER_IMAGE=$local_server_image
SAFARSUITE_LOCAL_SERVER_HTTP_PORT=$local_server_http_port
SAFARSUITE_LOCAL_SERVER_CONFIG_DIR=$local_server_container_config_dir
SAFARSUITE_LOCAL_SERVER_STATE_DIR=$local_server_container_state_dir
SAFARSUITE_APP_VERSION=$safarsuite_app_version
SAFARSUITE_APP_IMAGE=$safarsuite_app_image
SAFARSUITE_APP_HTTP_PORT=$safarsuite_app_http_port
SAFARSUITE_LOCAL_API_BASE_URL=$local_api_base_url
SAFARSUITE_LOCAL_API_ACCESS_KEY=$local_api_access_key
SAFARSUITE_LOCAL_API_TLS_MODE=$local_api_tls_mode
SAFARSUITE_LOCAL_API_ASPNETCORE_URLS=$local_api_aspnetcore_urls
SAFARSUITE_LOCAL_API_CERTIFICATE_PATH=$local_api_certificate_path
SAFARSUITE_LOCAL_API_CERTIFICATE_PASSWORD=$local_api_certificate_password
SAFARSUITE_LOCAL_API_CA_CERTIFICATE_PATH=$local_api_ca_certificate_path
SAFARSUITE_LOCAL_API_CERTIFICATE_DNS_NAMES=$local_api_certificate_dns_names
SAFARSUITE_LOCAL_API_CERTIFICATE_IP_ADDRESSES=$local_api_certificate_ip_addresses
SAFARSUITE_LOCAL_API_CERTIFICATE_DAYS=$local_api_certificate_days
SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_KEY_ID=$manager_session_signing_key_id
SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_SECRET=$manager_session_signing_secret
SAFARSUITE_LOCAL_MANAGER_SESSION_MINUTES=$manager_session_minutes
SAFARSUITE_MODULE_GATEWAY_URL=$module_gateway_url
SAFARSUITE_RUNTIME_MANIFEST_PATH=$runtime_manifest_path
SAFARSUITE_LOCAL_DB_IMAGE=$local_db_image
SAFARSUITE_LOCAL_DB_NAME=$local_db_name
SAFARSUITE_LOCAL_DB_USER=$local_db_user
SAFARSUITE_LOCAL_DB_PASSWORD=$local_db_password
SAFARSUITE_REGISTRATION_URL=$registration_url
SAFARSUITE_ENTITLEMENT_BUNDLE_URL=$entitlement_url
SAFARSUITE_HEARTBEAT_URL=$heartbeat_url
SAFARSUITE_PENDING_COMMANDS_URL=$pending_commands_url
SAFARSUITE_DIAGNOSTICS_URL=$diagnostics_url
DeploymentSecrets__Provider=Environment
LocalServer__BootstrapTrust__SigningKeys__0__KeyId=$bootstrap_trust_signing_key_id
LocalServer__BootstrapTrust__SigningKeys__0__Secret=$bootstrap_trust_signing_secret
LocalServer__EntitlementTrust__SigningKeys__0__KeyId=$entitlement_trust_signing_key_id
LocalServer__EntitlementTrust__SigningKeys__0__Secret=$entitlement_trust_signing_secret
ActivationSigning__SigningKeyId=$app_activation_signing_key_id
ActivationSigning__PublicKeyPem=$app_activation_public_key_pem
DeviceCredentials__SigningKeyId=$app_device_signing_key_id
DeviceCredentials__SigningSecret=$app_device_signing_secret
DeviceCredentials__ExpiresInDays=$app_device_credential_expires_days
UserSessions__SigningKeyId=$app_session_signing_key_id
UserSessions__SigningSecret=$app_session_signing_secret
FirstManagerBootstrap__AllowSetupCodeFallback=$first_manager_allow_setup_code_fallback
EOF

cat > "$runtime_manifest_file" <<EOF
{
  "runtimeMode": "DockerCompose",
  "composeProjectName": "safarsuite-local-server",
  "configDirectory": "$(json_escape "$local_server_container_config_dir")",
  "stateDirectory": "$(json_escape "$local_server_container_state_dir")",
  "localServerVersion": "$(json_escape "$local_server_version")",
  "safarSuiteAppVersion": "$(json_escape "$safarsuite_app_version")",
  "services": [
    {
      "serviceName": "local-api",
      "serviceRole": "Local entitlement, diagnostics, and module-gateway API",
      "startsByDefault": true,
      "composeProfile": null,
      "imageEnvironmentVariable": "SAFARSUITE_LOCAL_SERVER_IMAGE",
      "publishedPortEnvironmentVariable": "SAFARSUITE_LOCAL_SERVER_HTTP_PORT",
      "internalBaseUrl": "$(json_escape "$local_api_base_url")",
      "healthUrl": "$(json_escape "$local_api_base_url")/health",
      "dependsOn": ["local-db"]
    },
    {
      "serviceName": "local-worker",
      "serviceRole": "Background entitlement pull and heartbeat reporting",
      "startsByDefault": true,
      "composeProfile": null,
      "imageEnvironmentVariable": "SAFARSUITE_LOCAL_SERVER_IMAGE",
      "publishedPortEnvironmentVariable": null,
      "internalBaseUrl": "n/a",
      "healthUrl": "n/a",
      "dependsOn": ["local-db"]
    },
    {
      "serviceName": "local-agent",
      "serviceRole": "Support command polling, diagnostics, and acknowledgement bridge",
      "startsByDefault": true,
      "composeProfile": null,
      "imageEnvironmentVariable": "SAFARSUITE_LOCAL_SERVER_IMAGE",
      "publishedPortEnvironmentVariable": null,
      "internalBaseUrl": "n/a",
      "healthUrl": "n/a",
      "dependsOn": ["local-db"]
    },
    {
      "serviceName": "safarsuite-app",
      "serviceRole": "Customer-facing SafarSuite application runtime",
      "startsByDefault": false,
      "composeProfile": "app-runtime",
      "imageEnvironmentVariable": "SAFARSUITE_APP_IMAGE",
      "publishedPortEnvironmentVariable": "SAFARSUITE_APP_HTTP_PORT",
      "internalBaseUrl": "http://safarsuite-app:5280",
      "healthUrl": "http://safarsuite-app:5280/health",
      "dependsOn": ["local-api", "local-db"]
    }
  ]
}
EOF

cat > "$compose_file" <<'EOF'
name: safarsuite-local-server

services:
  local-db:
    image: ${SAFARSUITE_LOCAL_DB_IMAGE:-postgres:16-alpine}
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${SAFARSUITE_LOCAL_DB_NAME:-safarsuite_local}
      POSTGRES_USER: ${SAFARSUITE_LOCAL_DB_USER:-safarsuite}
      POSTGRES_PASSWORD: ${SAFARSUITE_LOCAL_DB_PASSWORD:?Set SAFARSUITE_LOCAL_DB_PASSWORD}
    volumes:
      - safarsuite-local-db:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${SAFARSUITE_LOCAL_DB_USER:-safarsuite} -d ${SAFARSUITE_LOCAL_DB_NAME:-safarsuite_local}"]
      interval: 10s
      timeout: 5s
      retries: 5

  local-api:
    image: ${SAFARSUITE_LOCAL_SERVER_IMAGE:?Set SAFARSUITE_LOCAL_SERVER_IMAGE}
    restart: unless-stopped
    command: ["safarsuite-local-api"]
    env_file:
      - ./local-server.env
    depends_on:
      local-db:
        condition: service_healthy
    ports:
      - "${SAFARSUITE_LOCAL_SERVER_HTTP_PORT:-8080}:8080"
    volumes:
      - safarsuite-local-data:/var/lib/safarsuite/local-server
      - ./certs/local-api:/etc/safarsuite/local-server/certs/local-api:ro
      - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro

  local-worker:
    image: ${SAFARSUITE_LOCAL_SERVER_IMAGE:?Set SAFARSUITE_LOCAL_SERVER_IMAGE}
    restart: unless-stopped
    command: ["safarsuite-local-worker"]
    env_file:
      - ./local-server.env
    depends_on:
      local-db:
        condition: service_healthy
    volumes:
      - safarsuite-local-data:/var/lib/safarsuite/local-server
      - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro

  local-agent:
    image: ${SAFARSUITE_LOCAL_SERVER_IMAGE:?Set SAFARSUITE_LOCAL_SERVER_IMAGE}
    restart: unless-stopped
    command: ["safarsuite-local-agent"]
    env_file:
      - ./local-server.env
    depends_on:
      local-db:
        condition: service_healthy
    volumes:
      - safarsuite-local-data:/var/lib/safarsuite/local-server
      - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro

  safarsuite-app:
    image: ${SAFARSUITE_APP_IMAGE:?Set SAFARSUITE_APP_IMAGE}
    restart: unless-stopped
    profiles:
      - app-runtime
    env_file:
      - ./local-server.env
    depends_on:
      local-db:
        condition: service_healthy
      local-api:
        condition: service_started
    ports:
      - "${SAFARSUITE_APP_HTTP_PORT:-5280}:5280"
    environment:
      ASPNETCORE_URLS: http://0.0.0.0:5280
      SAFARSUITE_LOCAL_API_BASE_URL: ${SAFARSUITE_LOCAL_API_BASE_URL:-https://local-api:8080}
      SAFARSUITE_MODULE_GATEWAY_URL: ${SAFARSUITE_MODULE_GATEWAY_URL:-https://local-api:8080}
      SAFARSUITE_RUNTIME_MANIFEST_PATH: ${SAFARSUITE_RUNTIME_MANIFEST_PATH:-/etc/safarsuite/local-server/runtime-services.manifest.json}
    volumes:
      - safarsuite-local-data:/var/lib/safarsuite/local-server
      - ./certs/trust:/etc/safarsuite/local-server/certs/trust:ro
      - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro
      - safarsuite-app-data:/var/lib/safarsuite/app

volumes:
  safarsuite-local-db:
  safarsuite-local-data:
  safarsuite-app-data:
EOF

chmod 600 "$config_file"
chmod 600 "$env_file"
chmod 600 "$compose_file"
chmod 600 "$runtime_manifest_file"

should_import_bundle_after_start="false"

if [ "$start_compose" = "true" ] && [ "$import_bundle_after_start" = "true" ] && [ -n "$bundle_file" ]; then
  should_import_bundle_after_start="true"
fi

if [ "$register_now" = "true" ] && [ "$should_import_bundle_after_start" != "true" ]; then
  if ! command -v curl >/dev/null 2>&1; then
    echo "curl is required for online registration." >&2
    exit 5
  fi

  request_body="$(printf '{"clientId":"%s","setupToken":"%s","localServerVersion":"%s","deploymentProfile":{"bootstrapMode":"%s","clientDeploymentMode":"%s","siteId":"%s","siteRole":"%s","parentSiteId":"%s","branchCode":"%s","syncTopologyId":"%s"}}' \
    "$(json_escape "$client_id")" \
    "$(json_escape "$setup_token")" \
    "$(json_escape "$local_server_version")" \
    "$(json_escape "$bootstrap_mode")" \
    "$(json_escape "$client_deployment_mode")" \
    "$(json_escape "$site_id")" \
    "$(json_escape "$site_role")" \
    "$(json_escape "$parent_site_id")" \
    "$(json_escape "$branch_code")" \
    "$(json_escape "$sync_topology_id")")"

  curl -fsS \
    -X POST "$registration_url" \
    -H "Content-Type: application/json" \
    --data "$request_body" \
    -o "$state_dir/registration-response.json"
fi

if [ "$start_compose" = "true" ]; then
  if ! command -v docker >/dev/null 2>&1; then
    echo "docker is required when SAFARSUITE_START_COMPOSE=true." >&2
    exit 6
  fi

  docker compose --env-file "$env_file" -f "$compose_file" up -d
fi

if [ "$should_import_bundle_after_start" = "true" ]; then
  if ! command -v curl >/dev/null 2>&1; then
    echo "curl is required to import the signed bootstrap bundle." >&2
    exit 5
  fi

  wait_for_local_api "$local_api_public_url"

  if local_api_has_current_bootstrap "$local_api_public_url"; then
    echo "SafarSuite local-server bootstrap already exists for $installation_id; skipping bundle import."
  else
    curl -fsS \
      "${local_api_curl_args[@]}" \
      -X POST "$local_api_public_url/api/v1/local-server/bootstrap-package/import" \
      -H "Content-Type: application/json" \
      --data-binary "@$bundle_file" \
      -o "$state_dir/bootstrap-import-response.json"
  fi
fi

echo "SafarSuite local-server bootstrap config written to $config_file"
echo "SafarSuite local-server compose template written to $compose_file"
echo "SafarSuite local-server runtime manifest written to $runtime_manifest_file"
