import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as api from '@/api/executor'

export const useExecutorStore = defineStore('executor', () => {
  const state = ref(null)
  const loading = ref(false)
  const error = ref(null)

  const isReady = computed(() => state.value?.isReady || false)
  const currentTicket = computed(() => state.value?.currentTicket || null)
  const servingStartedAt = computed(() => state.value?.servingStartedAt || null)
  const stats = computed(() => state.value?.stats || {})

  async function fetchState() {
    loading.value = true
    error.value = null
    try {
      const response = await api.getExecutorState()
      state.value = response
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки состояния'
      console.error('Failed to fetch executor state', err)
    } finally {
      loading.value = false
    }
  }

  async function toggleReadyState() {
    if (loading.value) return
    loading.value = true
    try {
      const response = await api.toggleReady()
      state.value = response
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка переключения готовности'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function startServingTicket() {
    if (loading.value) return
    loading.value = true
    try {
      const response = await api.startServing()
      state.value = response
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка начала обслуживания'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function completeServingTicket() {
    if (loading.value) return
    loading.value = true
    try {
      const response = await api.completeServing()
      state.value = response
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка завершения обслуживания'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function markTicketNoShow() {
    if (loading.value) return
    loading.value = true
    try {
      const response = await api.markNoShow()
      state.value = response
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка отметки неявки'
      throw err
    } finally {
      loading.value = false
    }
  }

  // Инициализация
  fetchState()

  // Polling
  let pollingInterval = null
  function startPolling(interval = 30000) {
    stopPolling()
    pollingInterval = setInterval(() => {
      fetchState()
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
    state,
    loading,
    error,
    isReady,
    currentTicket,
    servingStartedAt,
    stats,
    fetchState,
    toggleReadyState,
    startServingTicket,
    completeServingTicket,
    markTicketNoShow,
    startPolling,
    stopPolling
  }
})