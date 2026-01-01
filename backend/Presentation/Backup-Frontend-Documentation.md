# Backup System - Frontend Integration Guide

**Version:** 1.0  
**Last Updated:** December 2025  
**Backend:** .NET 8 / ASP.NET Core  
**Frontend:** React.js  
**Protocol:** REST API + SignalR WebSockets

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Backup Job Management](#backup-job-management)
4. [Real-Time Backup Events](#real-time-backup-events)
5. [Complete React Example](#complete-react-example)

---

## Overview

The backup system allows administrators to:
- Create scheduled backups from monitored servers to remote SFTP repositories
- Manually trigger backups on demand
- View backup execution logs and history
- List backup snapshots from Restic repositories

**Key Features:**
- Credentials (passwords, SSH keys) are encrypted in database and never returned in responses
- Cron-based scheduling with automatic execution
- Real-time backup status updates via SignalR
- Manual trigger capability for immediate backups

---

## Authentication

All backup endpoints require JWT Bearer authentication.

### Get JWT Token

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
const response = await fetch('https://your-backend.com/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ 
    email: 'admin@example.com', 
    password: 'admin123' 
  })
});
const { token } = await response.json();
localStorage.setItem('authToken', token);
```

---

## Backup Job Management

### Create Backup Job

**`POST /api/backups`**

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:**
```typescript
interface CreateBackupJobRequest {
  agentId: number;           // ID of the monitored server
  name: string;              // Friendly name (e.g., "Web Server Daily Backup")
  sourcePath: string;        // Local path on agent (e.g., "/var/www/html")
  repoUrl: string;           // SFTP destination (format: "sftp:user@host:/path")
  repoPassword: string;      // Restic repository password
  sshPrivateKey: string;     // SSH private key for SFTP authentication
  scheduleCron: string;      // Cron expression (e.g., "0 2 * * *" = daily at 2 AM)
  isActive: boolean;         // Enable/disable the job
}
```

**Example Request:**
```json
{
  "agentId": 1,
  "name": "Production Database Backup",
  "sourcePath": "/var/lib/postgresql/data",
  "repoUrl": "sftp:backup@backup-server.com:/backups/db",
  "repoPassword": "my-secure-password",
  "sshPrivateKey": "-----BEGIN OPENSSH PRIVATE KEY-----\nMIIEowIBAAKCAQEA...\n-----END OPENSSH PRIVATE KEY-----",
  "scheduleCron": "0 2 * * *",
  "isActive": true
}
```

**Response (201 Created):**
```typescript
interface BackupJobDto {
  id: string;                // GUID
  agentId: number;
  agentName: string;         // Friendly name of the monitored server
  name: string;
  sourcePath: string;
  repoUrl: string;
  scheduleCron: string;
  isActive: boolean;
  lastRunStatus: string;     // "pending", "success", or "error"
  lastRunAtUtc: string | null;  // ISO 8601
  createdAtUtc: string;      // ISO 8601
  updatedAtUtc: string;      // ISO 8601
}
```

**Example Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "agentId": 1,
  "agentName": "Production Server",
  "name": "Production Database Backup",
  "sourcePath": "/var/lib/postgresql/data",
  "repoUrl": "sftp:backup@backup-server.com:/backups/db",
  "scheduleCron": "0 2 * * *",
  "isActive": true,
  "lastRunStatus": "pending",
  "lastRunAtUtc": null,
  "createdAtUtc": "2025-12-29T18:00:00Z",
  "updatedAtUtc": "2025-12-29T18:00:00Z"
}
```

**?? Security Note:** Passwords and SSH keys are **NOT** included in the response (they are encrypted in the database).

---

### List All Backup Jobs

**`GET /api/backups`**

**Optional Query Parameter:**
- `agentId` (number): Filter jobs by specific agent

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```typescript
BackupJobDto[]  // Array of BackupJobDto (see structure above)
```

