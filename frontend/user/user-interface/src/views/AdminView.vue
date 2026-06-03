<template>
  <div class="admin-view">
    <h3 class="mb-4">
      <i class="bi bi-gear me-2"></i>Панель администратора
    </h3>

    <!-- Вкладки -->
    <ul class="nav nav-tabs mb-4">
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'sessions' }"
          @click="activeTab = 'sessions'"
        >
          Сессии очередей
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'configs' }"
          @click="activeTab = 'configs'"
        >
          Конфигурации
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'users' }"
          @click="activeTab = 'users'"
        >
          Пользователи
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'serviceTypes' }"
          @click="activeTab = 'serviceTypes'"
        >
          Типы услуг
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'statistics' }"
          @click="activeTab = 'statistics'"
        >
          Статистика
        </button>
      </li>
    </ul>

    <!-- Содержимое вкладок -->
    <div v-if="activeTab === 'sessions'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Сессии очередей</h5>
          <button class="btn btn-sm btn-primary" @click="showCreateSessionModal = true">
            <i class="bi bi-plus-circle me-1"></i> Создать сессию
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Конфигурация</th>
                <th>Статус</th>
                <th>Начало</th>
                <th>Конец</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="session in adminStore.queueSessions" :key="session.id">
                <td>{{ session.id }}</td>
                <td>{{ session.queueConfigName }}</td>
                <td>
                  <span class="badge" :class="sessionStatusClass(session.status)">
                    {{ formatStatus(session.status) }}
                  </span>
                </td>
                <td>{{ formatDate(session.startedAt) }}</td>
                <td>{{ formatDate(session.endedAt) }}</td>
                <td>
                  <div class="dropdown" @click.stop>
                    <button class="btn btn-sm btn-outline-secondary dropdown-toggle"
                            type="button"
                            @click="openDropdownSessionId = openDropdownSessionId === session.id ? null : session.id">
                      Статус
                    </button>
                    <ul class="dropdown-menu" :style="{ display: openDropdownSessionId === session.id ? 'block' : 'none' }">
                      <li v-for="transition in getAvailableTransitions(session.status)"
                          :key="transition.value">
                        <a class="dropdown-item"
                           href="javascript:void(0)"
                           @click.prevent="changeSessionStatus(session.id, transition.value)">
                          <i :class="`bi ${transition.icon} me-1`"></i>
                          {{ transition.label }}
                        </a>
                      </li>
                      <li v-if="getAvailableTransitions(session.status).length === 0">
                        <span class="dropdown-item text-muted">
                          Нет доступных действий
                        </span>
                      </li>
                    </ul>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'configs'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Конфигурации очередей</h5>
          <button class="btn btn-sm btn-primary" @click="openCreateConfigModal">
            <i class="bi bi-plus-circle me-1"></i> Создать конфигурацию
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Название</th>
                <th>Описание</th>
                <th>Макс. очередь</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="config in adminStore.queueConfigs" :key="config.id">
                <td>{{ config.id }}</td>
                <td>{{ config.name }}</td>
                <td>{{ config.description }}</td>
                <td>{{ config.maxQueueSize }}</td>
                <td>
                  <button class="btn btn-sm btn-outline-primary me-1" @click="openEditConfigModal(config)">Редактировать</button>
                  <button class="btn btn-sm btn-outline-warning" @click="deactivateConfig(config.id)">Деактивировать</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'users'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Пользователи</h5>
          <button class="btn btn-sm btn-primary" @click="openCreateUserModal">
            <i class="bi bi-plus-circle me-1"></i> Создать пользователя
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Логин</th>
                <th>Email</th>
                <th>Роли</th>
                <th>Статус</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="user in adminStore.users" :key="user.id">
                <td>{{ user.id }}</td>
                <td>{{ user.login }}</td>
                <td>{{ user.email }}</td>
                <td>
                  <span v-for="role in user.roles" :key="role.id" class="badge bg-secondary me-1">
                    {{ role.code }}
                  </span>
                </td>
                <td>
                  <span class="badge" :class="user.isActive ? 'bg-success' : 'bg-danger'">
                    {{ user.isActive ? 'Активен' : 'Неактивен' }}
                  </span>
                </td>
                <td>
                  <button class="btn btn-sm btn-outline-primary me-1" @click="openEditUserModal(user)">Редактировать</button>
                  <button v-if="user.isActive" class="btn btn-sm btn-outline-warning" @click="deactivateUser(user.id)">Деактивировать</button>
                  <button v-else class="btn btn-sm btn-outline-success" @click="activateUser(user.id)">Активировать</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'serviceTypes'">
      <div class="card shadow">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="card-title mb-0">Типы услуг</h5>
          <button class="btn btn-sm btn-primary" @click="openCreateServiceTypeModal">
            <i class="bi bi-plus-circle me-1"></i> Создать тип
          </button>
        </div>
        <div class="card-body">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>ID</th>
                <th>Конфигурация</th>
                <th>Название</th>
                <th>Код</th>
                <th>Буква</th>
                <th>Приоритет</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="type in adminStore.serviceTypes" :key="type.id">
                <td>{{ type.id }}</td>
                <td>{{ type.queueConfigName }}</td>
                <td>{{ type.name }}</td>
                <td>{{ type.code }}</td>
                <td>{{ type.letter }}</td>
                <td>{{ type.basePriorityLevel }}</td>
                <td>
                  <button class="btn btn-sm btn-outline-primary me-1" @click="openEditServiceTypeModal(type)">Редактировать</button>
                  <button class="btn btn-sm btn-outline-warning" @click="deactivateServiceType(type.id)">Деактивировать</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="activeTab === 'statistics'">
      <div class="card shadow">
        <div class="card-header">
          <h5 class="card-title mb-0">Статистика активной сессии</h5>
        </div>
        <div class="card-body">
          <div v-if="adminStore.statistics">
            <div class="row">
              <div class="col-md-3">
                <div class="card bg-primary text-white">
                  <div class="card-body">
                    <h6>Всего талонов</h6>
                    <p class="display-6">{{ adminStore.statistics.totalTickets }}</p>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card bg-success text-white">
                  <div class="card-body">
                    <h6>Обслужено</h6>
                    <p class="display-6">{{ adminStore.statistics.servedTickets }}</p>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card bg-warning text-white">
                  <div class="card-body">
                    <h6>В ожидании</h6>
                    <p class="display-6">{{ adminStore.statistics.waitingTickets }}</p>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card bg-info text-white">
                  <div class="card-body">
                    <h6>Среднее время</h6>
                    <p class="display-6">{{ formatDuration(adminStore.statistics.avgServingTime) }}</p>
                  </div>
                </div>
              </div>
            </div>
            <div class="mt-4">
              <h6>Детали</h6>
              <pre class="bg-light p-3 rounded">{{ JSON.stringify(adminStore.statistics, null, 2) }}</pre>
            </div>
          </div>
          <div v-else class="text-center p-4">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="mt-2">Загрузка статистики...</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Модальные окна -->
    <!-- Создание конфигурации -->
    <div class="modal fade" :class="{ show: showCreateConfigModal }" :style="{ display: showCreateConfigModal ? 'block' : 'none' }" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Создать конфигурацию очереди</h5>
            <button type="button" class="btn-close" @click="showCreateConfigModal = false"></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="submitCreateConfig">
              <div class="mb-3">
                <label class="form-label">Название *</label>
                <input type="text" class="form-control" v-model="configForm.name" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Описание</label>
                <textarea class="form-control" v-model="configForm.description" rows="2"></textarea>
              </div>
              <div class="mb-3">
                <label class="form-label">Режим распределения</label>
                <select class="form-select" v-model="configForm.distributionMode">
                  <option value="Manual">Ручной</option>
                  <option value="Auto">Автоматический</option>
                </select>
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="configForm.isServiceTypeEnabled">
                <label class="form-check-label">Включить типы услуг</label>
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="configForm.isPriorityEnabled">
                <label class="form-check-label">Включить приоритеты</label>
              </div>
              <div class="mb-3">
                <label class="form-label">Время эскалации приоритета (мин)</label>
                <input type="number" class="form-control" v-model="configForm.priorityEscalationWaitMin">
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showCreateConfigModal = false">Отмена</button>
            <button type="button" class="btn btn-primary" @click="submitCreateConfig">Создать</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Редактирование конфигурации -->
    <div class="modal fade" :class="{ show: showEditConfigModal }" :style="{ display: showEditConfigModal ? 'block' : 'none' }" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Редактировать конфигурацию</h5>
            <button type="button" class="btn-close" @click="showEditConfigModal = false"></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="submitEditConfig">
              <div class="mb-3">
                <label class="form-label">Название *</label>
                <input type="text" class="form-control" v-model="configForm.name" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Описание</label>
                <textarea class="form-control" v-model="configForm.description" rows="2"></textarea>
              </div>
              <div class="mb-3">
                <label class="form-label">Режим распределения</label>
                <select class="form-select" v-model="configForm.distributionMode">
                  <option value="Manual">Ручной</option>
                  <option value="Auto">Автоматический</option>
                </select>
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="configForm.isServiceTypeEnabled">
                <label class="form-check-label">Включить типы услуг</label>
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="configForm.isPriorityEnabled">
                <label class="form-check-label">Включить приоритеты</label>
              </div>
              <div class="mb-3">
                <label class="form-label">Время эскалации приоритета (мин)</label>
                <input type="number" class="form-control" v-model="configForm.priorityEscalationWaitMin">
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showEditConfigModal = false">Отмена</button>
            <button type="button" class="btn btn-primary" @click="submitEditConfig">Сохранить</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Создание пользователя -->
    <div class="modal fade" :class="{ show: showCreateUserModal }" :style="{ display: showCreateUserModal ? 'block' : 'none' }" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Создать пользователя</h5>
            <button type="button" class="btn-close" @click="showCreateUserModal = false"></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="submitCreateUser">
              <div class="mb-3">
                <label class="form-label">Логин *</label>
                <input type="text" class="form-control" v-model="userForm.login" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Пароль *</label>
                <input type="password" class="form-control" v-model="userForm.password" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Имя *</label>
                <input type="text" class="form-control" v-model="userForm.fullName" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Фамилия *</label>
                <input type="text" class="form-control" v-model="userForm.lastName" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Email</label>
                <input type="email" class="form-control" v-model="userForm.email">
              </div>
              <div class="mb-3">
                <label class="form-label">Роли (опционально)</label>
                <div v-if="adminStore.roles.length === 0" class="form-text">Загрузка ролей...</div>
                <div v-else class="role-checkboxes">
                  <div v-for="role in adminStore.roles" :key="role.id" class="form-check form-check-inline">
                    <input
                      type="checkbox"
                      class="form-check-input"
                      :id="'create-role-' + role.id"
                      :value="role.id"
                      v-model="userForm.roleIds"
                    >
                    <label class="form-check-label" :for="'create-role-' + role.id">
                      {{ role.name }} ({{ role.code }})
                    </label>
                  </div>
                </div>
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showCreateUserModal = false">Отмена</button>
            <button type="button" class="btn btn-primary" @click="submitCreateUser">Создать</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Редактирование пользователя -->
    <div class="modal fade" :class="{ show: showEditUserModal }" :style="{ display: showEditUserModal ? 'block' : 'none' }" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Редактировать пользователя</h5>
            <button type="button" class="btn-close" @click="showEditUserModal = false"></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="submitEditUser">
              <div class="mb-3">
                <label class="form-label">Логин</label>
                <input type="text" class="form-control" v-model="userForm.login" disabled>
              </div>
              <div class="mb-3">
                <label class="form-label">Новый пароль (оставьте пустым, чтобы не менять)</label>
                <input type="password" class="form-control" v-model="userForm.password">
              </div>
              <div class="mb-3">
                <label class="form-label">Имя *</label>
                <input type="text" class="form-control" v-model="userForm.fullName" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Фамилия *</label>
                <input type="text" class="form-control" v-model="userForm.lastName" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Email</label>
                <input type="email" class="form-control" v-model="userForm.email">
              </div>
              <div class="mb-3">
                <label class="form-label">Роли (опционально)</label>
                <div v-if="adminStore.roles.length === 0" class="form-text">Загрузка ролей...</div>
                <div v-else class="role-checkboxes">
                  <div v-for="role in adminStore.roles" :key="role.id" class="form-check form-check-inline">
                    <input
                      type="checkbox"
                      class="form-check-input"
                      :id="'edit-role-' + role.id"
                      :value="role.id"
                      v-model="userForm.roleIds"
                    >
                    <label class="form-check-label" :for="'edit-role-' + role.id">
                      {{ role.name }} ({{ role.code }})
                    </label>
                  </div>
                </div>
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showEditUserModal = false">Отмена</button>
            <button type="button" class="btn btn-primary" @click="submitEditUser">Сохранить</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Создание типа услуги -->
    <div class="modal fade" :class="{ show: showCreateServiceTypeModal }" :style="{ display: showCreateServiceTypeModal ? 'block' : 'none' }" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Создать тип услуги</h5>
            <button type="button" class="btn-close" @click="showCreateServiceTypeModal = false"></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="submitCreateServiceType">
              <div class="mb-3">
                <label class="form-label">Конфигурация очереди *</label>
                <select class="form-select" v-model="serviceTypeForm.queueConfigId" required>
                  <option v-for="config in adminStore.queueConfigs" :key="config.id" :value="config.id">{{ config.name }}</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label">Название *</label>
                <input type="text" class="form-control" v-model="serviceTypeForm.name" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Код *</label>
                <input type="text" class="form-control" v-model="serviceTypeForm.code" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Буква *</label>
                <input type="text" class="form-control" v-model="serviceTypeForm.letter" maxlength="1" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Базовый приоритет</label>
                <input type="number" class="form-control" v-model="serviceTypeForm.basePriorityLevel">
              </div>
              <div class="mb-3">
                <label class="form-label">Плановое время обслуживания (сек)</label>
                <input type="number" class="form-control" v-model="serviceTypeForm.planAvgServiceTimeSec">
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="serviceTypeForm.isActive">
                <label class="form-check-label">Активен</label>
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="serviceTypeForm.isHighlighting">
                <label class="form-check-label">Выделение</label>
              </div>
              <div class="mb-3">
                <label class="form-label">Порядок сортировки</label>
                <input type="number" class="form-control" v-model="serviceTypeForm.sortOrder">
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showCreateServiceTypeModal = false">Отмена</button>
            <button type="button" class="btn btn-primary" @click="submitCreateServiceType">Создать</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Редактирование типа услуги -->
    <div class="modal fade" :class="{ show: showEditServiceTypeModal }" :style="{ display: showEditServiceTypeModal ? 'block' : 'none' }" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Редактировать тип услуги</h5>
            <button type="button" class="btn-close" @click="showEditServiceTypeModal = false"></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="submitEditServiceType">
              <div class="mb-3">
                <label class="form-label">Конфигурация очереди *</label>
                <select class="form-select" v-model="serviceTypeForm.queueConfigId" required>
                  <option v-for="config in adminStore.queueConfigs" :key="config.id" :value="config.id">{{ config.name }}</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label">Название *</label>
                <input type="text" class="form-control" v-model="serviceTypeForm.name" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Код *</label>
                <input type="text" class="form-control" v-model="serviceTypeForm.code" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Буква *</label>
                <input type="text" class="form-control" v-model="serviceTypeForm.letter" maxlength="1" required>
              </div>
              <div class="mb-3">
                <label class="form-label">Базовый приоритет</label>
                <input type="number" class="form-control" v-model="serviceTypeForm.basePriorityLevel">
              </div>
              <div class="mb-3">
                <label class="form-label">Плановое время обслуживания (сек)</label>
                <input type="number" class="form-control" v-model="serviceTypeForm.planAvgServiceTimeSec">
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="serviceTypeForm.isActive">
                <label class="form-check-label">Активен</label>
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" v-model="serviceTypeForm.isHighlighting">
                <label class="form-check-label">Выделение</label>
              </div>
              <div class="mb-3">
                <label class="form-label">Порядок сортировки</label>
                <input type="number" class="form-control" v-model="serviceTypeForm.sortOrder">
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showEditServiceTypeModal = false">Отмена</button>
            <button type="button" class="btn btn-primary" @click="submitEditServiceType">Сохранить</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Создание сессии -->
    <div class="modal fade" :class="{ show: showCreateSessionModal }" :style="{ display: showCreateSessionModal ? 'block' : 'none' }" tabindex="-1" role="dialog">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Создать сессию очереди</h5>
            <button type="button" class="btn-close" @click="showCreateSessionModal = false"></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="submitCreateSession">
              <div class="mb-3">
                <label class="form-label">Конфигурация очереди *</label>
                <select class="form-select" v-model="sessionForm.queueConfigId" required>
                  <option value="" disabled>Выберите конфигурацию</option>
                  <option v-for="config in adminStore.queueConfigs" :key="config.id" :value="config.id">
                    {{ config.name }} (ID: {{ config.id }})
                  </option>
                </select>
                <div class="form-text">Выберите конфигурацию очереди, на основе которой будет создана сессия.</div>
              </div>
              <div class="mb-3">
                <label class="form-label">Описание (опционально)</label>
                <textarea class="form-control" v-model="sessionForm.description" rows="2" placeholder="Дополнительное описание сессии..."></textarea>
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showCreateSessionModal = false">Отмена</button>
            <button type="button" class="btn btn-primary" @click="submitCreateSession">Создать</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Ошибки -->
    <div v-if="adminStore.error" class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
      {{ adminStore.error }}
      <button type="button" class="btn-close" @click="adminStore.error = null"></button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useAdminStore } from '@/stores/admin'
