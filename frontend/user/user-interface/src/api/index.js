import axios from 'axios'

// API доступен по относительному пути /api — в dev режиме Vite проксирует запросы на бэкенд
const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  },
  timeout: 10000
})

// Интерцептор для добавления токена
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Интерцептор для обработки ошибок
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      const { status, data } = error.response
      
      // Автоматический выход при 401
      if (status === 401) {
        localStorage.removeItem('token')
        window.location.href = '/login'
      }
      
      // Преобразование ошибок в читаемый формат
      const message = data?.error || data?.message || error.message
      error.userMessage = message
      
      // Логирование ошибок
      console.error('API Error:', {
        url: error.config?.url,
        status,
        message
      })
    } else if (error.request) {
      error.userMessage = 'Сервер не отвечает. Проверьте подключение к сети.'
    } else {
      error.userMessage = 'Произошла ошибка при выполнении запроса.'
    }
    
    return Promise.reject(error)
  }
)

export default apiClient
