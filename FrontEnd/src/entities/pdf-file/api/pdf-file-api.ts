import { apiClient, type PagedResult, type UrlResponse } from '@shared/api'
import type { PdfFile, SharedFile } from '../model/types'

export const pdfFileApi = {
  list: (page: number, pageSize: number) =>
    apiClient
      .get<PagedResult<PdfFile>>('files', { params: { page, pageSize } })
      .then((r) => r.data),

  sharedWithMe: () =>
    apiClient
      .get<SharedFile[] | PagedResult<SharedFile>>('files/shared-with-me')
      .then((r) => (Array.isArray(r.data) ? r.data : r.data.items)),

  upload: (file: File, onProgress?: (percent: number) => void) => {
    const form = new FormData()
    form.append('file', file)
    return apiClient
      .post<PdfFile>('files', form, {
        onUploadProgress: (e) => {
          if (e.total) onProgress?.(Math.round((e.loaded / e.total) * 100))
        },
      })
      .then((r) => r.data)
  },

  remove: (id: string) => apiClient.delete(`files/${id}`).then(() => undefined),

  downloadUrl: (id: string) =>
    apiClient.get<UrlResponse>(`files/${id}/download-url`).then((r) => r.data.url),

  thumbnailUrl: (id: string) =>
    apiClient.get<UrlResponse>(`files/${id}/thumbnail-url`).then((r) => r.data.url),
}