import { Modal } from 'bootstrap'

const adminStore = useAdminStore()
const activeTab = ref('sessions')
const showCreateSessionModal = ref(false)

// Track which dropdown is open (by session ID)
const openDropdownSessionId = ref(null)

// Modal state variables
const showCreateConfigModal = ref(false)
const showEditConfigModal = ref(false)
const showCreateUserModal = ref(false)
const showEditUserModal = ref(false)
const showCreateServiceTypeModal = ref(false)
const showEditServiceTypeModal = ref(false)

// Form data
const configForm = ref({
  name: '',
  description: '',
  distributionMode: 'Manual',
  isServiceTypeEnabled: false,
  isPriorityEnabled: true,
  priorityEscalationWaitMin: null
})
const userForm = ref({
  login: '',
  password: '',
  fullName: '',
  lastName: '',
  email: '',
  roleIds: []
})
const serviceTypeForm = ref({
  queueConfigId: null,
  name: '',
  code: '',
  letter: '',
  basePriorityLevel: 0,
  planAvgServiceTimeSec: null,
  isActive: true,
  isHighlighting: false,
  sortOrder: 0
})

const sessionForm = ref({
  queueConfigId: null,
  description: ''
})

// Selected items for editing
const selectedConfig = ref(null)
const selectedUser = ref(null)
const selectedServiceType = ref(null)

