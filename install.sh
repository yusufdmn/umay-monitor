#!/usr/bin/env bash
set -euo pipefail

echo "=========================================="
echo "      UMAY MONITOR - Setup Wizard"
echo "=========================================="
echo ""

# --- 1. Domain Configuration ---
echo "How will you access this server?"
echo "1) Localhost (Testing on this machine)"
echo "2) Domain Name (e.g. example.com)"
read -p "Select [1/2]: " MODE

if [[ "$MODE" == "1" ]]; then
    FULL_API_URL="http://localhost:5123"
    DASHBOARD_URL="http://localhost:3000"
    echo "-> Mode: Localhost"
else
    echo ""
    read -p "Enter the Domain for the DASHBOARD (e.g. monitor.example.com): " USER_DASH_INPUT
    
    # Clean input
    CLEAN_DASH="${USER_DASH_INPUT#http://}"
    CLEAN_DASH="${CLEAN_DASH#https://}"
    CLEAN_DASH="${CLEAN_DASH%/}"

    DEFAULT_API="api.${CLEAN_DASH}"
    echo "The standard API domain is: ${DEFAULT_API}"
    read -p "Press ENTER to use this, or type a custom API domain: " CUSTOM_API_INPUT

    if [[ -z "$CUSTOM_API_INPUT" ]]; then
        FINAL_API_DOMAIN="${DEFAULT_API}"
    else
        FINAL_API_DOMAIN="${CUSTOM_API_INPUT#http://}"
        FINAL_API_DOMAIN="${FINAL_API_DOMAIN#https://}"
        FINAL_API_DOMAIN="${FINAL_API_DOMAIN%/}"
    fi

    FULL_API_URL="https://${FINAL_API_DOMAIN}"
    DASHBOARD_URL="https://${CLEAN_DASH}"
    echo "-> Mode: Domain"
fi

# --- 2. Secret Generation (The New Part) ---
echo ""
echo "-> Generating secure keys..."

# Generate random 64-char string for JWT
JWT_SECRET=$(openssl rand -base64 48 | tr -d '\n\r')

# Generate random 32-char string for Backup Encryption
ENCRYPTION_KEY=$(openssl rand -base64 32 | tr -d '\n\r')

echo "   [OK] JWT Secret generated"
echo "   [OK] Encryption Master Key generated"

echo ""
echo "-> Saving configuration to .env file..."

cat <<EOF > .env
# Network Config
UMAY_API_URL=${FULL_API_URL}
UMAY_DASHBOARD_URL=${DASHBOARD_URL}

# Security Secrets (Auto-Generated)
JWT_SECRET=${JWT_SECRET}
BACKUP_MASTER_KEY=${ENCRYPTION_KEY}

# Database Credentials
POSTGRES_USER=postgres
POSTGRES_PASSWORD=umay_secure_pass
POSTGRES_DB=umay_db
EOF

echo "-> Building containers..."
docker compose up -d --build

echo ""
echo "✅ Umay Monitor Installed Successfully!"
if [[ "$MODE" != "1" ]]; then
    echo "⚠️  Configure Nginx Proxy Manager:"
    echo "   Dashboard -> http://<YOUR_IP>:3000"
    echo "   API       -> http://<YOUR_IP>:5123"
fi
echo "------------------------------------------"