import apiClient from './index'

export const adminApi = {
  // QueueConfig
  getQueueConfigs() {
    return apiClient.get('/queue-configs')
      .then(response => response.data)
  },
  createQueueConfig(data) {
    return apiClient.post('/queue-configs', data)
      .then(response => response.data)
  },
  updateQueueConfig(id, data) {
    return apiClient.put(`/queue-configs/${id}`, data)
      .then(response => response.data)
  },
  deleteQueueConfig(id) {
    return apiClient.delete(`/queue-configs/${id}`)
  },
  deactivateQueueConfig(id) {
    return apiClient.patch(`/queue-configs/${id}/deactivate`)
      .then(response => response.data)
  },

  // QueueSession
  getQueueSessions(activeOnly = false) {
    return apiClient.get('/queue-sessions', { params: { isActive: activeOnly } })
      .then(response => response.data)
  },
  createQueueSession(data) {
    return apiClient.post('/queue-sessions', data)
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
    return apiClient.post(`/queue-sessions/${id}/status`, { status: numericStatus })
      .then(response => response.data)
  },
  getSessionStatistics(id) {
    return apiClient.get(`/queue-sessions/statistics/${id}`)
      .then(response => response.data)
  },
  getActiveSessionStatistics() {
    return apiClient.get('/queue-sessions/statistics/active')
      .then(response => response.data)
  },

  // User
  getUsers() {
    return apiClient.get('/users')
      .then(response => response.data)
  },
  createUser(data) {
    return apiClient.post('/users', data)
      .then(response => response.data)
  },
  updateUser(id, data) {
    return apiClient.patch(`/users/${id}`, data)
      .then(response => response.data)
  },
  deleteUser(id) {
    return apiClient.delete(`/users/${id}`)
  },
  deactivateUser(id) {
    return apiClient.patch(`/users/${id}/deactivate`)
      .then(response => response.data)
  },
  activateUser(id) {
    return apiClient.patch(`/users/${id}/activate`)
      .then(response => response.data)
  },

  // Role
  getRoles() {
    return apiClient.get('/roles')
      .then(response => response.data)
  },

  // ServiceType
  async getServiceTypes() {
    const response = await apiClient.get('/queue-sessions/active/service-types')
    // Endpoint возвращает ActiveSessionServiceTypesResponseDto с полем ServiceTypes
    return response.data.ServiceTypes || []
  },
  async getAllServiceTypesWithConfig() {
    const response = await apiClient.get('/service-types/all')
    return response.data || []
  },
  createServiceType(data) {
    return apiClient.post('/service-types', data)
      .then(response => response.data)
  },
  updateServiceType(id, data) {
    return apiClient.put(`/service-types/${id}`, data)
      .then(response => response.data)
  },
  deleteServiceType(id) {
    return apiClient.delete(`/service-types/${id}`)
  },
  deactivateServiceType(id) {
    return apiClient.patch(`/service-types/${id}/deactivate`)
      .then(response => response.data)
  }
}

export const getQueueConfigs = adminApi.getQueueConfigs
export const createQueueConfig = adminApi.createQueueConfig
export const updateQueueConfig = adminApi.updateQueueConfig
export const deleteQueueConfig = adminApi.deleteQueueConfig
export const getQueueSessions = adminApi.getQueueSessions
export const createQueueSession = adminApi.createQueueSession
export const changeSessionStatus = adminApi.changeSessionStatus
export const getUsers = adminApi.getUsers
export const createUser = adminApi.createUser
export const updateUser = adminApi.updateUser
export const deleteUser = adminApi.deleteUser
export const getServiceTypes = adminApi.getServiceTypes
export const getAllServiceTypesWithConfig = adminApi.getAllServiceTypesWithConfig
export const getActiveSessionStatistics = adminApi.getActiveSessionStatistics
export const getSessionStatistics = adminApi.getSessionStatistics
export const deactivateQueueConfig = adminApi.deactivateQueueConfig
export const deactivateUser = adminApi.deactivateUser
export const activateUser = adminApi.activateUser
export const getRoles = adminApi.getRoles
export const createServiceType = adminApi.createServiceType
export const updateServiceType = adminApi.updateServiceType
export const deleteServiceType = adminApi.deleteServiceType
export const deactivateServiceType = adminApi.deactivateServiceType
