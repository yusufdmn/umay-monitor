Feature Request: Automated Agent Installation via One-Line Script
To: Backend Development Team From: Agent Development Team Date: 2025-12-26 Subject: Implementation of "One-Line" Installation Workflow for Linux Agents

1. Objective
To streamline the installation process for the end-user by replacing manual configuration (copy-pasting tokens, certificates, editing files) with a single terminal command.
Current (Manual): User downloads .deb -> Installs -> Edits config file -> Pastes Token -> Pastes Cert -> Restarts Service. (High friction, error-prone). Target (Automated): User copies one curl command from UI -> Pastes into terminal -> Done.

2. Backend Requirements
We need two new public-facing assets/endpoints:
A. The Static Artifact Host (The .deb file)
We need a publicly accessible URL where the raw .deb package is hosted.
•	Location: https://api.yourdomain.com/downloads/super-agent_latest_amd64.deb (or an S3 bucket URL).
•	Update Strategy: When the Agent team releases a new version, this file is replaced.
B. The Installation Script Endpoint (The Dynamic Script)
An endpoint that generates a custom bash script for a specific agent instance.
•	Endpoint: GET /api/agents/install/{agent_id}
•	Auth: Verified by session (user must be logged into UI to see this) OR a short-lived signed URL.
•	Response Content-Type: text/x-shellscript (or text/plain).
•	Logic:
1.	Verify the user has rights to agent_id.
2.	Retrieve the Agent Token and Server Certificate (PEM format) associated with this ID/Server.
3.	Load the Script Template (see Section 3).
4.	Replace placeholders ({{TOKEN}}, {{CERT}}, etc.) with actual data.
5.	Return the processed script string.

3. The Script Template (install_template.sh)
Store this template in the backend codebase.
Bash
#!/bin/bash
set -e

# --- DYNAMIC VARIABLES (INJECTED BY BACKEND) ---
AGENT_ID="{{AGENT_ID}}"
TOKEN="{{TOKEN}}"
WS_URI="{{WS_URI}}"      # e.g., "wss://api.yourdomain.com:7287"
DOWNLOAD_URL="{{DOWNLOAD_URL}}" # URL to the .deb file

# The Backend MUST ensure this string is properly escaped or use a Here-Doc exactly like below
CERT_CONTENT="{{CERT_CONTENT}}"

echo "🚀 Starting Super-Agent Installation..."

# 1. Clean previous installs (optional but recommended)
rm -f /tmp/agent.deb

# 2. Create Configuration Directory
echo "📂 Setting up configuration..."
mkdir -p /etc/super-agent

# 3. Write Credentials (agent.env)
cat > /etc/super-agent/agent.env <<EOF
AGENT_ID=$AGENT_ID
TOKEN=$TOKEN
WS_URI=$WS_URI
EOF
chmod 600 /etc/super-agent/agent.env

# 4. Write SSL Certificate (server.crt)
# This writes the multi-line certificate content provided by the backend
cat > /etc/super-agent/server.crt <<EOF
$CERT_CONTENT
EOF
chmod 644 /etc/super-agent/server.crt

# 5. Download the Agent Package
echo "📦 Downloading Agent..."
if command -v curl >/dev/null 2>&1; then
    curl -L -o /tmp/agent.deb "$DOWNLOAD_URL"
elif command -v wget >/dev/null 2>&1; then
    wget -q -O /tmp/agent.deb "$DOWNLOAD_URL"
else
    echo "❌ Error: Neither curl nor wget found."
    exit 1
fi

# 6. Install the Package
# The package post-install script will detect the files we just wrote
# and start the service automatically.
echo "🔧 Installing Service..."
dpkg -i /tmp/agent.deb
apt-get install -f -y  # Fix dependencies if any

# 7. Cleanup
rm /tmp/agent.deb

echo "✅ Installation Complete! Agent is running."

4. Frontend / UI Changes
When the user clicks "Add New Agent" and enters a name:
1.	Frontend: Calls POST /api/agents (creates agent, gets ID/Token).
2.	Frontend: Displays a modal with the command:
Install Command: Run this on your Linux server:
Bash
curl -sL https://api.yourdomain.com/api/agents/install/<NEW_AGENT_ID> | sudo bash

5. Implementation Checklist
•	[ ] Agent Team: Provide the final production .deb file.
•	[ ] Backend Team: Set up static hosting for the .deb file.
•	[ ] Backend Team: Implement the script injection logic (replace {{CERT_CONTENT}} carefully to preserve newlines).
•	[ ] Frontend Team: Update the "Add Agent" modal to display the curl | sudo bash command.
6. Security Note
•	Token Exposure: The install command contains the sensitive token inside the script body. This is standard industry practice (AWS, Datadog), as the script runs in memory. Ensure the endpoint https://api.yourdomain.com/api/agents/install/... is protected or uses a one-time-use token if higher security is required.
•	Certificate Formatting: When injecting {{CERT_CONTENT}}, ensure no extra whitespace is added, as this will invalidate the SSL handshake.

