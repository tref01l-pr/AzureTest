import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { LoginForm } from '@features/auth/login'
import { RegisterForm } from '@features/auth/register'
import { useAuthStore } from '@shared/lib'
import { api, authResponse, problem, server } from '../../msw/server'
import { renderWithProviders } from '../../test-utils'

describe('LoginForm', () => {
  it('shows validation errors linked to the inputs', async () => {
    renderWithProviders(<LoginForm onSuccess={vi.fn()} />)
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    const email = screen.getByLabelText('Email')
    expect(email).toHaveAttribute('aria-invalid', 'true')
    expect(email).toHaveAccessibleDescription('Email is required')
    expect(screen.getByText('Password is required')).toBeInTheDocument()
  })

  it('maps Auth.InvalidCredentials to a friendly message', async () => {
    server.use(
      http.post(api('login'), () => problem(400, 'Auth.InvalidCredentials', 'raw backend text')),
    )
    renderWithProviders(<LoginForm onSuccess={vi.fn()} />)

    await userEvent.type(screen.getByLabelText('Email'), 'alice@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.')
  })

  it('stores the session in memory and calls onSuccess', async () => {
    let body: unknown
    server.use(
      http.post(api('login'), async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(authResponse)
      }),
    )
    const onSuccess = vi.fn()
    renderWithProviders(<LoginForm onSuccess={onSuccess} />)

    await userEvent.type(screen.getByLabelText('Email'), 'alice@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Secret123')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    await waitFor(() => expect(onSuccess).toHaveBeenCalled())
    expect(body).toEqual({ email: 'alice@example.com', password: 'Secret123' })
    expect(useAuthStore.getState()).toMatchObject({
      status: 'authenticated',
      accessToken: 'access-1',
      user: { email: 'alice@example.com' },
    })
  })
})

describe('RegisterForm', () => {
  it('requires matching passwords', async () => {
    renderWithProviders(<RegisterForm onSuccess={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Email'), 'bob@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Secret123')
    await userEvent.type(screen.getByLabelText('Confirm password'), 'Secret124')
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByText('Passwords do not match')).toBeInTheDocument()
  })

  it('sends the body the API expects, including confirmPassword', async () => {
    let body: unknown
    server.use(
      http.post(api('user-registration'), async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(authResponse)
      }),
    )
    const onSuccess = vi.fn()
    renderWithProviders(<RegisterForm onSuccess={onSuccess} />)
    await userEvent.type(screen.getByLabelText('Email'), 'bob@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Secret123')
    await userEvent.type(screen.getByLabelText('Confirm password'), 'Secret123')
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }))

    await waitFor(() => expect(onSuccess).toHaveBeenCalled())
    expect(body).toEqual({
      email: 'bob@example.com',
      password: 'Secret123',
      confirmPassword: 'Secret123',
    })
  })

  it('shows backend error descriptions', async () => {
    server.use(
      http.post(api('user-registration'), () =>
        problem(400, 'Auth.DuplicateEmail', 'Email is already taken.'),
      ),
    )
    renderWithProviders(<RegisterForm onSuccess={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Email'), 'bob@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Secret123')
    await userEvent.type(screen.getByLabelText('Confirm password'), 'Secret123')
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Email is already taken.')
  })
})
