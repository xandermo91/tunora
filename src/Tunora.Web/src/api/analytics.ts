import { apiClient } from './client';

export interface OverviewStats {
  totalLocations: number;
  activeLocations: number;
  playingNow: number;
  schedulesThisWeek: number;
}

export const analyticsApi = {
  getOverview: () => apiClient.get<OverviewStats>('/analytics/overview'),
};
