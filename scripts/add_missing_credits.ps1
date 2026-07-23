param (
    [switch]$ValidateOnly
)

# Configuration
$assetFolder = "artist_assets"
$jsonPath = "HideDetailsMod/localization/eng/artists.json"
$extensions = @("*.png", "*.jpg", "*.jpeg")
$excludeBeta = $true  # Set to $false to include beta subfolders

# Guard: Ensure the JSON file exists
if (-not (Test-Path $jsonPath -PathType Leaf)) {
    Write-Error "JSON file missing: '$jsonPath' does not exist."
    exit 1
}

# Guard: Ensure the artist assets directory exists
if (-not (Test-Path $assetFolder -PathType Container)) {
    Write-Error "Asset directory missing: '$assetFolder' does not exist."
    exit 1
}

# 1. Load existing JSON data safely
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

$missingKeys = @()
$rootFullPath = (Get-Item $assetFolder).FullName

# 2. Scan image assets
$files = Get-ChildItem -Path $assetFolder -Recurse -File -Include $extensions
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($rootFullPath.Length).TrimStart('\', '/')
    $pathParts = $relativePath -split '[\\/]'

    if ($pathParts.Count -gt 1) {
        $parentFolders = $pathParts[0..($pathParts.Count - 2)]
        if ($excludeBeta -and ($parentFolders -contains "beta")) { continue }
    }

    $pathParts[-1] = [System.IO.Path]::GetFileNameWithoutExtension($pathParts[-1])
    $key = $pathParts -join '.'

    if (-not $assetData.Contains($key)) {
        $missingKeys += $key
        if (-not $ValidateOnly) {
            $assetData[$key] = $null
        }
    }
}

# 3. Handle Results
if ($missingKeys.Count -gt 0) {
    if ($ValidateOnly) {
        Write-Host "`n[VALIDATION FAILED] Found $($missingKeys.Count) missing keys in JSON:" -ForegroundColor Red
        foreach ($key in $missingKeys) {
            Write-Host "  - $key" -ForegroundColor Yellow
        }
        exit 1
    }
    else {
        $jsonOutput = $assetData | ConvertTo-Json -Depth 10
        $jsonOutput = [regex]::Replace($jsonOutput, '\\u([0-9a-fA-F]{4})', { param($match) [char][int]"0x$($match.Groups[1].Value)" })
        [System.IO.File]::WriteAllText((Get-Item $jsonPath).FullName, $jsonOutput, [System.Text.Encoding]::UTF8)
        Write-Host "`nSuccessfully updated $jsonPath with $($missingKeys.Count) missing keys." -ForegroundColor Cyan
    }
}
else {
    Write-Host "`n[SUCCESS] All image assets perfectly match the keys in $jsonPath." -ForegroundColor Green
}