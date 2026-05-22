import { createApp } from 'vue';
import './style.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap';

import App from './App.vue';
import router from './router';
import pinia from './stores';

createApp(App)
  .use(router)
  .use(pinia)
  .mount('#app');
