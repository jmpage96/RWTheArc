param(
    [string]$RimWorldPath = $env:RIMWORLD_PATH,
    [switch]$Test
)
$ErrorActionPreference = 'Stop'
$workspacePath = Split-Path $PSScriptRoot -Parent
$buildArguments = @('build', (Join-Path $workspacePath 'Source\TheArk\TheArk.csproj'), '-c', 'Release')
if ($RimWorldPath) { $buildArguments += "-p:RimWorldPath=$RimWorldPath" }
& dotnet @buildArguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
if ($Test) {
    $testArguments = @('run', '--project', (Join-Path $workspacePath 'Tests\TheArk.Tests.csproj'), '-c', 'Release')
    if ($RimWorldPath) { $testArguments += "-p:RimWorldPath=$RimWorldPath" }
    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}

# Only explicitly selected mod/source files enter the release. Never copy game DLLs.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$artifactPath = Join-Path $workspacePath 'artifacts'
New-Item -ItemType Directory -Force $artifactPath | Out-Null
$archivePath = Join-Path $artifactPath 'TheArk-Milestone1.zip'
$archiveStream = [IO.File]::Open($archivePath, [IO.FileMode]::Create)
$archive = New-Object IO.Compression.ZipArchive($archiveStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    $files = @()
    foreach ($folder in @('About', 'Defs', 'Source', 'Tests', 'Tools', 'docs')) {
        $files += Get-ChildItem -LiteralPath (Join-Path $workspacePath $folder) -Recurse -File |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in @('.xml', '.cs', '.csproj', '.ps1', '.md') -and $_.Name -ne 'RimWorld.local.props' }
    }
    foreach ($name in @('Assemblies\TheArk.dll', 'README.md', 'LICENSE')) {
        $files += Get-Item -LiteralPath (Join-Path $workspacePath $name)
    }
    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($workspacePath.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, "TheArk/$relativePath") | Out-Null
    }
}
finally { $archive.Dispose(); $archiveStream.Dispose() }
Write-Host "Created $archivePath"
