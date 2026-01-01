// src/components/services/ServiceDetail.jsx
import React from 'react';

const formatMaybeDate = (iso) => {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
};

const toBadgeClass = (activeState) => {
  switch ((activeState || '').toLowerCase()) {
    case 'active':
      return 'badge badge-ok';
    case 'failed':
      return 'badge badge-bad';
    default:
      return 'badge badge-muted';
  }
};

const ServiceDetail = ({
  serverId,
  service,
  logs,
  loadingDetail,
  loadingLogs,
  restarting,
  onRestart,
  onRefreshDetails,
  onLoadLogs,
}) => {
  if (!service) {
    return (
      <div className="service-detail">
        <div className="detail-header">
          <h2>Service Details</h2>
          <span className="badge badge-muted">Server {serverId}</span>
        </div>
        <p>Select a service to view details.</p>
      </div>
    );
  }

  return (
    <div className="service-detail">
      <div className="service-header">
        <div>
          <div className="detail-header">
            <h2 title={service.name}>{service.name}</h2>
            <span className="badge badge-muted">Server {serverId}</span>
          </div>
          <p style={{ marginTop: 6 }}>
            Status:{' '}
            <span className={toBadgeClass(service.activeState)} style={{ marginRight: 6 }}>
              {service.activeState || 'unknown'}
            </span>
            <span className="badge badge-muted">{service.subState || '—'}</span>
          </p>
        </div>

        <div className="detail-actions">
          <button
            type="button"
            className="btn btn-muted"
            onClick={onRefreshDetails}
            disabled={loadingDetail}
          >
            {loadingDetail ? 'Refreshing…' : 'Refresh'}
          </button>
          <button
            type="button"
            className="btn"
            onClick={onLoadLogs}
            disabled={loadingLogs}
          >
            {loadingLogs ? 'Loading Logs…' : 'Load Logs'}
          </button>
          <button
            type="button"
            className="btn btn-warning"
            onClick={onRestart}
            disabled={restarting}
          >
            {restarting ? 'Restarting…' : 'Restart'}
          </button>
        </div>
      </div>

      <div className="service-properties">
        <h3>Properties</h3>
        {loadingDetail ? (
          <p>Loading details…</p>
        ) : (
          <ul>
            <li><strong>Main PID:</strong> {service.mainPID ?? '—'}</li>
            <li><strong>CPU Usage:</strong> {service.cpuUsagePercent ?? '—'}%</li>
            <li><strong>Memory:</strong> {service.memoryUsage ?? '—'} MB</li>
            <li><strong>Start Time:</strong> {formatMaybeDate(service.startTime)}</li>
            <li><strong>Exit Time:</strong> {formatMaybeDate(service.exitTime)}</li>
            <li><strong>Restart Policy:</strong> {service.restartPolicy ?? '—'}</li>
          </ul>
        )}
      </div>

      <div className="service-logs">
        <h3>Logs</h3>
        {loadingLogs ? (
          <p>Loading logs…</p>
        ) : logs && logs.length ? (
          <pre className="log-pre">{logs.join('\n')}</pre>
        ) : (
          <p>No logs loaded. Click <strong>Load Logs</strong>.</p>
        )}
      </div>
    </div>
  );
};

export default ServiceDetail;
