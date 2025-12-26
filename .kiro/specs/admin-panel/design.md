# Дизайн: Админ-панель HQ Studio

## Архитектура

```
┌─────────────────────────────────────────────────────────┐
│                    Web (Next.js)                        │
│  ┌─────────────────────────────────────────────────┐   │
│  │              AdminPanel.tsx                      │   │
│  │  ┌─────────┬─────────┬─────────┬─────────────┐  │   │
│  │  │Callbacks│ Clients │ Orders  │ Services    │  │   │
│  │  │ Panel   │  Panel  │  Panel  │   Panel     │  │   │
│  │  └─────────┴─────────┴─────────┴─────────────┘  │   │
│  └─────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────┘
                         │ REST API
              ┌──────────▼──────────┐
              │   API (ASP.NET)     │
              │                     │
              │ • CallbacksController
              │ • ClientsController │
              │ • OrdersController  │
              │ • ServicesController│
              │ • DashboardController
              └──────────┬──────────┘
                         │
              ┌──────────▼──────────┐
              │     PostgreSQL      │
              └─────────────────────┘
```

## Компоненты Web

### AdminPanel.tsx
- Главный контейнер с табами
- Управление состоянием через React Context
- Авторизация через JWT в localStorage

### CallbacksPanel.tsx
- Таблица заявок с пагинацией
- Модальное окно деталей заявки
- Кнопки: "Создать клиента", "Создать заказ"

### ClientsPanel.tsx
- Поиск и фильтрация
- Модальные окна создания/редактирования
- Связь с заказами

## API Endpoints

| Method | Endpoint | Описание |
|--------|----------|----------|
| GET | /api/callbacks | Список заявок |
| PATCH | /api/callbacks/{id}/status | Изменить статус |
| GET | /api/clients | Список клиентов |
| POST | /api/clients | Создать клиента |
| GET | /api/orders | Список заказов |
| POST | /api/orders | Создать заказ |
| GET | /api/dashboard/stats | Статистика |

## Авторизация

```
JWT Token Structure:
{
  "sub": "user_id",
  "role": "Admin|Editor|Manager",
  "mustChangePassword": false,
  "exp": 1234567890
}
```

### Права доступа

| Роль | Callbacks | Clients | Orders | Services | Users |
|------|-----------|---------|--------|----------|-------|
| Admin | CRUD | CRUD | CRUD | CRUD | CRUD |
| Editor | Read | CRUD | CRUD | CRUD | - |
| Manager | CRUD | Read | Read | Read | - |
