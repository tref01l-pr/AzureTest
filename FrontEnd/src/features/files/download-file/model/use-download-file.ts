import { useMutation } from '@tanstack/react-query'
import { pdfFileApi } from '@entities/pdf-file'
import { shareLinkApi } from '@entities/share-link'

export type DownloadSource = { fileId: string } | { token: string }

export function useDownloadFile() {
  return useMutation({
    mutationFn: (source: DownloadSource) =>
      'token' in source
        ? shareLinkApi.downloadUrlByToken(source.token)
        : pdfFileApi.downloadUrl(source.fileId),
    onSuccess: (url) => window.location.assign(url),
  })
}
