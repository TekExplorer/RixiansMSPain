param(
    [string]$Repository = 'megacrit/sts2-mod-uploader',
    [string]$Destination = (Join-Path (Split-Path -Parent $PSScriptRoot) 'uploader')
)

# Convert the current OS into the release naming convention used by GitHub.
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

# Convert the current CPU architecture into the release naming convention.
function Get-ArchitectureId {
    switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
        'Arm64' { 'arm64' }
        default { 'x64' }
    }
}

# Build the asset name for the current platform.
$platformId = Get-PlatformId
$architectureId = Get-ArchitectureId
$assetName = "ModUploader-$platformId-$architectureId.zip"
# Ask GitHub for the latest release metadata.
$releaseUrl = "https://api.github.com/repos/$Repository/releases/latest"
$requestHeaders = @{ 'User-Agent' = 'PowerShell' }
$release = Invoke-RestMethod -Uri $releaseUrl -Headers $requestHeaders
# Pick the zip file that matches this machine.
$asset = $release.assets | Where-Object Name -eq $assetName | Select-Object -First 1

if (-not $asset) {
    throw "Could not find asset '$assetName' in the latest release '$($release.tag_name)'."
}

# Create a temporary area for the download and extraction.
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ModUploader-" + [guid]::NewGuid().ToString('N'))
$extractRoot = Join-Path $tempRoot 'extract'
$zipPath = Join-Path $tempRoot $assetName

# Ensure the extraction folder exists before unpacking.
New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null

try {
    # Download the release archive.
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $requestHeaders

    # Replace any existing uploader folder so the contents stay current.
    if (Test-Path $Destination) {
        Remove-Item $Destination -Recurse -Force
    }

    # Recreate the destination folder before copying files into it.
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    # Unzip the release into the temporary extraction folder.
    Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force

    # Some archives unpack into a single top-level folder, others do not.
    $extractedItems = Get-ChildItem -Path $extractRoot -Force
    $sourceRoot = if ($extractedItems.Count -eq 1 -and $extractedItems[0].PSIsContainer) {
        $extractedItems[0].FullName
    } else {
        $extractRoot
    }

    # Copy the unpacked uploader files into workshop\uploader.
    Copy-Item -Path (Join-Path $sourceRoot '*') -Destination $Destination -Recurse -Force

    Write-Host "Downloaded $assetName to $Destination"
}
finally {
    # Remove the temporary download directory even if something fails.
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}