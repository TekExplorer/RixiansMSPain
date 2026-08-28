param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production'
)

$ErrorActionPreference = 'Stop'

# Resolve absolute paths
$binRoot = $PSScriptRoot
$workshopRoot = Split-Path -Parent $binRoot
$workspaceRoot = Split-Path -Parent $workshopRoot
$channelRoot = Join-Path $workspaceRoot $Channel
$contentRoot = Join-Path $channelRoot 'content'

$jsonPath = Join-Path $contentRoot 'HideDetailsMod.json'

if (-not (Test-Path $jsonPath)) {
    throw "HideDetailsMod.json not found at '$jsonPath'. Cannot determine version."
}

# 1. Parse JSON Version
$manifest = Get-Content $jsonPath -Raw | ConvertFrom-Json
$version = $manifest.version

if (-not $version) {
    throw "Could not find 'version' property inside HideDetailsMod.json."
}

if (-not $version.StartsWith('v')) {
    $version = "v$version"
}

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
    return $true # Explicitly exit successfully to allow upload
}

# 4. Create and Push Tag
Write-Host "Creating and pushing new Git tag: $version"
git tag -a $version -m "Workshop Release ($Channel): $version"
git push origin $version

return $true
