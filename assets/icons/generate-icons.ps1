# Генерация простых PNG-иконок для кнопок ribbon (16 и 32 px).
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$icons = @(
    @{ n = 'print';    c = [System.Drawing.Color]::FromArgb(0x2E, 0x86, 0xDE); t = 'P' },
    @{ n = 'navis';    c = [System.Drawing.Color]::FromArgb(0x10, 0xAC, 0x84); t = 'N' },
    @{ n = 'export';   c = [System.Drawing.Color]::FromArgb(0xC0, 0x4E, 0xB0); t = 'E' },
    @{ n = 'clash';    c = [System.Drawing.Color]::FromArgb(0xD6, 0x30, 0x30); t = 'C' },
    @{ n = 'systems';  c = [System.Drawing.Color]::FromArgb(0xC8, 0x7D, 0x1E); t = 'S' },
    @{ n = 'worksets'; c = [System.Drawing.Color]::FromArgb(0x3B, 0x7D, 0xC8); t = 'W' },
    @{ n = 'release';  c = [System.Drawing.Color]::FromArgb(0x6A, 0x3D, 0xC8); t = 'R' },
    @{ n = 'update';   c = [System.Drawing.Color]::FromArgb(0x4A, 0x55, 0x60); t = 'U' }
)

foreach ($icon in $icons) {
    foreach ($size in 16, 32) {
        $bmp = New-Object System.Drawing.Bitmap $size, $size
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear($icon.c)
        $font = New-Object System.Drawing.Font('Arial', [single]($size * 0.55), [System.Drawing.FontStyle]::Bold)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
        $g.DrawString($icon.t, $font, [System.Drawing.Brushes]::White, $rect, $format)
        $bmp.Save("$root\$($icon.n)$size.png", [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose()
        $bmp.Dispose()
        $font.Dispose()
        $format.Dispose()
    }
}

Write-Host "Icons generated in $root"
