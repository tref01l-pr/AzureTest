import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { env } from '../config'
import { useAuthStore } from '../lib/auth/auth-store'
import { refreshAccessToken } from './auth-transport'

type RetriableConfig = InternalAxiosRequestConfig & { _retry?: boolean }

export const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
  withCredentials: true,
})

let onSessionExpired: () => void = () => useAuthStore.getState().clear()

export function setSessionExpiredHandler(handler: () => void): void {
  onSessionExpired = handler
}

let refreshPromise: Promise<string> | null = null

function refreshOnce(): Promise<string> {
  refreshPromise ??= refreshAccessToken()
    .then((response) => {
      useAuthStore.getState().setSession(response)
      return response.accessToken
    })
    .finally(() => {
      refreshPromise = null
    })
  return refreshPromise
}

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

apiClient.interceptors.response.use(undefined, async (error: AxiosError) => {
  const config = error.config as RetriableConfig | undefined
  const hadToken = Boolean(config?.headers?.Authorization)
  if (!config || error.response?.status !== 401 || config._retry || !hadToken) {
    throw error
  }

  config._retry = true
  let token: string
  try {
    token = await refreshOnce()
  } catch {
    onSessionExpired()
    throw error
  }
  config.headers.Authorization = `Bearer ${token}`
  return apiClient(config)
})
