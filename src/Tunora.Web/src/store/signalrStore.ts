import { HubConnectionBuilder, HubConnection, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { create } from 'zustand';
import { useAuthStore } from './authStore';

interface SignalRStore {
  connection: HubConnection | null;
  status: HubConnectionState;
  connect: (token: string) => Promise<void>;
  disconnect: () => Promise<void>;
  watchInstance: (instanceId: number) => Promise<void>;
}

const HUB_URL = `${import.meta.env.VITE_API_URL?.replace('/api/v1', '')}/hubs/playback`;

export const useSignalRStore = create<SignalRStore>((set, get) => ({
  connection: null,
  status: HubConnectionState.Disconnected,

  connect: async (token: string) => {
    const existing = get().connection;
    if (existing?.state === HubConnectionState.Connected) return;

    const conn = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => useAuthStore.getState().token ?? token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    conn.onreconnecting(() => set({ status: HubConnectionState.Reconnecting }));
    conn.onreconnected(() => set({ status: HubConnectionState.Connected }));
    conn.onclose(() => set({ status: HubConnectionState.Disconnected }));

    await conn.start();
    set({ connection: conn, status: HubConnectionState.Connected });
  },

  disconnect: async () => {
    const conn = get().connection;
    if (conn) await conn.stop();
    set({ connection: null, status: HubConnectionState.Disconnected });
  },

  watchInstance: async (instanceId: number) => {
    const conn = get().connection;
    if (conn?.state === HubConnectionState.Connected) {
      await conn.invoke('WatchInstance', instanceId);
    }
  },
}));
