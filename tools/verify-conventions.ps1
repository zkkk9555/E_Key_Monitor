$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

# 约定校验器：把 AGENTS.md 里写下的项目约定变成可执行检查。
# 只读仓库，只写标准输出；不联网、不安装、不写仓库外文件。
# 退出码：0 = 全部通过；1 = 至少一条 FAIL。
#
# 扫描面：仓库工作树，排除 .gitignore 里声明的生成目录；
# 安全与编码类检查只扫产品文件（额外排除 .scratch/ 工作流留痕）。
#
# 本脚本可从任意目录执行：所有路径都相对脚本位置解析，不依赖当前工作目录。

$RepoRoot = Split-Path -Parent $PSScriptRoot

$script:Failures = New-Object System.Collections.ArrayList
$script:Passes = 0

function Write-Result([string] $Name, [bool] $Ok, [string] $Detail) {
    if ($Ok) {
        $script:Passes++
        Write-Output ("PASS {0}: {1}" -f $Name, $Detail)
    }
    else {
        [void]$script:Failures.Add(("FAIL {0}: {1}" -f $Name, $Detail))
        Write-Output ("FAIL {0}: {1}" -f $Name, $Detail)
    }
}

function Get-FullPath($Path) {
    if ($Path -is [System.IO.FileInfo]) { return $Path.FullName }
    if ($Path -is [System.IO.DirectoryInfo]) { return $Path.FullName }
    return [string]$Path
}

