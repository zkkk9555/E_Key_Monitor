[CmdletBinding()]
param(
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'

$ProjectRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)

$StandaloneLaunchScript = Join-Path $ProjectRoot 'Start_E_Key_Monitor.ps1'
$LaunchScript = Join-Path $ProjectRoot 'Start_Naraka_With_Monitor.ps1'
$StandaloneLaunchEntry = Join-Path $ProjectRoot '启动E键监控工具.bat'
$LaunchEntry = Join-Path $ProjectRoot '启动永劫无间E键提示.bat'
$PublishMonitor = Join-Path $ProjectRoot 'bin\Release\net8.0-windows\win-x64\publish\E_Key_Monitor.exe'
$RuntimeMonitor = Join-Path $ProjectRoot 'bin\Release\net8.0-windows\win-x64\E_Key_Monitor.exe'

function Exit-WithError {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host ''
    Write-Host '创建快捷方式失败：'
    Write-Host $Message
    Write-Host ''
    if (-not $NoPause) {
        [void](Read-Host '按 Enter 键退出')
    }
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
    if (-not (Test-Path -LiteralPath $StandaloneLaunchScript -PathType Leaf)) {
        Exit-WithError "找不到监控工具独立启动脚本：$StandaloneLaunchScript"
    }

    if (-not (Test-Path -LiteralPath $LaunchScript -PathType Leaf)) {
        Exit-WithError "找不到正式联合启动脚本：$LaunchScript"
    }

    if (-not (Test-Path -LiteralPath $StandaloneLaunchEntry -PathType Leaf)) {
        Exit-WithError "找不到中文监控工具入口：$StandaloneLaunchEntry"
    }

    if (-not (Test-Path -LiteralPath $LaunchEntry -PathType Leaf)) {
        Exit-WithError "找不到中文联合启动入口：$LaunchEntry"
    }

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
"@
    }

    $MonitorDir = Split-Path -Parent $MonitorExe
    $ConfigPath = Join-Path $MonitorDir 'config.json'
    $ConfiguredGame = Get-ConfiguredGameDirectory -ConfigPath $ConfigPath
    $GameDir = $ConfiguredGame[0]
    $GameLauncher = $ConfiguredGame[1]

    $Desktop = [Environment]::GetFolderPath([Environment+SpecialFolder]::Desktop)
    if ([string]::IsNullOrWhiteSpace($Desktop)) {
        Exit-WithError '无法确定当前用户桌面路径。'
    }

    $Shell = New-Object -ComObject WScript.Shell

    $MonitorShortcutPath = Join-Path $Desktop 'E键监控工具.lnk'
    $MonitorShortcut = $Shell.CreateShortcut($MonitorShortcutPath)
    $MonitorShortcut.TargetPath = $StandaloneLaunchEntry
    $MonitorShortcut.Arguments = ''
    $MonitorShortcut.WorkingDirectory = $ProjectRoot
    $MonitorShortcut.IconLocation = "$MonitorExe,0"
    $MonitorShortcut.Description = '单独启动 E 键监控悬浮工具'
    $MonitorShortcut.Save()

    $GameShortcutPath = Join-Path $Desktop '永劫无间（E键提示）.lnk'
    $GameShortcut = $Shell.CreateShortcut($GameShortcutPath)
    $GameShortcut.TargetPath = $LaunchEntry
    $GameShortcut.Arguments = ''
    $GameShortcut.WorkingDirectory = $ProjectRoot
    $GameShortcut.IconLocation = "$GameLauncher,0"
    $GameShortcut.Description = '启动 E 键监控工具和永劫无间启动器'
    $GameShortcut.Save()

    Write-Host ''
    Write-Host '已创建两个桌面快捷方式：'
    Write-Host $MonitorShortcutPath
    Write-Host $GameShortcutPath
    Write-Host ''
    if (-not $NoPause) {
        [void](Read-Host '按 Enter 键退出')
    }
    exit 0
}
catch {
    Exit-WithError $_.Exception.Message
}
