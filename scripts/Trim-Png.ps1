param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$InputPath,

    [Parameter(Position = 1)]
    [string]$OutputPath
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
                // In 32bppArgb format, the byte order is Blue, Green, Red, Alpha (offset 3)
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
            return Rectangle.Empty; // Completely transparent
        }

        return new Rectangle(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
    }
}
"@

Add-Type -TypeDefinition $cSharpCode -ReferencedAssemblies System.Drawing

# Resolve input file path
$resolvedInput = (Resolve-Path $InputPath).Path
if (-not (Test-Path $resolvedInput)) {
    Write-Error "File not found: $InputPath"
    exit 1
}

# Default output replaces the input file if not specified
if (-not $OutputPath) {
    $OutputPath = $resolvedInput
}
else {
    $OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
}

# Load image
$sourceBmp = [System.Drawing.Image]::FromFile($resolvedInput) -as [System.Drawing.Bitmap]

try {
    $cropArea = [ImageTrimmer]::GetContentBounds($sourceBmp, 0)

    if ($cropArea.IsEmpty) {
        Write-Warning "The image is completely transparent. No cropping performed."
        exit 0
    }

    if ($cropArea.Width -eq $sourceBmp.Width -and $cropArea.Height -eq $sourceBmp.Height) {
        Write-Host "No transparent padding found around image." -ForegroundColor Yellow
        exit 0
    }

    # Extract cropped section
    $croppedBmp = $sourceBmp.Clone($cropArea, $sourceBmp.PixelFormat)

    # Dispose source before saving (crucial if overwriting original)
    $sourceBmp.Dispose()
    $sourceBmp = $null

    # Save to disk
    $croppedBmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $croppedBmp.Dispose()

    Write-Host "Cropped successfully:" -ForegroundColor Green
    Write-Host "  New Bounds: $($cropArea.Width) x $($cropArea.Height)"
    Write-Host "  Saved To:   $OutputPath"
}
finally {
    if ($sourceBmp) { $sourceBmp.Dispose() }
}