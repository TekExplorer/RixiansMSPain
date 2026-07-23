# Configuration
$UsernamesPath = "HideDetailsMod/localization/eng/usernames.json"
$ArtistsPath = "HideDetailsMod/localization/eng/artists.json"

# Regex Patterns (Pre-escaped and case-sensitive)
$KeyIgnorePattern = "^\."       # Matches keys starting with a dot
$KeyBypassPattern = "\.TODO$"   # Matches keys ending strictly with uppercase .TODO
$ValueWrapPattern = "^\{.*\}$"  # Matches values wrapped fully in curly braces {}

# Guard: Ensure the JSON files exist
if (-not (Test-Path $UsernamesPath -PathType Leaf)) {
    Write-Error "JSON file missing: '$UsernamesPath' does not exist."
    exit 1
}
if (-not (Test-Path $ArtistsPath -PathType Leaf)) {
    Write-Error "JSON file missing: '$ArtistsPath' does not exist."
    exit 1
}

# 1. Load Usernames JSON data safely (compatible with PowerShell 5.1)
$usernamesData = [ordered]@{}
$usernamesContent = Get-Content -Raw -Path $UsernamesPath
if (-not [string]::IsNullOrWhiteSpace($usernamesContent)) {
    $parsedUsernames = $usernamesContent | ConvertFrom-Json
    if ($null -ne $parsedUsernames) {
        foreach ($prop in $parsedUsernames.psobject.Properties) {
            $usernamesData[$prop.Name] = $prop.Value
        }
    }
}

# 2. Load Artists JSON safely into an ordered lookup dictionary to preserve case matching
$artistsLookup = [ordered]@{}
$artistsContent = Get-Content -Raw -Path $ArtistsPath
if (-not [string]::IsNullOrWhiteSpace($artistsContent)) {
    $parsedArtists = $artistsContent | ConvertFrom-Json
    if ($null -ne $parsedArtists) {
        foreach ($prop in $parsedArtists.psobject.Properties) {
            $artistKey = $prop.Name
            $artistValue = $prop.Value

            # Regex Rule: Skip entry completely if key matches ignore pattern (e.g. starts with dot)
            if ($artistKey -cmatch $KeyIgnorePattern) { continue }

            # Regex Rule: Determine if key ends with bypass suffix (.TODO)
            $isBypassed = $artistKey -cmatch $KeyBypassPattern

            # Rule: Warn if key contains uppercase letters (unless bypassed)
            if ($artistKey -cmatch '[A-Z]' -and -not $isBypassed) {
                Write-Warning "Artist key '$artistKey' in artists.json contains uppercase letters."
            }

            # If it's a TODO item, skip processing its value completely
            if ($isBypassed) { continue }

            # Regex Rule: Skip if value matches wrap pattern (e.g. wrapped in {})
            if ($artistValue -cmatch $ValueWrapPattern) { continue }

            if (-not [string]::IsNullOrEmpty($artistValue)) {
                $artistsLookup[$artistValue] = $true
            }
        }
    }
}

$hasErrors = $false

# 3. Warn if any value in artists.json is not fully lowercase
foreach ($artist in $artistsLookup.Keys) {
    if ($artist -cmatch '[A-Z]') {
        Write-Warning "Artist value '$artist' in artists.json contains uppercase letters."
    }
}

# 4. Process each key-value pair in usernames.json
foreach ($key in $usernamesData.Keys) {
    $value = $usernamesData[$key]

    # Warn if any key in usernames.json is not fully lowercase
    if ($key -cmatch '[A-Z]') {
        Write-Warning "Username key '$key' in usernames.json contains uppercase letters."
    }

    # Match Check: Identical key-value pair found in usernames.json
    if ($key -ceq $value) {
        Write-Host "ERROR: Identical key-value pair found in usernames.json: key '$key' matches value '$value'." -ForegroundColor Red
        $hasErrors = $true
    }

    # Match Check: Key in usernames.json does not exactly match at least one value in artists.json
    if (-not $artistsLookup.Contains($key)) {
        Write-Host "ERROR: Extra key found: Username key '$key' does not match any entry in artists.json." -ForegroundColor Red
        $hasErrors = $true
    }
}

# Final evaluation
if ($hasErrors) {
    Write-Host "`nValidation FAILED with errors." -ForegroundColor Red
    exit 1
}
else {
    Write-Host "`nValidation PASSED successfully." -ForegroundColor Green
}
