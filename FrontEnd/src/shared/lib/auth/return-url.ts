export function getSafeReturnUrl(raw: string | null | undefined, fallback = '/files'): string {
  if (!raw || !raw.startsWith('/') || raw.startsWith('//') || raw.startsWith('/\\')) return fallback
  return raw
}

export function buildLoginUrl(returnUrl: string): string {
  return `/login?returnUrl=${encodeURIComponent(returnUrl)}`
}
