// src/pages/ServicesPage.jsx
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import api from '../api/axiosConfig';
import { useMonitoring } from '../context/MonitoringContext';
import signalRService from '../services/signalRService';
import { useAuth } from '../context/AuthContext';
import ServiceList from '../components/services/ServiceList';
import ServiceDetail from '../components/services/ServiceDetail';
import ServerSelect from '../components/common/ServerSelect';

const unwrap = (resData) => {
  if (resData && typeof resData === 'object' && 'status' in resData) {
    if (resData.status !== 'ok') throw new Error(resData.message || 'Server returned error');
    return resData.data;
  }
  return resData;
};

const friendlyMessage = (err, fallback = 'Failed') => {
  const status = err?.response?.status;
  const msg = err?.response?.data?.message || err?.message || fallback;
  if (status === 503 || /not\s*connected/i.test(String(msg))) return 'Server is not connected';
  return String(msg);
};

const ServicesPage = () => {
  const { token } = useAuth();
  const { selectedServerId, setSelectedServerId, ensureSubscribed } = useMonitoring();

  const [pageError, setPageError] = useState('');
  const [pageNotice, setPageNotice] = useState('');

  const [services, setServices] = useState([]);
  const [selectedName, setSelectedName] = useState(null);
  const [details, setDetails] = useState(null);
  const [logs, setLogs] = useState([]);

  // Watchlist (simple Watch / Watched toggle)
  const [watchlistConfig, setWatchlistConfig] = useState({ services: [], processes: [] });
  const [watchBusyName, setWatchBusyName] = useState(null);

  const [loadingServices, setLoadingServices] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [loadingLogs, setLoadingLogs] = useState(false);
  const [restarting, setRestarting] = useState(false);

  const normalizeServiceKey = useCallback((name) => {
    // Some backends may return "nginx" while service lists may show "nginx.service".
    // Normalize to keep the Watch toggle stable.
    return String(name || '')
      .trim()
      .replace(/\.service$/i, '')
      .toLowerCase();
  }, []);

  const loadWatchlistConfig = useCallback(
    async (serverId = selectedServerId) => {
      if (!token || !serverId) {
        setWatchlistConfig({ services: [], processes: [] });
        return;
      }

      try {
        const res = await api.get(`/api/servers/${serverId}/watchlist`);
        const data = unwrap(res.data) || {};
        setWatchlistConfig({
          services: Array.isArray(data.services) ? data.services : [],
          processes: Array.isArray(data.processes) ? data.processes : [],
        });
      } catch (err) {
        // Keep this silent to avoid noisy UI on page load.
        // (Errors are surfaced on user-triggered actions.)
      }
    },
    [selectedServerId, token]
  );

  const watchedServices = useMemo(() => {
    const set = new Set();
    (watchlistConfig.services || []).forEach((s) => {
      const k = normalizeServiceKey(s);
      if (k) set.add(k);
    });
    return set;
  }, [watchlistConfig.services, normalizeServiceKey]);

  const toggleServiceWatch = useCallback(
    async (serviceName) => {
      const serverId = selectedServerId;
      if (!token || !serverId) {
        setPageError('Select a server first');
        return;
      }
      const key = normalizeServiceKey(serviceName);
      const isWatched = key ? watchedServices.has(key) : false;
      const encodedName = encodeURIComponent(serviceName);

      try {
        setPageError('');
        setPageNotice('');
        setWatchBusyName(serviceName);

        if (isWatched) {
          await api.delete(`/api/servers/${serverId}/watchlist/services/${encodedName}`);
        } else {
          await api.post(`/api/servers/${serverId}/watchlist/services/${encodedName}`);
        }

        // Optimistic update so the UI reflects the change instantly
        setWatchlistConfig((prev) => {
          const prevServices = Array.isArray(prev?.services) ? prev.services : [];
          if (!key) return prev;
          if (isWatched) {
            return {
              ...prev,
              services: prevServices.filter((s) => normalizeServiceKey(s) !== key),
            };
          }
          const exists = prevServices.some((s) => normalizeServiceKey(s) === key);
          return exists ? prev : { ...prev, services: [...prevServices, serviceName] };
        });

        // Also refresh from backend to keep source-of-truth in sync
        loadWatchlistConfig(serverId);
      } catch (err) {
        setPageError(friendlyMessage(err, 'Watch operation failed'));
      } finally {
        setWatchBusyName(null);
      }
    },
    [loadWatchlistConfig, normalizeServiceKey, selectedServerId, token, watchedServices]
  );

  const loadServices = async () => {
    if (selectedServerId == null) {
      setServices([]);
      setSelectedName(null);
      setDetails(null);
      setLogs([]);
      setPageError('Please select a server');
      return;
    }
    try {
      setLoadingServices(true);
      setPageError('');
      setPageNotice('');

      await ensureSubscribed(selectedServerId);

      const res = await api.get(`/api/servers/${selectedServerId}/services`);
      const data = unwrap(res.data) || [];
      const list = Array.isArray(data) ? data : [];
      setServices(list);

      if (!selectedName && list[0]?.name) setSelectedName(list[0].name);
    } catch (err) {
      console.error(err);
      setPageError(friendlyMessage(err, 'Failed to load services'));
      setServices([]);
      setSelectedName(null);
      setDetails(null);
      setLogs([]);
    } finally {
      setLoadingServices(false);
    }
  };

  const loadDetails = async (name) => {
    if (selectedServerId == null) {
      setSelectedName(null);
      setDetails(null);
      setPageError('Please select a server');
      return;
    }
    if (!name) return;
    try {
      setLoadingDetail(true);
      setPageError('');

      await ensureSubscribed(selectedServerId);

      const res = await api.get(
        `/api/servers/${selectedServerId}/services/${encodeURIComponent(name)}`
      );
      setDetails(unwrap(res.data));
    } catch (err) {
      console.error(err);
      setPageError(friendlyMessage(err, 'Service details failed'));
      setDetails(null);
    } finally {
      setLoadingDetail(false);
    }
  };

  const loadLogs = async (name) => {
    if (selectedServerId == null) {
      setLogs([]);
      setPageError('Please select a server');
      return;
    }
    if (!name) return;
    try {
      setLoadingLogs(true);
      setPageError('');

      await ensureSubscribed(selectedServerId);

      const res = await api.get(
        `/api/servers/${selectedServerId}/services/${encodeURIComponent(name)}/logs`
      );
      const data = unwrap(res.data) || [];
      setLogs(Array.isArray(data) ? data.map((x) => x.log) : []);
    } catch (err) {
      console.error(err);
      setPageError(friendlyMessage(err, 'Service logs failed'));
      setLogs([]);
    } finally {
      setLoadingLogs(false);
    }
  };

  const restart = async () => {
    if (!selectedName) return;

    try {
      setRestarting(true);
      setPageError('');
      setPageNotice('');

      // listen for command result (restart-service)
      // Some builds may not expose explicit "off" helpers; keep it safe.
      signalRService.offCommandSuccess?.();
      signalRService.offCommandFailed?.();

      signalRService.onCommandSuccess((evt) => {
        if (evt?.action === 'restart-service') {
          setRestarting(false);
          setPageError('');
          setPageNotice(evt?.message || 'Service restarted');
          loadServices();
          loadDetails(selectedName);
        }
      });

      signalRService.onCommandFailed((evt) => {
        if (evt?.action === 'restart-service') {
          setRestarting(false);
          setPageNotice('');
          setPageError(String(evt?.message || 'Restart failed'));
        }
      });

      // ensure signalr connected
      if (token && !signalRService.isConnected()) {
        await signalRService.connect(token);
      }

      await ensureSubscribed(selectedServerId);

      await api.post(
        `/api/servers/${selectedServerId}/services/${encodeURIComponent(selectedName)}/restart`,
        {}
      );

      setPageNotice(`Restart request sent: ${selectedName}`);

      // safety timeout
      setTimeout(() => setRestarting(false), 35000);
    } catch (err) {
      console.error(err);
      setPageNotice('');
      setPageError(friendlyMessage(err, 'Restart failed'));
      setRestarting(false);
    }
  };

  useEffect(() => {
    loadServices();
    loadWatchlistConfig();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedServerId]);

  useEffect(() => {
    if (selectedName) {
      loadDetails(selectedName);
      setLogs([]);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedName]);

  return (
    <div>
      <div className="page-header">
          <h1 className="page-title">Services</h1>

        <div className="action-row">
          <ServerSelect label="Server" minWidth={360} value={selectedServerId} onChange={setSelectedServerId} />
          <button
            type="button"
            className="btn btn-muted"
            onClick={loadServices}
            disabled={loadingServices}
          >
            {loadingServices ? 'Refreshing…' : 'Refresh List'}
          </button>
        </div>
      </div>

      {pageError ? <div className="error-box">{pageError}</div> : null}
      {pageNotice ? <div className="notice">{pageNotice}</div> : null}

      <div className="services-page">
        <ServiceList
          services={services}
          loading={loadingServices}
          selectedServiceName={selectedName}
          onSelect={setSelectedName}
          watchedServices={watchedServices}
          onToggleWatch={toggleServiceWatch}
          watchBusyName={watchBusyName}
        />
        <ServiceDetail
          serverId={selectedServerId}
          service={details}
          logs={logs}
          loadingDetail={loadingDetail}
          loadingLogs={loadingLogs}
          restarting={restarting}
          onRestart={restart}
          onRefreshDetails={() => loadDetails(selectedName)}
          onLoadLogs={() => loadLogs(selectedName)}
        />
      </div>
    </div>
  );
};

export default ServicesPage;
