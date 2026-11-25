import type { ReactNode } from 'react'
import { FiFileText } from 'react-icons/fi'
import { Link, Navigate, useParams } from 'react-router-dom'
import { useSharedFileInfo } from '@entities/share-link'
import { DownloadButton } from '@features/files/download-file'
import { getErrorMessage, getHttpStatus } from '@shared/api'
import { buildLoginUrl, formatBytes, formatDate, useAuthStore } from '@shared/lib'
import { Spinner } from '@shared/ui'

function Shell({ children }: { children: ReactNode }) {
  return (
    <main className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-md rounded-lg bg-white p-6 text-center shadow">
        {children}
        <Link to="/" className="mt-6 inline-block text-sm text-indigo-600 underline">
          Go to PdfShare
        </Link>
      </div>
    </main>
  )
}

export function ShareLinkPage() {
  const { token = '' } = useParams()
  const user = useAuthStore((s) => s.user)
  const { data, isPending, error } = useSharedFileInfo(token)

  if (isPending) {
    return (
      <Shell>
        <Spinner label="Loading" />
      </Shell>
    )
  }

  if (error) {
    const httpStatus = getHttpStatus(error)
    const status = httpStatus === 410 ? 404 : httpStatus
    if (status === 401) return <Navigate to={buildLoginUrl(`/s/${token}`)} replace />
    return (
      <Shell>
        <h1 className="mb-2 text-xl font-semibold">
          {status === 403
            ? 'No access'
            : status === 404
              ? 'Link not available'
              : 'Something went wrong'}
        </h1>
        <p className="text-slate-600">
          {status === 403
            ? `This file was not shared with ${user?.email ?? 'your account'}. Ask the owner for access.`
            : status === 404
              ? 'The link does not exist, was revoked or has expired.'
              : getErrorMessage(error)}
        </p>
      </Shell>
    )
  }

  return (
    <Shell>
      <FiFileText aria-hidden="true" className="mx-auto mb-3 h-10 w-10 text-indigo-500" />
      <h1 className="mb-1 break-words text-xl font-semibold">{data.fileName}</h1>
      <p className="mb-4 text-sm text-slate-600">
        {formatBytes(data.sizeBytes)}
        {data.pageCount != null && ` · ${data.pageCount} ${data.pageCount === 1 ? 'page' : 'pages'}`}
        {data.expiresAt && ` · link expires ${formatDate(data.expiresAt)}`}
      </p>
      <DownloadButton source={{ token }} fileName={data.fileName} label="Download" />
    </Shell>
  )
}
