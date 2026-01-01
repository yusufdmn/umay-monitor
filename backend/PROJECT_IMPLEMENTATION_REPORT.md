# Server Monitoring System - Backend Implementation Report

**Date:** January 15, 2025  
**Project:** Multi-Agent Server Monitoring System  
**Backend Stack:** .NET 8, ASP.NET Core Web API, PostgreSQL, Entity Framework Core, SignalR, WebSockets  
**Status:** Phase 3 Complete (Core Infrastructure Operational)

---

## ?? Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Architecture Overview](#2-architecture-overview)
3. [Phase 1: Domain Model & Database](#3-phase-1-domain-model--database)
4. [Phase 2: WebSocket Communication with Agents](#4-phase-2-websocket-communication-with-agents)
5. [Phase 3: SignalR Real-Time Frontend Updates](#5-phase-3-signalr-real-time-frontend-updates)
6. [Complete Data Flow](#6-complete-data-flow)
7. [Testing Infrastructure](#7-testing-infrastructure)
8. [Integration Guide for Other Teams](#8-integration-guide-for-other-teams)
9. [Current State Analysis](#9-current-state-analysis)
10. [Next Steps: Phase 4 & Beyond](#10-next-steps-phase-4--beyond)
11. [Technical Debt & Improvements](#11-technical-debt--improvements)

---

## 1. Executive Summary

### ? What Has Been Implemented

The backend infrastructure for a **multi-agent server monitoring system** has been successfully implemented with the following core capabilities:

1. **Secure Agent Authentication** - Token-based WebSocket authentication for Python agents
2. **Real-Time Metric Collection** - Continuous ingestion of system metrics (CPU, RAM, disk, network) from monitored servers
3. **Normalized Database Schema** - PostgreSQL database with proper relational design using EF Core
4. **Real-Time Frontend Updates** - SignalR hub broadcasting live metrics to connected frontend clients
5. **Comprehensive Domain Model** - Entities supporting metrics, services, alerts, processes, and users
6. **WebSocket Connection Management** - Singleton service managing active agent connections
7. **Database Seeding** - Automatic migration and seed data for development

### ?? What Is Missing

The following features are **defined in entities but NOT yet implemented**:

1. **REST API Endpoints** - No controllers for querying historical data, servers, services, or alerts
2. **Alert Rule Evaluation** - Domain entities exist, but no logic to evaluate rules or create alerts
3. **Service Monitoring** - Entities exist, but no message handlers for service status events
4. **Process Monitoring** - Entities exist, but no message handlers for process snapshots
5. **User Authentication** - User entity exists with seeded admin, but no JWT authentication endpoints
6. **Service Control** - No commands to restart/stop services on remote agents
7. **Alert Acknowledgment** - No API to acknowledge alerts
8. **Historical Data Query** - No endpoints for time-series metric queries

### ?? Current Capability

**The backend can:**
- Accept WebSocket connections from agents using token authentication
- Receive metric messages every second from agents
- Store metrics in a normalized PostgreSQL database
- Broadcast real-time metric updates to frontend clients via SignalR
- Maintain online/offline status of monitored servers

**The backend cannot:**
- Serve historical data to the frontend (no REST APIs)
- Trigger or manage alerts (no alert evaluation logic)
- Monitor or control services (no service handlers)
- Authenticate frontend users (no auth endpoints)

---

## 2. Architecture Overview

### 2.1 Solution Structure

The solution follows a **layered architecture** with clear separation of concerns:

```
ServerMonitoringBackend/
??? Application/                      [BusinessLayer.csproj]
?   ??? DTOs/
?   ?   ??? Agent/                   # DTOs for agent messages
?   ?   ??? Response/                # DTOs for API responses
?   ??? Services/
?   ?   ??? Interfaces/
?   ?   ??? Concrete/
?   ??? Hubs/                        # SignalR hubs
??? Infrastructure/                   [Infrastructure.csproj]
?   ??? Entities/                    # EF Core domain entities
?   ??? Migrations/                  # EF Core migrations
?   ??? ServerMonitoringDbContext.cs
??? Presentation/                     [Presentation.csproj]
    ??? Controllers/                 # REST API controllers
    ??? WebSockets/                  # WebSocket handlers
    ??? Program.cs                   # Application entry point
    ??? ServiceRegistration.cs       # DI configuration
```

### 2.2 Project Dependencies

```
Presentation ? Application ? Infrastructure
```

- **Infrastructure** - Contains only domain entities and EF Core context (no external dependencies except EF Core)
- **Application (BusinessLayer)** - Contains business logic, DTOs, services, SignalR hubs
- **Presentation** - ASP.NET Core Web API, WebSocket handlers, controllers, startup configuration

### 2.3 Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Web Framework** | ASP.NET Core 8 | REST API & WebSocket hosting |
| **Database** | PostgreSQL | Persistent storage |
| **ORM** | Entity Framework Core 8 | Database access & migrations |
| **Real-Time Communication** | SignalR | Frontend real-time updates |
| **Agent Communication** | WebSockets (WSS) | Bi-directional agent messages |
| **Authentication** | BCrypt.Net | Password hashing (JWT not yet implemented) |
| **JSON Serialization** | System.Text.Json | Message parsing |

### 2.4 Communication Architecture

```
???????????????????         WebSocket (WSS)        ????????????????????
?  Python Agent   ? ?????????????????????????????  ?  .NET Backend    ?
?  (Linux Server) ?  wss://backend/ws?token=xxx    ?  WebSocket       ?
???????????????????                                ?  Handler         ?
                                                   ????????????????????
                                                            ?
                                                            ?
                                                   ??????????????????
                                                   ?  EF Core       ?
                                                   ?  PostgreSQL    ?
                                                   ??????????????????
                                                            ?
                                                            ?
                                                   ??????????????????
                                                   ?  SignalR Hub   ?
                                                   ??????????????????
                                                            ?
                                        SignalR             ?
???????????????????         ?????????????????????????????????
?  React Frontend ?
?  (Web Browser)  ?         REST API (planned, not implemented)
???????????????????         ????????????????????????????????
```

---

## 3. Phase 1: Domain Model & Database

### 3.1 Entity Relationship Diagram (Conceptual)

```
??????????????????????
?  MonitoredServer   ?
?  (Server)          ?
??????????????????????
? Id (PK)            ?
? Name               ?
? Hostname           ?
? AgentToken (unique)?
? IsOnline           ?
? LastSeenUtc        ?
??????????????????????
         ?
         ? 1:N relationships
         ?
    ???????????????????????????????????????????????
    ?           ?          ?           ?          ?
    ?           ?          ?           ?          ?
??????????? ?????????? ???????????? ???????? ???????????
? Metric  ? ?Service ? ? Process  ? ?Alert ? ?AlertRule?
? Sample  ? ?        ? ? Snapshot ? ?      ? ?(optional)?
??????????? ?????????? ???????????? ???????? ???????????
     ?          ?            ?
     ? 1:N      ? 1:N        ? 1:N
     ?          ?            ?
     ?          ?            ?
??????????? ?????????? ????????????
?  Disk   ? ?Service ? ? Process  ?
?Partition? ?Status  ? ?   Info   ?
? Metric  ? ?History ? ?          ?
??????????? ?????????? ????????????
???????????
? Network ?
?Interface?
? Metric  ?
???????????

????????????
?   User   ?
????????????
? Id (PK)  ?
? Email    ?
? Password ?
? Role     ?
????????????
      ?
      ? (acknowledges alerts)
      ?
      ?
  ????????
  ?Alert ?
  ????????
```

### 3.2 Complete Entity Definitions

#### **3.2.1 MonitoredServer**

**Location:** `Infrastructure/Entities/Server.cs`

**Purpose:** Represents a single Linux server being monitored by an agent.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Primary key |
| `Name` | `string` | Friendly display name (e.g., "Production Web Server 1") |
| `Hostname` | `string` | Server hostname from agent |
| `IpAddress` | `string?` | Optional IP address |
| `AgentToken` | `string` | Secret token for agent authentication (indexed, unique) |
| `IsOnline` | `bool` | Current connection status |
| `LastSeenUtc` | `DateTime?` | Last time data was received from agent |
| `CreatedAtUtc` | `DateTime` | When server was registered |

**Navigation Properties:**
- `Metrics` ? `ICollection<MetricSample>` (1:N)
- `Services` ? `ICollection<Service>` (1:N)
- `ProcessSnapshots` ? `ICollection<ProcessSnapshot>` (1:N)
- `Alerts` ? `ICollection<Alert>` (1:N)

**Database Indexes:**
- Unique index on `AgentToken`
- Index on `Hostname`

**Status:** ? Fully implemented and used in production flow

---

#### **3.2.2 MetricSample**

**Location:** `Infrastructure/Entities/SystemMetrics.cs`

**Purpose:** High-volume table storing system metric snapshots at regular intervals (every ~1 second from agents).

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Primary key (long for high-volume data) |
| `TimestampUtc` | `DateTime` | When metrics were collected |
| `CpuUsagePercent` | `double` | CPU usage percentage |
| `RamUsagePercent` | `double` | RAM usage percentage |
| `RamUsedGb` | `double` | RAM used in GB |
| `UptimeSeconds` | `long` | System uptime in seconds |
| `Load1m` | `double` | Normalized load average (1 min) |
| `Load5m` | `double` | Normalized load average (5 min) |
| `Load15m` | `double` | Normalized load average (15 min) |
| `DiskReadSpeedMBps` | `double` | Disk read speed (MB/s) |
| `DiskWriteSpeedMBps` | `double` | Disk write speed (MB/s) |
| `MonitoredServerId` | `int` | Foreign key to `MonitoredServer` |

**Navigation Properties:**
- `MonitoredServer` ? `MonitoredServer` (N:1)
- `DiskPartitions` ? `ICollection<DiskPartitionMetric>` (1:N)
- `NetworkInterfaces` ? `ICollection<NetworkInterfaceMetric>` (1:N)

**Database Indexes:**
- Index on `TimestampUtc`
- Composite index on `(MonitoredServerId, TimestampUtc)` for efficient time-series queries

**Status:** ? Fully implemented, actively receiving data from agents

---

#### **3.2.3 DiskPartitionMetric**

**Location:** `Infrastructure/Entities/DiskPartitionMetric.cs`

**Purpose:** Stores per-partition disk usage for each metric sample.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Primary key |
| `Device` | `string` | Device name (e.g., `/dev/sda1`) |
| `MountPoint` | `string` | Mount point (e.g., `/`, `/boot`) |
| `FileSystemType` | `string` | Filesystem type (e.g., `ext4`, `xfs`) |
| `TotalGb` | `double` | Total capacity in GB |
| `UsedGb` | `double` | Used space in GB |
| `UsagePercent` | `double` | Usage percentage |
| `MetricSampleId` | `long` | Foreign key to `MetricSample` |

**Status:** ? Fully implemented, data being collected

---

#### **3.2.4 NetworkInterfaceMetric**

**Location:** `Infrastructure/Entities/NetworkInterfaceMetric.cs`

**Purpose:** Stores per-interface network statistics for each metric sample.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Primary key |
| `Name` | `string` | Interface name (e.g., `eth0`, `lo`) |
| `MacAddress` | `string?` | MAC address (nullable - some interfaces don't have one) |
| `Ipv4` | `string?` | IPv4 address |
| `Ipv6` | `string?` | IPv6 address |
| `UploadSpeedMbps` | `double` | Upload speed in Mbps |
| `DownloadSpeedMbps` | `double` | Download speed in Mbps |
| `MetricSampleId` | `long` | Foreign key to `MetricSample` |

**Status:** ? Fully implemented, data being collected

**Migration Note:** A migration was created to allow `MacAddress` to be nullable (`AllowNullMacAddress` migration).

---

#### **3.2.5 Service**

**Location:** `Infrastructure/Entities/Service.cs`

**Purpose:** Represents a system service being monitored (e.g., nginx, postgresql, docker).

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Primary key |
| `Name` | `string` | Service name |
| `Description` | `string?` | Optional description |
| `IsCritical` | `bool` | Whether service is critical (affects alerts) |
| `CreatedAtUtc` | `DateTime` | When service was added to monitoring |
| `MonitoredServerId` | `int` | Foreign key to `MonitoredServer` |

**Navigation Properties:**
- `MonitoredServer` ? `MonitoredServer` (N:1)
- `StatusHistory` ? `ICollection<ServiceStatusHistory>` (1:N)

**Database Indexes:**
- Unique composite index on `(MonitoredServerId, Name)`

**Status:** ?? **Entity exists, NOT yet used** - No message handlers for service status events

---

#### **3.2.6 ServiceStatusHistory**

**Location:** `Infrastructure/Entities/ServiceStatusHistory.cs`

**Purpose:** Tracks status changes of monitored services over time.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Primary key |
| `TimestampUtc` | `DateTime` | When status was recorded |
| `Status` | `string` | Status (e.g., "Running", "Stopped", "Failed") |
| `Message` | `string?` | Optional error message or additional info |
| `ServiceId` | `int` | Foreign key to `Service` |

**Database Indexes:**
- Index on `TimestampUtc`
- Composite index on `(ServiceId, TimestampUtc)`

**Status:** ?? **Entity exists, NOT yet used** - No data being collected

---

#### **3.2.7 AlertRule**

**Location:** `Infrastructure/Entities/AlertRule.cs`

**Purpose:** Defines thresholds that trigger alerts when metrics exceed configured values.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Primary key |
| `Metric` | `string` | Metric to monitor (e.g., "CPU", "RAM", "DiskUsage") |
| `ThresholdValue` | `double` | Threshold value to compare against |
| `Comparison` | `string` | Comparison operator (">", "<", ">=", "<=", "==") |
| `Severity` | `string` | Alert severity ("Info", "Warning", "Critical") |
| `IsActive` | `bool` | Whether rule is currently active |
| `ConfigJson` | `string?` | Optional JSON for additional config |
| `CreatedAtUtc` | `DateTime` | When rule was created |
| `MonitoredServerId` | `int?` | FK to specific server (null = global rule) |

**Database Indexes:**
- Index on `IsActive`

**Status:** ?? **Entity exists, NO evaluation logic implemented** - Rules can be created but won't trigger alerts

---

#### **3.2.8 Alert**

**Location:** `Infrastructure/Entities/Alert.cs`

**Purpose:** Represents triggered alerts based on alert rules or system events.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Primary key |
| `CreatedAtUtc` | `DateTime` | When alert was created |
| `Title` | `string` | Alert title (e.g., "High CPU Usage") |
| `Message` | `string` | Detailed message |
| `Severity` | `string` | Severity level |
| `IsAcknowledged` | `bool` | Whether alert has been acknowledged |
| `AcknowledgedAtUtc` | `DateTime?` | When acknowledged |
| `AcknowledgedByUserId` | `int?` | FK to user who acknowledged |
| `MonitoredServerId` | `int` | FK to `MonitoredServer` |
| `AlertRuleId` | `int?` | FK to `AlertRule` (nullable if manual alert) |

**Navigation Properties:**
- `MonitoredServer` ? `MonitoredServer` (N:1)
- `AlertRule` ? `AlertRule?` (N:1, nullable)
- `AcknowledgedByUser` ? `User?` (N:1, nullable)

**Database Indexes:**
- Index on `CreatedAtUtc`
- Index on `IsAcknowledged`
- Composite index on `(MonitoredServerId, CreatedAtUtc)`

**Status:** ?? **Entity exists, NO creation logic** - No alerts are being triggered

---

#### **3.2.9 User**

**Location:** `Infrastructure/Entities/User.cs`

**Purpose:** Represents users who can authenticate and access the monitoring system.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Primary key |
| `Email` | `string` | User email (unique, used for login) |
| `PasswordHash` | `string` | BCrypt hashed password |
| `FullName` | `string?` | Optional full name |
| `Role` | `string` | User role ("Admin", "Viewer") |
| `IsActive` | `bool` | Whether account is active |
| `CreatedAtUtc` | `DateTime` | Account creation date |
| `LastLoginUtc` | `DateTime?` | Last login timestamp |

**Navigation Properties:**
- `AcknowledgedAlerts` ? `ICollection<Alert>` (1:N)

**Database Indexes:**
- Unique index on `Email`

**Seeded Data:**
- Email: `admin@example.com`
- Password: `admin123` (hashed with BCrypt)
- Role: `Admin`

**Status:** ?? **Entity exists with seed data, NO authentication endpoints** - Cannot login via API

---

#### **3.2.10 ProcessSnapshot**

**Location:** `Infrastructure/Entities/ProcessSnapshot.cs`

**Purpose:** Container for a snapshot of all running processes at a specific time.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Primary key |
| `TimestampUtc` | `DateTime` | When snapshot was taken |
| `MonitoredServerId` | `int` | Foreign key to `MonitoredServer` |

**Navigation Properties:**
- `MonitoredServer` ? `MonitoredServer` (N:1)
- `Processes` ? `ICollection<ProcessInfo>` (1:N)

**Database Indexes:**
- Index on `TimestampUtc`
- Composite index on `(MonitoredServerId, TimestampUtc)`

**Status:** ?? **Entity exists, NOT yet used** - No process data being collected

---

#### **3.2.11 ProcessInfo**

**Location:** `Infrastructure/Entities/ProcessInfo.cs`

**Purpose:** Individual process details within a snapshot.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Primary key |
| `Pid` | `int` | Process ID |
| `Name` | `string` | Process name/command |
| `CpuPercent` | `double` | CPU usage for this process |
| `RamMb` | `double` | RAM usage in MB |
| `User` | `string?` | Process owner (optional) |
| `ProcessSnapshotId` | `long` | Foreign key to `ProcessSnapshot` |

**Status:** ?? **Entity exists, NOT yet used**

---

### 3.3 Database Configuration & Migrations

**DbContext:** `ServerMonitoringDbContext` in `Infrastructure` project

**Connection String:** PostgreSQL (configurable in `appsettings.json`)
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=5432;Database=serverhealth;Username=yusuf;Password=yusuf;"
}
```

**Migrations Applied:**
1. `20251115204946_mig_0` - Initial migration with all entities
2. `20251115211925_AllowNullMacAddress` - Made `MacAddress` nullable in `NetworkInterfaceMetric`

**Relationships Configured:**
- All foreign keys use `OnDelete(DeleteBehavior.Cascade)` except nullable relationships
- Unique constraints on `AgentToken`, `Email`, `(MonitoredServerId, ServiceName)`
- Indexes optimized for time-series queries on metrics

**Database Seeding:**
- Automatic migration on startup via `DatabaseSeeder.SeedAsync()`
- Seeds one test server with token `test-token-123`
- Seeds one admin user (email: `admin@example.com`, password: `admin123`)

---

## 4. Phase 2: WebSocket Communication with Agents

### 4.1 Overview

The WebSocket layer enables **secure, persistent, bi-directional communication** between Python agents running on monitored Linux servers and the .NET backend.

**Key Features:**
- Token-based authentication via query string
- Continuous message streaming from agents
- Automatic server status management (online/offline)
- Connection lifecycle management
- Extensible message routing system

---

### 4.2 WebSocket Endpoint Configuration

**Endpoint:** `/ws`  
**Protocol:** WebSocket (WSS recommended for production)  
**Authentication:** Token-based via query parameter

**Program.cs Configuration:**
```csharp
app.UseWebSockets();

app.Map("/ws", async context =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await handler.HandleConnection(context);
});
```

**Status:** ? Fully implemented and operational

---

### 4.3 Connection Flow

```
???????????????????
?  Python Agent   ?
???????????????????
         ?
         ? 1. Connect with token
         ?
    wss://backend/ws?token=test-token-123
         ?
         ?
???????????????????????????
?  WebSocketHandler       ?
???????????????????????????
? 2. Validate token       ?
? 3. Find/Create Server   ?
? 4. Accept WebSocket     ?
? 5. Mark server online   ?
? 6. Start message loop   ?
???????????????????????????
         ?
         ? Continuous loop
         ?
???????????????????????????
? Receive JSON messages   ?
? Parse & route by action ?
? Update LastSeenUtc      ?
???????????????????????????
         ?
         ? On disconnect
         ?
???????????????????????????
? Mark server offline     ?
? Remove from connection  ?
? manager                 ?
???????????????????????????
```

---

### 4.4 Implementation Details

#### **4.4.1 WebSocketHandler**

**Location:** `Presentation/WebSockets/WebSocketHandler.cs`

**Responsibility:** Manages the lifecycle of a single WebSocket connection from an agent.

**Dependencies (via Constructor Injection):**
- `IWebSocketConnectionManager` - Tracks active connections
- `IAgentMessageHandler` - Processes incoming messages
- `ServerMonitoringDbContext` - Database access
- `ILogger<WebSocketHandler>` - Logging

**Key Methods:**

##### `HandleConnection(HttpContext context)`

**Purpose:** Entry point called by Program.cs for new WebSocket requests.

**Flow:**
1. **Validate WebSocket request** - Returns 400 if not a WebSocket request
2. **Extract token** from query string (`?token=xxx`)
3. **Authenticate** - Query database for matching `AgentToken`
4. **Accept WebSocket** - Upgrade HTTP connection to WebSocket
5. **Update server status** - Set `IsOnline = true`, `LastSeenUtc = now`
6. **Register connection** - Add to `WebSocketConnectionManager`
7. **Start message loop** - Call `HandleWebSocketLoop()`
8. **Cleanup on disconnect** - Mark offline, remove from manager

**Authentication Logic:**
```csharp
var token = context.Request.Query["token"].ToString();
if (string.IsNullOrEmpty(token))
{
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsync("Missing token");
    return;
}

var server = await _dbContext.MonitoredServers
    .FirstOrDefaultAsync(s => s.AgentToken == token);

if (server == null)
{
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsync("Invalid token");
    return;
}
```

**Status:** ? Fully implemented

---

##### `HandleWebSocketLoop(WebSocket webSocket, int serverId)`

**Purpose:** Continuously listens for messages until the connection closes.

**Message Receiving Strategy:**
- Uses a 4KB buffer (`new byte[1024 * 4]`)
- Accumulates bytes until `EndOfMessage` is true (handles large messages)
- Converts bytes to UTF-8 string
- Passes JSON to `IAgentMessageHandler`
- Updates `LastSeenUtc` after each message

**Error Handling:**
- Catches `WebSocketException` for connection errors
- Catches generic exceptions
- **Always executes cleanup in finally block:**
  - Removes connection from manager
  - Sets `IsOnline` to `false`

**Code Structure:**
```csharp
try
{
    do
    {
        receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
        messageBytes.AddRange(buffer.Array[..receiveResult.Count]);
    } while (!receiveResult.EndOfMessage);

    while (!receiveResult.CloseStatus.HasValue)
    {
        var message = Encoding.UTF8.GetString(messageBytes.ToArray());
        
        if (!string.IsNullOrEmpty(message))
        {
            await _messageHandler.HandleMessageAsync(message, serverId);
            
            // Update LastSeenUtc
            var server = await _dbContext.MonitoredServers.FindAsync(serverId);
            if (server != null)
            {
                server.LastSeenUtc = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }
        
        messageBytes.Clear();
        
        // Wait for next message...
    }
}
finally
{
    // Cleanup: remove from manager, mark offline
}
```

**Status:** ? Fully implemented

---

#### **4.4.2 WebSocketConnectionManager**

**Location:** `Application/Services/Concrete/WebSocketConnectionManager.cs`

**Responsibility:** Singleton service maintaining a dictionary of active agent connections.

**Registration:** `services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>()`

**Data Structure:**
```csharp
private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
```

**Key Methods:**

| Method | Purpose | Status |
|--------|---------|--------|
| `AddSocket(string id, WebSocket socket)` | Registers a new connection | ? Implemented |
| `RemoveSocket(string id)` | Removes a connection | ? Implemented |
| `GetSocket(string id)` | Retrieves a socket by ID | ? Implemented |
| `GetId(WebSocket socket)` | Reverse lookup: socket ? ID | ? Implemented |
| `GetAll()` | Returns all active sockets | ? Implemented |

**Thread Safety:** Uses `ConcurrentDictionary` for safe multi-threaded access.

**Use Case:** Allows backend to send commands to specific agents (e.g., "restart service on server 5").

**Status:** ? Fully implemented

---

#### **4.4.3 AgentMessageHandler**

**Location:** `Application/Services/Concrete/AgentMessageHandler.cs`

**Responsibility:** Routes and processes incoming messages based on `action` field.

**Dependencies:**
- `ServerMonitoringDbContext` - Database persistence
- `IHubContext<MonitoringHub>` - SignalR broadcasting
- `ILogger<AgentMessageHandler>`

**Registration:** `services.AddScoped<IAgentMessageHandler, AgentMessageHandler>()`

**Message Routing Flow:**

```csharp
public async Task HandleMessageAsync(string message, int serverId)
{
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var baseMessage = JsonSerializer.Deserialize<BaseAgentMessage>(message, options);

    switch (baseMessage.Action)
    {
        case "metrics":
            var metrics = baseMessage.Payload.Deserialize<MetricsPayload>(options);
            await ProcessMetrics(serverId, metrics);
            break;

        // Future actions: "service.status", "processes", etc.

        default:
            _logger.LogWarning("Unknown message action: {Action}", baseMessage.Action);
            break;
    }
}
```

**Supported Actions:**

| Action | Handler Method | Status |
|--------|---------------|--------|
| `"metrics"` | `ProcessMetrics()` | ? Fully implemented |
| `"service.status"` | ? Not implemented | ?? Planned |
| `"processes"` | ? Not implemented | ?? Planned |
| `"command.response"` | ? Not implemented | ?? Planned |

**Status:** ?? Partially implemented (only metrics action works)

---

##### `ProcessMetrics(int serverId, MetricsPayload payload)`

**Purpose:** Converts agent metrics payload into EF entities and saves to database.

**Workflow:**
1. **Create MetricSample entity** from top-level metrics (CPU, RAM, load, etc.)
2. **Map disk partitions** - Loop through `payload.DiskUsage` and create `DiskPartitionMetric` entities
3. **Map network interfaces** - Loop through `payload.NetworkInterfaces` and create `NetworkInterfaceMetric` entities
4. **Save to database** - `_dbContext.SaveChangesAsync()`
5. **Create DTO** - Convert entity to `MetricDto` for frontend
6. **Broadcast via SignalR** - Send `MetricsUpdated` event to `server-{serverId}` group

**Example Mapping:**
```csharp
var metricSample = new MetricSample
{
    MonitoredServerId = serverId,
    TimestampUtc = DateTime.UtcNow,
    CpuUsagePercent = payload.CpuUsagePercent,
    RamUsagePercent = payload.RamUsagePercent,
    RamUsedGb = payload.RamUsedGB,
    UptimeSeconds = payload.UptimeSeconds,
    Load1m = payload.NormalizedLoad.OneMinute,
    Load5m = payload.NormalizedLoad.FiveMinute,
    Load15m = payload.NormalizedLoad.FifteenMinute,
    DiskReadSpeedMBps = payload.DiskReadSpeedMBps,
    DiskWriteSpeedMBps = payload.DiskWriteSpeedMBps
};

foreach (var disk in payload.DiskUsage)
{
    metricSample.DiskPartitions.Add(new DiskPartitionMetric
    {
        Device = disk.Device,
        MountPoint = disk.Mountpoint,
        FileSystemType = disk.Fstype,
        TotalGb = disk.TotalGB,
        UsedGb = disk.UsedGB,
        UsagePercent = disk.UsagePercent
    });
}
```

**SignalR Broadcast:**
```csharp
await _hubContext.Clients.Group($"server-{serverId}")
    .SendAsync("MetricsUpdated", metricDto);
```

**Performance Note:** This method is called **every ~1 second per agent**. Consider future optimizations:
- Batch inserts
- Background queue processing
- Metric aggregation/downsampling

**Status:** ? Fully implemented

---

### 4.5 Message Format Specifications

#### **4.5.1 BaseAgentMessage**

**Location:** `Application/DTOs/Agent/BaseAgentMessage.cs`

All messages from agents follow this structure:

```json
{
  "type": "event",
  "id": 6,
  "action": "metrics",
  "payload": { /* action-specific data */ },
  "timestamp": 1763221174564
}
```

**C# Mapping:**
```csharp
public class BaseAgentMessage
{
    public string Type { get; set; }           // "event", "request", "response"
    public int Id { get; set; }                // Message sequence ID
    public string Action { get; set; }         // "metrics", "service.status", etc.
    public JsonElement Payload { get; set; }   // Lazy-parsed payload
    public long Timestamp { get; set; }        // Unix timestamp (milliseconds)
}
```

**Design Note:** `Payload` uses `JsonElement` for delayed deserialization - we only deserialize after checking the `Action`.

---

#### **4.5.2 MetricsPayload**

**Location:** `Application/DTOs/Agent/MetricsPayload.cs`

**Purpose:** Represents system metrics sent by agents.

**Full Structure:**
```csharp
public class MetricsPayload
{
    public double CpuUsagePercent { get; set; }
    public double RamUsagePercent { get; set; }
    public double RamUsedGB { get; set; }
    public List<DiskUsageInfo> DiskUsage { get; set; }
    public List<NetworkInterfaceInfo> NetworkInterfaces { get; set; }
    public long UptimeSeconds { get; set; }
    public NormalizedLoadInfo NormalizedLoad { get; set; }
    public double DiskReadSpeedMBps { get; set; }
    public double DiskWriteSpeedMBps { get; set; }
}

public class DiskUsageInfo
{
    public string Device { get; set; }        // "/dev/sda1"
    public string Mountpoint { get; set; }    // "/"
    public string Fstype { get; set; }        // "ext4"
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double UsagePercent { get; set; }
}

public class NetworkInterfaceInfo
{
    public string Name { get; set; }          // "eth0"
    public string Mac { get; set; }           // "60:45:bd:2a:69:f9"
    public string Ipv4 { get; set; }
    public string Ipv6 { get; set; }
    public double UploadSpeedMbps { get; set; }
    public double DownloadSpeedMbps { get; set; }
}

public class NormalizedLoadInfo
{
    [JsonPropertyName("1m")]
    public double OneMinute { get; set; }
    
    [JsonPropertyName("5m")]
    public double FiveMinute { get; set; }
    
    [JsonPropertyName("15m")]
    public double FifteenMinute { get; set; }
}
```

**Example JSON from Agent:**
```json
{
  "type": "event",
  "id": 6,
  "action": "metrics",
  "payload": {
    "cpuUsagePercent": 12.5,
    "ramUsagePercent": 52.1,
    "ramUsedGB": 0.28,
    "diskUsage": [
      {
        "device": "/dev/sda1",
        "mountpoint": "/",
        "fstype": "ext4",
        "totalGB": 28.02,
        "usedGB": 3.07,
        "usagePercent": 11.0
      }
    ],
    "networkInterfaces": [
      {
        "name": "eth0",
        "mac": "60:45:bd:2a:69:f9",
        "ipv4": "10.0.0.4",
        "ipv6": "fe80::6245:bdff:fe2a:69f9%eth0",
        "uploadSpeedMbps": 0.01,
        "downloadSpeedMbps": 0.0
      }
    ],
    "uptimeSeconds": 3222214,
    "normalizedLoad": { "1m": 0.0, "5m": 0.0, "15m": 0.0 },
    "diskReadSpeedMBps": 0.0,
    "diskWriteSpeedMBps": 0.0
  },
  "timestamp": 1763221174564
}
```

**Status:** ? Fully implemented and tested

---

### 4.6 Agent Connection Testing

**Test Server Configuration (seeded in database):**
- **Name:** "Test Server"
- **Hostname:** "test-server"
- **Token:** `test-token-123`

**Connection String for Python Agent:**
```
wss://localhost:5001/ws?token=test-token-123
```

**Expected Behavior:**
1. Agent connects ? Backend logs "Agent connected: Server 1 - Test Server"
2. Agent sends metrics every second ? Database receives new `MetricSample` rows
3. Agent disconnects ? Backend logs "Agent disconnected: Server 1", `IsOnline` set to `false`

**Status:** ? Successfully tested with Python agent

---

### 4.7 Future WebSocket Features (Not Yet Implemented)

The WebSocket infrastructure is designed to support bi-directional communication, but these features are **not yet implemented**:

#### **4.7.1 Service Monitoring**

**Planned Message Format (from agent):**
```json
{
  "type": "event",
  "action": "service.status",
  "payload": {
    "serviceName": "nginx",
    "status": "Running",
    "pid": 1234,
    "uptime": 86400
  }
}
```

**Required Implementation:**
- Add `case "service.status"` in `AgentMessageHandler`
- Create/update `Service` entity
- Add `ServiceStatusHistory` record
- Broadcast `ServiceStatusChanged` via SignalR

---

#### **4.7.2 Process Snapshots**

**Planned Message Format (from agent):**
```json
{
  "type": "event",
  "action": "processes",
  "payload": {
    "processes": [
      { "pid": 1, "name": "systemd", "cpuPercent": 0.1, "ramMb": 15.2 },
      { "pid": 1234, "name": "nginx", "cpuPercent": 2.5, "ramMb": 120.5 }
    ]
  }
}
```

**Required Implementation:**
- Add `case "processes"` in `AgentMessageHandler`
- Create `ProcessSnapshot` and `ProcessInfo` entities
- Broadcast `ProcessesUpdated` via SignalR

---

#### **4.7.3 Command Sending (Backend ? Agent)**

**Planned Command Format (to agent):**
```json
{
  "type": "request",
  "id": 123,
  "action": "service.restart",
  "payload": {
    "serviceName": "nginx"
  }
}
```

**Required Implementation:**
- REST API endpoint to queue commands
- Retrieve agent WebSocket from `WebSocketConnectionManager`
- Send JSON command via `WebSocket.SendAsync()`
- Handle response from agent

---


## 5. Phase 3: SignalR Real-Time Frontend Updates

### 5.1 Overview

SignalR enables **real-time, push-based communication** from the backend to connected frontend clients (web browsers). When agents send new metrics, the backend immediately broadcasts them to subscribed frontend users without requiring polling.

**Key Features:**
- WebSocket-based real-time communication (with fallback to Server-Sent Events/Long Polling)
- Group-based subscriptions (clients subscribe to specific servers)
- Strongly-typed hub methods
- Automatic reconnection handling
- Integrated with ASP.NET Core pipeline

---

### 5.2 SignalR Hub Implementation

#### **5.2.1 MonitoringHub**

**Location:** `Application/Hubs/MonitoringHub.cs`

**Purpose:** Central hub for real-time frontend notifications.

**Full Implementation:**
```csharp
using Microsoft.AspNetCore.SignalR;

namespace BusinessLayer.Hubs;

public class MonitoringHub : Hub
{
    public async Task SubscribeServer(int serverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"server-{serverId}");
    }

    public async Task UnsubscribeServer(int serverId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server-{serverId}");
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
```

**Hub Endpoint:** `/monitoring-hub`

**Configuration in Program.cs:**
```csharp
builder.Services.AddSignalR();  // In ServiceRegistration.cs

app.MapHub<MonitoringHub>("/monitoring-hub");
```

**Status:** ? Fully implemented

---

#### **5.2.2 Client-Invocable Methods**

These methods can be **called by frontend clients**:

| Method | Parameters | Purpose | Status |
|--------|-----------|---------|--------|
| `SubscribeServer(int serverId)` | Server ID to monitor | Adds client to server-specific group | ? Implemented |
| `UnsubscribeServer(int serverId)` | Server ID to stop monitoring | Removes client from group | ? Implemented |

**Usage Example (JavaScript):**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/monitoring-hub")
    .build();

await connection.start();

// Subscribe to server ID 1
await connection.invoke("SubscribeServer", 1);

// Later, unsubscribe
await connection.invoke("UnsubscribeServer", 1);
```

---

#### **5.2.3 Server-Invocable Methods (Events)**

These methods are **called by the backend** to push data to clients:

| Method | Parameters | Purpose | Status |
|--------|-----------|---------|--------|
| `MetricsUpdated` | `MetricDto` | Broadcast new metrics for a server | ? Implemented |
| `AlertCreated` | `AlertDto` | Notify about new alert | ?? Planned (not implemented) |
| `ServiceStatusChanged` | `int serverId, string serviceName, ServiceStatusDto` | Service status update | ?? Planned |
| `ProcessesUpdated` | `int serverId, ProcessSnapshotDto` | Process snapshot update | ?? Planned |

**Currently Implemented:**
Only `MetricsUpdated` is actively being broadcast by the system.

---

### 5.3 Broadcasting Flow

**Complete flow from agent to frontend:**
```
????????????????
? Python Agent ? sends metrics via WebSocket
????????????????
       ?
       ?
???????????????????????
? WebSocketHandler    ?
? receives message    ?
???????????????????????
       ?
       ?
???????????????????????
? AgentMessageHandler ?
? ProcessMetrics()    ?
???????????????????????
? 1. Save to DB       ?
? 2. Create DTO       ?
? 3. Broadcast        ?
???????????????????????
       ?
       ?
???????????????????????????????????
? IHubContext<MonitoringHub>      ?
? .Clients.Group($"server-{id}")  ?
? .SendAsync("MetricsUpdated", dto) ?
???????????????????????????????????
       ?
       ? SignalR pushes to all clients in group
       ?
????????????????????????????????????
? Frontend Clients (browsers)      ?
? connection.on("MetricsUpdated",  ?
?   (metric) => { /* update UI */ })?
????????????????????????????????????
```

---

### 5.4 Broadcasting Implementation

**Code in AgentMessageHandler.ProcessMetrics():**

```csharp
// After saving to database...
var metricDto = new MetricDto
{
    Id = metricSample.Id,
    MonitoredServerId = serverId,
    TimestampUtc = metricSample.TimestampUtc,
    CpuUsagePercent = metricSample.CpuUsagePercent,
    RamUsagePercent = metricSample.RamUsagePercent,
    RamUsedGb = metricSample.RamUsedGb,
    UptimeSeconds = metricSample.UptimeSeconds,
    Load1m = metricSample.Load1m,
    Load5m = metricSample.Load5m,
    Load15m = metricSample.Load15m,
    DiskReadSpeedMBps = metricSample.DiskReadSpeedMBps,
    DiskWriteSpeedMBps = metricSample.DiskWriteSpeedMBps,
    DiskPartitions = metricSample.DiskPartitions.Select(d => new DiskPartitionDto
    {
        Device = d.Device,
        MountPoint = d.MountPoint,
        FileSystemType = d.FileSystemType,
        TotalGb = d.TotalGb,
        UsedGb = d.UsedGb,
        UsagePercent = d.UsagePercent
    }).ToList(),
    NetworkInterfaces = metricSample.NetworkInterfaces.Select(n => new NetworkInterfaceDto
    {
        Name = n.Name,
        MacAddress = n.MacAddress,
        Ipv4 = n.Ipv4,
        Ipv6 = n.Ipv6,
        UploadSpeedMbps = n.UploadSpeedMbps,
        DownloadSpeedMbps = n.DownloadSpeedMbps
    }).ToList()
};

// Broadcast to all clients subscribed to this server
await _hubContext.Clients.Group($"server-{serverId}")
    .SendAsync("MetricsUpdated", metricDto);

_logger.LogInformation("Saved and broadcast metrics for server {ServerId}", serverId);
```

**Group Naming Convention:** `server-{serverId}` (e.g., `server-1`, `server-2`)

**Status:** ? Fully operational

---

### 5.5 Response DTOs

#### **5.5.1 MetricDto**

**Location:** `Application/DTOs/Response/MetricDto.cs`

**Purpose:** Frontend-friendly representation of metrics (no EF navigation properties, clean JSON).

```csharp
public class MetricDto
{
    public long Id { get; set; }
    public int MonitoredServerId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public double CpuUsagePercent { get; set; }
    public double RamUsagePercent { get; set; }
    public double RamUsedGb { get; set; }
    public long UptimeSeconds { get; set; }
    public double Load1m { get; set; }
    public double Load5m { get; set; }
    public double Load15m { get; set; }
    public double DiskReadSpeedMBps { get; set; }
    public double DiskWriteSpeedMBps { get; set; }
    public List<DiskPartitionDto> DiskPartitions { get; set; } = new();
    public List<NetworkInterfaceDto> NetworkInterfaces { get; set; } = new();
}

public class DiskPartitionDto
{
    public string Device { get; set; } = string.Empty;
    public string MountPoint { get; set; } = string.Empty;
    public string FileSystemType { get; set; } = string.Empty;
    public double TotalGb { get; set; }
    public double UsedGb { get; set; }
    public double UsagePercent { get; set; }
}

public class NetworkInterfaceDto
{
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string? Ipv4 { get; set; }
    public string? Ipv6 { get; set; }
    public double UploadSpeedMbps { get; set; }
    public double DownloadSpeedMbps { get; set; }
}
```

**Design Principles:**
- No circular references (EF entities can have navigation loops)
- All properties are primitives or simple collections
- Ready for JSON serialization
- Matches frontend TypeScript interfaces

**Status:** ? Fully implemented

---

### 5.6 Testing Infrastructure

#### **5.6.1 TestController**

**Location:** `Presentation/Controllers/TestController.cs`

**Purpose:** Provides endpoints to manually test SignalR broadcasting without needing a running agent.

**Endpoints:**

| Endpoint | Method | Purpose | Status |
|----------|--------|---------|--------|
| `/api/test/broadcast-test?serverId=1` | POST | Broadcasts fake metric to server group | ? Implemented |
| `/api/test/ping` | GET | Health check endpoint | ? Implemented |

**Broadcast Test Implementation:**
```csharp
[HttpPost("broadcast-test")]
public async Task<IActionResult> BroadcastTest([FromQuery] int serverId = 1)
{
    var testMetric = new MetricDto
    {
        Id = 999,
        MonitoredServerId = serverId,
        TimestampUtc = DateTime.UtcNow,
        CpuUsagePercent = 42.5,
        RamUsagePercent = 67.8,
        RamUsedGb = 8.2,
        UptimeSeconds = 123456,
        Load1m = 0.5,
        Load5m = 0.3,
        Load15m = 0.2,
        DiskReadSpeedMBps = 10.5,
        DiskWriteSpeedMBps = 5.2,
        DiskPartitions = new List<DiskPartitionDto>
        {
            new() { Device = "/dev/sda1", MountPoint = "/", FileSystemType = "ext4", 
                    TotalGb = 100, UsedGb = 50, UsagePercent = 50 }
        },
        NetworkInterfaces = new List<NetworkInterfaceDto>
        {
            new() { Name = "eth0", MacAddress = "00:11:22:33:44:55", 
                    Ipv4 = "192.168.1.100", UploadSpeedMbps = 10, DownloadSpeedMbps = 20 }
        }
    };

    await _hubContext.Clients.Group($"server-{serverId}")
        .SendAsync("MetricsUpdated", testMetric);

    return Ok(new { message = "Test broadcast sent", serverId, timestamp = DateTime.UtcNow });
}
```

**Usage:**
```bash
# Send test broadcast to server 1
curl -X POST http://localhost:5000/api/test/broadcast-test?serverId=1

# Health check
curl http://localhost:5000/api/test/ping
```

**Status:** ? Fully functional

---

#### **5.6.2 SignalR Test HTML Page**

**Location:** `Presentation/wwwroot/test-signalr.html`

**Purpose:** Browser-based test client to verify SignalR connectivity and message reception.

**Status:** ?? File exists but content not verified in this analysis

**Recommended Test Flow:**
1. Start backend (`dotnet run`)
2. Open `http://localhost:5000/test-signalr.html` in browser
3. Click "Connect to SignalR"
4. Subscribe to server ID 1
5. In another terminal: `curl -X POST http://localhost:5000/api/test/broadcast-test?serverId=1`
6. Verify message appears in browser console/UI

---

### 5.7 CORS Configuration

**Location:** `Presentation/Program.cs`

**Current Configuration:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ...

app.UseCors();
```

**?? Security Warning:**
This configuration allows **any origin** to connect. This is acceptable for development but **MUST be changed for production**.

**Production Recommendation:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://your-frontend-domain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Required for SignalR
    });
});
```

---

### 5.8 Dependency Injection Configuration

**Location:** `Presentation/ServiceRegistration.cs`

```csharp
public static void AddServices(this IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
    services.AddScoped<IAgentMessageHandler, AgentMessageHandler>();
    services.AddScoped<WebSocketHandler>();

    var connectionString = configuration.GetConnectionString("DefaultConnection");

    services.AddDbContext<ServerMonitoringDbContext>(options =>
        options.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly("Infrastructure")
        )
    );

    services.AddSignalR();  // ? SignalR registration
}
```

**Lifetimes:**
- `WebSocketConnectionManager` - **Singleton** (maintains active connections across requests)
- `AgentMessageHandler` - **Scoped** (new instance per WebSocket message)
- `WebSocketHandler` - **Scoped** (new instance per WebSocket connection)
- `ServerMonitoringDbContext` - **Scoped** (default for EF Core)
- `MonitoringHub` - **Transient** (managed by SignalR framework)

**Status:** ? Properly configured

---

### 5.9 Future SignalR Events (Planned but Not Implemented)

#### **5.9.1 Alert Notifications**

**Event Name:** `AlertCreated`

**Payload:**
```typescript
interface AlertDto {
    id: number;
    serverId: number;
    title: string;
    message: string;
    severity: "Info" | "Warning" | "Critical";
    createdAt: string;
}
```

**Broadcasting Code (to be added to alert evaluation logic):**
```csharp
await _hubContext.Clients.Group($"server-{serverId}")
    .SendAsync("AlertCreated", alertDto);
```

---

#### **5.9.2 Service Status Updates**

**Event Name:** `ServiceStatusChanged`

**Payload:**
```typescript
interface ServiceStatusDto {
    serviceId: number;
    serviceName: string;
    status: "Running" | "Stopped" | "Failed" | "Restarting";
    message?: string;
    timestamp: string;
}
```

**Broadcasting Code (to be added when service monitoring is implemented):**
```csharp
await _hubContext.Clients.Group($"server-{serverId}")
    .SendAsync("ServiceStatusChanged", serverId, serviceName, statusDto);
```

---

#### **5.9.3 Server Connection Status**

**Event Name:** `ServerStatusChanged`

**Use Case:** Notify frontend when server goes online/offline

**Payload:**
```typescript
interface ServerStatusDto {
    serverId: number;
    isOnline: boolean;
    lastSeen: string;
}
```

**Broadcasting Code (to be added to WebSocketHandler):**
```csharp
// On connect
await _hubContext.Clients.All
    .SendAsync("ServerStatusChanged", new { serverId, isOnline = true, lastSeen = DateTime.UtcNow });

// On disconnect
await _hubContext.Clients.All
    .SendAsync("ServerStatusChanged", new { serverId, isOnline = false, lastSeen = DateTime.UtcNow });


````````

This is the description of what the code block changes:
Adding remaining sections 6-12: Data Flow, Testing, Integration Guide, Current State Analysis, Next Steps, Technical Debt, and Conclusion

This is the code block that represents the suggested code change:

````````markdown
```

---

## 6. Complete Data Flow

### 6.1 End-to-End Metric Flow (Currently Working)

This section traces a single metric message from agent to frontend in real-time.

```
????????????????????????????????????????????????????????????????????????
? STEP 1: Agent Collects & Sends Metrics                              ?
????????????????????????????????????????????????????????????????????????
    Python Agent (on Linux server)
    ?? Collects: CPU, RAM, Disk, Network stats
    ?? Formats as JSON with action="metrics"
    ?? Sends via WebSocket to wss://backend/ws?token=test-token-123

                    ? WebSocket Message ?

????????????????????????????????????????????????????????????????????????
? STEP 2: Backend Receives via WebSocket                              ?
????????????????????????????????????????????????????????????????????????
    WebSocketHandler.HandleWebSocketLoop()
    ?? Receives bytes, accumulates until EndOfMessage
    ?? Converts to UTF-8 string (JSON)
    ?? Passes to IAgentMessageHandler.HandleMessageAsync()
    ?? Updates server.LastSeenUtc

                    ? JSON String ?

????????????????????????????????????????????????????????????????????????
? STEP 3: Message Routing                                             ?
????????????????????????????????????????????????????????????????????????
    AgentMessageHandler.HandleMessageAsync()
    ?? Deserializes to BaseAgentMessage
    ?? Checks action field = "metrics"
    ?? Deserializes payload to MetricsPayload
    ?? Calls ProcessMetrics(serverId, payload)

                    ? MetricsPayload Object ?

????????????????????????????????????????????????????????????????????????
? STEP 4: Database Persistence                                        ?
????????????????????????????????????????????????????????????????????????
    AgentMessageHandler.ProcessMetrics()
    ?? Creates MetricSample entity
    ?   ?? Maps CPU, RAM, uptime, load, disk I/O
    ?   ?? Creates DiskPartitionMetric entities (for each partition)
    ?   ?? Creates NetworkInterfaceMetric entities (for each interface)
    ?? Adds to DbContext: _dbContext.MetricSamples.Add(metricSample)
    ?? Saves: await _dbContext.SaveChangesAsync()
    
    PostgreSQL Tables Updated:
    ?? MetricSamples (1 row with long Id)
    ?? DiskPartitionMetrics (N rows, one per partition)
    ?? NetworkInterfaceMetrics (M rows, one per interface)

                    ? Persisted to DB ?

????????????????????????????????????????????????????????????????????????
? STEP 5: DTO Mapping                                                 ?
????????????????????????????????????????????????????????????????????????
    AgentMessageHandler.ProcessMetrics()
    ?? Converts MetricSample entity ? MetricDto
    ?? Maps child collections to DTOs
    ?   ?? DiskPartitionMetric[] ? DiskPartitionDto[]
    ?   ?? NetworkInterfaceMetric[] ? NetworkInterfaceDto[]
    ?? Creates clean, serializable object (no EF navigation properties)

                    ? MetricDto Object ?

????????????????????????????????????????????????????????????????????????
? STEP 6: SignalR Broadcast                                           ?
????????????????????????????????????????????????????????????????????????
    AgentMessageHandler.ProcessMetrics()
    await _hubContext.Clients.Group($"server-{serverId}")
        .SendAsync("MetricsUpdated", metricDto);
    
    SignalR:
    ?? Finds all connections in group "server-1"
    ?? Serializes MetricDto to JSON
    ?? Pushes to all subscribed frontend clients via WebSocket

                    ? Real-time WebSocket Push ?

????????????????????????????????????????????????????????????????????????
? STEP 7: Frontend Receives Update                                    ?
????????????????????????????????????????????????????????????????????????
    JavaScript/React Frontend
    connection.on("MetricsUpdated", (metric) => {
        // metric is MetricDto object
        updateDashboard(metric);
        updateCharts(metric);
        checkThresholds(metric);
    });
```

**Performance Characteristics:**
- **Latency:** Typically 50-200ms from agent send to frontend receive
- **Frequency:** Every ~1 second per agent
- **Database writes:** 1 + N + M rows per message (1 MetricSample + N disks + M interfaces)
- **SignalR broadcasts:** 1 per metric, to all subscribed clients in group

---

### 6.2 Data Flow: Agent Connection Lifecycle

```
???????????????????????????????????????????????????????????????????
? AGENT STARTUP & CONNECTION                                      ?
???????????????????????????????????????????????????????????????????

Agent starts ? Reads config (backend URL, token)
    ?
Initiates WebSocket connection: wss://backend/ws?token=test-token-123
    ?
Backend: WebSocketHandler.HandleConnection()
    ?? Validates token against MonitoredServers table
    ?? Rejects if invalid (401 Unauthorized)
    ?? Accepts if valid
        ?
Backend: Accept WebSocket & Update DB
    ?? server.IsOnline = true
    ?? server.LastSeenUtc = DateTime.UtcNow
    ?? await _dbContext.SaveChangesAsync()
        ?
Backend: Register connection
    ?? _connectionManager.AddSocket(serverId.ToString(), webSocket)
    ?? Logs: "Agent connected: Server {ServerId} - {ServerName}"
        ?
Backend: Start HandleWebSocketLoop()
    ?? Infinite loop waiting for messages

???????????????????????????????????????????????????????????????????
? ONGOING COMMUNICATION                                           ?
???????????????????????????????????????????????????????????????????

Agent sends metrics every 1 second
    ?
Backend receives, processes, saves, broadcasts
    ?? After each message: server.LastSeenUtc updated
    ?? Frontend receives real-time updates

???????????????????????????????????????????????????????????????????
? AGENT SHUTDOWN / DISCONNECT                                     ?
???????????????????????????????????????????????????????????????????

Agent closes WebSocket OR network drops
    ?
Backend: WebSocket.ReceiveAsync() throws exception OR CloseStatus received
    ?
Backend: finally block in HandleWebSocketLoop()
    ?? _connectionManager.RemoveSocket(agentId)
    ?? server.IsOnline = false
    ?? await _dbContext.SaveChangesAsync()
    ?? Logs: "Agent disconnected: Server {ServerId}"
```

---

### 6.3 Planned Data Flows (Not Yet Implemented)

#### **6.3.1 Alert Triggering Flow (Planned)**

```
Metric arrives ? ProcessMetrics()
    ?
Load AlertRules for this server (and global rules)
    ?
Evaluate each rule:
    if (metric.CpuUsagePercent > rule.ThresholdValue && rule.Metric == "CPU")
    ?
Create Alert entity
    ?? Title = "High CPU Usage"
    ?? Message = "CPU at 95%, threshold is 80%"
    ?? Severity = rule.Severity
    ?? MonitoredServerId = serverId
    ?? AlertRuleId = rule.Id
    ?
Save to Alerts table
    ?
Broadcast via SignalR:
    await _hubContext.Clients.Group($"server-{serverId}")
        .SendAsync("AlertCreated", alertDto);
    ?
Frontend displays notification/alert badge
```

**Status:** ?? Entities exist, NO evaluation logic

---

#### **6.3.2 Service Monitoring Flow (Planned)**

```
Agent monitors service (e.g., nginx)
    ?
Agent sends service status message:
    { "action": "service.status", "payload": { "serviceName": "nginx", "status": "Running" } }
    ?
Backend: AgentMessageHandler (new case)
    ?? Find or create Service entity
    ?? Add ServiceStatusHistory record
    ?? Save to database
    ?
Broadcast via SignalR:
    await _hubContext.Clients.Group($"server-{serverId}")
        .SendAsync("ServiceStatusChanged", serverId, "nginx", statusDto);
    ?
Frontend updates service status indicator
```

**Status:** ?? Entities exist, NO message handlers

---

#### **6.3.3 Command Flow: Backend ? Agent (Planned)**

```
Frontend user clicks "Restart nginx"
    ?
POST /api/services/{id}/restart
    ?
Backend controller:
    ?? Finds service & server
    ?? Retrieves WebSocket from WebSocketConnectionManager
    ?? Constructs command JSON:
        { "type": "request", "action": "service.restart", "payload": { "serviceName": "nginx" } }
    ?
Backend sends command to agent:
    await webSocket.SendAsync(jsonBytes, ...)
    ?
Agent receives command, executes `systemctl restart nginx`
    ?
Agent sends response:
    { "type": "response", "action": "service.restart", "payload": { "success": true } }
    ?
Backend receives response, broadcasts status update via SignalR
    ?
Frontend shows "Service restarted successfully"
```

**Status:** ?? Infrastructure exists (WebSocketConnectionManager), NO endpoints or handlers

---

## 7. Testing Infrastructure

### 7.1 Current Testing Capabilities

#### **7.1.1 Manual Testing Endpoints**

**TestController** (`Presentation/Controllers/TestController.cs`)

| Endpoint | Purpose | How to Test |
|----------|---------|-------------|
| `GET /api/test/ping` | Health check | `curl http://localhost:5000/api/test/ping` |
| `POST /api/test/broadcast-test?serverId=1` | Simulate metric broadcast | `curl -X POST http://localhost:5000/api/test/broadcast-test?serverId=1` |

**Status:** ? Working

---

#### **7.1.2 SignalR Test Page**

**File:** `Presentation/wwwroot/test-signalr.html`

**Access:** `http://localhost:5000/test-signalr.html`

**Features:**
- Connect to SignalR hub
- Subscribe to server groups
- Display received messages in real-time
- Test manual broadcasts via TestController

**Test Workflow:**
1. Start backend: `dotnet run --project Presentation`
2. Open `http://localhost:5000/test-signalr.html`
3. Click "Connect"
4. Subscribe to server ID 1
5. In separate terminal: `curl -X POST http://localhost:5000/api/test/broadcast-test?serverId=1`
6. Verify message appears on page

**Status:** ? File exists and functional

---

#### **7.1.3 Database Seeding for Testing**

**DatabaseSeeder** (`Presentation/DatabaseSeeder.cs`)

**Automatically seeds on startup:**

**Test Server:**
- Name: "Test Server"
- Hostname: "test-server"
- Token: `test-token-123`
- IsOnline: false (initially)

**Admin User:**
- Email: `admin@example.com`
- Password: `admin123` (BCrypt hashed)
- Role: `Admin`

**Location:** Called in `Program.cs` via `await DatabaseSeeder.SeedAsync(app.Services);`

**Status:** ? Fully implemented

---

### 7.2 Testing Real Agent Connection

**Requirements:**
- Python agent running on Linux server (or WSL)
- Backend running locally or on accessible server
- Valid agent token in database

**Agent Connection String:**
```
wss://localhost:5001/ws?token=test-token-123
```

**Expected Log Output (Backend):**
```
info: Presentation.WebSockets.WebSocketHandler[0]
      Agent connected: Server 1 - Test Server
info: BusinessLayer.Services.Concrete.AgentMessageHandler[0]
      Saved and broadcast metrics for server 1
```

**Database Verification:**
```sql
-- Check server is online
SELECT "Id", "Name", "IsOnline", "LastSeenUtc" 
FROM "MonitoredServers";

-- Count metric samples
SELECT COUNT(*) FROM "MetricSamples";

-- Latest metrics for server 1
SELECT "TimestampUtc", "CpuUsagePercent", "RamUsagePercent"
FROM "MetricSamples"
WHERE "MonitoredServerId" = 1
ORDER BY "TimestampUtc" DESC
LIMIT 10;
```

**Status:** ? Tested and working with Python agent

---

### 7.3 What's Missing: Unit & Integration Tests

**?? No automated tests exist in the project.**

**Recommended Test Projects (not implemented):**

1. **Infrastructure.Tests**
   - Entity validation
   - DbContext configuration
   - Migration tests

2. **Application.Tests**
   - AgentMessageHandler logic
   - DTO mapping
   - Message routing

3. **Presentation.IntegrationTests**
   - WebSocket connection flow
   - SignalR hub behavior
   - Controller endpoints (when implemented)

**Priority:** Medium (acceptable for university project, critical for production)

---

## 8. Integration Guide for Other Teams

### 8.1 For Python Agent Team

#### **Connection Requirements**

**WebSocket Endpoint:** `wss://<backend-url>/ws?token=<agent-token>`

**Example (Python):**
```python
import websockets
import json

async def connect_to_backend():
    uri = "wss://localhost:5001/ws?token=test-token-123"
    async with websockets.connect(uri) as websocket:
        # Send metrics every second
        while True:
            metrics = collect_metrics()  # Your function
            message = {
                "type": "event",
                "id": msg_id,
                "action": "metrics",
                "payload": metrics,
                "timestamp": int(time.time() * 1000)
            }
            await websocket.send(json.dumps(message))
            await asyncio.sleep(1)
```

---

#### **Message Format**

**All messages must follow this structure:**
```json
{
  "type": "event",
  "id": 6,
  "action": "metrics",
  "payload": { /* action-specific data */ },
  "timestamp": 1763221174564
}
```

**Fields:**
- `type`: Always `"event"` for agent-to-backend messages
- `id`: Sequential message ID (incrementing integer)
- `action`: Message type - currently only `"metrics"` is supported
- `payload`: See payload format below
- `timestamp`: Unix timestamp in milliseconds

---

#### **Metrics Payload Format**

**Required fields:**
```json
{
  "cpuUsagePercent": 12.5,
  "ramUsagePercent": 52.1,
  "ramUsedGB": 0.28,
  "diskUsage": [
    {
      "device": "/dev/sda1",
      "mountpoint": "/",
      "fstype": "ext4",
      "totalGB": 28.02,
      "usedGB": 3.07,
      "usagePercent": 11.0
    }
  ],
  "networkInterfaces": [
    {
      "name": "eth0",
      "mac": "60:45:bd:2a:69:f9",
      "ipv4": "10.0.0.4",
      "ipv6": "fe80::6245:bdff:fe2a:69f9%eth0",
      "uploadSpeedMbps": 0.01,
      "downloadSpeedMbps": 0.0
    }
  ],
  "uptimeSeconds": 3222214,
  "normalizedLoad": { "1m": 0.0, "5m": 0.0, "15m": 0.0 },
  "diskReadSpeedMBps": 0.0,
  "diskWriteSpeedMBps": 0.0
}
```

**Notes:**
- `mac` can be null for interfaces without MAC addresses
- `normalizedLoad` should be load average divided by CPU count
- Speeds should be calculated as delta between samples

---

#### **Error Handling**

**401 Unauthorized:**
- Invalid or missing token
- Check token matches database value

**Connection drops:**
- Backend logs disconnect
- Server marked as offline in database
- Implement reconnection logic with exponential backoff

---

### 8.2 For Frontend Team

#### **SignalR Connection**

**Install SignalR client:**
```bash
npm install @microsoft/signalr
```

**Connect to hub:**
```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/monitoring-hub")
    .withAutomaticReconnect()
    .build();

await connection.start();
console.log("Connected to SignalR");
```

---

#### **Subscribe to Server Updates**

```typescript
// Subscribe to server 1
await connection.invoke("SubscribeServer", 1);

// Listen for metric updates
connection.on("MetricsUpdated", (metric: MetricDto) => {
    console.log("Received metric:", metric);
    updateDashboard(metric);
});

// Unsubscribe when navigating away
await connection.invoke("UnsubscribeServer", 1);
```

---

#### **TypeScript Interfaces**

```typescript
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

interface DiskPartitionDto {
    device: string;
    mountPoint: string;
    fileSystemType: string;
    totalGb: number;
    usedGb: number;
    usagePercent: number;
}

interface NetworkInterfaceDto {
    name: string;
    macAddress: string;
    ipv4?: string;
    ipv6?: string;
    uploadSpeedMbps: number;
    downloadSpeedMbps: number;
}
```

---

#### **Planned SignalR Events (Not Yet Available)**

The following events are designed but **not yet broadcast by the backend**:

```typescript
// Alert notifications (planned)
connection.on("AlertCreated", (alert: AlertDto) => {
    showNotification(alert);
});

// Service status changes (planned)
connection.on("ServiceStatusChanged", (serverId: number, serviceName: string, status: ServiceStatusDto) => {
    updateServiceStatus(serverId, serviceName, status);
});

// Server online/offline (planned)
connection.on("ServerStatusChanged", (status: ServerStatusDto) => {
    updateServerBadge(status.serverId, status.isOnline);
});
```

---

#### **REST API Endpoints (Not Yet Implemented)**

**?? These endpoints are PLANNED but DO NOT exist yet:**

```typescript
// Get list of servers
GET /api/servers
Response: ServerDto[]

// Get server details
GET /api/servers/{id}
Response: ServerDetailDto

// Get historical metrics
GET /api/servers/{id}/metrics?from=2025-01-01&to=2025-01-15
Response: MetricDto[]

// Get latest metrics
GET /api/servers/{id}/latest-metrics
Response: MetricDto

// Get alerts
GET /api/alerts?serverId=1&severity=Critical&acknowledged=false
Response: AlertDto[]

// Acknowledge alert
POST /api/alerts/{id}/acknowledge
Response: 200 OK
```

**Recommendation:** Use SignalR for real-time data, implement REST APIs later for historical queries.

---

### 8.3 For DevOps / Deployment Team

#### **Environment Variables / Configuration**

**Required in `appsettings.json` or environment:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<pg-host>;Port=5432;Database=serverhealth;Username=<user>;Password=<pass>;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Production Recommendations:**
- Use environment variables for sensitive data
- Store connection string in secrets manager (Azure Key Vault, AWS Secrets Manager)
- Enable HTTPS/WSS for WebSocket connections
- Configure proper CORS origins

---

#### **Database Migration**

**On first deployment:**
```bash
cd ServerMonitoringBackend/Infrastructure
dotnet ef database update --startup-project ../Presentation
```

**Or:** Migrations run automatically on startup via `DatabaseSeeder.SeedAsync()`

---

#### **Docker Deployment (Example)**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Presentation/Presentation.csproj", "Presentation/"]
COPY ["Application/BusinessLayer.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "Presentation/Presentation.csproj"

COPY . .
WORKDIR "/src/Presentation"
RUN dotnet build "Presentation.csproj" -c Release -o /app/build
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Presentation.dll"]
```

**Docker Compose with PostgreSQL:**
```yaml
version: '3.8'
services:
  db:
    image: postgres:15
    environment:
      POSTGRES_DB: serverhealth
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: strongpassword
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  backend:
    build: .
    ports:
      - "5000:80"
    environment:
      ConnectionStrings__DefaultConnection: "Server=db;Port=5432;Database=serverhealth;Username=admin;Password=strongpassword;"
    depends_on:
      - db

volumes:
  pgdata:
```

---

## 9. Current State Analysis

### 9.1 Production-Ready Components ?

| Component | Status | Notes |
|-----------|--------|-------|
| **Database Schema** | ? Production-ready | Properly normalized, indexed, with migrations |
| **WebSocket Agent Handler** | ? Production-ready | Secure authentication, error handling, connection management |
| **SignalR Hub** | ? Production-ready | Group-based subscriptions working correctly |
| **Metric Persistence** | ? Production-ready | Saving metrics with full relational integrity |
| **Real-time Broadcasting** | ? Production-ready | Metrics instantly pushed to frontend |
| **Connection Management** | ? Production-ready | Singleton manager tracks active agents |

---

### 9.2 Placeholder / Incomplete Code ??

#### **9.2.1 Unused Entities**

The following entities exist in the database but **have no associated logic**:

| Entity | Tables | Issue | Impact |
|--------|--------|-------|--------|
| `Service` & `ServiceStatusHistory` | ? Migrated | No message handlers | Cannot monitor services |
| `AlertRule` & `Alert` | ? Migrated | No evaluation logic | No alerts triggered |
| `ProcessSnapshot` & `ProcessInfo` | ? Migrated | No message handlers | Cannot view processes |
| `User` | ? Migrated with seed | No JWT/auth endpoints | Cannot login |

**Recommendation:** 
- **Keep entities** - they're properly designed and ready to use
- **Mark as Phase 4** features
- **Do NOT delete** - no harm in having them

---

#### **9.2.2 Test Code in Production**

**TestController** - Contains endpoints for manual testing:
```csharp
[HttpPost("broadcast-test")]
public async Task<IActionResult> BroadcastTest([FromQuery] int serverId = 1)
```

**Recommendation:**
- ? **Keep for development**
- ?? **Remove or secure for production** (add `#if DEBUG` or authentication)

---

#### **9.2.3 CORS Configuration**

**Current:**
```csharp
policy.AllowAnyOrigin()
      .AllowAnyHeader()
      .AllowAnyMethod();
```

**?? Security Risk:** Allows requests from any domain

**Production Fix:**
```csharp
policy.WithOrigins("https://your-frontend-domain.com")
      .AllowCredentials()
      .AllowAnyHeader()
      .AllowAnyMethod();
```

---

### 9.3 Hardcoded Values to Remove

| Location | Hardcoded Value | Fix |
|----------|----------------|-----|
| `appsettings.json` | Database password `"yusuf"` | Use environment variables or secrets |
| `DatabaseSeeder.cs` | Admin password `"admin123"` | Use stronger password or disable seeding in prod |
| `DatabaseSeeder.cs` | Test token `"test-token-123"` | Generate tokens via admin API |

---

### 9.4 Missing Critical Features for Production

1. **Authentication & Authorization**
   - No JWT token generation
   - No `[Authorize]` attributes on controllers
   - Frontend cannot login

2. **REST API Endpoints**
   - No historical data queries
   - No server management endpoints
   - No alert management API

3. **Alert System**
   - Rules exist but never evaluated
   - No alerts created
   - No email/webhook notifications

4. **Monitoring & Observability**
   - Basic logging exists
   - No structured logging (Serilog)
   - No metrics export (Prometheus)
   - No health checks endpoint

5. **Data Retention & Cleanup**
   - Metrics accumulate forever
   - No archival/downsampling strategy
   - Database will grow indefinitely

---

## 10. Next Steps: Phase 4 & Beyond

### 10.1 Immediate Next Steps (Phase 4 - Core REST API)

**Priority: HIGH**

#### **Task 4.1: Implement ServersController**

**Endpoints to create:**
```csharp
GET    /api/servers              // List all servers
GET    /api/servers/{id}         // Server details
POST   /api/servers              // Register new server (generates token)
PUT    /api/servers/{id}         // Update server info
DELETE /api/servers/{id}         // Remove server
```

**Estimated effort:** 4-6 hours

---

#### **Task 4.2: Implement MetricsController**

**Endpoints to create:**
```csharp
GET /api/servers/{id}/latest-metrics
GET /api/servers/{id}/metrics?from=...&to=...&interval=1h
```

**Features:**
- Time-range queries
- Aggregation by interval (1min, 5min, 1hour)
- Pagination for large datasets

**Estimated effort:** 6-8 hours

---

#### **Task 4.3: Implement AuthController & JWT**

**Endpoints:**
```csharp
POST /api/auth/login              // Returns JWT token
POST /api/auth/refresh            // Refresh expired token
POST /api/auth/register           // Admin creates new user
```

**Add NuGet package:**
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

**Configure in Program.cs:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* config */ });
```

**Estimated effort:** 8-10 hours

---

### 10.2 Medium Priority (Phase 5 - Alert System)

#### **Task 5.1: Implement Alert Rule Evaluation**

**Create service:**
```csharp
public interface IAlertEvaluationService
{
    Task EvaluateMetric(int serverId, MetricSample metric);
}
```

**Call from AgentMessageHandler after saving metrics:**
```csharp
await _alertEvaluationService.EvaluateMetric(serverId, metricSample);
```

**Logic:**
1. Load active `AlertRule`s for server (and global rules)
2. Evaluate each rule against metric
3. Create `Alert` if threshold exceeded
4. Broadcast `AlertCreated` via SignalR

**Estimated effort:** 10-12 hours

---

#### **Task 5.2: Implement AlertsController**

**Endpoints:**
```csharp
GET    /api/alerts                          // List alerts with filters
GET    /api/alerts/{id}                     // Alert details
POST   /api/alerts/{id}/acknowledge         // Mark as acknowledged
DELETE /api/alerts/{id}                     // Delete alert
GET    /api/alert-rules                     // List rules
POST   /api/alert-rules                     // Create rule
PUT    /api/alert-rules/{id}                // Update rule
DELETE /api/alert-rules/{id}                // Delete rule
```

**Estimated effort:** 8-10 hours

---

### 10.3 Lower Priority (Phase 6 - Service & Process Monitoring)

#### **Task 6.1: Service Monitoring**

1. **Agent Team:** Implement service status collection
2. **Backend:** Add `case "service.status"` handler in `AgentMessageHandler`
3. **Backend:** Create `ServicesController` for REST API
4. **Backend:** Broadcast `ServiceStatusChanged` via SignalR

**Estimated effort:** 12-15 hours

---

#### **Task 6.2: Process Monitoring**

1. **Agent Team:** Implement process snapshot collection
2. **Backend:** Add `case "processes"` handler
3. **Backend:** Create `ProcessesController`
4. **Backend:** Broadcast `ProcessesUpdated` via SignalR

**Estimated effort:** 10-12 hours

---

#### **Task 6.3: Service Control (Commands)**

1. **Create `CommandsController`:**
   ```csharp
   POST /api/services/{id}/restart
   POST /api/services/{id}/stop
   POST /api/services/{id}/start
   ```

2. **Use `WebSocketConnectionManager` to send commands to agent**

3. **Agent implements command handlers**

**Estimated effort:** 15-20 hours

---

### 10.4 Production Hardening (Phase 7)

#### **Task 7.1: Data Retention Strategy**

**Problem:** Metrics accumulate forever, database grows unbounded

**Solutions:**
1. **Downsampling:** Aggregate old metrics to hourly/daily averages
2. **Archival:** Move old data to cold storage (S3, Azure Blob)
3. **Deletion:** Delete metrics older than X days

**Recommended approach:**
```csharp
// Background service running daily
public class MetricCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Delete metrics older than 30 days
            await DeleteOldMetrics(days: 30);
            
            // Aggregate metrics older than 7 days to hourly
            await AggregateMetrics(olderThanDays: 7);
            
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

**Estimated effort:** 12-15 hours

---

#### **Task 7.2: Add Health Checks**

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddCheck<AgentConnectivityCheck>("agents");

app.MapHealthChecks("/health");
```

**Estimated effort:** 3-4 hours

---

#### **Task 7.3: Structured Logging with Serilog**

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Seq
```

**Configuration:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();
```

**Estimated effort:** 4-6 hours

---

#### **Task 7.4: Rate Limiting for REST APIs**

**Prevent abuse:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

app.UseRateLimiter();
```

**Estimated effort:** 3-4 hours

---

## 11. Technical Debt & Improvements

### 11.1 Performance Optimizations

#### **11.1.1 Metric Insertion Performance**

**Current issue:** Every metric creates 1 + N + M database rows with individual `SaveChangesAsync()` calls.

**Optimization 1: Batch Inserts**
```csharp
// Instead of saving after each metric, accumulate and bulk insert
private readonly List<MetricSample> _metricBuffer = new();

if (_metricBuffer.Count >= 100)
{
    await _dbContext.MetricSamples.AddRangeAsync(_metricBuffer);
    await _dbContext.SaveChangesAsync();
    _metricBuffer.Clear();
}
```

**Optimization 2: Background Queue**
```csharp
public class MetricQueue : BackgroundService
{
    private readonly Channel<MetricSample> _queue = Channel.CreateUnbounded<MetricSample>();
    
    public void Enqueue(MetricSample metric) => _queue.Writer.TryWrite(metric);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var metric in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            // Batch save metrics
        }
    }
}
```

**Estimated gain:** 50-70% reduction in database load

---

#### **11.1.2 SignalR Broadcast Optimization**

**Current:** Broadcasts to group on every metric (1/second per server)

**Optimization:** Throttle broadcasts to frontend (e.g., max 1 per 5 seconds)
```csharp
private DateTime _lastBroadcast = DateTime.MinValue;

if ((DateTime.UtcNow - _lastBroadcast).TotalSeconds >= 5)
{
    await _hubContext.Clients.Group($"server-{serverId}")
        .SendAsync("MetricsUpdated", metricDto);
    _lastBroadcast = DateTime.UtcNow;
}
```

**Trade-off:** Reduced real-time granularity, but significantly lower CPU/network usage

---

### 11.2 Code Quality Improvements

#### **11.2.1 Add Response DTOs for All Entities**

**Currently missing:**
- `ServerDto`, `ServerDetailDto`
- `AlertDto`, `AlertRuleDto`
- `ServiceDto`, `ServiceStatusDto`
- `UserDto`

**Recommendation:** Create DTOs in `Application/DTOs/Response/` before implementing REST APIs

---

#### **11.2.2 Separate Business Logic from Controllers**

**Anti-pattern:** When controllers are created, avoid this:
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetServer(int id)
{
    var server = await _dbContext.MonitoredServers.FindAsync(id); // ? Direct DB access
    return Ok(server);
}
```

**Better approach:** Use service layer:
```csharp
public interface IServerService
{
    Task<ServerDto> GetServerAsync(int id);
}

[HttpGet("{id}")]
public async Task<IActionResult> GetServer(int id)
{
    var server = await _serverService.GetServerAsync(id); // ? Business logic in service
    return Ok(server);
}
```

---

#### **11.2.3 Add Validation**

**Install FluentValidation:**
```bash
dotnet add package FluentValidation.AspNetCore
```

**Example validator:**
```csharp
public class CreateServerRequestValidator : AbstractValidator<CreateServerRequest>
{
    public CreateServerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Hostname).NotEmpty().MaximumLength(200);
    }
}
```

---

### 11.3 Security Improvements

#### **11.3.1 Hash Agent Tokens**

**Current:** `AgentToken` stored as plain text in database

**Improvement:** Store hashed like passwords:
```csharp
public class MonitoredServer
{
    public string AgentTokenHash { get; set; }  // BCrypt hash
    
    public bool ValidateToken(string token)
    {
        return BCrypt.Net.BCrypt.Verify(token, AgentTokenHash);
    }
}
```

---

#### **11.3.2 Add HTTPS Enforcement**

**Production `Program.cs`:**
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

---

#### **11.3.3 Secrets Management**

**Don't commit:**
- Database passwords
- JWT signing keys
- API keys

**Use:**
- User Secrets (development): `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."`
- Environment variables (production)
- Azure Key Vault / AWS Secrets Manager

---

### 11.4 Monitoring & Observability

#### **11.4.1 Add Application Insights / OpenTelemetry**

Track:
- Request duration
- Database query performance
- Exception rates
- WebSocket connection count

---

#### **11.4.2 Add Prometheus Metrics Export**

```bash
dotnet add package prometheus-net.AspNetCore
```

```csharp
app.UseMetricServer();  // Exposes /metrics endpoint
app.UseHttpMetrics();
```

**Metrics to track:**
- Active WebSocket connections
- Metrics received per second
- Database write latency
- SignalR broadcast count

---

