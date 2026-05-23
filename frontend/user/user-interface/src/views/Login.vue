<template>
  <div class="login-container d-flex justify-content-center align-items-center min-vh-100 bg-light">
    <div class="card shadow-lg" style="width: 100%; max-width: 400px;">
      <div class="card-body p-4">
        <div class="text-center mb-4">
          <h2 class="fw-bold text-primary">Вход в систему</h2>
          <p class="text-muted">Введите логин и пароль для доступа к интерфейсу сотрудника</p>
        </div>

        <form @submit.prevent="handleLogin">
          <div class="mb-3">
            <label for="Login" class="form-label">Логин</label>
            <input
              type="text"
              class="form-control"
              id="Login"
              v-model="credentials.Login"
              placeholder="Введите логин"
              required
              :disabled="loading"
            />
          </div>

          <div class="mb-3">
            <label for="Password" class="form-label">Пароль</label>
            <input
              type="password"
              class="form-control"
              id="Password"
              v-model="credentials.Password"
              placeholder="Введите пароль"
              required
              :disabled="loading"
            />
          </div>

          <div v-if="error" class="alert alert-danger alert-dismissible fade show" role="alert">
            {{ error }}
            <button type="button" class="btn-close" @click="error = ''"></button>
          </div>

          <button
            type="submit"
            class="btn btn-primary w-100 py-2"
            :disabled="loading"
          >
            <span v-if="loading" class="spinner-border spinner-border-sm me-2" role="status"></span>
            {{ loading ? 'Вход...' : 'Войти' }}
          </button>
        </form>

        <div class="mt-4 text-center text-muted">
          <small>Система управления виртуальной очередью</small>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const credentials = ref({
  Login: '',
  Password: ''
})
const loading = ref(false)
const error = ref('')

async function handleLogin() {
  if (!credentials.value.Login || !credentials.value.Password) {
    error.value = 'Заполните все поля'
    return
  }

  loading.value = true
  error.value = ''

  try {
    await authStore.loginUser(credentials.value)
    router.push('/dashboard')
  } catch (err) {
    error.value = err.response?.data?.error || 'Неверный логин или пароль'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}
.card {
  border-radius: 15px;
  border: none;
}
</style>