// src/api/axiosConfig.js
import axios from 'axios';

export const API_BASE_URL =
  process.env.REACT_APP_API_BASE_URL || 'https://localhost:7287';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000, // services logs 15s, backend retry up to 30s
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Network / SSL / CORS errors => error.response undefined
    if (!error.response) {
      console.error('Network/API error:', error);
    }
    if (error.response?.status === 401) {
      localStorage.clear();
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
