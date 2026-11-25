import clsx from 'clsx'
import { Suspense } from 'react'
import { FiFileText } from 'react-icons/fi'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { LogoutButton } from '@features/auth/logout'
import { useAuthStore } from '@shared/lib'
import { ErrorBoundary, FullPageSpinner } from '@shared/ui'

const navClass = ({ isActive }: { isActive: boolean }) =>
  clsx(
    'rounded-md px-3 py-1.5 text-sm font-medium',
    isActive ? 'bg-indigo-50 text-indigo-700' : 'text-slate-600 hover:text-slate-900',
  )

export function AppLayout() {
  const user = useAuthStore((s) => s.user)
  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-4 px-4 py-3">
          <Link to="/files" className="flex items-center gap-2 font-semibold text-indigo-700">
            <FiFileText aria-hidden="true" /> PdfShare
          </Link>
          <nav aria-label="Main" className="flex gap-1">
            <NavLink to="/files" end className={navClass}>
              My files
            </NavLink>
            <NavLink to="/shared" className={navClass}>
              Shared with me
            </NavLink>
          </nav>
          <div className="ml-auto flex items-center gap-3 text-sm text-slate-600">
            <span>{user?.email}</span>
            <LogoutButton />
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">
        <ErrorBoundary>
          <Suspense fallback={<FullPageSpinner />}>
            <Outlet />
          </Suspense>
        </ErrorBoundary>
      </main>
    </div>
  )
}
