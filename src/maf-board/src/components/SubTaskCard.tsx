import { useState } from 'react';
import type { SubTask } from '../types';
import { StatusBadge } from './StatusBadge';
import { LogViewer } from './LogViewer';

export function SubTaskCard({ subTask }: { subTask: SubTask }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: 8,
        padding: 12,
        marginBottom: 8,
        backgroundColor: '#fafafa',
      }}
    >
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          cursor: 'pointer',
        }}
        onClick={() => setExpanded(!expanded)}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ fontSize: 12, color: '#9ca3af' }}>{expanded ? '▼' : '▶'}</span>
          <span style={{ fontWeight: 600, fontSize: 14 }}>{subTask.name}</span>
          <span
            style={{
              fontSize: 12,
              color: '#6366f1',
              backgroundColor: '#eef2ff',
              padding: '1px 6px',
              borderRadius: 4,
            }}
          >
            {subTask.agentName}
          </span>
        </div>
        <StatusBadge status={subTask.status} />
      </div>

      {subTask.result && (
        <p style={{ margin: '6px 0 0 20px', fontSize: 13, color: '#374151' }}>
          {subTask.result}
        </p>
      )}

      {subTask.error && (
        <p style={{ margin: '6px 0 0 20px', fontSize: 13, color: '#dc2626' }}>
          ❌ {subTask.error}
        </p>
      )}

      {expanded && (
        <div style={{ marginTop: 8, marginLeft: 20 }}>
          <p style={{ fontSize: 12, color: '#6b7280', marginBottom: 4 }}>{subTask.description}</p>
          <h5 style={{ fontSize: 12, color: '#374151', margin: '8px 0 4px' }}>执行日志</h5>
          <LogViewer logs={subTask.logs} />
        </div>
      )}
    </div>
  );
}
