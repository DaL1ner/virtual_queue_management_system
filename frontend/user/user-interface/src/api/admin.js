import apiClient from './index'

export const adminApi = {
  // QueueConfig
  getQueueConfigs() {
    return apiClient.get('/api/queue-configs')
      .then(response => response.data)
  },
  createQueueConfig(data) {
    return apiClient.post('/api/queue-configs', data)
      .then(response => response.data)
  },
  updateQueueConfig(id, data) {
    return apiClient.put(`/api/queue-configs/${id}`, data)
      .then(response => response.data)
  },
  deleteQueueConfig(id) {
    return apiClient.delete(`/api/queue-configs/${id}`)
  },

  // QueueSession
  getQueueSessions(activeOnly = false) {
    return apiClient.get('/api/queue-sessions', { params: { isActive: activeOnly } })
      .then(response => response.data)
  },
  createQueueSession(data) {
    return apiClient.post('/api/queue-sessions', data)
      .then(response => response.data)
  },
  changeSessionStatus(id, status) {
    // Преобразование строкового статуса в числовой enum (Draft=0, Open=1, Paused=2, Closed=3)
    const statusToNumber = {
      'Draft': 0,
      'Open': 1,
      'Paused': 2,
      'Closed': 3
    }
    const numericStatus = statusToNumber[status] ?? status
    return apiClient.post(`/api/queue-sessions/${id}/status`, { status: numericStatus })
      .then(response => response.data)
  },
  getSessionStatistics(id) {
    return apiClient.get(`/api/queue-sessions/statistics/${id}`)
      .then(response => response.data)
  },
  getActiveSessionStatistics() {
    return apiClient.get('/api/queue-sessions/statistics/active')
      .then(response => response.data)
  },

  // User
  getUsers() {
    return apiClient.get('/api/users')
      .then(response => response.data)
  },
  createUser(data) {
    return apiClient.post('/api/users', data)
      .then(response => response.data)
  },
  updateUser(id, data) {
    return apiClient.put(`/api/users/${id}`, data)
      .then(response => response.data)
  },
  deleteUser(id) {
    return apiClient.delete(`/api/users/${id}`)
  },

  // ServiceType
  async getServiceTypes() {
    const response = await apiClient.get('/api/queue-sessions/active/service-types')
    // Endpoint возвращает ActiveSessionServiceTypesResponseDto с полем ServiceTypes
    return response.data.ServiceTypes || []
  },
  createServiceType(data) {
    return apiClient.post('/api/service-types', data)
      .then(response => response.data)
  },
  updateServiceType(id, data) {
    return apiClient.put(`/api/service-types/${id}`, data)
      .then(response => response.data)
  },
  deleteServiceType(id) {
    return apiClient.delete(`/api/service-types/${id}`)
  }
}

export const getQueueConfigs = adminApi.getQueueConfigs
export const createQueueConfig = adminApi.createQueueConfig
export const getQueueSessions = adminApi.getQueueSessions
export const createQueueSession = adminApi.createQueueSession
export const getUsers = adminApi.getUsers
export const createUser = adminApi.createUser
export const getServiceTypes = adminApi.getServiceTypes
export const getActiveSessionStatistics = adminApi.getActiveSessionStatistics
export const getSessionStatistics = adminApi.getSessionStatistics
export const changeSessionStatus = adminApi.changeSessionStatus