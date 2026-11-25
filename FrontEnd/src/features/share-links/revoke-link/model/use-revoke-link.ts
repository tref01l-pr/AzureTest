import { useMutation, useQueryClient } from '@tanstack/react-query'
import { shareLinkApi, shareLinkKeys } from '@entities/share-link'

export function useRevokeLink(fileId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: shareLinkApi.revoke,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: shareLinkKeys.byFile(fileId) }),
  })
}
