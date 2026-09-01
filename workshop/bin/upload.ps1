param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production',

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

if ($DryRun) {
    Write-Host "`n================ [DRY RUN MODE ENABLED] ================" -ForegroundColor Magenta
}

$binRoot = $PSScriptRoot
$workshopScriptsRoot = Split-Path -Parent $binRoot
$workspaceRoot = Split-Path -Parent $workshopScriptsRoot

$channelRoot = Join-Path $workspaceRoot "workshop\$Channel"
$workshopRoot = $channelRoot
$contentRoot = Join-Path $channelRoot 'content'
$jsonPath = Join-Path $contentRoot 'HideDetailsMod.json'
$workshopJsonPath = Join-Path $channelRoot 'workshop.json'

$runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
$uploaderName = if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) { 'ModUploader.exe' } else { 'ModUploader' }
$uploaderPath = Join-Path $workspaceRoot "uploader\$uploaderName"

# 1. Verification Checks
if (-not (Test-Path -LiteralPath $workshopRoot)) {
    throw "Workshop channel folder not found at '$workshopRoot'."
}

$requiredContentFiles = @(
    'HideDetailsMod.dll', 'HideDetailsMod.json', 'HideDetailsMod.pdb',
    'HideDetailsMod.pck', 'HideDetailsMod.Beta.betapack', 'HideDetailsMod.Beta.pdb'
)
$missingFiles = foreach ($file in $requiredContentFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $contentRoot $file))) { $file }
}
if ($missingFiles) {
    throw "Missing required content files: $($missingFiles -join ', ')."
}
else {
    Write-Host "[Check] All $($requiredContentFiles.Count) content files exist in '$contentRoot'." -ForegroundColor Green
}

# 2. Extract Version and Notes
$manifest = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
[string]$rawVersion = if ($manifest.version) { $manifest.version } elseif ($manifest.Version) { $manifest.Version } else { $null }
$version = $rawVersion.Trim()
if (-not $version.StartsWith('v')) { $version = "v$version" }
if ($Channel -eq 'Canary' -and -not $version.EndsWith('-canary')) { $version = "$version-canary" }

$releaseNotes = ""
if (Test-Path -LiteralPath $workshopJsonPath) {
    $wsMeta = Get-Content -LiteralPath $workshopJsonPath -Raw | ConvertFrom-Json
    if ($wsMeta.changeNote) { $releaseNotes = $wsMeta.changeNote }
}

# 3. Check GitHub Release State
$hasGhCli = [bool](Get-Command gh -ErrorAction SilentlyContinue)
$githubReleaseNeedsSync = $true

if ($hasGhCli) {
    # Suppress NativeCommandError by merging stderr into stdout or catching via try/finally
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $existingReleaseJson = gh release view $version --json "assets,isDraft" 2>$null
    $ErrorActionPreference = $prevEAP

    if ($LASTEXITCODE -eq 0 -and $existingReleaseJson) {
        $releaseInfo = $existingReleaseJson | ConvertFrom-Json
        $assetName = "HideDetailsMod-$version.zip"
        $hasAsset = $releaseInfo.assets | Where-Object { $_.name -eq $assetName }
        if ($hasAsset -and -not $releaseInfo.isDraft) {
            Write-Host "[GitHub] Release $version already exists. Would skip GitHub release." -ForegroundColor Gray
            $githubReleaseNeedsSync = $false
        }
    }
}

# 4. Dry Run Output vs Live Execution
if ($DryRun) {
    Write-Host "`n--- Dry Run Plan Summary ---" -ForegroundColor Cyan
    Write-Host "Target Channel:   $Channel"
    Write-Host "Target Version:   $version"
    Write-Host "Workshop Folder:  $workshopRoot"
    Write-Host "Uploader Binary:  $uploaderPath (Found: $(Test-Path -LiteralPath $uploaderPath))"

    Write-Host "`n[Steam] Would run: '$uploaderPath upload -w $workshopRoot'" -ForegroundColor Yellow
    
    if ($githubReleaseNeedsSync) {
        Write-Host "[GitHub] Would zip: '$contentRoot\*' -> 'HideDetailsMod-$version.zip'" -ForegroundColor Yellow
        Write-Host "[GitHub] Would run: 'gh release create $version ... $(if ($Channel -eq 'Canary') {'--prerelease'})'" -ForegroundColor Yellow
    }

    Write-Host "`n================ [END OF DRY RUN] ================`n" -ForegroundColor Magenta
    exit 0
}

