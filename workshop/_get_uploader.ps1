param(
    [string]$Repository = 'megacrit/sts2-mod-uploader',
    [string]$Destination = (Join-Path $PSScriptRoot 'uploader')
)

$runtime = [System.Runtime.InteropServices.RuntimeInformation]
$osId = if ($runtime::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    'win'
} elseif ($runtime::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) {
    'linux'
} elseif ($runtime::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
    'osx'
} else {
    throw 'Unsupported operating system.'
}

$architecture = switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
    'Arm64' { 'arm64' }
    default { 'x64' }
}

$assetName = "ModUploader-$osId-$architecture.zip"
$releaseUri = "https://api.github.com/repos/$Repository/releases/latest"
$headers = @{ 'User-Agent' = 'PowerShell' }
$release = Invoke-RestMethod -Uri $releaseUri -Headers $headers
$asset = $release.assets | Where-Object { $_.name -eq $assetName } | Select-Object -First 1

if (-not $asset) {
    throw "Could not find asset '$assetName' in the latest release '$($release.tag_name)'."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ModUploader-" + [guid]::NewGuid().ToString('N'))
$extractRoot = Join-Path $tempRoot 'extract'
$zipPath = Join-Path $tempRoot $assetName

New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null

try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $headers

    if (Test-Path $Destination) {
        Remove-Item $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force

    $topLevelItems = Get-ChildItem -Path $extractRoot -Force
    $sourceRoot = if ($topLevelItems.Count -eq 1 -and $topLevelItems[0].PSIsContainer) {
        $topLevelItems[0].FullName
    } else {
        $extractRoot
    }

    Copy-Item -Path (Join-Path $sourceRoot '*') -Destination $Destination -Recurse -Force

    Write-Host "Downloaded $assetName to $Destination"
}
finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}