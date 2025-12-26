# Задачи: Админ-панель HQ Studio

## Статус: ✅ Завершено (базовая версия)

### Backend API

- [x] CallbacksController - CRUD для заявок
- [x] ClientsController - CRUD для клиентов
- [x] OrdersController - CRUD для заказов
- [x] ServicesController - CRUD для услуг
- [x] DashboardController - статистика
- [x] SiteContentController - контент сайта
- [x] JWT авторизация с ролями

### Web Frontend

- [x] AdminPanel.tsx - главный контейнер
- [x] CallbacksPanel.tsx - управление заявками
- [x] Авторизация через JWT
- [x] Кастомные UI компоненты (CustomSelect, модальные окна)

### Desktop (параллельная реализация)

- [x] Интеграция с API
- [x] CallbacksView - заявки с сайта
- [x] ClientsView - клиенты
- [x] OrdersView - заказы
- [x] ServicesView - услуги
- [x] DashboardView - аналитика

### Тесты

- [x] CallbacksControllerTests
- [x] ServicesControllerTests
- [x] DashboardControllerTests

## Будущие улучшения

- [ ] Расширенная фильтрация заказов
- [ ] Экспорт в Excel из Web
- [ ] Push-уведомления о новых заявках в Web
- [ ] Графики в Dashboard (Web)
