# Configuration
$assetFolder = "artist_assets"
$jsonPath = "HideDetailsMod/localization/eng/artists.json"
$extensions = @("*.png", "*.jpg", "*.jpeg")
$excludeBeta = $true  # Set to $false to include beta subfolders

# Guard: Ensure the JSON file exists
if (-not (Test-Path $jsonPath -PathType Leaf)) {
    Write-Error "JSON file missing: '$jsonPath' does not exist."
    exit
}

# Guard: Ensure the artist assets directory exists
if (-not (Test-Path $assetFolder -PathType Container)) {
    Write-Error "Asset directory missing: '$assetFolder' does not exist."
    exit
}

# 1. Load existing JSON data safely (compatible with PowerShell 5.1 & 7+)
$assetData = [ordered]@{}
$jsonContent = Get-Content -Raw -Path $jsonPath

if (-not [string]::IsNullOrWhiteSpace($jsonContent)) {
    $parsedJson = $jsonContent | ConvertFrom-Json

    if ($null -ne $parsedJson) {
        foreach ($prop in $parsedJson.psobject.Properties) {
            $assetData[$prop.Name] = $prop.Value
        }
    }
}

$hasUpdates = $false

# Get absolute path of root folder to compute relative paths accurately
$rootFullPath = (Get-Item $assetFolder).FullName

# 2. Get only .png, .jpg, and .jpeg files within artist_assets/
$files = Get-ChildItem -Path $assetFolder -Recurse -File -Include $extensions

foreach ($file in $files) {
    # Extract path relative to artist_assets directory
    $relativePath = $file.FullName.Substring($rootFullPath.Length).TrimStart('\', '/')
    $pathParts = $relativePath -split '[\\/]'

    # Skip if any parent folder is named "beta"
    if ($excludeBeta -and ($pathParts[0..($pathParts.Count - 2)] -contains "beta")) {
        continue
    }

    # Strip extension from filename
    $pathParts[-1] = [System.IO.Path]::GetFileNameWithoutExtension($pathParts[-1])

    # Join folder structure and filename with dots
    $key = $pathParts -join '.'

    # Only append if key is entirely absent
    if (-not $assetData.Contains($key)) {
        $assetData[$key] = $null
        $hasUpdates = $true
        Write-Host "Added missing key: $key" -ForegroundColor Green
    }
}

# 3. Save back to JSON only if new keys were added
if ($hasUpdates) {
    # Convert to JSON string
    $jsonOutput = $assetData | ConvertTo-Json -Depth 10

    # Unescape unnecessary \uXXXX sequences back to literal characters
    $jsonOutput = [regex]::Replace($jsonOutput, '\\u([0-9a-fA-F]{4})', {
            param($match)
            [char][int]"0x$($match.Groups[1].Value)"
        })

    [System.IO.File]::WriteAllText((Get-Item $jsonPath).FullName, $jsonOutput, [System.Text.Encoding]::UTF8)
    Write-Host "`nSuccessfully updated $jsonPath with missing keys." -ForegroundColor Cyan
}
else {
    Write-Host "`nNo missing PNG or JPG keys found. Existing JSON untouched." -ForegroundColor Yellow
}