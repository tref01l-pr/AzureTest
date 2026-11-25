import { isAxiosError } from 'axios'

export interface ApiError {
  code: string
  description: string
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: ApiError[] | Record<string, string[]>
}

export function getApiErrors(error: unknown): ApiError[] {
  if (!isAxiosError<ProblemDetails>(error)) return []
  const body = error.response?.data
  if (!body || typeof body !== 'object') return []

  if (Array.isArray(body.errors)) return body.errors
  if (body.errors) {
    return Object.entries(body.errors).flatMap(([field, messages]) =>
      messages.map((description) => ({ code: `Validation.${field}`, description })),
    )
  }
  const description = body.detail ?? body.title
  return description ? [{ code: 'Unknown', description }] : []
}

export function hasErrorCode(error: unknown, code: string): boolean {
  return getApiErrors(error).some((e) => e.code === code)
}

export function getHttpStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong.'): string {
  const errors = getApiErrors(error)
  if (errors.length) return errors.map((e) => e.description).join(' ')
  if (isAxiosError(error) && !error.response) return 'Network error. Check your connection.'
  return fallback
}
