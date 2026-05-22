<template>
  <div class="not-found">
    <h1>404 - Страница не найдена</h1>
    <p>Извините, запрашиваемая страница не существует.</p>
    <router-link to="/" class="btn btn-primary">Вернуться на главную</router-link>
  </div>
</template>

<script setup lang="ts">
import { RouterLink } from 'vue-router';
</script>

<style scoped>
.not-found {
  padding: 2rem;
  text-align: center;
}
</style>