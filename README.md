# HQ Studio

Комплексное решение для автотюнинг студии: веб-сайт, API и десктопное CRM приложение.

## 🏗️ Структура проекта

```
├── HQStudio.API/          # ASP.NET Core 8.0 Backend
├── HQStudio.API.Tests/    # API Integration Tests
├── HQStudio.Web/          # Next.js 14 Frontend
├── HQStudio.Desktop/      # WPF Desktop Application
├── HQStudio.Desktop.Tests/# Desktop Unit Tests
└── docker-compose.yml     # Production Docker setup
```

## 🚀 Быстрый старт

### Требования
- .NET 8.0 SDK
- Node.js 20+
- Docker (опционально)

### Локальная разработка

```bash
# Клонировать репозиторий
git clone https://github.com/randomu3/hqstudio.git
cd hqstudio

# Скопировать env файлы
cp .env.example .env

# Запустить API
cd HQStudio.API
dotnet run

# Запустить Web (в другом терминале)
cd HQStudio.Web
npm install
npm run dev

# Запустить Desktop (Windows)
cd HQStudio.Desktop
dotnet run
```

### Docker (разработка с hot-reload)

```bash
docker-compose -f docker-compose.dev.yml up --build
```

### Docker (production)

```bash
docker-compose up --build -d
```

## 🧪 Тесты

```bash
# API тесты
dotnet test HQStudio.API.Tests

# Web тесты
cd HQStudio.Web && npm test

# Desktop тесты
dotnet test HQStudio.Desktop.Tests
```

## 📦 Технологии

### Backend (API)
- ASP.NET Core 8.0
- Entity Framework Core
- PostgreSQL / SQLite
- JWT Authentication
- Swagger/OpenAPI

### Frontend (Web)
- Next.js 14 (App Router)
- React 18
- TypeScript
- Tailwind CSS
- Framer Motion

### Desktop
- .NET 8.0 WPF
- MVVM Pattern
- Material Design

## 🔐 Переменные окружения

См. `.env.example` для полного списка переменных.

## 📝 Лицензия

MIT License
