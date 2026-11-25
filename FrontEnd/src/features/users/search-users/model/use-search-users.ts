import { useQuery } from '@tanstack/react-query'
import { userApi, userKeys } from '@entities/user'
import { useDebounce } from '@shared/lib'

export const MIN_SEARCH_LENGTH = 2

export function useSearchUsers(term: string) {
  const email = useDebounce(term.trim(), 300)
  return useQuery({
    queryKey: userKeys.search(email),
    queryFn: ({ signal }) => userApi.search(email, signal),
    enabled: email.length >= MIN_SEARCH_LENGTH,
    staleTime: 30_000,
  })
}