# 5. Live Confirmation Prompt
$confirmation = Read-Host "Type UPLOAD to push $version ($Channel) to Steam Workshop (or press Enter to skip Steam)"
$shouldUploadToSteam = ($confirmation -eq 'UPLOAD')

if (-not $shouldUploadToSteam) {
    Write-Host "[Steam] Skipping Steam Workshop upload." -ForegroundColor Yellow
    if (-not $githubReleaseNeedsSync) {
        Write-Host "Nothing to upload. Exiting." -ForegroundColor Green
        exit 0
    }
}

Write-Host "`n>>> Starting Background Tasks for $version ($Channel) <<<`n" -ForegroundColor Magenta

$jobs = @()

# --- STEAM JOB ---
if ($shouldUploadToSteam) {
    $jobs += Start-Job -Name "SteamWorkshop" -ArgumentList $uploaderPath, $workshopRoot -ScriptBlock {
        param($uploader, $workshop)
        & $uploader upload -w $workshop 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Steam upload failed with exit code $LASTEXITCODE" }
    }
}

# --- GITHUB JOB ---
if ($githubReleaseNeedsSync -and $hasGhCli) {
    $jobs += Start-Job -Name "GitHubRelease" -ArgumentList $version, $channelRoot, $contentRoot, $releaseNotes, $Channel -ScriptBlock {
        param($ver, $channelDir, $contentDir, $notes, $chan)

        $zipPath = Join-Path $channelDir "HideDetailsMod-$ver.zip"
        if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
        Compress-Archive -Path (Join-Path $contentDir '*') -DestinationPath $zipPath -Force

        $tempNotes = [System.IO.Path]::GetTempFileName()
        $notes | Set-Content -Path $tempNotes -Encoding utf8

        $flags = @('--title', $ver, '--notes-file', $tempNotes)
        if ($chan -eq 'Canary') { $flags += '--prerelease' }

        # Temporarily drop EAP so gh release view doesn't throw NativeCommandError when release is missing
        $prevEAP = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $null = gh release view $ver 2>$null
        $releaseExists = ($LASTEXITCODE -eq 0)
        $ErrorActionPreference = $prevEAP

        if ($releaseExists) {
            gh release upload $ver $zipPath --clobber
        }
        else {
            gh release create $ver $zipPath @flags
        }

        if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
        if (Test-Path -LiteralPath $tempNotes) { Remove-Item -LiteralPath $tempNotes -Force }
    }
}

# 6. Stream Live Output
while ($jobs | Where-Object { $_.State -eq 'Running' }) {
    foreach ($job in $jobs) {
        $output = Receive-Job -Job $job
        if ($output) {
            $color = if ($job.Name -eq 'SteamWorkshop') { 'Cyan' } else { 'Yellow' }
            foreach ($line in $output) {
                Write-Host "[$($job.Name)] " -ForegroundColor $color -NoNewline
                Write-Host $line
            }
        }
    }
    Start-Sleep -Milliseconds 250
}

foreach ($job in $jobs) {
    $output = Receive-Job -Job $job
    if ($output) {
        $color = if ($job.Name -eq 'SteamWorkshop') { 'Cyan' } else { 'Yellow' }
        foreach ($line in $output) {
            Write-Host "[$($job.Name)] " -ForegroundColor $color -NoNewline
            Write-Host $line
        }
    }
}

$failedJobs = $jobs | Where-Object { $_.State -eq 'Failed' -or $_.JobStateInfo.Reason }
$jobs | Remove-Job -Force

if ($failedJobs) {
    throw "One or more upload operations failed. Inspect logs above."
}

Write-Host "`nAll operations completed successfully!" -ForegroundColor Green