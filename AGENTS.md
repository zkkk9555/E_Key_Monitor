# AGENTS.md

## 先读这里

本文件描述 `E_Key_Monitor` 当前真实实现，是后续 AI 阅读、诊断和修改项目时的项目级事实来源。

- 唯一有效的源码目录：`C:\Project\E_Key_Monitor`
- 不要把其他位置的 `E_Key_Monitor` 副本当作源码。
- E 盘上的项目副本是用户保留的备份，不要读取、修改、构建、同步或覆盖它。
- E 盘同时包含真实游戏安装目录；只有下文明确列出的游戏路径属于正常配置。
- 当前项目目录是可用的 Git 工作树；当前分支和提交信息必须以实际 `git status`、分支和日志命令为准。不要覆盖或清理现有未提交修改。
- `bin/`、`obj/`、`.appdata/`、`.dotnet-home/` 和 `.nuget/` 是构建或工具生成内容，不是源代码。

## 项目用途

`E_Key_Monitor` 是一个 Windows 桌面悬浮提示工具，用于显示全局 `E` 键最近是否被触发。

用户玩《永劫无间》时，会使用鼠标宏高频触发 `E` 键拾取物品。如果进入战斗后忘记关闭宏，持续的 `E` 输入会影响正常操作。这个工具通过屏幕上的小圆点直观提示宏是否还在触发 `E`：

- 未触发：偏白色圆点。
- 最近触发过：高亮青绿色圆点。
- 高频持续触发：青绿色持续亮起，不按每次按键闪烁。
- 停止触发约 `200ms`：恢复偏白色。

工具只负责观察和提示，不负责产生、拦截或修改输入。

## 安全边界

项目允许：

- 使用 `WH_KEYBOARD_LL` 全局键盘 Hook 被动观察 `E` 键。
- 根据最近一次 `E` 键触发时间更新悬浮圆点。
- 判断指定进程名是否存在，以便随游戏退出。
- 启动用户原有的游戏启动器 `StartGame.exe`。

项目禁止：

- 模拟或发送键盘、鼠标输入。
- 拦截、屏蔽或修改 `E` 键。
- 安装全局鼠标 Hook。
- 读取游戏内存、注入游戏进程或修改游戏文件。
- 修改网络封包或绕过反作弊。
- 直接控制、终止或自动化操作 `NarakaBladepoint.exe`。
- 使用某一次运行产生的固定 PID。

## 技术栈

- C#
- .NET 8
- WinForms
- Windows x64
- 不使用第三方 NuGet 包

不要改成 WPF、Avalonia、Electron、Python 或其他技术栈。保持项目小型、直接、适合阅读。

## 当前文件职责

```text
E_Key_Monitor/
├── .gitignore                            # 构建产物、本地状态和敏感文件排除规则
├── E_Key_Monitor.csproj                  # .NET 8 WinForms 项目定义
├── E_Key_Monitor.ico                     # EXE、托盘和独立快捷方式的多尺寸图标
├── E_Key_Monitor-icon.png                # 图标设计预览
├── Program.cs                            # 解析 --follow-game 并启动 MainForm
├── MainForm.cs                           # 配置、Hook、绘制、托盘、拖动、联合启动、快捷方式、游戏跟随和清理
├── config.json                           # 默认配置模板
├── 启动E键监控工具.bat                    # 中文独立监控入口，调用正式 PS1
├── 启动永劫无间E键提示.bat                # 中文正式联合启动入口
├── 开发启动永劫无间E键提示.bat            # 中文开发联合启动入口
├── 生成中文桌面入口.bat                   # 中文桌面快捷方式生成入口
├── 制作便携版监控工具.bat                 # 中文便携版打包入口
├── Package_E_Key_Monitor.ps1              # 生成便携版单文件包
├── Start_E_Key_Monitor.ps1               # 独立启动正式监控工具
├── Start_Naraka_With_Monitor.bat         # 正式启动的 ASCII 包装器
├── Start_Naraka_With_Monitor.ps1         # 正式联合启动逻辑
├── Start_Naraka_With_Monitor_Dev.bat     # 开发启动的 ASCII 包装器
├── Start_Naraka_With_Monitor_Dev.ps1     # dotnet run 联合启动逻辑
├── Create_Naraka_Monitor_Shortcut.bat    # 两个桌面快捷方式的创建入口
├── Create_Naraka_Monitor_Shortcut.ps1    # 创建独立监控与游戏联合启动快捷方式
├── E键监控工具中文使用说明书.md           # 面向用户的中文入口说明书
├── docs/superpowers/                     # 计划和设计记录
├── README.md                             # 面向用户的使用说明
└── AGENTS.md                             # 面向 AI 和维护者的当前项目说明
```

