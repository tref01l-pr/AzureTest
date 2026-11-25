import { QueryClient } from '@tanstack/react-query'
import { getHttpStatus } from '@shared/api'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        const status = getHttpStatus(error)
        return failureCount < 2 && (status === undefined || status >= 500)
      },
    },
  },
})
