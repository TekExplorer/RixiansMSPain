$ErrorActionPreference = 'Stop'

$workshopRoot = Split-Path -Parent $PSScriptRoot
$runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
$uploaderName = if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    'ModUploader.exe'
} else {
    'ModUploader'
}

$uploaderPath = Join-Path $workshopRoot "uploader\$uploaderName"
$contentRoot = Join-Path $workshopRoot 'content'
$requiredContentFiles = @(
    'HideDetailsMod.dll'
    'HideDetailsMod.json'
    'HideDetailsMod.pdb'
    'HideDetailsMod.pck'
)

$missingFiles = foreach ($fileName in $requiredContentFiles) {
    $filePath = Join-Path $contentRoot $fileName
    if (-not (Test-Path $filePath)) {
        $fileName
    }
}

if ($missingFiles) {
    throw "Missing required workshop content files: $($missingFiles -join ', '). Build and publish before uploading."
}

$confirmation = Read-Host "Type UPLOAD to continue"
if ($confirmation -ne 'UPLOAD') {
    Write-Host 'Upload cancelled.'
    exit 0
}

if (-not (Test-Path $uploaderPath)) {
    $downloadUploader = Read-Host 'Uploader is missing. Download it now? (Y/N)'
    if ($downloadUploader -notin @('Y', 'y', 'Yes', 'yes')) {
        Write-Host 'Upload cancelled.'
        exit 0
    }

    & (Join-Path $PSScriptRoot 'get_uploader.ps1')

    if (-not (Test-Path $uploaderPath)) {
        throw 'Uploader was not downloaded.'
    }
}

& $uploaderPath upload -w $workshopRoot