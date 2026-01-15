#!/bin/bash
set -e

# --- CONFIGURATION (Injected by Backend) ---
AGENT_ID="{{AGENT_ID}}"
TOKEN="{{TOKEN}}"
DOMAIN="{{DOMAIN}}"

# --- DYNAMIC PROTOCOL SELECTION ---
# Detect if this is a local/LAN environment (no SSL) or public domain (SSL)
# DOMAIN already contains the port if one was present (e.g., localhost:5123, 192.168.1.100:5123)

# Extract just the host part (without port) for pattern matching
HOST_ONLY="${DOMAIN%%:*}"

# Check for localhost, loopback, or private/LAN IP addresses
if [[ "$HOST_ONLY" == "localhost" || \
      "$HOST_ONLY" == "127.0.0.1" || \
      "$HOST_ONLY" =~ ^10\. || \
      "$HOST_ONLY" =~ ^172\.(1[6-9]|2[0-9]|3[0-1])\. || \
      "$HOST_ONLY" =~ ^192\.168\. ]]; then
    echo "🔧 Detected Local/LAN Environment."
    DEB_URL="http://${DOMAIN}/downloads/super-agent_amd64.deb"
    WS_URI="ws://${DOMAIN}"
else
    echo "☁️ Detected Public Environment."
    # Public domain - use HTTPS/WSS (nginx/apache handles SSL termination)
    DEB_URL="https://${DOMAIN}/downloads/super-agent_amd64.deb"
    WS_URI="wss://${DOMAIN}"
fi

echo "🚀 Starting Super-Agent Installation..."
echo "🌍 Target Backend: ${DOMAIN}"
echo "🔗 Source: ${DEB_URL}"
echo "🔌 WebSocket URI: ${WS_URI}"

# --- 1. PREPARE DIRECTORIES ---
echo "📂 Preparing directories..."
mkdir -p /etc/super-agent
mkdir -p /var/log/super-agent

# --- 2. WRITE SECRETS ---
echo "📝 Writing configuration..."

cat > /etc/super-agent/agent.env <<EOF
AGENT_ID=${AGENT_ID}
TOKEN=${TOKEN}
WS_URI=${WS_URI}
EOF
chmod 600 /etc/super-agent/agent.env

# --- 3. INSTALL PACKAGE ---
echo "📦 Downloading package..."

if wget --show-progress -O /tmp/agent.deb "$DEB_URL"; then
    echo "✅ Download successful."
else
    echo "❌ Error: Failed to download package from $DEB_URL"
    exit 1
fi

echo "📦 Installing package..."
if dpkg -i /tmp/agent.deb; then
    rm /tmp/agent.deb
    echo "✅ Installation Complete! Agent is running."
    echo "📋 Configuration saved to /etc/super-agent/agent.env"
    echo "🔌 WebSocket URI: ${WS_URI}"
else
    echo "❌ Error: Installation failed."
    exit 1
fi