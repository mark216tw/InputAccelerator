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
$fontFamilyName = if ($installedFonts.Families.Name -contains 'Noto Sans TC') {
    'Noto Sans TC'
}
elseif ($installedFonts.Families.Name -contains 'Microsoft JhengHei UI') {
    'Microsoft JhengHei UI'
}
else {
    'Microsoft JhengHei'
}

function New-RoundedRectPath {
    param(
        [System.Drawing.RectangleF]$Rect,
        [single]$Radius
    )

    $diameter = [single]($Radius * 2)
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($Rect.X, $Rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($Rect.Right - $diameter, $Rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($Rect.Right - $diameter, $Rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Rect.X, $Rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param([int]$Size)

    $scale = if ($Size -le 128) { 4 } else { 2 }
    $canvasSize = $Size * $scale

    $hiRes = [System.Drawing.Bitmap]::new($canvasSize, $canvasSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($hiRes)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $isSmall = $Size -le 24
        $edge = [single]($canvasSize * $(if ($isSmall) { 0.04 } else { 0.035 }))
        $bounds = [System.Drawing.RectangleF]::new($edge, $edge, $canvasSize - ($edge * 2), $canvasSize - ($edge * 2))
        $radius = [single]($bounds.Width * 0.23)

        $tilePath = New-RoundedRectPath -Rect $bounds -Radius $radius
        try {
            # Deep vibrant sapphire keycap base / outer housing
            $baseBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
                [System.Drawing.PointF]::new($bounds.Left, $bounds.Top),
                [System.Drawing.PointF]::new($bounds.Right, $bounds.Bottom),
                [System.Drawing.Color]::FromArgb(255, 37, 117, 252),
                [System.Drawing.Color]::FromArgb(255, 16, 39, 118)
            )
            try {
                $baseBlend = [System.Drawing.Drawing2D.ColorBlend]::new(3)
                if ($isSmall) {
                    $baseBlend.Colors = @(
                        [System.Drawing.Color]::FromArgb(255, 56, 152, 255),
                        [System.Drawing.Color]::FromArgb(255, 33, 96, 238),
                        [System.Drawing.Color]::FromArgb(255, 22, 54, 158)
                    )
                }
                else {
                    $baseBlend.Colors = @(
                        [System.Drawing.Color]::FromArgb(255, 42, 128, 255),
                        [System.Drawing.Color]::FromArgb(255, 24, 72, 204),
                        [System.Drawing.Color]::FromArgb(255, 15, 36, 112)
                    )
                }
                $baseBlend.Positions = @(0.0, 0.48, 1.0)
                $baseBrush.InterpolationColors = $baseBlend
                $graphics.FillPath($baseBrush, $tilePath)
            }
            finally {
                $baseBrush.Dispose()
            }

            # Clip all interior lighting, keycap face, and inner border strictly inside tilePath
            $savedState = $graphics.Save()
            try {
                $graphics.SetClip($tilePath)

                if (-not $isSmall) {
                    $insetX = [single]($bounds.Width * 0.082)
                    $insetTop = [single]($bounds.Height * 0.068)
                    $insetBottom = [single]($bounds.Height * 0.118)
                    $keycapRect = [System.Drawing.RectangleF]::new(
                        $bounds.X + $insetX,
                        $bounds.Y + $insetTop,
                        $bounds.Width - ($insetX * 2),
                        $bounds.Height - $insetTop - $insetBottom
                    )
                    $keycapRadius = [single]($keycapRect.Width * 0.185)
                    $keycapPath = New-RoundedRectPath -Rect $keycapRect -Radius $keycapRadius
                    try {
                        # Soft drop shadow under raised keycap face
                        $capShadowRect = [System.Drawing.RectangleF]::new(
                            $keycapRect.X,
                            $keycapRect.Y + [single]($bounds.Height * 0.024),
                            $keycapRect.Width,
                            $keycapRect.Height
                        )
                        $capShadowPath = New-RoundedRectPath -Rect $capShadowRect -Radius $keycapRadius
                        try {
                            $capShadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(72, 8, 18, 62))
                            try {
                                $graphics.FillPath($capShadowBrush, $capShadowPath)
                            }
                            finally {
                                $capShadowBrush.Dispose()
                            }
                        }
                        finally {
                            $capShadowPath.Dispose()
                        }

                        # Saturated Fluent cobalt-azure keycap top face (opaque so zero milky washout)
                        $capBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
                            [System.Drawing.PointF]::new($keycapRect.Left, $keycapRect.Top),
                            [System.Drawing.PointF]::new($keycapRect.Right, $keycapRect.Bottom),
                            [System.Drawing.Color]::FromArgb(255, 68, 168, 255),
                            [System.Drawing.Color]::FromArgb(255, 29, 78, 216)
                        )
                        try {
                            $capBlend = [System.Drawing.Drawing2D.ColorBlend]::new(3)
                            $capBlend.Colors = @(
                                [System.Drawing.Color]::FromArgb(255, 72, 172, 255),
                                [System.Drawing.Color]::FromArgb(255, 38, 108, 244),
                                [System.Drawing.Color]::FromArgb(255, 27, 72, 206)
                            )
                            $capBlend.Positions = @(0.0, 0.50, 1.0)
                            $capBrush.InterpolationColors = $capBlend
                            $graphics.FillPath($capBrush, $keycapPath)
                        }
                        finally {
                            $capBrush.Dispose()
                        }

                        # Subtle top-left specular glow clipped to keycap face
                        $capState = $graphics.Save()
                        try {
                            $graphics.SetClip($keycapPath)
                            $glowRect = [System.Drawing.RectangleF]::new(
                                $keycapRect.Left - ($keycapRect.Width * 0.25),
                                $keycapRect.Top - ($keycapRect.Height * 0.30),
                                $keycapRect.Width * 1.15,
                                $keycapRect.Height * 1.0
                            )
                            $glowPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
                            try {
                                $glowPath.AddEllipse($glowRect)
                                $glowBrush = [System.Drawing.Drawing2D.PathGradientBrush]::new($glowPath)
                                try {
                                    $glowBrush.CenterPoint = [System.Drawing.PointF]::new(
                                        $keycapRect.Left + ($keycapRect.Width * 0.24),
                                        $keycapRect.Top + ($keycapRect.Height * 0.16)
                                    )
                                    $glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(72, 186, 232, 255)
                                    $glowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 56, 148, 255))
                                    $graphics.FillPath($glowBrush, $glowPath)
                                }
                                finally {
                                    $glowBrush.Dispose()
                                }
                            }
                            finally {
                                $glowPath.Dispose()
                            }
                        }
                        finally {
                            $graphics.Restore($capState)
                        }

                        # Crisp top-lit inner rim on keycap face
                        $capRimBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
                            [System.Drawing.PointF]::new($keycapRect.Left, $keycapRect.Top),
                            [System.Drawing.PointF]::new($keycapRect.Left, $keycapRect.Bottom),
                            [System.Drawing.Color]::FromArgb(145, 224, 244, 255),
                            [System.Drawing.Color]::FromArgb(20, 255, 255, 255)
                        )
                        try {
                            $capRimWidth = [single][Math]::Max(1.2, $canvasSize * 0.0085)
                            $capRimPen = [System.Drawing.Pen]::new($capRimBrush, $capRimWidth)
                            try {
                                $graphics.DrawPath($capRimPen, $keycapPath)
                            }
                            finally {
                                $capRimPen.Dispose()
                            }
                        }
                        finally {
                            $capRimBrush.Dispose()
                        }
                    }
                    finally {
                        $keycapPath.Dispose()
                    }
                }

                # Inner edge highlight strictly clipped inside tilePath (zero exterior halo)
                $borderWidth = [single][Math]::Max(1.5, $canvasSize * 0.022)
                $borderBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
                    [System.Drawing.PointF]::new($bounds.Left, $bounds.Top),
                    [System.Drawing.PointF]::new($bounds.Right, $bounds.Bottom),
                    [System.Drawing.Color]::FromArgb(130, 196, 232, 255),
                    [System.Drawing.Color]::FromArgb(80, 10, 24, 72)
                )
                try {
                    $borderPen = [System.Drawing.Pen]::new($borderBrush, $borderWidth)
                    try {
                        $graphics.DrawPath($borderPen, $tilePath)
                    }
                    finally {
                        $borderPen.Dispose()
                    }
                }
                finally {
                    $borderBrush.Dispose()
                }
            }
            finally {
                $graphics.Restore($savedState)
            }
        }
        finally {
            $tilePath.Dispose()
        }

        # Render 「字」 with Winding fill mode so overlapping TrueType contours never punch holes
        $fontSize = [single]($canvasSize * $(if ($isSmall) { 0.64 } else { 0.54 }))
        $font = [System.Drawing.Font]::new($fontFamilyName, $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        try {
            $format = [System.Drawing.StringFormat]::GenericTypographic.Clone()
            try {
                $format.FormatFlags = $format.FormatFlags -bor [System.Drawing.StringFormatFlags]::NoClip

                $glyphPath = [System.Drawing.Drawing2D.GraphicsPath]::new([System.Drawing.Drawing2D.FillMode]::Winding)
                try {
                    $glyphPath.AddString('字', $font.FontFamily, [int]$font.Style, $font.Size, [System.Drawing.PointF]::new(0, 0), $format)
                    $glyphBounds = $glyphPath.GetBounds()

                    $centerX = [single]($canvasSize / 2.0)
                    $centerY = if ($isSmall) {
                        [single]($canvasSize / 2.0)
                    }
                    else {
                        # Optically centered inside the raised keycap face
                        [single]($bounds.Y + ($bounds.Height * 0.474))
                    }

                    $offsetX = [single]($centerX - ($glyphBounds.X + ($glyphBounds.Width / 2.0)))
                    $offsetY = [single]($centerY - ($glyphBounds.Y + ($glyphBounds.Height / 2.0)))

                    # Drop shadow behind 「字」
                    $shadowLayers = if ($isSmall) {
                        @(@{ Dy = [single]($canvasSize * 0.025); Alpha = 64 })
                    }
                    else {
                        @(
                            @{ Dy = [single]($canvasSize * 0.032); Alpha = 46 },
                            @{ Dy = [single]($canvasSize * 0.015); Alpha = 86 }
                        )
                    }
                    foreach ($layer in $shadowLayers) {
                        $shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb($layer.Alpha, 10, 26, 84))
                        try {
                            $graphics.TranslateTransform($offsetX, $offsetY + $layer.Dy)
                            $graphics.FillPath($shadowBrush, $glyphPath)
                            $graphics.ResetTransform()
                        }
                        finally {
                            $shadowBrush.Dispose()
                        }
                    }

                    # Crisp luminous white fill for 「字」
                    $textGradRect = [System.Drawing.RectangleF]::new(
                        $glyphBounds.X,
                        $glyphBounds.Y,
                        [Math]::Max(1.0, $glyphBounds.Width),
                        [Math]::Max(1.0, $glyphBounds.Height)
                    )
                    $textBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
                        $textGradRect,
                        [System.Drawing.Color]::FromArgb(255, 255, 255, 255),
                        [System.Drawing.Color]::FromArgb(255, 232, 243, 255),
                        90.0
                    )
                    try {
                        $graphics.TranslateTransform($offsetX, $offsetY)
                        $graphics.FillPath($textBrush, $glyphPath)
                        $graphics.ResetTransform()
                    }
                    finally {
                        $textBrush.Dispose()
                    }
                }
                finally {
                    $glyphPath.Dispose()
                }
            }
            finally {
                $format.Dispose()
            }
        }
        finally {
            $font.Dispose()
        }

        # High-quality downsample to target size
        $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $downGraphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $downGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $downGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $downGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $downGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $downGraphics.Clear([System.Drawing.Color]::Transparent)

            $destRect = [System.Drawing.Rectangle]::new(0, 0, $Size, $Size)
            $downGraphics.DrawImage($hiRes, $destRect, 0, 0, $canvasSize, $canvasSize, [System.Drawing.GraphicsUnit]::Pixel)
        }
        catch {
            $bitmap.Dispose()
            throw
        }
        finally {
            $downGraphics.Dispose()
        }

        return $bitmap
    }
    finally {
        $graphics.Dispose()
        $hiRes.Dispose()
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
    Write-Output "Font: $fontFamilyName"
}
finally {
    foreach ($bitmap in $bitmaps.Values) {
        $bitmap.Dispose()
    }
}
