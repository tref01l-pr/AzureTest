import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { userApi } from '@entities/user'
import { useAuthStore } from '@shared/lib'

export function useLogout() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  return useMutation({
    mutationFn: userApi.logout,
    onSettled: () => {
      useAuthStore.getState().clear()
      queryClient.clear()
      navigate('/login', { replace: true })
    },
  })
}
