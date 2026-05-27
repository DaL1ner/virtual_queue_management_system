# Полная документация фронтенда системы

## Оглавление

1. **[Часть 1: Обзор архитектуры](frontend_documentation_part1_overview.md)**
2. **[Часть 2: Клиентский интерфейс](frontend_documentation_part2_client_interface.md)**
3. **[Часть 3: Пользовательский интерфейс](frontend_documentation_part3_user_interface.md)**
4. **[Часть 4: Общие подходы и интеграция с API](frontend_documentation_part4_integration_approaches.md)**

## Краткое описание системы

Фронтенд система управления виртуальной очередью состоит из двух независимых веб-приложений:

### Клиентский интерфейс (client-interface)
**Назначение**: Для клиентов (студентов), которые хотят встать в очередь и отслеживать свой талон
**Технологии**: Vue 3 + TypeScript + Pinia + Bootstrap 5
**Порт**: 5173
**Ключевые функции**:
  - Создание талона с device fingerprint аутентификацией
  - Отслеживание позиции в очереди в реальном времени
  - Управление талоном (отмена, перемещение назад)
  - Адаптивный дизайн для мобильных устройств

### Пользовательский интерфейс (user-interface)
**Назначение**: Для персонала (операторов, исполнителей, администраторов)
**Технологии**: Vue 3 + JavaScript + Pinia + Bootstrap 5 + Bootstrap Icons
**Порт**: 5174
**Ключевые функции**:
  - Ролевой доступ (Operator, Executor, Admin)
  - Управление очередью (drag-and-drop, вызов, отмена)
  - Реальное время обновление статусов
  - Статистика и аналитика работы очереди
  - Административное управление системой

## Архитектурные принципы

1. **Разделение ответственности** - каждый интерфейс решает свою задачу
2. **Реактивность** - Composition API для управления состоянием
3. **Централизованное состояние** - Pinia stores для глобального состояния
4. **Компонентный подход** - переиспользуемые Vue-компоненты
5. **Адаптивный дизайн** - Bootstrap 5 для кросс-платформенности
6. **REST API интеграция** - Axios с интерцепторами для работы с бэкендом

## Технологический стек

| Компонент | Клиентский интерфейс | Пользовательский интерфейс |
|-----------|----------------------|----------------------------|
| Фреймворк | Vue 3 | Vue 3 |
| Язык | TypeScript | JavaScript |
| Состояние | Pinia 3 | Pinia 2 |
| Маршрутизация | Vue Router 4 | Vue Router 4 |
| HTTP-клиент | Axios | Axios |
| UI-фреймворк | Bootstrap 5 | Bootstrap 5 + Icons |
| Сборщик | Vite | Vite |
| Аутентификация | Device Fingerprint | JWT + Роли |

## Структура проекта

```
frontend/  
├── client/                    # Клиентский интерфейс  
│   └── client-interface/      # Vue 3 + TypeScript приложение  
└── user/                     # Пользовательский интерфейс  
    └── user-interface/       # Vue 3 + JavaScript приложение  
```

## Как использовать документацию

1. **Для новых разработчиков**: Начните с [Части 1](frontend_documentation_part1_overview.md) для понимания общей архитектуры
2. **Для работы с клиентским интерфейсом**: Изучите [Часть 2](frontend_documentation_part2_client_interface.md)
3. **Для работы с пользовательским интерфейсом**: Изучите [Часть 3](frontend_documentation_part3_user_interface.md)
4. **Для понимания общих подходов**: Прочтите [Часть 4](frontend_documentation_part4_integration_approaches.md)

## Ссылки на важные файлы

### Клиентский интерфейс
- `frontend/client/client-interface/src/App.vue` - корневой компонент
- `frontend/client/client-interface/src/stores/auth.store.ts` - управление аутентификацией
- `frontend/client/client-interface/src/composables/useApi.ts` - HTTP-клиент
- `frontend/client/client-interface/src/components/TicketForm.vue` - форма создания талона

### Пользовательский интерфейс
- `frontend/user/user-interface/src/views/Dashboard.vue` - главная панель
- `frontend/user/user-interface/src/stores/auth.js` - управление аутентификацией и ролями
- `frontend/user/user-interface/src/stores/operator.js` - состояние оператора
- `frontend/user/user-interface/src/api/operator.js` - API оператора

## Разработка и запуск

### Запуск в development режиме

``` bash
# Клиентский интерфейс (порт 5173)  
cd frontend/client/client-interface  
npm install  
npm run dev  
  
# Пользовательский интерфейс (порт 5174)  
cd frontend/user/user-interface  
npm install  
npm run dev  
```

### Сборка для production

``` bash
# Клиентский интерфейс  
cd frontend/client/client-interface  
npm run build  
  
# Пользовательский интерфейс  
cd frontend/user/user-interface  
npm run build  
```

## Конфигурация

### Переменные окружения
Создайте файл `.env` в соответствующей директории:

``` env
VITE_API_BASE_URL=http://localhost:8080  
```

### Проксирование
Оба приложения настроены на проксирование запросов `/api` на бэкенд (localhost:8080) в development режиме.

## Лицензия и авторские права

Документация создана для внутреннего использования в рамках проекта "Система управления виртуальной очередью". Все права на код принадлежат разработчикам проекта.

---
*Документация создана автоматически на основе анализа кодовой базы.*