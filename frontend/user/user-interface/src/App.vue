<template>
  <router-view />
</template>

<script setup>
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

onMounted(() => {
  // При загрузке приложения пытаемся получить текущего пользователя
  if (authStore.isAuthenticated) {
    authStore.fetchCurrentUser()
  }
})
</script>
