# Дизайн: Синхронизация паролей Desktop/Web

## Архитектура

```
┌─────────────────┐     ┌─────────────────┐
│  Desktop (WPF)  │     │   Web (Next.js) │
└────────┬────────┘     └────────┬────────┘
         │                       │
         └───────────┬───────────┘
                     │
              ┌──────▼──────┐
              │  API (.NET) │
              │             │
              │ AuthController
              │ UsersController
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │  PostgreSQL │
              │   (Users)   │
              └─────────────┘
```

## Модель данных

### User (расширение)
```csharp
public class User
{
    // ... существующие поля
    public bool MustChangePassword { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
}
```

## API Endpoints

### POST /api/auth/change-password
```json
Request:
{
  "currentPassword": "string",
  "newPassword": "string"
}

Response: 200 OK
{
  "message": "Пароль успешно изменён"
}
```

### POST /api/users/{id}/reset-password (Admin only)
```json
Response: 200 OK
{
  "temporaryPassword": "string",
  "message": "Пароль сброшен"
}
```

## Desktop Flow

```
LoginWindow
    │
    ▼ (успешный вход)
    │
    ├─── MustChangePassword = true ───► ChangePasswordDialog
    │                                          │
    │                                          ▼
    │                                   (смена пароля)
    │                                          │
    └─── MustChangePassword = false ◄──────────┘
                    │
                    ▼
              MainWindow
```

## Безопасность

- BCrypt с cost factor 12
- JWT токен истекает через 24 часа
- Lockout на 15 минут после 5 неудачных попыток
