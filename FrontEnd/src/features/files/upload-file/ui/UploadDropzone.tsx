import clsx from 'clsx'
import { useState } from 'react'
import { useDropzone } from 'react-dropzone'
import { FiUploadCloud } from 'react-icons/fi'
import { getErrorMessage } from '@shared/api'
import { Alert } from '@shared/ui'
import { validatePdf } from '../lib/validate-pdf'
import { useUploadFile } from '../model/use-upload-file'

export function UploadDropzone() {
  const upload = useUploadFile()
  const [clientError, setClientError] = useState<string | null>(null)

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: { 'application/pdf': ['.pdf'] },
    multiple: false,
    disabled: upload.isPending,
    onDrop: ([file], rejected) => {
      upload.reset()
      const candidate = file ?? rejected[0]?.file
      if (!candidate) return
      const error = validatePdf(candidate)
      setClientError(error)
      if (!error) upload.mutate(candidate)
    },
  })

  return (
    <div className="flex flex-col gap-2">
      <div
        {...getRootProps({ 'aria-label': 'Upload a PDF file' })}
        className={clsx(
          'flex cursor-pointer flex-col items-center gap-2 rounded-lg border-2 border-dashed p-8 text-center transition-colors',
          isDragActive
            ? 'border-indigo-500 bg-indigo-50'
            : 'border-slate-300 bg-white hover:border-indigo-400',
          upload.isPending && 'cursor-wait opacity-75',
        )}
      >
        <input {...getInputProps()} data-testid="upload-input" />
        <FiUploadCloud aria-hidden="true" className="h-8 w-8 text-indigo-500" />
        <p className="font-medium">
          {isDragActive ? 'Drop the PDF here' : 'Drag a PDF here or click to choose'}
        </p>
        <p className="text-xs text-slate-500">PDF only, up to 20 MB</p>
      </div>
      {upload.isPending && (
        <div className="flex items-center gap-3">
          <progress
            className="h-2 flex-1"
            max={100}
            value={upload.progress}
            aria-label="Upload progress"
          />
          <span className="w-10 text-right text-sm tabular-nums">{upload.progress}%</span>
        </div>
      )}
      {clientError && <Alert>{clientError}</Alert>}
      {upload.isError && <Alert>{getErrorMessage(upload.error, 'Upload failed.')}</Alert>}
    </div>
  )
}
