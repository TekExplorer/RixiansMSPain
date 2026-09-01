param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production',

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

if ($DryRun) {
    Write-Host "`n================ [DRY RUN MODE ENABLED] ================" -ForegroundColor Magenta
}

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

# 1. Parse Version Robustly
$manifest = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
[string]$rawVersion = if ($manifest.version) { $manifest.version } elseif ($manifest.Version) { $manifest.Version } else { $null }

if ([string]::IsNullOrWhiteSpace($rawVersion)) {
    throw "Could not find a valid 'version' property in '$jsonPath'."
}

$version = $rawVersion.Trim()
if (-not $version.StartsWith('v')) { $version = "v$version" }
if ($Channel -eq 'Canary' -and -not $version.EndsWith('-canary')) { $version = "$version-canary" }

Write-Host "Target version resolved: '$version' for channel: '$Channel'" -ForegroundColor Cyan

# 2. Evaluate CHANGELOG.md
$currentDate = Get-Date -Format "yyyy-MM-dd"
$oldContent = if (Test-Path -LiteralPath $changelogPath) { Get-Content -LiteralPath $changelogPath -Raw } else { "" }

if ($oldContent -match "(?m)^##\s+\[$([regex]::Escape($version))\]") {
    Write-Host "[CHANGELOG] Notes for $version already present. Would skip." -ForegroundColor Gray
}
elseif (Test-Path -LiteralPath $workshopJsonPath) {
    $workshopMetadata = Get-Content -LiteralPath $workshopJsonPath -Raw | ConvertFrom-Json
    $changeNote = $workshopMetadata.changeNote

    if ($changeNote) {
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

        if ($DryRun) {
            Write-Host "[CHANGELOG] Would prepend new section to CHANGELOG.md:" -ForegroundColor Yellow
            Write-Host "----------------------------------------" -ForegroundColor DarkGray
            Write-Host $newEntry.Trim() -ForegroundColor White
            Write-Host "`nFooter Reference:" -ForegroundColor DarkGray
            Write-Host $urlRef -ForegroundColor White
            Write-Host "----------------------------------------" -ForegroundColor DarkGray
            Write-Host "[Git] Would commit: 'docs(changelog): update release notes for $version'" -ForegroundColor Yellow
        }
        else {
            Write-Host "[CHANGELOG] Prepending release notes for $version..." -ForegroundColor Cyan
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

            git add $changelogPath
            git commit -m "docs(changelog): update release notes for $version" --quiet
            Write-Host "[Git] Committed updated CHANGELOG.md." -ForegroundColor Green
        }
    }
}

# 3. Evaluate Git Tag
$prevEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$tagCommit = (git rev-parse --verify --quiet "refs/tags/$version" 2>$null)
$tagExists = ($LASTEXITCODE -eq 0)
$headCommit = (git rev-parse HEAD 2>$null)
$ErrorActionPreference = $prevEAP

if ($tagExists -and $tagCommit -eq $headCommit) {
    Write-Host "[Git] Tag '$version' already points to HEAD. Would skip tag." -ForegroundColor Gray
}
elseif ($tagExists) {
    if ($DryRun) {
        Write-Host "[Git] Tag '$version' points to an older commit. Would force-move tag to HEAD ($headCommit) and push." -ForegroundColor Yellow
    }
    else {
        Write-Host "[Git] Moving tag '$version' to current commit..." -ForegroundColor Yellow
        git tag -f -a $version -m "Release ($Channel): $version"
        git push origin $version -f
    }
}
else {
    if ($DryRun) {
        Write-Host "[Git] Would create annotated tag '$version' and push to origin." -ForegroundColor Yellow
    }
    else {
        Write-Host "[Git] Creating and pushing tag $version..." -ForegroundColor Green
        git tag -a $version -m "Release ($Channel): $version"
        git push origin $version
    }
}

if ($DryRun) {
    Write-Host "================ [END OF DRY RUN] ================`n" -ForegroundColor Magenta
}
else {
    Write-Host "Pre-flight stage complete!" -ForegroundColor Green
}