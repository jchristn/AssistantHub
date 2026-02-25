import React from 'react';
import Modal from './Modal';
import './HealthHistogram.css';

export function HealthHistogram({ history, width = 120, height = 24 }) {
  if (!history || history.length === 0) return <span className="text-muted">No data</span>;

  const now = new Date();
  const sorted = [...history].sort((a, b) => new Date(a.TimestampUtc) - new Date(b.TimestampUtc));
  const oldest = new Date(sorted[0].TimestampUtc);
  const spanMs = now - oldest;
  const spanHours = spanMs / (1000 * 60 * 60);

  let buckets = [];
  if (spanHours < 1) {
    buckets = sorted.map(r => ({ success: r.Success ? 1 : 0, fail: r.Success ? 0 : 1, time: r.TimestampUtc }));
  } else {
    const bucketMs = spanHours <= 6 ? 60000 : 300000;
    const bucketMap = new Map();
    for (const r of sorted) {
      const t = new Date(r.TimestampUtc).getTime();
      const key = Math.floor(t / bucketMs);
      if (!bucketMap.has(key)) bucketMap.set(key, { success: 0, fail: 0 });
      const b = bucketMap.get(key);
      if (r.Success) b.success++; else b.fail++;
    }
    for (const [key, val] of bucketMap) {
      buckets.push({ ...val, time: new Date(key * bucketMs).toISOString() });
    }
  }

  const maxBars = Math.floor(width / 6);
  if (buckets.length > maxBars) {
    buckets = buckets.slice(-maxBars);
  }
  const barWidth = Math.max(4, Math.floor(width / buckets.length) - 2);

  return (
    <div style={{ display: 'flex', alignItems: 'flex-end', gap: '2px', height: height + 'px', maxWidth: width + 'px', overflow: 'hidden' }}>
      {buckets.map((b, i) => {
        let color = '#4caf50';
        if (b.fail > 0 && b.success === 0) color = '#f44336';
        else if (b.fail > 0 && b.success > 0) color = '#ff9800';
        const title = `${new Date(b.time).toLocaleTimeString()} - ${b.success} ok, ${b.fail} fail`;
        return <div key={i} title={title} style={{ width: barWidth + 'px', height: height + 'px', backgroundColor: color, borderRadius: '1px' }} />;
      })}
    </div>
  );
}

export function formatDuration(ms) {
  const hours = Math.floor(ms / 3600000);
  const minutes = Math.floor((ms % 3600000) / 60000);
  if (hours > 0) return `${hours}h ${minutes}m`;
  return `${minutes}m`;
}

export function HealthDetailModal({ isOpen, onClose, healthData }) {
  if (!isOpen || !healthData) return null;

  const uptimePct = healthData.UptimePercentage != null ? healthData.UptimePercentage.toFixed(2) + '%' : 'N/A';
  const history = healthData.History || [];
  const spanMs = history.length > 0
    ? new Date() - new Date([...history].sort((a, b) => new Date(a.TimestampUtc) - new Date(b.TimestampUtc))[0].TimestampUtc)
    : 0;
  const spanStr = spanMs > 0 ? formatDuration(spanMs) : 'No data';

  return (
    <Modal title={`Health: ${healthData.EndpointName}`} onClose={onClose} wide>
      <div className="health-modal">
        <div className="health-stats-row">
          <div className="health-stat-card">
            <div className="health-stat-label">Status</div>
            <div className="health-stat-value">
              <span className={`status-badge ${healthData.IsHealthy ? 'active' : 'inactive'}`}>
                {healthData.IsHealthy ? 'Healthy' : 'Unhealthy'}
              </span>
            </div>
          </div>
          <div className="health-stat-card">
            <div className="health-stat-label">Uptime</div>
            <div className="health-stat-value">{uptimePct}</div>
          </div>
          <div className="health-stat-card">
            <div className="health-stat-label">History Span</div>
            <div className="health-stat-value">{spanStr}</div>
          </div>
          <div className="health-stat-card">
            <div className="health-stat-label">Consecutive OK</div>
            <div className="health-stat-value health-stat-success">{healthData.ConsecutiveSuccesses}</div>
          </div>
          <div className="health-stat-card">
            <div className="health-stat-label">Consecutive Fail</div>
            <div className="health-stat-value health-stat-danger">{healthData.ConsecutiveFailures}</div>
          </div>
        </div>

        {healthData.LastError && (
          <div className="health-error-box">
            <div className="health-error-label">Last Error</div>
            <div className="health-error-message">{healthData.LastError}</div>
          </div>
        )}

        <div className="health-histogram-section">
          <div className="health-section-label">Health History</div>
          <div className="health-histogram-container">
            <HealthHistogram history={history} width={770} height={36} />
          </div>
        </div>

        <div className="health-timestamps">
          <div className="health-timestamp-item">
            <span className="health-timestamp-label">First check</span>
            <span className="health-timestamp-value">{healthData.FirstCheckUtc ? new Date(healthData.FirstCheckUtc).toLocaleString() : 'N/A'}</span>
          </div>
          <div className="health-timestamp-item">
            <span className="health-timestamp-label">Last check</span>
            <span className="health-timestamp-value">{healthData.LastCheckUtc ? new Date(healthData.LastCheckUtc).toLocaleString() : 'N/A'}</span>
          </div>
          <div className="health-timestamp-item">
            <span className="health-timestamp-label">Last healthy</span>
            <span className="health-timestamp-value">{healthData.LastHealthyUtc ? new Date(healthData.LastHealthyUtc).toLocaleString() : 'N/A'}</span>
          </div>
          <div className="health-timestamp-item">
            <span className="health-timestamp-label">Last unhealthy</span>
            <span className="health-timestamp-value">{healthData.LastUnhealthyUtc ? new Date(healthData.LastUnhealthyUtc).toLocaleString() : 'N/A'}</span>
          </div>
        </div>
      </div>
    </Modal>
  );
}
