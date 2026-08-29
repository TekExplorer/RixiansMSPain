param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production'
)

$ErrorActionPreference = 'Stop'

# Resolve absolute paths based on: workshop\bin\git_tag.ps1
$binRoot = $PSScriptRoot                         # workshop\bin
$workshopScriptsRoot = Split-Path -Parent $binRoot # workshop
$workspaceRoot = Split-Path -Parent $workshopScriptsRoot # workspace root

# Match path: workshop\{{channel}}\content\
$channelRoot = Join-Path $workspaceRoot "workshop\$Channel"
$contentRoot = Join-Path $channelRoot 'content'
$jsonPath = Join-Path $contentRoot 'HideDetailsMod.json'
$workshopJsonPath = Join-Path $channelRoot 'workshop.json'
$changelogPath = Join-Path $workspaceRoot 'CHANGELOG.md'

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

# 3. Check and Handle Existing Tags with Force Option
$tagExists = git tag -l $version
$shouldTag = $true

if ($tagExists) {
    Write-Warning "Tag '$version' already exists locally."
    $forceTag = Read-Host "Would you like to FORCE reset this tag to your current commit? (Y/N)"
    
    if ($forceTag -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Overwriting and pushing updated Git tag: $version"
        git tag -f -a $version -m "Workshop Release ($Channel): $version"
        git push origin $version -f
    }
    else {
        Write-Host "Keeping existing tag. Moving on to check GitHub Release..."
        $shouldTag = $false
    }
}
else {
    Write-Host "Creating and pushing new Git tag: $version"
    git tag -a $version -m "Workshop Release ($Channel): $version"
    git push origin $version
}

# --- NEW: Automated CHANGELOG.md Generation Flow ---
Write-Host "Updating local CHANGELOG.md..."
$currentDate = Get-Date -Format "yyyy-MM-dd"
$newChangelogEntry = ""

if (Test-Path $workshopJsonPath) {
    $workshopMetadata = Get-Content $workshopJsonPath -Raw | ConvertFrom-Json
    $changeNote = $workshopMetadata.changeNote

    if ($changeNote) {
        # Format multi-line change notes into clean Markdown bullet points
        $formattedNotes = ""
        $changeNote -split "\r?\n" | ForEach-Object {
            $line = $_.Trim()
            if ($line) {
                # Ensure each line starts cleanly with a Markdown bullet point
                if (-not ($line.StartsWith('-') -or $line.StartsWith('*'))) {
                    $line = "- $line"
                }
                $formattedNotes += "$line`n"
            }
        }

        # Build the fresh Markdown header blocks
        $newChangelogEntry = "## [$version] - $currentDate`n`n$formattedNotes`n"
    }
}

if ($newChangelogEntry) {
    # If a changelog file exists, prepend the new text to the top of it safely
    if (Test-Path $changelogPath) {
        $existingContent = Get-Content $changelogPath -Raw
        
        # Check to see if we've already written notes for this specific version entry to avoid stacking duplicates
        if (-not ($existingContent -match "## \[$version\]")) {
            $updatedContent = $newChangelogEntry + $existingContent
            Set-Content $changelogPath -Value $updatedContent
            Write-Host "Prepended new release notes to CHANGELOG.md."
        }
        else {
            Write-Host "Notes for version $version already exist in CHANGELOG.md. Skipping file prepend."
        }
    }
    else {
        # Fallback if no file exists yet: build a brand new one
        $initialContent = "# Changelog`n`nAll notable changes to this project will be documented in this file.`n`n" + $newChangelogEntry
        Set-Content $changelogPath -Value $initialContent
        Write-Host "Created a brand new CHANGELOG.md."
    }
}
# --- End Changelog Flow ---

# 4. GitHub CLI Availability & Installation Check
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Warning "GitHub CLI ('gh') is not installed."
    $installGh = Read-Host "Would you like to install GitHub CLI now using winget? (Y/N)"
    
    if ($installGh -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Installing GitHub CLI via winget... Please wait."
        winget install --id GitHub.cli --silent --accept-source-agreements --accept-package-agreements
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Installation successful! Refreshing environment variables..."
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

# Safely check if the release already exists on GitHub to make the script idempotent
$releaseExists = $false
try {
    $null = gh release view $version --json url 2>$null
    if ($LASTEXITCODE -eq 0) {
        $releaseExists = $true
    }
}
catch {
    $releaseExists = $false
}

if ($releaseExists) {
    if ($shouldTag -and $forceTag -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Existing GitHub release found. Deleting old release to replace it..."
        gh release delete $version --yes
    }
    else {
        Write-Host "GitHub Release for '$version' already exists. Skipping release step."
        return $true
    }
}

# Title and Configuration
$releaseTitle = "Release $version ($Channel)"
$isPrerelease = if ($Channel -eq 'Canary') { "--prerelease" } else { "" }

# Create Zip Archive of the Content Folder
$zipPath = Join-Path $channelRoot "HideDetailsMod-$version.zip"
Write-Host "Zipping workshop content folder to $zipPath..."

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $contentRoot '*') -DestinationPath $zipPath -Force

Write-Host "Creating GitHub Release draft for $version with zipped contents..."

# Create the release draft and upload the zip asset simultaneously
if ($isPrerelease) {
    gh release create $version $zipPath --draft --title $releaseTitle --generate-notes --prerelease
}
else {
    gh release create $version $zipPath --draft --title $releaseTitle --generate-notes
}

# Clean up the local temp zip file after uploading it to GitHub
if (Test-Path $zipPath) {
    Write-Host "Cleaning up local archive..."
    Remove-Item $zipPath -Force
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
