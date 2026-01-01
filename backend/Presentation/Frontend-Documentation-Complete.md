# Server Monitoring System - Complete Frontend Integration Guide

**Version:** 2.1  
**Last Updated:** December 2025  
**Backend:** .NET 8 / ASP.NET Core  
**Frontend:** React.js  
**Protocol:** REST API + SignalR WebSockets

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Authentication System](#authentication-system)
3. [Agent Management (NEW)](#agent-management-new)
4. [SignalR Real-Time Communication](#signalr-real-time-communication)
5. [Server Monitoring Endpoints](#server-monitoring-endpoints)
6. [Service & Process Management](#service--process-management)

---

## Quick Start

### Installation

```bash
npm install @microsoft/signalr axios
```

### Environment Variables

```env
REACT_APP_API_BASE_URL=https://localhost:7287
REACT_APP_SIGNALR_HUB=https://localhost:7287/monitoring-hub
```

### Basic Flow

```
1. Login → Get JWT Token
2. Register Agent → Get Install Command (ONE TIME)
3. Connect to SignalR Hub
4. Subscribe to Server(s)
5. Receive Real-Time Metrics
```

---

## Authentication System

### Login

**`POST /api/auth/login`**

**Request:**
```typescript
interface LoginRequest {
  email: string;
  password: string;
}
```

**Response (200 OK):**
```typescript
interface LoginResponse {
  token: string;           // JWT token (4-hour expiration)
  expiresAt: string;      // ISO 8601
  user: {
    id: number;
    email: string;
    fullName: string;
    role: string;
    isActive: boolean;
  };
}
```

**Example:**
```javascript
const response = await fetch('https://localhost:7287/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'admin@example.com', password: 'admin123' })
});
const data = await response.json();
localStorage.setItem('authToken', data.token);
```

---

## Agent Management (NEW)

### 🆕 Register New Agent

**`POST /api/agents/register`**

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Request:**
```typescript
interface RegisterAgentRequest {
  name: string;  // Server name (e.g., "Production Server")
}
```

**Response (200 OK):**
```typescript
interface RegisterAgentResponse {
  id: number;                 // Agent ID
  name: string;               // Server name
  token: string;              // ⚠️ PLAIN TOKEN - SHOWN ONLY ONCE
  installCommand: string;     // curl command with token embedded
  createdAtUtc: string;       // ISO 8601
}
```

**Example Response:**
```json
{
  "id": 5,
  "name": "Production Server",
  "token": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "installCommand": "curl -sL http://localhost:7286/api/agents/install/5?token=a1b2c3d4-e5f6-7890-abcd-ef1234567890 | sudo bash",
  "createdAtUtc": "2025-12-28T12:00:00Z"
}
```

**⚠️ Security Note:**
- Token is **hashed (BCrypt)** in database
- Plain token returned **only once** on registration
- Frontend must display warning: "Save this command - token won't be shown again"

**Frontend Implementation:**
```tsx
async function registerAgent(name: string) {
  const token = localStorage.getItem('authToken');
  
  const response = await fetch('https://localhost:7287/api/agents/register', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ name })
  });

  if (!response.ok) throw new Error('Registration failed');

  const data = await response.json();
  
  // Display modal with install command
  showInstallModal({
    command: data.installCommand,
    token: data.token,
    warning: '⚠️ Copy this command now. Token will not be shown again.'
  });

  return data;
}
```

---

### Get Installation Script (Backend Only)

**`GET /api/agents/install/{agentId}?token={plainToken}`**

**⚠️ Not for Frontend Use** - This endpoint is called by the curl command on the Linux server.

**Auth:** Anonymous (validates token in query string)

**Returns:** Bash script with injected credentials

---

### List All Agents

**`GET /api/agents`**

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```typescript
interface AgentListItem {
  id: number;
  name: string;
  hostname: string;       // Updated when agent connects
  isOnline: boolean;
  lastSeenUtc: string;    // ISO 8601
  createdAtUtc: string;   // ISO 8601
}

// Returns: AgentListItem[]
```

**Example:**
```javascript
const response = await fetch('https://localhost:7287/api/agents', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const agents = await response.json();
```

---

### Get Agent Status

**`GET /api/agents/{agentId}/status`**

**Response (200 OK):**
```typescript
interface AgentStatus {
  id: number;
  name: string;
  isOnline: boolean;
  lastSeenUtc: string;  // ISO 8601
}
```

---

### Delete Agent

**`DELETE /api/agents/{agentId}`**

**Response (200 OK):**
```json
{ "message": "Agent deleted successfully" }
```

**⚠️ Note:** This removes agent from database but doesn't uninstall from Linux server. User must manually uninstall.

---

## SignalR Real-Time Communication

### Connect to Hub

```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:7287/monitoring-hub', {
    accessTokenFactory: () => localStorage.getItem('authToken'),
    transport: signalR.HttpTransportType.WebSockets,
    skipNegotiation: true
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .configureLogging(signalR.LogLevel.Information)
  .build();

await connection.start();
const connectionId = connection.connectionId;
```

---

### Subscribe to Server Metrics

**`POST /api/monitoring/subscribe/{serverId}`**

**Headers:**
```
Authorization: Bearer {jwt_token}
X-SignalR-ConnectionId: {connection_id}
```

**Response (200 OK):**
```typescript
interface SubscribeResponse {
  message: string;
  serverId: number;
  connectionId: string;
  recentMetrics: MetricDto[];  // Last 50 metrics for immediate display
}

interface MetricDto {
  id: number;
  monitoredServerId: number;
  timestampUtc: string;
  cpuUsagePercent: number;
  ramUsagePercent: number;
  ramUsedGb: number;
  uptimeSeconds: number;
  load1m: number;
  load5m: number;
  load15m: number;
  diskReadSpeedMBps: number;
  diskWriteSpeedMBps: number;
  diskPartitions: DiskPartitionDto[];
  networkInterfaces: NetworkInterfaceDto[];
}
```

**SignalR Events:**
```typescript
// Real-time metrics (every 5 seconds)
connection.on('MetricsUpdated', (data: MetricDto) => {
  console.log('New metrics:', data);
});

// Watchlist metrics (services/processes)
connection.on('WatchlistMetricsUpdated', (data: WatchlistMetrics) => {
  console.log('Watchlist update:', data);
});

// Command results
connection.on('CommandSuccess', (event) => {
  console.log('Command succeeded:', event.message);
});

connection.on('CommandFailed', (event) => {
  console.error('Command failed:', event.message);
});
```

---

## Server Monitoring Endpoints

### Get All Servers

**`GET /api/server`**

**Response:**
```typescript
interface ServerListItem {
  id: number;
  name: string;
  hostname: string;
  ipAddress: string;
  os: string;
  isOnline: boolean;
  lastSeenUtc: string;
}
```

---

### Get Server Info

**`GET /api/server/{serverId}/info`**

**Response:**
```typescript
interface ServerInfo {
  status: string;
  data: {
    hostname: string;
    ipAddress: string;
    os: string;
    osVersion: string;
    kernel: string;
    architecture: string;
    cpuModel: string;
    cpuCores: number;
    cpuThreads: number;
  };
}
```

---

## Service & Process Management

### List Services

**`GET /api/servers/{serverId}/services`**

**Response:**
```typescript
interface Service {
  name: string;
  activeState: string;  // "active", "inactive", "failed"
  subState: string;
  cpuUsagePercent?: number;
  memoryUsage?: number;
  mainPID?: number;
}
```

---

### Restart Service

**`POST /api/servers/{serverId}/services/{serviceName}/restart`**

**Response:**
```json
{ "status": "ok", "message": "Restart command sent" }
```

**⚠️ Note:** Success response only means command was sent. Actual result arrives via SignalR `CommandSuccess` or `CommandFailed` event.

---

### List Processes

**`GET /api/servers/{serverId}/processes`**

**Response:**
```typescript
interface Process {
  pid: number;
  name: string;
  user: string;
  cpuPercent: number;
  memoryPercent: number;
  status: string;
}
```

---

## Complete React Example

```tsx
import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

function AgentManagement() {
  const [agents, setAgents] = useState([]);
  const [newAgentName, setNewAgentName] = useState('');
  const [installCommand, setInstallCommand] = useState(null);

  useEffect(() => {
    loadAgents();
  }, []);

  async function loadAgents() {
    const token = localStorage.getItem('authToken');
    const response = await fetch('https://localhost:7287/api/agents', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setAgents(data);
  }

  async function registerAgent() {
    const token = localStorage.getItem('authToken');
    const response = await fetch('https://localhost:7287/api/agents/register', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ name: newAgentName })
    });

    const data = await response.json();
    
    // Show install command modal
    setInstallCommand(data);
    setNewAgentName('');
    loadAgents();
  }

  return (
    <div>
      <h2>Register New Agent</h2>
      <input
        value={newAgentName}
        onChange={(e) => setNewAgentName(e.target.value)}
        placeholder="Server name"
      />
      <button onClick={registerAgent}>Register</button>

      {installCommand && (
        <div className="modal">
          <h3>⚠️ Installation Command (One Time Only)</h3>
          <p>Copy this command and run on your Linux server:</p>
          <pre>{installCommand.installCommand}</pre>
          <p><strong>Token:</strong> {installCommand.token}</p>
          <p style={{color: 'red'}}>
            This token will not be shown again!
          </p>
          <button onClick={() => setInstallCommand(null)}>Close</button>
        </div>
      )}

      <h2>Registered Agents</h2>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Hostname</th>
            <th>Status</th>
            <th>Last Seen</th>
          </tr>
        </thead>
        <tbody>
          {agents.map(agent => (
            <tr key={agent.id}>
              <td>{agent.name}</td>
              <td>{agent.hostname}</td>
              <td>{agent.isOnline ? '🟢 Online' : '🔴 Offline'}</td>
              <td>{new Date(agent.lastSeenUtc).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

---

## Key Changes Summary

### 🆕 New in v2.1:

1. **Agent Registration Endpoint**
   - `POST /api/agents/register` - Register agent, get install command
   - Token is hashed in database (BCrypt)
   - Plain token shown only once

2. **Install Script Endpoint**
   - `GET /api/agents/install/{id}?token={token}` - Dynamic bash script
   - Anonymous access (validates token)
   - Auto-detects domain/port from request

3. **Agent CRUD Endpoints**
   - `GET /api/agents` - List all agents
   - `GET /api/agents/{id}/status` - Get agent status
   - `DELETE /api/agents/{id}` - Delete agent

4. **Subscribe Response Enhanced**
   - Now includes `recentMetrics` array (last 50 metrics)
   - Frontend can display data immediately

### Security Notes:

- Agent tokens are **hashed** in database using BCrypt
- Install script URL contains plain token as query parameter
- Token is validated before returning script
- Frontend must warn users to save install command

---

**End of Documentation**
