import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { MyActiveTicketDetailDto } from '../types/api';

export const useTicketStore = defineStore('ticket', () => {
  const activeTicket = ref<MyActiveTicketDetailDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  function setActiveTicket(ticket: MyActiveTicketDetailDto | null) {
    activeTicket.value = ticket;
  }

  function setLoading(isLoading: boolean) {
    loading.value = isLoading;
  }

  function setError(err: string | null) {
    error.value = err;
  }

  function clear() {
    activeTicket.value = null;
    loading.value = false;
    error.value = null;
  }

  return {
    activeTicket,
    loading,
    error,
    setActiveTicket,
    setLoading,
    setError,
    clear,
  };
});