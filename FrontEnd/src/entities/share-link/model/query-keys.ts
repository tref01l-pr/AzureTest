export const shareLinkKeys = {
  all: ['share-links'] as const,
  byFile: (fileId: string) => [...shareLinkKeys.all, 'file', fileId] as const,
  byToken: (token: string) => [...shareLinkKeys.all, 'token', token] as const,
}
