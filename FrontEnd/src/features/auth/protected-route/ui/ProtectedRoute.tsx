import { Navigate, Outlet, useLocation, useSearchParams } from 'react-router-dom'
import { buildLoginUrl, getSafeReturnUrl, useAuthStore } from '@shared/lib'

export function ProtectedRoute() {
  const status = useAuthStore((s) => s.status)
  const location = useLocation()
  if (status !== 'authenticated') {
    return <Navigate to={buildLoginUrl(location.pathname + location.search)} replace />
  }
  return <Outlet />
}

export function GuestOnlyRoute() {
  const status = useAuthStore((s) => s.status)
  const [params] = useSearchParams()
  if (status === 'authenticated') {
    return <Navigate to={getSafeReturnUrl(params.get('returnUrl'))} replace />
  }
  return <Outlet />
}
