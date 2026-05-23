<template>
  <div class="operator-view">
    <div class="row mb-4">
      <!-- Статистика -->
      <div class="col-md-3">
        <div class="card bg-primary text-white shadow">
          <div class="card-body">
            <h5 class="card-title">В очереди</h5>
            <p class="display-4">{{ operatorStore.queueLength }}</p>
          </div>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card bg-info text-white shadow">
          <div class="card-body">
            <h5 class="card-title">Ожидают</h5>
            <p class="display-4">{{ operatorStore.waitingCount }}</p>
          </div>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card bg-warning text-white shadow">
          <div class="card-body">
            <h5 class="card-title">Вызваны</h5>
            <p class="display-4">{{ operatorStore.calledCount }}</p>
          </div>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card bg-success text-white shadow">
          <div class="card-body">
            <h5 class="card-title">Исполнители</h5>
            <p class="display-4">{{ readyExecutorsCount }}/{{ totalExecutorsCount }}</p>
            <small>Готовы</small>
          </div>
        </div>
      </div>
    </div>

    <div class="row">
      <!-- Левая колонка: очередь -->
      <div class="col-lg-8">
        <div class="card shadow">
          <div class="card-header d-flex justify-content-between align-items-center">
            <h5 class="card-title mb-0">
              <i class="bi bi-list-ol me-2"></i>Очередь
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
                    <td>
                      <span class="badge" :class="priorityClass(ticket.priorityLevel)">
                        {{ ticket.priorityLevel }}
                      </span>
                    </td>
                    <td>
                      <span class="badge" :class="statusClass(ticket.status)">
                        {{ ticket.status }}
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

      <!-- Правая колонка: исполнители и вызов -->
      <div class="col-lg-4">
        <div class="card shadow mb-4">
          <div class="card-header bg-secondary text-white">
            <h5 class="card-title mb-0">
              <i class="bi bi-people-fill me-2"></i>Исполнители
            </h5>
          </div>
          <div class="card-body">
            <div v-for="executor in operatorStore.executorStates" :key="executor.userId" class="mb-3">
              <div class="d-flex justify-content-between align-items-center">
                <div>
                  <h6 class="mb-0">{{ executor.userName }}</h6>
                  <small class="text-muted">ID: {{ executor.userId }}</small>
                </div>
                <div>
                  <span class="badge" :class="executor.isReady ? 'bg-success' : 'bg-danger'">
                    {{ executor.isReady ? 'Готов' : 'Не готов' }}
                  </span>
                </div>
              </div>
              <div class="mt-2">
                <button
                  class="btn btn-sm btn-outline-primary w-100"
                  @click="handleCallNext(executor.userId)"
                  :disabled="!executor.isReady || operatorStore.loading"
                >
                  <i class="bi bi-megaphone me-1"></i> Вызвать следующего
                </button>
              </div>
              <div v-if="executor.currentTicket" class="mt-2 small">
                <strong>Текущий клиент:</strong> Талон №{{ executor.currentTicket.id }}
              </div>
            </div>
          </div>
        </div>

        <!-- Быстрый вызов -->
        <div class="card shadow">
          <div class="card-header bg-info text-white">
            <h5 class="card-title mb-0">
              <i class="bi bi-lightning-charge me-2"></i>Быстрый вызов
            </h5>
          </div>
          <div class="card-body">
            <p class="card-text">Вызвать первого в очереди клиента любому готовому исполнителю.</p>
            <button
              class="btn btn-info w-100"
              @click="handleCallFirstAvailable"
              :disabled="!hasReadyExecutor || operatorStore.loading"
            >
              <i class="bi bi-play-fill me-1"></i> Вызвать первого доступного
            </button>
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
              <label for="positionInput" class="form-label">Новая позиция (1 - {{ operatorStore.queueLength }})</label>
              <input
                type="number"
                class="form-control"
                id="positionInput"
                v-model="movePosition"
                min="1"
                :max="operatorStore.queueLength"
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

const readyExecutorsCount = computed(() => {
  return operatorStore.executorStates.filter(e => e.isReady).length
})
const totalExecutorsCount = computed(() => operatorStore.executorStates.length)
const hasReadyExecutor = computed(() => readyExecutorsCount.value > 0)

onMounted(() => {
  // Обновление при монтировании
  operatorStore.fetchQueue()
  operatorStore.fetchExecutorStates()
})

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
    NO_SHOW: 'bg-dark'
  }
  return map[status] || 'bg-light'
}

function formatDuration(seconds) {
  if (!seconds) return '0:00'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

function handleCallTicket(ticket) {
  if (confirm(`Вызвать клиента ${ticket.clientName} ${ticket.clientSurname}?`)) {
    // Здесь нужно выбрать исполнителя, упрощённо вызываем первого готового
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
  movePosition.value = ticket.position || 1
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
</script>

<style scoped>
.operator-view {
  min-height: 70vh;
}
.card {
  border: none;
}
</style>