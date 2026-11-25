import { z } from 'zod'

export const createLinkSchema = z
  .object({
    type: z.enum(['Public', 'Restricted']),
    users: z.array(z.object({ id: z.string(), email: z.string() })),
    expiresAt: z
      .string()
      .refine((v) => !v || new Date(v).getTime() > Date.now(), 'Expiry must be in the future'),
  })
  .refine((v) => v.type === 'Public' || v.users.length > 0, {
    message: 'Add at least one user',
    path: ['users'],
  })

export type CreateLinkFormValues = z.infer<typeof createLinkSchema>
