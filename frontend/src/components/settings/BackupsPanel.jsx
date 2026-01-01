// src/components/settings/BackupsPanel.jsx
import React, { useEffect, useMemo, useState } from 'react';
import api from '../../api/axiosConfig';
import signalRService from '../../services/signalRService';
import { useMonitoring } from '../../context/MonitoringContext';

const bytesToHuman = (bytes) => {
  const n = Number(bytes);
  if (!Number.isFinite(n) || n <= 0) return '-';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let v = n;
  let i = 0;
  while (v >= 1024 && i < units.length - 1) {
    v /= 1024;
    i += 1;
  }
  return `${v.toFixed(v >= 10 || i === 0 ? 0 : 1)} ${units[i]}`;
};

const fmtDateTime = (iso) => {
  if (!iso) return '-';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return String(iso);
  return d.toLocaleString();
};

const statusBadgeClass = (s) => {
  const v = String(s || '').toLowerCase();
  if (v === 'success') return 'badge badge-ok';
  if (v === 'error' || v === 'failed') return 'badge badge-bad';
  return 'badge badge-muted';
};

function BackupJobModal({ mode, initial, agentId, agents = [], onClose, onSaved }) {
  const isEdit = mode === 'edit';

  // Allow choosing a different agent/server when creating a job.
  const [targetAgentId, setTargetAgentId] = useState(
    isEdit ? Number(initial?.agentId) : Number(agentId)
  );

  useEffect(() => {
    // When opening the create modal, default to currently selected server.
    if (!isEdit) setTargetAgentId(Number(agentId));
  }, [agentId, isEdit]);

  const agentOptions = useMemo(() => {
    const list = Array.isArray(agents) ? agents : [];
    return list
      .map((a) => {
        const id = Number(a.id ?? a.agentId ?? a.serverId ?? a.agentID);
        if (!Number.isFinite(id)) return null;
        const name = a.name || a.agentName || a.hostname || `Server ${id}`;
        const hostname = a.hostname || '';
        const ip = a.ipAddress || a.ip || '';
        const isOnline = Boolean(a.isOnline);
        const dot = isOnline ? '🟢' : '🔴';
        const parts = [name];
        const meta = [hostname, ip].filter(Boolean).join(' · ');
        if (meta) parts.push(`(${meta})`);
        return { id, label: `${dot} ${parts.join(' ')}` };
      })
      .filter(Boolean)
      .sort((x, y) => x.id - y.id);
  }, [agents]);

  const [name, setName] = useState(initial?.name || '');
  const [sourcePath, setSourcePath] = useState(initial?.sourcePath || '');
  const [repoUrl, setRepoUrl] = useState(initial?.repoUrl || '');
  const [scheduleCron, setScheduleCron] = useState(initial?.scheduleCron || '0 2 * * *');
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);

  // Credentials are NOT returned by backend. Only send when user provides them.
  const [repoPassword, setRepoPassword] = useState('');
  const [sshPrivateKey, setSshPrivateKey] = useState('');

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const validate = () => {
    if (!isEdit && !Number.isFinite(Number(targetAgentId))) return 'Please select a target server.';
    if (!name.trim()) return 'Name is required.';
    if (!sourcePath.trim()) return 'Source path is required.';
    if (!repoUrl.trim()) return 'Repository URL is required.';
    if (!scheduleCron.trim()) return 'Cron schedule is required.';
    if (!isEdit) {
      if (!repoPassword.trim()) return 'Repository password is required.';
      if (!sshPrivateKey.trim()) return 'SSH private key is required.';
    }
    return '';
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    const v = validate();
    if (v) {
      setError(v);
      return;
    }

    try {
      setSaving(true);
      setError('');

      if (isEdit) {
        const payload = {
          name: name.trim(),
          sourcePath: sourcePath.trim(),
          repoUrl: repoUrl.trim(),
          scheduleCron: scheduleCron.trim(),
          isActive: Boolean(isActive),
          ...(repoPassword.trim() ? { repoPassword: repoPassword.trim() } : {}),
          ...(sshPrivateKey.trim() ? { sshPrivateKey: sshPrivateKey.trim() } : {}),
        };

        await api.put(`/api/backups/${initial.id}`, payload);
      } else {
        const payload = {
          agentId: Number(targetAgentId),
          name: name.trim(),
          sourcePath: sourcePath.trim(),
          repoUrl: repoUrl.trim(),
          repoPassword: repoPassword.trim(),
          sshPrivateKey: sshPrivateKey.trim(),
          scheduleCron: scheduleCron.trim(),
          isActive: Boolean(isActive),
        };

        await api.post('/api/backups', payload);
      }

      onSaved?.();
      onClose?.();
    } catch (err) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Request failed';
      setError(msg);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal" style={{ maxWidth: 820 }} onMouseDown={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div className="modal-title">{isEdit ? 'Edit Backup Job' : 'Create Backup Job'}</div>
          <button className="btn btn-ghost" onClick={onClose}>Close</button>
        </div>

        <div className="modal-body">
          <div className="notice" style={{ marginBottom: 12 }}>
            <div style={{ fontWeight: 700 }}>Security note</div>
            <div className="small">
              Repository password and SSH key are encrypted on the backend and are never returned in responses.
              {isEdit && ' To change credentials, enter new values below. Leave blank to keep current values.'}
            </div>
          </div>

          {error ? <div className="error-box" style={{ marginBottom: 12 }}>{error}</div> : null}

          <form onSubmit={onSubmit} className="form-grid">
            <div className="input-group">
              <label>{isEdit ? 'Agent ID' : 'Target Server'}</label>
              {isEdit ? (
                <div className="small">{initial?.agentId}</div>
              ) : (
                <select
                  className="input"
                  value={Number.isFinite(Number(targetAgentId)) ? String(targetAgentId) : ''}
                  onChange={(e) => setTargetAgentId(Number(e.target.value))}
                >
                  <option value="" disabled>
                    {agentOptions.length ? 'Select a server…' : 'No servers loaded'}
                  </option>
                  {agentOptions.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.label}
                    </option>
                  ))}
                </select>
              )}
            </div>

            <div className="input-group">
              <label>Name</label>
              <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g., Production DB Backup" />
            </div>

            <div className="input-group">
              <label>Source Path</label>
              <input className="input" value={sourcePath} onChange={(e) => setSourcePath(e.target.value)} placeholder="/var/lib/postgresql/data" />
            </div>

            <div className="input-group">
              <label>Repository URL (SFTP)</label>
              <input className="input" value={repoUrl} onChange={(e) => setRepoUrl(e.target.value)} placeholder="sftp:user@host:/path" />
            </div>

            <div className="input-group">
              <label>Schedule (Cron)</label>
              <input className="input" value={scheduleCron} onChange={(e) => setScheduleCron(e.target.value)} placeholder="0 2 * * *" />
              <div className="help">
                Examples: <code>0 2 * * *</code> (daily 02:00), <code>0 */6 * * *</code> (every 6h), <code>*/30 * * * *</code> (every 30m)
              </div>
            </div>

            <div className="input-group">
              <label>Enabled</label>
              <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
                <span className="small">Active</span>
              </label>
            </div>

            <div className="hr" />

            <div className="input-group">
              <label>Repository Password</label>
              <input className="input" value={repoPassword} onChange={(e) => setRepoPassword(e.target.value)} placeholder={isEdit ? '(leave blank to keep)' : 'required'} />
            </div>

            <div className="input-group">
              <label>SSH Private Key</label>
              <textarea
                className="input"
                rows={6}
                value={sshPrivateKey}
                onChange={(e) => setSshPrivateKey(e.target.value)}
                placeholder={isEdit ? '(leave blank to keep)' : '-----BEGIN ... -----END ...'}
              />
            </div>

            <div className="toolbar" style={{ marginTop: 14 }}>
              <button className="btn btn-primary" type="submit" disabled={saving}>
                {saving ? 'Saving…' : (isEdit ? 'Save Changes' : 'Create Job')}
              </button>
              <button className="btn btn-muted" type="button" onClick={onClose} disabled={saving}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

