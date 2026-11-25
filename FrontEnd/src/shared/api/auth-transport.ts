import axios from 'axios'
import { env } from '../config'
import type { AuthenticationResponse } from './types'

export const authTransport = axios.create({
  baseURL: env.apiBaseUrl,
  withCredentials: true,
})

export async function refreshAccessToken(): Promise<AuthenticationResponse> {
  const { data } = await authTransport.post<AuthenticationResponse>('refresh-access-token')
  return data
}
