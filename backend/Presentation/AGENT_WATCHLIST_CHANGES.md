# Backend Changes for Agent Watchlist Update

## Summary
The backend has been updated to support the agent's new features:
1. **Dynamic configuration updates** (metrics interval and watchlist)
2. **Watchlist metrics events** (periodic monitoring of specific services and processes)

---

## ? Changes Implemented

### 1. **Updated DTOs** (`BusinessLayer/DTOs/Agent/`)

#### **Configuration DTOs** (`Configuration/AgentConfigurationDtos.cs`)
- ? Updated `UpdateAgentConfigRequest` to support:
  - Optional `metricsInterval` (int, 0-3600 seconds)
  - Optional `watchlist` object containing:
    - `services` (list of service names)
    - `processes` (list of process command lines)
- ? Created `WatchlistConfig` class

#### **Watchlist Metrics DTOs** (NEW: `Watchlist/WatchlistMetricsDtos.cs`)
- ? Created `WatchlistMetricsPayload` for the new event
- ? Created `WatchlistServiceInfo` with service details
- ? Created `WatchlistProcessInfo` with process details (including error field)

#### **Base Message Updates** (`BaseAgentMessage.cs`)
- ? Added `AgentActions.WatchlistMetrics = "watchlist-metrics"` constant

---

### 2. **Updated Message Handler** (`BusinessLayer/Services/Concrete/AgentMessageHandler.cs`)

#### **Event Handling**
- ? Added case for `AgentActions.WatchlistMetrics` in `HandleEvent()`
- ? Created `ProcessWatchlistMetrics()` method to:
  - Log received watchlist metrics
  - Broadcast to SignalR clients via `WatchlistMetricsUpdated` event
  - Optionally store in database (TODO marker added)

---

### 3. **Updated Configuration Controller** (`Presentation/Controllers/ConfigurationController.cs`)

#### **Endpoint: `PUT /api/servers/{serverId}/configuration`**
- ? Updated to support new request structure
- ? Validates `metricsInterval` range (0-3600)
- ? Handles both optional fields (interval and watchlist)
- ? Improved logging to show what's being updated

**Request Body Example:**
```json
{
  "metricsInterval": 10,
  "watchlist": {
    "services": ["nginx", "docker"],
    "processes": ["python app.py", "node server.js"]
  }
}
```

**Response:**
```json
{
  "message": "Configuration updated successfully"
}
```

---

## ?? New SignalR Event

### **Event: `WatchlistMetricsUpdated`**

**Sent to:** Clients in group `server-{serverId}`

**Payload Structure:**
```json
{
  "serverId": 1,
  "timestampUtc": "2025-01-11T18:30:00Z",
  "services": [
    {
      "name": "nginx",
      "activeState": "active",
      "subState": "running",
      "cpuUsagePercent": 1.2,
      "memoryUsage": 55.4,
      "mainPID": 1234,
      "startTime": "2025-01-11T10:00:00",
      "restartPolicy": "on-failure"
    }
  ],
  "processes": [
    {
      "pid": 4521,
      "name": "python",
      "cmdline": "/usr/bin/python app.py --worker",
      "cpuPercent": 5.0,
      "memoryMb": 120.5,
      "user": "www-data",
      "status": "running"
    },
    {
      "pid": null,
      "error": "Process not found: my-worker.py"
    }
  ]
}
```

---

## ?? API Usage

### **Update Agent Configuration**

**Endpoint:** `PUT /api/servers/{serverId}/configuration`

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body (All fields optional):**
```json
{
  "metricsInterval": 10,
  "watchlist": {
    "services": ["nginx", "postgresql", "docker"],
    "processes": ["python app.py", "node server.js"]
  }
}
```

**Success Response (200 OK):**
```json
{
  "message": "Configuration updated successfully"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid metricsInterval (must be 0-3600)
- `503 Service Unavailable` - Agent not connected
- `504 Gateway Timeout` - Request timeout

---

## ?? Frontend Integration

### **1. Subscribe to Watchlist Metrics**

```javascript
connection.on("WatchlistMetricsUpdated", (data) => {
  console.log(`Watchlist update for server ${data.serverId}`);
  
  // Update UI with service metrics
  data.services.forEach(service => {
    updateServiceUI(service.name, service);
  });
  
  // Update UI with process metrics
  data.processes.forEach(process => {
    if (process.error) {
      showProcessError(process.error);
    } else {
      updateProcessUI(process.pid, process);
    }
  });
});
```

### **2. Update Configuration**

```javascript
async function updateAgentConfig(serverId, config) {
  const response = await fetch(`/api/servers/${serverId}/configuration`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${jwtToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      metricsInterval: 10,  // Optional
      watchlist: {          // Optional
        services: ["nginx", "docker"],
        processes: ["python app.py"]
      }
    })
  });
  
  const result = await response.json();
  console.log(result.message);
}
```

---

## ?? TODO Items

### **Database Storage (Optional)**
Currently, watchlist metrics are only broadcast via SignalR. If you want to store them:

1. Create new entities in `Infrastructure/Entities/`:
   - `WatchlistServiceMetric`
   - `WatchlistProcessMetric`

2. Update `ProcessWatchlistMetrics()` in `AgentMessageHandler.cs` to save to database

3. Create new API endpoints to query historical watchlist data

---

## ?? Testing

### **Test Configuration Update**

```bash
# Update metrics interval to 10 seconds
curl -X PUT https://localhost:7287/api/servers/1/configuration \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "metricsInterval": 10
  }'

# Update watchlist
curl -X PUT https://localhost:7287/api/servers/1/configuration \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "watchlist": {
      "services": ["nginx", "postgresql"],
      "processes": ["python app.py"]
    }
  }'

# Update both
curl -X PUT https://localhost:7287/api/servers/1/configuration \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "metricsInterval": 15,
    "watchlist": {
      "services": ["nginx"],
      "processes": ["node server.js"]
    }
  }'
```

### **Expected Flow:**
1. Backend sends `update-agent-config` request to agent
2. Agent updates ConfigManager and responds with `{"status": "ok"}`
3. Agent starts sending `watchlist-metrics` events periodically
4. Backend receives events and broadcasts via SignalR
5. Frontend receives `WatchlistMetricsUpdated` events

---

## ? Build Status

**Build:** ? Successful  
**Compilation Errors:** None

All changes are backward compatible and don't break existing functionality.

---

## ?? Files Modified/Created

### Created:
- `BusinessLayer/DTOs/Agent/Watchlist/WatchlistMetricsDtos.cs`
- `Presentation/AGENT_WATCHLIST_CHANGES.md` (this file)

### Modified:
- `BusinessLayer/DTOs/Agent/Configuration/AgentConfigurationDtos.cs`
- `BusinessLayer/DTOs/Agent/BaseAgentMessage.cs`
- `BusinessLayer/Services/Concrete/AgentMessageHandler.cs`
- `Presentation/Controllers/ConfigurationController.cs`

---

**Ready to test!** ??
