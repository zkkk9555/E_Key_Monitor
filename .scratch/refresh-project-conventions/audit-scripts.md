# Audit: scripts & build

审计对象：`C:\Project\E_Key_Monitor`（分支 main，审计时工作树干净）｜审计日期：2026-10-22｜只读。

## Summary table

| # | AGENTS.md 的声明 | 判定 | 证据 |
| --- | --- | --- | --- |
| 1 | 根目录 8 个 BAT，且全部纯 ASCII | VERIFIED | 实测 8 个 `.bat`，每个非 ASCII 字节数 = 0 |
| 2 | 根目录 5 个 PS1，且全部 UTF-8 with BOM | VERIFIED | 实测 5 个，前 3 字节均为 `EF BB BF` |
| 3 | BAT 只调用同目录 PS1 | VERIFIED | 8 个 BAT 均为 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0<同名>.ps1"` |
| 4 | 路径检查用 `Test-Path -LiteralPath` | VERIFIED | 22 处 `Test-Path` 全部带 `-LiteralPath` |
| 5 | 根 `config.json` 可解析、`gameDirectory` 为空、带 BOM | VERIFIED | 解析成功、10 字段齐、`gameDirectory=null`、前 3 字节 `EF BB BF` |
| 6 | PowerShell 5.1 静态解析无错误 | VERIFIED | 6 个 PS1（含 `tools/verify-conventions.ps1`）全部 PARSE-OK |
| 7 | 便携包只含 EXE + config.json + 中文说明书 | VERIFIED | `Package_E_Key_Monitor.ps1:95-100` 断言发布目录恰好 2 件，`:117-123` 断言便携包恰好 3 件 |
| 8 | 便携包输出在 `C:\tmp` | **WRONG** | `Package_E_Key_Monitor.ps1:27-29` 默认输出 `<源码父目录>\artifacts\E_Key_Monitor-portable` |
| 9 | SDK 8.0.422 | **WRONG** | `dotnet --version` = 8.0.426 |

## 逐项明细

### 1-2. BAT / PS1 清点与编码

```text
BAT | Create_Naraka_Monitor_Shortcut.bat | 132 B | nonAscii=0
BAT | Start_Naraka_With_Monitor.bat | 127 B | nonAscii=0
BAT | Start_Naraka_With_Monitor_Dev.bat | 131 B | nonAscii=0
BAT | 制作便携版监控工具.bat | 159 B | nonAscii=0
BAT | 启动E键监控工具.bat | 121 B | nonAscii=0
BAT | 启动永劫无间E键提示.bat | 127 B | nonAscii=0
BAT | 开发启动永劫无间E键提示.bat | 131 B | nonAscii=0
BAT | 生成中文桌面入口.bat | 132 B | nonAscii=0
PS1 | Create_Naraka_Monitor_Shortcut.ps1 | 5627 B | utf8Bom=True
PS1 | Package_E_Key_Monitor.ps1 | 6701 B | utf8Bom=True
PS1 | Start_E_Key_Monitor.ps1 | 1571 B | utf8Bom=True
PS1 | Start_Naraka_With_Monitor.ps1 | 3627 B | utf8Bom=True
PS1 | Start_Naraka_With_Monitor_Dev.ps1 | 3336 B | utf8Bom=True
```

### 3. BAT → PS1 映射

```text
Create_Naraka_Monitor_Shortcut.bat -> Create_Naraka_Monitor_Shortcut.ps1
Start_Naraka_With_Monitor.bat     -> Start_Naraka_With_Monitor.ps1
Start_Naraka_With_Monitor_Dev.bat -> Start_Naraka_With_Monitor_Dev.ps1
制作便携版监控工具.bat              -> Package_E_Key_Monitor.ps1
启动E键监控工具.bat                -> Start_E_Key_Monitor.ps1
启动永劫无间E键提示.bat             -> Start_Naraka_With_Monitor.ps1
开发启动永劫无间E键提示.bat         -> Start_Naraka_With_Monitor_Dev.ps1
生成中文桌面入口.bat                -> Create_Naraka_Monitor_Shortcut.ps1
```

### 7. 打包脚本契约（读脚本逻辑）

- `dotnet publish` 参数：`-c Release -r win-x64 --self-contained true`，另加 `-p:PublishSingleFile=true`、`-p:IncludeNativeLibrariesForSelfExtract=true`、`-p:PublishTrimmed=false`、`-p:PublishReadyToRun=false`、`-p:DebugType=None`、`-p:DebugSymbols=false`。
- 发布目录必须恰好 2 件：`E_Key_Monitor.exe`、`config.json`，否则抛错。
- EXE 体积必须 > 1MB，否则判为「不是自包含」并抛错。
- `config.json` 必须保留便携默认值：`position=MiddleLeft`、`activeTime=200`、`clickThrough=true`、`gameDirectory=null`，否则抛错。
- 便携目录必须恰好 3 件：EXE、`config.json`、中文说明书；目录名必须是 `E_Key_Monitor-portable` 且在源码树之外，否则拒绝执行。
- ZIP 用 `Compress-Archive` 打包同样 3 件，并打印 EXE 与 ZIP 的 SHA-256。

### 8. 构建与发布实测

```text
dotnet --version                                  -> 8.0.426
dotnet build -c Release                           -> exit 0, 0 警告 0 错误
dotnet publish -c Release -r win-x64 --self-contained true -> exit 0
dotnet format E_Key_Monitor.csproj --verify-no-changes --no-restore -> exit 0
```

裸 publish 的输出目录：

```text
E_Key_Monitor.exe    161816043 B   <- 单文件 self-contained
E_Key_Monitor.pdb         24128 B   <- 裸 publish 会带；打包脚本用 -p:DebugType=None 去掉
config.json                229 B
```

## 新事实（原文档没有写、现在应当写进去的）

- 裸 `dotnet publish` 会多产出一个 `.pdb`；打包脚本用 `-p:DebugType=None -p:DebugSymbols=false` 并断言发布目录恰好 2 件。
- 便携包默认输出在 `<源码父目录>\artifacts\E_Key_Monitor-portable`（不是 `C:\tmp`），并生成同名 ZIP。
- 打包脚本自带三重断言：发布目录件数、便携包件数、`config.json` 便携默认值。
- 三个根目录启动脚本会回退到 `bin\Release\net8.0-windows\win-x64\E_Key_Monitor.exe`（**多文件**自包含形态），不是便携包形态。
- `Create_Naraka_Monitor_Shortcut.ps1` 创建的快捷方式与 EXE 托盘菜单创建的**不是同一对**：它写 `E键监控工具.lnk`（指向中文 BAT）和 `永劫无间（E键提示）.lnk`（指向中文 BAT），与 EXE 侧的 `E键监控工具.lnk`（指向 EXE）、`启动StartGame和监控.lnk` 同名但不同目标。

## 必须修正的错误陈述

- `AGENTS.md:324` / `README.md:351`「便携目录和 ZIP 已在 `C:\tmp` 生成」→ 脚本默认输出在 `<源码父目录>\artifacts\`。
- `AGENTS.md:323` / `README.md:349`「SDK 8.0.422」→ 8.0.426。
- `AGENTS.md:317` 段「普通模式启动时校验 gameDirectory，取消选择会退出」→ 代码是纯独立模式弹 Yes/No/Cancel，**可跳过**。
