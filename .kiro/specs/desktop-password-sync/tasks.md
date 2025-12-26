# Задачи: Синхронизация паролей Desktop/Web

## Статус: ✅ Завершено

### Backend (API)

- [x] Добавить поле `MustChangePassword` в модель User
- [x] Создать миграцию для нового поля
- [x] Реализовать endpoint `POST /api/auth/change-password`
- [x] Реализовать endpoint `POST /api/users/{id}/reset-password`
- [x] Добавить `MustChangePassword` в JWT claims
- [x] Обновить DbSeeder для development режима

### Desktop

- [x] Создать `ChangePasswordDialog.xaml`
- [x] Добавить проверку `MustChangePassword` после входа
- [x] Реализовать методы в `ApiService`:
  - [x] `ChangePasswordAsync(currentPassword, newPassword)`
  - [x] `ResetPasswordAsync(userId)`
- [x] Добавить кнопку "Сбросить пароль" в StaffView

### Web

- [x] Проверка `MustChangePassword` при входе (опционально)

### Тесты

- [x] Тесты для `AuthController.ChangePassword`
- [x] Тесты для `UsersController.ResetPassword`

## Коммиты

- `feat(api): добавлена обязательная смена пароля при первом входе` (ba5709c)
- `feat(desktop): добавлен диалог смены пароля при первом входе` (2dd2d23)
- `feat(desktop): методы смены и сброса пароля в ApiService` (8665211)
