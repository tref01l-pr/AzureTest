export type FileStatus = 'Processing' | 'Ready' | 'Rejected'

export interface PdfFile {
  id: string
  originalFileName: string
  sizeBytes: number
  uploadedAt: string
  status: FileStatus
  pageCount: number | null
  rejectReason: string | null
  hasThumbnail: boolean
}

export interface SharedFile {
  fileId: string
  originalFileName: string
  sizeBytes: number
  pageCount: number | null
  ownerEmail: string
  token: string
  expiresAt: string | null
}
