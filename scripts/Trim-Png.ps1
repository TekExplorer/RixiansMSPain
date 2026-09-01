param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$InputPath,

    [Parameter(Position = 1)]
    [string]$OutputPath,

    [switch]$Recurse
)

Add-Type -AssemblyName System.Drawing

# High-performance C# helper to scan transparent bounds via direct memory access (LockBits)
$cSharpCode = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class ImageTrimmer
{
    public static Rectangle GetContentBounds(Bitmap bmp, byte alphaThreshold = 0)
    {
        int width = bmp.Width;
        int height = bmp.Height;

        BitmapData data = bmp.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb
        );

        int stride = data.Stride;
        IntPtr scan0 = data.Scan0;
        int totalBytes = stride * height;
        byte[] pixels = new byte[totalBytes];
        Marshal.Copy(scan0, pixels, 0, totalBytes);
        bmp.UnlockBits(data);

        int minX = width, minY = height, maxX = -1, maxY = -1;

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                byte alpha = pixels[rowOffset + (x * 4) + 3];
                if (alpha > alphaThreshold)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX == -1 || maxY == -1)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
    }
}
"@

if (-not ([System.Management.Automation.PSTypeName]'ImageTrimmer').Type) {
    Add-Type -TypeDefinition $cSharpCode -ReferencedAssemblies System.Drawing
}

function Trim-SinglePng {
    param (
        [string]$FileIn,
        [string]$FileOut
    )

    $sourceBmp = $null
    try {
        # Read file stream to avoid GDI+ file-locking issues during in-place overwrite
        $bytes = [System.IO.File]::ReadAllBytes($FileIn)
        $ms = New-Object System.IO.MemoryStream(, $bytes)
        $sourceBmp = [System.Drawing.Image]::FromStream($ms) -as [System.Drawing.Bitmap]

        $cropArea = [ImageTrimmer]::GetContentBounds($sourceBmp, 0)

        if ($cropArea.IsEmpty) {
            Write-Warning "Skipped (fully transparent): $FileIn"
            return
        }

        if ($cropArea.Width -eq $sourceBmp.Width -and $cropArea.Height -eq $sourceBmp.Height) {
            Write-Host "No padding: $FileIn" -ForegroundColor DarkGray
            return
        }

        $croppedBmp = $sourceBmp.Clone($cropArea, $sourceBmp.PixelFormat)

        # Ensure target folder exists
        $targetDir = [System.IO.Path]::GetDirectoryName($FileOut)
        if (-not (Test-Path $targetDir)) {
            [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null
        }

        $croppedBmp.Save($FileOut, [System.Drawing.Imaging.ImageFormat]::Png)
        $croppedBmp.Dispose()

        Write-Host "Cropped [$($cropArea.Width)x$($cropArea.Height)] -> $FileOut" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to process $FileIn : $_"
    }
    finally {
        if ($sourceBmp) { $sourceBmp.Dispose() }
        if ($ms) { $ms.Dispose() }
    }
}

# Resolve input path
$resolvedInput = (Resolve-Path $InputPath -ErrorAction Stop).Path

if (Test-Path $resolvedInput -PathType Container) {
    # Directory mode
    $files = Get-ChildItem -Path $resolvedInput -Filter *.png -Recurse:$Recurse -File

    foreach ($file in $files) {
        if ($OutputPath) {
            $destDir = [System.IO.Path]::GetFullPath($OutputPath)
            $destFile = Join-Path $destDir $file.Name
        }
        else {
            $destFile = $file.FullName
        }
        Trim-SinglePng -FileIn $file.FullName -FileOut $destFile
    }
}
else {
    # Single file mode
    $destFile = if ($OutputPath) { [System.IO.Path]::GetFullPath($OutputPath) } else { $resolvedInput }
    Trim-SinglePng -FileIn $resolvedInput -FileOut $destFile
}