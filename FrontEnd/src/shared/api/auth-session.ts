import { useAuthStore } from '../lib/auth/auth-store'
import { refreshAccessToken } from './auth-transport'

export async function bootstrapAuthSession(): Promise<void> {
  try {
    useAuthStore.getState().setSession(await refreshAccessToken())
  } catch {
    useAuthStore.getState().clear()
  }
}
