import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMyFiles, type PdfFile } from '@entities/pdf-file'
import { UploadDropzone } from '@features/files/upload-file'
import { getErrorMessage } from '@shared/api'
import { Alert, Button, EmptyState, Spinner } from '@shared/ui'
import { FilesTable } from '@widgets/files-table'
import { ShareDialog } from '@widgets/share-dialog'

const PAGE_SIZE = 10

export function MyFilesPage() {
  const [params, setParams] = useSearchParams()
  const page = Math.max(1, Number(params.get('page')) || 1)
  const { data, isPending, isError, error, isPlaceholderData } = useMyFiles(page, PAGE_SIZE)
  const [sharing, setSharing] = useState<PdfFile | null>(null)
  const goTo = (p: number) => setParams(p === 1 ? {} : { page: String(p) })

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold">My files</h1>
      <UploadDropzone />

      {isPending ? (
        <Spinner label="Loading files" />
      ) : isError ? (
        <Alert>{getErrorMessage(error, 'Could not load files.')}</Alert>
      ) : data.items.length === 0 ? (
        <EmptyState title="No files yet">Upload a PDF to get started.</EmptyState>
      ) : (
        <>
          <FilesTable files={data.items} onShare={setSharing} />
          {data.totalPages > 1 && (
            <nav aria-label="Pagination" className="flex items-center justify-center gap-3">
              <Button
                variant="secondary"
                size="sm"
                disabled={page <= 1}
                onClick={() => goTo(page - 1)}
              >
                Previous
              </Button>
              <span className="text-sm" aria-live="polite">
                Page {data.page} of {data.totalPages}
              </span>
              <Button
                variant="secondary"
                size="sm"
                disabled={page >= data.totalPages || isPlaceholderData}
                onClick={() => goTo(page + 1)}
              >
                Next
              </Button>
            </nav>
          )}
        </>
      )}

      <ShareDialog file={sharing} onClose={() => setSharing(null)} />
    </div>
  )
}
