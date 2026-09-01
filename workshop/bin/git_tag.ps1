param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production'
)

$ErrorActionPreference = 'Stop'

$GitHubRepoUrl = "https://github.com/TekExplorer/RixiansMSPain"

$binRoot = $PSScriptRoot
$workshopScriptsRoot = Split-Path -Parent $binRoot
$workspaceRoot = Split-Path -Parent $workshopScriptsRoot

$channelRoot = Join-Path $workspaceRoot "workshop\$Channel"
$contentRoot = Join-Path $channelRoot 'content'
$jsonPath = Join-Path $contentRoot 'HideDetailsMod.json'
$workshopJsonPath = Join-Path $channelRoot 'workshop.json'
$changelogPath = Join-Path $workspaceRoot 'CHANGELOG.md'

if (-not (Test-Path -LiteralPath $jsonPath)) {
    throw "HideDetailsMod.json not found at '$jsonPath'. Cannot determine version."
}

# 1. Parse Version
$manifest = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$version = $manifest.version
if (-not $version) { throw "Could not find 'version' property inside HideDetailsMod.json." }

if (-not $version.StartsWith('v')) { $version = "v$version" }
if ($Channel -eq 'Canary' -and -not $version.EndsWith('-canary')) { $version = "$version-canary" }

# 2. Check Uncommitted Files (excluding CHANGELOG.md)
$gitStatus = git status --porcelain | Where-Object { $_ -notmatch 'CHANGELOG\.md' }
if ($gitStatus) {
    Write-Warning "You have uncommitted changes in your repository!"
    $gitStatus | Out-String | Write-Host
    $confirm = Read-Host "Stop execution to review/commit? (Y/N)"
    if ($confirm -in @('Y', 'y', 'Yes', 'yes', '')) {
        throw "Pipeline stopped. Please commit your changes and try again."
    }
}

# 3. CHANGELOG.md Update (Skips if already present)
$currentDate = Get-Date -Format "yyyy-MM-dd"
$oldContent = if (Test-Path -LiteralPath $changelogPath) { Get-Content -LiteralPath $changelogPath -Raw } else { "" }

if ($oldContent -match "(?m)^##\s+\[$([regex]::Escape($version))\]") {
    Write-Host "[CHANGELOG] Notes for $version are already up-to-date. Skipping." -ForegroundColor Gray
}
elseif (Test-Path -LiteralPath $workshopJsonPath) {
    $workshopMetadata = Get-Content -LiteralPath $workshopJsonPath -Raw | ConvertFrom-Json
    $changeNote = $workshopMetadata.changeNote

    if ($changeNote) {
        Write-Host "[CHANGELOG] Prepending release notes for $version..." -ForegroundColor Cyan
        $formattedNotes = ($changeNote -split "\r?\n" | Where-Object { $_.Trim() -ne '' } | ForEach-Object {
                $line = $_.Trim()
                if (-not ($line.StartsWith('-') -or $line.StartsWith('*'))) { "- $line" } else { $line }
            }) -join "`n"

        $newEntry = "## [$version] - $currentDate`n`n$formattedNotes`n"

        $prevTag = (git tag --sort=-v:refname | Where-Object { $_ -ne $version -and $_ -notlike "*-canary" } | Select-Object -First 1)
        if (-not $prevTag) { $prevTag = (git tag --sort=-v:refname | Where-Object { $_ -ne $version } | Select-Object -First 1) }

        $urlRef = if ($prevTag) {
            "[$version]: $GitHubRepoUrl/compare/$($prevTag.Trim())...$version"
        }
        else {
            "[$version]: $GitHubRepoUrl/releases/tag/$version"
        }

        if ([string]::IsNullOrWhiteSpace($oldContent)) {
            $finalChangelog = "# Changelog`n`nAll notable changes to this project will be documented in this file.`n`n$newEntry`n$urlRef`n"
        }
        else {
            $parts = $oldContent -split "(?m)(?=^\[[^\]]+\]:\s*http)"
            $body = $parts[0].TrimEnd()
            $links = if ($parts.Count -gt 1) { ($parts[1..($parts.Count - 1)] -join "`n").Trim() } else { "" }

            if ($body -match "(?s)^(#\s+Changelog\r?\n+.*?\r?\n)(##\s+\[.*)") {
                $body = $matches[1] + "`n" + $newEntry + "`n" + $matches[2]
            }
            else {
                $body = "# Changelog`n`n$newEntry`n" + ($body -replace "^#\s+Changelog\r?\n*", "")
            }

            $finalChangelog = "$body`n`n" + ($links + "`n" + $urlRef).Trim() + "`n"
        }

        Set-Content -LiteralPath $changelogPath -Value $finalChangelog -Encoding utf8
        Write-Host "[CHANGELOG] CHANGELOG.md updated." -ForegroundColor Green
    }
}

# 4. Commit CHANGELOG.md if modified
$changelogDirty = git status --porcelain | Where-Object { $_ -match 'CHANGELOG\.md' }
if ($changelogDirty) {
    git add $changelogPath
    git commit -m "docs(changelog): update release notes for $version" --quiet
    Write-Host "[Git] Committed updated CHANGELOG.md." -ForegroundColor Green
}

# 5. Git Tag Verification & Push (Skips if HEAD is already tagged)
$tagCommit = (git rev-list -n 1 $version 2>$null)
$headCommit = (git rev-parse HEAD)

if ($tagCommit -eq $headCommit) {
    Write-Host "[Git] Tag '$version' already exists and matches HEAD. Skipping tag." -ForegroundColor Gray
}
elseif ($tagCommit) {
    Write-Warning "Tag '$version' exists on an older commit ($tagCommit) vs HEAD ($headCommit)."
    $force = Read-Host "FORCE move tag '$version' to current commit? (Y/N)"
    if ($force -in @('Y', 'y', 'Yes', 'yes')) {
        git tag -f -a $version -m "Release ($Channel): $version"
        git push origin $version -f
    }
}
else {
    Write-Host "[Git] Creating and pushing tag $version..." -ForegroundColor Green
    git tag -a $version -m "Release ($Channel): $version"
    git push origin $version
}

Write-Host "Pre-flight stage complete!" -ForegroundColor Green