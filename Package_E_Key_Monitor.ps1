param(
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

function Get-FullPath([string] $Path) {
    return [IO.Path]::GetFullPath($Path)
}

function Test-IsWithin([string] $Path, [string] $Parent) {
    $pathWithSeparator = $Path.TrimEnd('\') + '\'
    $parentWithSeparator = $Parent.TrimEnd('\') + '\'
    return $pathWithSeparator.StartsWith($parentWithSeparator, [StringComparison]::OrdinalIgnoreCase)
}

$sourceDirectory = Get-FullPath $PSScriptRoot
$sourceProject = Join-Path $sourceDirectory 'E_Key_Monitor.csproj'
$userGuide = Join-Path $sourceDirectory 'E键监控工具中文使用说明书.md'
if (-not (Test-Path -LiteralPath $sourceProject -PathType Leaf)) {
    throw "Project file not found: $sourceProject"
}
if (-not (Test-Path -LiteralPath $userGuide -PathType Leaf)) {
    throw "User guide not found: $userGuide"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path (Join-Path (Split-Path $sourceDirectory -Parent) 'artifacts') 'E_Key_Monitor-portable'
}

$outputDirectory = Get-FullPath $OutputDirectory
$outputLeaf = Split-Path $outputDirectory -Leaf
$sourceRoot = $sourceDirectory.TrimEnd('\') + '\'
if ([string]::IsNullOrWhiteSpace($outputLeaf) -or $outputDirectory -eq [IO.Path]::GetPathRoot($outputDirectory)) {
    throw "Unsafe output directory: $outputDirectory"
}
if ($outputDirectory.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase) -or $outputDirectory -eq $sourceDirectory) {
    throw "Output directory must be outside the source tree: $outputDirectory"
}
if ($outputLeaf -ne 'E_Key_Monitor-portable') {
    throw "Output directory must be named E_Key_Monitor-portable so cleanup is unambiguous: $outputDirectory"
}

$outputParent = Split-Path $outputDirectory -Parent
$zipPath = Join-Path $outputParent 'E_Key_Monitor-portable.zip'
$stagingDirectory = Join-Path $outputParent ('.E_Key_Monitor-publish-' + [guid]::NewGuid().ToString('N'))

function Remove-IntendedDirectory([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }
    $item = Get-Item -LiteralPath $Path -Force
    if (-not $item.PSIsContainer -or (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
        throw "Refusing to remove an unsafe existing output target: $Path"
    }
    Remove-Item -LiteralPath $Path -Recurse -Force
}

function Remove-IntendedFile([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }
    $item = Get-Item -LiteralPath $Path -Force
    if ($item.PSIsContainer -or (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
        throw "Refusing to remove an unsafe existing ZIP target: $Path"
    }
    Remove-Item -LiteralPath $Path -Force
}

try {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
    Remove-IntendedDirectory $outputDirectory
    Remove-IntendedFile $zipPath
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null

    $publishArguments = @(
        'publish', $sourceProject,
        '-c', 'Release',
        '-r', 'win-x64',
        '--self-contained', 'true',
        '-o', $stagingDirectory,
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:PublishTrimmed=false',
        '-p:PublishReadyToRun=false',
        '-p:DebugType=None',
        '-p:DebugSymbols=false'
    )
    # dotnet is not always on PATH (a fresh machine often has it only under
    # Program Files), so look there before giving up.
    $dotnetCommand = Get-Command -Name 'dotnet' -CommandType Application -ErrorAction SilentlyContinue
    if ($null -eq $dotnetCommand) {
        $dotnetFallback = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
        if (Test-Path -LiteralPath $dotnetFallback -PathType Leaf) {
            $dotnetCommand = $dotnetFallback
        }
    }

    if ($null -eq $dotnetCommand) {
        throw '找不到 dotnet。请先安装 .NET 8 SDK，或把 dotnet 加入 PATH。'
    }

    & $dotnetCommand @publishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    $publishedFiles = @(Get-ChildItem -LiteralPath $stagingDirectory -File)
    $expectedNames = @('E_Key_Monitor.exe', 'config.json')
    $unexpectedFiles = @($publishedFiles | Where-Object { $_.Name -notin $expectedNames })
    if ($unexpectedFiles.Count -gt 0 -or $publishedFiles.Count -ne 2) {
        $names = ($publishedFiles | ForEach-Object Name) -join ', '
        throw "Single-file publish contract failed. Expected E_Key_Monitor.exe and config.json only; found: $names"
    }

    $publishedExe = Join-Path $stagingDirectory 'E_Key_Monitor.exe'
    $publishedConfig = Join-Path $stagingDirectory 'config.json'
    if ((Get-Item -LiteralPath $publishedExe).Length -le 1MB) {
        throw 'Published executable is unexpectedly small and was not accepted as self-contained.'
    }
    $config = Get-Content -LiteralPath $publishedConfig -Raw | ConvertFrom-Json
    if ($config.position -ne 'MiddleLeft' -or [int]$config.activeTime -ne 200 -or $config.clickThrough -ne $true -or $null -ne $config.gameDirectory) {
        throw 'Published config.json does not preserve the required portable defaults.'
    }

    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    Copy-Item -LiteralPath $publishedExe -Destination (Join-Path $outputDirectory 'E_Key_Monitor.exe')
    Copy-Item -LiteralPath $publishedConfig -Destination (Join-Path $outputDirectory 'config.json')
    Copy-Item -LiteralPath $userGuide -Destination (Join-Path $outputDirectory 'E键监控工具中文使用说明书.md')

    $packageFiles = @(Get-ChildItem -LiteralPath $outputDirectory -File)
    $expectedPackageNames = @('E_Key_Monitor.exe', 'config.json', 'E键监控工具中文使用说明书.md')
    $unexpectedPackageFiles = @($packageFiles | Where-Object { $_.Name -notin $expectedPackageNames })
    if ($unexpectedPackageFiles.Count -gt 0 -or $packageFiles.Count -ne $expectedPackageNames.Count) {
        $names = ($packageFiles | ForEach-Object Name) -join ', '
        throw "Portable package contract failed. Expected E_Key_Monitor.exe, config.json, and the Chinese user guide; found: $names"
    }

    $packageConfig = Join-Path $outputDirectory 'config.json'
    $configItem = Get-Item -LiteralPath $packageConfig
    $configItem.IsReadOnly = $false
    $writeTest = [IO.File]::Open($packageConfig, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::Read)
    $writeTest.Dispose()

    Compress-Archive -LiteralPath @($packageFiles | ForEach-Object FullName) -DestinationPath $zipPath -CompressionLevel Optimal
    $exeHash = (Get-FileHash -LiteralPath (Join-Path $outputDirectory 'E_Key_Monitor.exe') -Algorithm SHA256).Hash
    $zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    Write-Output "Portable directory: $outputDirectory"
    Write-Output "ZIP: $zipPath"
    Write-Output "E_Key_Monitor.exe SHA-256: $exeHash"
    Write-Output "ZIP SHA-256: $zipHash"
}
finally {
    if (Test-Path -LiteralPath $stagingDirectory) {
        Remove-IntendedDirectory $stagingDirectory
    }
}
