<template>
  <div class="dashboard">
    <!-- Навигационная панель -->
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary shadow">
      <div class="container-fluid">
        <a class="navbar-brand fw-bold" href="#">
          <i class="bi bi-people-fill me-2"></i>
          Система управления очередью
        </a>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" id="navbarNav">
          <div class="d-flex align-items-center ms-auto">
            <span class="nav-link text-light me-3">
              <i class="bi bi-person-circle me-1"></i>
              {{ authStore.user?.username }}
            </span>
            <button class="btn btn-outline-light" @click="handleLogout">
              <i class="bi bi-box-arrow-right me-1"></i> Выйти
            </button>
          </div>
        </div>
      </div>
    </nav>

    <!-- Вкладки ролей -->
    <div class="container-fluid mt-3">
      <ul class="nav nav-tabs" role="tablist">
        <li class="nav-item" v-if="hasExecutorRole">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'executor' }"
            @click="switchTab('executor')"
          >
            <i class="bi bi-person-workspace me-1"></i> Исполнитель
          </button>
        </li>
        <li class="nav-item" v-if="hasOperatorRole">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'operator' }"
            @click="switchTab('operator')"
          >
            <i class="bi bi-megaphone me-1"></i> Оператор
          </button>
        </li>
        <li class="nav-item" v-if="hasAdminRole">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'admin' }"
            @click="switchTab('admin')"
          >
            <i class="bi bi-gear me-1"></i> Администратор
          </button>
        </li>
      </ul>

      <!-- Содержимое вкладок -->
      <div class="tab-content p-3 border border-top-0 rounded-bottom shadow-sm bg-white">
        <div v-if="activeTab === 'executor' && hasExecutorRole">
          <ExecutorView />
        </div>
        <div v-if="activeTab === 'operator' && hasOperatorRole">
          <OperatorView />
        </div>
        <div v-if="activeTab === 'admin' && hasAdminRole">
          <AdminView />
        </div>
      </div>
    </div>

    <!-- Уведомление, если нет доступных ролей -->
    <div v-if="!hasAnyRole" class="container mt-5">
      <div class="alert alert-warning text-center">
        <h4 class="alert-heading">Нет доступных ролей</h4>
        <p>У вашей учётной записи нет прав для доступа к интерфейсам исполнителя, оператора или администратора.</p>
        <p>Обратитесь к администратору системы.</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useExecutorStore } from '@/stores/executor'
import { useOperatorStore } from '@/stores/operator'
import { useAdminStore } from '@/stores/admin'
import ExecutorView from '@/views/ExecutorView.vue'
import OperatorView from '@/views/OperatorView.vue'
import AdminView from '@/views/AdminView.vue'

const router = useRouter()
const authStore = useAuthStore()
const executorStore = useExecutorStore()
const operatorStore = useOperatorStore()
const adminStore = useAdminStore()

const activeTab = ref('executor')
const userHasManuallySwitched = ref(false)

const hasExecutorRole = computed(() => authStore.hasRole('EXECUTOR'))
const hasOperatorRole = computed(() => authStore.hasRole('OPERATOR'))
const hasAdminRole = computed(() => authStore.hasRole('ADMIN'))
const hasAnyRole = computed(() => hasExecutorRole.value || hasOperatorRole.value || hasAdminRole.value)

// Установить активную вкладку по умолчанию в зависимости от ролей
onMounted(() => {
  console.log('[Dashboard] onMounted - authStore.roles:', authStore.roles)
  console.log('[Dashboard] onMounted - hasExecutorRole:', hasExecutorRole.value)
  console.log('[Dashboard] onMounted - hasOperatorRole:', hasOperatorRole.value)
  console.log('[Dashboard] onMounted - hasAdminRole:', hasAdminRole.value)
  console.log('[Dashboard] onMounted - hasAnyRole:', hasAnyRole.value)
  if (!hasAnyRole.value) return
  if (hasExecutorRole.value) {
    activeTab.value = 'executor'
    executorStore.init()
  }
  else if (hasOperatorRole.value) {
    activeTab.value = 'operator'
    operatorStore.init()
  }
  else if (hasAdminRole.value) {
    activeTab.value = 'admin'
    adminStore.init()
  }
})

// Следим за изменением ролей (например, после fetchCurrentUser)
watch(() => authStore.roles, () => {
  console.log('[Dashboard] authStore.roles changed:', authStore.roles)
  if (!hasAnyRole.value) return
  // Если пользователь уже вручную переключил вкладку, не меняем автоматически
  if (userHasManuallySwitched.value) {
    console.log('[Dashboard] User has manually switched tabs, skipping auto-switch')
    return
  }
  if (hasExecutorRole.value && activeTab.value !== 'executor') {
    console.log('[Dashboard] Switching to executor tab')
    activeTab.value = 'executor'
  }
  else if (hasOperatorRole.value && activeTab.value !== 'operator') {
    console.log('[Dashboard] Switching to operator tab')
    activeTab.value = 'operator'
  }
  else if (hasAdminRole.value && activeTab.value !== 'admin') {
    console.log('[Dashboard] Switching to admin tab')
    activeTab.value = 'admin'
  }
}, { deep: true })

// Управление polling при переключении вкладок
watch(activeTab, (newTab, oldTab) => {
  console.log('[Dashboard] activeTab changed:', oldTab, '->', newTab)
  // Если вкладка не изменилась, ничего не делаем (может случиться из-за watch на roles)
  if (newTab === oldTab) {
    console.log('[Dashboard] Tab unchanged, skipping polling update')
    return
  }
  if (oldTab === 'executor') {
    console.log('[Dashboard] Stopping executor polling')
    executorStore.stopPolling?.()
  } else if (oldTab === 'operator') {
    console.log('[Dashboard] Stopping operator polling')
    operatorStore.stopPolling?.()
  } else if (oldTab === 'admin') {
    console.log('[Dashboard] Stopping admin polling')
    adminStore.stopPolling?.()
  }
  
  if (newTab === 'executor') {
    console.log('[Dashboard] Starting executor polling')
    executorStore.startPolling?.()
  } else if (newTab === 'operator') {
    console.log('[Dashboard] Starting operator polling')
    operatorStore.startPolling?.()
  } else if (newTab === 'admin') {
    console.log('[Dashboard] Starting admin polling')
    adminStore.startPolling?.()
  }
})

function switchTab(tab) {
  console.log('[Dashboard] switchTab called with:', tab, 'current activeTab:', activeTab.value)
  userHasManuallySwitched.value = true
  activeTab.value = tab
}

async function handleLogout() {
  await authStore.logoutUser()
  router.push('/login')
}
</script>

<style scoped>
.nav-tabs .nav-link {
  font-weight: 500;
}
.nav-tabs .nav-link.active {
  background-color: #f8f9fa;
  border-bottom-color: #f8f9fa;
}
</style>
