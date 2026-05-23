import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAuthStore } from '@/stores/auth'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import 'bootstrap-icons/font/bootstrap-icons.css'
import './style.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

// Инициализация авторизации: загрузка текущего пользователя при старте
const authStore = useAuthStore()

// Загружаем данные текущего пользователя до монтирования приложения
// Это обеспечивает корректное отображение ролей в Dashboard
authStore.fetchCurrentUser().catch(err => {
  console.warn('Failed to fetch current user on startup (may not be authenticated):', err)
})

app.mount('#app')
