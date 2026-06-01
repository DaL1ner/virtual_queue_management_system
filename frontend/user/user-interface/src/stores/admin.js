import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as api from '@/api/admin'

export const useAdminStore = defineStore('admin', () => {
  const queueConfigs = ref([])
  const queueSessions = ref([])
  const users = ref([])
  const serviceTypes = ref([])
  const statistics = ref(null)
  const loading = ref(false)
  const error = ref(null)

  async function fetchQueueConfigs() {
    loading.value = true
    try {
      queueConfigs.value = await api.getQueueConfigs()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки конфигураций'
      console.error('Failed to fetch queue configs', err)
    } finally {
      loading.value = false
    }
  }

  async function fetchQueueSessions(activeOnly = false) {
    loading.value = true
    try {
      queueSessions.value = await api.getQueueSessions(activeOnly)
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки сессий'
      console.error('Failed to fetch queue sessions', err)
    } finally {
      loading.value = false
    }
  }

  async function fetchUsers() {
    loading.value = true
    try {
      users.value = await api.getUsers()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки пользователей'
      console.error('Failed to fetch users', err)
    } finally {
      loading.value = false
    }
  }

  async function fetchServiceTypes() {
    loading.value = true
    try {
      serviceTypes.value = await api.getAllServiceTypesWithConfig()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки типов услуг'
      console.error('Failed to fetch service types', err)
    } finally {
      loading.value = false
    }
  }

  async function fetchStatistics(sessionId = null) {
    try {
      if (sessionId) {
        statistics.value = await api.getSessionStatistics(sessionId)
      } else {
        statistics.value = await api.getActiveSessionStatistics()
      }
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки статистики'
      console.error('Failed to fetch statistics', err)
    }
  }

  function init() {
    fetchQueueConfigs()
    fetchQueueSessions()
    fetchUsers()
    fetchServiceTypes()
    fetchStatistics()
  }

  // Polling
  let pollingInterval = null
  function startPolling(interval = 30000) {
    stopPolling()
    pollingInterval = setInterval(() => {
      fetchQueueSessions()
      fetchStatistics()
    }, interval)
  }
  
  function stopPolling() {
    if (pollingInterval) {
      clearInterval(pollingInterval)
      pollingInterval = null
    }
  }

  // QueueConfig actions
  async function createQueueConfig(data) {
    loading.value = true
    try {
      const newConfig = await api.createQueueConfig(data)
      await fetchQueueConfigs() // Refresh list
      return newConfig
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка создания конфигурации'
      console.error('Failed to create queue config', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateQueueConfig(id, data) {
    loading.value = true
    try {
      const updated = await api.updateQueueConfig(id, data)
      await fetchQueueConfigs()
      return updated
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка обновления конфигурации'
      console.error('Failed to update queue config', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deleteQueueConfig(id) {
    loading.value = true
    try {
      await api.deleteQueueConfig(id)
      await fetchQueueConfigs()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка удаления конфигурации'
      console.error('Failed to delete queue config', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deactivateQueueConfig(id) {
    loading.value = true
    try {
      await api.deactivateQueueConfig(id)
      await fetchQueueConfigs()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка деактивации конфигурации'
      console.error('Failed to deactivate queue config', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  // User actions
  async function createUser(data) {
    loading.value = true
    try {
      const newUser = await api.createUser(data)
      await fetchUsers()
      return newUser
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка создания пользователя'
      console.error('Failed to create user', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateUser(id, data) {
    loading.value = true
    try {
      const updated = await api.updateUser(id, data)
      await fetchUsers()
      return updated
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка обновления пользователя'
      console.error('Failed to update user', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deleteUser(id) {
    loading.value = true
    try {
      await api.deleteUser(id)
      await fetchUsers()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка удаления пользователя'
      console.error('Failed to delete user', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deactivateUser(id) {
    loading.value = true
    try {
      await api.deactivateUser(id)
      await fetchUsers()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка деактивации пользователя'
      console.error('Failed to deactivate user', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function activateUser(id) {
    loading.value = true
    try {
      await api.activateUser(id)
      await fetchUsers()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка активации пользователя'
      console.error('Failed to activate user', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  // ServiceType actions
  async function createServiceType(data) {
    loading.value = true
    try {
      const newType = await api.createServiceType(data)
      await fetchServiceTypes()
      return newType
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка создания типа услуги'
      console.error('Failed to create service type', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateServiceType(id, data) {
    loading.value = true
    try {
      const updated = await api.updateServiceType(id, data)
      await fetchServiceTypes()
      return updated
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка обновления типа услуги'
      console.error('Failed to update service type', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deleteServiceType(id) {
    loading.value = true
    try {
      await api.deleteServiceType(id)
      await fetchServiceTypes()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка удаления типа услуги'
      console.error('Failed to delete service type', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deactivateServiceType(id) {
    loading.value = true
    try {
      await api.deactivateServiceType(id)
      await fetchServiceTypes()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка деактивации типа услуги'
      console.error('Failed to deactivate service type', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  // QueueSession actions (create already exists in API, but add to store)
  async function createQueueSession(data) {
    loading.value = true
    try {
      const newSession = await api.createQueueSession(data)
      await fetchQueueSessions()
      return newSession
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка создания сессии'
      console.error('Failed to create queue session', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function changeSessionStatus(id, status) {
    loading.value = true
    try {
      const updated = await api.changeSessionStatus(id, status)
      await fetchQueueSessions()
      return updated
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка изменения статуса сессии'
      console.error('Failed to change session status', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  return {
    queueConfigs,
    queueSessions,
    users,
    serviceTypes,
    statistics,
    loading,
    error,
    fetchQueueConfigs,
    fetchQueueSessions,
    fetchUsers,
    fetchServiceTypes,
    fetchStatistics,
    startPolling,
    stopPolling,
    init,
    createQueueConfig,
    updateQueueConfig,
    deleteQueueConfig,
    deactivateQueueConfig,
    createUser,
    updateUser,
    deleteUser,
    deactivateUser,
    activateUser,
    createServiceType,
    updateServiceType,
    deleteServiceType,
    deactivateServiceType,
    createQueueSession,
    changeSessionStatus
  }
})
