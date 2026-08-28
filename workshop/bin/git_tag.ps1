param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production'
)

$ErrorActionPreference = 'Stop'

# Resolve absolute paths based on: workshop\bin\git_tag.ps1
$binRoot = $PSScriptRoot                         # workshop\bin
$workshopScriptsRoot = Split-Path -Parent $binRoot # workshop
$workspaceRoot = Split-Path -Parent $workshopScriptsRoot # workspace root

# Match path: workshop\{{channel}}\content\HideDetailsMod.json
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

# 3. Check if Local Tag Already Exists
$tagExists = git tag -l $version
if (-not $tagExists) {
    Write-Host "Creating and pushing new Git tag: $version"
    git tag -a $version -m "Workshop Release ($Channel): $version"
    git push origin $version
}
else {
    Write-Host "Tag '$version' already exists locally. Moving on to check GitHub Release..."
}

# 4. GitHub CLI Availability & Installation Check
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Warning "GitHub CLI ('gh') is not installed."
    $installGh = Read-Host "Would you like to install GitHub CLI now using winget? (Y/N)"
    
    if ($installGh -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Installing GitHub CLI via winget... Please wait."
        winget install --id GitHub.cli --silent --accept-source-agreements --accept-package-agreements
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Installation successful! Refreshing environment variables..."
            # Dynamically refresh PATH so the current session can find 'gh' without a restart
            $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
            
            if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
                Write-Warning "GitHub CLI was installed, but still cannot be found in PATH. Please restart your terminal later."
                return $true
            }
        }
        else {
            Write-Warning "winget failed to install GitHub CLI. Skipping GitHub Release flow."
            return $true
        }
    }
    else {
        Write-Host "Skipping GitHub CLI installation and Release creation."
        return $true
    }
}

# 5. GitHub Release Flow
Write-Host "GitHub CLI detected. Checking authentication status..."

# Verify the user is authenticated with GitHub
$authCheck = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning "GitHub CLI is not authenticated!"
    $loginGh = Read-Host "Would you like to log in to GitHub now? (Y/N)"
    if ($loginGh -in @('Y', 'y', 'Yes', 'yes')) {
        gh auth login
        # Re-check status after login attempt
        $authCheck = gh auth status 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Authentication failed or cancelled. Skipping release creation."
            return $true
        }
    }
    else {
        Write-Host "Skipping GitHub Release creation due to lack of authentication."
        return $true
    }
}

# Check if the release already exists on GitHub to make the script idempotent
$releaseExists = gh release view $version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "GitHub Release for '$version' already exists. Skipping release step."
    return $true
}

# Title and Configuration
$releaseTitle = "Release $version ($Channel)"
$isPrerelease = if ($Channel -eq 'Canary') { "--prerelease" } else { "" }

Write-Host "Creating GitHub Release draft for $version..."

# Create the release draft
if ($isPrerelease) {
    gh release create $version --draft --title $releaseTitle --generate-notes --prerelease
}
else {
    gh release create $version --draft --title $releaseTitle --generate-notes
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "Successfully created draft release: $releaseTitle"
    
    # Prompt to publish immediately
    $publishNow = Read-Host "Do you want to PUBLISH this release immediately? (Y/N)"
    if ($publishNow -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Publishing GitHub release..."
        gh release edit $version --draft=false
        Write-Host "Release is now public!"
    }
    else {
        Write-Host "Release left as a DRAFT. You can edit and publish it later on GitHub."
    }
}
else {
    Write-Warning "Failed to create GitHub release draft."
}

return $true
