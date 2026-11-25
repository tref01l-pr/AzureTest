import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { LoginForm } from '@features/auth/login'
import { getSafeReturnUrl } from '@shared/lib'

export function LoginPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const returnUrl = getSafeReturnUrl(params.get('returnUrl'))
  const registerHref = params.has('returnUrl')
    ? `/register?returnUrl=${encodeURIComponent(returnUrl)}`
    : '/register'

  return (
    <main className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-lg bg-white p-6 shadow">
        <h1 className="mb-6 text-2xl font-semibold">Sign in to PdfShare</h1>
        <LoginForm onSuccess={() => navigate(returnUrl, { replace: true })} />
        <p className="mt-4 text-sm text-slate-600">
          No account?{' '}
          <Link to={registerHref} className="text-indigo-600 underline">
            Create one
          </Link>
        </p>
      </div>
    </main>
  )
}
