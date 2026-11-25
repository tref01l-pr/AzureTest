import { isRouteErrorResponse, useRouteError } from 'react-router-dom'
import { ErrorFallback } from '@shared/ui'

export function RouteError() {
  const error = useRouteError()
  console.error(error)
  return (
    <ErrorFallback
      message={isRouteErrorResponse(error) ? `${error.status} ${error.statusText}` : undefined}
    />
  )
}
