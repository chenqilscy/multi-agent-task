import { useState, useMemo } from 'react';
import type { TaskFilter } from './api';
import { useTasks } from './hooks';
import { TaskPanel } from './components/TaskPanel';
import { FilterBar } from './components/FilterBar';

const AUTO_REFRESH_MS = 10_000; // 10 秒自动刷新

export default function App() {
  const [filter, setFilter] = useState<TaskFilter>({});
  const { tasks, loading, error, refresh } = useTasks(filter, AUTO_REFRESH_MS);

  const stats = useMemo(() => ({
    total: tasks.length,
    completed: tasks.filter((t) => t.status === 'completed').length,
    running: tasks.filter((t) => t.status === 'running').length,
    subTasks: tasks.reduce((sum, t) => sum + t.subTasks.length, 0),
    logEntries: tasks.reduce(
      (sum, t) => sum + t.logs.length + t.subTasks.reduce((s, sub) => s + sub.logs.length, 0),
      0,
    ),
  }), [tasks]);

  return (
    <div style={{ maxWidth: 960, margin: '0 auto', padding: '24px 16px' }}>
      <header style={{ marginBottom: 16 }}>
        <h1 style={{ margin: 0, fontSize: 28, fontWeight: 800, color: '#1e1b4b' }}>
          MAF Board
        </h1>
        <p style={{ margin: '4px 0 0', fontSize: 14, color: '#6b7280' }}>
          Multi-Agent Framework 任务看板 — 任务执行状态 & Agent 日志查看
        </p>
      </header>

      {/* 搜索/筛选栏 */}
      <FilterBar filter={filter} onChange={setFilter} onRefresh={refresh} loading={loading} />

      {/* 统计卡片 */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 24 }}>
        <StatCard label="任务总数" value={stats.total} color="#6366f1" />
        <StatCard label="已完成" value={stats.completed} color="#16a34a" />
        <StatCard label="运行中" value={stats.running} color="#f59e0b" />
        <StatCard label="子任务" value={stats.subTasks} color="#2563eb" />
        <StatCard label="日志条目" value={stats.logEntries} color="#d97706" />
      </div>

      {/* 错误提示 */}
      {error && (
        <div style={{ padding: 12, backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: 8, color: '#dc2626', marginBottom: 16 }}>
          ⚠️ 数据加载失败：{error}
        </div>
      )}

      {/* 空状态 */}
      {!loading && tasks.length === 0 && (
        <div style={{ textAlign: 'center', padding: 48, color: '#9ca3af' }}>
          {filter.search || filter.status || filter.agentName
            ? '没有匹配的任务，请调整筛选条件'
            : '暂无任务数据'}
        </div>
      )}

      {/* 任务列表 */}
      {tasks.map((task) => (
        <TaskPanel key={task.id} task={task} />
      ))}
    </div>
  );
}

function StatCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div
      style={{
        flex: 1,
        padding: '12px 16px',
        backgroundColor: '#fff',
        borderRadius: 8,
        border: '1px solid #e5e7eb',
        boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
      }}
    >
      <div style={{ fontSize: 24, fontWeight: 800, color }}>{value}</div>
      <div style={{ fontSize: 12, color: '#6b7280', marginTop: 2 }}>{label}</div>
    </div>
  );
}
