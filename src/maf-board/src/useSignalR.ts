import { useEffect, useRef, useCallback } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState, LogLevel } from '@microsoft/signalr';
import type { MafTask } from './types';

const HUB_URL = '/hub/maf';

interface SignalRCallbacks {
  onTaskCreated?: (task: MafTask) => void;
  onTaskUpdated?: (task: MafTask) => void;
  onTaskProgress?: (taskId: string, progress: number, status: string) => void;
}

/**
 * SignalR 实时通知 Hook
 * 自动连接/断线重连，接收任务状态更新
 */
export function useSignalR(callbacks: SignalRCallbacks) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbacksRef = useRef(callbacks);
  callbacksRef.current = callbacks;

  const connect = useCallback(async () => {
    if (connectionRef.current?.state === HubConnectionState.Connected) return;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('TaskCreated', (task: MafTask) => {
      callbacksRef.current.onTaskCreated?.(task);
    });

    connection.on('TaskUpdated', (task: MafTask) => {
      callbacksRef.current.onTaskUpdated?.(task);
    });

    connection.on('ReceiveTaskProgress', (taskId: string, progress: number, status: string) => {
      callbacksRef.current.onTaskProgress?.(taskId, progress, status);
    });

    try {
      await connection.start();
      connectionRef.current = connection;
    } catch {
      // 连接失败时静默降级到轮询模式
    }
  }, []);

  useEffect(() => {
    connect();
    return () => {
      connectionRef.current?.stop();
    };
  }, [connect]);
}
