# ?? Watchlist Metrics Troubleshooting Guide

## ? Backend Implementation Status

### **1. Backend IS Sending Watchlist Metrics**

The backend correctly:
- ? Receives `watchlist-metrics` events from agents
- ? Processes them in `ProcessWatchlistMetrics()` method
- ? Broadcasts to SignalR group `server-{serverId}`
- ? Sends event named `WatchlistMetricsUpdated`

**Code Location:** `AgentMessageHandler.cs` line ~250

```csharp
await _hubContext.Clients.Group($"server-{serverId}")
    .SendAsync("WatchlistMetricsUpdated", new
    {
        ServerId = serverId,
        TimestampUtc = DateTime.UtcNow,
        Services = payload.Services,
        Processes = payload.Processes
    });
```

---

## ?? Frontend Requirements

### **What Frontend Must Do:**

#### **Step 1: Connect to SignalR Hub**
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://your-backend.com/monitoring-hub', {
    accessTokenFactory: () => localStorage.getItem('authToken')
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

#### **Step 2: Subscribe to Server**
```javascript
// CRITICAL: Must call this API endpoint first!
const connectionId = connection.connectionId;

await fetch(`https://your-backend.com/api/monitoring/subscribe/${serverId}`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'X-SignalR-ConnectionId': connectionId  // ?? REQUIRED!
  }
});
```

**?? This adds your connection to the SignalR group `server-{serverId}`**

#### **Step 3: Listen for Watchlist Events**
```javascript
// MUST subscribe to this event BEFORE subscribing to server
connection.on('WatchlistMetricsUpdated', (data) => {
  console.log('Watchlist metrics received:', data);
  console.log('Services:', data.services);
  console.log('Processes:', data.processes);
  
  // Update your UI here
  updateServicesUI(data.services);
  updateProcessesUI(data.processes);
});
```

---

## ?? Common Issues

### **Issue 1: Not Calling Subscribe API**

? **Wrong:**
```javascript
// Just listening to SignalR event
connection.on('WatchlistMetricsUpdated', (data) => {
  // This will NEVER fire!
});
```

? **Correct:**
```javascript
// 1. First, subscribe to server
await fetch('/api/monitoring/subscribe/1', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-SignalR-ConnectionId': connection.connectionId
  }
});

// 2. THEN listen
connection.on('WatchlistMetricsUpdated', (data) => {
  console.log('Received:', data);
});
```

---

### **Issue 2: Missing Connection ID Header**

? **Wrong:**
```javascript
await fetch('/api/monitoring/subscribe/1', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
    // Missing X-SignalR-ConnectionId!
  }
});
```

? **Correct:**
```javascript
await fetch('/api/monitoring/subscribe/1', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-SignalR-ConnectionId': connection.connectionId  // ? Required!
  }
});
```

---

### **Issue 3: Wrong Event Name**

? **Wrong:**
```javascript
connection.on('watchlistMetrics', ...);  // Wrong case
connection.on('WatchlistMetrics', ...);  // Missing "Updated"
connection.on('watchlist-metrics', ...); // Wrong format
```

? **Correct:**
```javascript
connection.on('WatchlistMetricsUpdated', ...);  // Exact match!
```

---

### **Issue 4: Subscribing to Wrong Server ID**

The agent sends watchlist metrics with its server ID. Make sure frontend subscribes to the **correct server ID**.

```javascript
// If agent is server ID 5
await fetch('/api/monitoring/subscribe/5', ...)  // ? Correct
await fetch('/api/monitoring/subscribe/1', ...)  // ? Wrong server!
```

---

## ?? How to Debug

### **Check 1: Is Agent Sending Watchlist Metrics?**

**Backend logs:**
```sh
docker-compose logs backend | grep -i watchlist
```

**Expected output:**
```
[INFO] Received watchlist metrics for server 1: 2 services, 3 processes
[DEBUG] Broadcast watchlist metrics for server 1
```

**If you DON'T see this:** Agent is not sending watchlist metrics.

---

### **Check 2: Is Frontend Subscribed?**

**Backend logs:**
```sh
docker-compose logs backend | grep -i "Subscribed"
```

**Expected output:**
```
[INFO] Subscribed: ConnectionId abc12345... ? Server 1
```

**If you DON'T see this:** Frontend didn't call `/api/monitoring/subscribe/{id}`

---

### **Check 3: Is SignalR Connection Working?**

**Frontend console:**
```javascript
connection.on('MetricsUpdated', (data) => {
  console.log('Regular metrics working:', data);
});
```

**If regular metrics work but watchlist doesn't:**
- ? SignalR connection is fine
- ? Problem is with watchlist event subscription

---

### **Check 4: Browser Network Tab**

1. Open browser DevTools ? Network tab
2. Filter by "monitoring"
3. Look for:
   - ? POST `/api/monitoring/subscribe/{id}` ? 200 OK
   - ? WebSocket connection to `/monitoring-hub` ? Status 101

---

## ?? Complete Working Example

```javascript
import * as signalR from '@microsoft/signalr';