`.lnk` 是生成产物，包含绝对路径，不能作为可移植源码或路径事实。当前源码目录没有 `.lnk` 文件；项目移动后应重新生成快捷方式。

最终便携包只包含：

```text
E_Key_Monitor.exe
config.json
E键监控工具中文使用说明书.md
```

根目录的 BAT、PS1 和开发记录是源码维护文件，不进入最终便携包，也不是普通用户的日常入口。

## 应用图标

项目使用自定义多尺寸 `E_Key_Monitor.ico`，设计为深色圆角底、偏白色 `E`、高亮青绿色描边与状态点。图标必须在 16px 托盘尺寸下仍可辨识。

- `E_Key_Monitor.csproj` 通过 `ApplicationIcon` 将 ICO 嵌入 Windows EXE。
- `MainForm` 从当前可执行文件提取图标供 `NotifyIcon` 使用，失败时才回退到系统默认图标。
- 提取出的 `Icon` 由 `CleanupResources` 释放。
- `E键监控工具` 快捷方式使用监控 EXE 的新图标。
- `启动StartGame和监控` 联合快捷方式使用当前监控 EXE 图标，便于保证便携包移动后仍可直接使用。
- `E_Key_Monitor-icon.png` 仅作为文档和设计预览，不参与运行。

## 程序入口和运行模式

`Program.cs` 读取命令行参数：

- 不带参数：普通独立模式。
- 带 `--follow-game`：游戏跟随模式。
- 带 `--launch-game`：游戏跟随模式，并在目录验证后启动原始 `StartGame.exe`。

所有模式都会创建同一个 `MainForm`。普通模式不观察游戏进程，也不会因游戏退出自动关闭，但启动时会校验 `gameDirectory`；配置缺失或无效时会显示中文目录选择框，取消选择会退出。目录有效后，即使游戏进程不存在也可以运行，需要用户从托盘菜单退出。`--follow-game` 只等待和跟随游戏进程；`--launch-game` 还会以验证目录为工作目录启动原始 `StartGame.exe`。

## 全局 E 键监听

当前实现位于 `MainForm.cs`：

- Hook 类型：`SetWindowsHookEx(WH_KEYBOARD_LL)`。
- 目标虚拟键：`VK_E = 0x45`。
- 观察消息：`WM_KEYDOWN` 和 `WM_SYSKEYDOWN`。
- Hook 回调只记录 `Environment.TickCount64`，保持轻量。
- 无论是否检测到 `E`，都必须调用 `CallNextHookEx`。
- `System.Windows.Forms.Timer` 每 `50ms` 检查一次状态。
- 最近触发时间不超过 `activeTime` 时为激活状态，默认 `200ms`。
- 只有状态发生变化时才重新绘制圆点。
- 退出时必须调用 `UnhookWindowsHookEx`。

不要在 Hook 回调中增加磁盘访问、复杂计算、网络访问或 UI 阻塞操作。

## 悬浮窗和绘制

窗口是无边框、置顶、不显示任务栏按钮、不激活输入焦点的 WinForms 工具窗口。

当前实现不是普通 `OnPaint` 绘制，而是：

