# Configuration
$assetFolder = "artist_assets"
$jsonPath = "HideDetailsMod/localization/eng/artists.json"
$extensions = @("*.png", "*.jpg", "*.jpeg")
$excludeBeta = $true  # Set to $false to include beta subfolders
$validateOnly = $true  # Set to $true to fail/warn instead of writing to disk

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

$missingKeys = @()
$rootFullPath = (Get-Item $assetFolder).FullName

# 2. Get only .png, .jpg, and .jpeg files within artist_assets/
$files = Get-ChildItem -Path $assetFolder -Recurse -File -Include $extensions
foreach ($file in $files) {
    # Extract path relative to artist_assets directory
    $relativePath = $file.FullName.Substring($rootFullPath.Length).TrimStart('\', '/')
    $pathParts = $relativePath -split '[\\/]'

    # Skip if any parent folder is named "beta" (safeguarded against root-level files)
    if ($pathParts.Count -gt 1) {
        $parentFolders = $pathParts[0..($pathParts.Count - 2)]
        if ($excludeBeta -and ($parentFolders -contains "beta")) { continue }
    }

    # Strip extension from filename
    $pathParts[-1] = [System.IO.Path]::GetFileNameWithoutExtension($pathParts[-1])

    # Join folder structure and filename with dots
    $key = $pathParts -join '.'

    # Check if key is missing
    if (-not $assetData.Contains($key)) {
        $missingKeys += $key
        if (-not $validateOnly) {
            $assetData[$key] = $null
        }
    }
}

# 3. Handle Results based on Mode
if ($missingKeys.Count -gt 0) {
    if ($validateOnly) {
        Write-Host "`n[VALIDATION FAILED] Found $($missingKeys.Count) missing keys in JSON:" -ForegroundColor Red
        foreach ($key in $missingKeys) {
            Write-Host "  - $key" -ForegroundColor Yellow
        }
        Write-Host "`nValidation failed. Run with `$validateOnly = `$false to automatically add these keys." -ForegroundColor Red
        exit 1  # Exit with error code for CI/CD pipelines
    }
    else {
        # Convert to JSON string
        $jsonOutput = $assetData | ConvertTo-Json -Depth 10

        # Unescape unnecessary \uXXXX sequences back to literal characters
        $jsonOutput = [regex]::Replace($jsonOutput, '\\u([0-9a-fA-F]{4})', { param($match) [char][int]"0x$($match.Groups[1].Value)" })

        [System.IO.File]::WriteAllText((Get-Item $jsonPath).FullName, $jsonOutput, [System.Text.Encoding]::UTF8)
        Write-Host "`nSuccessfully updated $jsonPath with $($missingKeys.Count) missing keys." -ForegroundColor Cyan
    }
}
else {
    Write-Host "`n[SUCCESS] All image assets perfectly match the keys in $jsonPath." -ForegroundColor Green
}