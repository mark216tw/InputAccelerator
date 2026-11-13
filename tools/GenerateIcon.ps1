param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\src\InputAccelerator\Assets')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$outputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$installedFonts = [System.Drawing.Text.InstalledFontCollection]::new()
$fontFamily = if ($installedFonts.Families.Name -contains 'Noto Sans TC Black') {
    'Noto Sans TC Black'
}
elseif ($installedFonts.Families.Name -contains 'Microsoft JhengHei UI') {
    'Microsoft JhengHei UI'
}
else {
    'Microsoft JhengHei'
}

function New-IconBitmap {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $edge = [single]($Size * 0.02)
        $bounds = [System.Drawing.RectangleF]::new($edge, $edge, $Size - ($edge * 2), $Size - ($edge * 2))
        $radius = [single]($Size * 0.22)
        $diameter = [single]($radius * 2)
        $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
        try {
            $path.AddArc($bounds.X, $bounds.Y, $diameter, $diameter, 180, 90)
            $path.AddArc($bounds.Right - $diameter, $bounds.Y, $diameter, $diameter, 270, 90)
            $path.AddArc($bounds.Right - $diameter, $bounds.Bottom - $diameter, $diameter, $diameter, 0, 90)
            $path.AddArc($bounds.X, $bounds.Bottom - $diameter, $diameter, $diameter, 90, 90)
            $path.CloseFigure()

            $startColor = [System.Drawing.Color]::FromArgb(255, 30, 64, 175)
            $endColor = [System.Drawing.Color]::FromArgb(255, 59, 130, 246)
            $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new($bounds, $startColor, $endColor, 45.0)
            try {
                $graphics.FillPath($gradient, $path)
            }
            finally {
                $gradient.Dispose()
            }

            $borderWidth = [single][Math]::Max(1.0, $Size * 0.012)
            $borderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(55, 255, 255, 255), $borderWidth)
            try {
                $graphics.DrawPath($borderPen, $path)
            }
            finally {
                $borderPen.Dispose()
            }
        }
        finally {
            $path.Dispose()
        }

        $fontSize = [single]($Size * 0.64)
        $font = [System.Drawing.Font]::new($fontFamily, $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        try {
            $format = [System.Drawing.StringFormat]::new()
            try {
                $format.Alignment = [System.Drawing.StringAlignment]::Center
                $format.LineAlignment = [System.Drawing.StringAlignment]::Center
                $format.FormatFlags = [System.Drawing.StringFormatFlags]::NoClip

                $shadowOffset = [single][Math]::Max(1.0, $Size * 0.02)
                $shadowRect = [System.Drawing.RectangleF]::new(0, $shadowOffset, $Size, $Size)
                $shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(60, 0, 0, 0))
                try {
                    $graphics.DrawString('字', $font, $shadowBrush, $shadowRect, $format)
                }
                finally {
                    $shadowBrush.Dispose()
                }

                $textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
                try {
                    $graphics.DrawString('字', $font, $textBrush, [System.Drawing.RectangleF]::new(0, 0, $Size, $Size), $format)
                }
                finally {
                    $textBrush.Dispose()
                }
            }
            finally {
                $format.Dispose()
            }
        }
        finally {
            $font.Dispose()
        }

        return $bitmap
    }
    catch {
        $graphics.Dispose()
        $bitmap.Dispose()
        throw
    }
    finally {
        $graphics.Dispose()
    }
}

function Get-IconFrameBytes {
    param([System.Drawing.Bitmap]$Bitmap)

    $width = $Bitmap.Width
    $height = $Bitmap.Height
    $rect = [System.Drawing.Rectangle]::new(0, 0, $width, $height)
    $lock = $Bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $stride = $lock.Stride
        $pixels = [byte[]]::new($stride * $height)
        [System.Runtime.InteropServices.Marshal]::Copy($lock.Scan0, $pixels, 0, $pixels.Length)
    }
    finally {
        $Bitmap.UnlockBits($lock)
    }

    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([uint32]40)
        $writer.Write([int32]$width)
        $writer.Write([int32]($height * 2))
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]0)
        $writer.Write([uint32]($width * $height * 4))
        $writer.Write([int32]0)
        $writer.Write([int32]0)
        $writer.Write([uint32]0)
        $writer.Write([uint32]0)

        for ($row = $height - 1; $row -ge 0; $row--) {
            $writer.Write($pixels, $row * $stride, $width * 4)
        }

        $maskStride = [int]([Math]::Floor(($width + 31) / 32) * 4)
        $mask = [byte[]]::new($maskStride * $height)
        $writer.Write($mask)
        $writer.Flush()
        return $stream.ToArray()
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

$bitmaps = @{}
$frames = [System.Collections.Generic.List[object]]::new()

try {
    foreach ($size in $sizes) {
        $bitmap = New-IconBitmap -Size $size
        $bitmaps[$size] = $bitmap

        $previewPath = Join-Path $outputDirectory "AppIcon-$size.png"
        $bitmap.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $frames.Add([pscustomobject]@{
                Size = $size
                Data = Get-IconFrameBytes -Bitmap $bitmap
            })
    }

    $iconPath = Join-Path $outputDirectory 'AppIcon.ico'
    $fileStream = [System.IO.File]::Create($iconPath)
    $iconWriter = [System.IO.BinaryWriter]::new($fileStream)
    try {
        $iconWriter.Write([uint16]0)
        $iconWriter.Write([uint16]1)
        $iconWriter.Write([uint16]$frames.Count)

        $offset = 6 + (16 * $frames.Count)
        foreach ($frame in $frames) {
            $dimension = if ($frame.Size -ge 256) { 0 } else { $frame.Size }
            $iconWriter.Write([byte]$dimension)
            $iconWriter.Write([byte]$dimension)
            $iconWriter.Write([byte]0)
            $iconWriter.Write([byte]0)
            $iconWriter.Write([uint16]1)
            $iconWriter.Write([uint16]32)
            $iconWriter.Write([uint32]$frame.Data.Length)
            $iconWriter.Write([uint32]$offset)
            $offset += $frame.Data.Length
        }

        foreach ($frame in $frames) {
            $iconWriter.Write([byte[]]$frame.Data)
        }

        $iconWriter.Flush()
    }
    finally {
        $iconWriter.Dispose()
        $fileStream.Dispose()
    }

    Write-Output "Generated $iconPath"
    Write-Output "Font: $fontFamily"
}
finally {
    foreach ($bitmap in $bitmaps.Values) {
        $bitmap.Dispose()
    }
}
