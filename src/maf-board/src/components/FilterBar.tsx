import type { TaskStatus } from '../types';
import type { TaskFilter } from '../api';

interface Props {
  filter: TaskFilter;
  onChange: (filter: TaskFilter) => void;
  onRefresh: () => void;
  loading: boolean;
}

const statusOptions: { label: string; value: TaskStatus | '' }[] = [
  { label: '全部状态', value: '' },
  { label: '已完成', value: 'completed' },
  { label: '运行中', value: 'running' },
  { label: '待执行', value: 'pending' },
  { label: '失败', value: 'failed' },
];

export function FilterBar({ filter, onChange, onRefresh, loading }: Props) {
  return (
    <div
      style={{
        display: 'flex',
        gap: 12,
        alignItems: 'center',
        padding: '12px 0',
        flexWrap: 'wrap',
      }}
    >
      {/* 搜索框 */}
      <input
        type="text"
        placeholder="🔍 搜索任务、Agent、用户输入..."
        value={filter.search ?? ''}
        onChange={(e) => onChange({ ...filter, search: e.target.value || undefined })}
        style={{
          flex: 1,
          minWidth: 220,
          padding: '8px 12px',
          border: '1px solid #d1d5db',
          borderRadius: 6,
          fontSize: 14,
          outline: 'none',
        }}
      />

      {/* 状态筛选 */}
      <select
        value={filter.status ?? ''}
        onChange={(e) =>
          onChange({ ...filter, status: (e.target.value as TaskStatus) || undefined })
        }
        style={{
          padding: '8px 12px',
          border: '1px solid #d1d5db',
          borderRadius: 6,
          fontSize: 14,
          backgroundColor: '#fff',
          cursor: 'pointer',
        }}
      >
        {statusOptions.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>

      {/* Agent 筛选 */}
      <input
        type="text"
        placeholder="Agent 名称"
        value={filter.agentName ?? ''}
        onChange={(e) => onChange({ ...filter, agentName: e.target.value || undefined })}
        style={{
          width: 150,
          padding: '8px 12px',
          border: '1px solid #d1d5db',
          borderRadius: 6,
          fontSize: 14,
          outline: 'none',
        }}
      />

      {/* 刷新按钮 */}
      <button
        onClick={onRefresh}
        disabled={loading}
        style={{
          padding: '8px 16px',
          backgroundColor: loading ? '#9ca3af' : '#6366f1',
          color: '#fff',
          border: 'none',
          borderRadius: 6,
          fontSize: 14,
          cursor: loading ? 'not-allowed' : 'pointer',
          whiteSpace: 'nowrap',
        }}
      >
        {loading ? '⏳ 加载中...' : '🔄 刷新'}
      </button>
    </div>
  );
}
