// src/components/services/ServiceList.jsx
import React, { useMemo, useState } from 'react';

const EMPTY = [];

const getServiceName = (s) => s?.name || s?.serviceName || s?.unitName || '';
const norm = (v) => String(v || '').toLowerCase().trim();
const watchKey = (name) => norm(String(name || '').replace(/\.service$/i, ''));

const chipToneClass = (key) => {
  switch (key) {
    case 'active':
      return 'chip-tone chip-green';
    case 'inactive':
      return 'chip-tone chip-amber';
    case 'failed':
      return 'chip-tone chip-red';
    case 'unknown':
      return 'chip-tone chip-gray';
    default:
      return 'chip-tone';
  }
};

const ServiceList = (props) => {
  // Backward/forward compatible props
  const {
    services,
    loading,
    onSelect,
    selectedServiceName,
    watchedServices,
    onToggleWatch,
    watchBusyName,
    selected: selectedProp,
  } = props;

  const selected = selectedServiceName ?? selectedProp ?? null;

  const [q, setQ] = useState('');
  const [statusFilter, setStatusFilter] = useState('all'); // all | active | inactive | failed | unknown

  // ✅ Fix eslint warning: keep list reference stable via useMemo
  const list = useMemo(() => (Array.isArray(services) ? services : EMPTY), [services]);

  const counts = useMemo(() => {
    const c = { all: list.length, active: 0, inactive: 0, failed: 0, unknown: 0 };
    for (const s of list) {
      const a = norm(s?.activeState);
      if (a === 'active') c.active += 1;
      else if (a === 'inactive') c.inactive += 1;
      else if (a === 'failed') c.failed += 1;
      else c.unknown += 1;
    }
    return c;
  }, [list]);

  const selectedObj = useMemo(() => {
    if (!selected) return null;
    return list.find((s) => getServiceName(s) === selected) || null;
  }, [list, selected]);

  const filtered = useMemo(() => {
    const query = norm(q);

    return list.filter((s) => {
      const name = norm(getServiceName(s));
      const active = norm(s?.activeState);
      const sub = norm(s?.subState);

      if (statusFilter !== 'all') {
        if (statusFilter === 'unknown') {
          if (active === 'active' || active === 'inactive' || active === 'failed') return false;
        } else if (active !== statusFilter) {
          return false;
        }
      }

      if (!query) return true;
      return name.includes(query) || active.includes(query) || sub.includes(query);
    });
  }, [list, q, statusFilter]);

  const isSelectedHidden = useMemo(() => {
    if (!selected) return false;
    return !filtered.some((s) => getServiceName(s) === selected);
  }, [filtered, selected]);

  const renderRow = (s, { pinned = false } = {}) => {
    const name = getServiceName(s);
		const wk = watchKey(name);
		const isWatched = !!watchedServices?.has?.(wk);
		const isWatchBusy = !!watchBusyName && watchKey(watchBusyName) === wk;
    const isSelected = selected === name;

    const active = s?.activeState || 'unknown';
    const sub = s?.subState || '';

    const badgeClass =
      active === 'active'
        ? 'badge badge-ok'
        : active === 'inactive'
        ? 'badge badge-warn'
        : active === 'failed'
        ? 'badge badge-bad'
        : 'badge badge-muted';

    return (
      <li
        key={`${pinned ? 'pinned-' : ''}${name}`}
        className={`service-item ${isSelected ? 'selected' : ''} ${pinned ? 'pinned' : ''}`}
        onClick={() => onSelect?.(name)}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') onSelect?.(name);
        }}
        title={name}
      >
        <span className="service-name">
          {pinned ? <span className="pinned-pill">PINNED</span> : null}
          {name}
        </span>

			<span className="service-status">
				{onToggleWatch ? (
					<button
						type="button"
						className={`btn ${isWatched ? 'btn-primary' : 'btn-muted'}`}
						style={{ padding: '0.25rem 0.5rem', fontSize: '0.75rem', lineHeight: 1.1, whiteSpace: 'nowrap' }}
						disabled={isWatchBusy}
						onClick={(e) => {
							e.stopPropagation();
							onToggleWatch(name);
						}}
						title={isWatched ? 'Remove from watchlist' : 'Add to watchlist'}
					>
						{isWatchBusy ? '…' : isWatched ? '✓ Watched' : 'Watch'}
					</button>
				) : null}
          <span className={badgeClass}>{active}</span>
          {sub ? <span className="badge badge-muted">{sub}</span> : null}
        </span>
      </li>
    );
  };

  return (
    <div className="service-list">
      <div className="list-header services-list-header">
        <div className="list-title">
          <h2 style={{ margin: 0 }}>Services</h2>
          <div className="muted" style={{ marginTop: 2 }}>
            {loading ? 'Loading…' : `${filtered.length} / ${list.length} items`}
          </div>
        </div>

        <div className="search-wrap">
          <input
            className="search-input"
            placeholder="Search services…"
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />

          {q ? (
            <button
              type="button"
              className="btn btn-ghost btn-icon"
              title="Clear"
              onClick={() => setQ('')}
            >
              ✕
            </button>
          ) : (
            <button type="button" className="btn btn-ghost btn-icon" title="Search" disabled>
              🔎
            </button>
          )}
        </div>
      </div>

      <div className="chip-row">
        <button
          type="button"
          className={`chip ${statusFilter === 'all' ? 'chip-active' : ''}`}
          onClick={() => setStatusFilter('all')}
        >
          All <span className="chip-count">{counts.all}</span>
        </button>

        <button
          type="button"
          className={`chip ${chipToneClass('active')} ${statusFilter === 'active' ? 'chip-active' : ''}`}
          onClick={() => setStatusFilter('active')}
        >
          Active <span className="chip-count">{counts.active}</span>
        </button>

        <button
          type="button"
          className={`chip ${chipToneClass('inactive')} ${statusFilter === 'inactive' ? 'chip-active' : ''}`}
          onClick={() => setStatusFilter('inactive')}
        >
          Inactive <span className="chip-count">{counts.inactive}</span>
        </button>

        <button
          type="button"
          className={`chip ${chipToneClass('failed')} ${statusFilter === 'failed' ? 'chip-active' : ''}`}
          onClick={() => setStatusFilter('failed')}
        >
          Failed <span className="chip-count">{counts.failed}</span>
        </button>

        <button
          type="button"
          className={`chip ${chipToneClass('unknown')} ${statusFilter === 'unknown' ? 'chip-active' : ''}`}
          onClick={() => setStatusFilter('unknown')}
        >
          Unknown <span className="chip-count">{counts.unknown}</span>
        </button>
      </div>

      {loading ? (
        <div className="muted">Loading services…</div>
      ) : (
        <>
          {isSelectedHidden && selectedObj ? (
            <div className="pinned-block">
              <div className="pinned-title">Selected (Pinned)</div>
              <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                {renderRow(selectedObj, { pinned: true })}
              </ul>
              <div className="pinned-hint">
                Selected item is hidden by current filter/search. Clear filters to see it in the list.
              </div>
            </div>
          ) : null}

          {filtered.length ? (
            <ul>{filtered.map((s) => renderRow(s))}</ul>
          ) : (
            <div className="muted">No services found.</div>
          )}
        </>
      )}
    </div>
  );
};

export default ServiceList;
