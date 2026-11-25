import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, renderHook } from '@testing-library/react'
import type { ReactElement, ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'

function createWrapper(route = '/') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <MemoryRouter
          initialEntries={[route]}
          future={{ v7_startTransition: true, v7_relativeSplatPath: true }}
        >
          {children}
        </MemoryRouter>
      </QueryClientProvider>
    )
  }
}

export function renderWithProviders(ui: ReactElement, route?: string) {
  return render(ui, { wrapper: createWrapper(route) })
}

export function renderHookWithProviders<T>(hook: () => T) {
  return renderHook(hook, { wrapper: createWrapper() })
}