- 使用 `WS_EX_LAYERED`、`WS_EX_NOACTIVATE` 和 `WS_EX_TOOLWINDOW`。
- 使用 32 位预乘 Alpha DIB。
- 使用 `Graphics` 的抗锯齿和高质量像素偏移绘制圆形。
- 使用 `UpdateLayeredWindow` 提交逐像素透明窗口。
- 正常背景完全透明，只显示圆点。
- 初次绘制失败会写入诊断日志并安全退出。
- GDI 的 DC、Bitmap 和选入对象必须按现有顺序释放。

默认视觉配置：

- 窗口：`40 x 40`
- 圆点：中号，`26px`
- 未激活偏白色：`ARGB(255, 235, 235, 235)`
- 激活高亮青绿色：`ARGB(255, 65, 255, 145)`
- 全局透明度：`0.8`
- 默认位置：左中

圆点大小：小 `18px`、中 `26px`、大 `34px`。

## 鼠标穿透、菜单和拖动

当前真实需求是默认启用鼠标穿透，`config.json` 中为：

```json
"clickThrough": true
```

默认状态下，圆点的左键和右键都会穿透到后方窗口，避免影响游戏操作。用户通过系统托盘菜单关闭“鼠标穿透”后：

- 左键按住圆点可以拖动。
- 右键圆点可以打开与托盘相同的设置菜单。
- 拖动后的坐标保存为 `Custom` 位置。
- 重新启用穿透前会取消未完成的拖动。

托盘菜单和圆点菜单需要保持勾选状态同步。位置预设依次为右上、右中、右下、左下、左中、左上。

不要把默认行为改回“不穿透”。如果需要移动圆点，应先从托盘关闭穿透，移动完成后再开启。

## 配置读取和保存

根目录 `config.json` 是构建时复制到输出目录的默认模板。程序实际读取和保存的是：

```text
AppContext.BaseDirectory\config.json
```

也就是当前正在运行的 EXE 或 DLL 旁边的配置，而不一定是项目根目录的文件。

当前配置包含中文游戏路径，根目录模板及构建输出应保持 UTF-8 with BOM，以兼容 Windows PowerShell 5.1 的默认读取行为。

当前字段：

```json
{
  "key": "E",
  "activeTime": 200,
  "windowSize": 40,
  "opacity": 0.8,
  "dotSize": "Medium",
  "position": "MiddleLeft",
  "customX": null,
  "customY": null,
  "clickThrough": true,
  "gameDirectory": null
}
```

说明：

- `key` 为观察键（单个 A-Z/0-9，不区分大小写，默认 `E`），`MonitorSettings.TryParseObserveKey` 解析归一化，非法值回退 `E`；`HookCallback` 按解析后的 VK 比较，托盘标题显示当前键。
- `gameDirectory` 保存经验证的游戏目录绝对路径；根目录模板默认是 `null`，首次启动选择成功后保存，路径失效时重新选择。
- 无配置、JSON 损坏或字段无效时回退到安全默认值。
- 保存配置失败只能记录诊断，不能让程序崩溃。

## 游戏跟随状态机

`--follow-game` 只观察进程名：

```text
NarakaBladepoint
```

准确流程：

1. 监控工具启动后，每约 `1` 秒调用 `Process.GetProcessesByName`。
2. 第一次发现 `NarakaBladepoint` 前，最多等待约 `10` 分钟。
3. 发现游戏后记录 `_gameSeen = true`。
4. `StartGame.exe` 是否继续存在不影响监控工具。
5. 游戏进程消失后开始计算宽限时间。
6. 连续约 `5` 秒未发现游戏进程后调用 `RequestShutdown`。
7. 每次枚举出的 `Process` 对象都必须 `Dispose`。

不要改成观察 `StartGame`，不要使用固定 PID，也不要直接启动 `NarakaBladepoint.exe`。

## 联合启动逻辑

当前验证环境的游戏路径（启动脚本不硬编码此路径）：

