// src/components/layout/AppLayout.jsx
import React from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

const AppLayout = () => {
  const { user, logout } = useAuth();

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="app-logo">Server Health & Management</div>

        <nav className="app-nav">
          <NavLink to="/" end>
            Dashboard
          </NavLink>
          <NavLink to="/server-info">Server Info</NavLink>
          <NavLink to="/services">Services</NavLink>
          <NavLink to="/processes">Processes</NavLink>          <NavLink to="/agents">Agents</NavLink>

          <NavLink to="/notifications">Notifications</NavLink>
          <NavLink to="/alert-rules">Alert Rules</NavLink>

          <NavLink to="/settings">Settings</NavLink>
        </nav>

        <div className="app-user">
          <span>{user?.fullName}</span>
          <button type="button" className="btn btn-danger" onClick={logout}>
            Logout
          </button>
        </div>
      </header>

      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
};

export default AppLayout;
