import { apiClient } from './client';

export interface SubscriptionStatus {
  tier: string;
  status: string;
  isTrialing: boolean;
  periodEnd: string | null;
  maxInstances: number;  // -1 = unlimited
  maxChannels: number;
  canSchedule: boolean;
}

export const billingApi = {
  getStatus: () =>
    apiClient.get<SubscriptionStatus>('/billing/status'),

  createCheckout: (tier: string, successUrl: string, cancelUrl: string) =>
    apiClient.post<{ url: string }>('/billing/checkout', { tier, successUrl, cancelUrl }),

  createPortal: (returnUrl: string) =>
    apiClient.post<{ url: string }>('/billing/portal', { returnUrl }),
};