```text
游戏目录：E:\Youxi\网吧极速版\program
启动器：  E:\Youxi\网吧极速版\program\StartGame.exe
游戏本体：E:\Youxi\网吧极速版\program\NarakaBladepoint.exe
进程名：  NarakaBladepoint
```

联合启动前，正在使用的监控 EXE 旁边的 `config.json` 必须保存一个同时包含两个游戏 EXE 的有效目录。监控工具菜单中的“设置游戏目录...”负责选择、验证和保存目录；默认配置为空，路径失效时下次启动会再次要求选择。

正式联合启动流程：

1. 桌面快捷方式直接启动当前 `E_Key_Monitor.exe --launch-game`。
2. EXE 验证旁边的 `config.json`、游戏目录和两个游戏文件。
3. EXE 以游戏目录为工作目录启动原始 `StartGame.exe`。
4. EXE 同时启用 `NarakaBladepoint` 游戏跟随模式。
5. 不直接启动 `NarakaBladepoint.exe`。
6. 游戏结束后的清理由监控工具负责。

Windows 不会因为用户直接双击原始 `StartGame.exe` 而自动启动监控工具。用户应使用程序生成的“启动StartGame和监控”快捷方式。

源码目录还保留以下脚本作为开发和兼容入口；它们不属于最终便携包，也不是普通用户的日常入口：

- 旧中文 BAT 和英文 PS1：保留用于维护，不参与便携包运行。
- `E_Key_Monitor.exe`：最终便携包中的唯一业务 EXE。

## 桌面快捷方式

运行中的 `E_Key_Monitor.exe` 通过托盘菜单“生成桌面快捷方式”创建或覆盖两个当前用户桌面快捷方式：

```text
E键监控工具
启动StartGame和监控
```

- `E键监控工具` 直接指向当前 EXE，不带参数。
- `启动StartGame和监控` 直接指向当前 EXE，参数为 `--launch-game`。
- 两个快捷方式都使用当前 EXE 图标，工作目录为 EXE 所在目录。
- 便携文件夹移动后，必须从当前 EXE 的托盘菜单重新生成快捷方式。

## 脚本编码规则

根目录全部 8 个 BAT 必须始终保持纯 ASCII：

- `Start_Naraka_With_Monitor.bat`
- `Start_Naraka_With_Monitor_Dev.bat`
- `Create_Naraka_Monitor_Shortcut.bat`
- `启动E键监控工具.bat`
- `启动永劫无间E键提示.bat`
- `开发启动永劫无间E键提示.bat`
- `生成中文桌面入口.bat`
- `制作便携版监控工具.bat`

BAT 只负责调用同目录 PS1，不属于最终便携包；不要把 BAT 当作用户桌面入口。

当前根目录共有 5 个 PS1。以下启动和快捷方式脚本包含中文路径或中文错误信息，必须始终保存为 UTF-8 with BOM，文件头为 `EF BB BF`；打包脚本当前也保持相同编码：

- `Start_E_Key_Monitor.ps1`
- `Start_Naraka_With_Monitor.ps1`
- `Start_Naraka_With_Monitor_Dev.ps1`
- `Create_Naraka_Monitor_Shortcut.ps1`
- `Package_E_Key_Monitor.ps1`

路径存在性检查使用 `Test-Path -LiteralPath`。脚本需要兼容 Windows PowerShell 5.1。

## 构建和发布

常用命令：

```powershell
dotnet build -c Release
dotnet run
dotnet run -- --follow-game
dotnet run -- --launch-game
dotnet publish -c Release -r win-x64 --self-contained true
.\Package_E_Key_Monitor.ps1
```

常规自动检查可以执行 Release 构建。除非用户明确要求实际联调，否则不要自动启动游戏、BAT、PS1 或桌面快捷方式创建脚本。

不要直接编辑 `bin/` 或 `obj/`。源代码修改后由 `dotnet build` 或 `dotnet publish` 重新生成输出。

## 当前核查状态（2026-07-24）

