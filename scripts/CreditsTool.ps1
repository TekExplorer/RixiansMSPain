[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateSet("Validate", "Fix")]
    [string]$Mode = "Validate",

    [Parameter(Mandatory = $false)]
    [ValidateSet("Canary", "Production")]
    [string]$Channel = "Production",

    [Parameter(Mandatory = $false)]
    [switch]$AssetsOnly,

    # TODO: make these items mandatory and put them in the tasks.json. Use ENV
    [Parameter(Mandatory = $false)]
    [string]$AssetFolder = "HideDetailsMod/artist_assets",
    
    [Parameter(Mandatory = $false)]
    [string]$UsernamesPath = "HideDetailsMod/localization/eng/usernames.json",
    
    [Parameter(Mandatory = $false)]
    [string]$ArtistsPath = "HideDetailsMod/localization/eng/artists.json"
)

# ==========================================
# HARDCODED CONSTANTS
# ==========================================
$Extensions = @("*.png", "*.jpg", "*.jpeg")
$ExcludeBeta = $true
$KeyIgnorePattern = "^\."
$KeyBypassPattern = "\.TODO$"
$ValueWrapPattern = "^\{.*\}$"
$ColorSuffix = ".color"

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
    $jsonOutput = [regex]::Replace($jsonOutput, '\\u([0-9a-fA-F]{4})', {
            param($match)
            [char][int]"0x$($match.Value.Substring(2))"
        })

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

function Test-ValidColorString ([string]$colorVal) {
    if ([string]::IsNullOrWhiteSpace($colorVal)) { return $false }
    $trimmed = $colorVal.Trim()

    # Gradient format: #start->#end
    if ($trimmed.Contains("->")) {
        $parts = $trimmed -split '->'
        if ($parts.Count -ne 2) { return $false }
        return (Test-ValidSingleColor $parts[0].Trim()) -and (Test-ValidSingleColor $parts[1].Trim())
    }

    return Test-ValidSingleColor $trimmed
}

function Test-ValidSingleColor ([string]$singleColor) {
    if ([string]::IsNullOrWhiteSpace($singleColor)) { return $false }
    # Hex match (#RGB, #RGBA, #RRGGBB, #RRGGBBAA) or valid named identifier
    if ($singleColor -match '^#([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$') { return $true }
    if ($singleColor -match '^[a-zA-Z_]+$') { return $true }
    return $false
}

