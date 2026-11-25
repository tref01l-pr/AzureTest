import { describe, expect, it } from 'vitest'
import { buildLoginUrl, getSafeReturnUrl } from '@shared/lib'

describe('getSafeReturnUrl', () => {
  it.each([
    ['/s/abc?x=1', '/s/abc?x=1'],
    ['/shared', '/shared'],
    [null, '/files'],
    ['', '/files'],
    ['https://evil.com', '/files'],
    ['//evil.com', '/files'],
    ['/\\evil.com', '/files'],
    ['javascript:alert(1)', '/files'],
  ])('%s -> %s', (input, expected) => {
    expect(getSafeReturnUrl(input)).toBe(expected)
  })

  it('round-trips through buildLoginUrl', () => {
    const url = new URL(buildLoginUrl('/s/tok?a=1&b=2'), 'http://x')
    expect(getSafeReturnUrl(url.searchParams.get('returnUrl'))).toBe('/s/tok?a=1&b=2')
  })
})
