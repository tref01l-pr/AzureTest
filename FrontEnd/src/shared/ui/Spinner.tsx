import clsx from 'clsx'

export function Spinner({ size = 'md', label }: { size?: 'sm' | 'md' | 'lg'; label?: string }) {
  return (
    <span role={label ? 'status' : undefined} className="inline-flex items-center gap-2">
      <span
        aria-hidden="true"
        className={clsx(
          'inline-block animate-spin rounded-full border-2 border-current border-t-transparent',
          { sm: 'h-4 w-4', md: 'h-6 w-6', lg: 'h-10 w-10' }[size],
        )}
      />
      {label && <span className="sr-only">{label}</span>}
    </span>
  )
}

export function FullPageSpinner() {
  return (
    <div className="flex min-h-screen items-center justify-center text-indigo-600">
      <Spinner size="lg" label="Loading" />
    </div>
  )
}
