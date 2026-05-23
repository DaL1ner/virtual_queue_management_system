<template>
  <div class="admin-view">
    <h3 class="mb-4">
      <i class="bi bi-gear me-2"></i>Панель администратора
    </h3>

    <!-- Вкладки -->
    <ul class="nav nav-tabs mb-4">
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'sessions' }"
          @click="activeTab = 'sessions'"
        >
          Сессии очередей
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'configs' }"
          @click="activeTab = 'configs'"
        >
          Конфигурации
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'users' }"
          @click="activeTab = 'users'"
        >
          Пользователи
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'serviceTypes' }"
          @click="activeTab = 'serviceTypes'"
        >
          Типы услуг
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'statistics' }"
          @click="activeTab = 'statistics'"
        >
          Статистика
        </button>
      </li>
    </ul>

    <!-- Содержимое вкладок -->
    <div v-if="activeTab === 'sessions'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Сессии очередей</h5>
          <button class="btn btn-sm btn-primary" @click="showCreateSessionModal = true">
            <i class="bi bi-plus-circle me-1"></i> Создать сессию
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Конфигурация</th>
                <th>Статус</th>
                <th>Начало</th>
                <th>Конец</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="session in adminStore.queueSessions" :key="session.id">
                <td>{{ session.id }}</td>
                <td>{{ session.queueConfigName }}</td>
                <td>
                  <span class="badge" :class="sessionStatusClass(session.status)">
                    {{ session.status }}
                  </span>
                </td>
                <td>{{ formatDate(session.startedAt) }}</td>
                <td>{{ formatDate(session.endedAt) }}</td>
                <td>
                  <div class="btn-group btn-group-sm">
                    <button class="btn btn-outline-info" @click="viewSessionDetails(session)">
                      <i class="bi bi-eye"></i>
                    </button>
                    <button
                      v-if="session.status === 'ACTIVE'"
                      class="btn btn-outline-warning"
                      @click="changeSessionStatus(session.id, 'PAUSED')"
                    >
                      Приостановить
                    </button>
                    <button
                      v-if="session.status === 'PAUSED'"
                      class="btn btn-outline-success"
                      @click="changeSessionStatus(session.id, 'ACTIVE')"
                    >
                      Возобновить
                    </button>
                    <button
                      v-if="session.status !== 'ENDED'"
                      class="btn btn-outline-danger"
                      @click="changeSessionStatus(session.id, 'ENDED')"
                    >
                      Завершить
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'configs'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Конфигурации очередей</h5>
          <button class="btn btn-sm btn-primary">
            <i class="bi bi-plus-circle me-1"></i> Создать конфигурацию
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Название</th>
                <th>Описание</th>
                <th>Макс. очередь</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="config in adminStore.queueConfigs" :key="config.id">
                <td>{{ config.id }}</td>
                <td>{{ config.name }}</td>
                <td>{{ config.description }}</td>
                <td>{{ config.maxQueueSize }}</td>
                <td>
                  <button class="btn btn-sm btn-outline-primary me-1">Редактировать</button>
                  <button class="btn btn-sm btn-outline-danger">Удалить</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'users'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Пользователи</h5>
          <button class="btn btn-sm btn-primary">
            <i class="bi bi-plus-circle me-1"></i> Создать пользователя
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Логин</th>
                <th>Email</th>
                <th>Роли</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="user in adminStore.users" :key="user.id">
                <td>{{ user.id }}</td>
                <td>{{ user.login }}</td>
                <td>{{ user.email }}</td>
                <td>
                  <span v-for="role in user.roles" :key="role.id" class="badge bg-secondary me-1">
                    {{ role.code }}
                  </span>
                </td>
                <td>
                  <button class="btn btn-sm btn-outline-primary me-1">Редактировать</button>
                  <button class="btn btn-sm btn-outline-danger">Удалить</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'serviceTypes'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Типы услуг</h5>
          <button class="btn btn-sm btn-primary">
            <i class="bi bi-plus-circle me-1"></i> Создать тип
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Название</th>
                <th>Код</th>
                <th>Буква</th>
                <th>Приоритет</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="type in adminStore.serviceTypes" :key="type.id">
                <td>{{ type.id }}</td>
                <td>{{ type.name }}</td>
                <td>{{ type.code }}</td>
                <td>{{ type.letter }}</td>
                <td>{{ type.basePriorityLevel }}</td>
                <td>
                  <button class="btn btn-sm btn-outline-primary me-1">Редактировать</button>
                  <button class="btn btn-sm btn-outline-danger">Удалить</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'statistics'">
      <div class="card shadow">
        <div class="card-header">
          <h5 class="card-title mb-0">Статистика активной сессии</h5>
        </div>
        <div class="card-body">
          <div v-if="adminStore.statistics">
            <div class="row">
              <div class="col-md-3">
                <div class="card bg-primary text-white">
                  <div class="card-body">
                    <h6>Всего талонов</h6>
                    <p class="display-6">{{ adminStore.statistics.totalTickets }}</p>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card bg-success text-white">
                  <div class="card-body">
                    <h6>Обслужено</h6>
                    <p class="display-6">{{ adminStore.statistics.servedTickets }}</p>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card bg-warning text-white">
                  <div class="card-body">
                    <h6>В ожидании</h6>
                    <p class="display-6">{{ adminStore.statistics.waitingTickets }}</p>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card bg-info text-white">
                  <div class="card-body">
                    <h6>Среднее время</h6>
                    <p class="display-6">{{ formatDuration(adminStore.statistics.avgServingTime) }}</p>
                  </div>
                </div>
              </div>
            </div>
            <div class="mt-4">
              <h6>Детали</h6>
              <pre class="bg-light p-3 rounded">{{ JSON.stringify(adminStore.statistics, null, 2) }}</pre>
            </div>
          </div>
          <div v-else class="text-center p-4">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="mt-2">Загрузка статистики...</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Ошибки -->
    <div v-if="adminStore.error" class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
      {{ adminStore.error }}
      <button type="button" class="btn-close" @click="adminStore.error = null"></button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAdminStore } from '@/stores/admin'

const adminStore = useAdminStore()
const activeTab = ref('sessions')
const showCreateSessionModal = ref(false)

onMounted(() => {
  // Обновление при монтировании
  adminStore.fetchQueueSessions()
  adminStore.fetchStatistics()
})

function sessionStatusClass(status) {
  const map = {
    DRAFT: 'bg-secondary',
    ACTIVE: 'bg-success',
    PAUSED: 'bg-warning',
    ENDED: 'bg-danger'
  }
  return map[status] || 'bg-light'
}

function formatDate(dateString) {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleString('ru-RU')
}

function formatDuration(seconds) {
  if (!seconds) return '0:00'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

function viewSessionDetails(session) {
  alert(`Детали сессии ${session.id}`)
}

async function changeSessionStatus(sessionId, status) {
  if (confirm(`Изменить статус сессии на ${status}?`)) {
    // Здесь должен быть вызов API
    alert(`Статус изменён (заглушка)`)
    adminStore.fetchQueueSessions()
  }
}
</script>

<style scoped>
.admin-view {
  min-height: 70vh;
}
.card {
  border: none;
}
</style>