onMounted(() => {
  adminStore.init()
  // Add global click listener to close dropdowns
  document.addEventListener('click', handleDocumentClick)
})

onUnmounted(() => {
  document.removeEventListener('click', handleDocumentClick)
})

function handleDocumentClick(event) {
  // Close dropdown if clicking outside of any dropdown
  if (openDropdownSessionId.value && !event.target.closest('.dropdown')) {
    openDropdownSessionId.value = null
  }
}

// Преобразование числового статуса в строковый (для совместимости с C# enum)
function parseStatus(status) {
  const statusMap = {
    0: 'Draft',
    1: 'Open',
    2: 'Paused',
    3: 'Closed',
    'Draft': 'Draft',
    'Open': 'Open',
    'Paused': 'Paused',
    'Closed': 'Closed'
  }
  const parsed = statusMap[status]
  if (parsed === undefined) {
    console.warn('Unknown status:', status, '(type:', typeof status, ')')
    return status // Возвращаем как есть, если не знаю преобразование
  }
  return parsed
}

function sessionStatusClass(status) {
  const normalizedStatus = parseStatus(status)
  const map = {
    Draft: 'bg-secondary',
    Open: 'bg-success',
    Paused: 'bg-warning',
    Closed: 'bg-danger'
  }
  return map[normalizedStatus] || 'bg-light'
}

