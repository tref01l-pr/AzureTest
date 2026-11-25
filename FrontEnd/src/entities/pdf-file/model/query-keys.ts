export const pdfFileKeys = {
  all: ['files'] as const,
  lists: () => [...pdfFileKeys.all, 'list'] as const,
  list: (page: number, pageSize: number) => [...pdfFileKeys.lists(), { page, pageSize }] as const,
  shared: () => [...pdfFileKeys.all, 'shared-with-me'] as const,
  thumbnail: (id: string) => [...pdfFileKeys.all, 'thumbnail', id] as const,
}
