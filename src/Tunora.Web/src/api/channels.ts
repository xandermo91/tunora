import { apiClient } from './client';
import type { Channel } from '../types/channels';

export const channelsApi = {
  list: () => apiClient.get<Channel[]>('/channels'),
};
