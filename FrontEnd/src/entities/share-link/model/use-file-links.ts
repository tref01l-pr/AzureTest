import { useQuery } from '@tanstack/react-query'
import { shareLinkApi } from '../api/share-link-api'
import { shareLinkKeys } from './query-keys'

export function useFileLinks(fileId: string) {
  return useQuery({
    queryKey: shareLinkKeys.byFile(fileId),
    queryFn: () => shareLinkApi.listByFile(fileId),
  })
}

export function useSharedFileInfo(token: string) {
  return useQuery({
    queryKey: shareLinkKeys.byToken(token),
    queryFn: () => shareLinkApi.getByToken(token),
    retry: false,
  })
}
