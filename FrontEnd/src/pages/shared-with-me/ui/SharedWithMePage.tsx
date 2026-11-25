import { useSharedWithMe } from '@entities/pdf-file'
import { DownloadButton } from '@features/files/download-file'
import { getErrorMessage } from '@shared/api'
import { formatBytes, formatDate } from '@shared/lib'
import { Alert, EmptyState, Spinner } from '@shared/ui'

export function SharedWithMePage() {
  const { data, isPending, isError, error } = useSharedWithMe()

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold">Shared with me</h1>
      {isPending ? (
        <Spinner label="Loading files" />
      ) : isError ? (
        <Alert>{getErrorMessage(error, 'Could not load files.')}</Alert>
      ) : data.length === 0 ? (
        <EmptyState title="Nothing shared with you yet" />
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th scope="col" className="px-3 py-2">
                  Name
                </th>
                <th scope="col" className="px-3 py-2">
                  Owner
                </th>
                <th scope="col" className="px-3 py-2">
                  Pages
                </th>
                <th scope="col" className="px-3 py-2">
                  Size
                </th>
                <th scope="col" className="px-3 py-2">
                  Expires
                </th>
                <th scope="col" className="px-3 py-2 text-right">
                  Download
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data.map((file) => (
                <tr key={file.token}>
                  <td className="max-w-xs truncate px-3 py-2 font-medium">
                    {file.originalFileName}
                  </td>
                  <td className="px-3 py-2">{file.ownerEmail}</td>
                  <td className="px-3 py-2 tabular-nums">{file.pageCount ?? '—'}</td>
                  <td className="px-3 py-2 tabular-nums">{formatBytes(file.sizeBytes)}</td>
                  <td className="px-3 py-2">
                    {file.expiresAt ? formatDate(file.expiresAt) : 'Never'}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <DownloadButton
                      source={{ token: file.token }}
                      fileName={file.originalFileName}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
