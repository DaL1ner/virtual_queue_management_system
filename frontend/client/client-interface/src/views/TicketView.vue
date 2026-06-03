<template>
  <div class="ticket-view">
    <div v-if="loading && !ticket" class="text-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Загрузка...</span>
      </div>
      <p class="mt-3">Загрузка данных талона...</p>
    </div>
    
    <div v-else-if="error" class="alert alert-danger">
      <h5 class="alert-heading">Ошибка загрузки</h5>
      <p>{{ error }}</p>
      <button @click="loadTicket" class="btn btn-outline-danger btn-sm">
        Повторить попытку
      </button>
    </div>
    
    <div v-else-if="ticket">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h1 class="h3 mb-0">Статус вашего талона</h1>
        <div class="text-muted small">
          <span v-if="pollingActive" class="text-success">
            <span class="spinner-border spinner-border-sm" role="status"></span>
            Автообновление включено
          </span>
          <span v-else class="text-warning">
            <i class="bi bi-pause-circle"></i> Автообновление приостановлено
          </span>
        </div>
      </div>
      
      <TicketStatusCard :ticket="ticket" />
      <TicketActions :ticket="ticket" class="mt-4" />
      
      
      <div class="mt-4 text-center">
        <button @click="togglePolling" class="btn btn-outline-secondary btn-sm me-2">
          {{ pollingActive ? 'Приостановить автообновление' : 'Возобновить автообновление' }}
        </button>
        <button @click="loadTicket" class="btn btn-outline-primary btn-sm">
          <i class="bi bi-arrow-clockwise"></i> Обновить сейчас
        </button>
      </div>
    </div>
    
    <div v-else class="alert alert-warning">
      <h5 class="alert-heading">Нет активного талона</h5>
      <p>У вас нет активного талона в очереди. Вы можете создать новый талон.</p>
      <div class="mt-3">
        <router-link to="/" class="btn btn-primary">Встать в очередь</router-link>
        <button @click="loadTicket" class="btn btn-outline-secondary ms-2">Проверить снова</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import TicketStatusCard from '../components/TicketStatusCard.vue';
import TicketActions from '../components/TicketActions.vue';
import { useTicket } from '../composables/useTicket';
import { useTicketStore } from '../stores/ticket.store';
import type { MyActiveTicketDetailDto } from '../types/api';

const router = useRouter();
const { fetchActiveTicket, loading, error } = useTicket();
const ticketStore = useTicketStore();

const pollingActive = ref(true);
const pollingInterval = ref<ReturnType<typeof setInterval> | null>(null);
const localError = ref<string | null>(null);

const ticket = computed(() => ticketStore.activeTicket);

const statusText = computed(() => {
  if (!ticket.value) return '';
  const statusMap: Record<number, string> = {
    0: 'Ожидает',
    1: 'Вызван',
    2: 'Обслуживается',
    3: 'Обслужен',
    4: 'Пропущен',
    5: 'Отменён',
  };
  return statusMap[ticket.value.status] || ticket.value.status;
});

const statusBadgeClass = computed(() => {
  if (!ticket.value) return '';
  switch (ticket.value.status) {
    case 0: // Waiting
      return 'badge bg-warning text-dark';
    case 1: // Called
      return 'badge bg-info';
    case 2: // Serving
      return 'badge bg-primary';
    case 3: // Served
      return 'badge bg-success';
    case 5: // Cancelled
      return 'badge bg-secondary';
    case 4: // Skipped
      return 'badge bg-danger';
    default:
      return 'badge bg-light text-dark';
  }
});

async function loadTicket() {
  localError.value = null;
  try {
    await fetchActiveTicket();
  } catch (err: any) {
    localError.value = err.message || 'Не удалось загрузить данные талона';
  }
}

function startPolling() {
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value);
  }
  pollingInterval.value = setInterval(() => {
    if (pollingActive.value && ticket.value) {
      loadTicket();
    }
  }, 30000); // 30 секунд
}

function stopPolling() {
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value);
    pollingInterval.value = null;
  }
}

function togglePolling() {
  pollingActive.value = !pollingActive.value;
  if (pollingActive.value) {
    startPolling();
  } else {
    stopPolling();
  }
}

onMounted(() => {
  loadTicket();
  startPolling();
});

onUnmounted(() => {
  stopPolling();
});
</script>

<style scoped>
.ticket-view {
  padding: 2rem;
  max-width: 1200px;
  margin: 0 auto;
}

.badge {
  font-size: 0.85em;
  padding: 0.35em 0.65em;
}

.card {
  border: 1px solid #dee2e6;
  border-radius: 0.5rem;
}

.card-header {
  background-color: #f8f9fa;
  border-bottom: 1px solid #dee2e6;
  padding: 1rem 1.25rem;
}

.card-body {
  padding: 1.25rem;
}

@media (max-width: 768px) {
  .ticket-view {
    padding: 1rem;
  }
}
</style>
