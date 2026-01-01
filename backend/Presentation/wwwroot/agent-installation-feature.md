📋 Technical Workflow: Adding a New Agent
To: Backend & Frontend Teams Subject: "Add Agent" Flow & Implementation Details
This document outlines the step-by-step process for provisioning a new agent.
1. User Action (Frontend)
•	UI: The user clicks "Add New Agent".
•	Input: The user enters a unique Agent ID (e.g., paris-01).
2. API Request (Frontend → Backend)
•	The Frontend sends a POST request to the Backend.
•	Payload:
JSON
{
  "agent_id": "paris-01"
}
3. Token Generation & Script Preparation (Backend)
Upon receiving the request, the Backend performs the following:
1.	Generate Token: Creates a secure, unique token for this specific agent.
2.	Database: Saves the agent_id and token in the database (status: Pending).
3.	Template Injection: Reads the installer.sh template and injects the following variables:
•	{{AGENT_ID}}: The ID from the request (paris-01).
•	{{TOKEN}}: The newly generated token.
•	{{DOMAIN}}: The current running domain (Logic: If Backend is running locally, use localhost; if Production, use public domain).
4. Hosting the Binary (Backend)
•	The Backend must host the specific .deb package file (e.g., super-agent_amd64.deb) at a public static endpoint.
•	Example URL: http://{domain}/downloads/super-agent_amd64.deb
•	Note: The installer script generated in Step 3 will automatically point to this URL.
5. Final Response (Backend → Frontend)
•	The Backend constructs the final installer URL or Command.
•	It sends this back to the Frontend.
•	Response:
JSON
{
  "install_command": "curl -sL http://{domain}/api/install/paris-01 | sudo bash"
}
•	Frontend Action: Displays this command in a code block for the user to copy and run on their server.

