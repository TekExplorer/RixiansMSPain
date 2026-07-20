param(
    [ValidateSet('Canary', 'Production')]
    [string]$Channel = 'Production'
)

$ErrorActionPreference = 'Stop'

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$workshopRoot = Join-Path $workspaceRoot $Channel
$runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
# Use the platform-specific uploader name on Windows vs. other systems.
$uploaderName = if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    'ModUploader.exe'
}
else {
    'ModUploader'
}

# This is the uploader executable inside workshop\uploader.
$uploaderPath = Join-Path $workspaceRoot "uploader\$uploaderName"
# These are the files that must exist before upload starts.
$contentRoot = Join-Path $workshopRoot 'content'
$requiredContentFiles = @(
    'HideDetailsMod.dll'
    'HideDetailsMod.json'
    'HideDetailsMod.pdb'
    'HideDetailsMod.pck'
)

if (-not (Test-Path $workshopRoot)) {
    throw "Workshop channel folder not found at '$workshopRoot'."
}

# Collect any missing required files so we can fail early.
$missingFiles = foreach ($fileName in $requiredContentFiles) {
    $filePath = Join-Path $contentRoot $fileName
    if (-not (Test-Path $filePath)) {
        $fileName
    }
}

# Stop if the build/publish step has not produced all required outputs.
if ($missingFiles) {
    throw "Missing required workshop content files: $($missingFiles -join ', '). Build and publish before uploading."
}

# Require an explicit typed confirmation before uploading.
$confirmation = Read-Host "Type UPLOAD to continue"
if ($confirmation -ne 'UPLOAD') {
    Write-Host 'Upload cancelled.'
    exit 0
}

# If the uploader is missing, offer to download it first.
if (-not (Test-Path $uploaderPath)) {
    $downloadUploader = Read-Host 'Uploader is missing. Download it now? (Y/N)'
    if ($downloadUploader -notin @('Y', 'y', 'Yes', 'yes')) {
        Write-Host 'Upload cancelled.'
        exit 0
    }

    # Reuse the downloader script instead of duplicating the download logic.
    & (Join-Path $PSScriptRoot 'get_uploader.ps1')

    if (-not (Test-Path $uploaderPath)) {
        throw 'Uploader was not downloaded.'
    }
}

# Run the uploader against the workshop root.
& $uploaderPath upload -w $workshopRoot