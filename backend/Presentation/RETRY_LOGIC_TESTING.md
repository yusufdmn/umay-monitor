# Retry Logic Implementation - Testing Guide

## Implementation Summary ?

All retry logic and SignalR events have been successfully implemented in the backend.

### What Was Implemented:

#### 1. **Request Tracking & Retry State** ?
- Added `RetryCount`, `LastRetryTime`, and `Payload` to `PendingRequest` class
- Stores original payload for retries
- Tracks retry attempts (max 3)

#### 2. **Background Monitoring Task** ?
- Runs every 1 second checking pending requests
- Identifies requests needing retry (>5s elapsed, <3 retries)
- Identifies failed requests (>=3 retries)
- Automatic cleanup of failed requests

#### 3. **Retry Mechanism** ?
- Resends request with **same message ID** (agent will use cache)
- 5-second interval between retries
- Max 3 retry attempts
- Logs each retry attempt

#### 4. **SignalR Events** ?
- `CommandSuccess`: Emitted when agent responds successfully
- `CommandFailed`: Emitted when max retries exceeded
- Events include serverId, action, messageId, message, timestamp

#### 5. **Agent-Side Caching** ? (Already implemented)
- Agent caches responses by message ID
- Prevents double-execution on retries
- Returns cached response for same ID

---

## Testing Plan

### Test 1: Normal Operation (No Retry Needed) ?

**Scenario**: Agent responds within 5 seconds

**Steps:**
1. Start backend and agent
2. Send a command (e.g., `get-server-info`)
3. Agent responds immediately

**Expected Result:**
```
? Request sent (MessageId=1)
? Agent receives and processes
? Response received within 5s
? CommandSuccess SignalR event emitted
? No retries triggered
```

**Verification:**
- Check backend logs: `"Command sent successfully"`
- Check backend logs: `"Successfully matched response to pending request"`
- Check SignalR: `CommandSuccess` event received by frontend
- RetryCount should be 0

---

### Test 2: Single Retry (Agent Delayed Response) ??

**Scenario**: Agent responds after 7 seconds (1 retry needed)

**Steps:**
1. Simulate network delay or agent processing delay
2. Send command at T=0s
3. Agent receives but delays response until T=7s

**Expected Result:**
```
T=0s:  ? Request sent (MessageId=1)
T=5s:  ?? Retry #1 triggered (same MessageId=1)
T=7s:  ? Agent responds (uses cache if already processed)
T=7s:  ? CommandSuccess emitted
```

**Verification:**
- Check backend logs: `"Retrying request 1 (attempt 1/3)"`
- Check backend logs: `"?? Resending request"`
- Check agent logs: `"[5] Message ID 1 found in cache"` (on retry)
- RetryCount should be 1 when response arrives

---

### Test 3: Multiple Retries Then Success ??

**Scenario**: Agent responds after 12 seconds (2 retries needed)

**Steps:**
1. Simulate longer delay
2. Send command at T=0s
3. Agent responds at T=12s

**Expected Result:**
```
T=0s:  ? Request sent (MessageId=1)
T=5s:  ?? Retry #1
T=10s: ?? Retry #2
T=12s: ? Response received
T=12s: ? CommandSuccess emitted
```

**Verification:**
- Backend logs show 2 retry attempts
- Agent cache used for both retries
- CommandSuccess emitted after response
- RetryCount = 2

---

### Test 4: Max Retries Exceeded (Failure) ?

**Scenario**: Agent never responds (network issue or agent down)

**Steps:**
1. Stop agent or block network
2. Send command at T=0s
3. Wait for retries to exhaust

**Expected Result:**
```
T=0s:  ? Request sent (MessageId=1)
T=5s:  ?? Retry #1
T=10s: ?? Retry #2
T=15s: ?? Retry #3
T=16s: ? Max retries exceeded
T=16s: ? CommandFailed emitted
T=16s: ? Request removed from pending
```

**Verification:**
- Backend logs: `"Request 1 failed after 3 retries"`
- SignalR: `CommandFailed` event received
- Frontend shows error: "Connection timeout after 3 retries"
- RetryCount = 3

---

### Test 5: Agent Caching (Double Execution Prevention) ?

**Scenario**: Verify agent doesn't execute command twice on retry

**Steps:**
1. Send `restart-service` command
2. Let it retry once (wait 6 seconds before agent responds)
3. Check if service was restarted only once

**Expected Result:**
```
T=0s:  ?? Backend sends: restart-service (MessageId=1)
T=1s:  ?? Agent executes restart
T=1s:  ?? Agent caches response (MessageId=1)
T=5s:  ?? Backend retries (same MessageId=1)
T=6s:  ?? Agent receives retry
T=6s:  ?? Agent finds MessageId=1 in cache
T=6s:  ?? Agent sends cached response (NO re-execution)
T=6s:  ? Backend receives response
```

**Verification:**
- Service restarted only ONCE
- Agent logs: `"[5] Message ID 1 found in cache"`
- Agent logs: `"[6] Using cached response"`
- No duplicate execution

---

### Test 6: Multiple Concurrent Requests ??

**Scenario**: Send multiple commands simultaneously

**Steps:**
1. Send `get-server-info` (MessageId=1)
2. Send `get-services` (MessageId=2) 1 second later
3. Send `get-processes` (MessageId=3) 1 second later
4. Verify each tracked independently

