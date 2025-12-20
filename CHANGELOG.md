# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project setup with monorepo structure
- HQStudio.API - ASP.NET Core 8.0 backend with JWT auth
- HQStudio.Web - Next.js 14 frontend with Tailwind CSS
- HQStudio.Desktop - WPF desktop application
- Full test coverage (API, Web, Desktop)
- Docker support with hot-reload for development
- CI/CD with GitHub Actions
- Keyboard navigation in Desktop app
- Favicon for Web and Desktop

### Features
- User authentication with JWT
- Client management (CRUD)
- Order management with services
- Callback requests handling
- Dashboard with statistics
- Admin panel for content management
- Offline sync support in Desktop
- Auto-update system for Desktop

## [1.0.0] - 2024-12-21

### Added
- First stable release
- Complete API with all endpoints
- Responsive web interface
- Desktop CRM application
- PostgreSQL support for production
- SQLite for development
