# Server Monitoring Frontend (React)

React-based frontend for a .NET 8 Server Monitoring backend.  
Supports **JWT authentication**, **SignalR real-time metrics**, **server selection**, **services/process management**, **watchlist**, **agent registration**, and **alerts/settings**.

## Features

- **Login (JWT)** and protected routes
- **SignalR** live updates (`MetricsUpdated`, `WatchlistMetricsUpdated`, command events)
- **Dashboard**
  - Real-time CPU/RAM/Disk/Network metrics
  - Uses `recentMetrics` on subscribe for instant charts (no initial blank state)
- **Server Info** (REST)
- **Services**
  - List + search
  - Details + logs + restart (results via SignalR)
- **Processes**
  - List + search
  - Process details with safe handling for short-lived PIDs
- **Watchlist**
  - Chip/tag style input (Enter/Comma to add, × to remove; Backspace removes last chip when empty)
  - Trend charts based on watchlist history events (needs at least 2 events)
- **Agents (v2.1)**
  - Register agent and show **one-time install command/token**
  - List agents, status refresh, delete
- **Notifications / Alert Rules / Settings**
  - UI pages for alert and settings management (e.g., Telegram settings)

## Tech Stack

- React + React Router
- Axios (REST API)
- `@microsoft/signalr` (WebSockets)
- Context API (Auth + Monitoring state)

## Prerequisites

- Node.js 18+ recommended
- Backend running (default: `https://localhost:7287`)
- If using HTTPS with a self-signed cert in dev, browser must trust/accept it

## Setup

1) Install dependencies:
```bash
npm install
```

2) Create `.env` in project root:
```env
REACT_APP_API_BASE_URL=https://localhost:7287
REACT_APP_SIGNALR_HUB=https://localhost:7287/monitoring-hub
```

3) Start the app:
```bash
npm start
```

App runs at:
- `http://localhost:3000`

## Main Routes

- `/login` – Login
- `/` – Dashboard (subscribe/unsubscribe, charts)
- `/server-info` – Server information
- `/services` – Service list/details/logs/restart
- `/processes` – Process list/details
- `/watchlist` – Watchlist configuration + trends
- `/agents` – Agent registration & management
- `/notifications` – Notifications page
- `/alert-rules` – Alert rule management
- `/settings` – Settings (e.g., Telegram notification settings)

## How Subscriptions Work

1. User logs in and gets a JWT token.
2. Frontend connects to SignalR hub using the token.
3. Frontend subscribes to a server via:
   - `POST /api/monitoring/subscribe/{serverId}`
   - `X-SignalR-ConnectionId` header is required.
4. Backend returns `recentMetrics` (last 50 samples) so UI renders immediately.
5. Live updates continue via SignalR `MetricsUpdated` events.

## Notes / Troubleshooting

- **“Server is not connected” (HTTP 503)**  
  The selected server/agent is offline or not connected to backend. Confirm agent is running and backend can reach it.
- **Process details errors**  
  Processes can be short-lived. If PID disappears, the UI refreshes the list and selects another PID when possible.
- **CORS / SignalR issues**  
  Ensure backend CORS allows the frontend origin and credentials for SignalR.

## Project Structure (high level)

- `src/pages/` – Route pages (Dashboard, Services, Processes, Agents, etc.)
- `src/components/` – UI components (lists, charts, server select, etc.)
- `src/context/` – Auth + Monitoring contexts (state persistence + SignalR listeners)
- `src/services/` – SignalR service wrapper
- `src/api/` – Axios config and API helpers
