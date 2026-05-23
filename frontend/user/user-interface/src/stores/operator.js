import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as api from '@/api/operator'

export const useOperatorStore = defineStore('operator', () => {
  const queue = ref([])
  const allTickets = ref([])
  const executorStates = ref([])
  const loading = ref(false)
  const error = ref(null)

  const queueLength = computed(() => queue.value.length)
  const waitingCount = computed(() => queue.value.filter(t => t.status === 'WAITING').length)
  const calledCount = computed(() => queue.value.filter(t => t.status === 'CALLED').length)

  async function fetchQueue() {
    loading.value = true
    try {
      const data = await api.getQueue()
      queue.value = data
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки очереди'
      console.error('Failed to fetch queue', err)
    } finally {
      loading.value = false
    }
  }

  async function fetchAllTickets(sorted = false) {
    loading.value = true
    try {
      const data = await api.getAllTickets(sorted)
      allTickets.value = data
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки талонов'
      console.error('Failed to fetch tickets', err)
    } finally {
      loading.value = false
    }
  }

  async function fetchExecutorStates() {
    try {
      const data = await api.getExecutorStates()
      executorStates.value = data
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки состояний исполнителей'
      console.error('Failed to fetch executor states', err)
    }
  }

  async function callNextClient(executorId) {
    try {
      await api.callNext(executorId)
      await Promise.all([fetchQueue(), fetchExecutorStates()])
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка вызова следующего клиента'
      throw err
    }
  }

  async function cancelTicketById(ticketId) {
    try {
      await api.cancelTicket(ticketId)
      await fetchQueue()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка отмены талона'
      throw err
    }
  }

  async function moveTicket(ticketId, position) {
    try {
      await api.moveTicketToPosition(ticketId, position)
      await fetchQueue()
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка перемещения талона'
      throw err
    }
  }

  // Инициализация
  fetchQueue()
  fetchExecutorStates()

  // Polling
  let pollingInterval = null
  function startPolling(interval = 30000) {
    stopPolling()
    pollingInterval = setInterval(() => {
      fetchQueue()
      fetchExecutorStates()
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
    queue,
    allTickets,
    executorStates,
    loading,
    error,
    queueLength,
    waitingCount,
    calledCount,
    fetchQueue,
    fetchAllTickets,
    fetchExecutorStates,
    callNextClient,
    cancelTicketById,
    moveTicket,
    startPolling,
    stopPolling
  }
})