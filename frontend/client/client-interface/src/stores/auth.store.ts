import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('sessionToken'));
  const deviceFingerprint = ref<string | null>(localStorage.getItem('deviceFingerprint'));

  const isAuthenticated = computed(() => !!token.value);

  function setToken(newToken: string) {
    token.value = newToken;
    localStorage.setItem('sessionToken', newToken);
  }

  function clearToken() {
    token.value = null;
    localStorage.removeItem('sessionToken');
  }

  function setDeviceFingerprint(fingerprint: string) {
    deviceFingerprint.value = fingerprint;
    localStorage.setItem('deviceFingerprint', fingerprint);
  }

  function clearDeviceFingerprint() {
    deviceFingerprint.value = null;
    localStorage.removeItem('deviceFingerprint');
  }

  return {
    token,
    deviceFingerprint,
    isAuthenticated,
    setToken,
    clearToken,
    setDeviceFingerprint,
    clearDeviceFingerprint,
  };
});