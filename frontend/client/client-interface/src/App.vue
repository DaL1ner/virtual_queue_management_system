<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router';
import { useAuthStore } from './stores/auth.store';
import { ref } from 'vue';

const authStore = useAuthStore();

// Глобальные уведомления
const globalError = ref('');
const globalSuccess = ref('');

function showGlobalError(message: string) {
  globalError.value = message;
  setTimeout(() => {
    globalError.value = '';
  }, 5000);
}

function showGlobalSuccess(message: string) {
  globalSuccess.value = message;
  setTimeout(() => {
    globalSuccess.value = '';
  }, 3000);
}

// Экспортируем функции для использования в дочерних компонентах
defineExpose({
  showGlobalError,
  showGlobalSuccess,
});

// Функция выхода
function handleLogout() {
  if (confirm('Вы уверены, что хотите выйти?')) {
    authStore.clearToken();
    showGlobalSuccess('Вы успешно вышли из системы');
  }
}
</script>

<template>
  <div class="app">
    <!-- Навигационная панель -->
    <nav class="navbar navbar-expand-lg navbar-light bg-light shadow-sm">
      <div class="container">
        <RouterLink class="navbar-brand d-flex align-items-center" to="/">
          <span class="fw-bold text-primary">Virtual Queue</span>
          <span class="badge bg-secondary ms-2">Beta</span>
        </RouterLink>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" id="navbarNav">
          <ul class="navbar-nav">
            <li v-if="!authStore.isAuthenticated" class="nav-item">
              <RouterLink
                class="nav-link"
                to="/"
              >
                <i class="bi bi-house-door me-1"></i>Главная
              </RouterLink>
            </li>
            <li v-if="authStore.isAuthenticated" class="nav-item">
              <RouterLink 
                class="nav-link" 
                to="/ticket"
              >
                <i class="bi bi-ticket-detailed me-1"></i>Мой талон
              </RouterLink>
            </li>
            <li class="nav-item">
              <a class="nav-link" href="#" @click.prevent="showGlobalSuccess('Функция в разработке')">
                <i class="bi bi-info-circle me-1"></i>О системе
              </a>
            </li>
          </ul>
          <ul class="navbar-nav ms-auto">
            <li v-if="authStore.isAuthenticated" class="nav-item dropdown">
              <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                <i class="bi bi-person-circle me-1"></i>Аккаунт
              </a>
              <ul class="dropdown-menu dropdown-menu-end">
                <li><span class="dropdown-item-text small text-muted">ID устройства: {{ authStore.deviceFingerprint?.substring(0, 8) }}...</span></li>
                <li><hr class="dropdown-divider"></li>
                <li><button class="dropdown-item text-danger" @click="handleLogout"><i class="bi bi-box-arrow-right me-1"></i>Выйти</button></li>
              </ul>
            </li>
            <li v-else class="nav-item">
              <span class="nav-link text-muted">
                <i class="bi bi-person me-1"></i>Гость
              </span>
            </li>
          </ul>
        </div>
      </div>
    </nav>

    <!-- Глобальные уведомления -->
    <div class="container">
      <div v-if="globalError" class="alert alert-danger alert-dismissible fade show" role="alert">
        <i class="bi bi-exclamation-triangle me-2"></i>{{ globalError }}
        <button type="button" class="btn-close" @click="globalError = ''"></button>
      </div>
      <div v-if="globalSuccess" class="alert alert-success alert-dismissible fade show" role="alert">
        <i class="bi bi-check-circle me-2"></i>{{ globalSuccess }}
        <button type="button" class="btn-close" @click="globalSuccess = ''"></button>
      </div>
    </div>

    <!-- Основное содержимое -->
    <main class="container mb-5">
      <RouterView />
    </main>

    <!-- Футер -->
    <footer class="mt-auto py-4 bg-light border-top">
      <div class="container">
        <div class="row">
          <div class="col-md-6">
            <h5 class="fw-bold">Virtual Queue</h5>
            <p class="text-muted small">
              Система управления виртуальными очередями для организаций и учреждений.
            </p>
          </div>
          <div class="col-md-6">
            <h6 class="fw-bold">Навигация</h6>
            <ul class="list-unstyled">
              <li><RouterLink class="text-decoration-none small" to="/">Главная</RouterLink></li>
              <li v-if="authStore.isAuthenticated"><RouterLink class="text-decoration-none small" to="/ticket">Мой талон</RouterLink></li>
              <li><a href="#" class="text-decoration-none small" @click.prevent="showGlobalSuccess('Контакты в разработке')">Контакты</a></li>
            </ul>
          </div>
        </div>
        <hr class="my-3">
        <div class="row">
          <div class="col text-center">
            <p class="small text-muted mb-0">
              &copy; 2026 Virtual Queue Management System. Все права защищены.
            </p>
            <p class="small text-muted">
              <i class="bi bi-shield-check me-1"></i>Ваши данные защищены
            </p>
          </div>
        </div>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

main {
  flex: 1;
}

.navbar-brand {
  font-size: 1.5rem;
}

footer {
  background-color: #f8f9fa;
}

@media (max-width: 768px) {
  .navbar-brand {
    font-size: 1.2rem;
  }
}
</style>
