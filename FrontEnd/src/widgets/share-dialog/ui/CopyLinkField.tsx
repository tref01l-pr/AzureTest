import { useState } from 'react'
import { FiCheck, FiCopy } from 'react-icons/fi'
import { Button } from '@shared/ui'

export function CopyLinkField({ url }: { url: string }) {
  const [copied, setCopied] = useState(false)
  const copy = async () => {
    await navigator.clipboard.writeText(url)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }
  return (
    <div className="flex items-center gap-2">
      <input
        readOnly
        value={url}
        aria-label="Share link"
        onFocus={(e) => e.target.select()}
        className="min-w-0 flex-1 rounded-md border border-slate-300 bg-slate-50 px-2 py-1 text-sm"
      />
      <Button variant="secondary" size="sm" onClick={copy}>
        {copied ? <FiCheck aria-hidden="true" /> : <FiCopy aria-hidden="true" />}
        {copied ? 'Copied' : 'Copy'}
      </Button>
    </div>
  )
}
