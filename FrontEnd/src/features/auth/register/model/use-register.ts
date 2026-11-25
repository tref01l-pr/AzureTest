import { useMutation } from '@tanstack/react-query'
import { userApi } from '@entities/user'
import { useAuthStore } from '@shared/lib'

export function useRegister() {
  const setSession = useAuthStore((s) => s.setSession)
  return useMutation({ mutationFn: userApi.register, onSuccess: setSession })
}
