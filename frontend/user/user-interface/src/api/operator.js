import apiClient from './index'

export const operatorApi = {
  // Получить очередь
  getQueue() {
    return apiClient.get('/api/tickets/queue')
      .then(response => response.data)
  },
  // Получить все талоны
  getAllTickets(sorted = false) {
    return apiClient.get('/api/tickets/all', { params: { sorted } })
      .then(response => response.data)
  },
  // Вызвать следующего клиента
  callNext(executorId) {
    return apiClient.post('/api/executor-states/call-next', { executorId })
      .then(response => response.data)
  },
  // Отменить талон
  cancelTicket(ticketId) {
    return apiClient.post(`/api/tickets/${ticketId}/cancel`)
      .then(response => response.data)
  },
  // Переместить талон на позицию
  moveTicketToPosition(ticketId, position) {
    return apiClient.post(`/api/tickets/${ticketId}/move-to-position`, { position })
      .then(response => response.data)
  },
  // Получить состояния исполнителей
  getExecutorStates() {
    return apiClient.get('/api/executor-states')
      .then(response => response.data)
  }
}

export const getQueue = operatorApi.getQueue
export const getAllTickets = operatorApi.getAllTickets
export const callNext = operatorApi.callNext
export const cancelTicket = operatorApi.cancelTicket
export const moveTicketToPosition = operatorApi.moveTicketToPosition
export const getExecutorStates = operatorApi.getExecutorStates