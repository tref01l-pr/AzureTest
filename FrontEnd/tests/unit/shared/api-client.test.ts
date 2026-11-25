import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import {
  apiClient,
  bootstrapAuthSession,
  getApiErrors,
  getErrorMessage,
  hasErrorCode,
  setSessionExpiredHandler,
} from '@shared/api'
import { useAuthStore } from '@shared/lib'
import { api, authResponse, problem, server } from '../../msw/server'

const signIn = (accessToken: string) =>
  useAuthStore.setState({
    status: 'authenticated',
    accessToken,
    user: { id: 'u1', email: 'a@b.c', role: 'User' },
  })

describe('apiClient', () => {
  it('sends the bearer token', async () => {
    signIn('tok')
    server.use(
      http.get(api('account'), ({ request }) =>
        HttpResponse.json({ auth: request.headers.get('Authorization') }),
      ),
    )
    const { data } = await apiClient.get('account')
    expect(data).toEqual({ auth: 'Bearer tok' })
  })

  it('refreshes once for concurrent 401s and retries every request', async () => {
    signIn('expired')
    let refreshCalls = 0
    server.use(
      http.get(api('files'), ({ request }) =>
        request.headers.get('Authorization') === 'Bearer fresh'
          ? HttpResponse.json({ ok: true })
          : new HttpResponse(null, { status: 401 }),
      ),
      http.post(api('refresh-access-token'), () => {
        refreshCalls++
        return HttpResponse.json({ ...authResponse, accessToken: 'fresh' })
      }),
    )

    const results = await Promise.all([
      apiClient.get('files'),
      apiClient.get('files'),
      apiClient.get('files'),
    ])

    expect(results.map((r) => r.data)).toEqual([{ ok: true }, { ok: true }, { ok: true }])
    expect(refreshCalls).toBe(1)
    expect(useAuthStore.getState().accessToken).toBe('fresh')
  })

  it('calls the session-expired handler when refresh fails', async () => {
    signIn('expired')
    const onExpired = vi.fn()
    setSessionExpiredHandler(onExpired)
    server.use(http.get(api('files'), () => new HttpResponse(null, { status: 401 })))

    await expect(apiClient.get('files')).rejects.toMatchObject({ response: { status: 401 } })
    expect(onExpired).toHaveBeenCalledOnce()
  })

  it('does not try to refresh for anonymous callers', async () => {
    let refreshCalls = 0
    server.use(
      http.get(api('s/tok'), () => problem(401, 'Auth.Unauthorized', 'Sign in')),
      http.post(api('refresh-access-token'), () => {
        refreshCalls++
        return new HttpResponse(null, { status: 401 })
      }),
    )
    await expect(apiClient.get('s/tok')).rejects.toBeTruthy()
    expect(refreshCalls).toBe(0)
  })
})

describe('problem details', () => {
  it('parses errors[].code', async () => {
    server.use(http.get(api('files/1'), () => problem(404, 'File.NotFound', 'File not found.')))
    const error = await apiClient.get('files/1').catch((e: unknown) => e)
    expect(getApiErrors(error)).toEqual([{ code: 'File.NotFound', description: 'File not found.' }])
    expect(hasErrorCode(error, 'File.NotFound')).toBe(true)
    expect(getErrorMessage(error)).toBe('File not found.')
  })

  it('parses ASP.NET model-state errors', async () => {
    server.use(
      http.post(api('login'), () =>
        HttpResponse.json(
          { title: 'Validation', errors: { Email: ['Email is invalid'] } },
          { status: 400 },
        ),
      ),
    )
    const error = await apiClient.post('login').catch((e: unknown) => e)
    expect(getApiErrors(error)).toEqual([
      { code: 'Validation.Email', description: 'Email is invalid' },
    ])
  })
})

describe('bootstrapAuthSession', () => {
  it('restores the session from the refresh cookie', async () => {
    server.use(http.post(api('refresh-access-token'), () => HttpResponse.json(authResponse)))
    await bootstrapAuthSession()
    expect(useAuthStore.getState()).toMatchObject({
      status: 'authenticated',
      accessToken: 'access-1',
    })
  })

  it('falls back to anonymous', async () => {
    useAuthStore.setState({ status: 'pending' })
    await bootstrapAuthSession()
    expect(useAuthStore.getState().status).toBe('anonymous')
  })
})
