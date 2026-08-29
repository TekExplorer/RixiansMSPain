param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production'
)

$ErrorActionPreference = 'Stop'

# --- CONFIGURATION: REPOSITORY BASE PROPERTIES ---
$GitHubRepoUrl = "https://github.com/TekExplorer/RixiansMSPain"

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
if (-not $version) { throw "Could not find 'version' property inside HideDetailsMod.json." }

if (-not $version.StartsWith('v')) { $version = "v$version" }
if ($Channel -eq 'Canary' -and -not $version.EndsWith('-canary')) { $version = "$version-canary" }

# 2. Check for Uncommitted Changes
Write-Host "Checking local Git repository status..." -ForegroundColor Cyan
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Warning "You have uncommitted changes in your repository!"
    $gitStatus | Out-String | Write-Host
    
    $confirmUpdate = Read-Host "Did you forget to update the version and commit? Stop execution? (Y/N)"
    if ($confirmUpdate -in @('Y', 'y', 'Yes', 'yes', '')) {
        throw "Pipeline stopped. Please commit your active fields and try again."
    }
}

# 3. Check and Handle Existing Tags with Force Option
$tagExists = git tag -l $version
$shouldTag = $true
$forceTag = 'no'

if ($tagExists) {
    Write-Warning "Tag '$version' already exists locally."
    $forceTag = Read-Host "Would you like to FORCE reset this tag to your current commit? (Y/N)"
    
    if ($forceTag -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Overwriting and pushing updated Git tag: $version" -ForegroundColor Yellow
        git tag -f -a $version -m "Workshop Release ($Channel): $version"
        git push origin $version -f
    }
    else {
        Write-Host "Keeping existing tag. Moving on to check GitHub Release..." -ForegroundColor Gray
        $shouldTag = $false
    }
}
else {
    Write-Host "Creating and pushing new Git tag: $version" -ForegroundColor Green
    git tag -a $version -m "Workshop Release ($Channel): $version"
    git push origin $version
}

# --- 4. REFERENCE-STYLE CHANGELOG GENERATION & EXTRACTION FLOW ---
Write-Host "Updating local CHANGELOG.md..." -ForegroundColor Cyan
$currentDate = Get-Date -Format "yyyy-MM-dd"
$releaseDescriptionText = ""

if (Test-Path $workshopJsonPath) {
    $workshopMetadata = Get-Content $workshopJsonPath -Raw | ConvertFrom-Json
    $changeNote = $workshopMetadata.changeNote

    if ($changeNote) {
        $formattedNotes = ""
        $changeNote -split "\r?\n" | ForEach-Object {
            $line = $_.Trim()
            if ($line) {
                if (-not ($line.StartsWith('-') -or $line.StartsWith('*'))) { $line = "- $line" }
                $formattedNotes += "$line`n"
            }
        }

        # Extracted notes body payload for GitHub Release Notes usage
        $releaseDescriptionText = $formattedNotes
        $newChangelogEntry = "## [$version] - $currentDate`n`n$formattedNotes"

        $oldContent = ""
        if (Test-Path $changelogPath) { $oldContent = Get-Content $changelogPath -Raw }

        if ($oldContent -notmatch "## \[$version\]") {
            $prevTag = (git describe --tags --abbrev=0 "${version}^" 2>$null)
            if ($null -eq $prevTag) { $prevTag = (git tag --sort=-v:refname | Select-Object -Skip 1 -First 1) }

            $urlReferenceDefinition = "[$version]: $GitHubRepoUrl/releases/tag/$version"
            $diffRangeUrl = "$GitHubRepoUrl/releases/tag/$version"
            if ($prevTag) {
                $prevTagClean = $prevTag.Trim()
                $urlReferenceDefinition = "[$version]: $GitHubRepoUrl/compare/$prevTagClean...$version"
                $diffRangeUrl = "$GitHubRepoUrl/compare/$prevTagClean...$version"
            }

            # Inject localized compare link directly into the specific description blob
            $releaseDescriptionText += "`n### Git Timeline Diff`nSee complete code changes: [$prevTagClean...$version]($diffRangeUrl)`n"

            if ([string]::IsNullOrEmpty($oldContent)) {
                $initialContent = "# Changelog`n`nAll notable changes to this project will be documented in this file.`n`n" + $newChangelogEntry + "`n`n" + $urlReferenceDefinition
                Set-Content $changelogPath -Value $initialContent -Encoding utf8
                Write-Host "Created a brand new reference-style CHANGELOG.md." -ForegroundColor Green
            }
            else {
                $splitContent = $oldContent -split "(?=\r?\n\[v\d)"
                $mainTextSections = $splitContent
                $footerLinksDefinitions = ""
                if ($splitContent.Count -gt 1) { $footerLinksDefinitions = ($splitContent[1..($splitContent.Count - 1)] -join "") }

                $updatedContent = ($mainTextSections -replace "(# Changelog\r?\n\r?All.*\r?\n\r?)", "`$1$newChangelogEntry`n") + "`n" + $footerLinksDefinitions.TrimEnd() + "`n" + $urlReferenceDefinition
                Set-Content $changelogPath -Value $updatedContent -Encoding utf8
                Write-Host "Prepended release notes and updated footer references." -ForegroundColor Green
            }
        }
        else {
            Write-Host "Notes for version $version already exist in CHANGELOG.md. Prepend skipped." -ForegroundColor Gray
            
            # If skipping prepend, extract the previous tag range anyway to populate descriptions gracefully
            $prevTag = (git describe --tags --abbrev=0 "${version}^" 2>$null)
            if ($prevTag) {
                $diffRangeUrl = "$GitHubRepoUrl/compare/$($prevTag.Trim())...$version"
                $releaseDescriptionText += "`n### Git Timeline Diff`nSee complete code changes: [$($prevTag.Trim())...$version]($diffRangeUrl)`n"
            }
        }
    }
}