**Example:**
```javascript
// Get all backup jobs
const response = await fetch('https://your-backend.com/api/backups', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const jobs = await response.json();

// Get jobs for specific agent
const response = await fetch('https://your-backend.com/api/backups?agentId=1', {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

---

### Get Backup Job Details

**`GET /api/backups/{id}`**

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```typescript
BackupJobDto  // Single job object
```

**Response (404 Not Found):**
```json
{ "error": "Backup job not found" }
```

---

### Update Backup Job

**`PUT /api/backups/{id}`**

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:**
```typescript
interface UpdateBackupJobRequest {
  name?: string;              // Optional: Update job name
  sourcePath?: string;        // Optional: Update source path
  repoUrl?: string;           // Optional: Update repository URL
  repoPassword?: string;      // Optional: Update password
  sshPrivateKey?: string;     // Optional: Update SSH key
  scheduleCron?: string;      // Optional: Update schedule
  isActive?: boolean;         // Optional: Enable/disable job
}
```

**Example Request:**
```json
{
  "isActive": false,
  "scheduleCron": "0 3 * * *"
}
```

**Response (200 OK):**
```typescript
BackupJobDto  // Updated job object
```

**Response (404 Not Found):**
```json
{ "error": "Backup job not found" }
```

---

### Delete Backup Job

**`DELETE /api/backups/{id}`**

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{ "message": "Backup job deleted successfully" }
```

**Response (404 Not Found):**
```json
{ "error": "Backup job not found" }
```

**?? Note:** Deleting a job also deletes all associated backup logs.

---

### Manually Trigger Backup

**`POST /api/backups/{id}/trigger`**

This endpoint immediately triggers a backup job, bypassing the schedule.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```typescript
interface TriggerResponse {
  message: string;
  jobId: string;    // GUID
  taskId: string;   // GUID - used to track this specific execution
}
```

**Example Response:**
```json
{
  "message": "Backup triggered successfully",
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "taskId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"
}
```

**Response (400 Bad Request):**
```json
{ "error": "Agent is offline" }
```

**Response (404 Not Found):**
```json
{ "error": "Backup job not found" }
```

---

### Get Backup Logs

**`GET /api/backups/{id}/logs`**

Retrieves execution history for a backup job.

**Optional Query Parameter:**
- `limit` (number): Max number of logs to return (default: 50)

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```typescript
interface BackupLogDto {
  id: string;                    // GUID (taskId)
  jobId: string;                 // GUID
  status: string;                // "pending", "success", or "error"
  snapshotId: string | null;     // Restic snapshot hash
  filesNew: number | null;       // Number of new files backed up
  dataAdded: number | null;      // Bytes added to repository
  durationSeconds: number | null; // Execution time
  errorMessage: string | null;   // Error details if status is "error"
  createdAtUtc: string;          // ISO 8601
}

// Returns: BackupLogDto[]
```

**Example Response:**
```json
[
  {
    "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "success",
    "snapshotId": "1fbf7784a3c2",
    "filesNew": 150,
    "dataAdded": 52428800,
    "durationSeconds": 3.5,
    "errorMessage": null,
    "createdAtUtc": "2025-12-29T02:00:00Z"
  },
  {
    "id": "7c2e4f6a-8b9d-4c5e-a1f2-3d4e5f6a7b8c",
    "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "error",
    "snapshotId": null,
    "filesNew": null,
    "dataAdded": null,
    "durationSeconds": null,
    "errorMessage": "Permission denied: /var/lib/postgresql/data",
    "createdAtUtc": "2025-12-28T02:00:00Z"
  }
]
```

---

### Get Backup Snapshots

**`GET /api/backups/{id}/snapshots`**

Retrieves list of successful backup snapshots.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```typescript
interface BackupSnapshotDto {
  id: string;           // Snapshot ID from Restic
  time: string;         // ISO 8601
  hostname: string;     // Agent hostname
  paths: string[];      // Backed up paths
  size: number | null;  // Snapshot size in bytes
}

// Returns: BackupSnapshotDto[]
```

