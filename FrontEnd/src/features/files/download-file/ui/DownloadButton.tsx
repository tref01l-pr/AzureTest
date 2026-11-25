import { FiDownload } from 'react-icons/fi'
import { getErrorMessage } from '@shared/api'
import { Button } from '@shared/ui'
import { useDownloadFile, type DownloadSource } from '../model/use-download-file'

interface DownloadButtonProps {
  source: DownloadSource
  fileName: string
  disabled?: boolean
  label?: string
}

export function DownloadButton({ source, fileName, disabled, label }: DownloadButtonProps) {
  const download = useDownloadFile()
  return (
    <span className="inline-flex flex-col items-start">
      <Button
        variant={label ? 'primary' : 'ghost'}
        size={label ? 'md' : 'sm'}
        disabled={disabled}
        loading={download.isPending}
        onClick={() => download.mutate(source)}
        aria-label={label ? undefined : `Download ${fileName}`}
      >
        <FiDownload aria-hidden="true" /> {label}
      </Button>
      {download.isError && (
        <span role="alert" className="text-xs text-red-600">
          {getErrorMessage(download.error, 'Download failed.')}
        </span>
      )}
    </span>
  )
}
