import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { pdfFileApi, pdfFileKeys } from '@entities/pdf-file'

export function useUploadFile() {
  const queryClient = useQueryClient()
  const [progress, setProgress] = useState(0)
  const mutation = useMutation({
    mutationFn: (file: File) => {
      setProgress(0)
      return pdfFileApi.upload(file, setProgress)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pdfFileKeys.lists() }),
  })
  return { ...mutation, progress }
}