async function setupWatchlistMonitoring(serverId) {
  const token = localStorage.getItem('authToken');
  
  // 1. Create SignalR connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('https://your-backend.com/monitoring-hub', {
      accessTokenFactory: () => token,
      transport: signalR.HttpTransportType.WebSockets,
      skipNegotiation: true
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // 2. Set up event listeners BEFORE starting
  connection.on('WatchlistMetricsUpdated', (data) => {
    console.log('? Watchlist metrics received!');
    console.log('Server ID:', data.serverId);
    console.log('Services:', data.services);
    console.log('Processes:', data.processes);
    
    // Update UI
    updateServicesTable(data.services);
    updateProcessesTable(data.processes);
  });

  connection.on('MetricsUpdated', (data) => {
    console.log('Regular metrics:', data);
  });

  // 3. Start connection
  await connection.start();
  console.log('SignalR connected, Connection ID:', connection.connectionId);

  // 4. Subscribe to server (THIS IS CRITICAL!)
  const response = await fetch(
    `https://your-backend.com/api/monitoring/subscribe/${serverId}`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'X-SignalR-ConnectionId': connection.connectionId
      }
    }
  );

  if (response.ok) {
    const result = await response.json();
    console.log('? Subscribed to server:', result);
  } else {
    console.error('? Failed to subscribe:', await response.text());
  }
}

// Usage
setupWatchlistMonitoring(1);  // Subscribe to server ID 1
```

---

## ?? Event Payload Structure

When watchlist metrics arrive, you'll receive:

```javascript
{
  "serverId": 1,
  "timestampUtc": "2025-12-30T10:30:00Z",
  "services": [
    {
      "name": "nginx",
      "activeState": "active",
      "subState": "running",
      "cpuUsagePercent": 1.5,
      "memoryUsage": 45.2,
      "mainPID": 1234,
      "startTime": "2025-12-30T08:00:00",
      "restartPolicy": "on-failure"
    }
  ],
  "processes": [
    {
      "pid": 5678,
      "name": "python3",
      "cmdline": "/usr/bin/python3 app.py",
      "cpuPercent": 12.3,
      "memoryMb": 256.5,
      "user": "www-data",
      "status": "running"
    },
    {
      "pid": null,
      "error": "Process not found: node server.js"
    }
  ]
}
```

---

## ? Checklist

Before asking "Why isn't it working?", verify:

- [ ] SignalR connection is established (`connection.state === 'Connected'`)
- [ ] Called `POST /api/monitoring/subscribe/{serverId}` with connection ID
- [ ] Listening to `WatchlistMetricsUpdated` (exact spelling!)
- [ ] Subscribed to correct server ID
- [ ] Agent is online and configured with watchlist
- [ ] Backend logs show "Received watchlist metrics"
- [ ] Backend logs show "Subscribed: ConnectionId..."

---

## ?? If Still Not Working

### **Enable Debug Logging:**

**Frontend:**
```javascript
connection.configureLogging(signalR.LogLevel.Debug);
```

**Backend:**
Add to `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR": "Debug",
      "BusinessLayer.Services.Concrete.AgentMessageHandler": "Debug"
    }
  }
}
```

**Then check both frontend console and backend logs.**

---

## ?? Quick Test

Run this in browser console after connecting:

```javascript
// Test if event listener is registered
console.log('Registered events:', 
  connection._methods ? Object.keys(connection._methods) : 'Unknown');

// Should show: ['WatchlistMetricsUpdated', 'MetricsUpdated', ...]
```

If `WatchlistMetricsUpdated` is not in the list, you didn't register the listener!

---

**TL;DR:** Backend is working fine. Frontend must:
1. ? Connect to SignalR
2. ? Call `/api/monitoring/subscribe/{id}` with connection ID header
3. ? Listen to `WatchlistMetricsUpdated` event

That's it! ??
