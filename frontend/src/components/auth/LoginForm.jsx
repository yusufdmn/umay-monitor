// src/components/auth/LoginForm.jsx
import React, { useState } from 'react';
import logo from '../logo.png';

const LoginForm = ({ onSubmit, loading, error }) => {
  const [password, setPassword] = useState('');

  const handleSubmit = (e) => {
    e.preventDefault();
    onSubmit(password);
  };

  return (
    <div className="login-container">
      <form className="login-card" onSubmit={handleSubmit}>
        <div style={{ textAlign: 'center', marginBottom: '3rem' }}>
          <img 
  src={logo} 
  alt="Umay Monitor" 
  style={{ 
    width: '240px', 
    height: '240px', 
    objectFit: 'contain',
    transform: 'scale(2)'
  }} 
/>
        </div>
        <h2>Admin Login</h2>

        <div className="input-group">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Enter admin password"
            autoFocus
          />
        </div>

        {error && <div className="error-text">{error}</div>}

        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? 'Logging in…' : 'Login'}
        </button>
      </form>
    </div>
  );
};

export default LoginForm;
