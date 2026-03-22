import type { TaskStatus } from '../types';

const statusConfig: Record<TaskStatus, { label: string; color: string; bg: string }> = {
  pending: { label: '待执行', color: '#6b7280', bg: '#f3f4f6' },
  running: { label: '执行中', color: '#2563eb', bg: '#dbeafe' },
  completed: { label: '已完成', color: '#16a34a', bg: '#dcfce7' },
  failed: { label: '失败', color: '#dc2626', bg: '#fee2e2' },
  cancelled: { label: '已取消', color: '#9ca3af', bg: '#f9fafb' },
};

export function StatusBadge({ status }: { status: TaskStatus }) {
  const config = statusConfig[status];
  return (
    <span
      style={{
        display: 'inline-block',
        padding: '2px 10px',
        borderRadius: 12,
        fontSize: 12,
        fontWeight: 600,
        color: config.color,
        backgroundColor: config.bg,
      }}
    >
      {config.label}
    </span>
  );
}
