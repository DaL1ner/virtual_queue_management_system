import { defineStore } from 'pinia'
import { ref, computed, onBeforeUnmount, watch } from 'vue'
import * as api from '@/api/executor'

export const useExecutorStore = defineStore('executor', () => {
  const state = ref(null)
  const loading = ref(false)
  const error = ref(null)
  const queueStats = ref(null)
  const elapsedSeconds = ref(0)
  let elapsedInterval = null

  const isReady = computed(() => state.value?.isReady || false)
  const currentTicket = computed(() => state.value?.currentTicket || null)
  const servingStartedAt = computed(() => currentTicket.value?.serviceStartedAt || null)
  const calledAt = computed(() => currentTicket.value?.calledAt || null)
  const stats = computed(() => state.value?.stats || {})

  // Computed waiting time calculated once from createdAt to calledAt
  const calculatedWaitingTime = computed(() => {
    const ticket = currentTicket.value
    if (!ticket || !ticket.createdAt || !calledAt.value) return 0
    const created = new Date(ticket.createdAt)
    const called = new Date(calledAt.value)
    return Math.floor((called - created) / 1000)
  })

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

  async function fetchQueueStats() {
    try {
      const response = await api.getQueueStats()
      queueStats.value = response
    } catch (err) {
      console.error('Failed to fetch queue stats', err)
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
      startElapsedTimer(response.currentTicket?.serviceStartedAt)
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
      elapsedSeconds.value = 0
      stopElapsedTimer()
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
      elapsedSeconds.value = 0
      stopElapsedTimer()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка отметки неявки'
      throw err
    } finally {
      loading.value = false
    }
  }

  // Calculate waiting time from ticket creation to calledAt (calculated once)
  function calculateWaitingTime(createdAt, calledAt) {
    if (!createdAt || !calledAt) return 0
    const created = new Date(createdAt)
    const called = new Date(calledAt)
    return Math.floor((called - created) / 1000)
  }

  // Start elapsed time counter based on serviceStartedAt
  function startElapsedTimer(serviceStartedAt) {
    stopElapsedTimer()
    if (serviceStartedAt) {
      const started = new Date(serviceStartedAt)
      elapsedSeconds.value = Math.floor((Date.now() - started) / 1000)
    } else {
      elapsedSeconds.value = 0
    }
    elapsedInterval = setInterval(() => {
      elapsedSeconds.value++
    }, 1000)
  }

  function stopElapsedTimer() {
    if (elapsedInterval) {
      clearInterval(elapsedInterval)
      elapsedInterval = null
    }
  }

  // Polling for executor state
  let pollingInterval = null
  function startPolling(interval = 5000) { // 5 seconds instead of 30
    stopPolling()
    pollingInterval = setInterval(() => {
      fetchState()
      fetchQueueStats()
    }, interval)
  }
  
  function stopPolling() {
    if (pollingInterval) {
      clearInterval(pollingInterval)
      pollingInterval = null
    }
  }

  // Инициализация (вызывается из компонента)
  function init() {
    fetchState()
    fetchQueueStats()
    startElapsedTimer(servingStartedAt.value)
  }

  // Watch for ticket changes and update timer accordingly
  watch(currentTicket, (newTicket) => {
    if (newTicket?.serviceStartedAt) {
      startElapsedTimer(newTicket.serviceStartedAt)
    } else {
      elapsedSeconds.value = 0
      stopElapsedTimer()
    }
  })

  // Cleanup on unmount
  onBeforeUnmount(() => {
    stopElapsedTimer()
    stopPolling()
  })

  return {
    state,
    loading,
    error,
    queueStats,
    elapsedSeconds,
    isReady,
    currentTicket,
    servingStartedAt,
    calledAt,
    calculatedWaitingTime,
    stats,
    fetchState,
    fetchQueueStats,
    toggleReadyState,
    startServingTicket,
    completeServingTicket,
    markTicketNoShow,
    calculateWaitingTime,
    startElapsedTimer,
    stopElapsedTimer,
    startPolling,
    stopPolling,
    init
  }
})
