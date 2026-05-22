<template>
  <div class="ticket-status-card card shadow">
    <div class="card-header" :class="headerClass">
      <h2 class="h5 mb-0">Талон #{{ ticket.ticketNumber }}</h2>
    </div>
    <div class="card-body">
      <div class="row">
        <div class="col-md-6">
          <h3 class="h6 text-muted">Клиент</h3>
          <p class="fs-4">{{ ticket.clientName }} {{ ticket.clientSurname }}</p>
        </div>
        <div class="col-md-6">
          <h3 class="h6 text-muted">Статус</h3>
          <p>
            <span class="badge" :class="statusBadgeClass">{{ statusText }}</span>
          </p>
        </div>
      </div>

      <hr />

      <div class="row">
        <div class="col-md-4">
          <div class="text-center p-3 bg-light rounded">
            <h3 class="h6 text-muted">Позиция в очереди</h3>
            <p class="display-6">{{ ticket.positionInQueue }}</p>
            <small class="text-muted">из {{ ticket.totalWaiting }} ожидающих</small>
          </div>
        </div>
        <div class="col-md-4">
          <div class="text-center p-3 bg-light rounded">
            <h3 class="h6 text-muted">Примерное время ожидания</h3>
            <p class="display-6" v-if="ticket.estimatedWaitMinutes !== null && ticket.estimatedWaitMinutes !== undefined">
              {{ ticket.estimatedWaitMinutes }} мин
            </p>
            <p class="display-6 text-muted" v-else>—</p>
            <small class="text-muted">расчётное</small>
          </div>
        </div>
        <div class="col-md-4">
          <div class="text-center p-3 bg-light rounded">
            <h3 class="h6 text-muted">Тип обслуживания</h3>
            <p class="fs-4" v-if="ticket.serviceTypeName">{{ ticket.serviceTypeName }}</p>
            <p class="fs-4 text-muted" v-else>Базовый</p>
            <small class="text-muted" v-if="ticket.serviceLetter">Буква: {{ ticket.serviceLetter }}</small>
          </div>
        </div>
      </div>

      <div class="mt-4">
        <ul class="list-group list-group-flush">
          <li class="list-group-item">
            <span class="text-muted">Дата создания</span>
            <span class="ms-2">{{ formatDate(ticket.createdAt) }}</span>
          </li>
          <li class="list-group-item" v-if="ticket.calledAt">
            <span class="text-muted">Время вызова</span>
            <span class="ms-2">{{ formatDate(ticket.calledAt) }}</span>
          </li>
          <li class="list-group-item" v-if="ticket.servedByUserName">
            <span class="text-muted">Обслуживает</span>
            <span class="ms-2">{{ ticket.servedByUserName }}</span>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { MyActiveTicketDetailDto } from '../types/api';

const props = defineProps<{
  ticket: MyActiveTicketDetailDto;
}>();

const statusText = computed(() => {
  const statusMap: Record<string | number, string> = {
    Waiting: 'Ожидает',
    Called: 'Вызван',
    Serving: 'Обслуживается',
    Served: 'Обслужен',
    Cancelled: 'Отменён',
    Skipped: 'Пропущен',
    0: 'Ожидает',
    1: 'Вызван',
    2: 'Обслуживается',
    3: 'Обслужен',
    4: 'Пропущен',
    5: 'Отменён',
  };
  return statusMap[props.ticket.status] || props.ticket.status;
});

const statusBadgeClass = computed(() => {
  const map: Record<string | number, string> = {
    Waiting: 'bg-warning text-dark',
    Called: 'bg-info text-dark',
    Serving: 'bg-primary text-white',
    Served: 'bg-success text-white',
    Cancelled: 'bg-secondary text-dark',
    Skipped: 'bg-danger text-white',
    0: 'bg-warning text-dark',
    1: 'bg-info text-dark',
    2: 'bg-primary text-white',
    3: 'bg-success text-white',
    4: 'bg-danger text-white',
    5: 'bg-secondary text-dark',
  };
  return `badge ${map[props.ticket.status] || 'bg-secondary text-dark'}`;
});

const headerClass = computed(() => {
  const status = String(props.ticket.status);
  if (status === 'Waiting' || status === '0') return 'bg-warning text-dark';
  if (status === 'Called' || status === '1') return 'bg-info text-dark';
  if (status === 'Serving' || status === '2') return 'bg-primary text-white';
  return 'bg-secondary text-white';
});

function formatDate(dateString: string) {
  const date = new Date(dateString);
  return date.toLocaleString('ru-RU');
}
</script>

<style scoped>
.ticket-status-card {
  border-radius: 0.5rem;
}
.display-6 {
  font-size: 2.5rem;
  font-weight: 300;
}
</style>
