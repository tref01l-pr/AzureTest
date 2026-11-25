import { apiClient, type UrlResponse } from '@shared/api'
import type { CreateShareLinkRequest, SharedFileInfo, ShareLink } from '../model/types'

export const shareLinkApi = {
  listByFile: (fileId: string) =>
    apiClient.get<ShareLink[]>(`files/${fileId}/links`).then((r) => r.data),

  create: (fileId: string, body: CreateShareLinkRequest) =>
    apiClient.post<ShareLink>(`files/${fileId}/links`, body).then((r) => r.data),

  revoke: (linkId: string) => apiClient.delete(`links/${linkId}`).then(() => undefined),

  getByToken: (token: string) =>
    apiClient.get<SharedFileInfo>(`s/${encodeURIComponent(token)}`).then((r) => r.data),

  downloadUrlByToken: (token: string) =>
    apiClient
      .get<UrlResponse>(`s/${encodeURIComponent(token)}/download-url`)
      .then((r) => r.data.url),
}
