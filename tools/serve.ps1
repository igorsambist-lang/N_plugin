# Простой статический HTTP-сервер для локальной проверки обновлений (альтернатива python -m http.server).
# Пример: powershell -File tools\serve.ps1 -Root dist\server -Port 8080
param(
    [string]$Root = ".\",
    [int]$Port = 8080
)

$rootPath = (Resolve-Path $Root).Path
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Host "Serving $rootPath at http://localhost:$Port/ (Ctrl+C to stop)"

while ($listener.IsListening) {
    $context = $null
    try {
        $context = $listener.GetContext()
    } catch {
        break
    }

    $relativePath = [System.Uri]::UnescapeDataString($context.Request.Url.LocalPath).TrimStart('/')
    if ([string]::IsNullOrEmpty($relativePath)) { $relativePath = "index.html" }
    $filePath = Join-Path $rootPath $relativePath

    if ((Test-Path $filePath -PathType Leaf) -and ($filePath.StartsWith($rootPath))) {
        try {
            $bytes = [System.IO.File]::ReadAllBytes($filePath)
            switch ([System.IO.Path]::GetExtension($filePath).ToLowerInvariant()) {
                ".json" { $context.Response.ContentType = "application/json; charset=utf-8" }
                ".zip"  { $context.Response.ContentType = "application/zip" }
                default { $context.Response.ContentType = "application/octet-stream" }
            }
            $context.Response.ContentLength64 = $bytes.Length
            $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        } catch {
            $context.Response.StatusCode = 500
        }
    } else {
        $context.Response.StatusCode = 404
    }

    $context.Response.Close()
}
