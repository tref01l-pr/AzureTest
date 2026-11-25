import { create } from 'zustand'
import type { AuthenticationResponse, Role } from '../../api/types'

export interface SessionUser {
  id: string
  email: string
  role: Role
}

type AuthStatus = 'pending' | 'authenticated' | 'anonymous'

interface AuthState {
  status: AuthStatus
  user: SessionUser | null
  accessToken: string | null
  setSession: (response: AuthenticationResponse) => void
  clear: () => void
}

export const useAuthStore = create<AuthState>()((set) => ({
  status: 'pending',
  user: null,
  accessToken: null,
  setSession: ({ id, email, role, accessToken }) =>
    set({ status: 'authenticated', user: { id, email, role }, accessToken }),
  clear: () => set({ status: 'anonymous', user: null, accessToken: null }),
}))
