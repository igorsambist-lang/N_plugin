# Установка плагина на текущий ПК (per-user, без прав администратора).
# Берёт пакеты из dist\server (см. build\pack.ps1) и апдейтер из dist\updater.
$ErrorActionPreference = 'Stop'
$installDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverDir = Join-Path $installDir '..\dist\server'
$updaterSrc = Join-Path $installDir '..\dist\updater'

$baseDir = "$env:LOCALAPPDATA\NPlugin"
$pluginRoot = "$baseDir\plugin"
$addinsRoot = "$env:APPDATA\Autodesk\Revit\Addins"

# --- 1. Апдейтер ---
$updaterDest = "$baseDir\updater"
if (Test-Path $updaterDest) {
    Move-Item -LiteralPath $updaterDest -Destination "$updaterDest.bak-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
}
New-Item -ItemType Directory -Force $updaterDest | Out-Null
Copy-Item (Join-Path $updaterSrc '*') $updaterDest -Force
Write-Host "Апдейтер установлен: $updaterDest"

# --- 2. Плагин по версиям Revit ---
$manifest = Get-Content (Join-Path $serverDir 'manifest.json') -Raw | ConvertFrom-Json
foreach ($pkg in $manifest.packages) {
    $zip = Join-Path $serverDir $pkg.fileName
    $dest = Join-Path $pluginRoot $pkg.revitVersion
    if (Test-Path $dest) {
        Move-Item -LiteralPath $dest -Destination "$dest.bak-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    }
    New-Item -ItemType Directory -Force $dest | Out-Null
    Expand-Archive -Path $zip -DestinationPath $dest -Force

    # best-effort: чистим старые бэкапы (в ограниченных средах удаление может быть запрещено — просто пропускаем)
    Get-ChildItem $pluginRoot -Directory -Filter "$($pkg.revitVersion).bak-*" -ErrorAction SilentlyContinue | ForEach-Object {
        try { Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop } catch { }
    }

    $assemblyPath = Join-Path $dest 'NPlugin.dll'
    $addinDir = Join-Path $addinsRoot $pkg.revitVersion
    New-Item -ItemType Directory -Force $addinDir | Out-Null
    $addinPath = Join-Path $addinDir 'NPlugin.addin'
    @"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>N Plugin</Name>
    <Assembly>$assemblyPath</Assembly>
    <AddInId>3f9c1e52-b7a4-4d2e-9a6c-1e8f3d5b7a20</AddInId>
    <FullClassName>NPlugin.Application.NPluginApp</FullClassName>
    <VendorId>NPLG</VendorId>
    <VendorDescription>N Plugin</VendorDescription>
  </AddIn>
</RevitAddIns>
"@ | Set-Content $addinPath -Encoding UTF8
    Write-Host "Revit $($pkg.revitVersion): плагин в $dest, манифест $addinPath"
}

# --- 3. Локальная версия ---
New-Item -ItemType Directory -Force $baseDir | Out-Null
(@{ version = $manifest.version } | ConvertTo-Json) | Set-Content "$baseDir\version.json" -Encoding UTF8

# --- 4. Автопроверка обновлений при входе ---
$updaterExe = Join-Path $updaterDest 'NPlugin.Updater.exe'
$autostartDone = $false
try {
    $action = New-ScheduledTaskAction -Execute $updaterExe -Argument '/silent'
    $trigger = New-ScheduledTaskTrigger -AtLogOn
    Register-ScheduledTask -TaskName 'NPluginUpdater' -Action $action -Trigger $trigger -Force -ErrorAction Stop -Description 'Проверка обновлений N Plugin' | Out-Null
    Write-Host "Задача планировщика NPluginUpdater создана (запуск при входе, /silent)."
    $autostartDone = $true
} catch {
    Write-Host "Планировщик задач недоступен без прав администратора — переключаемся на автозапуск пользователя."
}

if (-not $autostartDone) {
    $runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    Set-ItemProperty -Path $runKey -Name 'NPluginUpdater' -Value "`"$updaterExe`" /silent"
    Write-Host "Автозапуск добавлен: HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NPluginUpdater"
}

Write-Host ""
Write-Host "Установка завершена. Откройте Revit — на вкладке «N Plugin» появятся команды плагина." -ForegroundColor Green