**Expected Result:**
```
Each request has unique MessageId
Each request retries independently
Each request can succeed/fail independently
No interference between requests
```

**Verification:**
- All 3 requests tracked in `_pendingRequests`
- Each has own retry counter
- Responses matched correctly by MessageId
- No cross-contamination

---

### Test 7: SignalR Event Reception (Frontend) ???

**Scenario**: Frontend receives real-time command status

**Steps:**
1. Connect frontend SignalR to backend
2. Subscribe to server events
3. Send command that succeeds
4. Send command that fails

**Expected Frontend Code:**
```javascript
connection.on("CommandSuccess", (data) => {
    console.log(`? Success: ${data.Action} on server ${data.ServerId}`);
    console.log(`Message: ${data.Message}`);
    // Update UI: Green check, re-enable button
});

connection.on("CommandFailed", (data) => {
    console.error(`? Failed: ${data.Action} on server ${data.ServerId}`);
    console.error(`Message: ${data.Message}`);
    // Update UI: Red X, show error message
});
```

**Verification:**
- Success event shows action name
- Failure event shows retry count in message
- Timestamps are correct
- ServerId matches

---

## How to Manually Test

### Using Test UI (if available):

1. **Open test page**: `/test-signalr.html`
2. **Login**: `admin@example.com` / `admin123`
3. **Connect SignalR** and subscribe to server 1
4. **Test Success**: Click "Get Server Info"
   - Should complete within 5s
   - No retries logged
5. **Test Failure**: Stop agent, click "Get Services"
   - Should see 3 retries in logs
   - CommandFailed event after 15s

### Using curl + Agent Logs:

```bash
# Terminal 1: Watch backend logs
dotnet run --project Presentation

# Terminal 2: Watch agent logs
tail -f ~/engineering-project-1/logs/server-agent.log

# Terminal 3: Send command
curl -X GET https://localhost:7287/api/server/1/info \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Observe retry behavior in logs
```

---

## Expected Log Output

### Successful Request (No Retry):
```
[INFO] Sending command to server 1: Action='get-server-info', MessageId=1
[INFO] ?? Sending exact message: {"type":"request","id":1,...}
[INFO] Command sent successfully to server 1, waiting for response...
[INFO] === RESPONSE RECEIVED ===
[INFO] Successfully matched response to pending request ID 1
[INFO] CommandSuccess broadcast for action 'get-server-info' on server 1
```

### Request with Retries:
```
[INFO] Sending command to server 1: Action='get-services', MessageId=2
[INFO] Command sent successfully to server 1, waiting for response...
[WARN] Retrying request 2 for action 'get-services' on server 1 (attempt 1/3)
[INFO] ?? Resending request 2 to server 1: Action='get-services'
[WARN] Retrying request 2 for action 'get-services' on server 1 (attempt 2/3)
[INFO] ?? Resending request 2 to server 1: Action='get-services'
[INFO] === RESPONSE RECEIVED ===
[INFO] Successfully matched response to pending request ID 2
[INFO] CommandSuccess broadcast
```

### Request Failure (Max Retries):
```
[INFO] Sending command to server 1: Action='restart-service', MessageId=3
[WARN] Retrying request 3 (attempt 1/3)
[WARN] Retrying request 3 (attempt 2/3)
[WARN] Retrying request 3 (attempt 3/3)
[ERROR] Request 3 for action 'restart-service' on server 1 failed after 3 retries
[ERROR] Broadcasting CommandFailed for action 'restart-service' on server 1
```

---

## Troubleshooting

### Issue: Retries not triggering
**Check:**
- Is `RequestResponseManager` singleton initialized?
- Is monitoring task running? (Should log "monitoring started")
- Are pending requests being tracked?

### Issue: Agent executes command twice
**Check:**
- Agent cache implementation correct?
- Same MessageId used for retries?
- Agent logs show "found in cache"?

### Issue: SignalR events not received
**Check:**
- Frontend subscribed to correct server group?
- SignalR connection established?
- Hub context available in AgentMessageHandler?

---

## Success Criteria

? **Test 1**: Command completes without retry (agent fast)
? **Test 2**: Command completes after 1 retry (agent slow)
? **Test 3**: Command completes after 2-3 retries
? **Test 4**: Command fails after 3 retries (agent down)
? **Test 5**: Agent doesn't execute command twice on retry
? **Test 6**: Multiple concurrent requests work independently
? **Test 7**: Frontend receives CommandSuccess/Failed events

---

## Configuration

Current retry settings (in `RequestResponseManager.cs`):
```csharp
private const int MaxRetries = 3;
private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);
```

To adjust:
- Change `MaxRetries` for more/fewer attempts
- Change `RetryInterval` for faster/slower retries
- Note: Agent cache size is 100 items (in `ServerAgent.py`)

---

## Next Steps

1. ? **Deploy to test environment**
2. ?? **Run Test 1-7** (documented above)
3. ?? **Update frontend** to handle SignalR events
4. ?? **Add UI feedback** (spinners, success/error states)
5. ?? **Monitor production** for retry frequency

---

**Implementation Status: 100% Complete** ?
**Testing Status: Ready for Testing** ??
**Frontend Integration: Pending** ??
