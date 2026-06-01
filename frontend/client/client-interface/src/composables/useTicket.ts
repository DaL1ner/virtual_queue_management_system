import { useApi } from './useApi';
import { useAuthStore } from '../stores/auth.store';
import { useTicketStore } from '../stores/ticket.store';
import type {
  CreateTicketWithDeviceDto,
  MyActiveTicketDetailDto,
  ServiceTypeSimpleDto,
  MoveTicketBackwardDto,
} from '../types/api';

export function useTicket() {
  const api = useApi();
  const authStore = useAuthStore();
  const ticketStore = useTicketStore();

  async function fetchServiceTypes() {
    try {
      const data = await api.get<ServiceTypeSimpleDto[]>('/queue-sessions/active/service-types');
      return data;
    } catch (err) {
      console.error('Failed to fetch service types:', err);
      throw err;
    }
  }

  async function createTicket(dto: Omit<CreateTicketWithDeviceDto, 'deviceFingerprint'>) {
    const deviceFingerprint = authStore.deviceFingerprint;
    if (!deviceFingerprint) {
      throw new Error('Device fingerprint not available');
    }
    const fullDto: CreateTicketWithDeviceDto = {
      ...dto,
      deviceFingerprint,
    };
    try {
      const response = await api.post<{ ticket: any; sessionToken: string }>('/tickets', fullDto);
      authStore.setToken(response.sessionToken);
      return response;
    } catch (err) {
      console.error('Failed to create ticket:', err);
      throw err;
    }
  }

  async function fetchActiveTicket() {
    try {
      const data = await api.get<MyActiveTicketDetailDto>('/tickets/me');
      ticketStore.setActiveTicket(data);
      return data;
    } catch (err: any) {
      if (err.response?.status === 404) {
        ticketStore.setActiveTicket(null);
        return null;
      }
      console.error('Failed to fetch active ticket:', err);
      throw err;
    }
  }

  async function cancelTicket() {
    try {
      const data = await api.post('/tickets/me/cancel');
      ticketStore.clear();
      authStore.clearToken();
      return data;
    } catch (err) {
      console.error('Failed to cancel ticket:', err);
      throw err;
    }
  }

  async function moveTicketBackward(steps: number) {
    try {
      const dto: MoveTicketBackwardDto = { steps };
      const data = await api.post<MyActiveTicketDetailDto>('/tickets/me/move-backward', dto);
      ticketStore.setActiveTicket(data);
      return data;
    } catch (err) {
      console.error('Failed to move ticket backward:', err);
      throw err;
    }
  }

  return {
    loading: api.loading,
    error: api.error,
    fetchServiceTypes,
    createTicket,
    fetchActiveTicket,
    cancelTicket,
    moveTicketBackward,
  };
}