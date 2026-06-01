import apiClient from './index'

export const executorApi = {
  // Получить своё состояние
  getMe() {
    return apiClient.get('/executor-states/me')
      .then(response => response.data)
  },
  // Переключить готовность
  toggleReady() {
    return apiClient.post('/executor-states/ready')
      .then(response => response.data)
  },
  // Начать обслуживание
  startServing() {
    return apiClient.post('/executor-states/start-serving')
      .then(response => response.data)
  },
  // Завершить обслуживание
  completeServing() {
    return apiClient.post('/executor-states/complete-serving')
      .then(response => response.data)
  },
  // Отметить неявку
  markNoShow() {
    return apiClient.post('/executor-states/mark-no-show')
      .then(response => response.data)
  },
  // Получить статистику активной сессии очереди
  getQueueStats() {
    return apiClient.get('/queue-sessions/statistics/active')
      .then(response => response.data)
  }
}

export const getExecutorState = executorApi.getMe
export const toggleReady = executorApi.toggleReady
export const startServing = executorApi.startServing
export const completeServing = executorApi.completeServing
export const markNoShow = executorApi.markNoShow
export const getQueueStats = executorApi.getQueueStats
