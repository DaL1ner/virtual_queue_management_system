<div align="center">

# Система управления виртуальной очередью<br> Virtual Queue Management System

> **Учебный проект**   
> Веб-приложение для управления виртуальной очередью в сервисных точках

[![Vue.js](https://img.shields.io/badge/Vue.js-4FC08D?logo=vuedotjs&logoColor=fff)](https://vuejs.org/)
[![Vite](https://img.shields.io/badge/Vite-646CFF?logo=vite&logoColor=fff)](https://vite-docs.ru/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?logo=bootstrap&logoColor=fff)](https://getbootstrap.ru/)
[![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fff)](https://fastapi.tiangolo.com/)
[![PostgreSQL](https://img.shields.io/badge/Postgres-%23316192.svg?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=fff)](https://www.docker.com/)

</div>

---

- [**📊 Доска задач**](https://github.com/users/DaL1ner/projects/1)
- [**📖 Документация**](https://dal1ner.github.io/virtual_queue_management_system/)

---

## 📋 Оглавление

- [Система управления виртуальной очередью Virtual Queue Management System](#система-управления-виртуальной-очередью-virtual-queue-management-system)
  - [📋 Оглавление](#-оглавление)
  - [📖 О проекте](#-о-проекте)
  - [🔸 Проблема и решение](#-проблема-и-решение)
    - [Проблема](#проблема)
    - [Решение](#решение)
  - [👥 Роли пользователей](#-роли-пользователей)
  - [🛠️ Технологический стек](#️-технологический-стек)
  - [⚙️ Функциональность MVP](#️-функциональность-mvp)
    - [Интерфейс клиента](#интерфейс-клиента)
    - [Интерфейс оператора/исполнителя](#интерфейс-оператораисполнителя)
  - [📁 Быстрый старт](#-быстрый-старт)

---

## 📖 О проекте

Virtual Queue Management System – это система управления виртуальной очередью, которая позволяет клиентам записываться в очередь через веб-интерфейс, отслеживать свой статус и получать уведомления, а операторам – эффективно управлять потоком клиентов.

**Цель проекта**: Спроектировать полную архитектуру и реализовать MVP веб-приложения для управления виртуальной очередью, обеспечивающее удобную запись, отслеживание статуса и оптимизацию фактического потока клиентов.

---

## 🔸 Проблема и решение

### Проблема
Клиенты тратят время на ожидание в физических очередях, вручную отслеживая изменения, а операторы не имеют инструментов для прогнозирования и управления потоком.

### Решение
Система предоставляет:

| Для клиентов | Для операторов и исполнителей |
|-------------|-------------------------------|
| Запись в очередь через веб-интерфейс | Управление вызовом клиентов |
| Отслеживание текущего статуса и положения | Перемещение клиентов в очереди |
| Примерное время ожидания | Статистика и мониторинг |
| Уведомления о приближении очереди | Гибкое управление потоком |
| Возможность выхода из очереди или перемещения назад | Настройка параметров очереди |

---

## 👥 Роли пользователей

| Роль | Описание |
|------|----------|
| **Наблюдатель очереди** | Только чтение, видит текущее состояние очереди без входа |
| **Неавторизованный клиент** | Новый клиент, желающий встать в очередь |
| **Клиент очереди** | Основной потребитель услуги: занимает место, отслеживает статус |
| **Исполнитель услуги** | Работает с вызванным клиентом, подтверждает завершение обслуживания |
| **Оператор** | Управляет очередью: вызывает, перемещает, удаляет клиентов |
| **Администратор** | Настраивает конфигурацию очереди, управляет пользователями и ролями |

---

## 🛠️ Технологический стек

| Категория | Технологии |-|
|-----------|------------|------------|
| **Frontend** | Vue.js 3, Vite, Bootstrap | <img height="50" src="https://raw.githubusercontent.com/marwin1991/profile-technology-icons/refs/heads/main/icons/vue_js.png"> |
| **Backend** | .NET | <img height="50" src="https://raw.githubusercontent.com/marwin1991/profile-technology-icons/refs/heads/main/icons/_net_core.png"> |
| **База данных** | PostgreSQL | <img height="50" src="https://raw.githubusercontent.com/marwin1991/profile-technology-icons/refs/heads/main/icons/postgresql.png"> |
| **Контейнеризация** | Docker | <img height="50" src="https://raw.githubusercontent.com/marwin1991/profile-technology-icons/refs/heads/main/icons/docker.png"> |
| **Обновление данных** | Polling | <img height="50" src="https://raw.githubusercontent.com/marwin1991/profile-technology-icons/refs/heads/main/icons/rest.png"> |

---

## ⚙️ Функциональность MVP

### Интерфейс клиента
- Актуализировать

### Интерфейс оператора/исполнителя
- Актуализировать

---

## 📁 Быстрый старт

```
Актуализировать
```

---

<div align="center">

**Virtual Queue Management System**

</div>
