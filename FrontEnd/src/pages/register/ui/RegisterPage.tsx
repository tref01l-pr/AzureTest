import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { RegisterForm } from '@features/auth/register'
import { getSafeReturnUrl } from '@shared/lib'

export function RegisterPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const returnUrl = getSafeReturnUrl(params.get('returnUrl'))

  return (
    <main className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-lg bg-white p-6 shadow">
        <h1 className="mb-6 text-2xl font-semibold">Create an account</h1>
        <RegisterForm onSuccess={() => navigate(returnUrl, { replace: true })} />
        <p className="mt-4 text-sm text-slate-600">
          Already registered?{' '}
          <Link to="/login" className="text-indigo-600 underline">
            Sign in
          </Link>
        </p>
      </div>
    </main>
  )
}