**Example Response:**
```json
[
  {
    "id": "1fbf7784a3c2",
    "time": "2025-12-29T02:00:00Z",
    "hostname": "Production Server",
    "paths": ["/var/lib/postgresql/data"],
    "size": 52428800
  },
  {
    "id": "8a3c4d5e6f7g",
    "time": "2025-12-28T02:00:00Z",
    "hostname": "Production Server",
    "paths": ["/var/lib/postgresql/data"],
    "size": 51380224
  }
]
```

---

## Real-Time Backup Events

### Connect to SignalR Hub

```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://your-backend.com/monitoring-hub', {
    accessTokenFactory: () => localStorage.getItem('authToken'),
    transport: signalR.HttpTransportType.WebSockets,
    skipNegotiation: true
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .build();

await connection.start();
```

### Subscribe to Backup Completion Events

```typescript
connection.on('BackupCompleted', (event) => {
  console.log('Backup completed:', event);
  // Update UI with backup results
});
```

**Event Payload:**
```typescript
interface BackupCompletedEvent {
  serverId: number;
  jobId: string;              // GUID
  taskId: string;             // GUID
  status: string;             // "success" or "error"
  snapshotId: string | null;
  filesNew: number | null;
  dataAdded: number | null;
  durationSeconds: number | null;
  errorMessage: string | null;
  timestamp: string;          // ISO 8601
}
```

**Example Event:**
```json
{
  "serverId": 1,
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "taskId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "status": "success",
  "snapshotId": "1fbf7784a3c2",
  "filesNew": 150,
  "dataAdded": 52428800,
  "durationSeconds": 3.5,
  "errorMessage": null,
  "timestamp": "2025-12-29T02:00:35Z"
}
```

---

## Complete React Example

