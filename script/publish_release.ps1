[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$version = (Get-Content (Join-Path $repositoryRoot "VERSION") -Raw).Trim()
if ($version -notmatch '^0\.[0-9]+\.[0-9]+$') {
    throw "VERSION must contain a stable 0.x.y version, got '$version'."
}

$tag = "v$version"
if ($env:GITHUB_REF_NAME -and $env:GITHUB_REF_NAME -ne $tag) {
    throw "GITHUB_REF_NAME '$($env:GITHUB_REF_NAME)' does not match VERSION tag '$tag'."
}
$releaseRoot = Join-Path $repositoryRoot "artifacts\release\$tag"
$publishRoot = Join-Path $releaseRoot "ThreadBeacon"
$singleFileRoot = Join-Path $releaseRoot "_single"
$archivePath = Join-Path $releaseRoot "ThreadBeacon-$tag-$Runtime.zip"
$exeAssetPath = Join-Path $releaseRoot "ThreadBeacon-$tag-$Runtime.exe"

New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null
if (Test-Path $publishRoot) {
    Remove-Item -Recurse -Force -LiteralPath $publishRoot
}
if (Test-Path $archivePath) {
    Remove-Item -Force -LiteralPath $archivePath
}
if (Test-Path $exeAssetPath) {
    Remove-Item -Force -LiteralPath $exeAssetPath
}
if (Test-Path $singleFileRoot) {
    Remove-Item -Recurse -Force -LiteralPath $singleFileRoot
}

dotnet publish (Join-Path $repositoryRoot "src\ThreadBeacon.App\ThreadBeacon.App.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishRoot `
    -p:Version=$version `
    -p:AssemblyVersion="$version.0" `
    -p:FileVersion="$version.0" `
    -p:InformationalVersion=$version

function Test-ReleaseArchive([string]$Path, [string]$SourceDirectory) {
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
        try {
            $fileEntries = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith("/") })
            $sourceFileCount = @(Get-ChildItem -LiteralPath $SourceDirectory -Recurse -File).Count
            if ($fileEntries.Count -ne $sourceFileCount) {
                return $false
            }

            $buffer = New-Object byte[] 65536
            foreach ($entry in $fileEntries) {
                $stream = $entry.Open()
                try {
                    while ($stream.Read($buffer, 0, $buffer.Length) -gt 0) {}
                }
                finally {
                    $stream.Dispose()
                }
            }
            return $true
        }
        finally {
            $archive.Dispose()
        }
    }
    catch {
        return $false
    }
}

$maximumArchiveAttempts = 3
$archiveIsValid = $false
for ($archiveAttempt = 1; $archiveAttempt -le $maximumArchiveAttempts; $archiveAttempt++) {
    if (Test-Path $archivePath) {
        Remove-Item -Force -LiteralPath $archivePath
    }

    try {
        Compress-Archive `
            -Path (Join-Path $publishRoot "*") `
            -DestinationPath $archivePath `
            -CompressionLevel Optimal `
            -ErrorAction Stop
        $archiveIsValid = Test-ReleaseArchive $archivePath $publishRoot
    }
    catch {
        $archiveIsValid = $false
    }

    if ($archiveIsValid) {
        break
    }
    if ($archiveAttempt -lt $maximumArchiveAttempts) {
        Start-Sleep -Seconds 1
    }
}

if (-not $archiveIsValid) {
    throw "Release archive validation failed after $maximumArchiveAttempts attempts."
}

dotnet publish (Join-Path $repositoryRoot "src\ThreadBeacon.App\ThreadBeacon.App.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $singleFileRoot `
    -p:Version=$version `
    -p:AssemblyVersion="$version.0" `
    -p:FileVersion="$version.0" `
    -p:InformationalVersion=$version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

Copy-Item (Join-Path $singleFileRoot "ThreadBeacon.App.exe") $exeAssetPath
Remove-Item -Recurse -Force -LiteralPath $singleFileRoot

[pscustomobject]@{
    Version = $version
    Tag = $tag
    Runtime = $Runtime
    Executable = $exeAssetPath
    Package = $archivePath
    PublishDirectory = $publishRoot
}
