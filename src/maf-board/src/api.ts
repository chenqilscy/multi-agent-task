import type { MafTask, TaskStatus } from './types';
import { mockTasks } from './mock-data';

const API_BASE = '/api';

export interface TaskFilter {
  status?: TaskStatus;
  search?: string;
  agentName?: string;
}

// 当后端 API 就绪时，切换 useMockData = false 即可
const useMockData = false;

async function fetchTasks(filter?: TaskFilter): Promise<MafTask[]> {
  if (useMockData) {
    return filterTasks(mockTasks, filter);
  }

  const params = new URLSearchParams();
  if (filter?.status) params.set('status', filter.status);
  if (filter?.search) params.set('search', filter.search);
  if (filter?.agentName) params.set('agent', filter.agentName);

  const res = await fetch(`${API_BASE}/tasks?${params}`);
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  const tasks: MafTask[] = await res.json();
  return filterTasks(tasks, filter);
}

async function fetchTaskById(id: string): Promise<MafTask | undefined> {
  if (useMockData) {
    return mockTasks.find((t) => t.id === id);
  }

  const res = await fetch(`${API_BASE}/tasks/${encodeURIComponent(id)}`);
  if (res.status === 404) return undefined;
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

function filterTasks(tasks: MafTask[], filter?: TaskFilter): MafTask[] {
  if (!filter) return tasks;

  return tasks.filter((task) => {
    if (filter.status && task.status !== filter.status) return false;

    if (filter.search) {
      const q = filter.search.toLowerCase();
      const matchName = task.name.toLowerCase().includes(q);
      const matchDesc = task.description.toLowerCase().includes(q);
      const matchInput = task.userInput.toLowerCase().includes(q);
      const matchSubTask = task.subTasks.some(
        (s) => s.name.toLowerCase().includes(q) || s.agentName.toLowerCase().includes(q),
      );
      if (!matchName && !matchDesc && !matchInput && !matchSubTask) return false;
    }

    if (filter.agentName) {
      const a = filter.agentName.toLowerCase();
      const matchLeader = task.leaderAgentName.toLowerCase().includes(a);
      const matchSub = task.subTasks.some((s) => s.agentName.toLowerCase().includes(a));
      if (!matchLeader && !matchSub) return false;
    }

    return true;
  });
}

export const api = { fetchTasks, fetchTaskById };
