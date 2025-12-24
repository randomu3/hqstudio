# HQ Studio Desktop - Build & Publish Script
# Создаёт оптимизированную сборку для распространения

param(
    [switch]$CreateInstaller,
    [switch]$CreateZip,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ProjectPath = "$PSScriptRoot\..\HQStudio.Desktop\HQStudio.csproj"
$PublishDir = "$PSScriptRoot\..\dist"
$OutputDir = "$PublishDir\HQStudio"

Write-Host "╔══════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     HQ Studio Desktop - Build Script     ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Очистка
Write-Host "🧹 Очистка предыдущей сборки..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Сборка
Write-Host "🔨 Сборка проекта ($Configuration)..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputDir `
    -p:PublishReadyToRun=true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Ошибка сборки!" -ForegroundColor Red
    exit 1
}

# Информация о сборке
$exePath = Get-ChildItem "$OutputDir\*.exe" | Select-Object -First 1
$exeSize = [math]::Round($exePath.Length / 1MB, 2)
Write-Host ""
Write-Host "✅ Сборка завершена!" -ForegroundColor Green
Write-Host "   Файл: $($exePath.Name)" -ForegroundColor Gray
Write-Host "   Размер: $exeSize MB" -ForegroundColor Gray

# Создание ZIP архива
if ($CreateZip) {
    Write-Host ""
    Write-Host "📦 Создание ZIP архива..." -ForegroundColor Yellow
    
    $version = (Get-Item $exePath).VersionInfo.FileVersion
    $zipPath = "$PublishDir\HQStudio-$version-win-x64.zip"
    
    Compress-Archive -Path "$OutputDir\*" -DestinationPath $zipPath -Force
    
    $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
    Write-Host "✅ ZIP создан: $zipPath ($zipSize MB)" -ForegroundColor Green
}

# Создание инсталлятора (требует Inno Setup)
if ($CreateInstaller) {
    Write-Host ""
    Write-Host "📦 Создание инсталлятора..." -ForegroundColor Yellow
    
    $innoPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $innoPath)) {
        $innoPath = "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    }
    
    if (Test-Path $innoPath) {
        & $innoPath "$PSScriptRoot\build-installer.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Инсталлятор создан в папке dist\" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Ошибка создания инсталлятора" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ Inno Setup не найден. Установите с https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Готово! Файлы в папке: $PublishDir" -ForegroundColor Cyan
