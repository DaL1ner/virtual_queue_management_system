<template>
  <div class="operator-view">
    <!-- Статистика очереди -->
    <div class="row mb-4">
      <div class="col-md-3">
        <div class="card bg-primary text-white shadow">
          <div class="card-body">
            <h5 class="card-title"><i class="bi bi-hourglass-split me-2"></i>Ожидают</h5>
            <p class="display-4">{{ operatorStore.waitingCount }}</p>
            <small>В очереди</small>
          </div>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card bg-warning text-white shadow">
          <div class="card-body">
            <h5 class="card-title"><i class="bi bi-megaphone me-2"></i>Вызваны</h5>
            <p class="display-4">{{ operatorStore.calledCount }}</p>
            <small>Ожидают обслуживания</small>
          </div>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card bg-info text-white shadow">
          <div class="card-body">
            <h5 class="card-title"><i class="bi bi-person-fill me-2"></i>Обслуживаются</h5>
            <p class="display-4">{{ operatorStore.servingCount }}</p>
            <small>В работе</small>
          </div>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card bg-success text-white shadow">
          <div class="card-body">
            <h5 class="card-title"><i class="bi bi-check-circle-fill me-2"></i>Обслужено</h5>
            <p class="display-4">{{ operatorStore.servedCount }}</p>
            <small>Завершено</small>
          </div>
        </div>
      </div>
    </div>

    <!-- Исполнители - общая статистика -->
    <div class="row mb-4">
      <div class="col-12">
        <div class="card shadow">
          <div class="card-header bg-dark text-white d-flex justify-content-between align-items-center">
            <h5 class="card-title mb-0">
              <i class="bi bi-people-fill me-2"></i>Исполнители
            </h5>
            <div class="btn-group btn-group-sm">
              <button class="btn btn-outline-light" @click="refreshAll" :disabled="operatorStore.loading">
                <i class="bi bi-arrow-clockwise me-1"></i> Обновить
              </button>
              <button
                class="btn btn-outline-info"
                @click="handleCallFirstAvailable"
                :disabled="!operatorStore.hasReadyExecutor || operatorStore.loading"
                title="Вызвать первого доступного клиента"
              >
                <i class="bi bi-lightning-charge me-1"></i> Быстрый вызов
              </button>
            </div>
          </div>
          <div class="card-body">
            <!-- Список исполнителей -->
            <div class="row">
              <div v-if="operatorStore.executorStates.length === 0" class="col-12 text-center text-muted py-4">
                Нет активных исполнителей
              </div>
              <div v-else class="col-12">
                <div class="table-responsive">
                  <table class="table table-hover align-middle">
                    <thead class="table-light">
                      <tr>
                        <th>Исполнитель</th>
                        <th>Статус</th>
                        <th>Обслужено</th>
                        <th>Ср. время</th>
                        <th>Текущий клиент</th>
                        <th>Действия</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="executor in operatorStore.executorStates" :key="executor.userId">
                        <td>
                          <div class="d-flex align-items-center">
                            <div class="avatar-circle me-2" :class="executor.isReady ? 'bg-success' : 'bg-danger'">
                              {{ getInitials(executor.userName) }}
                            </div>
                            <div>
                              <strong>{{ executor.userName }}</strong>
                              <div class="small text-muted">ID: {{ executor.userId }}</div>
                            </div>
                          </div>
                        </td>
                        <td>
                          <span class="badge" :class="executor.isReady ? 'bg-success' : 'bg-danger'">
                            {{ executor.isReady ? 'Готов' : 'Не готов' }}
                          </span>
                        </td>
                        <td>
                          <span class="fw-bold">{{ executor.totalServedCount || 0 }}</span>
                        </td>
                        <td>
                          {{ formatDuration(executor.avgServiceTimeSec ? executor.avgServiceTimeSec * 1000 : 0) }}
                        </td>
                        <td>
                          <span v-if="executor.currentTicket" class="small">
                            Талон №{{ executor.currentTicket.id }}
                          </span>
                          <span v-else class="text-muted small">—</span>
                        </td>
                        <td>
                          <button
                            class="btn btn-sm btn-outline-primary"
                            @click="handleCallNext(executor.userId)"
                            :disabled="!executor.isReady || operatorStore.loading"
                            title="Вызвать следующего"
                          >
                            <i class="bi bi-megaphone me-1"></i> Вызвать
                          </button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            <!-- Итоговая статистика -->
            <div class="row mt-3 pt-3 border-top">
              <div class="col-md-4">
                <div class="d-flex align-items-center">
                  <i class="bi bi-people me-2 text-primary"></i>
                  <span class="text-muted">Всего исполнителей:</span>
                  <span class="ms-2 fw-bold">{{ operatorStore.totalExecutorsCount }}</span>
                </div>
              </div>
              <div class="col-md-4">
                <div class="d-flex align-items-center">
                  <i class="bi bi-check-circle me-2 text-success"></i>
                  <span class="text-muted">Готовы к работе:</span>
                  <span class="ms-2 fw-bold">{{ operatorStore.readyExecutorsCount }}</span>
                </div>
              </div>
              <div class="col-md-4">
                <div class="d-flex align-items-center">
                  <i class="bi bi-graph-up me-2 text-info"></i>
                  <span class="text-muted">Всего обслужено:</span>
                  <span class="ms-2 fw-bold">{{ operatorStore.totalServedByExecutors }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Очередь талонов -->
    <div class="row mb-4">
      <div class="col-12">
        <div class="card shadow">
          <div class="card-header d-flex justify-content-between align-items-center">
            <h5 class="card-title mb-0">
              <i class="bi bi-list-ol me-2"></i>Очередь талонов
            </h5>
            <div>
              <button class="btn btn-sm btn-outline-primary" @click="operatorStore.fetchQueue">
                <i class="bi bi-arrow-clockwise"></i>
              </button>
            </div>
          </div>
          <div class="card-body p-0">
            <div v-if="operatorStore.loading" class="text-center p-4">
              <div class="spinner-border text-primary" role="status"></div>
            </div>
            <div v-else>
              <table class="table table-hover mb-0">
                <thead class="table-light">
                  <tr>
                    <th>№</th>
                    <th>Клиент</th>
                    <th>Услуга</th>
                    <th>Приоритет</th>
                    <th>Статус</th>
                    <th>Время ожидания</th>
                    <th>Действия</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="ticket in operatorStore.queue" :key="ticket.id">
                    <td class="fw-bold">{{ ticket.id }}</td>
                    <td>{{ ticket.clientName }} {{ ticket.clientSurname }}</td>
                    <td>{{ ticket.serviceTypeName || '—' }}</td>
                    <td>
                      <span class="badge" :class="priorityClass(ticket.priorityLevel)">
                        {{ ticket.priorityLevel }}
                      </span>
                    </td>
                    <td>
                      <span class="badge" :class="statusClass(ticket.status)">
                        {{ statusLabel(ticket.status) }}
                      </span>
                    </td>
                    <td>{{ formatDuration(ticket.waitingTime) }}</td>
                    <td>
                      <div class="btn-group btn-group-sm">
                        <button
                          v-if="ticket.status === 'WAITING'"
                          class="btn btn-outline-success"
                          @click="handleCallTicket(ticket)"
                          title="Вызвать"
                        >
                          <i class="bi bi-megaphone"></i>
                        </button>
                        <button
                          class="btn btn-outline-danger"
                          @click="handleCancelTicket(ticket.id)"
                          title="Отменить"
                        >
                          <i class="bi bi-x-circle"></i>
                        </button>
                        <button
                          class="btn btn-outline-secondary"
                          @click="showMoveModal(ticket)"
                          title="Переместить"
                        >
                          <i class="bi bi-arrow-down-up"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
              <div v-if="operatorStore.queue.length === 0" class="text-center p-4 text-muted">
                Очередь пуста
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Информация о сессии -->
    <div class="row mb-4">
      <div class="col-12">
        <div class="card shadow">
          <div class="card-header bg-secondary text-white">
            <h5 class="card-title mb-0">
              <i class="bi bi-info-circle me-2"></i>Информация
            </h5>
          </div>
          <div class="card-body">
            <div class="row">
              <div class="col-md-3">
                <div class="d-flex align-items-center">
                  <i class="bi bi-receipt me-2 text-primary"></i>
                  <span class="text-muted">Всего талонов:</span>
                  <span class="ms-2 fw-bold">{{ operatorStore.totalTicketsCount }}</span>
                </div>
              </div>
              <div class="col-md-3">
                <div class="d-flex align-items-center">
                  <i class="bi bi-check-circle me-2 text-success"></i>
                  <span class="text-muted">Обслужено:</span>
                  <span class="ms-2 fw-bold text-success">{{ operatorStore.servedCount }}</span>
                </div>
              </div>
              <div class="col-md-3">
                <div class="d-flex align-items-center">
                  <i class="bi bi-x-circle me-2 text-danger"></i>
                  <span class="text-muted">Отменено:</span>
                  <span class="ms-2 fw-bold text-danger">{{ operatorStore.statistics?.cancelledTickets || 0 }}</span>
                </div>
              </div>
              <div class="col-md-3">
                <div class="d-flex align-items-center">
                  <i class="bi bi-forward me-2 text-dark"></i>
                  <span class="text-muted">Неявок:</span>
                  <span class="ms-2 fw-bold text-dark">{{ operatorStore.statistics?.skippedTickets || 0 }}</span>
                </div>
              </div>
            </div>
            <div v-if="operatorStore.statistics?.avgServiceTimeSec" class="mt-2">
              <div class="d-flex align-items-center">
                <i class="bi bi-clock me-2 text-info"></i>
                <span class="text-muted">Ср. время обслуживания:</span>
                <span class="ms-2 fw-bold">{{ formatDuration(operatorStore.statistics.avgServiceTimeSec * 1000) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Модальное окно перемещения -->
    <div class="modal fade" id="moveTicketModal" tabindex="-1">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Переместить талон</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body">
            <p>Талон №{{ selectedTicket?.id }} - {{ selectedTicket?.clientName }} {{ selectedTicket?.clientSurname }}</p>
            <div class="mb-3">
              <label for="positionInput" class="form-label">Новая позиция (1 - {{ operatorStore.queue.length }})</label>
              <input
                type="number"
                class="form-control"
                id="positionInput"
                v-model="movePosition"
                min="1"
                :max="operatorStore.queue.length"
              />
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
            <button type="button" class="btn btn-primary" @click="confirmMove">Переместить</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Ошибки -->
    <div v-if="operatorStore.error" class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
      {{ operatorStore.error }}
      <button type="button" class="btn-close" @click="operatorStore.error = null"></button>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useOperatorStore } from '@/stores/operator'
import { Modal } from 'bootstrap'

const operatorStore = useOperatorStore()

const selectedTicket = ref(null)
const movePosition = ref(1)

onMounted(() => {
  operatorStore.init()
})

function getInitials(name) {
  if (!name) return '?'
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}

function priorityClass(level) {
  if (level > 5) return 'bg-danger'
  if (level > 3) return 'bg-warning'
  return 'bg-secondary'
}

function statusClass(status) {
  const map = {
    WAITING: 'bg-secondary',
    CALLED: 'bg-warning',
    SERVING: 'bg-info',
    SERVED: 'bg-success',
    CANCELLED: 'bg-danger',
    SKIPPED: 'bg-dark'
  }
  return map[status] || 'bg-light'
}

function statusLabel(status) {
  const map = {
    WAITING: 'Ожидает',
    CALLED: 'Вызван',
    SERVING: 'Обслуживается',
    SERVED: 'Обслужен',
    CANCELLED: 'Отменён',
    SKIPPED: 'Пропущен'
  }
  return map[status] || status
}

function formatDuration(ms) {
  if (!ms) return '0:00'
  const seconds = Math.floor(ms / 1000)
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

function handleCallTicket(ticket) {
  if (confirm(`Вызвать клиента ${ticket.clientName} ${ticket.clientSurname}?`)) {
    const readyExecutor = operatorStore.executorStates.find(e => e.isReady)
    if (readyExecutor) {
      operatorStore.callNextClient(readyExecutor.userId)
    } else {
      alert('Нет готовых исполнителей')
    }
  }
}

function handleCancelTicket(ticketId) {
  if (confirm('Вы уверены, что хотите отменить талон?')) {
    operatorStore.cancelTicketById(ticketId)
  }
}

function showMoveModal(ticket) {
  selectedTicket.value = ticket
  movePosition.value = ticket.sortOrder || 1
  const modal = new Modal(document.getElementById('moveTicketModal'))
  modal.show()
}

function confirmMove() {
  if (!selectedTicket.value) return
  operatorStore.moveTicket(selectedTicket.value.id, movePosition.value)
  const modal = Modal.getInstance(document.getElementById('moveTicketModal'))
  modal.hide()
}

function handleCallNext(executorId) {
  operatorStore.callNextClient(executorId)
}

function handleCallFirstAvailable() {
  const readyExecutor = operatorStore.executorStates.find(e => e.isReady)
  if (readyExecutor) {
    operatorStore.callNextClient(readyExecutor.userId)
  }
}

function refreshAll() {
  operatorStore.fetchQueue()
  operatorStore.fetchExecutorStates()
  operatorStore.fetchActiveStatistics()
}
</script>

<style scoped>
.operator-view {
  min-height: 70vh;
}
.card {
  border: none;
}
.avatar-circle {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: bold;
  font-size: 14px;
}
</style>
