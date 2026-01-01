Agent Update: Config Manager & Watchlist
Metrics
Overview
We have updated the Server Agent to support dynamic configuration and targeted monitoring. The
agent can now persist configuration settings (like metric intervals and specific lists of
services/processes to watch) and report detailed metrics for those specific items periodically.
1. New Action: Update Agent Config
The backend can now send a command to update the agent's internal configuration. These changes are
persistent (handled by ConfigManager).
Action Name: update-agent-config
Payload Structure: You can update metricsInterval, watchlist, or both.
JSON
{
 "action": "update-agent-config",
 "id": "uuid-1234",
 "payload": {
 "metricsInterval": 10, // (Optional) Interval in seconds
 "watchlist": { // (Optional) Lists of items to monitor
 "services": ["nginx", "docker"],
 "processes": ["python app.py", "node server.js"]
 }
 }
}
Response:
JSON
{"status": "ok"}
2. New Event: Periodic Watchlist Metrics
We added a new background loop send_periodic_watchlist_metrics. It runs alongside the
standard system metrics loop.
Event Type: event Event Name: watchlist-metrics
Data Structure: The agent iterates through the configured watchlist.
1. Services: Fetches full systemd details.
2. Processes: Searches for processes where the cmdline matches the string provided in the
config.
Example Payload received by Backend:
JSON
{
 "type": "event",
 "name": "watchlist-metrics",
 "data": {
 "services": [
 {
 "name": "nginx",
 "activeState": "active",
 "subState": "running",
 "cpuUsagePercent": 1.2,
 "memoryUsage": 55.4,
 ... // Standard service details
 }
 ],
 "processes": [
 {
 "pid": 4521,
 "name": "python",
 "cmdline": "/usr/bin/python app.py --worker",
 "cpuPercent": 5.0,
 "memoryMb": 120.5,
 ... // Standard process details
 },
 // Note: If a process is not found by cmdline search,
 // the agent returns None or an error object in this list.
 ]
 }
}
3. Logic Details (For Context)
Process Matching
When you add a process string to the watchlist (e.g., "my-worker.py"), the agent searches the
command line arguments of all running processes.
• Method: get_process_details_by_name(cmdline_string)
• Behavior: It performs a substring match. If cmdline_string is found inside the process's
full command arguments, that process is tracked.
Config Persistence
The ConfigManager saves these settings locally on the agent. If the agent restarts, it will remember
the last metricsInterval and watchlist sent by the backend.
Backend Requirements
To support these changes, the backend needs to:
1. Listen for watchlist-metrics events and store/display this high-priority data separately
from general server stats.
2. Provide a UI/API to send update-agent-config requests so users can modify the
monitoring frequency or add new services/processes to the watchlist dynamically.