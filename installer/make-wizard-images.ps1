# Generates the branded installer side images (gradient + logo) used by EtherDNS.iss.
param([string]$OutDir = $PSScriptRoot)

Add-Type -AssemblyName System.Drawing

function New-WizardImage([int]$w, [int]$h, [string]$path, [bool]$large) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'

    $rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(8, 11, 21)), ([System.Drawing.Color]::FromArgb(26, 16, 48)), 90
    $g.FillRectangle($bg, $rect)

    # Soft glow blobs
    foreach ($b in @(@(-40, -30, 180, [System.Drawing.Color]::FromArgb(70, 34, 211, 238)), @(($w - 90), ($h - 160), 220,[System.Drawing.Color]::FromArgb(70, 139, 92, 246)))) {
        $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
        $gp.AddEllipse([int]$b[0], [int]$b[1], [int]$b[2], [int]$b[2])
        $pb = New-Object System.Drawing.Drawing2D.PathGradientBrush $gp
        $pb.CenterColor = $b[3]
        $pb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, $b[3].R, $b[3].G, $b[3].B))
        $g.FillPath($pb, $gp)
    }

    # Logo tile
    $size = if ($large) { 72 } else { 40 }
    $x = [int](($w - $size) / 2); $y = if ($large) { 90 } else { [int](($h - $size) / 2) }
    $tile = New-Object System.Drawing.Rectangle $x, $y, $size, $size
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $tile, ([System.Drawing.Color]::FromArgb(34, 211, 238)), ([System.Drawing.Color]::FromArgb(139, 92, 246)), 45
    $r = [int]($size * 0.28)
    $tilePath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $tilePath.AddArc($x, $y, $r * 2, $r * 2, 180, 90)
    $tilePath.AddArc($x + $size - $r * 2, $y, $r * 2, $r * 2, 270, 90)
    $tilePath.AddArc($x + $size - $r * 2, $y + $size - $r * 2, $r * 2, $r * 2, 0, 90)
    $tilePath.AddArc($x, $y + $size - $r * 2, $r * 2, $r * 2, 90, 90)
    $tilePath.CloseFigure()
    $g.FillPath($grad, $tilePath)

    $iconFont = New-Object System.Drawing.Font 'Segoe MDL2 Assets', ($size * 0.42), ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
    $g.DrawString([string][char]0xE774, $iconFont, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF $x, ($y + 2), $size, $size), $fmt)

    if ($large) {
        $title = New-Object System.Drawing.Font 'Segoe UI Semibold', 22, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
        $sub = New-Object System.Drawing.Font 'Segoe UI', 12, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
        $tr = New-Object System.Drawing.RectangleF 0, ($y + $size + 18), $w, 30
        $tg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $tr, ([System.Drawing.Color]::FromArgb(34, 211, 238)), ([System.Drawing.Color]::FromArgb(244, 114, 182)), 0
        $g.DrawString('EtherDNS', $title, $tg, $tr, $fmt)
        $g.DrawString('Network Toolkit', $sub, (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(142, 152, 189))), (New-Object System.Drawing.RectangleF 0, ($y + $size + 52), $w, 20), $fmt)
    }

    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $bmp.Dispose()
}

# Sizes for 100% DPI wizard (modern style scales them as needed).
New-WizardImage 164 314 (Join-Path $OutDir 'wizard-large.bmp') $true
New-WizardImage 55 58 (Join-Path $OutDir 'wizard-small.bmp') $false
