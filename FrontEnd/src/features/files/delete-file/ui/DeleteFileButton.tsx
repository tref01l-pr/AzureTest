import { FiTrash2 } from 'react-icons/fi'
import { Button } from '@shared/ui'
import { useDeleteFile } from '../model/use-delete-file'

export function DeleteFileButton({ fileId, fileName }: { fileId: string; fileName: string }) {
  const remove = useDeleteFile()
  const onClick = () => {
    if (window.confirm(`Delete "${fileName}"? Its share links stop working.`)) remove.mutate(fileId)
  }
  return (
    <Button
      variant="ghost"
      size="sm"
      loading={remove.isPending}
      onClick={onClick}
      aria-label={`Delete ${fileName}`}
    >
      <FiTrash2 aria-hidden="true" />
    </Button>
  )
}
