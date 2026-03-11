import { apiClient } from './client';

export interface AdvisorResponse {
  insight: string;
  suggestions: string[];
}

export const advisorApi = {
  getMusicAdvice: (data: { businessType: string; description?: string }) =>
    apiClient.post<AdvisorResponse>('/advisor/music', data).then(r => r.data),
  getAnalyticsInsight: () =>
    apiClient.post<AdvisorResponse>('/advisor/analytics-insight').then(r => r.data),
};
