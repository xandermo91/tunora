import { apiClient } from './client';
import type { AuthResponse } from '../types/auth';

export const authApi = {
  register: (data: {
    companyName: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => apiClient.post<AuthResponse>('/auth/register', data),

  login: (data: { email: string; password: string }) =>
    apiClient.post<AuthResponse>('/auth/login', data),

  refresh: (refreshToken: string) =>
    apiClient.post<AuthResponse>('/auth/refresh', { refreshToken }),

  logout: () => apiClient.post('/auth/logout'),
};
