$ErrorActionPreference = 'Stop'

$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)

$PublishMonitor = Join-Path $ProjectRoot 'bin\Release\net8.0-windows\win-x64\publish\E_Key_Monitor.exe'
$RuntimeMonitor = Join-Path $ProjectRoot 'bin\Release\net8.0-windows\win-x64\E_Key_Monitor.exe'

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
    $MonitorExe = $null
    if (Test-Path -LiteralPath $PublishMonitor -PathType Leaf) {
        $MonitorExe = $PublishMonitor
    }
    elseif (Test-Path -LiteralPath $RuntimeMonitor -PathType Leaf) {
        $MonitorExe = $RuntimeMonitor
    }
    else {
        Exit-WithError @"
找不到 E_Key_Monitor.exe。

请先在项目目录执行发布命令：
dotnet publish -c Release -r win-x64 --self-contained true

优先查找路径：
$PublishMonitor

兼容查找路径：
$RuntimeMonitor
"@
    }

    if (-not (Test-Path -LiteralPath $MonitorExe -PathType Leaf)) {
        Exit-WithError "找不到监控工具：$MonitorExe"
    }

    $MonitorDir = Split-Path -Parent $MonitorExe
    $ConfigPath = Join-Path $MonitorDir 'config.json'
    $ConfiguredGame = Get-ConfiguredGameDirectory -ConfigPath $ConfigPath
    $GameDir = $ConfiguredGame[0]
    $GameLauncher = $ConfiguredGame[1]

    Start-Process `
        -FilePath $MonitorExe `
        -ArgumentList @('--follow-game') `
        -WorkingDirectory $MonitorDir

    Start-Process `
        -FilePath $GameLauncher `
        -WorkingDirectory $GameDir

    exit 0
}
catch {
    Exit-WithError $_.Exception.Message
}
