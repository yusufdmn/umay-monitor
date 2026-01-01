// src/pages/DashboardPage.jsx
import React from 'react';
import Dashboard from '../components/dashboard/Dashboard';
import { useMonitoring } from '../context/MonitoringContext';

const DashboardPage = () => {
  const {
    metrics,
    history,
    selectedServerId,
    setSelectedServerId,
    isSubscribed,
    connecting,
    subscribing,
    clearHistory,
    lastError,
  } = useMonitoring();

  return (
    <Dashboard
      metrics={metrics}
      history={history}
      selectedServerId={selectedServerId}
      isSubscribed={isSubscribed}
      onChangeServer={setSelectedServerId}
      connecting={connecting}
      subscribing={subscribing}
      lastError={lastError}
      onClearHistory={clearHistory}
    />
  );
};

export default DashboardPage;
