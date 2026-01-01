// src/pages/SettingsPage.jsx
import React, { useEffect, useState } from 'react';
import api from '../api/axiosConfig';
import BackupsPanel from '../components/settings/BackupsPanel';

const SettingsPage = () => {
  const [loading, setLoading] = useState(true);
  const [settings, setSettings] = useState(null);
  const [error, setError] = useState('');

  const [botToken, setBotToken] = useState('');
  const [savingToken, setSavingToken] = useState(false);

  const [enabledBusy, setEnabledBusy] = useState(false);

  const [newChatId, setNewChatId] = useState('');
  const [newChatLabel, setNewChatLabel] = useState('');
  const [addingChat, setAddingChat] = useState(false);

  const [labelDraft, setLabelDraft] = useState({});
  const [savingLabelId, setSavingLabelId] = useState(null);

  const [deletingId, setDeletingId] = useState(null);

  const [testing, setTesting] = useState(false);
  const [testMsg, setTestMsg] = useState('');

  const getErrMsg = (err, fallback) =>
    err?.response?.data?.message || err?.message || fallback;

  const loadSettings = async () => {
    setLoading(true);
    setError('');
    setTestMsg('');
    try {
      const res = await api.get('/api/notification-settings');
      setSettings(res.data);

      // init draft labels
      const draft = {};
      const list = Array.isArray(res.data?.chatIds) ? res.data.chatIds : [];
      list.forEach((c) => {
        draft[c.id] = c.label || '';
      });
      setLabelDraft(draft);
    } catch (err) {
      setError(getErrMsg(err, 'Failed to load notification settings'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSettings();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleToggleEnabled = async (checked) => {
    setEnabledBusy(true);
    setError('');
    setTestMsg('');
    try {
      await api.put('/api/notification-settings/telegram/enabled', {
        enabled: checked,
      });
      await loadSettings();
    } catch (err) {
      setError(getErrMsg(err, 'Failed to update enabled setting'));
    } finally {
      setEnabledBusy(false);
    }
  };

  const handleUpdateToken = async () => {
    if (!botToken.trim()) return;
    setSavingToken(true);
    setError('');
    setTestMsg('');
    try {
      await api.put('/api/notification-settings/telegram/bot-token', {
        botToken: botToken.trim(),
      });
      setBotToken('');
      await loadSettings();
    } catch (err) {
      setError(getErrMsg(err, 'Failed to update bot token'));
    } finally {
      setSavingToken(false);
    }
  };

  const handleAddChatId = async () => {
    if (!newChatId.trim()) return;
    setAddingChat(true);
    setError('');
    setTestMsg('');
    try {
      await api.post('/api/notification-settings/telegram/chat-ids', {
        chatId: newChatId.trim(),
        label: newChatLabel.trim() ? newChatLabel.trim() : undefined,
      });
      setNewChatId('');
      setNewChatLabel('');
      await loadSettings();
    } catch (err) {
      setError(getErrMsg(err, 'Failed to add chat id'));
    } finally {
      setAddingChat(false);
    }
  };

  const handleSaveLabel = async (id) => {
    setSavingLabelId(id);
    setError('');
    setTestMsg('');
    try {
      await api.put(`/api/notification-settings/telegram/chat-ids/${id}`, {
        label: labelDraft[id] || '',
      });
      await loadSettings();
    } catch (err) {
      setError(getErrMsg(err, 'Failed to update label'));
    } finally {
      setSavingLabelId(null);
    }
  };

  const handleDeleteChatId = async (id) => {
    setDeletingId(id);
    setError('');
    setTestMsg('');
    try {
      await api.delete(`/api/notification-settings/telegram/chat-ids/${id}`);
      await loadSettings();
    } catch (err) {
      setError(getErrMsg(err, 'Failed to delete chat id'));
    } finally {
      setDeletingId(null);
    }
  };

  const handleTest = async () => {
    setTesting(true);
    setError('');
    setTestMsg('');
    try {
      const res = await api.post('/api/notification-settings/telegram/test');
      const msg = res?.data?.message || (res?.data?.success ? 'Success' : 'Failed');
      setTestMsg(msg);
    } catch (err) {
      setError(getErrMsg(err, 'Telegram test failed'));
    } finally {
      setTesting(false);
    }
  };

  const enabled = !!settings?.isTelegramEnabled;
  const hasBotToken = !!settings?.hasBotToken;
  const updatedAt = settings?.updatedAtUtc
    ? new Date(settings.updatedAtUtc).toLocaleString()
    : '';

  return (
    <div>
      <div className="page-header">
        <h1 className="page-title">Settings</h1>
        <button className="btn" onClick={loadSettings} disabled={loading}>
          Refresh
        </button>
      </div>

      {error ? <div className="error-box">{error}</div> : null}
      {testMsg ? <div className="notice">{testMsg}</div> : null}

      <div className="card">
        <h2 style={{ marginTop: 0 }}>Telegram Notification Settings</h2>
        <p className="small" style={{ marginTop: 6 }}>
          Manage the bot token, chat ID list, and Telegram notifications.
        </p>

        <div className="action-row" style={{ marginTop: 10 }}>
          <span className={`badge ${enabled ? 'badge-ok' : 'badge-muted'}`}>
            Telegram: {enabled ? 'Enabled' : 'Disabled'}
          </span>
          <span className={`badge ${hasBotToken ? 'badge-ok' : 'badge-warn'}`}>
            Bot token: {hasBotToken ? 'Set' : 'Not set'}
          </span>
          {updatedAt ? <span className="badge badge-muted">Updated: {updatedAt}</span> : null}
        </div>

        {loading ? (
          <div style={{ marginTop: 14 }}>Loading…</div>
        ) : (
          <>
            {/* Enable/Disable */}
            <div style={{ marginTop: 16 }}>
              <h3 style={{ marginBottom: 8 }}>Enable / Disable</h3>
              <label style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                <input
                  type="checkbox"
                  checked={enabled}
                  disabled={enabledBusy}
                  onChange={(e) => handleToggleEnabled(e.target.checked)}
                />
                <span className="small">Enable Telegram notifications</span>
              </label>
              {!hasBotToken && (
                <div className="small" style={{ marginTop: 8 }}>
                  Note: If there is no bot token, the enable request may be rejected by the backend.
                </div>
              )}
            </div>

            {/* Bot Token */}
            <div style={{ marginTop: 16 }}>
              <h3 style={{ marginBottom: 8 }}>Bot Token</h3>
              <div className="form-row">
                <div className="input-group" style={{ flex: 1 }}>
                  <label>Bot Token</label>
                  <input
                    type="password"
                    value={botToken}
                    onChange={(e) => setBotToken(e.target.value)}
                    placeholder={hasBotToken ? 'Token already set (enter to replace)' : 'Paste token from BotFather'}
                  />
                </div>
                <button
                  className="btn btn-primary"
                  onClick={handleUpdateToken}
                  disabled={savingToken || !botToken.trim()}
                >
                  {savingToken ? 'Saving…' : 'Update Token'}
                </button>
              </div>
            </div>

            {/* Chat IDs */}
            <div style={{ marginTop: 16 }}>
              <h3 style={{ marginBottom: 8 }}>Chat IDs</h3>

              <div className="form-row">
                <div className="input-group">
                  <label>Chat ID</label>
                  <input
                    value={newChatId}
                    onChange={(e) => setNewChatId(e.target.value)}
                    placeholder="123456789"
                  />
                </div>
                <div className="input-group">
                  <label>Label (optional)</label>
                  <input
                    value={newChatLabel}
                    onChange={(e) => setNewChatLabel(e.target.value)}
                    placeholder="Prod Alerts"
                  />
                </div>
                <button
                  className="btn btn-primary"
                  onClick={handleAddChatId}
                  disabled={addingChat || !newChatId.trim()}
                >
                  {addingChat ? 'Adding…' : 'Add'}
                </button>
              </div>

              <div style={{ marginTop: 12 }} className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th style={{ width: 70 }}>ID</th>
                      <th>Chat ID</th>
                      <th>Label</th>
                      <th style={{ width: 220 }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(settings?.chatIds || []).length === 0 ? (
                      <tr>
                        <td colSpan={4} className="small">
                          No chat IDs configured.
                        </td>
                      </tr>
                    ) : (
                      (settings.chatIds || []).map((c) => (
                        <tr key={c.id}>
                          <td>{c.id}</td>
                          <td>{c.chatId}</td>
                          <td>
                            <input
                              value={labelDraft[c.id] ?? ''}
                              onChange={(e) =>
                                setLabelDraft((prev) => ({ ...prev, [c.id]: e.target.value }))
                              }
                              style={{
                                width: '100%',
                                padding: '0.45rem',
                                borderRadius: 6,
                                border: '1px solid #334155',
                                background: '#020617',
                                color: '#e5e7eb',
                              }}
                            />
                          </td>
                          <td>
                            <div className="action-row">
                              <button
                                className="btn btn-muted"
                                onClick={() => handleSaveLabel(c.id)}
                                disabled={savingLabelId === c.id}
                              >
                                {savingLabelId === c.id ? 'Saving…' : 'Save'}
                              </button>
                              <button
                                className="btn btn-danger"
                                onClick={() => handleDeleteChatId(c.id)}
                                disabled={deletingId === c.id}
                              >
                                {deletingId === c.id ? 'Deleting…' : 'Delete'}
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Test */}
            <div style={{ marginTop: 16 }}>
              <h3 style={{ marginBottom: 8 }}>Test Telegram</h3>
              <button className="btn btn-warning" onClick={handleTest} disabled={testing}>
                {testing ? 'Testing…' : 'Test Connection'}
              </button>
              <div className="small" style={{ marginTop: 8 }}>
                Test verifies that the backend can connect to the Telegram bot.
              </div>
            </div>
          </>
        )}
      </div>
      {/* Backups (REST + SignalR) */}
      <BackupsPanel />
    </div>
  );
};

export default SettingsPage;
