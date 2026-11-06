# Упаковка релиза: сборка решения, ZIP-пакеты по версиям Revit, manifest.json с SHA256.
# Пример: powershell -File build\pack.ps1 -Version 1.0.0 -ReleaseNotes "Первый релиз"
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$ReleaseNotes = "",
    [string]$ServerDir = ""
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot\..").Path
if (-not $ServerDir) { $ServerDir = Join-Path $root 'dist\server' }

$dotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

Push-Location $root
& $dotnet build NPlugin.sln -c Release -v minimal --nologo
$buildExit = $LASTEXITCODE
Pop-Location
if ($buildExit -ne 0) { throw "Сборка завершилась с ошибкой $buildExit" }

New-Item -ItemType Directory -Force $ServerDir | Out-Null

$packages = @()
foreach ($rev in 2022, 2023, 2024, 2025, 2026) {
    $outDir = Join-Path $root "src\NPlugin.Revit$rev\bin\Release"
    $zipName = "NPlugin-$Version-revit$rev.zip"
    $zipPath = Join-Path $ServerDir $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    # В пакет — только бинарники; .addin и .pdb не включаем (апдейтер генерирует .addin при установке)
    $files = Get-ChildItem $outDir -File |
        Where-Object { $_.Name -ne 'NPlugin.addin' -and $_.Extension -ne '.pdb' -and $_.Extension -ne '.deps.json' }
    Compress-Archive -Path $files.FullName -DestinationPath $zipPath -CompressionLevel Optimal

    $fileHashes = @()
    foreach ($file in $files) {
        $fileHashes += [pscustomobject]@{
            path   = $file.Name
            sha256 = (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLower()
        }
    }

    $sha = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower()
    $packages += [pscustomobject]@{
        revitVersion = "$rev"
        fileName     = $zipName
        sha256       = $sha
        size         = (Get-Item $zipPath).Length
        files        = $fileHashes
    }
    Write-Host "Пакет Revit ${rev}: $zipName ($sha)"
}

$manifest = [pscustomobject]@{
    version           = $Version
    date              = (Get-Date -Format 'yyyy-MM-dd')
    minUpdaterVersion = '1.0.0'
    releaseNotes      = $ReleaseNotes
    packages          = $packages
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $ServerDir 'manifest.json') -Encoding UTF8

# Апдейтер для установщика
$updaterDir = Join-Path $root 'dist\updater'
New-Item -ItemType Directory -Force $updaterDir | Out-Null
Get-ChildItem (Join-Path $root 'src\NPlugin.Updater\bin\Release\net8.0-windows') -File |
    Where-Object { $_.Extension -ne '.pdb' } |
    Copy-Item -Destination $updaterDir -Force

Write-Host ""
Write-Host "Готово." -ForegroundColor Green
Write-Host "  Серверный каталог (залить на сервер обновлений): $ServerDir"
Write-Host "  Установщик берёт dist\server + dist\updater через install\install.bat"
