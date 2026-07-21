# .\GifToSpritesheet.ps1 -InputGif ".\bundle_of_joy.gif"
param(
    [Parameter(Mandatory = $true)][string]$InputGif,
    [string]$OutputPng
)

# Load System.Drawing library
Add-Type -AssemblyName System.Drawing

# Resolve absolute path to handle relative inputs safely
try {
    $gifPath = Convert-Path -Path $InputGif -ErrorAction Stop
}
catch {
    Write-Error "Input GIF file not found at: $InputGif"
    exit
}

# Auto-generate output path if not explicitly provided
if ([string]::IsNullOrWhiteSpace($OutputPng)) {
    $gifFile = Get-Item $gifPath
    # Creates "filename.sheet.png" in the exact same directory as the input GIF
    $outputPath = Join-Path $gifFile.DirectoryName "$($gifFile.BaseName).sheet.png"
}
else {
    $outputPath = $OutputPng
}

# Open GIF and get frame metadata
$gif = [System.Drawing.Image]::FromFile($gifPath)
$dimension = [System.Drawing.Imaging.FrameDimension]::Time
$frameCount = $gif.GetFrameCount($dimension)

$frameWidth = $gif.Width
$frameHeight = $gif.Height
$sheetWidth = $frameWidth * $frameCount

Write-Host "Processing '$($gifFile.Name)': $frameCount frames ($frameWidth x $frameHeight px each)..."

# Create a blank sprite sheet (horizontal strip)
$spriteSheet = New-Object System.Drawing.Bitmap($sheetWidth, $frameHeight)
$graphics = [System.Drawing.Graphics]::FromImage($spriteSheet)

# Clear background with transparency
$graphics.Clear([System.Drawing.Color]::Transparent)

# Draw each frame onto the horizontal strip
for ($i = 0; $i -lt $frameCount; $i++) {
    $null = $gif.SelectActiveFrame($dimension, $i)
    $xOffset = $i * $frameWidth
    $graphics.DrawImage($gif, $xOffset, 0, $frameWidth, $frameHeight)
}

# Save output PNG
$spriteSheet.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Clean up GDI+ resources
$graphics.Dispose()
$spriteSheet.Dispose()
$gif.Dispose()

Write-Host "Success! Spritesheet saved to: $outputPath" -ForegroundColor Green