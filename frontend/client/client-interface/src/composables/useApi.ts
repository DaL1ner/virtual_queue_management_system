import { ref } from 'vue';
import axios, { type AxiosInstance, type AxiosRequestConfig, type AxiosResponse } from 'axios';
import { useAuthStore } from '../stores/auth.store';

// API доступен по относительному пути /api — в dev режиме Vite проксирует запросы на бэкенд
const API_BASE_URL = '/api';

export function useApi() {
  const authStore = useAuthStore();
  const loading = ref(false);
  const error = ref<string | null>(null);

  const apiClient: AxiosInstance = axios.create({
    baseURL: API_BASE_URL,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Интерцептор для добавления токена
  apiClient.interceptors.request.use((config) => {
    if (authStore.token) {
      config.headers.Authorization = `Bearer ${authStore.token}`;
    }
    return config;
  });

  // Интерцептор для обработки ошибок
  apiClient.interceptors.response.use(
    (response) => response,
    (err) => {
      if (err.response?.status === 401) {
        authStore.clearToken();
        // Можно перенаправить на страницу входа
        window.location.href = '/';
      }
      return Promise.reject(err);
    }
  );

  async function request<T = any>(config: AxiosRequestConfig): Promise<T> {
    loading.value = true;
    error.value = null;
    try {
      const response: AxiosResponse<T> = await apiClient.request(config);
      return response.data;
    } catch (err: any) {
      error.value = err.response?.data?.message || err.message || 'Ошибка сети';
      throw err;
    } finally {
      loading.value = false;
    }
  }

  const get = <T = any>(url: string, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'GET', url });

  const post = <T = any>(url: string, data?: any, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'POST', url, data });

  const put = <T = any>(url: string, data?: any, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'PUT', url, data });

  const del = <T = any>(url: string, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'DELETE', url });

  return {
    loading,
    error,
    get,
    post,
    put,
    del,
    request,
  };
}
