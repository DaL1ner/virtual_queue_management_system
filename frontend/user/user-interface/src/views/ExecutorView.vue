<template>
  <div class="executor-view">
    <div class="row">
      <!-- Левая колонка: состояние и управление -->
      <div class="col-md-4">
        <div class="card shadow mb-4">
          <div class="card-header bg-primary text-white">
            <h5 class="card-title mb-0">
              <i class="bi bi-person-workspace me-2"></i>Моё состояние
            </h5>
          </div>
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <span class="fw-bold">Готовность:</span>
              <div class="form-check form-switch">
                <input
                  class="form-check-input"
                  type="checkbox"
                  role="switch"
                  :checked="executorStore.isReady"
                  @change="handleToggleReady"
                  :disabled="executorStore.loading || currentTicket"
                />
                <label class="form-check-label">
                  {{ executorStore.isReady ? 'Готов' : 'Не готов' }}
                </label>
              </div>
            </div>

            <div v-if="currentTicket" class="alert alert-info">
              <h6 class="alert-heading">Текущий клиент</h6>
              <p class="mb-1"><strong>Талон №{{ currentTicket.id }}</strong></p>
              <p class="mb-1">{{ currentTicket.clientName }} {{ currentTicket.clientSurname }}</p>
              <p class="mb-0">Приоритет: <span class="badge bg-warning">{{ currentTicket.priorityLevel }}</span></p>
            </div>
            <div v-else class="alert alert-secondary">
              Нет текущего клиента
            </div>

            <div class="d-grid gap-2">
              <button
                class="btn btn-success"
                :disabled="!currentTicket || servingStartedAt || executorStore.loading"
                @click="handleStartServing"
              >
                <i class="bi bi-play-circle me-1"></i> Начать обслуживание
              </button>
              <button
                class="btn btn-warning"
                :disabled="!servingStartedAt || executorStore.loading"
                @click="handleCompleteServing"
              >
                <i class="bi bi-check-circle me-1"></i> Завершить обслуживание
              </button>
              <button
                class="btn btn-danger"
                :disabled="!currentTicket || executorStore.loading"
                @click="handleMarkNoShow"
              >
                <i class="bi bi-x-circle me-1"></i> Клиент не явился
              </button>
            </div>
          </div>
        </div>

        <!-- Статистика -->
        <div class="card shadow">
          <div class="card-header bg-secondary text-white">
            <h5 class="card-title mb-0">
              <i class="bi bi-graph-up me-2"></i>Статистика
            </h5>
          </div>
          <div class="card-body">
            <ul class="list-group list-group-flush">
              <li class="list-group-item d-flex justify-content-between">
                <span>Обслужено сегодня:</span>
                <span class="fw-bold">{{ executorStore.stats.servedToday || 0 }}</span>
              </li>
              <li class="list-group-item d-flex justify-content-between">
                <span>Среднее время:</span>
                <span class="fw-bold">{{ formatDuration(executorStore.stats.avgServingTime) }}</span>
              </li>
              <li class="list-group-item d-flex justify-content-between">
                <span>В очереди:</span>
                <span class="fw-bold">{{ executorStore.stats.queueLength || 0 }} чел.</span>
              </li>
            </ul>
          </div>
        </div>
      </div>

      <!-- Правая колонка: информация о текущем обслуживании -->
      <div class="col-md-8">
        <div class="card shadow">
          <div class="card-header bg-info text-white">
            <h5 class="card-title mb-0">
              <i class="bi bi-clock-history me-2"></i>Текущее обслуживание
            </h5>
          </div>
          <div class="card-body">
            <div v-if="servingStartedAt" class="text-center">
              <h3 class="display-4">{{ formatElapsedTime(servingStartedAt) }}</h3>
              <p class="text-muted">Время обслуживания</p>
              <div class="row mt-4">
                <div class="col">
                  <div class="card bg-light">
                    <div class="card-body">
                      <h6>Информация о клиенте</h6>
                      <p v-if="currentTicket">
                        <strong>Тип услуги:</strong> {{ currentTicket.serviceTypeName }}<br>
                        <strong>Время ожидания:</strong> {{ formatDuration(currentTicket.waitingTime) }}<br>
                        <strong>Дата записи:</strong> {{ formatDate(currentTicket.createdAt) }}
                      </p>
                    </div>
                  </div>
                </div>
                <div class="col">
                  <div class="card bg-light">
                    <div class="card-body">
                      <h6>Действия</h6>
                      <p>Используйте кнопки слева для управления процессом обслуживания.</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="text-center py-5">
              <i class="bi bi-hourglass-split display-1 text-muted"></i>
              <h4 class="mt-3">Обслуживание не начато</h4>
              <p>Начните обслуживание, когда клиент подойдёт.</p>
            </div>
          </div>
        </div>

        <!-- История -->
        <div class="card shadow mt-4">
          <div class="card-header">
            <h5 class="card-title mb-0">Последние клиенты</h5>
          </div>
          <div class="card-body">
            <p class="text-muted">Здесь будет история обслуживания (в будущем).</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Ошибки -->
    <div v-if="executorStore.error" class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
      {{ executorStore.error }}
      <button type="button" class="btn-close" @click="executorStore.error = null"></button>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useExecutorStore } from '@/stores/executor'

const executorStore = useExecutorStore()

const currentTicket = computed(() => executorStore.currentTicket)
const servingStartedAt = computed(() => executorStore.servingStartedAt)

onMounted(() => {
  // Загружаем состояние при монтировании
  executorStore.fetchState()
})

function handleToggleReady() {
  executorStore.toggleReadyState()
}

function handleStartServing() {
  executorStore.startServingTicket()
}

function handleCompleteServing() {
  executorStore.completeServingTicket()
}

function handleMarkNoShow() {
  if (confirm('Вы уверены, что клиент не явился?')) {
    executorStore.markTicketNoShow()
  }
}

function formatDuration(seconds) {
  if (!seconds) return '0:00'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

function formatElapsedTime(startTime) {
  if (!startTime) return '0:00'
  const start = new Date(startTime)
  const now = new Date()
  const diff = Math.floor((now - start) / 1000)
  return formatDuration(diff)
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<style scoped>
.executor-view {
  min-height: 70vh;
}
.card {
  border: none;
}
</style>