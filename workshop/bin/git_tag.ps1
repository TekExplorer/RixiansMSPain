param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production'
)

$ErrorActionPreference = 'Stop'

# Map absolute path structure using the workspace root
$binRoot = $PSScriptRoot
$workshopScriptsRoot = Split-Path -Parent $binRoot
$workspaceRoot = Split-Path -Parent $workshopScriptsRoot

# Match your specific structure: workshop\{{channel}}\content\HideDetailsMod.json
$channelRoot = Join-Path $workspaceRoot "workshop\$Channel"
$jsonPath = Join-Path $channelRoot 'content\HideDetailsMod.json'

if (-not (Test-Path $jsonPath)) {
    throw "HideDetailsMod.json not found at expected path: '$jsonPath'. Cannot determine version."
}

# 1. Parse JSON Version
$manifest = Get-Content $jsonPath -Raw | ConvertFrom-Json
$version = $manifest.version

if (-not $version) {
    throw "Could not find 'version' property inside HideDetailsMod.json."
}

# Format safety for the tag prefix
if (-not $version.StartsWith('v')) {
    $version = "v$version"
}

# Append the channel configuration if Canary to protect production spaces
if ($Channel -eq 'Canary') {
    $version = "$version-canary"
}

# 2. Check for Uncommitted Changes
Write-Host "Checking local Git repository status..."
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Warning "You have uncommitted changes in your repository!"
    $gitStatus | Out-String | Write-Host
    
    $confirmUpdate = Read-Host "Did you forget to update the version and commit? Stop execution? (Y/N)"
    if ($confirmUpdate -in @('Y', 'y', 'Yes', 'yes', '')) {
        throw "Pipeline stopped. Please bump the version in HideDetailsMod.json, commit, and try again."
    }
}

# 3. Check if Tag Already Exists
$tagExists = git tag -l $version
if ($tagExists) {
    Write-Host "Tag '$version' already exists. Skipping Git tagging step."
    return $true
}

# 4. Create and Push Tag
Write-Host "Creating and pushing new Git tag: $version"
git tag -a $version -m "Workshop Release ($Channel): $version"
git push origin $version

return $true
