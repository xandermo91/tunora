import { apiClient } from './client';

export const playbackApi = {
  play:          (instanceId: number, channelId: number) =>
    apiClient.post(`/playback/${instanceId}/play`, { channelId }),
  stop:          (instanceId: number) =>
    apiClient.post(`/playback/${instanceId}/stop`),
  next:          (instanceId: number) =>
    apiClient.post(`/playback/${instanceId}/next`),
  changeChannel: (instanceId: number, channelId: number) =>
    apiClient.put(`/playback/${instanceId}/channel`, { channelId }),
};
