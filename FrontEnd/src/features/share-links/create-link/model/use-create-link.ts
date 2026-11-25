import { useMutation, useQueryClient } from '@tanstack/react-query'
import { shareLinkApi, shareLinkKeys } from '@entities/share-link'
import type { CreateLinkFormValues } from './create-link-schema'

export function useCreateLink(fileId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ type, users, expiresAt }: CreateLinkFormValues) =>
      shareLinkApi.create(fileId, {
        type,
        userIds: type === 'Restricted' ? users.map((u) => u.id) : [],
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null,
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: shareLinkKeys.byFile(fileId) }),
  })
}
