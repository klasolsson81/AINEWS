import * as signalR from '@microsoft/signalr';

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'https://localhost:5001';

let connection: signalR.HubConnection | null = null;

export function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/broadcast`)
      .withAutomaticReconnect()
      .build();
  }
  return connection;
}

export async function startConnection(): Promise<void> {
  const conn = getConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
  }
}

export async function joinBroadcastGroup(jobId: string): Promise<void> {
  const conn = getConnection();
  await conn.invoke('JoinBroadcastGroup', jobId);
}

export async function leaveBroadcastGroup(jobId: string): Promise<void> {
  const conn = getConnection();
  await conn.invoke('LeaveBroadcastGroup', jobId);
}
