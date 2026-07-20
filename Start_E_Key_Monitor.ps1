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

    try {
        Add-Type -AssemblyName System.Windows.Forms
        [void][System.Windows.Forms.MessageBox]::Show(
            $Message,
            'E Key Monitor',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error)
    }
    catch {
        Write-Error $Message
    }

    exit 1
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
"@
    }

    Start-Process `
        -FilePath $MonitorExe `
        -WorkingDirectory (Split-Path -Parent $MonitorExe)

    exit 0
}
catch {
    Exit-WithError $_.Exception.Message
}
