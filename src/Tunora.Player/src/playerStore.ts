import { create } from 'zustand';

export type PlayerStatus = 'idle' | 'connecting' | 'waiting' | 'playing' | 'stopped' | 'error';
export type HubStatus = 'connected' | 'reconnecting' | 'disconnected';

export interface CurrentTrack {
  trackId: string;
  title: string;
  artistName: string;
  audioUrl: string;
  albumImageUrl: string | null;
}

interface PlayerStore {
  token: string | null;
  instanceId: number | null;
  instanceName: string | null;
  channelId: number | null;
  status: PlayerStatus;
  hubStatus: HubStatus;
  currentTrack: CurrentTrack | null;
  error: string | null;

  setAuth: (token: string, instanceId: number, instanceName: string) => void;
  setChannelId: (channelId: number) => void;
  setStatus: (status: PlayerStatus) => void;
  setHubStatus: (hubStatus: HubStatus) => void;
  setCurrentTrack: (track: CurrentTrack | null) => void;
  setError: (error: string) => void;
}

export const usePlayerStore = create<PlayerStore>((set) => ({
  token: null,
  instanceId: null,
  instanceName: null,
  channelId: null,
  status: 'idle',
  hubStatus: 'disconnected',
  currentTrack: null,
  error: null,

  setAuth: (token, instanceId, instanceName) =>
    set({ token, instanceId, instanceName, status: 'waiting' }),
  setChannelId: (channelId) => set({ channelId }),
  setStatus: (status) => set({ status }),
  setHubStatus: (hubStatus) => set({ hubStatus }),
  setCurrentTrack: (currentTrack) => set({ currentTrack }),
  setError: (error) => set({ error, status: 'error' }),
}));