# ==========================================
# PIPELINE STEP FUNCTIONS
# ==========================================
function Sync-ImageAssets ([System.Collections.Specialized.OrderedDictionary]$artistsData) {
    $missingKeys = @()
    $nullKeys = @()

    # 1. Audit pre-existing keys in artists.json for null/empty values
    foreach ($artistKey in $artistsData.Keys) {
        if ($artistKey -cmatch $KeyIgnorePattern) { continue }
        $val = $artistsData[$artistKey]
        if ($null -eq $val -or [string]::IsNullOrWhiteSpace([string]$val)) {
            $nullKeys += $artistKey
        }
    }

    # 2. Scan physical image files in the assets directory
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

    # 3. Output reports based on current Mode
    if ($Mode -eq "Validate") {
        if ($missingKeys.Count -gt 0) {
            Write-Host "[CREDITS FAIL] Found $($missingKeys.Count) missing asset key(s) in artists.json:" -ForegroundColor Red
            foreach ($key in $missingKeys) { Write-Host "  -> Missing key: $key" -ForegroundColor Yellow }
            $Global:HasValidationErrors = $true
        }

        if ($nullKeys.Count -gt 0) {
            Write-Host "[CREDITS FAIL] Found $($nullKeys.Count) null/unassigned value(s) in artists.json:" -ForegroundColor Red
            foreach ($key in $nullKeys) { Write-Host "  -> Null value at key: $key" -ForegroundColor Red }
            $Global:HasValidationErrors = $true
        }

        if ($missingKeys.Count -eq 0 -and $nullKeys.Count -eq 0) {
            Write-Host "[CREDITS OK] Asset files match artists.json keys perfectly with no null values." -ForegroundColor Green
        }
    }
    else {
        # Fix Mode
        if ($missingKeys.Count -gt 0) {
            Write-Host "Credits Fix: Added $($missingKeys.Count) missing asset key(s) to artists.json (set to null)." -ForegroundColor Cyan
            foreach ($key in $missingKeys) { Write-Host "  -> Added key: $key" -ForegroundColor Cyan }
        }

        if ($nullKeys.Count -gt 0) {
            Write-Host "Credits Fix Info: Found $($nullKeys.Count) pre-existing key(s) set to null in artists.json:" -ForegroundColor Cyan
            foreach ($key in $nullKeys) { Write-Host "  -> Existing null key: $key" -ForegroundColor Cyan }
        }

        if ($missingKeys.Count -eq 0 -and $nullKeys.Count -eq 0) {
            Write-Host "[CREDITS OK] Asset files match artists.json keys perfectly with no null values." -ForegroundColor Green
        }
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

        if (-not [string]::IsNullOrWhiteSpace([string]$artistValue)) {
            # Split pipe-delimited multiple authors (e.g. "tekexplorer|person2")
            $authors = [string]$artistValue -split '\|'
            foreach ($rawAuthor in $authors) {
                $author = $rawAuthor.Trim()
                if (-not [string]::IsNullOrWhiteSpace($author)) {
                    $lookup[$author] = $true
                }
            }
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

        if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) {
            if ($Mode -eq "Validate") {
                Write-Host "[CREDITS FAIL] Null or empty value found in usernames.json for key '$key'." -ForegroundColor Red
                $Global:HasValidationErrors = $true
            }
            else {
                Write-Host "Credits Fix Info: Null or empty value currently in usernames.json for key '$key'." -ForegroundColor Cyan
            }
            continue
        }

        # 1. Handle Companion Color Override keys (e.g., "tekexplorer.color")
        if ($key.EndsWith($ColorSuffix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $baseName = $key.Substring(0, $key.Length - $ColorSuffix.Length)
            
            # Warn if base author isn't referenced anywhere in artists.json
            if (-not $artistsLookup.Contains($baseName)) {
                Write-Host "[CREDITS FAIL] Orphan color key: '$key' exists in usernames.json, but '$baseName' is never used in artists.json." -ForegroundColor Red
                $Global:HasValidationErrors = $true
            }

            # Validate color syntax (#hex, named, or #start->#end)
            if (-not (Test-ValidColorString ([string]$value))) {
                Write-Host "[CREDITS FAIL] Invalid color/gradient syntax in usernames.json at '$key': '$value'." -ForegroundColor Red
                $Global:HasValidationErrors = $true
            }
            continue
        }

        # 2. Standard Username Key Validation
        if ($key -cmatch '[A-Z]') {
            Write-Warning "Credits: Username key '$key' in usernames.json contains uppercase letters."
        }

        # Redundant entry check: key equals value without any custom BBCode formatting
        if ($key -ceq $value -and -not ($value -match '\[.+\]')) {
            # If the user has a companion .color key, keeping the base key might be unnecessary unless intentional,
            # but if it's strictly identical, prune in Fix mode.
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

        # Extra unreferenced key check
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

# Step 1: Manage asset file footprints & check for null values
$artistsData = Get-JsonSafe -path $ArtistsPath
Sync-ImageAssets -artistsData $artistsData

if ($AssetsOnly) {
    if ($Mode -eq "Fix" -and $Global:ArtistFileModified) {
        Set-JsonSafe -path $ArtistsPath -data $artistsData
    }

    if ($Mode -eq "Validate" -and $Global:HasValidationErrors -and $Channel -eq "Production") {
        exit 1
    }
    exit 0
}

# Step 2: Extract mapping rules and sanitize files
$artistsLookup = Get-ArtistsLookup -artistsData $artistsData
$usernamesData = Get-JsonSafe -path $UsernamesPath

Test-AndFixUsernames -usernamesData $usernamesData -artistsLookup $artistsLookup
Save-ChangesIfNeeded -artistsData $artistsData -usernamesData $usernamesData