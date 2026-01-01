1\. Architecture Overview

The backup system uses a Centralized Command model. The Backend acts as the orchestrator and secure vault, while the Agent acts as the stateless executor.

•	Backend: Stores encrypted credentials, manages schedules, and triggers tasks.

•	Agent: Receives ephemeral credentials, executes Restic commands, reports filtered results via WebSocket events, and provides secure directory listing.

•	Frontend: Provides interfaces for configuration (with file browsing), monitoring, and log visualization.



2\. Backend Specification

A. Database Schema (PostgreSQL)

Table 1: backup\_jobs

Stores configuration and schedule.

•	id (UUID, PK): Unique identifier.

•	agent\_id (UUID, FK): The server this job targets.

•	name (VARCHAR): Friendly name (e.g., "Web Server Backup").

•	source\_path (VARCHAR): Local path on Agent (e.g., /var/www).

•	repo\_url (VARCHAR): Destination. Format: sftp:user@host:/path.

•	repo\_password\_enc (TEXT): Encrypted Restic password.

•	ssh\_private\_key\_enc (TEXT): Encrypted SSH Identity file content.

•	schedule\_cron (VARCHAR): Cron expression.

•	is\_active (BOOLEAN): Toggle to enable/disable.

•	last\_run\_status (VARCHAR): success, error, or pending.

Table 2: backup\_logs

Stores history of execution.

•	id (UUID, PK): Unique Log ID (serves as taskId).

•	job\_id (UUID, FK): Link to parent job.

•	status (VARCHAR): success or error.

•	snapshot\_id (VARCHAR): Restic snapshot hash.

•	files\_new (INTEGER): Count of new files.

•	data\_added (BIGINT): Bytes added to repo.

•	duration\_seconds (FLOAT): Execution time.

•	created\_at (TIMESTAMP): Time of execution.

B. Encryption Strategy

•	Master Key: Store a 32-byte BACKUP\_ENCRYPTION\_KEY in the Backend .env.

•	Logic: Encrypt sensitive fields before DB insertion; decrypt only for WebSocket dispatch.

C. Scheduler Logic

•	Tick: Every minute, query active jobs where schedule\_cron matches.

•	Action: Decrypt credentials and dispatch the trigger-backup payload.

D. Agent Communication Protocol (WSS)

1\. Request: Trigger Backup (Backend $\\rightarrow$ Agent)

JSON

{

&nbsp; "id": "req-uuid",

&nbsp; "action": "trigger-backup",

&nbsp; "payload": {

&nbsp;   "taskId": "log-uuid",

&nbsp;   "source": "/var/www/html",

&nbsp;   "repo": "sftp:user@host:/backups",

&nbsp;   "password": "decrypted-password",

&nbsp;   "sshKey": "-----BEGIN OPENSSH PRIVATE KEY-----..."

&nbsp; }

}

2\. Request: Browse Filesystem (Backend $\\rightarrow$ Agent)

JSON

{

&nbsp; "id": "req-uuid",

&nbsp; "action": "browse-filesystem",

&nbsp; "payload": { "path": "/var" }

}

3\. Event: Task Completed (Agent $\\rightarrow$ Backend)

The Agent now filters output to send only the summary.

JSON

{

&nbsp; "type": "event",

&nbsp; "action": "backup-completed",

&nbsp; "payload": {

&nbsp;   "taskId": "log-uuid",

&nbsp;   "result": {

&nbsp;     "status": "ok",

&nbsp;     "snapshot\_id": "1fbf7784...",

&nbsp;     "files\_new": 1,

&nbsp;     "data\_added": 1522,

&nbsp;     "duration": 1.06

&nbsp;   }

&nbsp; }

}



3\. Frontend Specification

A. New Backup Job Modal

•	Source Path: Text Input + "Browse" Button.

•	File Browser Modal: Navigates remote Agent directories starting at /.

•	SSH Key: File Upload or Text Area.

B. Backup Dashboard

•	Job List: Table with Name, Schedule, Status, and "Run Now" action.

•	Snapshot Viewer: Hits /api/backups/:id/snapshots. Displays a table of historical snapshots (ID, Date, Size).

C. Log History

•	View: Displays execution logs. Errors show the specific Restic/SSH failure message (e.g., "Permission Denied" or "Repo Init Failed").



4\. Implementation Details (Agent Side)

•	SSH Handling: The Agent extracts the host from the repo\_url to construct a valid sftp.command using the ephemeral key.

•	Statelessness: No keys are stored on the Agent disk after the process completes.

•	Output Filtering: Progress status lines are discarded; only the summary message type is parsed and returned to the Backend.



5\. Summary of Tasks

Backend Team:

1\.	Create backup\_jobs and backup\_logs tables.

2\.	Implement AES encryption/decryption logic.

3\.	Implement the WebSocket event listener for backup-completed to update logs via taskId.

Frontend Team:

1\.	Build the Remote File Browser component.

2\.	Build the Add Backup form with SSH key support.

3\.	Add the Snapshot Table and Log Viewer.

Would you like me to generate a specific API route map for your Backend team to follow?





