export type ShareLinkType = 'Public' | 'Restricted'

export interface ShareLinkUser {
  id: string
  email: string
}

export interface ShareLink {
  id: string
  token: string
  type: ShareLinkType
  createdAt: string
  expiresAt: string | null
  isRevoked: boolean
  users: ShareLinkUser[]
}

export interface CreateShareLinkRequest {
  type: ShareLinkType
  userIds: string[]
  expiresAt: string | null
}

export interface SharedFileInfo {
  fileName: string
  sizeBytes: number
  pageCount: number | null
  type: ShareLinkType
  expiresAt: string | null
}
