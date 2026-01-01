// src/context/AuthContext.jsx
import React, { createContext, useContext, useEffect, useState } from 'react';
import api from '../api/axiosConfig';
import signalRService from '../services/signalRService';

const AuthContext = createContext(null);

const TOKEN_EXPIRY_KEY = 'tokenExpiry';

function isTokenExpired() {
  const expiry = localStorage.getItem(TOKEN_EXPIRY_KEY);
  if (!expiry) return true;
  return new Date(expiry) < new Date();
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [authLoading, setAuthLoading] = useState(true);

  // Load from localStorage
  useEffect(() => {
    const storedToken = localStorage.getItem('authToken');
    const storedUser = localStorage.getItem('user');

    if (storedToken && storedUser && !isTokenExpired()) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
      setIsAuthenticated(true);
    } else {
      localStorage.clear();
    }
    setAuthLoading(false);
  }, []);

  // Periodic expiry check
  useEffect(() => {
    const interval = setInterval(() => {
      if (isTokenExpired() && isAuthenticated) {
        logout();
      }
    }, 60000);
    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated]);

  const login = async (email, password) => {
    const response = await api.post('/api/auth/login', { email, password });

    const data = response.data;
    localStorage.setItem('authToken', data.token);
    localStorage.setItem('user', JSON.stringify(data.user));
    localStorage.setItem(TOKEN_EXPIRY_KEY, data.expiresAt);

    setToken(data.token);
    setUser(data.user);
    setIsAuthenticated(true);

    // Connect SignalR immediately after login
    try {
      await signalRService.connect(data.token);
    } catch (err) {
      console.error('SignalR connect failed:', err);
    }
  };

  const logout = async () => {
    await signalRService.disconnect();
    localStorage.clear();
    setToken(null);
    setUser(null);
    setIsAuthenticated(false);
  };

  const value = {
    user,
    token,
    isAuthenticated,
    authLoading,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
