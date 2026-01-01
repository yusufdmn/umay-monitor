#!/usr/bin/env bash
set -euo pipefail

# -------- configurable defaults --------
API_URL_HTTP="${API_URL_HTTP:-http://localhost:5123/swagger/index.html}"
API_URL_HTTPS="${API_URL_HTTPS:-https://localhost:7287/swagger/index.html}"

DB_NAME="${DB_NAME:-serverhealth}"
DB_USER="${DB_USER:-yusuf}"
DB_PASS="${DB_PASS:-yusuf}"

EF_VERSION="${EF_VERSION:-9.0.1}"

# Project directories (repo-relative)
STARTUP_PROJECT_DIR="${STARTUP_PROJECT_DIR:-Presentation}"
MIGRATIONS_PROJECT_DIR="${MIGRATIONS_PROJECT_DIR:-Infrastructure}"

# Certs expected in repo
CERT_DIR_REPO="${CERT_DIR_REPO:-Presentation/Security}"
CERT_PEM_REPO="${CERT_PEM_REPO:-${CERT_DIR_REPO}/cert.pem}"
KEY_PEM_REPO="${KEY_PEM_REPO:-${CERT_DIR_REPO}/key.pem}"

# Where certs appear inside the api container (must match docker-compose.yml)
CERT_DIR_IN_CONTAINER="${CERT_DIR_IN_CONTAINER:-/app/Security}"
CERT_PEM_IN_CONTAINER="${CERT_PEM_IN_CONTAINER:-${CERT_DIR_IN_CONTAINER}/cert.pem}"
KEY_PEM_IN_CONTAINER="${KEY_PEM_IN_CONTAINER:-${CERT_DIR_IN_CONTAINER}/key.pem}"

# -------- helpers --------
die() { echo "ERROR: $*" >&2; exit 1; }

need_cmd() { command -v "$1" >/dev/null 2>&1; }

# -------- checks --------
echo "==> Working dir: $(pwd)"

[[ -f docker-compose.yml ]] || die "docker-compose.yml not found in current directory. Run from repo root."
[[ -d "${STARTUP_PROJECT_DIR}" ]] || die "Startup project dir not found: ./${STARTUP_PROJECT_DIR}"
[[ -d "${MIGRATIONS_PROJECT_DIR}" ]] || die "Migrations project dir not found: ./${MIGRATIONS_PROJECT_DIR}"

# If you want WSS, cert files must exist
[[ -f "${CERT_PEM_REPO}" ]] || die "Missing cert: ${CERT_PEM_REPO} (expected for HTTPS/WSS)"
[[ -f "${KEY_PEM_REPO}" ]] || die "Missing key:  ${KEY_PEM_REPO} (expected for HTTPS/WSS)"

# Docker availability
need_cmd docker || die "docker not found. Install docker + compose plugin first."

# -------- start services --------
echo "==> Starting services (build if needed)..."
docker compose up -d --build

echo "==> Waiting for db container..."
DB_CID=""
for i in $(seq 1 30); do
  DB_CID="$(docker compose ps -q db 2>/dev/null || true)"
  [[ -n "$DB_CID" ]] && break
  sleep 1
done
[[ -n "$DB_CID" ]] || die "Could not find db container. Ensure compose service is named 'db'."

echo "==> Waiting for db to be ready..."
for i in $(seq 1 90); do
  status="$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}no-healthcheck{{end}}' "$DB_CID" 2>/dev/null || true)"
  if [[ "$status" == "healthy" || "$status" == "no-healthcheck" ]]; then
    break
  fi
  sleep 1
done

# Determine the compose network from db container
COMPOSE_NETWORK="$(docker inspect -f '{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}' "$DB_CID")"
[[ -n "$COMPOSE_NETWORK" ]] || die "Could not determine compose network."

echo "==> Using docker network: $COMPOSE_NETWORK"

# -------- sanity check cert mount inside api container --------
echo "==> Checking certs inside api container..."
API_CID="$(docker compose ps -q api 2>/dev/null || true)"
[[ -n "$API_CID" ]] || die "Could not find api container. Ensure compose service is named 'api'."

# This will fail if you forgot the volume mount in docker-compose.yml
docker exec "$API_CID" test -f "${CERT_PEM_IN_CONTAINER}" || die "Cert not found inside container at ${CERT_PEM_IN_CONTAINER}. Check docker-compose.yml volume mount."
docker exec "$API_CID" test -f "${KEY_PEM_IN_CONTAINER}"  || die "Key not found inside container at ${KEY_PEM_IN_CONTAINER}. Check docker-compose.yml volume mount."

# -------- run EF migrations (no host dotnet needed) --------
echo "==> Running EF migrations..."
docker run --rm --network "${COMPOSE_NETWORK}" \
  -v "$(pwd):/src" -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -lc "
    set -e
    dotnet nuget locals all --clear
    dotnet tool install --tool-path /tmp dotnet-ef --version ${EF_VERSION}
    /tmp/dotnet-ef database update \
      --project ${MIGRATIONS_PROJECT_DIR} \
      --startup-project ${STARTUP_PROJECT_DIR} \
      --connection \"Server=db;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS};\"
  "

# -------- restart api --------
echo "==> Restarting api..."
docker compose restart api

echo
echo "✅ Done."
echo "HTTP Swagger:  ${API_URL_HTTP}"
echo "HTTPS Swagger: ${API_URL_HTTPS}"
echo
echo "Logs:"
echo "  docker compose logs -f api"
