# Setup GitHub Actions Runner autostart
# RUN AS ADMINISTRATOR!

$ErrorActionPreference = "Stop"

Write-Host "Setting up GitHub Actions Runner autostart..." -ForegroundColor Cyan

$taskName = "GitHub Actions Runner"
$runnerPath = "C:\actions-runner\run.cmd"
$workingDir = "C:\actions-runner"

if (-not (Test-Path $runnerPath)) {
    Write-Host "ERROR: Runner not found at $runnerPath" -ForegroundColor Red
    exit 1
}

$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "Removing existing task..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

Write-Host "Creating scheduled task..." -ForegroundColor Green

$action = New-ScheduledTaskAction -Execute $runnerPath -WorkingDirectory $workingDir
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit (New-TimeSpan -Hours 0)

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force

Write-Host "Task created successfully!" -ForegroundColor Green
Write-Host "Runner will start automatically on Windows startup." -ForegroundColor Cyan
