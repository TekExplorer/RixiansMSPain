$ErrorActionPreference = 'Stop'

$runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
$uploaderName = if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    'ModUploader.exe'
} else {
    'ModUploader'
}

$uploaderPath = Join-Path $PSScriptRoot "uploader\$uploaderName"

if (-not (Test-Path $uploaderPath)) {
    & (Join-Path $PSScriptRoot '_get_uploader.ps1')

    if (-not (Test-Path $uploaderPath)) {
        throw 'Uploader was not downloaded.'
    }
}

& $uploaderPath upload -w $PSScriptRoot