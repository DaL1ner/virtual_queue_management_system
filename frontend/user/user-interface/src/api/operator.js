import apiClient from './index'

export const operatorApi = {
  // Получить очередь
  getQueue() {
    return apiClient.get('/tickets/queue')
      .then(response => response.data)
  },
  // Получить все талоны
  getAllTickets(sorted = false) {
    return apiClient.get('/tickets/all', { params: { sorted } })
      .then(response => response.data)
  },
  // Получить статистику активной сессии
  getActiveStatistics() {
    return apiClient.get('/queue-sessions/statistics/active')
      .then(response => response.data)
  },
  // Вызвать следующего клиента
  callNext(executorId) {
    return apiClient.post('/executor-states/call-next', { executorId })
      .then(response => response.data)
  },
  // Отменить талон
  cancelTicket(ticketId) {
    return apiClient.post(`/tickets/${ticketId}/cancel`)
      .then(response => response.data)
  },
  // Переместить талон на позицию
  moveTicketToPosition(ticketId, position) {
    return apiClient.post(`/tickets/${ticketId}/move-to-position`, { position })
      .then(response => response.data)
  },
  // Получить состояния исполнителей
  getExecutorStates() {
    return apiClient.get('/executor-states')
      .then(response => response.data)
  }
}

export const getQueue = operatorApi.getQueue
export const getAllTickets = operatorApi.getAllTickets
export const getActiveStatistics = operatorApi.getActiveStatistics
export const callNext = operatorApi.callNext
export const cancelTicket = operatorApi.cancelTicket
export const moveTicketToPosition = operatorApi.moveTicketToPosition
export const getExecutorStates = operatorApi.getExecutorStates
