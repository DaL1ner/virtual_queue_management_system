import { ref, onMounted } from 'vue';
import FingerprintJS from '@fingerprintjs/fingerprintjs';
import { useAuthStore } from '../stores/auth.store';

export function useAuth() {
  const authStore = useAuthStore();
  const fingerprintLoading = ref(false);
  const fingerprintError = ref<string | null>(null);

  async function loadOrCreateFingerprint() {
    if (authStore.deviceFingerprint) {
      return authStore.deviceFingerprint;
    }

    fingerprintLoading.value = true;
    fingerprintError.value = null;

    try {
      const fp = await FingerprintJS.load();
      const result = await fp.get();
      const visitorId = result.visitorId;
      authStore.setDeviceFingerprint(visitorId);
      return visitorId;
    } catch (err) {
      fingerprintError.value = 'Не удалось получить идентификатор устройства';
      console.error('Fingerprint error:', err);
      // Fallback: генерируем случайный UUID
      const fallbackId = 'fallback-' + Math.random().toString(36).substring(2);
      authStore.setDeviceFingerprint(fallbackId);
      return fallbackId;
    } finally {
      fingerprintLoading.value = false;
    }
  }

  onMounted(() => {
    // При монтировании можно предзагрузить fingerprint, если нужно
    // loadOrCreateFingerprint();
  });

  return {
    fingerprintLoading,
    fingerprintError,
    loadOrCreateFingerprint,
  };
}