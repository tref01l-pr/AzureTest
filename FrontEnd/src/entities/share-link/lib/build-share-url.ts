export function buildShareUrl(token: string, origin = window.location.origin): string {
  return `${origin}/s/${token}`
}
