// src/components/dashboard/Dashboard.jsx
import React from 'react';
import MetricsOverview from './MetricsOverview';
import ServerSelect from '../common/ServerSelect';

const Dashboard = ({
  metrics,
  history,
  selectedServerId,
  isSubscribed,
  onChangeServer,
  connecting,
  subscribing,
  lastError,
  onClearHistory,
}) => {
  /* ---------- Subscription status ---------- */
  /*
  const statusText = connecting
    ? 'Connecting…'
    : subscribing
    ? 'Subscribing…'
    : isSubscribed
    ? 'Live'
    : 'Not subscribed';

  const statusClass = connecting || subscribing
    ? 'bg-slate-800/60 text-slate-200 ring-1 ring-slate-700'
    : isSubscribed
    ? 'bg-emerald-500/15 text-emerald-200 ring-1 ring-emerald-500/25'
    : 'bg-amber-500/15 text-amber-200 ring-1 ring-amber-500/25';
  */
  return (
    <div className="space-y-4">
      {/* Top bar */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 12,
        }}
      >
  
        {/* Left: status */}
        {/*
        <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 12 }}>
          <span className={`inline-flex items-center rounded-full px-3 py-1 text-sm ${statusClass}`}>
            <span className="mr-2 inline-block h-2 w-2 rounded-full bg-current opacity-80" />
            {statusText}
          </span>
        </div>
        */}

        {/* Right: Select Server aligned right */}
        <div
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'center',
            gap: 12,
            marginLeft: 'auto',
            justifyContent: 'flex-end',
          }}
        >
          <ServerSelect
            label="Select Server"
            value={selectedServerId}
            onChange={onChangeServer}
            showHostname
            showIp
            minWidth={360}
          />

          <button
            type="button"
            onClick={onClearHistory}
            className="btn btn-secondary"
            title="Clear chart history"
          >
            Clear History
          </button>
        </div>
      </div>

      {lastError ? (
        <div className="rounded-xl border border-amber-500/20 bg-amber-500/10 px-4 py-3 text-amber-100">
          {lastError}
        </div>
      ) : null}

      <MetricsOverview metrics={metrics} history={history} />
    </div>
  );
};

export default Dashboard;
