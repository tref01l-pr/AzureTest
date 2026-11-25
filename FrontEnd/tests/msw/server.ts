import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'

export const api = (path: string) => `*/api/${path}`

export const authResponse = {
  id: 'u1',
  role: 'User',
  accessToken: 'access-1',
  email: 'alice@example.com',
}

export const problem = (status: number, code: string, description: string) =>
  HttpResponse.json(
    { title: 'Error', status, errors: [{ code, description }] },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  )

export const server = setupServer(
  http.post(api('refresh-access-token'), () =>
    problem(401, 'Auth.InvalidRefreshToken', 'No session'),
  ),
)
