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
config_dir="${SAFARSUITE_LOCAL_SERVER_CONFIG_DIR:-/etc/safarsuite/local-server}"
state_dir="${SAFARSUITE_LOCAL_SERVER_STATE_DIR:-/var/lib/safarsuite/local-server}"
bundle_file="${SAFARSUITE_BOOTSTRAP_BUNDLE_FILE:-}"
bundle_sha256="${SAFARSUITE_BOOTSTRAP_BUNDLE_SHA256:-}"
local_server_image="${SAFARSUITE_LOCAL_SERVER_IMAGE:-ghcr.io/safarsuite/local-server:$local_server_version}"
local_server_http_port="${SAFARSUITE_LOCAL_SERVER_HTTP_PORT:-8080}"
safarsuite_app_image="${SAFARSUITE_APP_IMAGE:-ghcr.io/safarsuite/app:$safarsuite_app_version}"
safarsuite_app_http_port="${SAFARSUITE_APP_HTTP_PORT:-8090}"
local_api_base_url="${SAFARSUITE_LOCAL_API_BASE_URL:-http://local-api:8080}"
module_gateway_url="${SAFARSUITE_MODULE_GATEWAY_URL:-http://local-api:8080}"
runtime_manifest_path="${SAFARSUITE_RUNTIME_MANIFEST_PATH:-$config_dir/runtime-services.manifest.json}"
local_db_name="${SAFARSUITE_LOCAL_DB_NAME:-safarsuite_local}"
local_db_user="${SAFARSUITE_LOCAL_DB_USER:-safarsuite}"
local_db_password="${SAFARSUITE_LOCAL_DB_PASSWORD:-change-me-before-start}"

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

mkdir -p "$config_dir" "$state_dir"
chmod 700 "$config_dir" "$state_dir"

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
SAFARSUITE_APP_VERSION=$safarsuite_app_version
SAFARSUITE_APP_IMAGE=$safarsuite_app_image
SAFARSUITE_APP_HTTP_PORT=$safarsuite_app_http_port
SAFARSUITE_LOCAL_API_BASE_URL=$local_api_base_url
SAFARSUITE_MODULE_GATEWAY_URL=$module_gateway_url
SAFARSUITE_RUNTIME_MANIFEST_PATH=$runtime_manifest_path
SAFARSUITE_LOCAL_DB_NAME=$local_db_name
SAFARSUITE_LOCAL_DB_USER=$local_db_user
SAFARSUITE_LOCAL_DB_PASSWORD=$local_db_password
SAFARSUITE_REGISTRATION_URL=$registration_url
SAFARSUITE_ENTITLEMENT_BUNDLE_URL=$entitlement_url
SAFARSUITE_HEARTBEAT_URL=$heartbeat_url
SAFARSUITE_PENDING_COMMANDS_URL=$pending_commands_url
SAFARSUITE_DIAGNOSTICS_URL=$diagnostics_url
EOF

cat > "$runtime_manifest_file" <<EOF
{
  "runtimeMode": "DockerCompose",
  "composeProjectName": "safarsuite-local-server",
  "configDirectory": "$(json_escape "$config_dir")",
  "stateDirectory": "$(json_escape "$state_dir")",
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
      "internalBaseUrl": "http://local-api:8080",
      "healthUrl": "http://local-api:8080/health",
      "dependsOn": ["local-db"]
    },
    {
      "serviceName": "local-worker",
      "serviceRole": "Background cloud pull, heartbeat, and command processing",
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
      "serviceRole": "Host/runtime diagnostics and support command bridge",
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
      "internalBaseUrl": "http://safarsuite-app:8080",
      "healthUrl": "http://safarsuite-app:8080/health",
      "dependsOn": ["local-api", "local-db"]
    }
  ]
}
EOF

cat > "$compose_file" <<'EOF'
name: safarsuite-local-server

services:
  local-db:
    image: postgres:16-alpine
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
      - "${SAFARSUITE_APP_HTTP_PORT:-8090}:8080"
    environment:
      SAFARSUITE_LOCAL_API_BASE_URL: ${SAFARSUITE_LOCAL_API_BASE_URL:-http://local-api:8080}
      SAFARSUITE_MODULE_GATEWAY_URL: ${SAFARSUITE_MODULE_GATEWAY_URL:-http://local-api:8080}
      SAFARSUITE_RUNTIME_MANIFEST_PATH: ${SAFARSUITE_RUNTIME_MANIFEST_PATH:-/etc/safarsuite/local-server/runtime-services.manifest.json}
    volumes:
      - safarsuite-local-data:/var/lib/safarsuite/local-server
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

if [ "$register_now" = "true" ]; then
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

echo "SafarSuite local-server bootstrap config written to $config_file"
echo "SafarSuite local-server compose template written to $compose_file"
echo "SafarSuite local-server runtime manifest written to $runtime_manifest_file"
