import { buildShareUrl, useFileLinks } from '@entities/share-link'
import { useRevokeLink } from '@features/share-links/revoke-link'
import { getErrorMessage } from '@shared/api'
import { formatDate } from '@shared/lib'
import { Alert, Button, Spinner } from '@shared/ui'
import { CopyLinkField } from './CopyLinkField'

export function LinksList({ fileId }: { fileId: string }) {
  const { data: links, isPending, isError, error } = useFileLinks(fileId)
  const revoke = useRevokeLink(fileId)

  if (isPending) return <Spinner label="Loading links" />
  if (isError) return <Alert>{getErrorMessage(error)}</Alert>
  if (links.length === 0) return <p className="text-sm text-slate-500">No links yet.</p>

  return (
    <ul aria-label="Existing links" className="flex flex-col gap-3">
      {links.map((link) => (
        <li key={link.id} className="rounded-md border border-slate-200 p-3">
          <div className="mb-2 flex flex-wrap items-center justify-between gap-2 text-sm">
            <span>
              <strong>{link.type}</strong>
              {link.expiresAt && <> · expires {formatDate(link.expiresAt)}</>}
              {link.isRevoked && <span className="ml-2 text-red-600">Revoked</span>}
            </span>
            {!link.isRevoked && (
              <Button
                variant="danger"
                size="sm"
                loading={revoke.isPending && revoke.variables === link.id}
                onClick={() => revoke.mutate(link.id)}
              >
                Revoke
              </Button>
            )}
          </div>
          {link.users.length > 0 && (
            <p className="mb-2 text-xs text-slate-600">
              Shared with: {link.users.map((u) => u.email).join(', ')}
            </p>
          )}
          {!link.isRevoked && <CopyLinkField url={buildShareUrl(link.token)} />}
        </li>
      ))}
      {revoke.isError && <Alert>{getErrorMessage(revoke.error)}</Alert>}
    </ul>
  )
}
