/** 任务状态 */
export type TaskStatus = 'pending' | 'running' | 'completed' | 'failed' | 'cancelled';

/** Agent 角色 */
export type AgentRole = 'main' | 'sub';

/** Agent 执行日志条目 */
export interface AgentLogEntry {
  id: string;
  timestamp: string;
  agentId: string;
  agentName: string;
  role: AgentRole;
  action: string;
  message: string;
  duration?: number;
  status: 'info' | 'success' | 'warning' | 'error';
  metadata?: Record<string, unknown>;
}

/** 子任务 */
export interface SubTask {
  id: string;
  parentTaskId: string;
  name: string;
  description: string;
  status: TaskStatus;
  agentId: string;
  agentName: string;
  startedAt?: string;
  completedAt?: string;
  result?: string;
  error?: string;
  logs: AgentLogEntry[];
}

/** 主任务 */
export interface MafTask {
  id: string;
  name: string;
  description: string;
  status: TaskStatus;
  leaderAgentId: string;
  leaderAgentName: string;
  userInput: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  subTasks: SubTask[];
  logs: AgentLogEntry[];
}
