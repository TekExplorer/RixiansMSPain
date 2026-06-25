param(
    [string]$Repository = 'megacrit/sts2-mod-uploader',
    [string]$Destination = (Join-Path $PSScriptRoot 'uploader')
)

function Get-PlatformId {
    $runtimeInfo = [System.Runtime.InteropServices.RuntimeInformation]

    if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        return 'win'
    }

    if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) {
        return 'linux'
    }

    if ($runtimeInfo::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
        return 'osx'
    }

    throw 'Unsupported operating system.'
}

function Get-ArchitectureId {
    switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
        'Arm64' { 'arm64' }
        default { 'x64' }
    }
}

$platformId = Get-PlatformId
$architectureId = Get-ArchitectureId
$assetName = "ModUploader-$platformId-$architectureId.zip"
$releaseUrl = "https://api.github.com/repos/$Repository/releases/latest"
$requestHeaders = @{ 'User-Agent' = 'PowerShell' }
$release = Invoke-RestMethod -Uri $releaseUrl -Headers $requestHeaders
$asset = $release.assets | Where-Object Name -eq $assetName | Select-Object -First 1

if (-not $asset) {
    throw "Could not find asset '$assetName' in the latest release '$($release.tag_name)'."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ModUploader-" + [guid]::NewGuid().ToString('N'))
$extractRoot = Join-Path $tempRoot 'extract'
$zipPath = Join-Path $tempRoot $assetName

New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null

try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $requestHeaders

    if (Test-Path $Destination) {
        Remove-Item $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force

    $extractedItems = Get-ChildItem -Path $extractRoot -Force
    $sourceRoot = if ($extractedItems.Count -eq 1 -and $extractedItems[0].PSIsContainer) {
        $extractedItems[0].FullName
    } else {
        $extractRoot
    }

    Copy-Item -Path (Join-Path $sourceRoot '*') -Destination $Destination -Recurse -Force

    Write-Host "Downloaded $assetName to $Destination"
}
finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}