- VERIFIED：Windows x64 环境安装 .NET SDK `8.0.422`。
- VERIFIED：`dotnet build -c Release --no-restore` 退出码为 0，0 个警告、0 个错误。
- VERIFIED：`dotnet publish -c Release -r win-x64 --self-contained true --no-restore` 退出码为 0；最终便携目录和 ZIP 已在 `C:\tmp` 生成。
- VERIFIED：`dotnet format E_Key_Monitor.csproj --verify-no-changes --no-restore` 退出码为 0。
- VERIFIED：5 个根目录 PS1 均为 UTF-8 with BOM，PowerShell 5.1 静态语法解析无错误；8 个 BAT 均为纯 ASCII；根目录 `config.json` 可解析且 `gameDirectory` 默认为空；安全边界静态搜索未发现输入注入、鼠标 Hook、进程注入或直接启动/终止游戏的命中。
- PARTIAL：`dotnet test -c Release --no-restore --verbosity normal` 退出码为 0，但项目没有测试项目或测试用例，因此不能据此声称有自动化测试通过。
- VERIFIED：当前验证环境中的 `StartGame.exe`、`NarakaBladepoint.exe` 均存在，核查时 `NarakaBladepoint` 进程未运行。
- PARTIAL：已配置运行目录中的监控 EXE 能启动并保持响应，`--follow-game` 在没有游戏进程时能保持等待；通过外部关闭窗口请求后，进程在 30 秒内未退出，本次测试进程需要强制清理。根因未定位，托盘菜单“退出”路径未单独验证。
- STALE：当前桌面两个快捷方式仍指向 `C:\Users\24310\Projects\E_Key_Monitor`，不是本源码目录；项目目录内没有可用 `.lnk` 文件。需要重新运行创建脚本。
- VERIFIED：便携打包验证通过，目录和 ZIP 内只包含一个 EXE、`config.json` 和中文说明书；没有 BAT 或 PS1。
- UNKNOWN：本次没有模拟 `E` 输入、验证圆点颜色切换、实际启动游戏、验证游戏退出后的 5 秒跟随清理、从托盘创建桌面快捷方式或实际双击便携包 EXE。
- VERIFIED：本次修改集中在 `Program.cs`、`MainForm.cs`、配置、打包脚本和文档；没有修改测试、依赖或锁文件。

当前 Git 工作树核查到分支为 `main`，最近提交为 `838e43e Initial commit`；本次保留所有未提交修改，没有执行提交、重置、切换或清理。

便携文件夹移动后，应启动当前 EXE，在托盘菜单选择“生成桌面快捷方式”：

```powershell
E_Key_Monitor.exe
```

由程序生成指向当前便携文件夹的两个新桌面快捷方式。不要手工修补旧 `.lnk` 的绝对路径。

## 修改原则

- 先读现有实现，再做最小范围修改。
- 不因旧提示词重建项目或替换技术栈。
- 不破坏已经可用的圆点绘制、键盘 Hook、鼠标穿透、托盘菜单、拖动、配置和游戏跟随逻辑。
- 资源释放必须保持幂等，Hook、Timer、NotifyIcon、ContextMenuStrip 和 GDI 资源都要正常释放。
- 修改脚本后必须重新检查 BAT 的 ASCII 和 PS1 的 BOM。
- 修改代码后至少执行 `dotnet build -c Release`，并报告真实结果。
- 不要声称未实际执行的 GUI、键盘或游戏联动测试已经通过。
- 文档、源码和脚本中的路径说明必须以当前 C 盘源码与上述真实游戏目录为准。

## Agent skills

### Issue tracker

Local markdown under `.scratch/` (one directory per effort). See `docs/agents/issue-tracker.md`.

### Triage labels

Five canonical roles with identical label strings. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `AGENTS.md` + `docs/agents/` + `docs/adr/`. See `docs/agents/domain.md`.

若 `.scratch/WORKFLOW-ACTIVE.md` 存在 → 先调 autopilot 再继续；新任务开局拿不准走哪条路时 → 也先调它分流。