function LogsModal({ job, logs, loading, error, onClose, onRefresh }) {
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal" style={{ maxWidth: 980 }} onMouseDown={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div className="modal-title">Backup Logs — {job?.name || job?.id}</div>
          <div className="toolbar">
            <button className="btn btn-muted" onClick={onRefresh} disabled={loading}>Refresh</button>
            <button className="btn btn-ghost" onClick={onClose}>Close</button>
          </div>
        </div>

        <div className="modal-body">
          {error ? <div className="error-box" style={{ marginBottom: 12 }}>{error}</div> : null}
          {loading ? <div className="muted">Loading…</div> : null}

          {!loading && (!logs || logs.length === 0) ? (
            <div className="muted">No logs found yet.</div>
          ) : null}

          {!loading && logs && logs.length > 0 ? (
            <table className="table">
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Status</th>
                  <th>Snapshot</th>
                  <th>Files</th>
                  <th>Added</th>
                  <th>Duration</th>
                  <th>Error</th>
                </tr>
              </thead>
              <tbody>
                {logs.map((l) => (
                  <tr key={l.id}>
                    <td>{fmtDateTime(l.createdAtUtc)}</td>
                    <td><span className={statusBadgeClass(l.status)}>{l.status}</span></td>
                    <td>{l.snapshotId || '-'}</td>
                    <td>{l.filesNew ?? '-'}</td>
                    <td>{l.dataAdded != null ? bytesToHuman(l.dataAdded) : '-'}</td>
                    <td>{l.durationSeconds != null ? `${Number(l.durationSeconds).toFixed(1)}s` : '-'}</td>
                    <td style={{ maxWidth: 360 }}>{l.errorMessage || '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : null}
        </div>
      </div>
    </div>
  );
}

function SnapshotsModal({ job, snapshots, loading, error, onClose, onRefresh }) {
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal" style={{ maxWidth: 980 }} onMouseDown={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div className="modal-title">Snapshots — {job?.name || job?.id}</div>
          <div className="toolbar">
            <button className="btn btn-muted" onClick={onRefresh} disabled={loading}>Refresh</button>
            <button className="btn btn-ghost" onClick={onClose}>Close</button>
          </div>
        </div>

        <div className="modal-body">
          {error ? <div className="error-box" style={{ marginBottom: 12 }}>{error}</div> : null}
          {loading ? <div className="muted">Loading…</div> : null}

          {!loading && (!snapshots || snapshots.length === 0) ? (
            <div className="muted">No snapshots found yet.</div>
          ) : null}

          {!loading && snapshots && snapshots.length > 0 ? (
            <table className="table">
              <thead>
                <tr>
                  <th>Snapshot ID</th>
                  <th>Time</th>
                  <th>Hostname</th>
                  <th>Paths</th>
                  <th>Size</th>
                </tr>
              </thead>
              <tbody>
                {snapshots.map((s) => (
                  <tr key={s.id}>
                    <td>{s.id}</td>
                    <td>{fmtDateTime(s.time)}</td>
                    <td>{s.hostname || '-'}</td>
                    <td style={{ maxWidth: 420 }}>{Array.isArray(s.paths) ? s.paths.join(', ') : '-'}</td>
                    <td>{s.size != null ? bytesToHuman(s.size) : '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : null}
        </div>
      </div>
    </div>
  );
}

export default function BackupsPanel() {
  const { selectedServerId, servers } = useMonitoring();

  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [inlineError, setInlineError] = useState('');
  const [inlineNotice, setInlineNotice] = useState('');

  const [showAllAgents, setShowAllAgents] = useState(false);
  const agentId = useMemo(() => (showAllAgents ? null : (selectedServerId ? Number(selectedServerId) : null)), [showAllAgents, selectedServerId]);

  const [jobModal, setJobModal] = useState(null); // { mode:'create'|'edit', job?:... }

  const [logsModal, setLogsModal] = useState({ open: false, job: null });
  const [logs, setLogs] = useState([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [logsError, setLogsError] = useState('');

  const [snapModal, setSnapModal] = useState({ open: false, job: null });
  const [snapshots, setSnapshots] = useState([]);
  const [snapLoading, setSnapLoading] = useState(false);
  const [snapError, setSnapError] = useState('');

  const loadJobs = async () => {
    if (!showAllAgents && !selectedServerId) {
      setInlineError('Select an agent/server first to manage backups.');
      setJobs([]);
      return;
    }
    try {
      setLoading(true);
      setInlineError('');
      const res = await api.get('/api/backups', { params: agentId ? { agentId } : {} });
      setJobs(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Failed to load backup jobs';
      setInlineError(msg);
    } finally {
      setLoading(false);
    }
  };

  const loadLogs = async (job) => {
    if (!job?.id) return;
    try {
      setLogsLoading(true);
      setLogsError('');
      const res = await api.get(`/api/backups/${job.id}/logs`, { params: { limit: 50 } });
      setLogs(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Failed to load logs';
      setLogsError(msg);
    } finally {
      setLogsLoading(false);
    }
  };

  const loadSnapshots = async (job) => {
    if (!job?.id) return;
    try {
      setSnapLoading(true);
      setSnapError('');
      const res = await api.get(`/api/backups/${job.id}/snapshots`);
      setSnapshots(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Failed to load snapshots';
      setSnapError(msg);
    } finally {
      setSnapLoading(false);
    }
  };

  useEffect(() => {
    setInlineNotice('');
    setInlineError('');
    loadJobs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [agentId]);

  // Live updates (SignalR)
  useEffect(() => {
    const handler = (event) => {
      // Expected payload: { serverId, jobId, taskId, status, ... }
      const serverId = Number(event?.serverId);
      if (!showAllAgents && serverId !== Number(selectedServerId)) return;

      const ok = String(event?.status || '').toLowerCase() === 'success';
      setInlineNotice(
        ok
          ? `Backup completed successfully (job ${event.jobId}). Snapshot: ${event.snapshotId || '-'}`
          : `Backup failed (job ${event.jobId}). ${event.errorMessage || ''}`.trim()
      );

      loadJobs();

      if (logsModal.open && logsModal.job?.id === event.jobId) {
        loadLogs(logsModal.job);
      }
      if (snapModal.open && snapModal.job?.id === event.jobId) {
        loadSnapshots(snapModal.job);
      }
    };

    try {
      if (signalRService.isConnected()) {
        signalRService.onBackupCompleted(handler);
        return () => {
          signalRService.offBackupCompleted?.();
        };
      }
    } catch {
      // SignalR might not be ready on this route; ignore.
    }

    return undefined;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedServerId, showAllAgents, logsModal.open, logsModal.job?.id, snapModal.open, snapModal.job?.id]);

  const triggerNow = async (job) => {
    try {
      setInlineError('');
      setInlineNotice('');
      const res = await api.post(`/api/backups/${job.id}/trigger`);
      const taskId = res?.data?.taskId;
      setInlineNotice(`Backup triggered. Task ID: ${taskId || '-'}`);
    } catch (err) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Trigger failed';
      setInlineError(msg);
    }
  };

  const toggleActive = async (job) => {
    try {
      setInlineError('');
      await api.put(`/api/backups/${job.id}`, { isActive: !job.isActive });
      loadJobs();
    } catch (err) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Update failed';
      setInlineError(msg);
    }
  };

  const deleteJob = async (job) => {
    // eslint-disable-next-line no-alert
    const ok = window.confirm('Delete this backup job? This will also delete its logs.');
    if (!ok) return;

    try {
      setInlineError('');
      await api.delete(`/api/backups/${job.id}`);
      loadJobs();
    } catch (err) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Delete failed';
      setInlineError(msg);
    }
  };

  const openLogs = async (job) => {
    setLogsModal({ open: true, job });
    setLogs([]);
    await loadLogs(job);
  };

  const openSnapshots = async (job) => {
    setSnapModal({ open: true, job });
    setSnapshots([]);
    await loadSnapshots(job);
  };

  const filteredJobs = useMemo(() => {
    if (showAllAgents) return jobs;
    return jobs.filter((j) => Number(j.agentId) === Number(selectedServerId));
  }, [jobs, showAllAgents, selectedServerId]);

  return (
    <div className="card" style={{ marginTop: 16 }}>
      <div className="card-header">
        <div>
          <div className="card-title">Backups</div>
          <div className="card-subtitle">
            Manage scheduled Restic backups to SFTP repositories.
          </div>
        </div>

        <div className="toolbar" style={{ gap: 10 }}>
          <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <input type="checkbox" checked={showAllAgents} onChange={(e) => setShowAllAgents(e.target.checked)} />
            <span className="small">All agents</span>
          </label>

          <button className="btn btn-muted" onClick={loadJobs} disabled={loading}>
            {loading ? 'Refreshing…' : 'Refresh'}
          </button>

          <button
            className="btn btn-primary"
            onClick={() => setJobModal({ mode: 'create' })}
            disabled={!selectedServerId}
            title={!selectedServerId ? 'Select an agent/server first' : ''}
          >
            New Job
          </button>
        </div>
      </div>

      <div className="card-body">
        <div className="notice" style={{ marginBottom: 12 }}>
          <div className="small">
            Current agent filter: <code>{showAllAgents ? 'All' : `agentId=${selectedServerId}`}</code>.
            Passwords and SSH keys are encrypted and never returned by the API.
          </div>
        </div>

        {inlineNotice ? <div className="notice" style={{ marginBottom: 12 }}>{inlineNotice}</div> : null}
        {inlineError ? <div className="error-box" style={{ marginBottom: 12 }}>{inlineError}</div> : null}

        {loading ? <div className="muted">Loading…</div> : null}

        {!loading && (!filteredJobs || filteredJobs.length === 0) ? (
          <div className="muted">No backup jobs yet. Click “New Job” to create one.</div>
        ) : null}

        {!loading && filteredJobs && filteredJobs.length > 0 ? (
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Agent</th>
                <th>Schedule</th>
                <th>Status</th>
                <th>Last Run</th>
                <th style={{ width: 520 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredJobs.map((j) => (
                <tr key={j.id}>
                  <td>
                    <div style={{ fontWeight: 700 }}>{j.name}</div>
                    <div className="small">{j.sourcePath} → {j.repoUrl}</div>
                  </td>
                  <td>
                    <div>{j.agentId}</div>
                    <div className="small">{j.agentName || ''}</div>
                  </td>
                  <td><code>{j.scheduleCron}</code></td>
                  <td>
                    <span className={statusBadgeClass(j.lastRunStatus)}>{j.lastRunStatus || 'pending'}</span>
                    <div className="small">{j.isActive ? 'Enabled' : 'Disabled'}</div>
                  </td>
                  <td>{fmtDateTime(j.lastRunAtUtc)}</td>
                  <td>
                    <div className="toolbar" style={{ flexWrap: 'wrap', gap: 8 }}>
                      <button className="btn btn-primary" onClick={() => triggerNow(j)}>Trigger</button>
                      <button className="btn btn-muted" onClick={() => toggleActive(j)}>
                        {j.isActive ? 'Disable' : 'Enable'}
                      </button>
                      <button className="btn btn-muted" onClick={() => openLogs(j)}>Logs</button>
                      <button className="btn btn-muted" onClick={() => openSnapshots(j)}>Snapshots</button>
                      <button className="btn btn-muted" onClick={() => setJobModal({ mode: 'edit', job: j })}>Edit</button>
                      <button className="btn btn-danger" onClick={() => deleteJob(j)}>Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </div>

      {jobModal ? (
        <BackupJobModal
          mode={jobModal.mode}
          initial={jobModal.job}
          agentId={Number(selectedServerId)}
          agents={servers || []}
          onClose={() => setJobModal(null)}
          onSaved={loadJobs}
        />
      ) : null}

      {logsModal.open ? (
        <LogsModal
          job={logsModal.job}
          logs={logs}
          loading={logsLoading}
          error={logsError}
          onClose={() => setLogsModal({ open: false, job: null })}
          onRefresh={() => loadLogs(logsModal.job)}
        />
      ) : null}

      {snapModal.open ? (
        <SnapshotsModal
          job={snapModal.job}
          snapshots={snapshots}
          loading={snapLoading}
          error={snapError}
          onClose={() => setSnapModal({ open: false, job: null })}
          onRefresh={() => loadSnapshots(snapModal.job)}
        />
      ) : null}
    </div>
  );
}
