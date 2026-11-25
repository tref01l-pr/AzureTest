import { ErrorBoundary } from '@shared/ui'
import { AuthSessionProvider } from './providers/AuthSessionProvider'
import { QueryProvider } from './providers/QueryProvider'
import { RouterProvider } from './providers/RouterProvider'
import './styles/index.css'

export function App() {
  return (
    <ErrorBoundary>
      <QueryProvider>
        <AuthSessionProvider>
          <RouterProvider />
        </AuthSessionProvider>
      </QueryProvider>
    </ErrorBoundary>
  )
}
