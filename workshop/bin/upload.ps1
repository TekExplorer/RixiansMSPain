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
$workshopRoot = Split-Path -Parent $binRoot
$workspaceRoot = Split-Path -Parent $workshopRoot

$channelRoot = Join-Path $workshopRoot $Channel
$contentRoot = Join-Path $channelRoot 'content'
$jsonPath = Join-Path $contentRoot 'HideDetailsMod.json'
$workshopJsonPath = Join-Path $channelRoot 'workshop.json'

$runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
$uploaderName = if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) { 'ModUploader.exe' } else { 'ModUploader' }
$uploaderPath = Join-Path $workshopRoot "uploader\$uploaderName"

# 0. Ensure ModUploader Exists
if (-not (Test-Path -LiteralPath $uploaderPath)) {
    $getUploaderScript = Join-Path $binRoot 'get_uploader.ps1'
    if (Test-Path -LiteralPath $getUploaderScript) {
        Write-Host "[Uploader] '$uploaderName' not found. Downloading via get_uploader.ps1..." -ForegroundColor Yellow
        & $getUploaderScript
        if (-not (Test-Path -LiteralPath $uploaderPath)) {
            throw "Failed to download or locate uploader at '$uploaderPath'."
        }
    }
    else {
        throw "Uploader not found at '$uploaderPath' and script '$getUploaderScript' is missing."
    }
}