function formatStatus(status) {
  const normalizedStatus = parseStatus(status)
  const map = {
    Draft: 'Черновик',
    Open: 'Активна',
    Paused: 'Приостановлена',
    Closed: 'Завершена'
  }
  return map[normalizedStatus] || status
}

function formatDate(dateString) {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleString('ru-RU')
}

function formatDuration(seconds) {
  if (!seconds) return '0:00'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

function getAvailableTransitions(currentStatus) {
  const normalizedStatus = parseStatus(currentStatus)
  console.log('getAvailableTransitions called with status:', currentStatus, 'normalized:', normalizedStatus);
  const transitions = {
    'Draft': [
      { value: 'Open', label: 'Начать сессию', icon: 'bi-play-circle', variant: 'outline-success' },
      { value: 'Closed', label: 'Завершить (отмена)', icon: 'bi-x-circle', variant: 'outline-danger' }
    ],
    'Open': [
      { value: 'Paused', label: 'Приостановить', icon: 'bi-pause-circle', variant: 'outline-warning' },
      { value: 'Closed', label: 'Завершить', icon: 'bi-stop-circle', variant: 'outline-danger' }
    ],
    'Paused': [
      { value: 'Open', label: 'Возобновить', icon: 'bi-play-circle', variant: 'outline-success' },
      { value: 'Closed', label: 'Завершить', icon: 'bi-stop-circle', variant: 'outline-danger' }
    ],
    'Closed': [
      { value: 'Draft', label: 'Переоткрыть', icon: 'bi-arrow-clockwise', variant: 'outline-secondary' }
    ]
  };
  const result = transitions[normalizedStatus] || [];
  console.log('Available transitions for', normalizedStatus, ':', result);
  return result;
}

function viewSessionDetails(session) {
  alert(`Детали сессии ${session.id}`)
}

async function changeSessionStatus(sessionId, status) {
  if (confirm(`Изменить статус сессии на ${formatStatus(status)}?`)) {
    try {
      await adminStore.changeSessionStatus(sessionId, status)
      // Список сессий автоматически обновится через store
    } catch (err) {
      // Ошибка уже обработана в store
    }
  }
}

async function submitCreateSession() {
  try {
    await adminStore.createQueueSession({
      queueConfigId: sessionForm.value.queueConfigId,
      description: sessionForm.value.description || null
    })
    showCreateSessionModal.value = false
    // Сбросить форму
    sessionForm.value = {
      queueConfigId: null,
      description: ''
    }
  } catch (err) {
    // Ошибка уже обработана в store
  }
}

// QueueConfig methods
function openCreateConfigModal() {
  configForm.value = {
    name: '',
    description: '',
    distributionMode: 'Manual',
    isServiceTypeEnabled: false,
    isPriorityEnabled: true,
    priorityEscalationWaitMin: null
  }
  showCreateConfigModal.value = true
}

function openEditConfigModal(config) {
  selectedConfig.value = config
  configForm.value = {
    name: config.name,
    description: config.description || '',
    distributionMode: config.distributionMode,
    isServiceTypeEnabled: config.isServiceTypeEnabled,
    isPriorityEnabled: config.isPriorityEnabled,
    priorityEscalationWaitMin: config.priorityEscalationWaitMin
  }
  showEditConfigModal.value = true
}

async function submitCreateConfig() {
  try {
    await adminStore.createQueueConfig(configForm.value)
    showCreateConfigModal.value = false
  } catch (err) {
    // Error already handled in store
  }
}

async function submitEditConfig() {
  try {
    await adminStore.updateQueueConfig(selectedConfig.value.id, configForm.value)
    showEditConfigModal.value = false
  } catch (err) {
    // Error already handled in store
  }
}

async function deactivateConfig(id) {
  if (confirm('Деактивировать конфигурацию?')) {
    try {
      await adminStore.deactivateQueueConfig(id)
    } catch (err) {
      // Error already handled in store
    }
  }
}

// User methods
function openCreateUserModal() {
  userForm.value = {
    login: '',
    password: '',
    fullName: '',
    lastName: '',
    email: '',
    roleIds: []
  }
  showCreateUserModal.value = true
}

function openEditUserModal(user) {
  selectedUser.value = user
  userForm.value = {
    login: user.login,
    password: '', // Password not shown for security
    fullName: user.fullName,
    lastName: user.lastName,
    email: user.email || '',
    roleIds: user.roles.map(r => r.id)
  }
  showEditUserModal.value = true
}

async function submitCreateUser() {
  try {
    await adminStore.createUser(userForm.value)
    showCreateUserModal.value = false
  } catch (err) {
    // Error already handled in store
  }
}

async function submitEditUser() {
  try {
    // Remove password if empty (not updating)
    const data = { ...userForm.value }
    if (!data.password) delete data.password
    await adminStore.updateUser(selectedUser.value.id, data)
    showEditUserModal.value = false
  } catch (err) {
    // Error already handled in store
  }
}

async function deactivateUser(id) {
  if (confirm('Деактивировать пользователя?')) {
    try {
      await adminStore.deactivateUser(id)
    } catch (err) {
      // Error already handled in store
    }
  }
}

async function activateUser(id) {
  if (confirm('Активировать пользователя?')) {
    try {
      await adminStore.activateUser(id)
    } catch (err) {
      // Error already handled in store
    }
  }
}

// ServiceType methods
function openCreateServiceTypeModal() {
  serviceTypeForm.value = {
    queueConfigId: adminStore.queueConfigs.length > 0 ? adminStore.queueConfigs[0].id : null,
    name: '',
    code: '',
    letter: '',
    basePriorityLevel: 0,
    planAvgServiceTimeSec: null,
    isActive: true,
    isHighlighting: false,
    sortOrder: 0
  }
  showCreateServiceTypeModal.value = true
}

function openEditServiceTypeModal(type) {
  selectedServiceType.value = type
  serviceTypeForm.value = {
    queueConfigId: type.queueConfigId,
    name: type.name,
    code: type.code,
    letter: type.letter,
    basePriorityLevel: type.basePriorityLevel,
    planAvgServiceTimeSec: type.planAvgServiceTimeSec,
    isActive: type.isActive,
    isHighlighting: type.isHighlighting,
    sortOrder: type.sortOrder
  }
  showEditServiceTypeModal.value = true
}

async function submitCreateServiceType() {
  try {
    await adminStore.createServiceType(serviceTypeForm.value)
    showCreateServiceTypeModal.value = false
  } catch (err) {
    // Error already handled in store
  }
}

async function submitEditServiceType() {
  try {
    await adminStore.updateServiceType(selectedServiceType.value.id, serviceTypeForm.value)
    showEditServiceTypeModal.value = false
  } catch (err) {
    // Error already handled in store
  }
}

async function deactivateServiceType(id) {
  if (confirm('Деактивировать тип услуги?')) {
    try {
      await adminStore.deactivateServiceType(id)
    } catch (err) {
      // Error already handled in store
    }
  }
}
</script>

<style scoped>
.admin-view {
  min-height: 70vh;
}
.card {
  border: none;
}
</style>