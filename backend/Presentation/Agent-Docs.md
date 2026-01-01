Current Server agent info and how should be the backend:
1. Communication Protocol
• Transport: WebSocket Secure (wss://).
• Security: TLS v1.2+, ECDHE+AESGCM cipher suite.
• Authentication: Occurs immediately after connection.
• Auth Request: { "type": "request", "action": "authenticate", "payload":
{ "agent_id": "...", "token": "..." } }.
• Success Response: { "status": "ok" }.
2. Message Envelope
All messages (Requests, Responses, Events) share this structure:
JSON
{
 "type": "request" | "response" | "event",
 "id": 12345, // Integer. Crucial for matching Response to Request.
 "action": "action_name", // String.
 "payload": { ... }, // Object. Varies by action.
 "timestamp": 1733936600000 // Integer (Unix ms).
}
3. Event Schemas (Agent $\rightarrow$ Server)
Action: metrics
Trigger: Periodic (Default 5s).
Payload Structure:
JSON
{
 "cpuUsagePercent": 12.5, // Float
 "ramUsagePercent": 45.2, // Float
 "ramUsedGB": 4.1, // Float
 "uptimeSeconds": 3600, // Integer
 "normalizedLoad": { // Object
 "1m": 0.5, "5m": 0.4, "15m": 0.2
 },
 "diskReadSpeedMBps": 1.2, // Float
 "diskWriteSpeedMBps": 0.5, // Float
 "diskUsage": [ // Array of Objects
 {
 "device": "/dev/sda1",
 "mountpoint": "/",
 "fstype": "ext4",
 "totalGB": 450.5,
 "usedGB": 20.1,
 "usagePercent": 4.5
 }
 ],
 "networkInterfaces": [ // Array of Objects
 {
 "name": "eth0",
 "mac": "00:11:22:33:44:55",
 "ipv4": "192.168.1.10",
 "ipv6": "fe80::...",
 "uploadSpeedMbps": 5.2,
 "downloadSpeedMbps": 12.4
 }
 ]
}
4. Request/Response Reference (Server $\rightarrow$ Agent)
The backend sends request, Agent replies with response containing the same id.
System Information
• Action: get-server-info
• Request Payload: null
• Response Payload:
JSON
{
 "status": "ok",
 "data": {
 "hostname": "server-01",
 "ipAddress": "192.168.1.5",
 "os": "Linux",
 "osVersion": "#75-Ubuntu...",
 "kernel": "5.15.0",
 "architecture": "x86_64",
 "cpuModel": "Intel...",
 "cpuCores": 4, // Physical
 "cpuThreads": 8 // Logical
 }
}
Service Management
• Action: get-services
• Response: List of { "name": "nginx", "activeState": "active", "subState":
"running" }.
• Action: get-service
• Request Payload: { "name": "service_name" }.
• Response Data: Includes mainPID, cpuUsagePercent, memoryUsage (MB),
startTime, exitTime, restart policy.
• Action: get-service-log
• Request Payload: { "name": "service_name" }.
• Response Data: List of { "log": "timestamp message..." } (Max 1000 lines).
• Action: restart-service
• Request Payload: { "name": "service_name" }.
• Response: { "status": "ok" } or { "status": "error", "message": "..." }.
Process Management
• Action: get-processes
• Response: List of { "pid": 123, "name": "python", "user": "root", "status":
"running", "cpuPercent": 0.5, "memoryPercent": 1.2 }.
• Action: get-process
• Request Payload: { "pid": 1234 } (String or Int, Agent casts to Int).
• Response Data: Detailed stats including cmdline, nice, numThreads,
uptimeSeconds.
Configuration
• Action: update-agent-config
• Request Payload:
JSON
{
 "metricsInterval": 10, // Integer (Seconds)
 "watchlist": ["nginx", "db"] // List of Strings
}
• Response: { "status": "ok" }.
5. Important Implementation Notes
1. Request Caching (Critical):
The Agent caches responses based on the Message ID (id).
• If the Backend sends a request with an id that was already processed, the
Agent will not execute the command again but will return the cached
response immediately.
• Requirement: The Backend must increment or randomize the id for every
new command.
2. Data Types:
• pid in requests can be string or int (Agent casts it: int(...)).
• metricsInterval in config update must be an integer between 0 and 3600.