# 1. Verification Checks
if (-not (Test-Path -LiteralPath $channelRoot)) {
    throw "Workshop channel folder not found at '$channelRoot'."
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

# 2. Extract Version and Build Notes with Timeline Diff
$manifest = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
[string]$rawVersion = if ($manifest.version) { $manifest.version } elseif ($manifest.Version) { $manifest.Version } else { $null }
$version = $rawVersion.Trim()
if (-not $version.StartsWith('v')) { $version = "v$version" }
if ($Channel -eq 'Canary' -and -not $version.EndsWith('-canary')) { $version = "$version-canary" }

$rawChangeNotes = ""
if (Test-Path -LiteralPath $workshopJsonPath) {
    $wsMeta = Get-Content -LiteralPath $workshopJsonPath -Raw | ConvertFrom-Json
    if ($wsMeta.changeNote) { $rawChangeNotes = $wsMeta.changeNote }
}

# Format bullets
$bulletNotes = ($rawChangeNotes -split "\r?\n" | Where-Object { $_.Trim() -ne '' } | ForEach-Object {
        $line = $_.Trim()
        if (-not ($line.StartsWith('-') -or $line.StartsWith('*'))) { "- $line" } else { $line }
    }) -join "`n"

# Determine previous tag for comparison diff
$prevTag = if ($Channel -eq 'Canary') {
    (git tag --sort=-v:refname | Where-Object { $_ -ne $version -and $_ -like "*-canary" } | Select-Object -First 1)
}
else {
    (git tag --sort=-v:refname | Where-Object { $_ -ne $version -and $_ -notlike "*-canary" } | Select-Object -First 1)
}
if (-not $prevTag) {
    $prevTag = (git tag --sort=-v:refname | Where-Object { $_ -ne $version } | Select-Object -First 1)
}

$diffSection = if ($prevTag) {
    "### Git Timeline Diff`nSee complete code changes: [$($prevTag.Trim())...$version]($GitHubRepoUrl/compare/$($prevTag.Trim())...$version)"
}
else {
    "### Git Timeline Diff`nSee release tag: [$version]($GitHubRepoUrl/releases/tag/$version)"
}

$fullReleaseNotes = if ($bulletNotes) {
    "$bulletNotes`n`n$diffSection"
}
else {
    $diffSection
}

# 3. Check GitHub Release State
$hasGhCli = [bool](Get-Command gh -ErrorAction SilentlyContinue)
$githubReleaseNeedsSync = $true

if ($hasGhCli) {
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $existingReleaseJson = gh release view $version --json "assets,isDraft" 2>$null
    $ErrorActionPreference = $prevEAP

    if ($LASTEXITCODE -eq 0 -and $existingReleaseJson) {
        $releaseInfo = $existingReleaseJson | ConvertFrom-Json
        $assetName = "HideDetailsMod-$version.zip"
        $hasAsset = $releaseInfo.assets | Where-Object { $_.name -eq $assetName }
        if ($hasAsset -and -not $releaseInfo.isDraft) {
            Write-Host "[GitHub] Release $version already exists with asset '$assetName'. Would skip." -ForegroundColor Gray
            $githubReleaseNeedsSync = $false
        }
    }
}

# 4. Dry Run Output vs Live Execution
if ($DryRun) {
    Write-Host "`n--- Dry Run Plan Summary ---" -ForegroundColor Cyan
    Write-Host "Target Channel:   $Channel"
    Write-Host "Target Version:   $version"
    Write-Host "Workshop Folder:  $channelRoot"
    Write-Host "Uploader Binary:  $uploaderPath (Found: $(Test-Path -LiteralPath $uploaderPath))"

    Write-Host "`n[Steam] Would run: '$uploaderPath upload -w $channelRoot'" -ForegroundColor Yellow
    
    if ($githubReleaseNeedsSync) {
        Write-Host "`n[GitHub] Target Release Body:" -ForegroundColor DarkGray
        Write-Host "----------------------------------------" -ForegroundColor DarkGray
        Write-Host $fullReleaseNotes -ForegroundColor White
        Write-Host "----------------------------------------" -ForegroundColor DarkGray
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
    $jobs += Start-Job -Name "SteamWorkshop" -ArgumentList $uploaderPath, $channelRoot -ScriptBlock {
        param($uploader, $workshop)
        Write-Output "Starting Steam Workshop upload..."
        & $uploader upload -w $workshop 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Steam upload failed with exit code $LASTEXITCODE" }
        Write-Output "Steam Workshop upload finished successfully."
    }
}

# --- GITHUB JOB ---
if ($githubReleaseNeedsSync -and $hasGhCli) {
    $jobs += Start-Job -Name "GitHubRelease" -ArgumentList $version, $channelRoot, $contentRoot, $fullReleaseNotes, $Channel, $workspaceRoot -ScriptBlock {
        param($ver, $channelDir, $contentDir, $notes, $chan, $repoRoot)

        Set-Location -LiteralPath $repoRoot

        $zipName = "HideDetailsMod-$ver.zip"
        $zipPath = Join-Path $channelDir $zipName
        if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

        Write-Output "Collecting content files for archive..."
        $filesToZip = Get-ChildItem -Path $contentDir -File
        $totalBytes = ($filesToZip | Measure-Object -Property Length -Sum).Sum
        $sizeMB = [math]::Round($totalBytes / 1MB, 2)
        Write-Output "Compressing $($filesToZip.Count) files (~$sizeMB MB) into $zipName..."
        
        Compress-Archive -Path (Join-Path $contentDir '*') -DestinationPath $zipPath -Force
        $zipSizeMB = [math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 2)
        Write-Output "Archive created ($zipSizeMB MB)."

        $tempNotes = [System.IO.Path]::GetTempFileName()
        $notes | Set-Content -Path $tempNotes -Encoding utf8

        Write-Output "Querying GitHub release status for tag '$ver'..."
        $null = cmd /c "gh release view $ver 2>nul"
        $releaseExists = ($LASTEXITCODE -eq 0)

        if ($releaseExists) {
            Write-Output "Release '$ver' exists. Updating notes and uploading '$zipName'..."
            cmd /c "gh release edit $ver --notes-file `"$tempNotes`" 2>&1"
            cmd /c "gh release upload $ver `"$zipPath`" --clobber 2>&1"
        }
        else {
            Write-Output "Creating new release '$ver' and publishing asset..."
            $flags = @('--title', "`"$ver`"", '--notes-file', "`"$tempNotes`"")
            if ($chan -eq 'Canary') { $flags += '--prerelease' }
            
            $cmd = "gh release create $ver `"$zipPath`" " + ($flags -join ' ') + " 2>&1"
            $createOut = cmd /c $cmd
            if ($createOut) { Write-Output ($createOut -join "`n") }
        }

        if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
        if (Test-Path -LiteralPath $tempNotes) { Remove-Item -LiteralPath $tempNotes -Force }
        
        Write-Output "GitHub Release completed successfully."
    }
}

# 6. Stream Live Output Safely
while ($jobs | Where-Object { $_.State -eq 'Running' }) {
    foreach ($job in $jobs) {
        $output = Receive-Job -Job $job -ErrorAction SilentlyContinue
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

# Flush remaining output
foreach ($job in $jobs) {
    $output = Receive-Job -Job $job -ErrorAction SilentlyContinue
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
    foreach ($failedJob in $failedJobs) {
        Write-Host "[$($failedJob.Name)] Execution Error: $($failedJob.JobStateInfo.Reason.Message)" -ForegroundColor Red
    }
    throw "One or more upload operations failed. Inspect logs above."
}

Write-Host "`nAll operations completed successfully!" -ForegroundColor Green