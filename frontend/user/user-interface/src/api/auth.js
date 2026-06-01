import apiClient from './index'

export const authApi = {
  login(credentials) {
    return apiClient.post('/auth/login', credentials)
      .then(response => response.data)
  },
  logout() {
    return apiClient.post('/auth/logout')
  },
  getCurrentUser() {
    return apiClient.get('/users/me')
      .then(response => response.data)
  },
  refreshToken() {
    return apiClient.post('/auth/refresh')
      .then(response => response.data)
  }
}

// Для удобства
export const login = authApi.login
export const logout = authApi.logout
export const getCurrentUser = authApi.getCurrentUser
export const refreshToken = authApi.refreshToken