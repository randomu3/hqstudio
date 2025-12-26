# Git-интеграция и CI/CD

[![en](https://img.shields.io/badge/lang-en-blue.svg)](GIT-INTEGRATION.md) [![ru](https://img.shields.io/badge/lang-ru-red.svg)](GIT-INTEGRATION.ru.md)

Полная документация по интеграции с Git, автоматизации и CI/CD пайплайнам проекта HQ Studio.

## Содержание

- [Обзор инфраструктуры](#обзор-инфраструктуры)
- [Conventional Commits](#conventional-commits)
- [Git Hooks (Husky)](#git-hooks-husky)
- [GitHub Actions Workflows](#github-actions-workflows)
- [Semantic Release](#semantic-release)
- [Dependabot](#dependabot)
- [Codecov](#codecov)

---

## Обзор инфраструктуры

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Git Push to main                            │
└─────────────────────────────────────────────────────────────────────┘
                                   │
         ┌─────────────────────────┼─────────────────────────┐
         │                         │                         │
         ▼                         ▼                         ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   CI Workflow   │    │ Release Workflow│    │ Pages Workflow  │
│                 │    │                 │    │                 │
│ • API Tests     │    │ • Semantic Ver  │    │ • Build Next.js │
│ • Web Tests     │    │ • CHANGELOG     │    │ • Deploy Pages  │
│ • Desktop Build │    │ • GitHub Release│    │                 │
│ • Docker Build  │    │ • Docker Push   │    │                 │
│ • Codecov       │    │ • Desktop ZIP   │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Ключевые компоненты

| Компонент | Файл | Назначение |
|-----------|------|------------|
| Commitlint | `commitlint.config.js` | Валидация сообщений коммитов |
| Husky | `.husky/commit-msg` | Git hooks для проверки коммитов |
| Semantic Release | `.releaserc.json` | Автоматическое версионирование |
| Dependabot | `.github/dependabot.yml` | Автообновление зависимостей |
| Codecov | `codecov.yml` | Отслеживание покрытия кода |

---

## Conventional Commits

Проект использует [Conventional Commits](https://www.conventionalcommits.org/) для стандартизации сообщений коммитов.

### Формат

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Типы коммитов

| Тип | Описание | Влияние на версию |
|-----|----------|-------------------|
| `feat` | Новая функциональность | **minor** (1.x.0) |
| `fix` | Исправление бага | **patch** (1.0.x) |
| `perf` | Улучшение производительности | **patch** |
| `refactor` | Рефакторинг кода | **patch** |
| `docs` | Документация | Без релиза |
| `style` | Форматирование | Без релиза |
| `test` | Тесты | Без релиза |
| `build` | Сборка/зависимости | Без релиза |
| `ci` | CI/CD конфигурация | Без релиза |
| `chore` | Прочие изменения | Без релиза |

### Области (Scopes)

| Scope | Описание |
|-------|----------|
| `api` | HQStudio.API (ASP.NET Core) |
| `web` | HQStudio.Web (Next.js) |
| `desktop` | HQStudio.Desktop (WPF) |
| `tests` | Тесты любого компонента |
| `docker` | Docker конфигурация |
| `ci` | CI/CD пайплайны |
| `deps` | Зависимости |

### Примеры коммитов

```bash
# Новая функция (minor release)
feat(api): add order export endpoint

# Исправление бага (patch release)
fix(web): fix contact form validation error

# Документация (без релиза)
docs: update API documentation

# Breaking change (major release)
feat(api)!: change API response format

BREAKING CHANGE: `status` field now returns enum instead of string
```

### Интерактивный коммит

```bash
npm run commit
```

---

## Git Hooks (Husky)

### Структура

```
.husky/
├── _/                  # Husky internals
└── commit-msg          # Валидация сообщения коммита
```

### Установка hooks

```bash
# Автоматически при npm install
npm install

# Или вручную
npx husky install
```

---

## GitHub Actions Workflows

### 1. CI Workflow (`ci.yml`)

**Триггеры:** Push/PR в `main`, `develop`

**Jobs:**
- `api-test` — Ubuntu, .NET 8.0, xUnit тесты
- `web-test` — Ubuntu, Node 20, ESLint, Vitest
- `desktop-build` — Windows, .NET 8.0
- `docker-build` — сборка Docker образов

### 2. Release Workflow (`release.yml`)

**Триггеры:** Push в `main`

**Этапы:**
1. Анализ коммитов с последнего релиза
2. Определение новой версии (semver)
3. Генерация CHANGELOG.md
4. Создание Git tag и GitHub Release
5. Push Docker images в GHCR
6. Сборка Desktop ZIP

### 3. Pages Workflow (`pages.yml`)

**Триггеры:** Push в `main`

**URL:** https://randomu3.github.io/hqstudio/

### 4. CodeQL Workflow (`codeql.yml`)

**Триггеры:** Push/PR в `main` + Weekly (понедельник 6:00 UTC)

**Языки:** C#, JavaScript/TypeScript

---

## Semantic Release

### Release Rules

| Тип коммита | Релиз |
|-------------|-------|
| `feat` | minor |
| `fix`, `perf`, `refactor` | patch |
| `docs`, `style`, `chore`, `test`, `build`, `ci` | Без релиза |

### Секции CHANGELOG

| Тип | Секция |
|-----|--------|
| `feat` | Features |
| `fix` | Bug Fixes |
| `perf` | Performance |
| `refactor` | Refactoring |

### Локальный dry-run

```bash
npm run release:dry
```

---

## Dependabot

### Расписание обновлений

| Ecosystem | Directory | Schedule |
|-----------|-----------|----------|
| npm | `/HQStudio.Web` | Weekly (Mon) |
| npm | `/` (root) | Monthly |
| nuget | `/HQStudio.API` | Weekly (Mon) |
| nuget | `/HQStudio.Desktop` | Weekly (Mon) |
| github-actions | `/` | Monthly |

**Auto-merge:** patch/minor updates автоматически мержатся после прохождения CI.

---

## Codecov

### Flags

| Flag | Путь | Источник |
|------|------|----------|
| `api` | HQStudio.API/ | xUnit + coverlet |
| `web` | HQStudio.Web/lib/ | Vitest + v8 |

### Локальный запуск с coverage

```bash
# API
dotnet test HQStudio.API.Tests --collect:"XPlat Code Coverage"

# Web
cd HQStudio.Web && npm test -- --coverage
```

---

## Быстрые команды

```bash
# Тесты перед коммитом
dotnet test HQStudio.API.Tests
npm test --prefix HQStudio.Web

# Интерактивный коммит
npm run commit

# Dry-run релиза
npm run release:dry

# Статус CI
gh run list --limit 5
```
