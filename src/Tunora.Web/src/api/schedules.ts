import { apiClient } from './client';
import type { Schedule, CreateScheduleRequest, UpdateScheduleRequest } from '../types/schedules';

const base = (instanceId: number) => `/instances/${instanceId}/schedules`;

export const schedulesApi = {
  list:   (instanceId: number) =>
    apiClient.get<Schedule[]>(base(instanceId)),

  create: (instanceId: number, data: CreateScheduleRequest) =>
    apiClient.post<Schedule>(base(instanceId), data),

  update: (instanceId: number, id: number, data: UpdateScheduleRequest) =>
    apiClient.put<Schedule>(`${base(instanceId)}/${id}`, data),

  delete: (instanceId: number, id: number) =>
    apiClient.delete(`${base(instanceId)}/${id}`),
};
