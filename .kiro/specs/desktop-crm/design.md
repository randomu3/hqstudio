# Дизайн: Desktop CRM для HQ Studio

## Архитектура MVVM

```
┌─────────────────────────────────────────────────────────┐
│                    Views (XAML)                         │
│  ┌─────────┬─────────┬─────────┬─────────┬──────────┐  │
│  │ Login   │Dashboard│ Orders  │ Clients │Callbacks │  │
│  │ Window  │  View   │  View   │  View   │  View    │  │
│  └────┬────┴────┬────┴────┬────┴────┬────┴────┬─────┘  │
│       │         │         │         │         │        │
│  ┌────▼─────────▼─────────▼─────────▼─────────▼────┐   │
│  │              ViewModels (C#)                    │   │
│  │  BaseViewModel → INotifyPropertyChanged         │   │
│  │  RelayCommand → ICommand                        │   │
│  └─────────────────────┬───────────────────────────┘   │
│                        │                               │
│  ┌─────────────────────▼───────────────────────────┐   │
│  │                 Services                         │   │
│  │  ApiService │ ApiCacheService │ ThemeService    │   │
│  └─────────────────────┬───────────────────────────┘   │
└────────────────────────┼───────────────────────────────┘
                         │ HTTP/REST
              ┌──────────▼──────────┐
              │   API (ASP.NET)     │
              └─────────────────────┘
```

## Структура проекта

```
HQStudio.Desktop/
├── Views/
│   ├── MainWindow.xaml
│   ├── LoginWindow.xaml
│   ├── DashboardView.xaml
│   ├── OrdersView.xaml
│   ├── ClientsView.xaml
│   ├── CallbacksView.xaml
│   ├── ServicesView.xaml
│   ├── StaffView.xaml
│   └── Dialogs/
│       ├── ChangePasswordDialog.xaml
│       ├── ConfirmDialog.xaml
│       └── EditOrderDialog.xaml
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── MainViewModel.cs
│   └── {Feature}ViewModel.cs
├── Services/
│   ├── ApiService.cs
│   ├── ApiCacheService.cs
│   ├── ThemeService.cs
│   └── PrintService.cs
├── Models/
│   ├── Client.cs
│   ├── Order.cs
│   └── User.cs
└── Styles/
    ├── DarkTheme.xaml
    └── LightTheme.xaml
```

## Кеширование (ApiCacheService)

```csharp
// Стратегия кеширования
- TTL: 5 минут для списков
- Инвалидация при CRUD операциях
- Rate limiting: max 10 запросов/сек

// Использование
var clients = await _cache.GetOrFetchAsync(
    "clients_list",
    () => _api.GetClientsAsync(),
    TimeSpan.FromMinutes(5)
);
```

## Темы оформления

```xaml
<!-- Динамические ресурсы -->
<SolidColorBrush x:Key="BackgroundBrush" Color="{DynamicResource BackgroundColor}"/>
<SolidColorBrush x:Key="ForegroundBrush" Color="{DynamicResource ForegroundColor}"/>

<!-- Переключение темы -->
ThemeService.SetTheme(isDark: true);
```

## Навигация

```
LoginWindow
    │
    ▼ (успешный вход)
MainWindow
    ├── Dashboard (по умолчанию)
    ├── Заказы
    ├── Клиенты
    ├── Заявки
    ├── Услуги
    ├── Сотрудники
    └── Настройки
```
