#!/bin/bash
set -e

# --- CONFIGURATION (Injected by Backend) ---
AGENT_ID="{{AGENT_ID}}"
TOKEN="{{TOKEN}}"
DOMAIN="{{DOMAIN}}"

# --- DYNAMIC PROTOCOL SELECTION ---
if [[ "$DOMAIN" == "localhost" || "$DOMAIN" == "127.0.0.1" ]]; then
    echo "🔧 Detected Localhost Environment."
    DEB_URL="http://${DOMAIN}/downloads/super-agent_amd64.deb"
    WS_URI="ws://${DOMAIN}:7287"
else
    echo "☁️ Detected Public Environment."
    DEB_URL="https://${DOMAIN}/downloads/super-agent_amd64.deb"
    WS_URI="wss://${DOMAIN}"
fi

echo "🚀 Starting Super-Agent Installation..."
echo "🌍 Target Backend: ${DOMAIN}"
echo "🔗 Source: ${DEB_URL}"

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

# CHANGED: Removed -q, added error checking
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
else
    echo "❌ Error: Installation failed."
    exit 1
fi