function Get-RelativePath($FullPath) {
    $full = Get-FullPath $FullPath
    $prefix = $RepoRoot.TrimEnd('\') + '\'
    if ($full.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($prefix.Length)
    }
    return $full
}

function Get-Text($Path) {
    return [System.IO.File]::ReadAllText((Get-FullPath $Path), [System.Text.Encoding]::UTF8)
}

function Get-Lines($Path) {
    return (Get-Text $Path) -split "`r?`n"
}

function Test-HasUtf8Bom($Path) {
    $bytes = [System.IO.File]::ReadAllBytes((Get-FullPath $Path))
    if ($bytes.Length -lt 3) { return $false }
    return ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

function Get-GitExe {
    $cmd = Get-Command git -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidate = Join-Path $env:ProgramFiles 'Git\cmd\git.exe'
    if (Test-Path -LiteralPath $candidate) { return $candidate }
    return $null
}

# .gitignore 里声明的目录一律不进扫描面：它们是生成物，不该被当成仓库内容。
function Get-IgnoredDirectoryNames {
    $names = New-Object System.Collections.ArrayList
    $gitignore = Join-Path $RepoRoot '.gitignore'
    if (-not (Test-Path -LiteralPath $gitignore)) { return $names }
    foreach ($line in (Get-Lines $gitignore)) {
        $entry = $line.Trim()
        if ($entry -eq '' -or $entry.StartsWith('#') -or $entry.StartsWith('!')) { continue }
        # 只有以 / 结尾的条目才是目录；其余是文件或通配模式。
        if (-not $entry.EndsWith('/')) { continue }
        $entry = $entry.TrimEnd('/')
        if ($entry -match '[*?]') { continue }
        # 字符组只取第一个候选：[Bb]in -> Bin。
        $entry = $entry -replace '\[([A-Za-z])\]', '$1'
        if ($entry -ne '') { [void]$names.Add($entry.ToLowerInvariant()) }
    }
    return $names
}

function Test-IsIgnoredPath([string] $RelativePath, $IgnoredNames) {
    foreach ($name in $IgnoredNames) {
        if ($RelativePath -like ($name + '\*')) { return $true }
        if ($RelativePath -like ('*\' + $name + '\*')) { return $true }
    }
    return $false
}

function Get-RepoFilePaths([switch] $ProductOnly) {
    $ignoredNames = Get-IgnoredDirectoryNames
    $files = New-Object System.Collections.ArrayList
    foreach ($f in Get-ChildItem -LiteralPath $RepoRoot -File -Recurse -Force) {
        $rel = Get-RelativePath $f.FullName
        if ($rel -like '.git\*') { continue }
        if (Test-IsIgnoredPath $rel $ignoredNames) { continue }
        if ($ProductOnly -and $rel -like '.scratch\*') { continue }
        [void]$files.Add($f.FullName)
    }
    return $files
}

# 解析文档里的目录树代码块：定位标题后的第一个 ```text 块，逐行取路径。
function Get-DocumentTree($Path, [string] $HeadingPattern) {
    $entries = New-Object System.Collections.ArrayList
    $headingSeen = $false
    $inTree = $false
    $treeFound = $false
    foreach ($line in (Get-Lines $Path)) {
        if (-not $headingSeen) {
            if ($line -match $HeadingPattern) { $headingSeen = $true }
            continue
        }
        if (-not $inTree) {
            if ($line -match '^\s*```text\s*$') { $inTree = $true }
            continue
        }
        if ($line -match '^\s*```\s*$') { $treeFound = $true; break }
        if ($line -match '^E_Key_Monitor/\s*$') { continue }
        if ($line -match '^[\s\u2502\u251c\u2514\u2500]*([A-Za-z0-9_.\u4e00-\u9fff][^\s#]*)') {
            $entry = $Matches[1].TrimEnd('/')
            if ($entry -ne '') { [void]$entries.Add($entry) }
        }
    }
    if (-not $treeFound) { return $null }
    return $entries
}

# 取出脚本里声明的文件名清单，例如 $expectedPackageNames = @('a','b')。
function Get-DeclaredNames([string] $ScriptText, [string] $VariableName) {
    $pattern = [regex]::Escape($VariableName) + '\s*=\s*@\(([^)]*)\)'
    $match = [regex]::Match($ScriptText, $pattern)
    if (-not $match.Success) { return @() }
    $found = New-Object System.Collections.ArrayList
    foreach ($m in [regex]::Matches($match.Groups[1].Value, "'([^']+)'")) {
        [void]$found.Add($m.Groups[1].Value)
    }
    return $found
}

Write-Output '=== verify-conventions ==='

# --- C1 bat-ascii: 根目录 BAT 固定 8 个且必须纯 ASCII ---
$expectedBats = @(
    'Start_Naraka_With_Monitor.bat',
    'Start_Naraka_With_Monitor_Dev.bat',
    'Create_Naraka_Monitor_Shortcut.bat',
    '启动E键监控工具.bat',
    '启动永劫无间E键提示.bat',
    '开发启动永劫无间E键提示.bat',
    '生成中文桌面入口.bat',
    '制作便携版监控工具.bat'
)
$actualBats = @(Get-ChildItem -LiteralPath $RepoRoot -Filter '*.bat' -File | ForEach-Object { $_.Name })
$missingBats = @($expectedBats | Where-Object { $_ -notin $actualBats })
$extraBats = @($actualBats | Where-Object { $_ -notin $expectedBats })
$nonAsciiBats = @()
foreach ($bat in $actualBats) {
    $bytes = [System.IO.File]::ReadAllBytes((Join-Path $RepoRoot $bat))
    $nonAsciiCount = @($bytes | Where-Object { $_ -gt 127 }).Count
    if ($nonAsciiCount -gt 0) { $nonAsciiBats += ("{0}({1} 个非 ASCII 字节)" -f $bat, $nonAsciiCount) }
}
$batProblems = @()
if ($missingBats.Count -gt 0) { $batProblems += ("缺少 BAT: " + ($missingBats -join ', ')) }
if ($extraBats.Count -gt 0) { $batProblems += ("多出 BAT: " + ($extraBats -join ', ')) }
if ($nonAsciiBats.Count -gt 0) { $batProblems += ("非 ASCII: " + ($nonAsciiBats -join ', ')) }
$batDetail = if ($batProblems.Count -eq 0) { "{0} 个根目录 BAT 全部纯 ASCII" -f $actualBats.Count } else { $batProblems -join '; ' }
Write-Result 'bat-ascii' ($batProblems.Count -eq 0) $batDetail

# --- C2 ps1-bom: 产品 .ps1 必须是 UTF-8 with BOM ---
$ps1Files = @(Get-RepoFilePaths -ProductOnly | Where-Object { $_.ToLower().EndsWith('.ps1') })
$ps1WithoutBom = @($ps1Files | Where-Object { -not (Test-HasUtf8Bom $_) } | ForEach-Object { Get-RelativePath $_ })
$ps1Detail = if ($ps1WithoutBom.Count -eq 0) { "{0} 个产品 PS1 均为 UTF-8 with BOM" -f $ps1Files.Count } else { ("缺少 BOM: " + ($ps1WithoutBom -join ', ')) }
Write-Result 'ps1-bom' ($ps1WithoutBom.Count -eq 0) $ps1Detail

# --- C3 config-fields: 根 config.json 字段齐且 gameDirectory 为空 ---
$configPath = Join-Path $RepoRoot 'config.json'
$expectedFields = @('key', 'activeTime', 'windowSize', 'opacity', 'dotSize', 'position', 'customX', 'customY', 'clickThrough', 'gameDirectory')
$configOk = $false
$configDetail = 'config.json 不可解析'
try {
    $config = Get-Text $configPath | ConvertFrom-Json
    $actualFields = @($config.PSObject.Properties.Name)
    $missingFields = @($expectedFields | Where-Object { $_ -notin $actualFields })
    $extraFields = @($actualFields | Where-Object { $_ -notin $expectedFields })
    if ($missingFields.Count -eq 0 -and $extraFields.Count -eq 0 -and $null -eq $config.gameDirectory) {
        $configOk = $true
        $configDetail = '10 个字段齐备，gameDirectory 为 null'
    }
    else {
        $configDetail = ("缺少字段: " + ($missingFields -join ', ') + "; 多出字段: " + ($extraFields -join ', ') + "; gameDirectory=" + $config.gameDirectory)
    }
}
catch { $configDetail = $_.Exception.Message }
Write-Result 'config-fields' $configOk $configDetail

# --- C4/C5 doc-tree: AGENTS.md 的文件职责树必须与真实仓库一致 ---
$agentsPath = Join-Path $RepoRoot 'AGENTS.md'
$treeEntries = Get-DocumentTree $agentsPath '^#+.*当前文件职责'
if ($null -eq $treeEntries) {
    Write-Result 'doc-tree-exists' $false 'AGENTS.md 里找不到「当前文件职责」的目录树代码块'
    Write-Result 'doc-tree-covers' $false '没有目录树可比对'
}
else {
    $missingOnDisk = @()
    foreach ($entry in $treeEntries) {
        $entryNorm = $entry -replace '/', '\'
        if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $entryNorm))) { $missingOnDisk += $entry }
    }
    $treeDetail = if ($missingOnDisk.Count -eq 0) { "{0} 个树条目全部存在" -f $treeEntries.Count } else { ("树上写了但磁盘上没有: " + ($missingOnDisk -join ', ')) }
    Write-Result 'doc-tree-exists' ($missingOnDisk.Count -eq 0) $treeDetail

    $uncovered = @()
    foreach ($file in (Get-RepoFilePaths)) {
        $rel = Get-RelativePath $file
        $covered = $false
        foreach ($entry in $treeEntries) {
            $entryNorm = $entry -replace '/', '\'
            if ($rel -eq $entryNorm -or $rel.StartsWith($entryNorm + '\')) { $covered = $true; break }
        }
        if (-not $covered) { $uncovered += $rel }
    }
    $coverDetail = if ($uncovered.Count -eq 0) { '仓库中每个文件都被文件职责树覆盖' } else { ("未被树覆盖的文件: " + ($uncovered -join ', ')) }
    Write-Result 'doc-tree-covers' ($uncovered.Count -eq 0) $coverDetail
}

# --- C6 doc-trees-match: README 的结构树必须与 AGENTS.md 的同一份 ---
$readmePath = Join-Path $RepoRoot 'README.md'
$readmeTree = Get-DocumentTree $readmePath '^#+.*项目结构'
$treeMatchOk = $false
$treeMatchDetail = 'README.md 里找不到「项目结构」的目录树代码块'
if ($null -ne $readmeTree -and $null -ne $treeEntries) {
    $onlyAgents = @($treeEntries | Where-Object { $_ -notin $readmeTree })
    $onlyReadme = @($readmeTree | Where-Object { $_ -notin $treeEntries })
    if ($onlyAgents.Count -eq 0 -and $onlyReadme.Count -eq 0) {
        $treeMatchOk = $true
        $treeMatchDetail = "两棵树条目一致（{0} 条）" -f $treeEntries.Count
    }
    else {
        $treeMatchDetail = ("只在 AGENTS.md: " + ($onlyAgents -join ', ') + "; 只在 README.md: " + ($onlyReadme -join ', '))
    }
}
Write-Result 'doc-trees-match' $treeMatchOk $treeMatchDetail

# --- C7 no-pinned-sha: 规范文档不得钉住会腐烂的提交号 ---
$normativeDocs = @('AGENTS.md', 'README.md', 'E键监控工具中文使用说明书.md')
$gitExe = Get-GitExe
$shaHits = @()
if (-not $gitExe) {
    $shaHits = @('git 不可用，无法核查提交号')
}
else {
    $full = @(& $gitExe -C $RepoRoot log --all --pretty=format:%H 2>$null)
    $short = @(& $gitExe -C $RepoRoot log --all --pretty=format:%h 2>$null)
    $tokens = @($full) + @($short) | Sort-Object -Unique
    foreach ($doc in $normativeDocs) {
        $docPath = Join-Path $RepoRoot $doc
        if (-not (Test-Path -LiteralPath $docPath)) { $shaHits += ("缺少文档: " + $doc); continue }
        $text = Get-Text $docPath
        foreach ($token in $tokens) {
            if ($token.Length -ge 7 -and $text.Contains($token)) { $shaHits += ("{0} 含有真实提交号 {1}" -f $doc, $token) }
        }
        # 提交号形态的十六进制串一律不出现：即便它不是本仓真实提交，也不该被写进规范。
        foreach ($m in [regex]::Matches($text, '(?<![0-9A-Za-z_-])[0-9a-f]{7,40}(?![0-9A-Za-z_-])')) {
            $candidate = $m.Value
            if ($candidate -match '[0-9]') { $shaHits += ("{0} 含有疑似提交号 {1}" -f $doc, $candidate) }
        }
    }
}
$shaDetail = if ($shaHits.Count -eq 0) { '规范文档未钉住任何提交号' } else { ($shaHits | Sort-Object -Unique) -join '; ' }
Write-Result 'no-pinned-sha' ($shaHits.Count -eq 0) $shaDetail

# --- C8 no-old-skill-name / C9 skill-credential-explicit ---
$oldName = 'auto' + 'pilot'
$oldNameHits = @()
foreach ($file in (Get-RepoFilePaths -ProductOnly)) {
    $ext = [System.IO.Path]::GetExtension($file).ToLower()
    if ($ext -eq '.exe' -or $ext -eq '.dll' -or $ext -eq '.png' -or $ext -eq '.ico' -or $ext -eq '.pdb') { continue }
    $rel = Get-RelativePath $file
    try { $text = Get-Text $file } catch { continue }
    if ($text.Contains($oldName)) { $oldNameHits += $rel }
}
$oldDetail = if ($oldNameHits.Count -eq 0) { '产品文件里没有旧 skill 名' } else { ("仍含旧 skill 名: " + ($oldNameHits -join ', ')) }
Write-Result 'no-old-skill-name' ($oldNameHits.Count -eq 0) $oldDetail

$credentialLines = @((Get-Lines $agentsPath) | Where-Object { $_ -match '^\s*automatt:\s*(on|off)\s*$' })
$credDetail = if ($credentialLines.Count -eq 1) { "AGENTS.md 显式声明凭据行: " + $credentialLines[0].Trim() } else { ("AGENTS.md 中应恰好有 1 行 automatt: on|off，实际 " + $credentialLines.Count + " 行") }
Write-Result 'skill-credential-explicit' ($credentialLines.Count -eq 1) $credDetail

# --- C10 safety-boundary: 静态安全边界零命中 ---
$forbiddenApi = @(
    'SendInput', 'keybd_event', 'mouse_event',
    'WriteProcessMemory', 'VirtualAllocEx', 'CreateRemoteThread', 'OpenProcess', 'TerminateProcess',
    'WH_MOUSE', 'GetProcessById',
    'HttpClient', 'TcpClient', 'UdpClient', 'Socket'
)
$safetyHits = @()
$csFiles = @(Get-RepoFilePaths -ProductOnly | Where-Object { $_.ToLower().EndsWith('.cs') })
foreach ($file in $csFiles) {
    $rel = Get-RelativePath $file
    $lines = Get-Lines $file
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = [string]$lines[$i]
        foreach ($token in $forbiddenApi) {
            if ($line.Contains($token)) { $safetyHits += ("{0}:{1} 命中 {2}" -f $rel, ($i + 1), $token) }
        }
        if ($line.Contains('.Kill(')) { $safetyHits += ("{0}:{1} 出现终止进程调用" -f $rel, ($i + 1)) }
        # 启动目标只能是 StartGame.exe：拼游戏本体路径的 Path.Combine 必须是存在性校验，
        # 否则就可能被改成直接启动游戏本体。
        if ($line.Contains('Path.Combine') -and $line.Contains('NarakaBladepoint.exe') -and -not $line.Contains('IsRegularFile')) {
            if (-not ($line.Contains('StartGame.exe') -or $line.Contains('IsRegularFile'))) {
                $safetyHits += ("{0}:{1} 拼出了游戏本体路径且不是存在性校验" -f $rel, ($i + 1))
            }
        }
        if ($line.Contains('NarakaBladepoint.exe') -and ($line.Contains('Process.Start') -or $line.Contains('StartInfo') -or $line.Contains('FileName'))) {
            $safetyHits += ("{0}:{1} 疑似直接启动游戏本体" -f $rel, ($i + 1))
        }
    }
}
$scriptFiles = @(Get-ChildItem -LiteralPath $RepoRoot -File | Where-Object { $_.Extension -eq '.ps1' -or $_.Extension -eq '.bat' })
foreach ($file in $scriptFiles) {
    $rel = Get-RelativePath $file
    $lines = Get-Lines $file
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = [string]$lines[$i]
        if ($line -match '[A-Za-z]:\\') { $safetyHits += ("{0}:{1} 根目录入口脚本里硬编码了绝对路径" -f $rel, ($i + 1)) }
    }
}
$safetyDetail = if ($safetyHits.Count -eq 0) { '无输入模拟 / 鼠标 Hook / 进程注入 / 固定 PID / 终止进程 / 网络 / 直启游戏本体 / 脚本硬编码绝对路径' } else { $safetyHits -join '; ' }
Write-Result 'safety-boundary' ($safetyHits.Count -eq 0) $safetyDetail

# --- C11 portable-contract: 单文件发布与便携包内容契约 ---
$csprojText = Get-Text (Join-Path $RepoRoot 'E_Key_Monitor.csproj')
$packageText = Get-Text (Join-Path $RepoRoot 'Package_E_Key_Monitor.ps1')
$portableProblems = @()
if ($csprojText -notmatch '<PublishSingleFile>\s*true\s*</PublishSingleFile>') { $portableProblems += 'csproj 未设置 PublishSingleFile=true' }
if ($csprojText -notmatch '<IncludeNativeLibrariesForSelfExtract>\s*true\s*</IncludeNativeLibrariesForSelfExtract>') { $portableProblems += 'csproj 未设置 IncludeNativeLibrariesForSelfExtract=true' }
if ($csprojText -notmatch '<PublishTrimmed>\s*false\s*</PublishTrimmed>') { $portableProblems += 'csproj 未设置 PublishTrimmed=false' }
$publishedNames = @(Get-DeclaredNames $packageText 'expectedNames') | Sort-Object
$expectedPublished = @('E_Key_Monitor.exe', 'config.json') | Sort-Object
if (($publishedNames -join '|') -ne ($expectedPublished -join '|')) {
    $portableProblems += ("打包脚本的发布产物契约不是 EXE + config.json 两件（实际: " + ($publishedNames -join ', ') + ")")
}
$packageNames = @(Get-DeclaredNames $packageText 'expectedPackageNames') | Sort-Object
$expectedPackage = @('E_Key_Monitor.exe', 'config.json', 'E键监控工具中文使用说明书.md') | Sort-Object
if (($packageNames -join '|') -ne ($expectedPackage -join '|')) {
    $portableProblems += ("打包脚本的便携包契约不是三件（实际: " + ($packageNames -join ', ') + ")")
}
$portableDetail = if ($portableProblems.Count -eq 0) { '单文件发布与便携包三件套契约保持不变' } else { $portableProblems -join '; ' }
Write-Result 'portable-contract' ($portableProblems.Count -eq 0) $portableDetail

# --- C12 adr-pointer: 决策落点存在，且 domain.md 列出的 ADR 逐个可达 ---
$adrDir = Join-Path $RepoRoot 'docs\adr'
$adrFiles = @()
if (Test-Path -LiteralPath $adrDir) { $adrFiles = @(Get-ChildItem -LiteralPath $adrDir -Filter '*.md' -File) }
$pointerProblems = @()
if ($adrFiles.Count -eq 0) { $pointerProblems += 'docs/adr/ 不存在或没有 ADR' }
$domainPath = Join-Path $RepoRoot 'docs\agents\domain.md'
$domainText = Get-Text $domainPath
if (-not $domainText.Contains('docs/adr/')) { $pointerProblems += 'docs/agents/domain.md 没有指向 docs/adr/' }
$referencedAdrs = @([regex]::Matches($domainText, 'docs/adr/([0-9A-Za-z_\-]+\.md)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
if ($referencedAdrs.Count -eq 0) { $pointerProblems += 'docs/agents/domain.md 没有列出任何具体 ADR 文件' }
foreach ($adr in $referencedAdrs) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot ("docs\adr\" + $adr)))) { $pointerProblems += ("domain.md 指向的 ADR 不存在: " + $adr) }
}
$pointerDetail = if ($pointerProblems.Count -eq 0) { "{0} 条 ADR，domain.md 列出的 {1} 个指针全部可达" -f $adrFiles.Count, $referencedAdrs.Count } else { $pointerProblems -join '; ' }
Write-Result 'adr-pointer' ($pointerProblems.Count -eq 0) $pointerDetail

Write-Output ''
Write-Output ("=== verify-conventions: {0} passed, {1} failed ===" -f $script:Passes, $script:Failures.Count)
if ($script:Failures.Count -gt 0) { exit 1 }
exit 0
