import { zodResolver } from '@hookform/resolvers/zod'
import clsx from 'clsx'
import { Controller, useForm } from 'react-hook-form'
import { FiX } from 'react-icons/fi'
import { buildShareUrl, type ShareLinkType } from '@entities/share-link'
import {
  createLinkSchema,
  useCreateLink,
  type CreateLinkFormValues,
} from '@features/share-links/create-link'
import { UserSearch } from '@features/users/search-users'
import { getErrorMessage } from '@shared/api'
import { Alert, Button, Input } from '@shared/ui'
import { CopyLinkField } from './CopyLinkField'

const TABS: { type: ShareLinkType; label: string; hint: string }[] = [
  { type: 'Public', label: 'Public', hint: 'Anyone with the link can download the file.' },
  {
    type: 'Restricted',
    label: 'Restricted',
    hint: 'Only the people you add can download (after signing in).',
  },
]

export function CreateLinkForm({ fileId }: { fileId: string }) {
  const create = useCreateLink(fileId)
  const {
    control,
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<CreateLinkFormValues>({
    resolver: zodResolver(createLinkSchema),
    defaultValues: { type: 'Public', users: [], expiresAt: '' },
  })
  const type = watch('type')

  const submit = handleSubmit((values) => create.mutate(values))

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-4">
      <div role="tablist" aria-label="Link type" className="flex gap-1 rounded-lg bg-slate-100 p-1">
        {TABS.map((tab) => (
          <button
            key={tab.type}
            type="button"
            role="tab"
            aria-selected={type === tab.type}
            onClick={() => {
              setValue('type', tab.type)
              create.reset()
            }}
            className={clsx(
              'flex-1 rounded-md px-3 py-1.5 text-sm font-medium',
              type === tab.type ? 'bg-white shadow' : 'text-slate-600 hover:text-slate-900',
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <p className="text-sm text-slate-600">{TABS.find((t) => t.type === type)?.hint}</p>

      {type === 'Restricted' && (
        <Controller
          control={control}
          name="users"
          render={({ field }) => (
            <div className="flex flex-col gap-2">
              <UserSearch
                excludeIds={field.value.map((u) => u.id)}
                onSelect={(user) => field.onChange([...field.value, user])}
                error={errors.users?.message}
              />
              {field.value.length > 0 && (
                <ul aria-label="Selected people" className="flex flex-wrap gap-2">
                  {field.value.map((user) => (
                    <li
                      key={user.id}
                      className="flex items-center gap-1 rounded-full bg-indigo-50 px-3 py-1 text-sm"
                    >
                      {user.email}
                      <button
                        type="button"
                        aria-label={`Remove ${user.email}`}
                        onClick={() => field.onChange(field.value.filter((u) => u.id !== user.id))}
                      >
                        <FiX aria-hidden="true" />
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        />
      )}

      <Input
        label="Expires at (optional)"
        type="datetime-local"
        error={errors.expiresAt?.message}
        {...register('expiresAt')}
      />

      {create.isError && (
        <Alert>{getErrorMessage(create.error, 'Could not create the link.')}</Alert>
      )}
      {create.data && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 p-3">
          <p className="mb-2 text-sm font-medium text-emerald-800">Link created</p>
          <CopyLinkField url={buildShareUrl(create.data.token)} />
        </div>
      )}

      <Button type="submit" loading={create.isPending} className="self-start">
        Create {type.toLowerCase()} link
      </Button>
    </form>
  )
}
