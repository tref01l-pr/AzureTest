import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import type { ShareLink } from '@entities/share-link'
import { ShareDialog } from '@widgets/share-dialog'
import { api, server } from '../../msw/server'
import { renderWithProviders } from '../../test-utils'

const file = { id: 'f1', originalFileName: 'report.pdf' }

function mockLinks(initial: ShareLink[] = []) {
  let links = [...initial]
  const requests: { create: unknown[]; revoked: string[] } = { create: [], revoked: [] }
  server.use(
    http.get(api('files/f1/links'), () => HttpResponse.json(links)),
    http.post(api('files/f1/links'), async ({ request }) => {
      const body = (await request.json()) as { type: ShareLink['type'] }
      requests.create.push(body)
      const link: ShareLink = {
        id: `l${links.length + 1}`,
        token: `tok${links.length + 1}`,
        type: body.type,
        createdAt: '2026-01-01T00:00:00Z',
        expiresAt: null,
        isRevoked: false,
        users: [],
      }
      links = [...links, link]
      return HttpResponse.json(link, { status: 201 })
    }),
    http.delete(api('links/:id'), ({ params }) => {
      requests.revoked.push(String(params.id))
      links = links.map((l) => (l.id === params.id ? { ...l, isRevoked: true } : l))
      return new HttpResponse(null, { status: 204 })
    }),
    http.get(api('users/search'), ({ request }) =>
      HttpResponse.json(
        [
          { id: 'u2', email: 'bob@example.com' },
          { id: 'u3', email: 'bobby@example.com' },
        ].filter((u) => u.email.includes(new URL(request.url).searchParams.get('email') ?? '')),
      ),
    ),
  )
  return requests
}

describe('ShareDialog', () => {
  it('creates a public link and shows its URL', async () => {
    const requests = mockLinks()
    renderWithProviders(<ShareDialog file={file} onClose={vi.fn()} />)

    expect(await screen.findByText('No links yet.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Create public link' }))

    expect(await screen.findByText('Link created')).toBeInTheDocument()
    expect(requests.create).toEqual([{ type: 'Public', userIds: [], expiresAt: null }])
    await waitFor(() =>
      expect(screen.getAllByDisplayValue(`${window.location.origin}/s/tok1`)).toHaveLength(2),
    )
  })

  it('creates a restricted link for users picked via autocomplete', async () => {
    const requests = mockLinks()
    renderWithProviders(<ShareDialog file={file} onClose={vi.fn()} />)

    await userEvent.click(screen.getByRole('tab', { name: 'Restricted' }))
    await userEvent.click(screen.getByRole('button', { name: 'Create restricted link' }))
    expect(await screen.findByText('Add at least one user')).toBeInTheDocument()

    await userEvent.type(screen.getByRole('combobox', { name: 'Add people by email' }), 'bob@')
    const option = await screen.findByRole('option', { name: 'bob@example.com' })
    await userEvent.click(option)

    const selected = screen.getByRole('list', { name: 'Selected people' })
    expect(within(selected).getByText('bob@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Create restricted link' }))
    await screen.findByText('Link created')
    expect(requests.create).toEqual([{ type: 'Restricted', userIds: ['u2'], expiresAt: null }])
  })

  it('revokes an existing link', async () => {
    const requests = mockLinks([
      {
        id: 'l9',
        token: 'tok9',
        type: 'Restricted',
        createdAt: '2026-01-01T00:00:00Z',
        expiresAt: null,
        isRevoked: false,
        users: [{ id: 'u2', email: 'bob@example.com' }],
      },
    ])
    renderWithProviders(<ShareDialog file={file} onClose={vi.fn()} />)

    expect(await screen.findByText('Shared with: bob@example.com')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Revoke' }))

    expect(await screen.findByText('Revoked')).toBeInTheDocument()
    expect(requests.revoked).toEqual(['l9'])
  })

  it('closes on Escape', async () => {
    mockLinks()
    const onClose = vi.fn()
    renderWithProviders(<ShareDialog file={file} onClose={onClose} />)
    expect(screen.getByRole('dialog', { name: 'Share "report.pdf"' })).toBeInTheDocument()
    await userEvent.keyboard('{Escape}')
    expect(onClose).toHaveBeenCalled()
  })
})
