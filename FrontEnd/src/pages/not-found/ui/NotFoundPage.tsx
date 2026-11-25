import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-3 p-4 text-center">
      <h1 className="text-3xl font-semibold">Page not found</h1>
      <p className="text-slate-600">The page you are looking for does not exist.</p>
      <Link to="/" className="text-indigo-600 underline">
        Back to PdfShare
      </Link>
    </main>
  )
}
