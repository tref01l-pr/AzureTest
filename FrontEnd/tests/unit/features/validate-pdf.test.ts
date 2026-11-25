import { describe, expect, it } from 'vitest'
import { MAX_PDF_BYTES, validatePdf } from '@features/files/upload-file'

const file = (name: string, type: string, size: number) =>
  new File([new Uint8Array(size)], name, { type })

describe('validatePdf', () => {
  it('accepts a PDF', () => expect(validatePdf(file('a.pdf', 'application/pdf', 10))).toBeNull())
  it('rejects other types', () =>
    expect(validatePdf(file('a.png', 'image/png', 10))).toBe('Only PDF files are allowed'))
  it('rejects empty files', () =>
    expect(validatePdf(file('a.pdf', 'application/pdf', 0))).toBe('The file is empty'))
  it('rejects files over 20 MB', () =>
    expect(validatePdf(file('a.pdf', 'application/pdf', MAX_PDF_BYTES + 1))).toBe(
      'The file is larger than 20 MB',
    ))
})
