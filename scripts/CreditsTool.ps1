[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateSet("Validate", "Fix")]
    [string]$Mode = "Validate",

    [Parameter(Mandatory = $false)]
    [ValidateSet("Canary", "Production")]
    [string]$Channel = "Production",

    [Parameter(Mandatory = $false)]
    [switch]$AssetsOnly
)

# ==========================================
# HARDCODED CONSTANTS & DIRECTORIES
# ==========================================
$AssetFolder = "artist_assets"
$UsernamesPath = "HideDetailsMod/localization/eng/usernames.json"
$ArtistsPath = "HideDetailsMod/localization/eng/artists.json"

$Extensions = @("*.png", "*.jpg", "*.jpeg")
$ExcludeBeta = $true
$KeyIgnorePattern = "^\."
$KeyBypassPattern = "\.TODO$"
$ValueWrapPattern = "^\{.*\}$"

# Global pipeline tracking flags
$Global:HasValidationErrors = $false
$Global:ArtistFileModified = $false
$Global:UsernameFileModified = $false

# ==========================================
# HELPER FUNCTIONS
# ==========================================
function Get-JsonSafe ([string]$path) {
    $data = [ordered]@{}
    if (Test-Path $path -PathType Leaf) {
        $content = Get-Content -Raw -Path $path
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $parsed = $content | ConvertFrom-Json
            if ($null -ne $parsed) {
                foreach ($prop in $parsed.psobject.Properties) {
                    $data[$prop.Name] = $prop.Value
                }
            }
        }
    }
    return $data
}

function Set-JsonSafe ([string]$path, [System.Collections.Specialized.OrderedDictionary]$data) {
    $jsonOutput = $data | ConvertTo-Json -Depth 10
    $jsonOutput = [regex]::Replace($jsonOutput, '\\u([0-9a-fA-F]{4})', { param($match) [char][int]"0x$($match.Groups.Value)" })
    [System.IO.File]::WriteAllText((Get-Item $path).FullName, $jsonOutput, [System.Text.Encoding]::UTF8)
}

function Assert-Environment {
    if (-not $AssetsOnly) {
        $RequiredPaths = @($UsernamesPath, $ArtistsPath)
        foreach ($path in $RequiredPaths) {
            if (-not (Test-Path $path -PathType Leaf)) {
                Write-Error "Required JSON file missing: '$path' does not exist."
                exit 1
            }
        }
    }
    if (-not (Test-Path $AssetFolder -PathType Container)) {
        Write-Error "Asset directory missing: '$AssetFolder' does not exist."
        exit 1
    }
}

