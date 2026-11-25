import { FiFileText } from 'react-icons/fi'
import { useFileThumbnail } from '../model/use-my-files'
import type { PdfFile } from '../model/types'

export function FileThumbnail({ file }: { file: PdfFile }) {
  const { data: url } = useFileThumbnail(file.id, file.hasThumbnail)
  return (
    <div className="flex h-14 w-11 items-center justify-center overflow-hidden rounded border border-slate-200 bg-slate-100">
      {url ? (
        <img src={url} alt="" className="h-full w-full object-cover" loading="lazy" />
      ) : (
        <FiFileText aria-hidden="true" className="text-slate-400" />
      )}
    </div>
  )
}
