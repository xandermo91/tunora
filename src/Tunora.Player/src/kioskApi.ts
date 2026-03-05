const API_BASE = import.meta.env.VITE_API_URL as string;

export interface KioskAuthResponse {
  accessToken: string;
  instanceId: number;
  instanceName: string;
}

export interface TrackResponse {
  trackId: string;
  title: string;
  artistName: string;
  audioUrl: string;
  albumImageUrl: string | null;
}

async function apiFetch<T>(path: string, token?: string, options?: RequestInit): Promise<T> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error((body as { error?: string }).error ?? `HTTP ${res.status}`);
  }
  return res.json() as Promise<T>;
}

export const kioskApi = {
  authenticate: (connectionKey: string) =>
    apiFetch<KioskAuthResponse>('/kiosk/auth', undefined, {
      method: 'POST',
      body: JSON.stringify({ connectionKey }),
    }),

  nextTrack: (channelId: number, token: string) =>
    apiFetch<TrackResponse>(`/player/tracks/next?channelId=${channelId}`, token),
};
