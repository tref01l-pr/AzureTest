import { act, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { PROCESSING_POLL_MS, useMyFiles, type PdfFile } from '@entities/pdf-file'
import { useSearchUsers } from '@features/users/search-users'
import { api, server } from '../../msw/server'
import { renderHookWithProviders } from '../../test-utils'

const pdf = (status: PdfFile['status']): PdfFile => ({
  id: 'f1',
  originalFileName: 'a.pdf',
  sizeBytes: 100,
  uploadedAt: '2026-01-01T00:00:00Z',
  status,
  pageCount: status === 'Ready' ? 3 : null,
  rejectReason: null,
  hasThumbnail: status === 'Ready',
})

const page = (items: PdfFile[]) => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 10,
  totalPages: 1,
})

afterEach(() => vi.useRealTimers())

describe('useMyFiles', () => {
  it('polls while a file is Processing and stops once it is Ready', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    let calls = 0
    server.use(
      http.get(api('files'), () => {
        calls++
        return HttpResponse.json(page([pdf(calls === 1 ? 'Processing' : 'Ready')]))
      }),
    )

    const { result } = renderHookWithProviders(() => useMyFiles(1))
    await waitFor(() => expect(result.current.data?.items[0]?.status).toBe('Processing'))

    await act(() => vi.advanceTimersByTimeAsync(PROCESSING_POLL_MS))
    await waitFor(() => expect(result.current.data?.items[0]?.status).toBe('Ready'))

    await act(() => vi.advanceTimersByTimeAsync(PROCESSING_POLL_MS * 3))
    expect(calls).toBe(2)
  })
})

describe('useSearchUsers', () => {
  it('debounces and skips terms shorter than 2 characters', async () => {
    const terms: string[] = []
    server.use(
      http.get(api('users/search'), ({ request }) => {
        terms.push(new URL(request.url).searchParams.get('email') ?? '')
        return HttpResponse.json([{ id: 'u2', email: 'bob@example.com' }])
      }),
    )

    let term = 'b'
    const { result, rerender } = renderHookWithProviders(() => useSearchUsers(term))
    term = 'bo'
    rerender()
    term = 'bob'
    rerender()

    await waitFor(() =>
      expect(result.current.data).toEqual([{ id: 'u2', email: 'bob@example.com' }]),
    )
    expect(terms).toEqual(['bob'])
  })
})
