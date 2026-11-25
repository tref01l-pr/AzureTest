import { z } from 'zod'

export const MAX_PDF_BYTES = 20 * 1024 * 1024

export const pdfFileSchema = z
  .instanceof(File)
  .refine(
    (f) => f.type === 'application/pdf' || f.name.toLowerCase().endsWith('.pdf'),
    'Only PDF files are allowed',
  )
  .refine((f) => f.size > 0, 'The file is empty')
  .refine((f) => f.size <= MAX_PDF_BYTES, 'The file is larger than 20 MB')

export function validatePdf(file: File): string | null {
  const result = pdfFileSchema.safeParse(file)
  return result.success ? null : (result.error.issues[0]?.message ?? 'Invalid file')
}
