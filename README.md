# Umay Monitor

Server monitoring solution with real-time metrics, alerting, and backup management. Deploy the entire stack with a single script using Docker.

## Name Origin
Umay is inspired by Umay Ana, a figure in Turkic mythology associated with protection.
This project uses the name respectfully and is not intended to represent or replace
any cultural or religious belief.

## Features

- Real-time server monitoring (CPU, memory, disk, network)
- Process and service management
- Alert rules and notifications
- Automated backup jobs with encryption
- Multi-server support
- WebSocket real-time updates

## Tech Stack

- **Frontend**: React.js
- **Backend**: ASP.NET Core 8.0
- **Database**: PostgreSQL 16
- **Deployment**: Docker Compose

## Installation

```bash
chmod +x install.sh
./install.sh
```

The installer will ask you to choose:

**Localhost Mode**
- Access via `http://localhost:3000`
- No additional configuration needed

**Domain Mode**
- Access via your domain (e.g., `https://monitor.example.com`)
- Requires reverse proxy with SSL

## Management

```bash
# View logs
docker compose logs -f

# Stop
docker compose down

# Restart
docker compose restart

# Update
git pull && docker compose up -d --build
```

## License

MIT License - see [LICENSE](LICENSE) file for details.
