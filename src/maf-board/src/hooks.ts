import { useEffect, useRef, useState, useCallback } from 'react';
import type { MafTask } from './types';
import type { TaskFilter } from './api';
import { api } from './api';

/**
 * 自定义 Hook：获取任务列表，支持搜索/筛选/自动刷新
 */
export function useTasks(filter: TaskFilter, refreshInterval = 0) {
  const [tasks, setTasks] = useState<MafTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const load = useCallback(async () => {
    try {
      const data = await api.fetchTasks(filter);
      setTasks(data);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => {
    setLoading(true);
    load();
  }, [load]);

  // 自动刷新
  useEffect(() => {
    if (refreshInterval > 0) {
      timerRef.current = setInterval(load, refreshInterval);
    }
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [load, refreshInterval]);

  return { tasks, loading, error, refresh: load };
}
