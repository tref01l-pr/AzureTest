export const userKeys = {
  all: ['users'] as const,
  search: (email: string) => [...userKeys.all, 'search', email] as const,
}
