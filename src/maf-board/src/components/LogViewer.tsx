import type { AgentLogEntry } from '../types';

const levelStyle: Record<string, { icon: string; color: string }> = {
  info: { icon: 'ℹ️', color: '#2563eb' },
  success: { icon: '✅', color: '#16a34a' },
  warning: { icon: '⚠️', color: '#d97706' },
  error: { icon: '❌', color: '#dc2626' },
};

export function LogViewer({ logs }: { logs: AgentLogEntry[] }) {
  if (logs.length === 0) {
    return <p style={{ color: '#9ca3af', fontSize: 13 }}>暂无日志</p>;
  }

  return (
    <div style={{ fontFamily: 'monospace', fontSize: 13 }}>
      {logs.map((log) => {
        const style = levelStyle[log.status] ?? levelStyle.info;
        return (
          <div
            key={log.id}
            style={{
              display: 'flex',
              gap: 8,
              padding: '4px 0',
              borderBottom: '1px solid #f3f4f6',
              alignItems: 'flex-start',
            }}
          >
            <span style={{ flexShrink: 0 }}>{style.icon}</span>
            <span style={{ color: '#9ca3af', flexShrink: 0, fontSize: 11 }}>
              {new Date(log.timestamp).toLocaleTimeString('zh-CN')}
            </span>
            <span
              style={{
                color: '#6366f1',
                fontWeight: 600,
                flexShrink: 0,
                minWidth: 120,
              }}
            >
              [{log.agentName}]
            </span>
            <span style={{ color: style.color }}>{log.action}</span>
            <span style={{ color: '#374151', flex: 1 }}>{log.message}</span>
            {log.duration != null && (
              <span style={{ color: '#9ca3af', flexShrink: 0, fontSize: 11 }}>
                {log.duration}ms
              </span>
            )}
          </div>
        );
      })}
    </div>
  );
}