# ==========================================
# PIPELINE STEP FUNCTIONS
# ==========================================
function Sync-ImageAssets ([System.Collections.Specialized.OrderedDictionary]$artistsData) {
    $missingKeys = @()
    $rootFullPath = (Get-Item $AssetFolder).FullName
    $files = Get-ChildItem -Path $AssetFolder -Recurse -File -Include $Extensions

    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($rootFullPath.Length).TrimStart('\', '/')
        $pathParts = $relativePath -split '[\\/]'

        if ($pathParts.Count -gt 1) {
            $parentFolders = $pathParts[0..($pathParts.Count - 2)]
            if ($ExcludeBeta -and ($parentFolders -contains "beta")) { continue }
        }

        $pathParts[-1] = [System.IO.Path]::GetFileNameWithoutExtension($pathParts[-1])
        $key = $pathParts -join '.'

        if (-not $artistsData.Contains($key)) {
            $missingKeys += $key
            if ($Mode -eq "Fix") {
                $artistsData[$key] = $null
                $Global:ArtistFileModified = $true
            }
        }
    }

    if ($missingKeys.Count -gt 0) {
        if ($Mode -eq "Validate") {
            Write-Host "[CREDITS FAIL] Found $($missingKeys.Count) missing asset keys in artists.json:" -ForegroundColor Red
            foreach ($key in $missingKeys) { Write-Host "  -> Missing: $key" -ForegroundColor Yellow }
            $Global:HasValidationErrors = $true
        }
        else {
            Write-Host "Credits Fix: Added $($missingKeys.Count) missing asset keys to artists.json." -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "[CREDITS OK] Asset files match artists.json keys perfectly." -ForegroundColor Green
    }
}

function Get-ArtistsLookup ([System.Collections.Specialized.OrderedDictionary]$artistsData) {
    $lookup = [ordered]@{}

    foreach ($artistKey in $artistsData.Keys) {
        $artistValue = $artistsData[$artistKey]

        if ($artistKey -cmatch $KeyIgnorePattern) { continue }
        $isBypassed = $artistKey -cmatch $KeyBypassPattern

        if ($artistKey -cmatch '[A-Z]' -and -not $isBypassed) {
            Write-Warning "Credits: Artist key '$artistKey' in artists.json contains uppercase letters."
        }
        if ($isBypassed -or ($artistValue -cmatch $ValueWrapPattern)) { continue }

        if (-not [string]::IsNullOrEmpty($artistValue)) {
            $lookup[$artistValue] = $true
        }
    }

    foreach ($artist in $lookup.Keys) {
        if ($artist -cmatch '[A-Z]') {
            Write-Warning "Credits: Artist value '$artist' in artists.json contains uppercase letters."
        }
    }
    return $lookup
}

function Test-AndFixUsernames ([System.Collections.Specialized.OrderedDictionary]$usernamesData, [System.Collections.Specialized.OrderedDictionary]$artistsLookup) {
    $usernamesKeys = @($usernamesData.Keys)
    foreach ($key in $usernamesKeys) {
        $value = $usernamesData[$key]

        if ($key -cmatch '[A-Z]') {
            Write-Warning "Credits: Username key '$key' in usernames.json contains uppercase letters."
        }

        if ($key -ceq $value) {
            if ($Mode -eq "Fix") {
                $usernamesData.Remove($key)
                $Global:UsernameFileModified = $true
                Write-Host "Credits Fix: Removed identical key-value entry '$key' from usernames.json." -ForegroundColor Cyan
            }
            else {
                Write-Host "[CREDITS FAIL] Identical key-value pair found in usernames.json: key '$key' matches value '$value'." -ForegroundColor Red
                $Global:HasValidationErrors = $true
            }
            continue
        }

        if (-not $artistsLookup.Contains($key)) {
            Write-Host "[CREDITS FAIL] Extra entry: Username key '$key' does not match any artist value in artists.json." -ForegroundColor Red
            $Global:HasValidationErrors = $true
        }
    }
}

function Save-ChangesIfNeeded ([System.Collections.Specialized.OrderedDictionary]$artistsData, [System.Collections.Specialized.OrderedDictionary]$usernamesData) {
    if ($Mode -eq "Fix") {
        if ($Global:ArtistFileModified) {
            Set-JsonSafe -path $ArtistsPath -data $artistsData
            Write-Host "Updated artists.json successfully." -ForegroundColor Green
        }
        if ($Global:UsernameFileModified) {
            Set-JsonSafe -path $UsernamesPath -data $usernamesData
            Write-Host "Updated usernames.json successfully." -ForegroundColor Green
        }
        Write-Host "`nCredits fix pipeline pass complete." -ForegroundColor Green
    }
    else {
        if ($Global:HasValidationErrors) {
            if ($Channel -eq "Production") {
                Write-Host "`n[CRITICAL] Credits validation FAILED for Production deployment." -ForegroundColor Red
                exit 1
            }
            else {
                Write-Host "`n[WARNING] Credits validation FAILED. Proceeding anyway because target is Canary." -ForegroundColor Yellow
                exit 0
            }
        }
        else {
            Write-Host "`nCredits validation PASSED successfully." -ForegroundColor Green
        }
    }
}

# ==========================================
# EXECUTION LIFECYCLE
# ==========================================
Assert-Environment

# Step 1: Manage asset file footprints
$artistsData = Get-JsonSafe -path $ArtistsPath
Sync-ImageAssets -artistsData $artistsData

if ($AssetsOnly) {
    if ($Global:ArtistFileModified) { Set-JsonSafe -path $ArtistsPath -data $artistsData }

    if ($Global:HasValidationErrors -and $Channel -eq "Production") {
        exit 1
    }
    exit 0
}

# Step 2: Extract mapping rules and sanitize files
$artistsLookup = Get-ArtistsLookup -artistsData $artistsData
$usernamesData = Get-JsonSafe -path $UsernamesPath

Test-AndFixUsernames -usernamesData $usernamesData -artistsLookup $artistsLookup
Save-ChangesIfNeeded -artistsData $artistsData -usernamesData $usernamesData