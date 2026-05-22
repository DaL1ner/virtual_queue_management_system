<template>
  <div class="home-view">
    <div class="row justify-content-center">
      <div class="col-md-8 col-lg-6">
        <div class="card shadow">
          <div class="card-header bg-primary text-white">
            <h1 class="h4 mb-0">Запись в очередь</h1>
          </div>
          <div class="card-body">
            <p class="card-text">
              Заполните форму ниже, чтобы встать в очередь. После записи вы получите талон с номером и сможете отслеживать его статус.
            </p>
            <TicketForm />
          </div>
          <div class="card-footer text-muted">
            <small>
              Ваши данные используются только для идентификации в очереди.
              При повторном посещении сайта с этого устройства вы сможете продолжить отслеживание.
            </small>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import TicketForm from '../components/TicketForm.vue';
</script>

<style scoped>
.home-view {
  padding: 2rem 1rem;
}
.card {
  border-radius: 0.5rem;
}
</style>