```tsx
import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

function BackupManagement() {
  const [jobs, setJobs] = useState([]);
  const [logs, setLogs] = useState({});
  const [connection, setConnection] = useState(null);

  // Initialize SignalR connection
  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://your-backend.com/monitoring-hub', {
        accessTokenFactory: () => localStorage.getItem('authToken')
      })
      .withAutomaticReconnect()
      .build();

    newConnection.start()
      .then(() => {
        console.log('SignalR connected');
        
        // Listen for backup completion events
        newConnection.on('BackupCompleted', (event) => {
          console.log('Backup completed:', event);
          loadJobs(); // Refresh job list
          loadLogs(event.jobId); // Refresh logs for this job
        });
      })
      .catch(err => console.error('SignalR connection error:', err));

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, []);

  // Load all backup jobs
  const loadJobs = async () => {
    const token = localStorage.getItem('authToken');
    const response = await fetch('https://your-backend.com/api/backups', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setJobs(data);
  };

  // Load logs for a specific job
  const loadLogs = async (jobId) => {
    const token = localStorage.getItem('authToken');
    const response = await fetch(`https://your-backend.com/api/backups/${jobId}/logs`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setLogs(prev => ({ ...prev, [jobId]: data }));
  };

  // Create new backup job
  const createJob = async (jobData) => {
    const token = localStorage.getItem('authToken');
    const response = await fetch('https://your-backend.com/api/backups', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(jobData)
    });
    
    if (response.ok) {
      const newJob = await response.json();
      loadJobs(); // Refresh list
      return newJob;
    } else {
      const error = await response.json();
      throw new Error(error.error || 'Failed to create backup job');
    }
  };

  // Manually trigger backup
  const triggerBackup = async (jobId) => {
    const token = localStorage.getItem('authToken');
    const response = await fetch(`https://your-backend.com/api/backups/${jobId}/trigger`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    
    if (response.ok) {
      const result = await response.json();
      alert(`Backup triggered! Task ID: ${result.taskId}`);
    } else {
      const error = await response.json();
      alert(`Failed to trigger backup: ${error.error}`);
    }
  };

  // Toggle job active status
  const toggleJobStatus = async (jobId, isActive) => {
    const token = localStorage.getItem('authToken');
    await fetch(`https://your-backend.com/api/backups/${jobId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ isActive: !isActive })
    });
    loadJobs();
  };

  // Delete job
  const deleteJob = async (jobId) => {
    if (!confirm('Are you sure you want to delete this backup job?')) return;
    
    const token = localStorage.getItem('authToken');
    await fetch(`https://your-backend.com/api/backups/${jobId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    loadJobs();
  };

  useEffect(() => {
    loadJobs();
  }, []);

  return (
    <div>
      <h2>Backup Jobs</h2>
      
      {/* Job List */}
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Agent</th>
            <th>Schedule</th>
            <th>Status</th>
            <th>Last Run</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {jobs.map(job => (
            <tr key={job.id}>
              <td>{job.name}</td>
              <td>{job.agentName}</td>
              <td>{job.scheduleCron}</td>
              <td>
                <span className={`status-${job.lastRunStatus}`}>
                  {job.lastRunStatus}
                </span>
              </td>
              <td>
                {job.lastRunAtUtc 
                  ? new Date(job.lastRunAtUtc).toLocaleString() 
                  : 'Never'}
              </td>
              <td>
                <button onClick={() => triggerBackup(job.id)}>
                  Trigger Now
                </button>
                <button onClick={() => toggleJobStatus(job.id, job.isActive)}>
                  {job.isActive ? 'Disable' : 'Enable'}
                </button>
                <button onClick={() => loadLogs(job.id)}>
                  View Logs
                </button>
                <button onClick={() => deleteJob(job.id)}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Logs Modal */}
      {Object.entries(logs).map(([jobId, jobLogs]) => (
        <div key={jobId} className="logs-modal">
          <h3>Backup Logs</h3>
          <table>
            <thead>
              <tr>
                <th>Time</th>
                <th>Status</th>
                <th>Files</th>
                <th>Size</th>
                <th>Duration</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {jobLogs.map(log => (
                <tr key={log.id}>
                  <td>{new Date(log.createdAtUtc).toLocaleString()}</td>
                  <td>{log.status}</td>
                  <td>{log.filesNew || '-'}</td>
                  <td>{log.dataAdded ? `${(log.dataAdded / 1024 / 1024).toFixed(2)} MB` : '-'}</td>
                  <td>{log.durationSeconds ? `${log.durationSeconds.toFixed(1)}s` : '-'}</td>
                  <td>{log.errorMessage || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </div>
  );
}

export default BackupManagement;
```

---

## Common Cron Expressions

| Expression | Description |
|------------|-------------|
| `0 2 * * *` | Daily at 2:00 AM |
| `0 */6 * * *` | Every 6 hours |
| `*/30 * * * *` | Every 30 minutes |
| `0 0 * * 0` | Every Sunday at midnight |
| `0 3 * * 1-5` | Weekdays at 3:00 AM |
| `0 0 1 * *` | First day of every month |

Test your expressions at: https://crontab.guru/

---

## Error Handling

All endpoints may return these error responses:

**401 Unauthorized:**
```json
{ "error": "Unauthorized" }
```
*Solution:* Check if JWT token is valid and not expired.

**400 Bad Request:**
```json
{ "error": "Agent is offline" }
```
*Solution:* Ensure the agent is connected before triggering backups.

**404 Not Found:**
```json
{ "error": "Backup job not found" }
```
*Solution:* Verify the job ID exists.

**500 Internal Server Error:**
```json
{ "error": "Internal server error" }
```
*Solution:* Check backend logs for details.

---

## Security Notes

1. **Never display credentials:** Passwords and SSH keys are encrypted and never returned by the API
2. **Token expiration:** JWT tokens expire after 4 hours - implement token refresh logic
3. **HTTPS only:** Always use HTTPS in production
4. **Validate input:** Sanitize all user inputs before sending to backend
5. **SSH key format:** Ensure SSH keys include proper headers (`-----BEGIN...-----END...`)

---

**End of Documentation**
