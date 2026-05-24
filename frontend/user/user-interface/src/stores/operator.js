import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as api from '@/api/operator'

export const useOperatorStore = defineStore('operator', () => {
  const queue = ref([])
  const allTickets = ref([])
  const executorStates = ref([])
  const statistics = ref({
    totalTickets: 0,
    waitingTickets: 0,
    calledTickets: 0,
    servingTickets: 0,
    servedTickets: 0,
    skippedTickets: 0,
    cancelledTickets: 0,
    avgServiceTimeSec: 0,
    sessionDuration: null
  })
  const loading = ref(false)
  const error = ref(null)

  // Статистика очереди
  const waitingCount = computed(() => statistics.value.waitingTickets)
  const calledCount = computed(() => statistics.value.calledTickets)
  const servingCount = computed(() => statistics.value.servingTickets)
  const servedCount = computed(() => statistics.value.servedTickets)
  const totalTicketsCount = computed(() => statistics.value.totalTickets)

  // Статистика исполнителей
  const totalExecutorsCount = computed(() => executorStates.value.length)
  const readyExecutorsCount = computed(() => executorStates.value.filter(e => e.isReady).length)
  const hasReadyExecutor = computed(() => readyExecutorsCount.value > 0)
  const totalServedByExecutors = computed(() => executorStates.value.reduce((sum, e) => sum + (e.totalServedCount || 0), 0))

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

  async function fetchActiveStatistics() {
    try {
      const data = await api.getActiveStatistics()
      statistics.value = {
        totalTickets: data.totalTickets || 0,
        waitingTickets: data.waitingTickets || 0,
        calledTickets: data.calledTickets || 0,
        servingTickets: data.servingTickets || 0,
        servedTickets: data.servedTickets || 0,
        skippedTickets: data.skippedTickets || 0,
        cancelledTickets: data.cancelledTickets || 0,
        avgServiceTimeSec: data.avgServiceTimeSec || 0,
        sessionDuration: data.sessionDuration || null
      }
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка загрузки статистики'
      console.error('Failed to fetch active statistics', err)
    }
  }

  async function callNextClient(executorId) {
    try {
      await api.callNext(executorId)
      await Promise.all([fetchQueue(), fetchExecutorStates(), fetchActiveStatistics()])
    } catch (err) {
      error.value = err.response?.data?.error || 'Ошибка вызова следующего клиента'
      throw err
    }
  }

  async function cancelTicketById(ticketId) {
    try {
      await api.cancelTicket(ticketId)
      await Promise.all([fetchQueue(), fetchActiveStatistics()])
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

  // Polling
  let pollingInterval = null
  function startPolling(interval = 30000) {
    stopPolling()
    pollingInterval = setInterval(() => {
      fetchQueue()
      fetchExecutorStates()
      fetchActiveStatistics()
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
    fetchQueue()
    fetchExecutorStates()
    fetchActiveStatistics()
  }

  return {
    queue,
    allTickets,
    executorStates,
    statistics,
    loading,
    error,
    waitingCount,
    calledCount,
    servingCount,
    servedCount,
    totalTicketsCount,
    totalExecutorsCount,
    readyExecutorsCount,
    hasReadyExecutor,
    totalServedByExecutors,
    fetchQueue,
    fetchAllTickets,
    fetchExecutorStates,
    fetchActiveStatistics,
    callNextClient,
    cancelTicketById,
    moveTicket,
    startPolling,
    stopPolling,
    init
  }
})
