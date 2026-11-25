import { useMutation, useQueryClient } from '@tanstack/react-query'
import { pdfFileApi, pdfFileKeys } from '@entities/pdf-file'

export function useDeleteFile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: pdfFileApi.remove,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pdfFileKeys.lists() }),
  })
}
