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
      serviceTypes.value = await api.getServiceTypes()
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

  // Инициализация
  fetchQueueConfigs()
  fetchQueueSessions()
  fetchUsers()
  fetchServiceTypes()
  fetchStatistics()

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
  
  startPolling()

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
    stopPolling
  }
})