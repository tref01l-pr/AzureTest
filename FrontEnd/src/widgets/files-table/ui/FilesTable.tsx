import { FiShare2 } from 'react-icons/fi'
import { FileStatusBadge, FileThumbnail, type PdfFile } from '@entities/pdf-file'
import { DeleteFileButton } from '@features/files/delete-file'
import { DownloadButton } from '@features/files/download-file'
import { formatBytes, formatDate } from '@shared/lib'
import { Button } from '@shared/ui'

interface FilesTableProps {
  files: PdfFile[]
  onShare: (file: PdfFile) => void
}

export function FilesTable({ files, onShare }: FilesTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
      <table className="w-full text-left text-sm">
        <thead className="bg-slate-50 text-slate-600">
          <tr>
            <th scope="col" className="px-3 py-2">
              <span className="sr-only">Preview</span>
            </th>
            <th scope="col" className="px-3 py-2">
              Name
            </th>
            <th scope="col" className="px-3 py-2">
              Pages
            </th>
            <th scope="col" className="px-3 py-2">
              Size
            </th>
            <th scope="col" className="px-3 py-2">
              Status
            </th>
            <th scope="col" className="px-3 py-2">
              Uploaded
            </th>
            <th scope="col" className="px-3 py-2 text-right">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {files.map((file) => {
            const ready = file.status === 'Ready'
            return (
              <tr key={file.id}>
                <td className="px-3 py-2">
                  <FileThumbnail file={file} />
                </td>
                <td
                  className="max-w-xs truncate px-3 py-2 font-medium"
                  title={file.originalFileName}
                >
                  {file.originalFileName}
                </td>
                <td className="px-3 py-2 tabular-nums">{file.pageCount ?? '—'}</td>
                <td className="px-3 py-2 tabular-nums">{formatBytes(file.sizeBytes)}</td>
                <td className="px-3 py-2">
                  <FileStatusBadge status={file.status} title={file.rejectReason ?? undefined} />
                </td>
                <td className="whitespace-nowrap px-3 py-2 text-slate-600">
                  {formatDate(file.uploadedAt)}
                </td>
                <td className="px-3 py-2">
                  <div className="flex items-start justify-end gap-1">
                    <DownloadButton
                      source={{ fileId: file.id }}
                      fileName={file.originalFileName}
                      disabled={!ready}
                    />
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={!ready}
                      onClick={() => onShare(file)}
                      aria-label={`Share ${file.originalFileName}`}
                    >
                      <FiShare2 aria-hidden="true" />
                    </Button>
                    <DeleteFileButton fileId={file.id} fileName={file.originalFileName} />
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
