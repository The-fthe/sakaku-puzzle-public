[CmdletBinding()]
param(
    [string]$ProjectPath,
    [string]$ArchivePath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = [System.IO.Path]::GetFullPath(
        (Join-Path $PSScriptRoot "../../../.."))
}

$ProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
$projectVersionPath = Join-Path $ProjectPath "ProjectSettings/ProjectVersion.txt"
if (!(Test-Path -LiteralPath $projectVersionPath -PathType Leaf)) {
    throw "Unity project version file was not found: $projectVersionPath"
}

$projectVersionContent =
    Get-Content -Raw -Encoding UTF8 -LiteralPath $projectVersionPath
$versionMatch = [regex]::Match(
    $projectVersionContent,
    "(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$")
if (!$versionMatch.Success) {
    throw "Could not read m_EditorVersion from $projectVersionPath"
}

$editorVersion = $versionMatch.Groups["version"].Value
if ($editorVersion -notmatch "^6000\.5\.") {
    throw "unityroom requires Unity 6000.5.x; project uses $editorVersion"
}

$buildRoot = Join-Path $ProjectPath "Builds/WebGL/unityroom_build"
if (!(Test-Path -LiteralPath $buildRoot -PathType Container)) {
    throw "Complete WebGL build root was not found: $buildRoot"
}

$indexPath = Join-Path $buildRoot "index.html"
if (!(Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    throw "WebGL index.html was not found: $indexPath"
}

$templateDataDirectory = Join-Path $buildRoot "TemplateData"
if (!(Test-Path -LiteralPath $templateDataDirectory -PathType Container)) {
    throw "WebGL TemplateData folder was not found: $templateDataDirectory"
}

$buildDirectory = Join-Path $buildRoot "Build"
if (!(Test-Path -LiteralPath $buildDirectory -PathType Container)) {
    throw "WebGL Build folder was not found: $buildDirectory"
}

$requiredStreamingAssetPaths = @(
    "Tutorial/peek-how-to.mp4",
    "Tutorial/stage1-how-to.mp4"
)
foreach ($requiredStreamingAssetPath in $requiredStreamingAssetPaths) {
    $streamingAssetPath = Join-Path (
        Join-Path $buildRoot "StreamingAssets") $requiredStreamingAssetPath
    if (!(Test-Path -LiteralPath $streamingAssetPath -PathType Leaf)) {
        throw "Required StreamingAsset was not found: $streamingAssetPath"
    }
}

$requiredPatterns = @(
    "*.loader.js",
    "*.data.gz",
    "*.framework.js.gz",
    "*.wasm.gz"
)
$buildFiles = @()
foreach ($requiredPattern in $requiredPatterns) {
    $matches = @(
        Get-ChildItem -LiteralPath $buildDirectory -File |
            Where-Object Name -Like $requiredPattern)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one '$requiredPattern' file; found $($matches.Count)."
    }
    $buildFiles += $matches[0]
}

$unexpectedFiles = @(
    Get-ChildItem -LiteralPath $buildDirectory -File |
        Where-Object FullName -NotIn $buildFiles.FullName)
if ($unexpectedFiles.Count -gt 0) {
    throw "Unexpected files in Build folder: $($unexpectedFiles.Name -join ', ')"
}

foreach ($buildFile in $buildFiles | Where-Object Extension -EQ ".gz") {
    $stream = [System.IO.File]::OpenRead($buildFile.FullName)
    try {
        $firstByte = $stream.ReadByte()
        $secondByte = $stream.ReadByte()
    }
    finally {
        $stream.Dispose()
    }
    if ($firstByte -ne 0x1F -or $secondByte -ne 0x8B) {
        throw "$($buildFile.Name) does not have a Gzip header."
    }
}

if ([string]::IsNullOrWhiteSpace($ArchivePath)) {
    $archiveName =
        "SakakuPuzzle-unityroom-WebGL-$editorVersion.zip"
    $ArchivePath = Join-Path $ProjectPath "Builds/WebGL/Packages/$archiveName"
}
$ArchivePath = [System.IO.Path]::GetFullPath($ArchivePath)
$artifactDirectory = Split-Path -Parent $ArchivePath
[System.IO.Directory]::CreateDirectory($artifactDirectory) | Out-Null

$temporaryRoot = Join-Path $ProjectPath ".utmp/unityroom-webgl-package"
$resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
$resolvedTemporaryParent = [System.IO.Path]::GetFullPath(
    (Join-Path $ProjectPath ".utmp"))
if ((Split-Path -Parent $resolvedTemporaryRoot) -ne $resolvedTemporaryParent) {
    throw "Unexpected temporary path: $resolvedTemporaryRoot"
}

if (Test-Path -LiteralPath $resolvedTemporaryRoot) {
    Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
}
$temporaryPackageDirectory =
    Join-Path $resolvedTemporaryRoot "unityroom_build"
[System.IO.Directory]::CreateDirectory(
    $temporaryPackageDirectory) | Out-Null
$packageSourceItems = @(
    Get-ChildItem -LiteralPath $buildRoot -Force |
        Where-Object Name -NotLike "*_BurstDebugInformation_DoNotShip")
foreach ($packageSourceItem in $packageSourceItems) {
    Copy-Item -LiteralPath $packageSourceItem.FullName `
        -Destination $temporaryPackageDirectory -Recurse
}

if (Test-Path -LiteralPath $ArchivePath) {
    Remove-Item -LiteralPath $ArchivePath -Force
}
Compress-Archive -LiteralPath $temporaryPackageDirectory `
    -DestinationPath $ArchivePath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
try {
    $archiveEntries = @(
        $archive.Entries |
            Where-Object { ![string]::IsNullOrEmpty($_.Name) })
    $expectedEntryNames = @(
        Get-ChildItem -LiteralPath $temporaryPackageDirectory -File -Recurse |
            ForEach-Object {
                $relativeName = $_.FullName.Substring(
                    $temporaryPackageDirectory.Length + 1).Replace("\", "/")
                "unityroom_build/$relativeName"
            })
    $actualEntryNames = @(
        $archiveEntries |
            ForEach-Object { $_.FullName.Replace("\", "/") })

    $missingEntries = @(
        $expectedEntryNames |
            Where-Object { $_ -notin $actualEntryNames })
    $extraEntries = @(
        $actualEntryNames |
            Where-Object { $_ -notin $expectedEntryNames })
    if ($missingEntries.Count -gt 0 -or $extraEntries.Count -gt 0) {
        throw "ZIP manifest mismatch. Missing: $($missingEntries -join ', '); " +
            "Extra: $($extraEntries -join ', ')"
    }
}
finally {
    $archive.Dispose()
    Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
}

$archiveInfo = Get-Item -LiteralPath $ArchivePath
[pscustomobject]@{
    EditorVersion = $editorVersion
    BuildRoot = $buildRoot
    BuildDirectory = $buildDirectory
    ArchivePath = $archiveInfo.FullName
    ArchiveSizeBytes = $archiveInfo.Length
    Files = @($buildFiles.Name)
    StreamingAssets = @($requiredStreamingAssetPaths)
} | ConvertTo-Json -Depth 3
