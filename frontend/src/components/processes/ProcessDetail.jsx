import React from 'react';

const ProcessDetail = ({ process, loading, onRefresh }) => {
  if (!process) {
    return (
      <div className="process-detail">
        <div className="detail-header">
          <h2>Process Details</h2>
        </div>
        <p>Select a process.</p>
      </div>
    );
  }

  return (
    <div className="process-detail">
      <div className="process-header">
        <div>
          <div className="detail-header">
            <h2>
              {process.name} <span className="muted">(PID {process.pid})</span>
            </h2>
          </div>
          <p style={{ marginTop: 6 }}>
            <span className="badge badge-muted" style={{ marginRight: 6 }}>
              {process.user || '—'}
            </span>
            <span className="badge badge-muted">{process.status || '—'}</span>
          </p>
        </div>
        <div className="detail-actions">
          <button
            type="button"
            className="btn btn-muted"
            onClick={onRefresh}
            disabled={loading}
          >
            {loading ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>
      </div>

      {loading ? (
        <p>Loading…</p>
      ) : (
        <div className="process-properties">
          <h3>Details</h3>
          <ul>
            <li><strong>CPU %:</strong> {typeof process.cpuPercent === 'number' ? process.cpuPercent.toFixed(2) : '—'}</li>
            <li><strong>Memory %:</strong> {typeof process.memoryPercent === 'number' ? process.memoryPercent.toFixed(2) : '—'}</li>
            <li><strong>Nice:</strong> {process.nice ?? '—'}</li>
            <li><strong>Threads:</strong> {process.numThreads ?? '—'}</li>
            <li><strong>Uptime (sec):</strong> {process.uptimeSeconds ?? '—'}</li>
          </ul>

          <h3>Command Line</h3>
          <pre className="log-pre">{process.cmdline || '—'}</pre>
        </div>
      )}
    </div>
  );
};

export default ProcessDetail;
