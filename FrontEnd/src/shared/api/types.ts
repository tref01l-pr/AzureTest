export type Role = 'Admin' | 'User'

export interface AuthenticationResponse {
  id: string
  role: Role
  accessToken: string
  email: string
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface UrlResponse {
  url: string
}
