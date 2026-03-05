import { apiClient } from './client';
import type { Instance, InstanceCreated, AssignedChannel } from '../types/instances';

export const instancesApi = {
  list: () => apiClient.get<Instance[]>('/instances'),
  get: (id: number) => apiClient.get<Instance>(`/instances/${id}`),
  create: (data: { name: string; location: string }) =>
    apiClient.post<InstanceCreated>('/instances', data),
  update: (id: number, data: { name: string; location: string }) =>
    apiClient.put<Instance>(`/instances/${id}`, data),
  delete: (id: number) => apiClient.delete(`/instances/${id}`),
  getConnectionKey: (id: number) =>
    apiClient.get<{ connectionKey: string }>(`/instances/${id}/connection-key`),
  getChannels: (id: number) => apiClient.get<AssignedChannel[]>(`/instances/${id}/channels`),
  assignChannel: (instanceId: number, channelId: number) =>
    apiClient.post(`/instances/${instanceId}/channels`, { channelId }),
  removeChannel: (instanceId: number, channelId: number) =>
    apiClient.delete(`/instances/${instanceId}/channels/${channelId}`),
};
