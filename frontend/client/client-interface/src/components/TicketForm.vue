<template>
  <div class="ticket-form">
    <form @submit.prevent="handleSubmit">
      <div class="mb-3">
        <label for="clientName" class="form-label">Имя *</label>
        <input
          type="text"
          class="form-control"
          id="clientName"
          v-model="form.clientName"
          required
          :disabled="loading"
        />
        <div class="invalid-feedback" v-if="errors.clientName">
          {{ errors.clientName }}
        </div>
      </div>

      <div class="mb-3">
        <label for="clientSurname" class="form-label">Фамилия *</label>
        <input
          type="text"
          class="form-control"
          id="clientSurname"
          v-model="form.clientSurname"
          required
          :disabled="loading"
        />
        <div class="invalid-feedback" v-if="errors.clientSurname">
          {{ errors.clientSurname }}
        </div>
      </div>

      <div class="mb-3" v-if="serviceTypes.length > 0">
        <label for="serviceTypeId" class="form-label">Тип обслуживания</label>
        <select
          class="form-select"
          id="serviceTypeId"
          v-model="form.serviceTypeId"
          :disabled="loading"
        >
          <option :value="undefined">Не выбрано</option>
          <option v-for="st in serviceTypes" :key="st.id" :value="st.id">
            {{ st.name }}
          </option>
        </select>
      </div>

      <div class="alert alert-info" v-if="serviceTypes.length === 0">
        В данный момент доступен только базовый тип обслуживания.
      </div>

      <div class="mb-3">
        <button type="submit" class="btn btn-primary" :disabled="loading">
          <span v-if="loading" class="spinner-border spinner-border-sm me-1"></span>
          {{ loading ? 'Отправка...' : 'Встать в очередь' }}
        </button>
      </div>

      <div v-if="error" class="alert alert-danger mt-3">
        {{ error }}
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuth } from '../composables/useAuth';
import { useTicket } from '../composables/useTicket';
import type { ServiceTypeSimpleDto } from '../types/api';

const router = useRouter();
const { loadOrCreateFingerprint } = useAuth();
const { fetchServiceTypes, createTicket, loading, error } = useTicket();

const serviceTypes = ref<ServiceTypeSimpleDto[]>([]);
const serviceTypesLoading = ref(false);

const form = reactive({
  clientName: '',
  clientSurname: '',
  serviceTypeId: undefined as number | undefined,
});

const errors = reactive({
  clientName: '',
  clientSurname: '',
});

async function loadServiceTypes() {
  serviceTypesLoading.value = true;
  try {
    const data = await fetchServiceTypes();
    serviceTypes.value = data;
  } catch (err) {
    console.error('Failed to load service types:', err);
  } finally {
    serviceTypesLoading.value = false;
  }
}

async function handleSubmit() {
  // Валидация
  errors.clientName = form.clientName.trim() ? '' : 'Введите имя';
  errors.clientSurname = form.clientSurname.trim() ? '' : 'Введите фамилию';
  if (errors.clientName || errors.clientSurname) {
    return;
  }

  try {
    // Убедимся, что device fingerprint есть
    await loadOrCreateFingerprint();
    // Создаём талон
    const response = await createTicket({
      clientName: form.clientName.trim(),
      clientSurname: form.clientSurname.trim(),
      serviceTypeId: form.serviceTypeId,
    });
    // Перенаправляем на страницу талона
    router.push('/ticket');
  } catch (err) {
    // Ошибка уже обработана в useTicket
    console.error('Submit error:', err);
  }
}

onMounted(() => {
  loadServiceTypes();
});
</script>

<style scoped>
.ticket-form {
  max-width: 500px;
  margin: 0 auto;
}
.invalid-feedback {
  display: block;
}
</style>