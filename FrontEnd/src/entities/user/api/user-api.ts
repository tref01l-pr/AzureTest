import { apiClient, authTransport, type AuthenticationResponse } from '@shared/api'
import type { LoginRequest, RegisterRequest, UserSummary } from '../model/types'

export const userApi = {
  login: (body: LoginRequest) =>
    authTransport.post<AuthenticationResponse>('login', body).then((r) => r.data),

  register: (body: RegisterRequest) =>
    authTransport.post<AuthenticationResponse>('user-registration', body).then((r) => r.data),

  logout: () => apiClient.post('logout').then(() => undefined),

  search: (email: string, signal?: AbortSignal) =>
    apiClient.get<UserSummary[]>('users/search', { params: { email }, signal }).then((r) => r.data),
}
