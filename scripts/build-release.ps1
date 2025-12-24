# HQ Studio Desktop - Полная сборка релиза
# Создаёт инсталлятор или ZIP для распространения

param(
    [switch]$Installer,  # Создать инсталлятор (требует Inno Setup)
    [switch]$Zip         # Создать ZIP архив
)

$ErrorActionPreference = "Stop"
$RootDir = Split-Path $PSScriptRoot -Parent
$ProjectPath = "$RootDir\HQStudio.Desktop\HQStudio.csproj"
$DistDir = "$RootDir\dist"
$PublishDir = "$DistDir\publish"

Write-Host ""
Write-Host "╔══════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     HQ Studio - Сборка релиза            ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# 1. Очистка
Write-Host "🧹 Очистка..." -ForegroundColor Yellow
Remove-Item -Recurse -Force $DistDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

# 2. Публикация (single-file, self-contained)
Write-Host "🔨 Сборка приложения..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $PublishDir `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Ошибка сборки!" -ForegroundColor Red
    exit 1
}

# Информация о сборке
$exePath = Get-ChildItem "$PublishDir\*.exe" | Select-Object -First 1
$exeSize = [math]::Round($exePath.Length / 1MB, 2)
$version = (Get-Item $exePath).VersionInfo.ProductVersion

Write-Host "✅ Сборка завершена!" -ForegroundColor Green
Write-Host "   Версия: $version" -ForegroundColor Gray
Write-Host "   Размер: $exeSize MB" -ForegroundColor Gray
Write-Host ""

# 3. Создание ZIP
if ($Zip -or (-not $Installer)) {
    Write-Host "📦 Создание ZIP архива..." -ForegroundColor Yellow
    $zipPath = "$DistDir\HQStudio-$version-win-x64.zip"
    Compress-Archive -Path "$PublishDir\*" -DestinationPath $zipPath -Force
    $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
    Write-Host "✅ ZIP: $zipPath ($zipSize MB)" -ForegroundColor Green
}

# 4. Создание инсталлятора
if ($Installer) {
    Write-Host ""
    Write-Host "📦 Создание инсталлятора..." -ForegroundColor Yellow
    
    # Ищем Inno Setup
    $innoPath = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    
    if ($innoPath) {
        # Обновляем версию в скрипте
        $issPath = "$PSScriptRoot\build-installer.iss"
        $issContent = Get-Content $issPath -Raw
        $issContent = $issContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$version`""
        Set-Content $issPath $issContent
        
        & $innoPath $issPath
        
        if ($LASTEXITCODE -eq 0) {
            $setupPath = Get-ChildItem "$DistDir\HQStudio-Setup-*.exe" | Select-Object -First 1
            Write-Host "✅ Инсталлятор: $($setupPath.FullName)" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Ошибка создания инсталлятора" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ Inno Setup не найден!" -ForegroundColor Yellow
        Write-Host "   Скачайте с https://jrsoftware.org/isinfo.php" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Готово! Файлы в папке: $DistDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Для установки:" -ForegroundColor Gray
if ($Installer) {
    Write-Host "  - Запустите HQStudio-Setup-$version.exe" -ForegroundColor White
} else {
    Write-Host "  - Распакуйте ZIP и запустите HQStudio.exe" -ForegroundColor White
}
