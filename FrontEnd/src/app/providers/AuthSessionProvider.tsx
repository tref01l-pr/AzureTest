import { useEffect, type ReactNode } from 'react'
import { bootstrapAuthSession } from '@shared/api'
import { useAuthStore } from '@shared/lib'
import { FullPageSpinner } from '@shared/ui'

// Module-level: StrictMode runs effects twice, and two refreshes would rotate the token out from under us.
let bootstrap: Promise<void> | null = null

export function AuthSessionProvider({ children }: { children: ReactNode }) {
  const status = useAuthStore((s) => s.status)
  useEffect(() => {
    bootstrap ??= bootstrapAuthSession()
  }, [])
  return status === 'pending' ? <FullPageSpinner /> : <>{children}</>
}
