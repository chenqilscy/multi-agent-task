import { useState } from 'react';
import type { MafTask } from '../types';
import { StatusBadge } from './StatusBadge';
import { SubTaskCard } from './SubTaskCard';
import { LogViewer } from './LogViewer';

export function TaskPanel({ task }: { task: MafTask }) {
  const [showLogs, setShowLogs] = useState(false);

  const completedCount = task.subTasks.filter((s) => s.status === 'completed').length;
  const totalCount = task.subTasks.length;

  return (
    <div
      style={{
        border: '1px solid #d1d5db',
        borderRadius: 12,
        padding: 16,
        marginBottom: 16,
        backgroundColor: '#fff',
        boxShadow: '0 1px 3px rgba(0,0,0,0.08)',
      }}
    >
      {/* 主任务头部 */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
            <h3 style={{ margin: 0, fontSize: 18, fontWeight: 700 }}>{task.name}</h3>
            <StatusBadge status={task.status} />
          </div>
          <p style={{ margin: 0, fontSize: 13, color: '#6b7280' }}>{task.description}</p>
        </div>
        <div style={{ textAlign: 'right', fontSize: 12, color: '#9ca3af' }}>
          <div>LeaderAgent: <strong style={{ color: '#6366f1' }}>{task.leaderAgentName}</strong></div>
          <div>{new Date(task.createdAt).toLocaleString('zh-CN')}</div>
        </div>
      </div>

      {/* 用户输入 */}
      <div
        style={{
          marginTop: 12,
          padding: '8px 12px',
          backgroundColor: '#f0fdf4',
          borderRadius: 8,
          fontSize: 14,
          borderLeft: '3px solid #16a34a',
        }}
      >
        🗣️ <strong>用户输入：</strong>{task.userInput}
      </div>

      {/* 子任务进度 */}
      <div style={{ marginTop: 12 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
          <span style={{ fontSize: 14, fontWeight: 600 }}>
            子任务 ({completedCount}/{totalCount})
          </span>
          <button
            onClick={() => setShowLogs(!showLogs)}
            style={{
              fontSize: 12,
              color: '#6366f1',
              background: 'none',
              border: '1px solid #6366f1',
              borderRadius: 4,
              padding: '2px 8px',
              cursor: 'pointer',
            }}
          >
            {showLogs ? '隐藏LeaderAgent日志' : '查看LeaderAgent日志'}
          </button>
        </div>

        {/* 进度条 */}
        <div
          style={{
            height: 6,
            backgroundColor: '#e5e7eb',
            borderRadius: 3,
            marginBottom: 12,
            overflow: 'hidden',
          }}
        >
          <div
            style={{
              height: '100%',
              width: totalCount > 0 ? `${(completedCount / totalCount) * 100}%` : '0%',
              backgroundColor: completedCount === totalCount ? '#16a34a' : '#2563eb',
              borderRadius: 3,
              transition: 'width 0.3s',
            }}
          />
        </div>

        {/* LeaderAgent 日志 */}
        {showLogs && (
          <div
            style={{
              marginBottom: 12,
              padding: 12,
              backgroundColor: '#f8fafc',
              borderRadius: 8,
              border: '1px solid #e2e8f0',
            }}
          >
            <h5 style={{ margin: '0 0 8px', fontSize: 13, color: '#475569' }}>
              LeaderAgent 执行日志
            </h5>
            <LogViewer logs={task.logs} />
          </div>
        )}

        {/* 子任务列表 */}
        {task.subTasks.map((sub) => (
          <SubTaskCard key={sub.id} subTask={sub} />
        ))}
      </div>
    </div>
  );
}
