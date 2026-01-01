// src/services/signalRService.js
import * as signalR from '@microsoft/signalr';

/**
 * Thin wrapper around a single shared SignalR connection.
 * Keeps backward-compatible method names used across the app.
 *
 * IMPORTANT:
 * We intentionally DO NOT set `skipNegotiation: true` because the frontend
 * relies on a server-issued `connectionId` for REST subscribe calls
 * (`X-SignalR-ConnectionId`). Some environments may not populate `connectionId`
 * when negotiation is skipped.
 */
class SignalRService {
  /** @type {signalR.HubConnection | null} */
  connection = null;
  /** @type {string | null} */
  connectionId = null;
  /** @type {Promise<void> | null} */
  _connectingPromise = null;

  _hubUrl() {
    return (
      process.env.REACT_APP_SIGNALR_HUB ||
      `${process.env.REACT_APP_API_BASE_URL || 'https://localhost:7287'}/monitoring-hub`
    );
  }

  /**
   * Wait until SignalR client has a usable connectionId.
   * @param {number} timeoutMs
   * @returns {Promise<string|null>}
   */
  async waitForConnectionId(timeoutMs = 5000) {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
      const cid = this.connection?.connectionId || this.connectionId;
      if (cid) {
        this.connectionId = cid;
        return cid;
      }
      // eslint-disable-next-line no-await-in-loop
      await new Promise((r) => setTimeout(r, 100));
    }
    const cid = this.connection?.connectionId || this.connectionId;
    if (cid) this.connectionId = cid;
    return cid || null;
  }

  /**
   * Ensure connectionId is available (best-effort).
   * @param {number} timeoutMs
   */
  async ensureConnectionId(timeoutMs = 5000) {
    const cid = this.getConnectionId();
    if (cid) return cid;
    return this.waitForConnectionId(timeoutMs);
  }

  /**
   * Connect to hub with JWT auth. Safe to call multiple times.
   * @param {string} jwtToken
   */
  async connect(jwtToken) {
    if (!jwtToken) throw new Error('Missing JWT token for SignalR');

    // Already connected
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.ensureConnectionId();
      return;
    }

    // De-dupe concurrent connection attempts
    if (this._connectingPromise) {
      await this._connectingPromise;
      await this.ensureConnectionId();
      return;
    }

    // Clean up any old connection instance
    if (this.connection) {
      try {
        this.removeAllListeners();
        await this.connection.stop();
      } catch {
        // ignore
      } finally {
        this.connection = null;
        this.connectionId = null;
      }
    }

    const hubUrl = this._hubUrl();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => jwtToken,
        // Prefer WebSockets, but keep negotiation enabled so the server can
        // provide a stable connectionId.
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.onreconnecting((err) => {
      console.warn('SignalR reconnecting:', err);
      this.connectionId = null;
    });

    this.connection.onreconnected((cid) => {
      const next = cid || this.connection?.connectionId || null;
      this.connectionId = next;
      console.log('SignalR reconnected. ConnectionId:', next);
    });

    this.connection.onclose((err) => {
      console.warn('SignalR closed:', err);
      this.connectionId = null;
    });

    this._connectingPromise = (async () => {
      try {
        await this.connection.start();
        this.connectionId = this.connection.connectionId || null;
        await this.ensureConnectionId();
        console.log('✅ SignalR connected. ConnectionId:', this.connectionId);
      } finally {
        this._connectingPromise = null;
      }
    })();

    await this._connectingPromise;
  }

  async disconnect() {
    if (!this.connection) return;
    try {
      this.removeAllListeners();
      await this.connection.stop();
    } finally {
      this.connection = null;
      this.connectionId = null;
      this._connectingPromise = null;
      console.log('SignalR disconnected');
    }
  }

  isConnected() {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  getConnectionId() {
    return this.connection?.connectionId || this.connectionId || null;
  }

  getState() {
    return this.connection?.state || null;
  }

  /** @private */
  _ensureConnected() {
    if (!this.connection) throw new Error('SignalR not connected');
  }

  /** @private */
  _on(eventName, callback) {
    this._ensureConnected();
    this.connection.on(eventName, callback);
  }

  /** @private */
  _off(eventName) {
    if (this.connection) this.connection.off(eventName);
  }

  onMetricsUpdated(callback) {
    this._on('MetricsUpdated', callback);
  }
  offMetricsUpdated() {
    this._off('MetricsUpdated');
  }

  onWatchlistMetricsUpdated(callback) {
    this._on('WatchlistMetricsUpdated', callback);
  }
  offWatchlistMetricsUpdated() {
    this._off('WatchlistMetricsUpdated');
  }

  onCommandSuccess(callback) {
    this._on('CommandSuccess', callback);
  }
  offCommandSuccess() {
    this._off('CommandSuccess');
  }

  onCommandFailed(callback) {
    this._on('CommandFailed', callback);
  }
  offCommandFailed() {
    this._off('CommandFailed');
  }

  onAlertTriggered(callback) {
    this._on('AlertTriggered', callback);
  }
  offAlertTriggered() {
    this._off('AlertTriggered');
  }

  // Backup events (v2.1 / Dec 2025)
  onBackupCompleted(callback) {
    this._on('BackupCompleted', callback);
  }
  offBackupCompleted() {
    this._off('BackupCompleted');
  }

  removeAllListeners() {
    if (!this.connection) return;
    [
      'MetricsUpdated',
      'WatchlistMetricsUpdated',
      'CommandSuccess',
      'CommandFailed',
      'AlertTriggered',
      'BackupCompleted',
    ].forEach((ev) => this.connection.off(ev));
  }
}

export default new SignalRService();
