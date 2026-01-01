// src/pages/NotificationsPage.jsx
import React, { useEffect, useMemo, useState } from 'react';
import api from '../api/axiosConfig';
import signalRService from '../services/signalRService';
import { useAuth } from '../context/AuthContext';
import { useMonitoring } from '../context/MonitoringContext';
import ServerSelect from '../components/common/ServerSelect';

const severityClass = (sev) => {
  const s = (sev || '').toLowerCase();
  if (s === 'critical') return 'badge-crit';
  if (s === 'warning') return 'badge-warn';
  if (s === 'info') return 'badge-info';
  return 'badge-muted';
};

const getErrMsg = (err, fallback) =>
  err?.response?.data?.message || err?.message || fallback;

const NotificationsPage = () => {
  const { token } = useAuth();

  const { selectedServerId, setSelectedServerId } = useMonitoring();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // Server selection is managed globally by MonitoringContext (dropdown)

  const [acknowledged, setAcknowledged] = useState(false);
  const [severity, setSeverity] = useState(''); // Info/Warning/Critical

  const [pageSize, setPageSize] = useState(50);
  const [page, setPage] = useState(1);

  const [alerts, setAlerts] = useState([]);
  const [totalCount, setTotalCount] = useState(null);

  const [selectedIds, setSelectedIds] = useState({});
  const selectedList = useMemo(
    () => Object.keys(selectedIds).filter((k) => selectedIds[k]).map((k) => Number(k)),
    [selectedIds]
  );

  const [rtConnected, setRtConnected] = useState(false);
  const [newWhileFiltered, setNewWhileFiltered] = useState(0);

  const onChangeServer = (sid) => {
    setSelectedServerId(sid);
    setPage(1);
    setNewWhileFiltered(0);
  };

  const loadAlerts = async () => {
    setLoading(true);
    setError('');
    try {
      const params = {
        serverId: selectedServerId || undefined,
        acknowledged: acknowledged ? true : false,
        severity: severity ? severity : undefined,
        page,
        pageSize,
      };

      const res = await api.get('/api/alerts', { params });

      setAlerts(Array.isArray(res.data) ? res.data : []);
      // it will be present if headers are CORS-exposed
      const tc = res.headers?.['x-total-count'];
      if (tc != null) setTotalCount(Number(tc));
      else setTotalCount(null);

      setSelectedIds({});
      setNewWhileFiltered(0);
    } catch (err) {
      setError(getErrMsg(err, 'Failed to load alerts'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAlerts();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedServerId, acknowledged, severity, page, pageSize]);

  // Realtime AlertTriggered
  useEffect(() => {
    if (!token) return;

    let mounted = true;

    const ensureRealtime = async () => {
      try {
        if (!signalRService.isConnected()) {
          await signalRService.connect(token);
        }
        if (!mounted) return;
        setRtConnected(true);

        signalRService.offAlertTriggered();

        signalRService.onAlertTriggered((evt) => {
          if (!mounted) return;

          // doc payload -> AlertDto shape
          const a = {
            id: evt.id,
            createdAtUtc: evt.timestamp,
            title: evt.title,
            message: evt.message,
            severity: evt.severity,
            isAcknowledged: false,
            acknowledgedAtUtc: null,
            monitoredServerId: evt.serverId,
            serverName: evt.serverName,
            alertRuleId: evt.ruleId,
          };

          const matchesServer = Number(selectedServerId) === Number(a.monitoredServerId);
          const matchesSeverity = !severity || String(severity) === String(a.severity);
          const matchesAck = acknowledged === false; // new alert is unack

          // Sadece ilk sayfadaysak listeye prepend edelim
          if (matchesServer && matchesSeverity && matchesAck && page === 1) {
            setAlerts((prev) => [a, ...prev]);
          } else {
            setNewWhileFiltered((n) => n + 1);
          }
        });
      } catch (err) {
        console.error('Alert realtime failed:', err);
        if (mounted) setRtConnected(false);
      }
    };

    ensureRealtime();

    return () => {
      mounted = false;
      signalRService.offAlertTriggered();
    };
  }, [token, selectedServerId, severity, acknowledged, page]);

  const toggleSelected = (id, checked) => {
    setSelectedIds((prev) => ({ ...prev, [id]: checked }));
  };

  const toggleSelectAll = (checked) => {
    const next = {};
    (alerts || []).forEach((a) => {
      next[a.id] = checked;
    });
    setSelectedIds(next);
  };

  const acknowledgeOne = async (id) => {
    setError('');
    try {
      await api.post(`/api/alerts/${id}/acknowledge`);
      setAlerts((prev) =>
        prev.map((a) =>
          a.id === id
            ? { ...a, isAcknowledged: true, acknowledgedAtUtc: new Date().toISOString() }
            : a
        )
      );
    } catch (err) {
      setError(getErrMsg(err, 'Failed to acknowledge alert'));
    }
  };

  const acknowledgeSelected = async () => {
    if (!selectedList.length) return;
    setError('');
    try {
      await api.post('/api/alerts/acknowledge-batch', { alertIds: selectedList });
      setAlerts((prev) =>
        prev.map((a) =>
          selectedIds[a.id]
            ? { ...a, isAcknowledged: true, acknowledgedAtUtc: new Date().toISOString() }
            : a
        )
      );
      setSelectedIds({});
    } catch (err) {
      setError(getErrMsg(err, 'Failed to acknowledge batch'));
    }
  };

  const deleteOne = async (id) => {
    setError('');
    try {
      await api.delete(`/api/alerts/${id}`);
      setAlerts((prev) => prev.filter((a) => a.id !== id));
      setSelectedIds((prev) => {
        const n = { ...prev };
        delete n[id];
        return n;
      });
    } catch (err) {
      setError(getErrMsg(err, 'Failed to delete alert'));
    }
  };

  const deleteAcknowledged = async () => {
    setError('');
    try {
      await api.delete('/api/alerts/acknowledged', {
        params: { serverId: selectedServerId || undefined },
      });
      await loadAlerts();
    } catch (err) {
      setError(getErrMsg(err, 'Failed to delete acknowledged alerts'));
    }
  };

  const allChecked = alerts.length > 0 && alerts.every((a) => selectedIds[a.id]);
  const someChecked = alerts.some((a) => selectedIds[a.id]);

  return (
    <div>
      <div className="page-header">
        <h1 className="page-title">Notifications</h1>
        <div className="action-row">
          <span className={`badge ${rtConnected ? 'badge-ok' : 'badge-warn'}`}>
            Realtime: {rtConnected ? 'Connected' : 'Disconnected'}
          </span>
          {newWhileFiltered > 0 ? (
            <span className="badge badge-info">{newWhileFiltered} new (refresh)</span>
          ) : null}
        </div>
      </div>

      {error ? <div className="error-box">{error}</div> : null}

      <div className="card">
        <h2 style={{ marginTop: 0 }}>Alert History</h2>

        <div className="form-row">
          <ServerSelect label="Server" value={selectedServerId} onChange={onChangeServer} minWidth={360} />

          <div className="input-group" style={{ minWidth: 220 }}>
            <label>Acknowledged</label>
            <select
              value={acknowledged ? 'true' : 'false'}
              onChange={(e) => setAcknowledged(e.target.value === 'true')}
            >
              <option value="false">Unacknowledged</option>
              <option value="true">Acknowledged</option>
            </select>
          </div>

          <div className="input-group" style={{ minWidth: 220 }}>
            <label>Severity</label>
            <select value={severity} onChange={(e) => setSeverity(e.target.value)}>
              <option value="">All</option>
              <option value="Info">Info</option>
              <option value="Warning">Warning</option>
              <option value="Critical">Critical</option>
            </select>
          </div>

          <div className="input-group" style={{ minWidth: 220 }}>
            <label>Page Size</label>
            <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))}>
              <option value="20">20</option>
              <option value="50">50</option>
              <option value="100">100</option>
            </select>
          </div>

          <button className="btn btn-primary" onClick={loadAlerts} disabled={loading}>
            {loading ? 'Loading…' : 'Refresh'}
          </button>
        </div>

        <div className="action-row" style={{ marginTop: 12 }}>
          <button
            className="btn btn-muted"
            onClick={acknowledgeSelected}
            disabled={!selectedList.length}
            title="Select alerts from the list below"
          >
            Acknowledge Selected ({selectedList.length})
          </button>

          <button className="btn btn-danger" onClick={deleteAcknowledged}>
            Delete Acknowledged
          </button>

          <div className="small" style={{ marginLeft: 'auto' }}>
            {totalCount != null ? `Total: ${totalCount}` : ''}
          </div>
        </div>
      </div>

      <div className="card">
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: 42 }}>
                  <input
                    type="checkbox"
                    checked={allChecked}
                    ref={(el) => {
                      if (el) el.indeterminate = !allChecked && someChecked;
                    }}
                    onChange={(e) => toggleSelectAll(e.target.checked)}
                  />
                </th>
                <th style={{ width: 110 }}>Severity</th>
                <th style={{ width: 220 }}>Time</th>
                <th>Title</th>
                <th>Message</th>
                <th style={{ width: 140 }}>Status</th>
                <th style={{ width: 220 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {alerts.length === 0 ? (
                <tr>
                  <td colSpan={7} className="small">
                    {loading ? 'Loading…' : 'No alerts found.'}
                  </td>
                </tr>
              ) : (
                alerts.map((a) => (
                  <tr key={a.id}>
                    <td>
                      <input
                        type="checkbox"
                        checked={!!selectedIds[a.id]}
                        onChange={(e) => toggleSelected(a.id, e.target.checked)}
                      />
                    </td>
                    <td>
                      <span className={`badge ${severityClass(a.severity)}`}>{a.severity}</span>
                    </td>
                    <td className="small">
                      {a.createdAtUtc ? new Date(a.createdAtUtc).toLocaleString() : '—'}
                    </td>
                    <td>{a.title}</td>
                    <td className="small">{a.message}</td>
                    <td>
                      {a.isAcknowledged ? (
                        <span className="badge badge-muted">Acknowledged</span>
                      ) : (
                        <span className="badge badge-warn">New</span>
                      )}
                    </td>
                    <td>
                      <div className="action-row">
                        {!a.isAcknowledged ? (
                          <button className="btn btn-warning" onClick={() => acknowledgeOne(a.id)}>
                            Ack
                          </button>
                        ) : (
                          <button className="btn btn-muted" disabled>
                            Ack
                          </button>
                        )}
                        <button className="btn btn-danger" onClick={() => deleteOne(a.id)}>
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="action-row" style={{ marginTop: 12 }}>
          <button className="btn" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
            Prev
          </button>
          <span className="badge badge-muted">Page: {page}</span>
          <button className="btn" onClick={() => setPage((p) => p + 1)}>
            Next
          </button>
        </div>
      </div>
    </div>
  );
};

export default NotificationsPage;
