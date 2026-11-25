import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { pdfFileApi } from '../api/pdf-file-api'
import { pdfFileKeys } from './query-keys'

export const PROCESSING_POLL_MS = 3000

export function useMyFiles(page: number, pageSize = 10) {
  return useQuery({
    queryKey: pdfFileKeys.list(page, pageSize),
    queryFn: () => pdfFileApi.list(page, pageSize),
    placeholderData: keepPreviousData,
    refetchInterval: (query) =>
      query.state.data?.items.some((f) => f.status === 'Processing') ? PROCESSING_POLL_MS : false,
  })
}

export function useSharedWithMe() {
  return useQuery({ queryKey: pdfFileKeys.shared(), queryFn: pdfFileApi.sharedWithMe })
}

export function useFileThumbnail(id: string, enabled: boolean) {
  return useQuery({
    queryKey: pdfFileKeys.thumbnail(id),
    queryFn: () => pdfFileApi.thumbnailUrl(id),
    enabled,
    staleTime: 4 * 60_000, // SAS URLs live 5 minutes
  })
}
