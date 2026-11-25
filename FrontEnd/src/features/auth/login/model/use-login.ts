import { useMutation } from '@tanstack/react-query'
import { userApi } from '@entities/user'
import { useAuthStore } from '@shared/lib'

export function useLogin() {
  const setSession = useAuthStore((s) => s.setSession)
  return useMutation({ mutationFn: userApi.login, onSuccess: setSession })
}
