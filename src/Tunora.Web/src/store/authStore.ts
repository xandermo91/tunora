import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { jwtDecode } from 'jwt-decode';
import type { UserInfo } from '../types/auth';

interface JwtPayload {
  sub: string;
  email: string;
  given_name: string;
  family_name: string;
  companyId: string;
  role: string;
  exp: number;
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserInfo | null;
  isAuthenticated: boolean;
  setTokens: (accessToken: string, refreshToken: string) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,

      setTokens: (accessToken, refreshToken) => {
        const payload = jwtDecode<JwtPayload>(accessToken);

        if (payload.exp * 1000 < Date.now()) {
          throw new Error('Received an already-expired access token.');
        }

        const user: UserInfo = {
          id: parseInt(payload.sub),
          email: payload.email,
          firstName: payload.given_name ?? '',
          lastName: payload.family_name ?? '',
          companyId: parseInt(payload.companyId),
          role: payload.role as 'Admin' | 'Staff',
        };
        set({ accessToken, refreshToken, user, isAuthenticated: true });
      },

      clearAuth: () =>
        set({ accessToken: null, refreshToken: null, user: null, isAuthenticated: false }),
    }),
    {
      name: 'tunora-auth',
      // Only persist the refresh token — access token is rebuilt on page reload
      partialize: (state) => ({
        refreshToken: state.refreshToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
