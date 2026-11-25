import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props {
  children: ReactNode
  fallback?: ReactNode
}

export class ErrorBoundary extends Component<Props, { hasError: boolean }> {
  state = { hasError: false }

  static getDerivedStateFromError() {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(error, info.componentStack)
  }

  render() {
    if (!this.state.hasError) return this.props.children
    return this.props.fallback ?? <ErrorFallback />
  }
}

export function ErrorFallback({ message = 'Something went wrong.' }: { message?: string }) {
  return (
    <div role="alert" className="mx-auto max-w-md p-8 text-center">
      <h1 className="mb-2 text-xl font-semibold">Unexpected error</h1>
      <p className="mb-4 text-slate-600">{message}</p>
      <a href="/" className="text-indigo-600 underline">
        Go to home page
      </a>
    </div>
  )
}
