<template>
  <div class="ticket-actions card shadow">
    <div class="card-header bg-light">
      <h3 class="h6 mb-0">Управление талоном</h3>
    </div>
    <div class="card-body">
      <!-- Уведомления -->
      <div v-if="successMessage" class="alert alert-success alert-dismissible fade show" role="alert">
        {{ successMessage }}
        <button type="button" class="btn-close" @click="successMessage = ''"></button>
      </div>
      <div v-if="errorMessage" class="alert alert-danger alert-dismissible fade show" role="alert">
        {{ errorMessage }}
        <button type="button" class="btn-close" @click="errorMessage = ''"></button>
      </div>

      <div class="row g-3">
        <div class="col-md-6">
          <div class="d-grid">
            <button
              class="btn btn-outline-danger btn-lg"
              @click="handleCancel"
              :disabled="loading"
            >
              <span v-if="loading" class="spinner-border spinner-border-sm me-1"></span>
              Выйти из очереди
            </button>
            <small class="text-muted mt-1 d-block">
              Ваш талон будет отменён, и вы покинете очередь.
            </small>
          </div>
        </div>
        <div class="col-md-6">
          <div class="d-grid">
            <button
              class="btn btn-outline-primary btn-lg"
              data-bs-toggle="modal"
              data-bs-target="#moveBackwardModal"
              :disabled="loading || isLastInQueue"
            >
              Переместиться назад
            </button>
            <small class="text-muted mt-1 d-block">
              <span v-if="isLastInQueue">Вы в конце очереди, перемещение невозможно.</span>
              <span v-else>Вы можете переместиться к концу очереди на указанное число шагов.</span>
            </small>
          </div>
        </div>
      </div>

      <div class="mt-4">
        <div class="alert alert-info">
          <h4 class="alert-heading h6">Советы</h4>
          <ul class="mb-0">
            <li>Если вы не готовы подойти, используйте "Переместиться назад".</li>
            <li>Выйти из очереди можно в любой момент до начала обслуживания.</li>
            <li>После выхода из очереди запись аннулируется.</li>
          </ul>
        </div>
      </div>
    </div>
  </div>

  <!-- Модальное окно для перемещения назад -->
  <div class="modal fade" id="moveBackwardModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">Перемещение назад</h5>
          <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        <div class="modal-body">
          <p>Укажите, на сколько шагов вы хотите переместиться к концу очереди.</p>
          <div class="mb-3">
            <label for="stepsInput" class="form-label">Количество шагов (максимум {{ maxSteps }})</label>
            <input
              type="number"
              class="form-control"
              id="stepsInput"
              v-model="steps"
              min="1"
              :max="maxSteps"
              :disabled="loading"
            />
            <div class="form-text">
              Сейчас перед вами {{ ticket.positionInQueue - 1 }} человек.
            </div>
            
            <!-- Быстрые кнопки выбора -->
            <div class="mt-3">
              <p class="small mb-2">Быстрый выбор:</p>
              <div class="btn-group btn-group-sm" role="group">
                <button
                  v-for="quickStep in quickSteps"
                  :key="quickStep"
                  type="button"
                  class="btn btn-outline-secondary"
                  @click="steps = quickStep"
                  :class="{ active: steps === quickStep }"
                >
                  {{ quickStep }}
                </button>
              </div>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
          <button
            type="button"
            class="btn btn-primary"
            @click="handleMoveBackward"
            :disabled="!steps || loading"
          >
            <span v-if="loading" class="spinner-border spinner-border-sm me-1"></span>
            Переместить
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useTicket } from '../composables/useTicket';
import type { MyActiveTicketDetailDto } from '../types/api';
import { Modal } from 'bootstrap';

const props = defineProps<{
  ticket: MyActiveTicketDetailDto;
}>();

const router = useRouter();
const { cancelTicket, moveTicketBackward, loading, error } = useTicket();

const steps = ref(1);
const maxSteps = computed(() => props.ticket.totalWaiting - props.ticket.positionInQueue);
const isLastInQueue = computed(() => maxSteps.value <= 0);
const successMessage = ref('');
const errorMessage = ref('');

const quickSteps = computed(() => {
  const max = maxSteps.value;
  if (max <= 5) {
    return Array.from({ length: max }, (_, i) => i + 1);
  }
  // Возвращаем 1, 3, 5, 10, половину, максимум
  const steps = [1, 3, 5];
  if (max >= 10) steps.push(10);
  const half = Math.floor(max / 2);
  if (half > 10 && !steps.includes(half)) steps.push(half);
  if (!steps.includes(max)) steps.push(max);
  return steps;
});

let moveBackwardModal: InstanceType<typeof Modal> | null = null;

onMounted(() => {
  const modalElement = document.getElementById('moveBackwardModal');
  if (modalElement) {
    moveBackwardModal = new Modal(modalElement);
  }
});

function showSuccess(message: string) {
  successMessage.value = message;
  errorMessage.value = '';
  setTimeout(() => {
    successMessage.value = '';
  }, 5000);
}

function showError(message: string) {
  errorMessage.value = message;
  successMessage.value = '';
  setTimeout(() => {
    errorMessage.value = '';
  }, 5000);
}

async function handleCancel() {
  if (!confirm('Вы уверены, что хотите выйти из очереди? Талон будет отменён.')) {
    return;
  }
  try {
    await cancelTicket();
    showSuccess('Талон успешно отменён. Вы вышли из очереди.');
    setTimeout(() => {
      router.push('/');
    }, 1500);
  } catch (err: any) {
    const msg = err.response?.data?.message || err.message || 'Не удалось отменить талон';
    showError(msg);
    console.error('Cancel failed:', err);
  }
}

async function handleMoveBackward() {
  if (!steps.value || steps.value < 1 || steps.value > maxSteps.value) {
    showError('Укажите корректное количество шагов.');
    return;
  }
  try {
    await moveTicketBackward(steps.value);
    showSuccess(`Талон успешно перемещён назад на ${steps.value} шагов.`);
    // Закрываем модальное окно
    moveBackwardModal?.hide();
    steps.value = 1;
  } catch (err: any) {
    const msg = err.response?.data?.message || err.message || 'Не удалось переместить талон';
    showError(msg);
    console.error('Move backward failed:', err);
  }
}
</script>

<style scoped>
.ticket-actions {
  border-radius: 0.5rem;
}
</style>