# 5. GitHub CLI Availability & Installation Check
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Warning "GitHub CLI ('gh') is not installed. Skipping release flow."
    return $true
}

# 6. GitHub Release Flow
Write-Host "GitHub CLI detected. Checking authentication status..." -ForegroundColor Cyan
$authCheck = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning "GitHub CLI is not authenticated! Skipping release creation."
    return $true
}

$releaseExists = $false
try {
    $null = gh release view $version --json url 2>$null
    if ($LASTEXITCODE -eq 0) { $releaseExists = $true }
}
catch { $releaseExists = $false }

if ($releaseExists) {
    if ($shouldTag -and $forceTag -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Deleting old GitHub release to replace it..." -ForegroundColor Yellow
        gh release delete $version --yes
    }
    else {
        Write-Host "GitHub Release for '$version' already exists. Skipping release step." -ForegroundColor Gray
        return $true
    }
}

# Create Zip Archive of the Content Folder
$zipPath = Join-Path $channelRoot "HideDetailsMod-$version.zip"
Write-Host "Zipping workshop content folder to $zipPath..." -ForegroundColor Cyan

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $contentRoot '*') -DestinationPath $zipPath -Force

# --- 7. FIXED: DUMP EXTRACTED LOG TEXT TO TEMPORARY GENERATION VECTOR ---
$tempNotesPath = [System.IO.Path]::GetTempFileName()
$releaseDescriptionText | Set-Content -Path $tempNotesPath -Encoding utf8

# $releaseTitle = "Release $version ($Channel)"
$releaseTitle = "$version"

$isPrerelease = if ($Channel -eq 'Canary') { $true } else { $false }

Write-Host "Creating GitHub Release draft for $version with localized changenotes..." -ForegroundColor Green

if ($isPrerelease) {
    gh release create $version $zipPath --draft --title $releaseTitle --notes-file $tempNotesPath --prerelease
}
else {
    gh release create $version $zipPath --draft --title $releaseTitle --notes-file $tempNotesPath
}

# Clean up temporary generation trackers safely
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (Test-Path $tempNotesPath) { Remove-Item $tempNotesPath -Force }

if ($LASTEXITCODE -eq 0) {
    Write-Host "Successfully created draft release: $releaseTitle" -ForegroundColor Green
    
    $publishNow = Read-Host "Do you want to PUBLISH this release immediately? (Y/N)"
    if ($publishNow -in @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Publishing GitHub release..." -ForegroundColor Cyan
        gh release edit $version --draft=false
        Write-Host "Release is now public!" -ForegroundColor Green
    }
}
else {
    Write-Warning "Failed to create GitHub release draft."
}

return $true
