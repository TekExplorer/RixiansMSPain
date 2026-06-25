$ErrorActionPreference = 'Stop'

$workshopRoot = Split-Path -Parent $PSScriptRoot
$runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
$uploaderName = if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    'ModUploader.exe'
} else {
    'ModUploader'
}

$uploaderPath = Join-Path $workshopRoot "uploader\$uploaderName"

if (-not (Test-Path $uploaderPath)) {
    & (Join-Path $PSScriptRoot 'get_uploader.ps1')

    if (-not (Test-Path $uploaderPath)) {
        throw 'Uploader was not downloaded.'
    }
}

& $uploaderPath upload -w $workshopRoot