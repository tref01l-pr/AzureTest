import { RouterProvider as ReactRouterProvider } from 'react-router-dom'
import { setSessionExpiredHandler } from '@shared/api'
import { buildLoginUrl, useAuthStore } from '@shared/lib'
import { router } from '../router/routes'
import { queryClient } from './query-client'

setSessionExpiredHandler(() => {
  useAuthStore.getState().clear()
  queryClient.clear()
  const { pathname, search } = router.state.location
  if (pathname !== '/login')
    void router.navigate(buildLoginUrl(pathname + search), { replace: true })
})

export function RouterProvider() {
  return <ReactRouterProvider router={router} future={{ v7_startTransition: true }} />
}
