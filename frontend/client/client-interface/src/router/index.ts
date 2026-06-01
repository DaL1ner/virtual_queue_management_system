import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '../views/HomeView.vue';
import TicketView from '../views/TicketView.vue';
import NotFound from '../views/NotFound.vue';
import { useAuthStore } from '../stores/auth.store';

// В production (сборка раздаётся бэкендом по пути /client/) Vue Router должен
// использовать base /client/ для корректной работы навигации.
// В dev-режиме (Vite dev server) base должен быть /.
const isProduction = import.meta.env.PROD;

const routes = [
  {
    path: '/',
    name: 'Home',
    component: HomeView,
  },
  {
    path: '/ticket',
    name: 'Ticket',
    component: TicketView,
    meta: { requiresAuth: true },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: NotFound,
  },
];

const router = createRouter({
  history: createWebHistory(isProduction ? '/client/' : '/'),
  routes,
});

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  
  // Если маршрут требует авторизации и пользователь не авторизован
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // Перенаправляем на главную страницу
    next('/');
    return;
  }
  
  // Если пользователь авторизован и пытается попасть на главную
  if (to.path === '/' && authStore.isAuthenticated) {
    next('/ticket');
    return;
  }
  
  next();
});

export default router;