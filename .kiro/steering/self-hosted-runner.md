---
inclusion: manual
---

# Self-Hosted GitHub Actions Runner

## Статус
Runner установлен на локальной машине для выполнения задач, требующих GUI (скриншоты Desktop приложения).

## Расположение
```
C:\actions-runner\
```

## Запуск Runner'а

### Вариант 1: В фоне (рекомендуется)
```powershell
Start-Process -FilePath "C:\actions-runner\run.cmd" -WorkingDirectory "C:\actions-runner" -WindowStyle Hidden
```

### Вариант 2: В отдельном окне
```powershell
C:\actions-runner\run.cmd
```

**ВАЖНО:** После перезагрузки ПК runner нужно запустить вручную!

## Автозапуск при старте Windows

Запустить PowerShell **от администратора** и выполнить:
```powershell
$action = New-ScheduledTaskAction -Execute "C:\actions-runner\run.cmd" -WorkingDirectory "C:\actions-runner"
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERNAME" -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
Register-ScheduledTask -TaskName "GitHub Actions Runner" -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force
```

## Проверка статуса
```powershell
# Проверить процесс
Get-Process -Name "Runner.Listener" -ErrorAction SilentlyContinue

# Или через GitHub
# Settings → Actions → Runners → должен быть статус "Idle" (зелёный)
```

- Имя runner'а: `hqstudio-local`
- Labels: `self-hosted`, `Windows`, `X64`, `screenshots`

## Скрытый режим скриншотов

При запуске workflow скриншоты делаются в скрытом режиме:
- `SCREENSHOT_HIDDEN=true` — окна рендерятся за пределами экрана (-10000, -10000)
- Процесс запускается с `-WindowStyle Hidden`
- Пользователь не видит мелькающих окон

## Если runner не запущен
Workflows с `runs-on: self-hosted` будут висеть в очереди "Waiting for a runner".

## Остановка runner'а
```powershell
Stop-Process -Name "Runner.Listener" -Force
```

## Безопасность
⚠️ Public репозиторий + self-hosted runner = риск. Злоумышленник может создать PR с вредоносным кодом, который выполнится на твоём ПК.

Рекомендации:
- Не принимать PR от незнакомых людей без review
- Использовать `pull_request_target` вместо `pull_request` для workflows
- Или сделать репозиторий приватным
