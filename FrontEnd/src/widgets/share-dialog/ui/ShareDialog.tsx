import { Dialog } from '@shared/ui'
import { CreateLinkForm } from './CreateLinkForm'
import { LinksList } from './LinksList'

interface ShareDialogProps {
  file: { id: string; originalFileName: string } | null
  onClose: () => void
}

export function ShareDialog({ file, onClose }: ShareDialogProps) {
  return (
    <Dialog
      open={file !== null}
      title={`Share "${file?.originalFileName ?? ''}"`}
      onClose={onClose}
    >
      {file && (
        <div className="flex flex-col gap-6">
          <CreateLinkForm key={file.id} fileId={file.id} />
          <section>
            <h3 className="mb-2 font-medium">Links</h3>
            <LinksList fileId={file.id} />
          </section>
        </div>
      )}
    </Dialog>
  )
}
