import clsx from 'clsx'
import type { FileStatus } from '../model/types'

const styles: Record<FileStatus, string> = {
  Processing: 'bg-amber-100 text-amber-800',
  Ready: 'bg-emerald-100 text-emerald-800',
  Rejected: 'bg-red-100 text-red-800',
}

export function FileStatusBadge({ status, title }: { status: FileStatus; title?: string }) {
  return (
    <span
      title={title}
      className={clsx('rounded-full px-2 py-0.5 text-xs font-medium', styles[status])}
    >
      {status}
    </span>
  )
}
