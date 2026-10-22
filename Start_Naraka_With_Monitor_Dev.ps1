$ErrorActionPreference = 'Stop'

$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)

$ProjectFile = Join-Path $ProjectRoot 'E_Key_Monitor.csproj'
$ConfigPath = Join-Path $ProjectRoot 'bin\Debug\net8.0-windows\config.json'

function Exit-WithError {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host ''
    Write-Host '启动失败：'
    Write-Host $Message
    Write-Host ''
    [void](Read-Host '按 Enter 键退出')
    exit 1
}

function Get-ConfiguredGameDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath
    )

    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        throw "找不到配置文件：$ConfigPath"
    }

    try {
        $Config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
    }
    catch {
        throw "配置文件格式无效：$ConfigPath"
    }

    $GameDirectoryProperty = $Config.PSObject.Properties['gameDirectory']
    if ($null -eq $GameDirectoryProperty -or [string]::IsNullOrWhiteSpace([string]$GameDirectoryProperty.Value)) {
        throw '配置文件中的 gameDirectory 未设置。请先在监控工具中选择游戏目录。'
    }

    $ConfiguredPath = [string]$GameDirectoryProperty.Value
    if ($ConfiguredPath -notmatch '^(?:[A-Za-z]:[\\/]|\\\\)') {
        throw "gameDirectory 必须是绝对路径：$ConfiguredPath"
    }

    try {
        $GameDir = [System.IO.Path]::GetFullPath($ConfiguredPath)
    }
    catch {
        throw "gameDirectory 路径无效：$ConfiguredPath"
    }

    $GameLauncher = Join-Path $GameDir 'StartGame.exe'
    $GameExe = Join-Path $GameDir 'NarakaBladepoint.exe'
    if (-not (Test-Path -LiteralPath $GameDir -PathType Container)) {
        throw "找不到游戏目录：$GameDir"
    }
    if (-not (Test-Path -LiteralPath $GameLauncher -PathType Leaf)) {
        throw "找不到游戏启动器：$GameLauncher"
    }
    if (-not (Test-Path -LiteralPath $GameExe -PathType Leaf)) {
        throw "找不到游戏本体：$GameExe"
    }

    return @($GameDir, $GameLauncher)
}

try {
    $DotNetCommand = Get-Command -Name 'dotnet' -CommandType Application -ErrorAction SilentlyContinue
    if ($null -eq $DotNetCommand) {
        $dotnetFallback = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
        if (Test-Path -LiteralPath $dotnetFallback -PathType Leaf) {
            $DotNetCommand = $dotnetFallback
        }
    }

    $DotNetCommandPath = if ($DotNetCommand -is [string]) { $DotNetCommand } else { $DotNetCommand.Source }
    if ($null -eq $DotNetCommand) {
        Exit-WithError '找不到 dotnet。请先安装 .NET 8 SDK，并确认 dotnet 可以在命令行中运行。'
    }

    if (-not (Test-Path -LiteralPath $ProjectFile -PathType Leaf)) {
        Exit-WithError "找不到项目文件：$ProjectFile"
    }

    $ConfiguredGame = Get-ConfiguredGameDirectory -ConfigPath $ConfigPath
    $GameDir = $ConfiguredGame[0]
    $GameLauncher = $ConfiguredGame[1]

    Start-Process `
        -FilePath $DotNetCommandPath `
        -ArgumentList @(
            'run',
            '--project',
            "`"$ProjectFile`"",
            '--',
            '--follow-game'
        ) `
        -WorkingDirectory $ProjectRoot

    Start-Process `
        -FilePath $GameLauncher `
        -WorkingDirectory $GameDir

    exit 0
}
catch {
    Exit-WithError $_.Exception.Message
}
